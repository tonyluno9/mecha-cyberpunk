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

        [UnityTest]
        public IEnumerator Postes_T921_CruceSinLetreroDeCalle()
        {
            if (!Activas)
            {
                yield break;
            }
            // El cruce del Eje Central con la Av. Neón: tenía letreros colgados del poste de la esquina.
            Foto(new Vector3(47f, 1.7f, -22f), new Vector3(47f, 4f, 0f), "t921-" + Fase + "-cruce-eje-neon");
            Foto(new Vector3(-47f, 1.7f, -22f), new Vector3(-47f, 4f, 0f), "t921-" + Fase + "-cruce-eje-coyote");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Interiores_T923()
        {
            if (!Activas)
            {
                yield break;
            }
            Vector3 ojo(float x, float z) => new Vector3(x, 1.65f, z);
            // I+D+I del Doc (x -104..-96, z -16..-10): las cuatro paredes desde dentro.
            Foto(ojo(-100f, -10.6f), ojo(-100f, -16f), "t923-" + Fase + "-idi-fondo");
            Foto(ojo(-96.4f, -13f), ojo(-104f, -13f), "t923-" + Fase + "-idi-izq");
            Foto(ojo(-103.6f, -13f), ojo(-96f, -13f), "t923-" + Fase + "-idi-der");
            Foto(ojo(-100f, -15.6f), ojo(-100f, -10f), "t923-" + Fase + "-idi-frente");
            Foto(ojo(-98f, -10.8f), ojo(-103f, -15f), "t923-" + Fase + "-idi-esq1");
            Foto(ojo(-102f, -10.8f), ojo(-97f, -15f), "t923-" + Fase + "-idi-esq2");
            // Cuarto (x -66.5..-61.5, z -75.5..-71.5, puerta al sur en x -62).
            Foto(ojo(-62.2f, -73.2f), ojo(-66.5f, -73f), "t923-" + Fase + "-cuarto-posters");
            Foto(ojo(-65.5f, -72f), ojo(-62f, -75.5f), "t923-" + Fase + "-cuarto-puerta-dentro");
            Foto(ojo(-62f, -77.2f), ojo(-62f, -75.5f), "t923-" + Fase + "-cuarto-puerta-pasillo");
            Foto(ojo(-65f, -74f), ojo(-64f, -71.5f), "t923-" + Fase + "-cuarto-fondo");
            // Entrada de la casa desde dentro (pasillo, x -63.6..-57).
            Foto(ojo(-60f, -77f), ojo(-56.7f, -77f), "t923-" + Fase + "-casa-entrada");
            Foto(ojo(-61f, -77f), ojo(-56.7f, -75.8f), "t923-" + Fase + "-casa-entrada-n");
            Foto(ojo(-61f, -77f), ojo(-56.7f, -78.2f), "t923-" + Fase + "-casa-entrada-s");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Puertas_T924_ConNeon()
        {
            if (!Activas)
            {
                yield break;
            }
            Core.IRelojMundo reloj = Core.Registro<Core.IRelojMundo>.Actual;
            reloj.Pausar(true);
            reloj.AvanzarHasta(23f);
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
            string f = "t924-" + Fase + "-";
            // Entradas con marco de neón (VestirEntradas): casino, clandestino, mercado negro y la casa, de frente y en diagonal.
            Foto(new Vector3(49f, 1.7f, 47f), new Vector3(57f, 2.2f, 47f), f + "casino-frente");
            Foto(new Vector3(51f, 1.7f, 42f), new Vector3(57f, 2.2f, 47f), f + "casino-diagonal");
            Foto(new Vector3(0f, 1.7f, 3f), new Vector3(0f, 2.2f, 10f), f + "clandestino-frente");
            Foto(new Vector3(-5f, 1.7f, 4f), new Vector3(0f, 2.2f, 10f), f + "clandestino-diagonal");
            Foto(new Vector3(-32f, 1.7f, 26f), new Vector3(-25f, 2.2f, 26f), f + "mercado-frente");
            Foto(new Vector3(-31f, 1.7f, 21f), new Vector3(-25f, 2.2f, 26f), f + "mercado-diagonal");
            Foto(new Vector3(-50f, 1.7f, -77f), new Vector3(-57f, 2.2f, -77f), f + "casa-frente");
            Foto(new Vector3(-100f, 1.7f, -3f), new Vector3(-100f, 2.2f, -10f), f + "doc-frente");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Charcos_T924()
        {
            if (!Activas)
            {
                yield break;
            }
            Core.IRelojMundo reloj = Core.Registro<Core.IRelojMundo>.Actual;
            reloj.Pausar(true);
            reloj.AvanzarHasta(23f);
            for (int i = 0; i < 8; i++)
            {
                yield return null;
            }
            // Los charcos más cercanos a la Av. Neón y al Eje (los que se ven al caminar), desde 4-5 m y a la altura de los ojos.
            Transform[] charcos = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name == "Charco" && t.gameObject.activeInHierarchy && Mathf.Abs(t.position.x) < 60f && t.position.z > -60f)
                .OrderBy(t => (t.position - new Vector3(0f, 0f, 0f)).sqrMagnitude).Take(4).ToArray();
            Debug.Log("T924 charcos elegidos: " + charcos.Length);
            string f = "t924-" + Fase + "-charco-";
            MechaCyberpunk.Mundo.Ambiente.ReflejoCharcos reflejo = Object.FindFirstObjectByType<MechaCyberpunk.Mundo.Ambiente.ReflejoCharcos>();
            Camera principal = Camera.main;
            yield return null;
            for (int i = 0; i < charcos.Length; i++)
            {
                Vector3 c = charcos[i].position;
                Vector3 desde = c + new Vector3(0f, 1.6f, -3.5f);
                principal.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(c + Vector3.up * 0.2f - desde));
                reflejo.Actualizar(principal);
                reflejo.Camara.Render();
                FotoConCamara(principal, f + i);
            }
            yield return null;
        }

        private static void FotoConCamara(Camera cam, string nombre)
        {
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
