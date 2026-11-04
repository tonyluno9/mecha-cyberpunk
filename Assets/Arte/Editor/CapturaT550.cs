using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-550: captura de un cuarto con una cÃ¡mara a la altura de los ojos desde el punto de apariciÃ³n (donde queda el jugador al entrar), con posproceso,
    /// y la junta con su referencia (izquierda referencia, derecha juego). Batchmode:
    /// -executeMethod MechaCyberpunk.Arte.Editor.CapturaT550.Habitacion
    /// </summary>
    public static class CapturaT550
    {
        private const int Ancho = 960;
        private const int Alto = 540;

        public static void Habitacion()
        {
            EditorSceneManager.OpenScene(ColocarT550.RutaEscena, OpenSceneMode.Single);
            Transform cuarto = GameObject.Find(ColocarT550.NombreRaiz).transform.Find("Cuarto_Habitacion");
            Vector3 ojos = cuarto.TransformPoint(new Vector3(0.95f, 1.6f, -1.9f)); // junto a la puerta, a la altura de los ojos del jugador
            Vector3 mira = cuarto.TransformPoint(new Vector3(-0.3f, 1.0f, 2.0f));
            Capturar(ojos, mira, 62f, "docs/capturas/cuarto-habitacion-juego.png");
            Componer("docs/referencias-interiores/habitacion.png", "docs/capturas/cuarto-habitacion-juego.png", "docs/capturas/cuarto-habitacion-comparacion.png");
        }

        /// <summary>Escribe en Logs/t550-lista.txt la posición local y el tamaño de cada hijo directo del cuarto de la habitación.</summary>
        public static void Listar()
        {
            EditorSceneManager.OpenScene(ColocarT550.RutaEscena, OpenSceneMode.Single);
            Transform cuarto = GameObject.Find(ColocarT550.NombreRaiz).transform.Find("Cuarto_Habitacion");
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (Transform t in cuarto)
            {
                Renderer[] rs = t.GetComponentsInChildren<Renderer>();
                Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.zero);
                foreach (Renderer r in rs) { b.Encapsulate(r.bounds); }
                sb.AppendLine(t.name + " centro " + cuarto.InverseTransformPoint(b.center).ToString("F2") + " tam " + b.size.ToString("F2") + " activo " + t.gameObject.activeInHierarchy);
            }
            File.WriteAllText("Logs/t550-lista.txt", sb.ToString());
        }

        public static void Capturar(Vector3 desde, Vector3 hacia, float campo, string ruta)
        {
            GameObject go = new GameObject("CamaraT550");
            Camera cam = go.AddComponent<Camera>();
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.fieldOfView = campo;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            foto.Apply();
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }

        public static void Componer(string referencia, string juego, string salida)
        {
            Texture2D tira = new Texture2D(Ancho * 2, Alto, TextureFormat.RGB24, false);
            string[] rutas = { referencia, juego };
            for (int i = 0; i < 2; i++)
            {
                Texture2D origen = new Texture2D(2, 2, TextureFormat.RGB24, false);
                origen.LoadImage(File.ReadAllBytes(rutas[i]));
                RenderTexture rt = RenderTexture.GetTemporary(Ancho, Alto, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(origen, rt);
                RenderTexture.active = rt;
                tira.ReadPixels(new Rect(0, 0, Ancho, Alto), i * Ancho, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(origen);
            }
            tira.Apply();
            File.WriteAllBytes(salida, tira.EncodeToPNG());
            Object.DestroyImmediate(tira);
        }
    }
}
