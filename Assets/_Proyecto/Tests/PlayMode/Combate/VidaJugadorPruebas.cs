using System.Collections;
using System.Text.RegularExpressions;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Movimiento;
using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Combate
{
    /// <summary>Pruebas jugables de T-013 sobre la escena real Pruebas_Tony: tecla H, barra, sacudida, KO y control.</summary>
    public class VidaJugadorPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DanioDeLaEscena = 10f;
        private const float VidaMaximaDelAsset = 100f;

        private class PeleadorFalso : IPeleador
        {
            public string Nombre => "Falso";
            public LadoPelea Lado => LadoPelea.Rival;
            public Transform Raiz => null;
            public IVida Vida => null;
            public bool ControlActivo => true;
            public void ActivarControl(bool activo) { }
            public void PrepararPelea(Pose inicio) { }
        }

        private Keyboard teclado;
        private VidaMecha vida;
        private PeleadorMecha peleador;
        private SacudidaCabina sacudida;
        private PantallaVidaCabina pantalla;
        private int avisosKO;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            Application.logMessageReceived += ContarKO;
            avisosKO = 0;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= ContarKO;
            VaciarEscenaDePruebas();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // La escena cargada sigue viva al terminar la prueba: se vacía para no estorbar a las pruebas siguientes.
        private static void VaciarEscenaDePruebas()
        {
            Scene escena = SceneManager.GetSceneByPath(RutaEscena);
            if (!escena.IsValid())
            {
                return;
            }
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
        }

        private void ContarKO(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Log && mensaje.StartsWith("[Combate] KO"))
            {
                avisosKO++;
            }
        }

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            // La escena también tiene un saco con VidaMecha: se busca la del mecha del jugador.
            ControladorMecha jugador = Object.FindFirstObjectByType<ControladorMecha>();
            Assert.IsNotNull(jugador, "Falta el mecha del jugador en la escena");
            vida = jugador.GetComponent<VidaMecha>();
            peleador = jugador.GetComponent<PeleadorMecha>();
            sacudida = Object.FindFirstObjectByType<SacudidaCabina>();
            pantalla = Object.FindFirstObjectByType<PantallaVidaCabina>();
            Assert.IsNotNull(vida, "Falta VidaMecha en la escena");
            Assert.IsNotNull(peleador);
            Assert.IsNotNull(sacudida);
            Assert.IsNotNull(pantalla);
            yield return Calentar();
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

        private IEnumerator PulsarH()
        {
            Press(teclado.hKey);
            yield return null;
            Release(teclado.hKey);
            yield return null;
        }

        private static InfoDanio Golpe(float cantidad)
        {
            return new InfoDanio(cantidad, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null);
        }

        [UnityTest]
        public IEnumerator Escena_AlEmpezar_BarraLlenaYTextoVida100()
        {
            yield return CargarEscena();
            Assert.AreEqual(VidaMaximaDelAsset, vida.Actual);
            Assert.AreEqual("VIDA 100", pantalla.Texto.text);
            Assert.AreEqual(1f, pantalla.Relleno.fillAmount, 1e-4f);
            Assert.AreEqual(LadoPelea.Jugador, peleador.Lado);
            Assert.IsTrue(peleador.ControlActivo);
        }

        [UnityTest]
        public IEnumerator Escena_PantallaVida_CuelgaDelAnclaDelTablero()
        {
            yield return CargarEscena();
            Canvas canvas = pantalla.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.AreEqual("AnclaPantallaVida", pantalla.transform.parent.name);
            Camera camara = peleador.GetComponentInChildren<Camera>();
            Vector3 vista = camara.WorldToViewportPoint(pantalla.transform.position);
            Assert.Greater(vista.z, 0f);
            Assert.Less(vista.y, 0.5f, "Está en la parte baja de la vista");
        }

        [UnityTest]
        public IEnumerator Escena_PresionarH_BajanBarraYNumeroYLaCabinaSeSacude()
        {
            yield return CargarEscena();
            Transform movil = sacudida.transform;

            yield return PulsarH();

            Assert.AreEqual(VidaMaximaDelAsset - DanioDeLaEscena, vida.Actual, 1e-4f);
            Assert.AreEqual("VIDA 90", pantalla.Texto.text);
            Assert.AreEqual(0.9f, pantalla.Relleno.fillAmount, 1e-4f);
            Assert.Greater(sacudida.Intensidad, 0f);
            Assert.Greater(movil.localPosition.magnitude, 0f, "La cabina se movió");

            yield return PulsarH();
            Assert.AreEqual("VIDA 80", pantalla.Texto.text);
        }

        [UnityTest]
        public IEnumerator Escena_Sacudida_DanioGrandeSacudeMasYSeApagaEnMenosDeUnSegundo()
        {
            yield return CargarEscena();

            vida.RecibirDanio(Golpe(2f));
            float chica = sacudida.Intensidad;
            yield return Esperar(1f);
            Assert.AreEqual(0f, sacudida.Intensidad, "Se apagó antes de un segundo");
            Assert.AreEqual(0f, sacudida.transform.localPosition.magnitude, 1e-5f, "La cabina volvió a su sitio");

            vida.RecibirDanio(Golpe(25f));
            float grande = sacudida.Intensidad;
            Assert.Greater(grande, chica);
            yield return Esperar(1f);
            Assert.AreEqual(0f, sacudida.Intensidad);
            Assert.AreEqual(0f, sacudida.transform.localPosition.magnitude, 1e-5f);
        }

        [UnityTest]
        public IEnumerator Escena_Sacudida_ImpactoDeOtroPeleador_NoSacude()
        {
            yield return CargarEscena();
            IPeleador otro = new PeleadorFalso();
            EventosPelea.PublicarImpacto(otro, Golpe(30f), new ResultadoDanio(30f, TipoBloqueo.Ninguno, false));
            yield return null;
            Assert.AreEqual(0f, sacudida.Intensidad);
        }

        [UnityTest]
        public IEnumerator Escena_Sacudida_ImpactoSinDanio_DaSacudidaMinima()
        {
            yield return CargarEscena();
            EventosPelea.PublicarImpacto(peleador, Golpe(10f), new ResultadoDanio(0f, TipoBloqueo.Perfecto, false));
            Assert.Greater(sacudida.Intensidad, 0f);
            vida.RecibirDanio(Golpe(30f));
            Assert.Greater(sacudida.Intensidad, 0.5f);
        }

        [UnityTest]
        public IEnumerator Escena_DiezH_KOUnSoloAvisoYElMechaNoCamina()
        {
            yield return CargarEscena();
            for (int i = 0; i < 10; i++)
            {
                yield return PulsarH();
            }

            Assert.IsTrue(vida.EstaKO);
            Assert.AreEqual(0f, vida.Actual);
            Assert.AreEqual("VIDA 0", pantalla.Texto.text);
            Assert.AreEqual(0f, pantalla.Relleno.fillAmount);
            Assert.AreEqual(1, avisosKO, "Un solo aviso de KO");
            Assert.IsFalse(peleador.ControlActivo);

            Vector3 antes = peleador.transform.position;
            float giroAntes = peleador.transform.eulerAngles.y;
            Press(teclado.wKey);
            Set(Mouse.current.delta, new Vector2(500f, 0f));
            yield return Esperar(0.6f);
            Release(teclado.wKey);
            Assert.AreEqual(0f, Vector3.Distance(antes, peleador.transform.position), 1e-3f, "No camina en KO");
            Assert.AreEqual(giroAntes, peleador.transform.eulerAngles.y, 1e-3f, "No gira en KO");
        }

        [UnityTest]
        public IEnumerator Escena_AntesDelKO_ElMechaSiCamina()
        {
            yield return CargarEscena();
            Vector3 antes = peleador.transform.position;
            Press(teclado.wKey);
            yield return Esperar(0.6f);
            Release(teclado.wKey);
            Assert.Greater(Vector3.Distance(antes, peleador.transform.position), 0.3f);
        }

        [UnityTest]
        public IEnumerator Escena_HDespuesDelKO_NoBajaNoSacudeNiDaErrores()
        {
            yield return CargarEscena();
            for (int i = 0; i < 10; i++)
            {
                yield return PulsarH();
            }
            yield return Esperar(1f);
            Assert.IsTrue(vida.EstaKO);
            LogAssert.Expect(LogType.Log, new Regex(@"\[Combate\] KO"));

            yield return PulsarH();
            yield return PulsarH();

            Assert.AreEqual(0f, vida.Actual);
            Assert.AreEqual(0f, sacudida.Intensidad);
            Assert.AreEqual(0f, sacudida.transform.localPosition.magnitude, 1e-5f);
            Assert.AreEqual(1, avisosKO);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Escena_VidaMaximaDe50_ElKOLlegaConLaMitadDeGolpes()
        {
            yield return CargarEscena();
            DatosMecha cincuenta = ScriptableObject.CreateInstance<DatosMecha>();
            SerializedObject so = new SerializedObject(cincuenta);
            so.FindProperty("vidaMaxima").floatValue = 50f;
            so.ApplyModifiedPropertiesWithoutUndo();
            vida.Configurar(cincuenta);
            Assert.AreEqual("VIDA 50", pantalla.Texto.text);

            for (int i = 0; i < 4; i++)
            {
                yield return PulsarH();
            }
            Assert.IsFalse(vida.EstaKO);
            yield return PulsarH();

            Assert.IsTrue(vida.EstaKO, "5 golpes de 10 con 50 de vida");
            Assert.AreEqual(1, avisosKO);
            Object.Destroy(cincuenta);
        }

        [UnityTest]
        public IEnumerator Escena_VidaBaja_LaBarraCambiaDeColor()
        {
            yield return CargarEscena();
            Color normal = pantalla.Relleno.color;
            vida.RecibirDanio(Golpe(85f));
            Assert.AreNotEqual(normal, pantalla.Relleno.color);
            Assert.Greater(pantalla.Relleno.color.r, pantalla.Relleno.color.g, "Rojo con poca vida");
        }
    }
}
