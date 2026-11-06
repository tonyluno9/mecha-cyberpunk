using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>Guarda en docs/capturas lo que ve una cámara (sin esperar al final del cuadro: en batchmode eso se cuelga).</summary>
    public static class CapturaPrueba
    {
        public static void Guardar(Camera camara, string nombre, int ancho = 1280, int alto = 720)
        {
            RenderTexture rt = new RenderTexture(ancho, alto, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            camara.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "docs", "capturas", nombre), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
        }
    }
}
