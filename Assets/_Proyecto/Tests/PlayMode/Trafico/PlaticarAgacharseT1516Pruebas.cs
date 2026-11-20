using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1516: los peatones se quedan platicando en corros y, al oír disparos, algunos se agachan antes de huir.</summary>
    public class PlaticarAgacharseT1516Pruebas
    {
        private static readonly Vector3[] Ruta = { new Vector3(0f, 0f, 0f), new Vector3(200f, 0f, 0f) };

        private GameObject raiz;
        private ConfigPeatones config;

        [SetUp]
        public void Preparar()
        {
            EventosRuido.LimpiarSuscriptores();
            config = ScriptableObject.CreateInstance<ConfigPeatones>();
            raiz = new GameObject("Peatones");
        }

        [TearDown]
        public void Limpiar()
        {
            EventosRuido.LimpiarSuscriptores();
            Object.Destroy(raiz);
            Object.Destroy(config);
        }

        private PeatonMundo CrearPeaton(float metros, int semilla)
        {
            GameObject objeto = new GameObject("Peaton");
            objeto.transform.SetParent(raiz.transform, false);
            objeto.SetActive(false);
            PeatonMundo peaton = objeto.AddComponent<PeatonMundo>();
            peaton.Preparar(Ruta, config, new System.Random(semilla), () => (Vector3?)null, metros / config.Velocidad);
            objeto.SetActive(true);
            return peaton;
        }

        [UnityTest]
        public IEnumerator Disparo_ConProbabilidadUno_HaceQueSeAgacheYSeQuedeQuieto_YLuegoSeLevanta()
        {
            config.ConfigurarReaccion(0, 1f, 1.5f);
            PeatonMundo peaton = CrearPeaton(50f, 1);
            yield return null;
            Vector3 inicio = peaton.transform.position;
            EventosRuido.PublicarDisparo(new Vector3(40f, 0f, 0f));
            Assert.IsTrue(peaton.Agachado, "se agacha al oír el disparo");
            Assert.IsFalse(peaton.Huyendo, "agachado todavía no huye");
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(peaton.Agachado);
            Assert.AreEqual(inicio.x, peaton.transform.position.x, 0.05f, "agachado se queda en su lugar");
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(peaton.Agachado, "después de unos segundos se levanta");
        }

        [UnityTest]
        public IEnumerator Disparo_SinProbabilidadDeAgacharse_HuyeComoAntes()
        {
            config.ConfigurarReaccion(0, 0f, 1.5f);
            PeatonMundo peaton = CrearPeaton(50f, 1);
            yield return null;
            EventosRuido.PublicarDisparo(new Vector3(40f, 0f, 0f));
            Assert.IsFalse(peaton.Agachado);
            Assert.IsTrue(peaton.Huyendo);
        }

        [UnityTest]
        public IEnumerator Corro_DePareja_SeQuedaFrenteAFrente_YUnDisparoLosSaca()
        {
            config.ConfigurarReaccion(8, 0f, 1.5f);
            PeatonMundo a = CrearPeaton(50f, 1);
            PeatonMundo b = CrearPeaton(50f, 2);
            a.Platicar(0, 2);
            b.Platicar(1, 2);
            yield return null;
            Assert.IsTrue(a.Platicando && b.Platicando);
            Vector3 antesA = a.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(ReglaPlatica.RadioCorro * 2f, Vector3.Distance(a.transform.position, b.transform.position), 0.1f, "uno frente al otro, a la distancia del corro");
            Assert.AreEqual(0f, Vector3.Distance(antesA, a.transform.position), 0.05f, "platicando no camina");
            Vector3 haciaB = (b.transform.position - a.transform.position).normalized;
            Assert.Greater(Vector3.Dot(a.transform.forward, haciaB), 0.9f, "se miran");
            EventosRuido.PublicarDisparo(new Vector3(40f, 0f, 0f));
            Assert.IsFalse(a.Platicando, "el disparo corta la plática");
            Assert.IsTrue(a.Huyendo, "y huye como los demás");
        }
    }
}
