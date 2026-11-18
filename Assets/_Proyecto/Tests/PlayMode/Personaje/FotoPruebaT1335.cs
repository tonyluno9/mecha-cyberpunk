using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1335/T-1337: guarda una foto de la escena de prueba (con la cámara dada o con una nueva puesta en desde → hacia), solo si la variable de entorno lo pide.</summary>
    public static class FotoPruebaT1335
    {
        public static bool Pedida(string variable) => System.Environment.GetEnvironmentVariable(variable) == "1";

        public static void Desde(Vector3 desde, Vector3 hacia, string ruta)
        {
            GameObject cam = new GameObject("CamaraFotoPrueba");
            Camera c = cam.AddComponent<Camera>();
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            c.fieldOfView = 50f;
            Con(c, ruta);
            Object.DestroyImmediate(cam);
        }

        public static void Con(Camera c, string ruta)
        {
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            RenderTexture antes = c.targetTexture;
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            c.targetTexture = antes;
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(imagen);
        }
    }
}
