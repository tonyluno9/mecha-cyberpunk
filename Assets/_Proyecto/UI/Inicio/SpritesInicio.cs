using UnityEngine;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// Los sprites de la pantalla de inicio, dibujados con código al pedirlos (blancos con alfa, se tiñen con el color del Image):
    /// degradados, un brillo redondo, líneas de barrido, un triángulo y el panel de esquinas cortadas (9-slice).
    /// </summary>
    public static class SpritesInicio
    {
        private const int LadoPanel = 64;
        private const int CortePanel = 18;
        private const float GrosorBorde = 2.2f;
        private const int LadoBrillo = 128;
        private const int AnchoDegradado = 128;

        private static Sprite blanco;
        private static Sprite degradadoH;
        private static Sprite degradadoV;
        private static Sprite degradadoVAbajo;
        private static Sprite brillo;
        private static Sprite lineas;
        private static Sprite triangulo;
        private static Sprite panel;
        private static Sprite panelBorde;
        private static Sprite circulo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            blanco = degradadoH = degradadoV = degradadoVAbajo = brillo = lineas = triangulo = panel = panelBorde = circulo = null;
        }

        /// <summary>Cuadrado blanco liso.</summary>
        public static Sprite Blanco => blanco != null ? blanco : blanco = Crear(4, 4, (x, y) => 1f, FilterMode.Point, Vector4.zero);

        /// <summary>Opaco a la izquierda y transparente a la derecha, con curva suave.</summary>
        public static Sprite DegradadoH => degradadoH != null ? degradadoH
            : degradadoH = Crear(AnchoDegradado, 4, (x, y) => Suave(1f - x / (AnchoDegradado - 1f)), FilterMode.Bilinear, Vector4.zero);

        /// <summary>Opaco arriba y transparente abajo, con curva suave.</summary>
        public static Sprite DegradadoV => degradadoV != null ? degradadoV
            : degradadoV = Crear(4, AnchoDegradado, (x, y) => Suave(y / (AnchoDegradado - 1f)), FilterMode.Bilinear, Vector4.zero);

        /// <summary>Opaco abajo y transparente arriba, con curva suave (viñetas del pie de la pantalla).</summary>
        public static Sprite DegradadoVAbajo => degradadoVAbajo != null ? degradadoVAbajo
            : degradadoVAbajo = Crear(4, AnchoDegradado, (x, y) => Suave(1f - y / (AnchoDegradado - 1f)), FilterMode.Bilinear, Vector4.zero);

        /// <summary>Mancha redonda que se apaga hacia el borde (para halos de luz).</summary>
        public static Sprite Brillo => brillo != null ? brillo : brillo = Crear(LadoBrillo, LadoBrillo, (x, y) =>
        {
            float dx = (x - (LadoBrillo - 1) * 0.5f) / ((LadoBrillo - 1) * 0.5f);
            float dy = (y - (LadoBrillo - 1) * 0.5f) / ((LadoBrillo - 1) * 0.5f);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
        }, FilterMode.Bilinear, Vector4.zero);

        /// <summary>Círculo lleno de borde suave.</summary>
        public static Sprite Circulo => circulo != null ? circulo : circulo = Crear(LadoBrillo, LadoBrillo, (x, y) =>
        {
            float dx = (x - (LadoBrillo - 1) * 0.5f) / ((LadoBrillo - 1) * 0.5f);
            float dy = (y - (LadoBrillo - 1) * 0.5f) / ((LadoBrillo - 1) * 0.5f);
            return Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 18f);
        }, FilterMode.Bilinear, Vector4.zero);

        /// <summary>Una línea fina cada 4 píxeles: se repite (Tiled) como rejilla de barrido.</summary>
        public static Sprite Lineas => lineas != null ? lineas : lineas = Crear(4, 4, (x, y) => y == 0 ? 1f : 0f, FilterMode.Point, Vector4.zero);

        /// <summary>Triángulo que apunta a la derecha.</summary>
        public static Sprite Triangulo => triangulo != null ? triangulo : triangulo = Crear(64, 64, (x, y) =>
        {
            float limite = 1f - Mathf.Abs(y - 31.5f) / 31.5f;
            return Mathf.Clamp01((limite - x / 63f) * 40f);
        }, FilterMode.Bilinear, Vector4.zero);

        /// <summary>Relleno del panel con dos esquinas cortadas (arriba izquierda y abajo derecha). Image tipo Sliced.</summary>
        public static Sprite Panel => panel != null ? panel
            : panel = Crear(LadoPanel, LadoPanel, (x, y) => DentroPanel(x, y) ? 1f : 0f, FilterMode.Bilinear, new Vector4(CortePanel, CortePanel, CortePanel, CortePanel));

        /// <summary>Solo el borde del panel (misma forma). Image tipo Sliced.</summary>
        public static Sprite PanelBorde => panelBorde != null ? panelBorde : panelBorde = Crear(LadoPanel, LadoPanel, (x, y) =>
        {
            if (!DentroPanel(x, y))
            {
                return 0f;
            }
            int arriba = LadoPanel - 1 - y;
            int derecha = LadoPanel - 1 - x;
            float cercaBorde = Mathf.Min(Mathf.Min(x, derecha), Mathf.Min(y, arriba));
            float diagonalSuperior = x + arriba - CortePanel;
            float diagonalInferior = derecha + y - CortePanel;
            float cercaDiagonal = Mathf.Min(diagonalSuperior, diagonalInferior) * 0.7071f;
            float distancia = Mathf.Min(cercaBorde, cercaDiagonal);
            return Mathf.Clamp01(GrosorBorde - distancia);
        }, FilterMode.Bilinear, new Vector4(CortePanel, CortePanel, CortePanel, CortePanel));

        private static bool DentroPanel(int x, int y)
        {
            int arriba = LadoPanel - 1 - y;
            int derecha = LadoPanel - 1 - x;
            return x + arriba >= CortePanel && derecha + y >= CortePanel;
        }

        private static float Suave(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Sprite Crear(int ancho, int alto, System.Func<int, int, float> alfa, FilterMode filtro, Vector4 borde)
        {
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false)
            {
                filterMode = filtro,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "Inicio_Sprite",
            };
            Color32[] pixeles = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa(x, y)) * 255f);
                    pixeles[y * ancho + x] = new Color32(255, 255, 255, a);
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply(false, false);
            Sprite sprite = Sprite.Create(textura, new Rect(0f, 0f, ancho, alto), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borde);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sprite.name = "Inicio_Sprite";
            return sprite;
        }

        /// <summary>Sprite de lineas con repetición: el Image debe ser Tiled.</summary>
        public static void ConfigurarRepeticion(Sprite sprite)
        {
            if (sprite != null && sprite.texture != null)
            {
                sprite.texture.wrapMode = TextureWrapMode.Repeat;
            }
        }
    }
}
