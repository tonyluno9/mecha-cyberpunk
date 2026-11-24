using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR51/T91: los sprites de la retícula del arma en VR, dibujados en código (texturas con borde suave): un anillo, un punto, un círculo lleno,
    /// la cruz fina del francotirador, la cruz simple de pistola y fusil (cuatro trazos con hueco al centro) y el agujero del visor (negro con un
    /// círculo transparente). Se crean una vez y se reusan; si la textura se destruyó (fin del Play) se vuelven a crear.
    /// T101: la mira propia del juego (<see cref="MiraArmaVR"/>): <see cref="Vineta"/> (negro con el círculo de borde difuminado),
    /// <see cref="CruzMarcas"/> (cruz negra delgada con marcas de distancia y postes gruesos en los extremos), <see cref="AnilloAbierto"/>
    /// (el anillo rojo con cuatro aberturas) y <see cref="AcentoCian"/> (arcos finos cerca del borde). Todas blancas: el color lo pone la Image.
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
        private static Sprite vineta;
        private static Sprite cruzMarcas;
        private static Sprite anilloAbierto;
        private static Sprite acentoCian;

        // T101: lados (px) de las texturas de la mira; la cruz es más grande porque sus trazos son finos y abarca todo el círculo.
        private const int LadoCruzMarcas = 512;
        private const int LadoVineta = 256;
        private const int LadoAnillo = 128;
        private const int LadoAcento = 256;

        // T101: medidas en fracción del medio lado de cada textura.
        private const float VinetaDentro = 0.80f;        // hasta aquí se ve el mundo entero
        private const float VinetaFuera = 0.99f;         // desde aquí todo negro
        private const float CruzHueco = 0.16f;           // la cruz no entra al anillo rojo
        private const float CruzMedioGrosor = 0.0035f;
        private const float PosteDesde = 0.72f;          // postes gruesos en los extremos
        private const float PosteMedioGrosor = 0.016f;
        private const float AnilloDentro = 0.80f;
        private const float AnilloFuera = 0.97f;
        private const float AnilloAberturaGrados = 16f;  // media abertura alrededor de 0°, 90°, 180° y 270°
        private const float AcentoDentro = 0.955f;
        private const float AcentoFuera = 0.97f;
        private const float AcentoMedioArcoGrados = 14f; // arcos alrededor de 45°, 135°, 225° y 315°

        // Marcas de distancia: (posición en el brazo, medio largo). Las del brazo de abajo, más largas, son la caída de la bala.
        private static readonly Vector2[] MarcasAbajo = { new Vector2(0.30f, 0.055f), new Vector2(0.42f, 0.042f), new Vector2(0.54f, 0.032f), new Vector2(0.64f, 0.024f) };
        private static readonly Vector2[] MarcasLados = { new Vector2(0.30f, 0.028f), new Vector2(0.45f, 0.022f), new Vector2(0.60f, 0.018f) };

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

        /// <summary>T101: negro con un círculo transparente de borde difuminado: lo que tapa toda la vista salvo la mira.</summary>
        public static Sprite Vineta => Generar(ref vineta, "MiraVR_Vineta", LadoVineta,
            (x, y) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(VinetaDentro, VinetaFuera, Radio(x, y))));

        /// <summary>T101: cruz negra delgada (con hueco al centro), marcas de distancia y postes gruesos en los extremos.</summary>
        public static Sprite CruzMarcas => Generar(ref cruzMarcas, "MiraVR_CruzMarcas", LadoCruzMarcas, AlfaCruzMarcas);

        /// <summary>T101: anillo con cuatro aberturas (arriba, abajo y a los lados): el rojo del centro.</summary>
        public static Sprite AnilloAbierto => Generar(ref anilloAbierto, "MiraVR_AnilloAbierto", LadoAnillo,
            (x, y) => GradosAlEje(x, y) < AnilloAberturaGrados ? 0f : Banda01(Radio(x, y), AnilloDentro, AnilloFuera, LadoAnillo));

        /// <summary>T101: cuatro arcos finos en diagonal cerca del borde: el acento cian.</summary>
        public static Sprite AcentoCian => Generar(ref acentoCian, "MiraVR_AcentoCian", LadoAcento,
            (x, y) => GradosAlEje(x, y) > 45f - AcentoMedioArcoGrados ? Banda01(Radio(x, y), AcentoDentro, AcentoFuera, LadoAcento) : 0f);

        private static float Radio(float x, float y) => Mathf.Sqrt(x * x + y * y);

        // Grados (0..45) entre la dirección (x, y) y el eje más cercano.
        private static float GradosAlEje(float x, float y)
        {
            float a = Mathf.Repeat(Mathf.Atan2(y, x) * Mathf.Rad2Deg, 90f);
            return Mathf.Min(a, 90f - a);
        }

        // Alfa 1 entre los radios, con borde suave de un píxel de una textura de ese lado.
        private static float Banda01(float r, float dentro, float fuera, int lado)
        {
            float paso = 2f / lado;
            return Mathf.Min(Mathf.Clamp01((fuera - r) / paso), Mathf.Clamp01((r - dentro) / paso));
        }

        // Un trazo con borde suave: la distancia a su línea contra su medio grosor.
        private static float Trazo(float distancia, float medioGrosor, float paso) => Mathf.Clamp01(0.5f + (medioGrosor - distancia) / paso);

        private static float AlfaCruzMarcas(float x, float y)
        {
            if (Radio(x, y) > 1f)
            {
                return 0f;
            }
            float paso = 2f / LadoCruzMarcas;
            float ax = Mathf.Abs(x);
            float ay = Mathf.Abs(y);
            float a = 0f;
            // Brazos finos y postes gruesos en los extremos.
            if (ax > CruzHueco)
            {
                a = Mathf.Max(a, Trazo(ay, ax > PosteDesde ? PosteMedioGrosor : CruzMedioGrosor, paso));
            }
            if (ay > CruzHueco)
            {
                a = Mathf.Max(a, Trazo(ax, ay > PosteDesde ? PosteMedioGrosor : CruzMedioGrosor, paso));
            }
            // Marcas de distancia: trazos cortos atravesados en cada brazo (más en el de abajo).
            foreach (Vector2 m in y < 0f ? MarcasAbajo : MarcasLados)
            {
                if (ax < m.y)
                {
                    a = Mathf.Max(a, Trazo(Mathf.Abs(ay - m.x), CruzMedioGrosor, paso));
                }
            }
            foreach (Vector2 m in MarcasLados)
            {
                if (ay < m.y)
                {
                    a = Mathf.Max(a, Trazo(Mathf.Abs(ax - m.x), CruzMedioGrosor, paso));
                }
            }
            return a;
        }

        // T101: textura blanca de ese lado con el alfa de la función (x, y en -1..1 desde el centro, +y arriba).
        private static Sprite Generar(ref Sprite sprite, string nombre, int lado, System.Func<float, float, float> alfa)
        {
            if (sprite != null)
            {
                return sprite;
            }
            var t = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                name = nombre,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixeles = new Color32[lado * lado];
            float mitad = (lado - 1) * 0.5f;
            for (int py = 0; py < lado; py++)
            {
                for (int px = 0; px < lado; px++)
                {
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa((px - mitad) / mitad, (py - mitad) / mitad)) * 255f);
                    pixeles[py * lado + px] = new Color32(255, 255, 255, a);
                }
            }
            t.SetPixels32(pixeles);
            t.Apply(false, true);
            sprite = Sprite.Create(t, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), lado);
            sprite.name = nombre;
            return sprite;
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
            vineta = null;
            cruzMarcas = null;
            anilloAbierto = null;
            acentoCian = null;
        }
    }
}
