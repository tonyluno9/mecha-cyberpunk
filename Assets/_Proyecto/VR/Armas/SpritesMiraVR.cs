using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR51: los sprites de la mira del arma, dibujados en código (texturas con borde suave): un anillo, un punto y un círculo lleno (la máscara
    /// del visor del francotirador). Se crean una vez y se reusan; si la textura se destruyó (fin del Play) se vuelven a crear.
    /// </summary>
    public static class SpritesMiraVR
    {
        private const int Lado = 128;

        private static Sprite anillo;
        private static Sprite punto;
        private static Sprite circulo;
        private static Sprite cruz;

        /// <summary>Un anillo fino (la retícula del holográfico y el borde del visor).</summary>
        public static Sprite Anillo => Asegurar(ref anillo, "MiraVR_Anillo", (r) => Banda(r, 0.86f, 0.97f));

        /// <summary>Un punto al centro.</summary>
        public static Sprite Punto => Asegurar(ref punto, "MiraVR_Punto", (r) => Banda(r, -1f, 0.22f));

        /// <summary>Un círculo lleno.</summary>
        public static Sprite Circulo => Asegurar(ref circulo, "MiraVR_Circulo", (r) => Banda(r, -1f, 0.99f));

        /// <summary>Una cruz fina con hueco al centro (la retícula del francotirador).</summary>
        public static Sprite Cruz
        {
            get
            {
                if (cruz != null)
                {
                    return cruz;
                }
                var t = NuevaTextura("MiraVR_Cruz");
                float mitad = (Lado - 1) * 0.5f;
                for (int y = 0; y < Lado; y++)
                {
                    for (int x = 0; x < Lado; x++)
                    {
                        float dx = Mathf.Abs(x - mitad) / mitad;
                        float dy = Mathf.Abs(y - mitad) / mitad;
                        bool linea = (dx < 0.012f && dy > 0.12f) || (dy < 0.012f && dx > 0.12f);
                        t.SetPixel(x, y, linea ? Color.white : Color.clear);
                    }
                }
                t.Apply(false, true);
                cruz = Sprite.Create(t, new Rect(0f, 0f, Lado, Lado), new Vector2(0.5f, 0.5f), Lado);
                cruz.name = t.name;
                return cruz;
            }
        }

        // Alfa 1 entre los radios (0..1 del medio lado), con un borde suave de un píxel.
        private static float Banda(float r, float dentro, float fuera)
        {
            float paso = 1.5f / Lado;
            float a = Mathf.Clamp01((fuera - r) / paso);
            if (dentro > 0f)
            {
                a = Mathf.Min(a, Mathf.Clamp01((r - dentro) / paso));
            }
            return a;
        }

        private static Sprite Asegurar(ref Sprite sprite, string nombre, System.Func<float, float> alfa)
        {
            if (sprite != null)
            {
                return sprite;
            }
            var t = NuevaTextura(nombre);
            float mitad = (Lado - 1) * 0.5f;
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    float r = new Vector2(x - mitad, y - mitad).magnitude / mitad;
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, alfa(r)));
                }
            }
            t.Apply(false, true);
            sprite = Sprite.Create(t, new Rect(0f, 0f, Lado, Lado), new Vector2(0.5f, 0.5f), Lado);
            sprite.name = nombre;
            return sprite;
        }

        private static Texture2D NuevaTextura(string nombre)
        {
            return new Texture2D(Lado, Lado, TextureFormat.RGBA32, false)
            {
                name = nombre,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            anillo = null;
            punto = null;
            circulo = null;
            cruz = null;
        }
    }
}
