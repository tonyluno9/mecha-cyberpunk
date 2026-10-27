using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-312: mide una zona rectangular de una captura PNG (x, y desde arriba a la izquierda, como en un visor de imágenes).
    /// Da luminancia media, dispersión robusta (P90 - P10, no se deja engañar por las gotas de lluvia), dispersión del matiz y
    /// fracción de píxeles de color vivo (reflejo de neón). Se usa con el editor vivo:
    /// <c>unity-vivo.sh eval 'return MechaCyberpunk.Mundo.Editor.MedirCaptura.Zona("ruta.png", 100, 540, 1000, 650);'</c>
    /// </summary>
    public static class MedirCaptura
    {
        private const float LuminanciaRoja = 0.2126f;
        private const float LuminanciaVerde = 0.7152f;
        private const float LuminanciaAzul = 0.0722f;
        private const float PercentilBajo = 0.1f;
        private const float PercentilAlto = 0.9f;
        private const float SaturacionMinimaMatiz = 0.1f;
        private const float SaturacionColorVivo = 0.3f;
        private const float LuminanciaColorVivo = 0.15f;
        private const float PorcentajeTotal = 100f;

        public static string Zona(string ruta, int x0, int y0, int x1, int y1)
        {
            Texture2D textura = new Texture2D(2, 2);
            textura.LoadImage(File.ReadAllBytes(ruta));
            List<float> luminancias = new List<float>();
            List<float> matices = new List<float>();
            int vivos = 0;
            double suma = 0.0;
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    Color pixel = textura.GetPixel(x, textura.height - 1 - y);
                    float luminancia = LuminanciaRoja * pixel.r + LuminanciaVerde * pixel.g + LuminanciaAzul * pixel.b;
                    Color.RGBToHSV(pixel, out float matiz, out float saturacion, out _);
                    luminancias.Add(luminancia);
                    suma += luminancia;
                    if (saturacion > SaturacionMinimaMatiz)
                    {
                        matices.Add(matiz);
                    }
                    if (saturacion > SaturacionColorVivo && luminancia > LuminanciaColorVivo)
                    {
                        vivos++;
                    }
                }
            }
            UnityEngine.Object.DestroyImmediate(textura);
            luminancias.Sort();
            float p10 = luminancias[(int)(PercentilBajo * (luminancias.Count - 1))];
            float p90 = luminancias[(int)(PercentilAlto * (luminancias.Count - 1))];
            StringBuilder texto = new StringBuilder();
            texto.Append("media=").Append((suma / luminancias.Count).ToString("F4"));
            texto.Append(" dispersionP90P10=").Append((p90 - p10).ToString("F4"));
            texto.Append(" dispersionMatiz=").Append(DispersionCircular(matices).ToString("F4"));
            texto.Append(" colorVivo=").Append((PorcentajeTotal * vivos / luminancias.Count).ToString("F2")).Append('%');
            return texto.ToString();
        }

        // Desviación circular del matiz (0 a 1 de vuelta): sqrt(-2 ln R) / 2π, con R la longitud del vector medio.
        private static float DispersionCircular(List<float> matices)
        {
            if (matices.Count == 0)
            {
                return 0f;
            }
            double seno = 0.0, coseno = 0.0;
            foreach (float matiz in matices)
            {
                double angulo = matiz * 2.0 * Math.PI;
                seno += Math.Sin(angulo);
                coseno += Math.Cos(angulo);
            }
            double r = Math.Sqrt(seno * seno + coseno * coseno) / matices.Count;
            return (float)(Math.Sqrt(-2.0 * Math.Log(Math.Max(r, 1e-6))) / (2.0 * Math.PI));
        }
    }
}
