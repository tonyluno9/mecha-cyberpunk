using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>Guarda en un PNG lo que ve una cámara (las pruebas corren en batchmode, sin WaitForEndOfFrame).</summary>
    public static class CapturaPruebasVehiculos
    {
        public static void Tomar(Camera camara, string ruta, int ancho = 1280, int alto = 720)
        {
            RenderTexture rt = new RenderTexture(ancho, alto, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            textura.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, textura.EncodeToPNG());
            Object.DestroyImmediate(textura);
            Object.DestroyImmediate(rt);
        }
    }
}
