using System.Collections;
using System.IO;
using MechaCyberpunk.Herramientas.ModoVideo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-472: F10 oculta y restaura un Canvas, F11 entra y sale de la cámara libre, F12 guarda una captura.</summary>
    public class ModoVideoPruebas : InputTestFixture
    {
        private Keyboard teclado;
        private GameObject camaraJugador;
        private GameObject objetoCanvas;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            camaraJugador = new GameObject("CamaraJugador", typeof(Camera));
            camaraJugador.tag = "MainCamera";
            objetoCanvas = new GameObject("HudPrueba", typeof(Canvas));
            objetoCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            ModoVideo.Asegurar();
        }

        public override void TearDown()
        {
            if (ModoVideo.Instancia != null)
            {
                Object.DestroyImmediate(ModoVideo.Instancia.gameObject);
            }
            Object.DestroyImmediate(camaraJugador);
            Object.DestroyImmediate(objetoCanvas);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator F10_OcultaYRestauraElCanvas()
        {
            Canvas canvas = objetoCanvas.GetComponent<Canvas>();
            yield return null;
            PressAndRelease(teclado.f10Key);
            yield return null;
            Assert.IsFalse(canvas.enabled);
            Assert.IsTrue(ModoVideo.Instancia.HudOculto);
            PressAndRelease(teclado.f10Key);
            yield return null;
            Assert.IsTrue(canvas.enabled);
            Assert.IsFalse(ModoVideo.Instancia.HudOculto);
        }

        [UnityTest]
        public IEnumerator F11_ActivaCamaraLibreYAlSalirVuelveLaOriginal()
        {
            Camera original = camaraJugador.GetComponent<Camera>();
            camaraJugador.transform.position = new Vector3(3f, 2f, 1f);
            yield return null;
            PressAndRelease(teclado.f11Key);
            yield return null;
            Assert.IsTrue(ModoVideo.Instancia.CamaraLibreActiva);
            Assert.IsFalse(original.enabled);
            Camera libre = ModoVideo.Instancia.CamaraLibre;
            Assert.IsTrue(libre.enabled);
            Vector3 inicio = libre.transform.position;
            Assert.AreEqual(3f, inicio.x, 0.01f);
            Press(teclado.eKey);
            yield return new WaitForSeconds(0.3f);
            Release(teclado.eKey);
            Assert.Greater(libre.transform.position.y, inicio.y + 0.1f, "E no subió la cámara libre.");
            PressAndRelease(teclado.f11Key);
            yield return null;
            Assert.IsFalse(ModoVideo.Instancia.CamaraLibreActiva);
            Assert.IsTrue(original.enabled);
            Assert.IsTrue(ModoVideo.Instancia.CamaraLibre == null);
        }

        [UnityTest]
        public IEnumerator F11_ApagaYRestauraLasAccionesActivas()
        {
            InputAction accion = new InputAction("Prueba", InputActionType.Button, "<Keyboard>/space");
            accion.Enable();
            yield return null;
            PressAndRelease(teclado.f11Key);
            yield return null;
            Assert.IsFalse(accion.enabled);
            PressAndRelease(teclado.f11Key);
            yield return null;
            Assert.IsTrue(accion.enabled);
            accion.Dispose();
        }

        [UnityTest]
        public IEnumerator F12_GuardaCapturaEnCapturas()
        {
            yield return null;
            PressAndRelease(teclado.f12Key);
            yield return null;
            string ruta = ModoVideo.Instancia.UltimaCaptura;
            Assert.IsNotNull(ruta);
            Assert.IsTrue(ruta.Contains("Capturas"));
            Assert.IsTrue(Path.GetFileName(ruta).StartsWith("captura_") && ruta.EndsWith(".png"));
            Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(ruta)));
            if (Application.isBatchMode)
            {
                // En batchmode sin ventana ScreenCapture no escribe el archivo: aquí solo se comprueba la ruta.
                yield break;
            }
            float fin = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(ruta) && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.IsTrue(File.Exists(ruta), "No se guardó la captura " + ruta);
            File.Delete(ruta);
        }

        // Captura 1920x1080 de la vista con el HUD oculto (solo con CAPTURAR_T472=1).
        [UnityTest]
        public IEnumerator Captura_HudOcultoSeVeLimpio()
        {
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_T472") != "1")
            {
                Assert.Ignore("Solo con CAPTURAR_T472=1.");
            }
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.localScale = new Vector3(5f, 1f, 5f);
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.transform.position = new Vector3(0f, 0.5f, 4f);
            GameObject luz = new GameObject("Luz", typeof(Light));
            luz.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            Camera camara = camaraJugador.GetComponent<Camera>();
            camara.transform.position = new Vector3(0f, 1.6f, 0f);
            GameObject texto = new GameObject("TextoHud", typeof(UnityEngine.UI.Text));
            texto.transform.SetParent(objetoCanvas.transform, false);
            texto.GetComponent<UnityEngine.UI.Text>().text = "HUD DE PRUEBA";
            texto.GetComponent<UnityEngine.UI.Text>().font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            yield return null;
            PressAndRelease(teclado.f10Key);
            yield return null;
            Assert.IsFalse(objetoCanvas.GetComponent<Canvas>().enabled);
            RenderTexture rt = new RenderTexture(1920, 1080, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/T-472-1.png", foto.EncodeToPNG());
            Object.DestroyImmediate(suelo);
            Object.DestroyImmediate(cubo);
            Object.DestroyImmediate(luz);
        }
    }
}
