using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Teclas de prueba del reloj en Pruebas_Alan.unity (N, M y T), con entrada simulada.</summary>
    public class ControlRelojPruebas : InputTestFixture
    {
        private const string RutaPruebas = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const float ToleranciaHora = 0.05f;
        private const float EscalaEsperada = 30f;
        private const float SegundosTresDiasRapidos = 45f;

        private Keyboard teclado;
        private RelojMundo reloj;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        private IEnumerator CargarPruebas()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaPruebas, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            reloj = Object.FindFirstObjectByType<RelojMundo>();
            Assert.IsNotNull(reloj, "Pruebas_Alan debe traer un RelojMundo");
        }

        public override void TearDown()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
            base.TearDown();
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator N_Pulsada_DejaElRelojALas22()
        {
            yield return CargarPruebas();
            reloj.AvanzarHasta(10f);
            yield return Pulsar(teclado.nKey);
            Assert.AreEqual(22f, reloj.Hora, ToleranciaHora);
            Assert.IsTrue(reloj.EsDeNoche);
        }

        [UnityTest]
        public IEnumerator M_Pulsada_DejaElRelojALas12()
        {
            yield return CargarPruebas();
            yield return Pulsar(teclado.mKey);
            Assert.AreEqual(12f, reloj.Hora, ToleranciaHora);
            Assert.IsFalse(reloj.EsDeNoche);
            Assert.AreSame(reloj, Registro<IRelojMundo>.Actual);
        }

        [UnityTest]
        public IEnumerator T_Pulsada_MultiplicaLaVelocidadPor30YOtraVezLaRegresa()
        {
            yield return CargarPruebas();
            Assert.AreEqual(1f, reloj.EscalaTiempo);
            yield return Pulsar(teclado.tKey);
            Assert.AreEqual(EscalaEsperada, reloj.EscalaTiempo);
            yield return Pulsar(teclado.tKey);
            Assert.AreEqual(1f, reloj.EscalaTiempo);
        }

        [UnityTest]
        public IEnumerator T_ConVelocidadX30_PasaUnDiaCompletoSinErroresEnConsola()
        {
            yield return CargarPruebas();
            int diaInicial = reloj.Dia;
            LogAssert.Expect(LogType.Log, "[Mundo] Reloj de prueba: velocidad x30");
            yield return Pulsar(teclado.tKey);
            float fin = Time.realtimeSinceStartup + SegundosTresDiasRapidos;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.GreaterOrEqual(reloj.Dia - diaInicial, 1, "A x30 un día de 1200 s dura 40 s reales");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
