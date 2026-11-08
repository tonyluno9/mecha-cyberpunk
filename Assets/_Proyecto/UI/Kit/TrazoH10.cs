using MechaCyberpunk.UI.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// La base visual de los menús (docs/guia-estilo-ui.md): la del HUD H10 llevada a pantalla completa. Líneas finas, un solo corte en la
    /// esquina superior derecha, paneles oscuros casi transparentes, turquesa de color principal, amarillo del logo de acento y coral solo
    /// para alertas y costos que no alcanzan. Todo se arma por código y los sprites se dibujan al pedirlos (blancos: se tiñen).
    /// </summary>
    public static class TrazoH10
    {
        public const float GrosorLinea = 1.5f;
        public const float SeparacionModulo = 12f;
        public const float MargenPantalla = 48f;
        public const int TamanoMicro = 20;
        public const int TamanoDato = 24;
        public const int TamanoFila = 28;
        public const int TamanoSeccion = 34;
        public const int TamanoTitulo = 52;
        public const int TamanoCifra = 44;

        private const int LadoSprite = 64;
        private const int Corte = 16;
        private const int LadoCheuron = 32;

        public static readonly Color Turquesa = new Color(0.361f, 0.890f, 0.839f, 1f);
        public static readonly Color TurquesaTenue = new Color(0.361f, 0.890f, 0.839f, 0.42f);
        public static readonly Color TurquesaFondo = new Color(0.361f, 0.890f, 0.839f, 0.12f);
        public static readonly Color Amarillo = new Color(0.965f, 0.953f, 0.149f, 1f);
        public static readonly Color Coral = new Color(0.961f, 0.431f, 0.388f, 1f);
        public static readonly Color Texto = new Color(0.902f, 0.961f, 0.953f, 1f);
        public static readonly Color TextoTenue = new Color(0.557f, 0.690f, 0.690f, 1f);
        public static readonly Color FondoPanel = new Color(0.012f, 0.035f, 0.051f, 0.62f);
        public static readonly Color FondoPantalla = new Color(0.008f, 0.024f, 0.035f, 0.82f);

        private static Sprite relleno;
        private static Sprite linea;
        private static Sprite blanco;
        private static Sprite cheuron;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            relleno = linea = blanco = cheuron = null;
        }

        /// <summary>Rajdhani, la misma letra del HUD H10.</summary>
        public static Font Fuente
        {
            get
            {
                ConfigHudH10 config = ConfigHudH10.Actual;
                return config != null && config.Fuente != null ? config.Fuente : KitUI.Estilo.Fuente;
            }
        }

        /// <summary>Relleno con la esquina superior derecha cortada (Image Sliced).</summary>
        public static Sprite Relleno => relleno != null ? relleno : relleno = CrearCorte(false);

        /// <summary>Solo la línea del contorno, con el mismo corte (Image Sliced).</summary>
        public static Sprite Linea => linea != null ? linea : linea = CrearCorte(true);

        /// <summary>Cuadrado blanco.</summary>
        public static Sprite Blanco => blanco != null ? blanco : blanco = Crear(4, 4, (x, y) => 1f, Vector4.zero);

        /// <summary>Punta de flecha fina que apunta a la derecha, la marca de "elegido".</summary>
        public static Sprite Cheuron => cheuron != null ? cheuron : cheuron = Crear(LadoCheuron, LadoCheuron, (x, y) =>
        {
            float centro = (LadoCheuron - 1) * 0.5f;
            float dx = x + Mathf.Abs(y - centro) * 0.75f;
            return Mathf.Clamp01(1f - Mathf.Abs(dx - 22f) / 3f);
        }, Vector4.zero);

        public static RectTransform Nodo(Transform padre, string nombre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.layer = padre.gameObject.layer;
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            return rect;
        }

        /// <summary>Imagen sin raycast; con sprite de corte queda Sliced.</summary>
        public static Image Imagen(Transform padre, string nombre, Color color, Sprite sprite = null)
        {
            Image imagen = Nodo(padre, nombre).gameObject.AddComponent<Image>();
            imagen.sprite = sprite != null ? sprite : Blanco;
            imagen.type = sprite == Relleno || sprite == Linea ? Image.Type.Sliced : Image.Type.Simple;
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        /// <summary>Texto en Rajdhani, sin raycast y sin recortes.</summary>
        public static Text Etiqueta(Transform padre, string nombre, string contenido, int tamano, Color color, TextAnchor ancla = TextAnchor.MiddleLeft)
        {
            Text texto = Nodo(padre, nombre).gameObject.AddComponent<Text>();
            texto.font = Fuente;
            texto.fontSize = tamano;
            texto.alignment = ancla;
            texto.color = color;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.supportRichText = false;
            texto.raycastTarget = false;
            texto.text = contenido;
            return texto;
        }

        /// <summary>Panel con relleno y línea. El relleno es el Image que se devuelve; la línea es su hijo "Linea".</summary>
        public static Image Panel(Transform padre, string nombre, Color linea, Color? fondo = null)
        {
            Image panel = Imagen(padre, nombre, fondo ?? FondoPanel, Relleno);
            Image contorno = Imagen(panel.transform, "Linea", linea, Linea);
            Estirar(contorno.rectTransform);
            return panel;
        }

        /// <summary>Línea recta horizontal o vertical de un grosor fijo.</summary>
        public static Image Recta(Transform padre, string nombre, Color color, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 posicion, Vector2 tamano)
        {
            Image recta = Imagen(padre, nombre, color);
            Colocar(recta.rectTransform, anclaMin, anclaMax, pivote, posicion, tamano);
            return recta;
        }

        /// <summary>Encabezado de sección: texto chico, en turquesa, sobre una línea que se apaga hacia la derecha.</summary>
        public static Text Seccion(Transform padre, string nombre, string contenido, Vector2 posicion, float ancho)
        {
            Text texto = Etiqueta(padre, nombre, contenido, TamanoDato, Turquesa);
            Punto(texto.rectTransform, new Vector2(0f, 1f), posicion, new Vector2(ancho, 30f));
            Image recta = Recta(texto.transform, "Regla", TurquesaTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, GrosorLinea));
            recta.raycastTarget = false;
            return texto;
        }

        /// <summary>Marcas de regla a lo largo de un borde: la textura de instrumento del HUD.</summary>
        public static RectTransform Regla(Transform padre, string nombre, int marcas, float separacion, float alto, Color color)
        {
            RectTransform raiz = Nodo(padre, nombre);
            for (int i = 0; i < marcas; i++)
            {
                bool mayor = i % 5 == 0;
                Image marca = Imagen(raiz, "Marca", color);
                Punto(marca.rectTransform, new Vector2(0f, 0f), new Vector2(i * separacion, 0f), new Vector2(GrosorLinea, mayor ? alto : alto * 0.5f));
            }
            raiz.sizeDelta = new Vector2(marcas * separacion, alto);
            return raiz;
        }

        public static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Punto(RectTransform rect, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            Colocar(rect, ancla, ancla, ancla, posicion, tamano);
        }

        public static void Colocar(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 posicion, Vector2 tamano)
        {
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.pivot = pivote;
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
        }

        public static Color ConAlfa(Color color, float alfa)
        {
            color.a = alfa;
            return color;
        }

        private static Sprite CrearCorte(bool soloLinea)
        {
            return Crear(LadoSprite, LadoSprite, (x, y) =>
            {
                int arriba = LadoSprite - 1 - y;
                int derecha = LadoSprite - 1 - x;
                float diagonal = (derecha + arriba - Corte) * 0.7071f;
                if (diagonal < -0.5f)
                {
                    return 0f;
                }
                if (!soloLinea)
                {
                    return Mathf.Clamp01(diagonal + 0.5f);
                }
                float distancia = Mathf.Min(Mathf.Min(x, derecha), Mathf.Min(y, Mathf.Min(arriba, diagonal)));
                return Mathf.Clamp01(GrosorLinea - distancia);
            }, new Vector4(Corte + 2, Corte + 2, Corte + 2, Corte + 2));
        }

        private static Sprite Crear(int ancho, int alto, System.Func<int, int, float> alfa, Vector4 borde)
        {
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "TrazoH10",
            };
            Color32[] pixeles = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    pixeles[y * ancho + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa(x, y)) * 255f));
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply(false, false);
            Sprite sprite = Sprite.Create(textura, new Rect(0f, 0f, ancho, alto), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borde);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sprite.name = "TrazoH10";
            return sprite;
        }
    }
}
