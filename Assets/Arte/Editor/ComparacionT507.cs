using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>T-507: junta tres imágenes (referencia, antes, después) en una tira de 2880x540 para docs/capturas/pulido-*-comparacion.png.</summary>
    public static class ComparacionT507
    {
        private const int Ancho = 960;
        private const int Alto = 540;

        public static string Componer(string referencia, string antes, string despues, string salida)
        {
            Texture2D tira = new Texture2D(Ancho * 3, Alto, TextureFormat.RGB24, false);
            string[] rutas = { referencia, antes, despues };
            for (int i = 0; i < 3; i++)
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
            return "ok " + salida;
        }
    }
}
