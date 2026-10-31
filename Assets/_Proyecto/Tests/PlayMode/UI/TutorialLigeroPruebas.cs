using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Opciones;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-441 en Ciudad_Prologo con rutas temporales: al empezar sale el aviso de moverse; pulsar W lo quita y lo guarda en las opciones;
    /// después sale otro aviso; apagar TUTORIAL en las opciones lo esconde y no vuelve a salir; la fila TUTORIAL existe en las opciones.
    /// </summary>
    public class TutorialLigeroPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t441_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            ServicioOpciones.FijarRuta(null);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            try
            {
                Directory.Delete(carpeta, true);
            }
            catch (Exception)
            {
                // La carpeta temporal se queda; no importa.
            }
            InputSystem.DisableAllEnabledActions();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            base.TearDown();
            DescartarEstadoDeMapas(new InputDevice[0]);
        }

        // Cambiar `devices` en un mapa apagado descarta su estado de resolución: la escena los resuelve de nuevo al prenderlos.
        private static void DescartarEstadoDeMapas(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    asset.Disable();
                    asset.devices = dispositivos;
                    asset.devices = null;
                }
            }
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
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

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Tutorial_AvisaMoverseSeGuardaYSeApagaEnOpciones()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas d = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return d != null && d.Iniciado && Registro<IJugador>.Actual != null && Registro<IOpciones>.Actual is ServicioOpciones
                    && UnityEngine.Object.FindFirstObjectByType<TutorialLigero>() != null;
            }, "la ciudad arrancó con el tutorial", 30f);
            ServicioOpciones servicio = (ServicioOpciones)Registro<IOpciones>.Actual;
            TutorialLigero tutorial = UnityEngine.Object.FindFirstObjectByType<TutorialLigero>();
            PantallaOpciones opciones = UnityEngine.Object.FindFirstObjectByType<PantallaOpciones>(FindObjectsInactive.Include);
            Assert.IsNotNull(opciones.Tutorial, "Las opciones tienen la fila TUTORIAL");
            Assert.IsTrue(servicio.Tutorial, "El tutorial viene encendido");

            // a. Sale el aviso de moverse con su texto.
            yield return EsperarHasta(() => tutorial.Visible, "el aviso de moverse", 10f);
            Assert.AreEqual(AccionTutorial.Moverse, tutorial.Regla.Aviso);
            StringAssert.Contains("W A S D", tutorial.Texto.text);

            // b. W lo quita y lo guarda en las opciones.
            yield return Pulsar(teclado.wKey);
            Assert.IsFalse(tutorial.Visible, "Moverse quita el aviso");
            StringAssert.Contains("Moverse", servicio.TutorialVisto, "Lo visto se guarda en las opciones");
            Assert.IsTrue(File.ReadAllText(ServicioOpciones.Ruta).Contains("Moverse"), "y en opciones.json");

            // c. Más tarde sale otro aviso distinto.
            yield return EsperarHasta(() => tutorial.Visible, "el siguiente aviso", 10f);
            Assert.AreNotEqual(AccionTutorial.Moverse, tutorial.Regla.Aviso, "No repite moverse");
            Debug.Log("[Prueba] T-441 siguiente aviso: " + tutorial.Regla.Aviso + " · " + tutorial.Texto.text);

            // d. Apagar TUTORIAL esconde el aviso y no vuelve a salir.
            int vistasAntes = tutorial.Regla.CantidadVistas;
            servicio.FijarTutorial(false);
            yield return null;
            yield return null;
            Assert.IsFalse(tutorial.Visible, "Apagarlo esconde el aviso");
            yield return new WaitForSecondsRealtime(3f);
            Assert.IsFalse(tutorial.Visible, "Apagado no vuelve a salir");
            Assert.AreEqual(vistasAntes, tutorial.Regla.CantidadVistas, "Apagado no da nada por visto");
        }
    }
}
