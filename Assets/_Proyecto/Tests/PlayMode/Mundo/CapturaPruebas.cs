using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Guarda en un PNG lo que ve una cámara (evidencia visual de las tareas de Mundo). La ruta es relativa a la raíz del repo.</summary>
    public static class CapturaPruebas
    {
        private const int Ancho = 1280;
        private const int Alto = 720;
        private const int BitsProfundidad = 24;

        public static void Guardar(Camera camara, string ruta)
        {
            RenderTexture rt = new RenderTexture(Ancho, Alto, BitsProfundidad);
            RenderTexture previa = RenderTexture.active;
            RenderTexture destinoPrevio = camara.targetTexture;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, Ancho, Alto), 0, 0);
            imagen.Apply();
            RenderTexture.active = previa;
            camara.targetTexture = destinoPrevio;

            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.DestroyImmediate(imagen);
            Object.DestroyImmediate(rt);
        }
    }
}
