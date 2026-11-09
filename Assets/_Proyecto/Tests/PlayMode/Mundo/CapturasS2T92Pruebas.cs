using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// Capturas de las tareas T-920 a T-924 de la sesión S2 (11-nov). Solo escriben con T92_CAPTURAS=1 (el prefijo, "antes" o "despues", va en
    /// T92_FASE). Cámara con RenderTexture: en batchmode ScreenCapture no sirve.
    /// </summary>
    public class CapturasS2T92Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        public static bool Activas => System.Environment.GetEnvironmentVariable("T92_CAPTURAS") == "1";
        private static string Fase => System.Environment.GetEnvironmentVariable("T92_FASE") ?? "despues";

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            if (!Activas)
            {
                yield break;
            }
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Niebla_T920_VistaALoLargoDeLaAvenida()
        {
            if (!Activas)
            {
                yield break;
            }
            Core.IRelojMundo reloj = Core.Registro<Core.IRelojMundo>.Actual;
            reloj.Pausar(true);
            foreach (float hora in new[] { 13f, 23f })
            {
                reloj.AvanzarHasta(hora);
                for (int i = 0; i < 5; i++)
                {
                    yield return null;
                }
                Debug.Log("T920 hora=" + hora + " densidad=" + RenderSettings.fogDensity);
                string h = hora < 20f ? "dia" : "noche";
                // A lo largo de la Av. Neón (x=47), del Eje Central (z=0) y de la Av. Coyote (x=-47).
                Foto(new Vector3(47f, 1.7f, -60f), new Vector3(47f, 4f, 100f), "t920-" + Fase + "-" + h + "-av-neon");
                Foto(new Vector3(-100f, 1.7f, 0f), new Vector3(100f, 4f, 0f), "t920-" + Fase + "-" + h + "-eje");
                Foto(new Vector3(-47f, 1.7f, 70f), new Vector3(-47f, 4f, -100f), "t920-" + Fase + "-" + h + "-av-coyote");
            }
            yield return null;
        }

        public static void Foto(Vector3 desde, Vector3 hacia, string nombre)
        {
            if (!Activas)
            {
                return;
            }
            GameObject go = new GameObject("CamaraFoto");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            cam.fieldOfView = 60f;
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
