using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// Herramienta de T-520: dibuja uno o varios personajes con una cámara a 1920x1080 y guarda el PNG. Sirve para revisar los modelos
    /// en su escenario real (se pasa la escena abierta) o sobre un piso de prueba.
    /// </summary>
    public static class CapturaPersonajes
    {
        public const int Ancho = 1920;
        public const int Alto = 1080;

        /// <summary>Dibuja lo que ve <paramref name="camara"/> en un PNG de 1920x1080.</summary>
        public static void Guardar(Camera camara, string ruta)
        {
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            RenderTexture anterior = camara.targetTexture;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            foto.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            camara.targetTexture = anterior;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        /// <summary>Cámara a <paramref name="distancia"/> m frente a <paramref name="objetivo"/> (mirando su +Z local de frente), a la altura del pecho.</summary>
        public static Camera ColocarCamara(Vector3 objetivo, Vector3 hacia, float distancia, float altura, float apertura)
        {
            GameObject g = GameObject.Find("CamaraCaptura");
            if (g == null)
            {
                g = new GameObject("CamaraCaptura");
                g.AddComponent<Camera>();
            }
            Camera c = g.GetComponent<Camera>();
            c.enabled = false;
            c.fieldOfView = apertura;
            c.nearClipPlane = 0.05f;
            c.farClipPlane = 400f;
            c.clearFlags = CameraClearFlags.Skybox;
            Vector3 posicion = objetivo + hacia.normalized * distancia + Vector3.up * altura;
            g.transform.position = posicion;
            g.transform.LookAt(objetivo + Vector3.up * altura * 0.9f);
            return c;
        }

        /// <summary>Escena de prueba: piso, luz y sol. Devuelve la escena abierta.</summary>
        public static Scene EscenaDePrueba()
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = new Vector3(5f, 1f, 5f);
            piso.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.25f, 0.27f, 0.3f) };
            return escena;
        }
    }
}
