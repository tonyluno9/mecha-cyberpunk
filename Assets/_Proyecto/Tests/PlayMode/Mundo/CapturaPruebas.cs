using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Guarda en un PNG lo que ve una cámara (evidencia visual de las tareas de Mundo) y mide su luminancia. La ruta es relativa a la raíz del repo.</summary>
    public static class CapturaPruebas
    {
        private const int Ancho = 1280;
        private const int Alto = 720;
        private const int BitsProfundidad = 24;
        private const float LuminanciaRoja = 0.2126f;
        private const float LuminanciaVerde = 0.7152f;
        private const float LuminanciaAzul = 0.0722f;
        private const float UmbralSaturado = 0.98f;
        private const float UmbralNegro = 0.02f;

        /// <summary>Luminancia media (0 a 1) y fracción (0 a 1) de píxeles saturados y de píxeles casi negros.</summary>
        public readonly struct Medicion
        {
            public readonly float Media;
            public readonly float Saturados;
            public readonly float Negros;

            public Medicion(float media, float saturados, float negros)
            {
                Media = media;
                Saturados = saturados;
                Negros = negros;
            }

            public override string ToString()
            {
                return "media " + Media.ToString("0.000") + ", saturados " + (Saturados * 100f).ToString("0.0") + " %, negros " + (Negros * 100f).ToString("0.0") + " %";
            }
        }

        public static void Guardar(Camera camara, string ruta)
        {
            GuardarYMedir(camara, ruta);
        }

        public static Medicion GuardarYMedir(Camera camara, string ruta)
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

            Color[] pixeles = imagen.GetPixels();
            double suma = 0.0;
            int saturados = 0;
            int negros = 0;
            foreach (Color p in pixeles)
            {
                float luminancia = LuminanciaRoja * p.r + LuminanciaVerde * p.g + LuminanciaAzul * p.b;
                suma += luminancia;
                if (luminancia >= UmbralSaturado)
                {
                    saturados++;
                }
                else if (luminancia <= UmbralNegro)
                {
                    negros++;
                }
            }
            Medicion medicion = new Medicion((float)(suma / pixeles.Length), saturados / (float)pixeles.Length, negros / (float)pixeles.Length);

            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.DestroyImmediate(imagen);
            Object.DestroyImmediate(rt);
            return medicion;
        }
    }
}
