using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-716: en la ciudad, en cada paso de la historia el objetivo existe, su destino se resuelve desde la escena (no (0,0,0)) y el HUD H10 lo
    /// muestra con la distancia. F6 (tecla real) salta de paso; el guardado va a una carpeta temporal.
    /// </summary>
    public class ObjetivoSiempreT716Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private string carpeta;
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            carpeta = Path.Combine(Application.temporaryCachePath, "t716_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            if (carpeta != null && Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(raiz);
            Registros.LimpiarTodos();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CadaPasoDeLaHistoria_TieneObjetivoConDestinoYElHudLoMuestra()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            float fin = Time.realtimeSinceStartup + 40f;
            while (director == null || !director.Iniciado || Registro<IJugador>.Actual == null || Registro<IObjetivos>.Actual == null)
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                Assert.Less(Time.realtimeSinceStartup, fin, "El director no arrancó");
                yield return null;
            }
            HudH10 hud = Object.FindFirstObjectByType<HudH10>();
            Assert.IsNotNull(hud, "No hay HudH10 en la escena");
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;

            int vistos = 0;
            foreach (PasoHistoria paso in Enum.GetValues(typeof(PasoHistoria)))
            {
                // El guardado (Cargar) pone el paso, como al continuar una partida: cada paso se comprueba desde su propio inicio.
                director.Cargar("{\"paso\":\"" + paso + "\"}");
                Assert.AreEqual(paso, director.Paso);
                yield return null;
                yield return null;
                yield return new WaitForSecondsRealtime(0.2f);
                if (!DirectorPandillas.LlevaObjetivo(paso))
                {
                    continue;
                }
                Assert.IsTrue(objetivos.HayObjetivo, paso + ": sin objetivo");
                Assert.IsTrue(objetivos.PosicionResuelta, paso + ": el objetivo no se pudo ubicar");
                Assert.Greater(objetivos.Posicion.sqrMagnitude, 1f, paso + ": el destino quedó en (0,0,0)");
                string prefijo = paso == PasoHistoria.Tianguis ? DirectorPandillas.PrefijoTianguis : DirectorPandillas.TextoDelPaso(paso);
                Assert.IsTrue(objetivos.Actual.Texto.StartsWith(prefijo, StringComparison.Ordinal), paso + ": el objetivo dice '" + objetivos.Actual.Texto + "'");
                Text_Hud(hud, paso, prefijo);
                vistos++;
                if (Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1" && (paso == PasoHistoria.Despierta || paso == PasoHistoria.Doc || paso == PasoHistoria.Tianguis))
                {
                    MechaCyberpunk.Tests.UI.CapturaUI.Capturar("docs/capturas/s1c-T-716-" + paso + ".png", Registro<IJugador>.Actual.Camara);
                }
            }
            Assert.GreaterOrEqual(vistos, 9, "Se esperaban al menos los 9 pasos con objetivo");
        }

        private static void Text_Hud(HudH10 hud, PasoHistoria paso, string prefijo)
        {
            // El HUD H10 muestra arriba a la derecha la frase y la distancia ("FRASE · 42 m").
            Assert.IsNotNull(hud.TextoObjetivo, paso + ": el HUD no tiene el texto del objetivo");
            string visible = hud.TextoObjetivo.text;
            Assert.IsTrue(visible.StartsWith(prefijo, StringComparison.Ordinal), paso + ": el HUD dice '" + visible + "'");
            Assert.IsTrue(visible.Contains(" · "), paso + ": el HUD no muestra la distancia: '" + visible + "'");
        }
    }
}
