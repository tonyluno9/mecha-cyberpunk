using System.Collections;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Pruebas;
using MechaCyberpunk.IA.Rival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>Pruebas de T-021 sobre la escena real Pruebas_Tony: el rival arranca sin control y la tecla C se lo da.</summary>
    public class RivalEnEscenaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaEsperada = 15f;
        private const float MargenDistancia = 1f;
        private const float DistanciaMinimaAlSaco = 8f;

        private Keyboard teclado;
        private RivalBasico rival;
        private PeleadorMecha peleadorRival;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Scene escena = SceneManager.GetSceneByPath(RutaEscena);
            if (escena.IsValid())
            {
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    Object.Destroy(raiz);
                }
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            rival = Object.FindFirstObjectByType<RivalBasico>();
            Assert.IsNotNull(rival, "Falta el rival en Pruebas_Tony");
            peleadorRival = rival.GetComponent<PeleadorMecha>();
            yield return Calentar();
        }

        private static GameObject BuscarJugador()
        {
            foreach (MonoBehaviour candidato in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidato is IPeleador peleador && peleador.Lado == LadoPelea.Jugador)
                {
                    return candidato.gameObject;
                }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Escena_TrasCargar_ElRivalEstaAUnos15mDelJugadorMirandoloYSinControl()
        {
            yield return CargarEscena();
            GameObject jugador = BuscarJugador();
            Assert.IsNotNull(jugador);

            Vector3 haciaJugador = jugador.transform.position - rival.transform.position;
            haciaJugador.y = 0f;
            Assert.AreEqual(DistanciaEsperada, haciaJugador.magnitude, MargenDistancia);
            Assert.Greater(Vector3.Dot(rival.transform.forward, haciaJugador.normalized), 0.99f, "Mira al jugador");
            Assert.AreSame(jugador, rival.Objetivo, "Lo encontró solo");
            Assert.AreEqual(LadoPelea.Rival, peleadorRival.Lado);
            Assert.IsFalse(peleadorRival.ControlActivo, "Arranca sin control");
            Assert.IsFalse(rival.enabled);
            Assert.IsNotNull(Object.FindFirstObjectByType<AlternadorControlRival>(), "El alternador está en la escena");

            GameObject saco = GameObject.Find("SacoDePrueba");
            GameObject atacante = GameObject.Find("AtacanteDePrueba");
            Assert.Greater(Vector3.Distance(rival.transform.position, saco.transform.position), DistanciaMinimaAlSaco, "Lejos del saco");
            Assert.Greater(Vector3.Distance(rival.transform.position, atacante.transform.position), DistanciaMinimaAlSaco, "Lejos del atacante");
        }

        [UnityTest]
        public IEnumerator Escena_SinTocarNada_ElRivalNoSeMueveNiAtacaPor3s()
        {
            yield return CargarEscena();
            GameObject jugador = BuscarJugador();
            VidaMecha vida = jugador.GetComponent<VidaMecha>();
            Vector3 inicio = rival.transform.position;
            float vidaAntes = vida.Actual;

            yield return Esperar(3f);

            Assert.AreEqual(0f, Vector3.Distance(inicio, rival.transform.position), 0.01f);
            Assert.AreEqual(vidaAntes, vida.Actual);
        }

        [UnityTest]
        public IEnumerator Escena_TeclaC_AlternaElControlYElRivalCaminaHaciaElJugador()
        {
            yield return CargarEscena();
            GameObject jugador = BuscarJugador();
            float antes = Vector3.Distance(rival.transform.position, jugador.transform.position);

            Press(teclado.cKey);
            yield return null;
            Release(teclado.cKey);
            yield return null;
            Assert.IsTrue(peleadorRival.ControlActivo, "C le da el control");
            Assert.IsTrue(rival.enabled);

            yield return Esperar(2f);
            float despues = Vector3.Distance(rival.transform.position, jugador.transform.position);
            Assert.Less(despues, antes - 3f, "Camina hacia el jugador");

            Press(teclado.cKey);
            yield return null;
            Release(teclado.cKey);
            yield return null;
            Assert.IsFalse(peleadorRival.ControlActivo, "Otra C se lo quita");
            Vector3 quieto = rival.transform.position;
            yield return Esperar(1f);
            Assert.AreEqual(0f, Vector3.Distance(quieto, rival.transform.position), 0.01f);
        }
    }
}
