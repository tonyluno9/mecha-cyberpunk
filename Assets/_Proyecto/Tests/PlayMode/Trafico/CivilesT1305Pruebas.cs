using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1305: los civiles reciben daño y mueren tirados, huyen al oír disparos cerca y al ser heridos.</summary>
    public class CivilesT1305Pruebas
    {
        private const float SegundosDeCaida = 0.8f;
        private const float MaximoUpYTirado = 0.2f;
        private static readonly Vector3[] Ruta = { new Vector3(0f, 0f, 0f), new Vector3(200f, 0f, 0f) };

        private GameObject raiz;
        private ConfigPeatones config;

        [SetUp]
        public void Preparar()
        {
            EventosRuido.LimpiarSuscriptores();
            config = ScriptableObject.CreateInstance<ConfigPeatones>();
            raiz = new GameObject("PeatonesT1305");
        }

        [TearDown]
        public void Limpiar()
        {
            EventosRuido.LimpiarSuscriptores();
            Object.Destroy(raiz);
            Object.Destroy(config);
        }

        private PeatonMundo CrearPeaton(float metros)
        {
            GameObject objeto = new GameObject("Civil");
            objeto.transform.SetParent(raiz.transform, false);
            objeto.SetActive(false);
            PeatonMundo peaton = objeto.AddComponent<PeatonMundo>();
            peaton.Preparar(Ruta, config, new System.Random(1), () => (Vector3?)new Vector3(metros, 0f, 30f), metros / config.Velocidad);
            objeto.SetActive(true);
            return peaton;
        }

        private static InfoDanio Bala(float cantidad, Vector3 donde) => new InfoDanio(cantidad, TipoDanio.Disparo, donde, Vector3.forward, null);

        [UnityTest]
        public IEnumerator Danio_MortalAlCivil_MuereYQuedaTiradoEnElPiso()
        {
            PeatonMundo civil = CrearPeaton(50f);
            yield return null;
            ResultadoDanio resultado = civil.RecibirDanio(Bala(config.VidaPeaton + 1f, civil.transform.position));
            Assert.IsTrue(resultado.CausoKO, "El daño lo mata");
            Assert.IsTrue(civil.Muerto);
            yield return new WaitForSeconds(SegundosDeCaida);
            Assert.Less(Mathf.Abs(civil.transform.up.y), MaximoUpYTirado, "Queda acostado: su arriba es casi horizontal");
        }

        [UnityTest]
        public IEnumerator Danio_NoMortal_LoDejaVivoYHuyeSiElJugadorEstaLejos()
        {
            PeatonMundo civil = CrearPeaton(50f);
            yield return null;
            ResultadoDanio resultado = civil.RecibirDanio(Bala(config.VidaPeaton / 4f, civil.transform.position));
            Assert.IsFalse(resultado.CausoKO);
            Assert.IsFalse(civil.Muerto);
            Assert.IsTrue(civil.Huyendo, "Con el jugador a 30 m no pelea: huye");
        }

        [UnityTest]
        public IEnumerator Disparo_Cercano_LoHaceHuir()
        {
            PeatonMundo civil = CrearPeaton(50f);
            yield return null;
            EventosRuido.PublicarDisparo(new Vector3(50f - config.RadioOirDisparo / 2f, 0f, 0f));
            Assert.IsTrue(civil.Huyendo);
        }

        [UnityTest]
        public IEnumerator Muerto_YaNoHuyeAlOirDisparos()
        {
            PeatonMundo civil = CrearPeaton(50f);
            yield return null;
            civil.RecibirDanio(Bala(config.VidaPeaton + 1f, civil.transform.position));
            EventosRuido.PublicarDisparo(civil.transform.position);
            Assert.IsFalse(civil.Huyendo);
        }
    }
}
