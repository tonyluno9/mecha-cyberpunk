using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR51/T91: los sprites de la retícula del arma en VR, dibujados en código (texturas con borde suave): un anillo, un punto, un círculo lleno,
    /// la cruz fina del francotirador, la cruz simple de pistola y fusil (cuatro trazos con hueco al centro) y el agujero del visor (negro con un
    /// círculo transparente). Se crean una vez y se reusan; si la textura se destruyó (fin del Play) se vuelven a crear.
    /// </summary>
    public static class SpritesMiraVR
    {
        private const int Lado = 128;

        private static Sprite anillo;
        private static Sprite punto;
        private static Sprite circulo;
        private static Sprite cruz;
        private static Sprite cruzSimple;
        private static Sprite agujero;

        /// <summary>Un anillo fino (la retícula de fusil y escopeta y el borde del visor).</summary>
        public static Sprite Anillo => Asegurar(ref anillo, "MiraVR_Anillo", (r) => Banda(r, 0.86f, 0.97f));

        /// <summary>Un punto al centro.</summary>
        public static Sprite Punto => Asegurar(ref punto, "MiraVR_Punto", (r) => Banda(r, -1f, 0.22f));

        /// <summary>Un círculo lleno.</summary>
        public static Sprite Circulo => Asegurar(ref circulo, "MiraVR_Circulo", (r) => Banda(r, -1f, 0.99f));

        /// <summary>T91: negro (alfa 1) con un círculo transparente al centro: el borde negro del visor del francotirador.</summary>
        public static Sprite Agujero => Asegurar(ref agujero, "MiraVR_Agujero", (r) => Banda(r, 0.97f, 10f));

        /// <summary>Una cruz fina con hueco al centro (la retícula del francotirador).</summary>
        public static Sprite Cruz => AsegurarCruz(ref cruz, "MiraVR_Cruz", 0.012f, 0.12f, 1.01f);

        /// <summary>T91: cuatro trazos gruesos con hueco al centro (la retícula de pistola, revólver y fusil).</summary>
        public static Sprite CruzSimple => AsegurarCruz(ref cruzSimple, "MiraVR_CruzSimple", 0.07f, 0.3f, 0.95f);

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

        // Una cruz de trazos de medio grosor (0..1 del medio lado), del hueco al fin.
        private static Sprite AsegurarCruz(ref Sprite sprite, string nombre, float medioGrosor, float hueco, float fin)
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
                    float dx = Mathf.Abs(x - mitad) / mitad;
                    float dy = Mathf.Abs(y - mitad) / mitad;
                    bool linea = (dx < medioGrosor && dy > hueco && dy < fin) || (dy < medioGrosor && dx > hueco && dx < fin);
                    t.SetPixel(x, y, linea ? Color.white : Color.clear);
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
            cruzSimple = null;
            agujero = null;
        }
    }
}
