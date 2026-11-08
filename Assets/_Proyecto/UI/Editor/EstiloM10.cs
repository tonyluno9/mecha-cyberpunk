using MechaCyberpunk.UI.Mapa;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// De un mapa de alturas (m, fila 0 al sur) al plano con el estilo de M10 (T-747): calles oscuras, banquetas, edificios del color de su barrio y más claros
    /// cuanto más altos, contorno oscuro donde hay un salto de altura, borde claro en los techos y sombra hacia el sureste.
    /// </summary>
    public static class EstiloM10
    {
        private const float AlturaCalle = 0.3f;
        private const float AlturaBanqueta = 0.6f;
        private const float AlturaEdificio = 3f;
        private const float AlturaTope = 45f;
        private const float SaltoContorno = 1.5f;
        // Sombra hacia el sureste: la luz viene del noroeste con unos 50 grados de altura.
        private const float TangenteSol = 1.19f;
        private const int PasosSombra = 16;
        private const int SaltoSombra = 3;

        private static readonly Color Calle = new Color(0.06f, 0.075f, 0.11f);
        private static readonly Color Banqueta = new Color(0.15f, 0.17f, 0.22f);
        private static readonly Color Contorno = new Color(0.015f, 0.02f, 0.04f);

        public static Texture2D Pintar(float[] alturas, int ancho, int alto)
        {
            Color32[] salida = new Color32[ancho * alto];
            for (int j = 0; j < alto; j++)
            {
                for (int i = 0; i < ancho; i++)
                {
                    int k = j * ancho + i;
                    float h = alturas[k];
                    Vector2 mundo = MapaImagen.PixelAMundo(new Vector2(i + 0.5f, j + 0.5f));
                    Color tinte = BarriosMapa.ColorEn(mundo);
                    Color color;
                    if (h < AlturaCalle)
                    {
                        color = Color.Lerp(Calle, tinte * 0.35f, 0.14f);
                    }
                    else if (h < AlturaBanqueta)
                    {
                        color = Color.Lerp(Banqueta, tinte * 0.5f, 0.18f);
                    }
                    else if (h < AlturaEdificio)
                    {
                        color = Color.Lerp(Banqueta, tinte * 0.7f, 0.55f);
                    }
                    else
                    {
                        float t = Mathf.Clamp01((h - AlturaEdificio) / AlturaTope);
                        color = Color.Lerp(tinte * 0.42f, tinte * 0.98f, Mathf.Sqrt(t));
                    }
                    if (h < AlturaEdificio)
                    {
                        color *= Sombra(alturas, ancho, alto, i, j, h);
                    }
                    // Un poco de grano para que no se vea plano.
                    float grano = Hash(i, j) * 0.05f - 0.025f;
                    color += new Color(grano, grano, grano, 0f);
                    color = Contornos(alturas, ancho, alto, i, j, h, color, tinte);
                    color.a = 1f;
                    salida[k] = color;
                }
            }
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false, false);
            textura.SetPixels32(salida);
            textura.Apply();
            return textura;
        }

        // Oscurece el suelo que queda a la sombra de algo más alto al noroeste.
        private static float Sombra(float[] alturas, int ancho, int alto, int i, int j, float h)
        {
            for (int p = 1; p <= PasosSombra; p++)
            {
                int d = p * SaltoSombra;
                int x = i - d;
                int y = j + d;
                if (x < 0 || y >= alto)
                {
                    break;
                }
                float metros = d * 1.4142f / MapaImagen.PixelesPorMetro;
                if (alturas[y * ancho + x] - h > metros * TangenteSol && alturas[y * ancho + x] > AlturaEdificio)
                {
                    return 0.5f;
                }
            }
            return 1f;
        }

        // Contorno oscuro en el lado bajo de un salto de altura y borde claro en el alto.
        private static Color Contornos(float[] alturas, int ancho, int alto, int i, int j, float h, Color color, Color tinte)
        {
            bool bajo = false;
            bool alta = false;
            for (int d = 1; d <= 2; d++)
            {
                for (int v = 0; v < 4; v++)
                {
                    int x = i + (v == 0 ? d : v == 1 ? -d : 0);
                    int y = j + (v == 2 ? d : v == 3 ? -d : 0);
                    if (x < 0 || y < 0 || x >= ancho || y >= alto)
                    {
                        continue;
                    }
                    float otro = alturas[y * ancho + x];
                    if (otro - h > SaltoContorno)
                    {
                        bajo = true;
                    }
                    else if (d == 1 && h - otro > SaltoContorno)
                    {
                        alta = true;
                    }
                }
            }
            if (bajo)
            {
                return Contorno;
            }
            if (alta)
            {
                return Color.Lerp(tinte, Color.white, 0.45f);
            }
            return color;
        }

        private static float Hash(int x, int y)
        {
            uint n = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            n = (n << 13) ^ n;
            return ((n * (n * n * 15731u + 789221u) + 1376312589u) & 0x7fffffffu) / (float)0x7fffffff;
        }
    }
}
