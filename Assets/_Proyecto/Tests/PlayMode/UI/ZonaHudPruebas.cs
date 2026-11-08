using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-523 en Ciudad_Prologo: el panel de la zona (arriba a la derecha) dice la zona donde estás y cambia al cruzar; el centro no lleva cartel de lugar.</summary>
    public class ZonaHudPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private static readonly Vector3 EnSuburbios = new Vector3(-120f, 0.5f, 0f);
        private static readonly Vector3 EnTianguis = new Vector3(0f, 0.5f, 0f);
        private static readonly Vector3 EnNeon = new Vector3(120f, 0.5f, 120f);

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            Scene escena = SceneManager.GetActiveScene();
            if (escena.name == "Ciudad_Prologo")
            {
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    UnityEngine.Object.Destroy(raiz);
                }
            }
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Zona_AlCruzarDeZona_ElPanelDiceLaZonaDondeEstas()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null;
            }, "el director arrancó", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            HudTerritorio zona = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            Assert.IsNotNull(zona);
            ITerritorio territorio = Registro<ITerritorio>.Actual;

            // Al empezar, el panel dice la zona donde está el jugador.
            yield return EsperarHasta(() => zona.TextoZona.text.Length > 0, "la zona al empezar", 20f);
            string alInicio = territorio.Nombre(territorio.ZonaEn(jugador.Raiz.position));
            StringAssert.StartsWith(alInicio, zona.TextoZona.text, "el panel dice la zona del jugador al empezar");

            string[] esperadas = { "SUBURBIOS", "EL TIANGUIS", "NEÓN ALTO" };
            Vector3[] sitios = { EnSuburbios, EnTianguis, EnNeon };
            for (int i = 0; i < sitios.Length; i++)
            {
                string esperada = esperadas[i];
                jugador.Colocar(new Pose(sitios[i], Quaternion.identity));
                yield return EsperarHasta(() => zona.TextoZona.text.StartsWith(esperada), "la zona " + esperada, 5f);
                string id = territorio.ZonaEn(jugador.Raiz.position);
                Assert.IsTrue(zona.TextoZona.transform.parent.gameObject.activeSelf, "el panel se ve");
                Assert.AreEqual(FormatoTerritorio.ColorDe(territorio.Dueno(id)), zona.TextoZona.color, "el color es el del dueño de " + esperada);
            }

            // Sin cartel de lugar en el centro: ningún texto dibujado fuera de la cola repite el nombre de la ciudad.
            foreach (Text t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                // T-701: el HUD nuevo lleva el nombre de la zona chico y delgado arriba al centro (TituloZona de CanvasH10), que se desvanece solo; no es el cartel de lugar viejo.
                bool tituloDelHudNuevo = t.name == "TituloZona" && t.canvas.name == "CanvasH10";
                if (t.enabled && t.text.Contains("MEGALOPOLIS") && t.canvas.name != "CanvasAvisos" && !tituloDelHudNuevo)
                {
                    Assert.Fail("cartel de lugar suelto fuera de la cola: " + t.name);
                }
            }
        }

        /// <summary>T-524: el panel de zona dibuja el nombre grande (Teko, 40 px o más) y debajo RESPETO con su barra.</summary>
        [UnityTest]
        public IEnumerator Zona_PanelConNombreGrandeYRespetoConBarra()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null;
            }, "el director arrancó", 30f);
            Registro<IJugador>.Actual.Colocar(new Pose(EnTianguis, Quaternion.identity));
            HudTerritorio zona = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            yield return EsperarHasta(() => zona.NombreZona.text == "EL TIANGUIS", "el nombre de la zona", 10f);
            Assert.GreaterOrEqual(zona.NombreZona.fontSize, 40, "El nombre de la zona es grande");
            Assert.IsTrue(zona.NombreZona.enabled, "El nombre se dibuja");
            Assert.IsFalse(zona.TextoZona.enabled, "La línea larga queda solo como dato");
            Assert.AreEqual("RESPETO", zona.EtiquetaRespeto.text);
            Assert.IsNotNull(zona.TextoZona.transform.parent.Find("HuecoRespeto/Respeto"), "La barra de respeto existe");
            Assert.AreEqual(zona.NombreZona.font, zona.TextoZona.font, "El nombre usa la fuente Teko del HUD");
        }

        /// <summary>Con CAPTURAR_T523=1 guarda docs/capturas/T-523-hud.png (1080p) y T-523-hud-1440.png con el HUD en juego en Suburbios.</summary>
        [UnityTest]
        public IEnumerator Captura_HudEnSuburbios()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T523") != "1")
            {
                Assert.Ignore("Solo con CAPTURAR_T523=1");
            }
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<IPandillaJugador>.Actual != null;
            }, "el director arrancó", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
            jugador.Colocar(new Pose(new Vector3(-80f, 0.5f, 0f), Quaternion.LookRotation(Vector3.right)));
            Registro<ICartera>.Actual.Abonar(1250, MotivoDinero.Premio);
            yield return new WaitForSecondsRealtime(1.2f);
            ColaAvisos.Publicar("SALIR A LA CALLE CUMPLIDO", PrioridadAviso.Alta, TipoAviso.Bueno, 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            Camera camara = jugador.Camara;
            CapturaUI.Capturar("docs/capturas/T-523-hud.png", camara);
            CapturaUI.Capturar2K("docs/capturas/T-523-hud-1440.png", camara);
        }

        /// <summary>Con CAPTURAR_T524=1 guarda docs/capturas/T-524-hud.png (1080p) y T-524-hud-1440.png con el HUD en juego en Suburbios.</summary>
        [UnityTest]
        public IEnumerator Captura_HudT524()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T524") != "1")
            {
                Assert.Ignore("Solo con CAPTURAR_T524=1");
            }
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<IPandillaJugador>.Actual != null;
            }, "el director arrancó", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
            jugador.Colocar(new Pose(new Vector3(-80f, 0.5f, 0f), Quaternion.LookRotation(Vector3.right)));
            Registro<ICartera>.Actual.Abonar(1250, MotivoDinero.Premio);
            yield return new WaitForSecondsRealtime(1.2f);
            ColaAvisos.Publicar("SALIR A LA CALLE CUMPLIDO", PrioridadAviso.Alta, TipoAviso.Bueno, 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (Text t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if (t.enabled && t.gameObject.activeInHierarchy && t.text.Length > 0 && t.canvas != null && t.canvas.enabled)
                {
                    Debug.Log("T524-TEXTO " + t.transform.parent.name + "/" + t.name + " = " + t.text);
                }
            }
            Camera camara = jugador.Camara;
            CapturaUI.Capturar("docs/capturas/T-524-hud.png", camara);
            CapturaUI.Capturar2K("docs/capturas/T-524-hud-1440.png", camara);
        }
    }
}
