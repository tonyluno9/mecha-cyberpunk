using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas jugables de la pistola a pie: prefab real, objetivos y muros creados en la prueba, entrada simulada.</summary>
    public class PistolaAPiePruebas : BaseJugadorAPie
    {
        /// <summary>Objetivo de prueba que solo cuenta los daños recibidos.</summary>
        private class ObjetivoContador : MonoBehaviour, IRecibeDanio
        {
            public int Golpes;
            public InfoDanio Ultimo;

            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Golpes++;
                Ultimo = info;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private PistolaAPie pistola;
        private ArmasAPie armas;
        private GolpeAPie golpe;
        private GameObject objetivo;
        private int disparos;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            pistola = jugador.GetComponent<PistolaAPie>();
            armas = jugador.GetComponent<ArmasAPie>();
            golpe = jugador.GetComponent<GolpeAPie>();
            disparos = 0;
            pistola.Disparo += () => disparos++;
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            if (objetivo != null)
            {
                Object.Destroy(objetivo);
            }
            base.TearDown();
        }

        private MunecoAPie CrearMuneco(float distancia)
        {
            objetivo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objetivo.transform.position = new Vector3(0f, 4f, distancia);
            objetivo.transform.localScale = new Vector3(1f, 8f, 1f);
            MunecoAPie muneco = objetivo.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            return muneco;
        }

        private ObjetivoContador CrearContador(float distancia)
        {
            objetivo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objetivo.transform.position = new Vector3(0f, 4f, distancia);
            objetivo.transform.localScale = new Vector3(1f, 8f, 1f);
            ObjetivoContador contador = objetivo.AddComponent<ObjetivoContador>();
            Physics.SyncTransforms();
            return contador;
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        private IEnumerator Tecla(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pistola_ConDosYClicAMunecoA20m_RecibeDanioDeDisparo()
        {
            yield return Calentar();
            MunecoAPie muneco = CrearMuneco(20f);
            InfoDanio recibida = default;
            int impactos = 0;
            EventosPelea.ImpactoRecibido += (quien, info, resultado) =>
            {
                recibida = info;
                impactos++;
            };
            yield return null;
            float maxima = muneco.Maxima;

            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva);
            yield return Clic();

            Assert.AreEqual(1, disparos);
            Assert.AreEqual(maxima - pistola.Datos.Danio, muneco.Actual, 0.0001f);
            Assert.AreEqual(1, impactos);
            Assert.AreEqual(TipoDanio.Disparo, recibida.Tipo);
            Assert.AreEqual(pistola.Datos.Danio, recibida.Cantidad, 0.0001f);
            Assert.AreEqual(pistola.TamanoCargador - 1, pistola.Balas);
        }

        [UnityTest]
        public IEnumerator Pistola_MunecoA80m_FueraDeAlcance_NoRecibeDanio()
        {
            yield return Calentar();
            MunecoAPie muneco = CrearMuneco(80f);
            yield return null;

            yield return Tecla(teclado.digit2Key);
            yield return Clic();

            Assert.AreEqual(1, disparos, "El tiro sale aunque no llegue a nadie");
            Assert.AreEqual(muneco.Maxima, muneco.Actual, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Pistola_MuroEntreMedio_ProtegeAlMuneco()
        {
            yield return Calentar();
            MunecoAPie muneco = CrearMuneco(20f);
            Caja(new Vector3(0f, 4f, 10f), new Vector3(6f, 8f, 0.5f));
            Physics.SyncTransforms();
            yield return null;

            yield return Tecla(teclado.digit2Key);
            yield return Clic();

            Assert.AreEqual(1, disparos);
            Assert.AreEqual(muneco.Maxima, muneco.Actual, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Pistola_VeinteClicsEnUnSegundo_DisparaComoMaximoCincoVeces()
        {
            yield return Calentar();
            yield return Tecla(teclado.digit2Key);
            int clics = 0;
            float fin = Time.time + 1f;
            while (Time.time < fin)
            {
                yield return Clic();
                clics++;
            }
            Assert.GreaterOrEqual(clics, 8, "La prueba debe hacer varios clics para ser válida");
            Assert.LessOrEqual(disparos, 5);
            Assert.GreaterOrEqual(disparos, 3);
        }

        [UnityTest]
        public IEnumerator Pistola_DespuesDeOchoDisparos_ElSiguienteClicNoHaceDanioYRecarga()
        {
            yield return Calentar();
            ObjetivoContador contador = CrearContador(15f);
            yield return null;
            yield return Tecla(teclado.digit2Key);

            for (int i = 0; i < pistola.TamanoCargador; i++)
            {
                yield return Clic();
                yield return Esperar(pistola.Datos.TiempoEntreDisparos + 0.05f);
            }
            Assert.AreEqual(8, contador.Golpes);
            Assert.AreEqual(0, pistola.Balas);
            Assert.IsFalse(pistola.Recargando);
            Assert.AreEqual(TipoDanio.Disparo, contador.Ultimo.Tipo);

            yield return Clic();
            Assert.AreEqual(8, contador.Golpes, "El clic vacío no hace daño");
            Assert.AreEqual(8, disparos);
            Assert.IsTrue(pistola.Recargando);

            yield return Clic();
            Assert.AreEqual(8, contador.Golpes, "Recargando no dispara");

            yield return Esperar(pistola.Datos.TiempoRecarga + 0.1f);
            Assert.IsFalse(pistola.Recargando);
            Assert.AreEqual(8, pistola.Balas);

            yield return Clic();
            Assert.AreEqual(9, contador.Golpes, "Tras recargar vuelve a disparar");
        }

        [UnityTest]
        public IEnumerator Pistola_TeclaR_RecargaUnCargadorIncompleto()
        {
            yield return Calentar();
            yield return Tecla(teclado.digit2Key);
            yield return Clic();
            Assert.AreEqual(7, pistola.Balas);

            yield return Tecla(teclado.rKey);
            Assert.IsTrue(pistola.Recargando);
            yield return Esperar(pistola.Datos.TiempoRecarga + 0.1f);
            Assert.AreEqual(8, pistola.Balas);
        }

        [UnityTest]
        public IEnumerator Pistola_ConUnoVuelvenLosGolpesYNoDispara()
        {
            yield return Calentar();
            MunecoAPie muneco = CrearMuneco(1.2f);
            Caja(new Vector3(0f, 4f, 30f), new Vector3(6f, 8f, 0.5f));
            yield return null;
            yield return Tecla(teclado.digit2Key);
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva);
            float maxima = muneco.Maxima;

            yield return Clic();
            yield return Esperar(0.4f);

            Assert.AreEqual(0, disparos);
            Assert.AreEqual(8, pistola.Balas);
            Assert.Less(muneco.Actual, maxima, "El golpe de puños sigue funcionando");
        }

        [UnityTest]
        public IEnumerator Pistola_ConDosElClicNoGolpea()
        {
            yield return Calentar();
            MunecoAPie muneco = CrearMuneco(1.2f);
            yield return null;
            yield return Tecla(teclado.digit2Key);

            yield return Clic();
            yield return Esperar(0.4f);

            Assert.AreEqual(1, disparos);
            Assert.AreEqual(muneco.Maxima - pistola.Datos.Danio, muneco.Actual, 0.0001f, "Solo el daño de la bala, sin golpe");
        }

        [UnityTest]
        public IEnumerator Pistola_EnKO_NoDispara()
        {
            yield return Calentar();
            yield return Tecla(teclado.digit2Key);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            vida.RecibirDanio(new InfoDanio(vida.Maxima + 1f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.IsTrue(vida.EstaKO);
            yield return null;

            yield return Clic();
            yield return Tecla(teclado.rKey);

            Assert.AreEqual(0, disparos);
            Assert.AreEqual(8, pistola.Balas);

            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "En KO tampoco se cambia de arma");
        }

        [UnityTest]
        public IEnumerator Pistola_DisparandoAlPisoASusPies_NoSeHaceDanio()
        {
            yield return Calentar();
            yield return Tecla(teclado.digit2Key);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            int impactos = 0;
            EventosPelea.ImpactoRecibido += (quien, info, resultado) => impactos++;

            Set(raton.delta, new Vector2(0f, -1000000f));
            yield return null;
            yield return null;
            Assert.Greater(controlador.Inclinacion, 55f, "La cámara mira al piso (tope de inclinación 60°)");

            yield return Clic();

            Assert.AreEqual(1, disparos);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.0001f);
            Assert.AreEqual(0, impactos);
        }

        [UnityTest]
        public IEnumerator Pistola_NoSeCambiaDeArmaEnLaPreparacionDeUnGolpe()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            yield return null;

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            Assert.AreNotEqual(MechaCyberpunk.Combate.Golpe.FaseGolpe.Listo, golpe.Fase);

            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, "No se cambia de arma mientras se prepara el golpe");
        }

        [UnityTest]
        public IEnumerator Pistola_DiezYSeisMarcasComoMaximoVisibles()
        {
            yield return Calentar();
            Caja(new Vector3(0f, 4f, 6f), new Vector3(8f, 8f, 0.5f));
            Physics.SyncTransforms();
            yield return Tecla(teclado.digit2Key);

            for (int i = 0; i < 20; i++)
            {
                if (pistola.Balas == 0)
                {
                    yield return Tecla(teclado.rKey);
                    yield return Esperar(pistola.Datos.TiempoRecarga + 0.05f);
                }
                yield return Clic();
                yield return Esperar(pistola.Datos.TiempoEntreDisparos + 0.02f);
            }

            int visibles = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name == "MarcaImpacto" && t.gameObject.activeSelf)
                {
                    visibles++;
                }
            }
            Assert.Greater(visibles, 0, "El tiro deja marca en el muro");
            Assert.LessOrEqual(visibles, 16);
        }
    }
}
