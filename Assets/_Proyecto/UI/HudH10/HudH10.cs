using System.Collections.Generic;
using System.Globalization;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// El HUD nuevo (T-700), con el acomodo de H10_HUD.png y diseño propio: líneas finas, sin paneles, rojo-rosa para la vida y turquesa para
    /// el resto. Arriba a la izquierda escudo, vida con su número, 5 estrellas de búsqueda y dinero; arriba a la derecha el objetivo con su
    /// distancia (el minimapa cuadrado lo dibuja <c>Minimapa</c> encima); abajo a la izquierda los objetos con su tecla y cantidad; abajo a la
    /// derecha las ayudas de botón, el arma con sus balas y el muñequito con los implantes y el daño. Lo agrega <see cref="HudAPie"/> al nacer,
    /// lee los mismos servicios que él y esconde (sin apagar) lo viejo que duplica. A pie muestra todo; en el carro solo estrellas y objetivo;
    /// en el mecha nada.
    /// </summary>
    public class HudH10 : MonoBehaviour
    {
        public const int OrdenCanvas = 12;
        public const float Margen = 63f;

        private const float LadoRanura = 114f;
        private const float HuecoRanura = 18f;
        private const int Ranuras = 5;
        private const float LadoEstrella = 38f;
        private const float PasoEstrella = 49f;
        private const float Linea = 2f;
        private const float UmbralVidaBaja = 0.25f;

        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color TurquesaSuave = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color Rosa = new Color(0.93f, 0.40f, 0.46f, 1f);
        private static readonly Color RosaSuave = new Color(1f, 0.82f, 0.85f, 1f);
        private static readonly Color Rojo = new Color(1f, 0.22f, 0.28f, 1f);
        private static readonly Color Blanco = new Color(0.93f, 0.97f, 0.97f, 1f);
        private static readonly Color FondoCasilla = new Color(0.02f, 0.07f, 0.11f, 0.38f);

        private HudAPie hud;
        private ConfigHudH10 cfg;
        private Canvas canvas;
        private GameObject grupoIzquierda;
        private GameObject grupoEstrellas;
        private GameObject grupoObjetivo;
        private GameObject grupoObjetos;
        private GameObject grupoArma;
        private Text textoTitulo;
        private string tituloActual = string.Empty;
        private float inicioTitulo = float.MinValue;
        private float duracionTitulo;
        private string zonaPrevia;
        private ITerritorio territorioVisto;

        private RectTransform barraEscudo;
        private BarraConPerdida barraEscudoPerdida;
        private BarraConPerdida barraVida;
        private Color colorVidaNormal = Rosa;
        private Image emblemaVida;
        private Text textoVida;
        private Text textoDinero;
        private Image[] estrellas;
        private Text textoObjetivo;
        private RanuraObjeto[] ranuras;
        private Image iconoArma;
        private Text textoBalas;
        private Text[] textosAyuda;
        private Image[] cuerpoLinea;
        private Image[] cuerpoRelleno;

        private IJugador jugador;
        private IVida vida;
        private ArmasAPie armas;
        private InteraccionAPie interaccion;
        private ConsumiblesJugador consumibles;
        private int armaMostrada = -1;
        private int estrellasLlenas;
        private float segundosSinVer;
        private bool buscando;
        private ZonaCuerpoH10 ultimaZona;
        private float segundosGolpe = float.MaxValue;
        private bool horaEscondida;
        private InputAction accionRecargar;
        private InputAction accionGolpe;
        private InputAction accionInteractuar;
        private float proximaBusquedaAcciones;

        private sealed class RanuraObjeto
        {
            public GameObject Raiz;
            public Image Fondo;
            public GameObject Borde;
            public Image Icono;
            public Text Tecla;
            public Text Cantidad;
            public Text Nombre;
        }

        public bool Armado => canvas != null;
        public Canvas Canvas => canvas;
        public Text TextoVida => textoVida;
        public Image RellenoVida => barraVida.Relleno;
        public Image RellenoEscudo => barraEscudoPerdida.Relleno;
        /// <summary>T-725: la capa de la parte perdida (se queda un momento y baja con DOTween).</summary>
        public Image PerdidaVida => barraVida.Perdida;
        public Image PerdidaEscudo => barraEscudoPerdida.Perdida;
        public bool PerdidaBajando => barraVida.Bajando || barraEscudoPerdida.Bajando;
        public Image EmblemaVida => emblemaVida;
        /// <summary>Tinte del relleno de vida con la vida baja.</summary>
        public static Color ColorVidaBaja => Rojo;
        public GameObject BarraEscudo => barraEscudo != null ? barraEscudo.gameObject : null;
        public Text TextoDinero => textoDinero;
        public Text TextoObjetivo => textoObjetivo;
        public Text TextoBalas => textoBalas;
        public Image IconoArma => iconoArma;
        /// <summary>El HUD nuevo vivo (null si no hay).</summary>
        public static HudH10 Actual { get; private set; }

        /// <summary>El nombre de zona de arriba al centro: lo que dice ahora y si se ve (alfa mayor que 0).</summary>
        public string TituloZona => tituloActual;
        public bool TituloVisible => textoTitulo != null && textoTitulo.color.a > 0.01f;
        public int EstrellasLlenas => estrellasLlenas;
        public bool Buscando => buscando;
        public Image Estrella(int indice) => estrellas[indice];
        public GameObject GrupoIzquierda => grupoIzquierda;
        public GameObject GrupoEstrellas => grupoEstrellas;
        public GameObject GrupoObjetivo => grupoObjetivo;
        public GameObject GrupoObjetos => grupoObjetos;
        public GameObject GrupoArma => grupoArma;
        public Text[] TextosAyuda => textosAyuda;

        /// <summary>Casilla de objeto <paramref name="indice"/>: si se ve, su tecla, su cantidad y su ícono.</summary>
        public bool RanuraVisible(int indice) => ranuras != null && ranuras[indice].Raiz.activeSelf;
        public string TeclaRanura(int indice) => ranuras[indice].Tecla.text;
        public string CantidadRanura(int indice) => ranuras[indice].Cantidad.text;
        public Sprite IconoRanura(int indice) => ranuras[indice].Icono.sprite;
        public Image BordeRanura(int indice) => ranuras[indice].Fondo;

        /// <summary>El muñequito: color de la línea y del relleno de una parte (rojo con daño, relleno turquesa con implante).</summary>
        public Color ColorLineaParte(ZonaCuerpoH10 zona) => cuerpoLinea[(int)zona].color;
        public float AlfaRellenoParte(ZonaCuerpoH10 zona) => cuerpoRelleno[(int)zona].color.a;
        public Color ColorRellenoParte(ZonaCuerpoH10 zona) => cuerpoRelleno[(int)zona].color;

        private void Awake() => Actual = this;

        private void Start()
        {
            hud = GetComponent<HudAPie>();
            cfg = ConfigHudH10.Actual;
            if (hud == null)
            {
                enabled = false;
                return;
            }
            Armar();
            EscondeLoViejo();
            EventosPelea.ImpactoRecibido += AlImpacto;
        }

        private void OnDestroy()
        {
            barraVida?.Detener();
            barraEscudoPerdida?.Detener();
            EventosPelea.ImpactoRecibido -= AlImpacto;
            if (Actual == this)
            {
                Actual = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico() => Actual = null;

        /// <summary>
        /// El nombre de la zona, chico y delgado arriba al centro unos segundos y luego se desvanece (T-701). False si no hay HUD nuevo armado
        /// (quien lo pide puede usar la cola de avisos).
        /// </summary>
        public static bool MostrarTitulo(string texto, float segundos)
        {
            HudH10 h = Actual;
            if (h == null || string.IsNullOrEmpty(texto))
            {
                return false;
            }
            h.tituloActual = texto;
            h.duracionTitulo = Mathf.Max(ReglaTituloZonaH10.SegundosDesvanecer, segundos);
            h.inicioTitulo = Time.unscaledTime;
            return true;
        }

        // ---- Armado ----

        private Font Fuente => cfg != null && cfg.Fuente != null ? cfg.Fuente : KitUI.Estilo.Fuente;
        private Font FuenteNumeros => cfg != null && cfg.FuenteNumeros != null ? cfg.FuenteNumeros : KitUI.Estilo.Fuente;

        private void Armar()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasH10", OrdenCanvas);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            grupoIzquierda = Grupo("Izquierda");
            Vineta(grupoIzquierda.transform, "VinetaArribaIzquierda", new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(820f, 460f), false, true);
            grupoEstrellas = Grupo("Estrellas");
            grupoObjetivo = Grupo("Objetivo");
            grupoObjetos = Grupo("Objetos");
            Vineta(grupoObjetos.transform, "VinetaAbajoIzquierda", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(860f, 340f), false, false);
            grupoArma = Grupo("Arma");
            textoTitulo = Texto(canvas.transform, "TituloZona", new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 44f), 30, TurquesaSuave, TextAnchor.MiddleCenter);
            textoTitulo.rectTransform.pivot = new Vector2(0.5f, 1f);
            textoTitulo.color = new Color(TurquesaSuave.r, TurquesaSuave.g, TurquesaSuave.b, 0f);
            ArmarEscudoYVida();
            ArmarEstrellas();
            ArmarObjetivo();
            ArmarObjetos();
            ArmarArma();
            grupoIzquierda.SetActive(false);
            grupoEstrellas.SetActive(false);
            grupoObjetivo.SetActive(false);
            grupoObjetos.SetActive(false);
            grupoArma.SetActive(false);
        }

        private GameObject Grupo(string nombre)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform));
            g.transform.SetParent(canvas.transform, false);
            KitUI.Estirar((RectTransform)g.transform);
            return g;
        }

        private static RectTransform Nuevo(Transform padre, string nombre, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform));
            g.transform.SetParent(padre, false);
            RectTransform r = (RectTransform)g.transform;
            KitUI.Colocar(r, ancla, posicion, tamano);
            return r;
        }

        private Text Texto(Transform padre, string nombre, Vector2 ancla, Vector2 posicion, Vector2 tamano, int tamanoLetra, Color color, TextAnchor alineacion, bool numeros = false)
        {
            RectTransform r = Nuevo(padre, nombre, ancla, posicion, tamano);
            Text t = r.gameObject.AddComponent<Text>();
            t.font = numeros ? FuenteNumeros : Fuente;
            t.fontSize = tamanoLetra;
            t.fontStyle = FontStyle.Normal;
            t.alignment = alineacion;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.text = string.Empty;
            // Un contorno oscuro fino para que se lea sobre paredes claras; el texto sigue siendo delgado.
            Outline sombra = r.gameObject.AddComponent<Outline>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.75f);
            sombra.effectDistance = new Vector2(1.3f, -1.3f);
            return t;
        }

        private static Image Imagen(Transform padre, string nombre, Sprite sprite, Color color)
        {
            Image i = KitUI.CrearImagen(padre, nombre, color);
            i.sprite = sprite;
            i.preserveAspect = true;
            return i;
        }

        private static readonly Sprite[] spritesVineta = new Sprite[4];

        // Un degradado negro suave desde la esquina (sin bordes ni panel) para que las letras finas se lean sobre paredes claras.
        private static void Vineta(Transform padre, string nombre, Vector2 ancla, Vector2 posicion, Vector2 tamano, bool derecha, bool arriba)
        {
            int indice = (derecha ? 1 : 0) + (arriba ? 2 : 0);
            if (spritesVineta[indice] == null)
            {
                const int n = 64;
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (derecha ? n - 1 - x : x) + 0.5f;
                        float dy = (arriba ? n - 1 - y : y) + 0.5f;
                        float d = Mathf.Clamp01(new Vector2(dx / n, dy / n).magnitude);
                        t.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Pow(1f - d, 1.3f)));
                    }
                }
                t.Apply();
                spritesVineta[indice] = Sprite.Create(t, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f));
            }
            RectTransform r = Nuevo(padre, nombre, ancla, posicion, tamano);
            Image imagen = r.gameObject.AddComponent<Image>();
            imagen.sprite = spritesVineta[indice];
            imagen.color = new Color(1f, 1f, 1f, 0.8f);
            imagen.raycastTarget = false;
            r.SetAsFirstSibling();
        }

        private static Sprite Blanco1x1()
        {
            Texture2D blanco = Texture2D.whiteTexture;
            return Sprite.Create(blanco, new Rect(0f, 0f, blanco.width, blanco.height), new Vector2(0.5f, 0.5f));
        }

        // Barra del HUD: fondo, parte perdida, relleno y marco encima. Con los gráficos del chat de arte (marco y relleno) el relleno lleva su
        // color propio (tinte blanco); sin ellos queda la barra de línea fina de código (borde de 2 px y relleno liso).
        private BarraConPerdida ArmarBarra(RectTransform barra, Sprite marco, Sprite sprRelleno, Color colorSinGrafico, Color colorPerdida, Vector2 margen)
        {
            Image fondo = KitUI.CrearImagen(barra, "Fondo", new Color(0f, 0f, 0f, 0.28f));
            KitUI.Estirar(fondo.rectTransform);
            fondo.rectTransform.offsetMin = margen;
            fondo.rectTransform.offsetMax = -margen;
            Image perdida = RellenoFilled(barra, "Perdida", sprRelleno, colorPerdida, margen);
            Image fill = RellenoFilled(barra, "Relleno", sprRelleno, sprRelleno != null ? Color.white : colorSinGrafico, margen);
            if (marco != null)
            {
                Image m = KitUI.CrearImagen(barra, "Marco", Color.white);
                m.sprite = marco;
                KitUI.Estirar(m.rectTransform);
            }
            else
            {
                KitUI.CrearBorde(barra, Linea, new Color(colorSinGrafico.r, colorSinGrafico.g, colorSinGrafico.b, 0.9f));
            }
            return new BarraConPerdida(fill, perdida, cfg != null ? cfg.SegundosEsperaPerdida : 0.45f, cfg != null ? cfg.SegundosBajaPerdida : 0.6f);
        }

        private static Image RellenoFilled(RectTransform barra, string nombre, Sprite sprite, Color color, Vector2 margen)
        {
            Image fill = KitUI.CrearImagen(barra, nombre, color);
            fill.sprite = sprite != null ? sprite : Blanco1x1();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            KitUI.Estirar(fill.rectTransform);
            fill.rectTransform.offsetMin = margen;
            fill.rectTransform.offsetMax = -margen;
            return fill;
        }

        // T-725: sin las palabras VIDA ni ESCUDO. Emblema a la izquierda, escudo delgado y corto arriba, vida larga abajo con su número al lado.
        private void ArmarEscudoYVida()
        {
            Transform g = grupoIzquierda.transform;
            Vector2 tl = new Vector2(0f, 1f);
            const float xBarras = Margen + 90f;
            Sprite emblema = cfg != null ? cfg.EmblemaVida : null;
            if (emblema != null)
            {
                RectTransform re = Nuevo(g, "Emblema", tl, new Vector2(Margen, -48f), new Vector2(72f, 82f));
                emblemaVida = re.gameObject.AddComponent<Image>();
                emblemaVida.sprite = emblema;
                emblemaVida.preserveAspect = true;
                emblemaVida.raycastTarget = false;
                emblemaVida.color = Color.white;
            }
            barraEscudo = Nuevo(g, "BarraEscudo", tl, new Vector2(xBarras, -46f), new Vector2(215f, 29f));
            barraEscudoPerdida = ArmarBarra(barraEscudo, cfg != null ? cfg.MarcoEscudo : null, cfg != null ? cfg.RellenoEscudo : null, Turquesa, new Color(0.80f, 1f, 1f, 0.9f), new Vector2(4f, 4f));

            RectTransform marcoVida = Nuevo(g, "BarraVida", tl, new Vector2(xBarras, -98f), new Vector2(460f, 43f));
            barraVida = ArmarBarra(marcoVida, cfg != null ? cfg.MarcoVida : null, cfg != null ? cfg.RellenoVida : null, Rosa, new Color(1f, 0.86f, 0.88f, 0.9f), new Vector2(5f, 5f));
            colorVidaNormal = barraVida.Relleno.color;
            textoVida = Texto(g, "TextoVida", tl, new Vector2(xBarras + 476f, -98f), new Vector2(240f, 43f), 34, new Color(1f, 0.90f, 0.92f, 0.95f), TextAnchor.MiddleLeft, true);

            textoDinero = Texto(g, "Dinero", tl, new Vector2(Margen, -238f), new Vector2(420f, 60f), 46, Blanco, TextAnchor.MiddleLeft, true);
        }

        private void ArmarEstrellas()
        {
            estrellas = new Image[ReglaEstrellasH10.Total];
            Sprite contorno = cfg != null ? cfg.EstrellaContorno : null;
            for (int i = 0; i < estrellas.Length; i++)
            {
                RectTransform r = Nuevo(grupoEstrellas.transform, "Estrella" + i, new Vector2(0f, 1f), new Vector2(Margen + i * PasoEstrella, -187f), new Vector2(LadoEstrella, LadoEstrella));
                estrellas[i] = r.gameObject.AddComponent<Image>();
                estrellas[i].sprite = contorno;
                estrellas[i].preserveAspect = true;
                estrellas[i].raycastTarget = false;
                estrellas[i].color = Turquesa;
            }
        }

        private void ArmarObjetivo()
        {
            textoObjetivo = Texto(grupoObjetivo.transform, "Objetivo", new Vector2(1f, 1f), new Vector2(-Margen, -326f), new Vector2(760f, 40f), 30, Blanco, TextAnchor.MiddleRight);
        }

        private void ArmarObjetos()
        {
            ranuras = new RanuraObjeto[Ranuras];
            for (int i = 0; i < Ranuras; i++)
            {
                RanuraObjeto r = new RanuraObjeto();
                RectTransform raiz = Nuevo(grupoObjetos.transform, "Objeto" + i, new Vector2(0f, 0f), new Vector2(Margen + i * (LadoRanura + HuecoRanura), 62f), new Vector2(LadoRanura, LadoRanura));
                r.Raiz = raiz.gameObject;
                Sprite casilla = cfg != null ? cfg.CasillaObjeto : null;
                r.Fondo = raiz.gameObject.AddComponent<Image>();
                r.Fondo.color = FondoCasilla;
                r.Fondo.raycastTarget = false;
                if (casilla != null)
                {
                    // T-725: el gráfico de la casilla va encima del fondo (su cuadrito de tecla queda arriba a la izquierda).
                    Image marco = KitUI.CrearImagen(raiz, "Casilla", Color.white);
                    marco.sprite = casilla;
                    KitUI.Estirar(marco.rectTransform);
                    r.Borde = marco.gameObject;
                }
                else
                {
                    r.Borde = KitUI.CrearBorde(raiz, Linea, new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.85f));
                }
                RectTransform ri = Nuevo(raiz, "Icono", new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(80f, 80f));
                ri.pivot = new Vector2(0.5f, 0.5f);
                r.Icono = ri.gameObject.AddComponent<Image>();
                r.Icono.preserveAspect = true;
                r.Icono.raycastTarget = false;
                r.Icono.color = Turquesa;
                r.Nombre = Texto(raiz, "Nombre", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(LadoRanura, 40f), 20, TurquesaSuave, TextAnchor.MiddleCenter);
                r.Nombre.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                r.Tecla = casilla != null
                    ? TeclaEnCuadrito(raiz)
                    : Texto(raiz, "Tecla", new Vector2(1f, 1f), new Vector2(-8f, -4f), new Vector2(70f, 26f), 20, new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.9f), TextAnchor.MiddleRight, true);
                r.Cantidad = Texto(raiz, "Cantidad", new Vector2(1f, 0f), new Vector2(-9f, 4f), new Vector2(80f, 34f), 28, Blanco, TextAnchor.MiddleRight, true);
                r.Raiz.SetActive(false);
                ranuras[i] = r;
            }
        }

        // T-725: la tecla va dentro del cuadrito de la casilla (H26), sin corchetes y sin contorno para que se lea a ese tamaño.
        private Text TeclaEnCuadrito(RectTransform raiz)
        {
            Text t = Texto(raiz, "Tecla", new Vector2(0f, 1f), new Vector2(1f, -1f), new Vector2(LadoRanura * 0.25f, LadoRanura * 0.25f), 20, TurquesaSuave, TextAnchor.MiddleCenter, true);
            t.GetComponent<Outline>().enabled = false;
            return t;
        }

        private void ArmarArma()
        {
            Transform g = grupoArma.transform;
            Vector2 br = new Vector2(1f, 0f);
            Vineta(g, "VinetaAbajoDerecha", br, Vector2.zero, new Vector2(760f, 420f), true, false);
            // El muñequito, al borde de abajo a la derecha: una capa de línea y una de relleno por parte.
            RectTransform cuerpo = Nuevo(g, "Cuerpo", br, new Vector2(-62f, 62f), new Vector2(53f, 115f));
            cuerpoLinea = new Image[ConfigHudH10.Zonas];
            cuerpoRelleno = new Image[ConfigHudH10.Zonas];
            for (int z = 0; z < ConfigHudH10.Zonas; z++)
            {
                ZonaCuerpoH10 zona = (ZonaCuerpoH10)z;
                cuerpoRelleno[z] = Imagen(cuerpo, "Relleno" + zona, cfg != null ? cfg.CuerpoRelleno(zona) : null, new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0f));
                KitUI.Estirar(cuerpoRelleno[z].rectTransform);
                cuerpoLinea[z] = Imagen(cuerpo, "Linea" + zona, cfg != null ? cfg.CuerpoLinea(zona) : null, new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.85f));
                KitUI.Estirar(cuerpoLinea[z].rectTransform);
            }

            // El holograma trae margen: el cuerpo del arma (61 % del ancho de la imagen) queda a 141 px del borde, como en H10.
            RectTransform arma = Nuevo(g, "Arma", br, new Vector2(-76f, 62f), new Vector2(280f, 180f));
            iconoArma = arma.gameObject.AddComponent<Image>();
            iconoArma.preserveAspect = true;
            iconoArma.raycastTarget = false;
            iconoArma.color = Turquesa;
            iconoArma.enabled = false;

            textoBalas = Texto(g, "Balas", br, new Vector2(-309f, 52f), new Vector2(220f, 72f), 62, Blanco, TextAnchor.MiddleRight, true);
            textosAyuda = new Text[ReglaAyudasH10.Maximo];
            for (int i = 0; i < textosAyuda.Length; i++)
            {
                textosAyuda[i] = Texto(g, "Ayuda" + i, br, new Vector2(-62f, 212f + i * 48f), new Vector2(520f, 38f), 28, new Color(0.84f, 0.96f, 0.96f, 1f), TextAnchor.MiddleRight);
            }
        }

        // Lo viejo que duplica esto queda sin dibujarse (con CanvasGroup, sin apagarlo: HudAPie sigue llevando su estado).
        private void EscondeLoViejo()
        {
            Esconder(hud.GrupoVida);
            Esconder(hud.GrupoArma);
            Esconder(hud.GrupoCalor);
            Esconder(hud.GrupoDinero);
            Esconder(hud.GrupoObjetivo);
            Transform implantes = transform.Find("CanvasImplantes");
            if (implantes != null)
            {
                Esconder(implantes.gameObject);
            }
        }

        /// <summary>Deja de dibujar un grupo sin apagarlo.</summary>
        public static void Esconder(GameObject grupo)
        {
            if (grupo == null)
            {
                return;
            }
            CanvasGroup lienzo = grupo.GetComponent<CanvasGroup>();
            if (lienzo == null)
            {
                lienzo = grupo.AddComponent<CanvasGroup>();
            }
            lienzo.alpha = 0f;
        }

        // ---- Cada cuadro ----

        private void Update()
        {
            if (canvas == null)
            {
                return;
            }
            Vincular(Registro<IJugador>.Actual);
            ModoJugador modo = jugador != null && jugador.Raiz != null ? jugador.Modo : ModoJugador.Mecha;
            bool aPie = modo == ModoJugador.APie;
            bool carro = modo == ModoJugador.Carro;
            Activar(grupoIzquierda, aPie);
            Activar(grupoEstrellas, aPie || carro);
            Activar(grupoObjetivo, (aPie || carro) && hud.GrupoObjetivo != null && hud.GrupoObjetivo.activeSelf);
            Activar(grupoObjetos, aPie);
            Activar(grupoArma, aPie);
            ActualizarTitulo(aPie || carro);
            if (!aPie && !carro)
            {
                return;
            }
            ActualizarEstrellas();
            ActualizarObjetivo();
            if (!aPie)
            {
                return;
            }
            ActualizarVida();
            ActualizarEscudo();
            ActualizarDinero();
            ActualizarObjetos();
            ActualizarArma();
            ActualizarAyudas();
            ActualizarCuerpo();
        }

        // Al cruzar a otra zona sale su nombre unos segundos; el texto se desvanece solo.
        private void ActualizarTitulo(bool puedeVerse)
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            string zona = territorio != null && jugador != null && jugador.Raiz != null ? territorio.ZonaEn(jugador.Raiz.position) : null;
            if (!ReferenceEquals(territorio, territorioVisto))
            {
                territorioVisto = territorio;
                zonaPrevia = zona;
            }
            else if (zona != null && zona != zonaPrevia)
            {
                zonaPrevia = zona;
                string nombre = zona.Length > 0 ? territorio.Nombre(zona) : string.Empty;
                if (nombre.Length > 0)
                {
                    MostrarTitulo(ReglaTituloZonaH10.Texto(nombre), ReglaTituloZonaH10.SegundosVisible);
                }
            }
            float alfa = puedeVerse ? ReglaTituloZonaH10.Alfa(Time.unscaledTime - inicioTitulo, duracionTitulo) : 0f;
            Poner(textoTitulo, alfa > 0f ? tituloActual : string.Empty);
            Color c = textoTitulo.color;
            if (!Mathf.Approximately(c.a, alfa))
            {
                textoTitulo.color = new Color(c.r, c.g, c.b, alfa);
            }
        }

        private static void Activar(GameObject objeto, bool ver)
        {
            if (objeto.activeSelf != ver)
            {
                objeto.SetActive(ver);
            }
        }

        private static void Poner(Text texto, string valor)
        {
            if (texto.text != valor)
            {
                texto.text = valor;
            }
        }

        private void Vincular(IJugador actual)
        {
            if (actual == jugador)
            {
                return;
            }
            jugador = actual;
            Transform raiz = jugador != null ? jugador.Raiz : null;
            vida = raiz != null ? raiz.GetComponent<IVida>() : null;
            armas = raiz != null ? raiz.GetComponent<ArmasAPie>() : null;
            interaccion = raiz != null ? raiz.GetComponent<InteraccionAPie>() : null;
            consumibles = raiz != null ? raiz.GetComponent<ConsumiblesJugador>() : null;
        }

        private void ActualizarVida()
        {
            if (vida == null)
            {
                return;
            }
            float actual = Mathf.Max(0f, vida.Actual);
            barraVida.Poner(FormatoHud.Fraccion(actual, vida.Maxima));
            barraVida.Relleno.color = FormatoHud.EsVidaBaja(actual, vida.Maxima, UmbralVidaBaja) ? Rojo : colorVidaNormal;
            Poner(textoVida, Mathf.CeilToInt(actual) + " / " + Mathf.CeilToInt(vida.Maxima));
        }

        private void ActualizarEscudo()
        {
            IEscudo escudo = Registro<IEscudo>.Actual;
            bool hay = escudo != null && escudo.Maximo > 0f;
            Activar(barraEscudo.gameObject, hay);
            if (!hay)
            {
                return;
            }
            barraEscudoPerdida.Poner(FormatoHud.Fraccion(escudo.Actual, escudo.Maximo));
        }

        private void ActualizarDinero()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            Poner(textoDinero, cartera != null ? TextoDineroH10(cartera.Dinero) : string.Empty);
            // T-701: la hora ya no sale en el HUD a pie (H10 no la lleva); HudAPie sigue llevándola, escondida.
            Text hora = hud.TextoHora;
            if (hora != null && !horaEscondida)
            {
                horaEscondida = true;
                Esconder(hora.gameObject);
            }
        }

        /// <summary>"$ 12,450": el dinero con separador de miles.</summary>
        public static string TextoDineroH10(int dinero) => "$ " + dinero.ToString("N0", CultureInfo.InvariantCulture);

        private void ActualizarEstrellas()
        {
            ICalor calor = Registro<ICalor>.Actual;
            estrellasLlenas = calor != null ? ReglaEstrellasH10.Llenas(calor.Nivel, calor.Valor, calor.Maximo) : 0;
            IZonaBusqueda zona = Registro<IZonaBusqueda>.Actual;
            bool conBusqueda = estrellasLlenas > 0 && zona != null && zona.Activa;
            // T-729: cuánto llevas sin que te vea la policía (la zona dice si te ven ahora); parpadean solo cuando te están por perder.
            segundosSinVer = conBusqueda && !zona.Visto ? segundosSinVer + Time.unscaledDeltaTime : 0f;
            buscando = ReglaEstrellasH10.DebeParpadear(conBusqueda, conBusqueda && zona.Visto, segundosSinVer);
            float alfa = ReglaEstrellasH10.Alfa(Time.unscaledTime, buscando);
            for (int i = 0; i < estrellas.Length; i++)
            {
                bool llena = i < estrellasLlenas;
                Sprite sprite = llena ? cfg?.EstrellaLlena : cfg?.EstrellaContorno;
                if (estrellas[i].sprite != sprite)
                {
                    estrellas[i].sprite = sprite;
                }
                estrellas[i].color = new Color(Turquesa.r, Turquesa.g, Turquesa.b, llena ? alfa : 0.75f);
            }
        }

        private void ActualizarObjetivo()
        {
            if (!grupoObjetivo.activeSelf)
            {
                return;
            }
            string texto = hud.TextoObjetivo != null ? hud.TextoObjetivo.text : string.Empty;
            string distancia = hud.TextoDistancia;
            Poner(textoObjetivo, string.IsNullOrEmpty(distancia) ? texto : texto + " · " + distancia);
        }

        private void ActualizarObjetos()
        {
            IReadOnlyList<DatosConsumible> lista = consumibles != null ? consumibles.Disponibles : null;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosConsumible elegido = consumibles != null ? consumibles.Seleccionado : null;
            for (int i = 0; i < ranuras.Length; i++)
            {
                RanuraObjeto r = ranuras[i];
                DatosConsumible c = lista != null && i < lista.Count ? lista[i] : null;
                Activar(r.Raiz, c != null && inventario != null);
                if (c == null || inventario == null)
                {
                    continue;
                }
                Sprite icono = cfg != null ? cfg.Icono(c.Id) : null;
                if (r.Icono.sprite != icono)
                {
                    r.Icono.sprite = icono;
                }
                r.Icono.enabled = icono != null;
                Poner(r.Nombre, icono == null ? c.Nombre : string.Empty);
                string tecla = consumibles.TextoTecla(c);
                bool enCuadrito = r.Borde != null && r.Borde.name == "Casilla";
                Poner(r.Tecla, tecla.Length > 0 ? (enCuadrito ? tecla : "[" + tecla + "]") : string.Empty);
                Poner(r.Cantidad, "x" + inventario.Cantidad(c));
                bool sel = c == elegido;
                r.Fondo.color = sel ? new Color(0.10f, 0.32f, 0.34f, 0.55f) : FondoCasilla;
            }
        }

        private void ActualizarArma()
        {
            bool fuego = armas != null && armas.ArmaActiva != ArmaAPie.Punos;
            if (armas != null)
            {
                int arma = (int)armas.ArmaActiva;
                PiezasUI piezas = PiezasUI.Actual;
                if (piezas != null && arma != armaMostrada)
                {
                    armaMostrada = arma;
                    Sprite sprite = piezas.ArmaHud(arma);
                    Material material = piezas.MaterialArmaHud(arma);
                    iconoArma.sprite = sprite;
                    iconoArma.material = material != null ? material : iconoArma.defaultMaterial;
                    // Solo el holograma de línea fina (H01-H04); los puños no llevan ícono: la ayuda dice Golpe.
                    iconoArma.enabled = sprite != null && piezas.EsHolograma(arma);
                }
            }
            string balas = string.Empty;
            if (fuego)
            {
                balas = armas.Balas + "<size=40><color=#8fdcdacc> | " + (armas.Reserva >= 0 ? armas.Reserva : armas.TamanoCargador) + "</color></size>"; // T-720: con reserva, la segunda cifra son las balas que quedan
            }
            Poner(textoBalas, balas);
        }

        private void ActualizarAyudas()
        {
            if (Time.unscaledTime >= proximaBusquedaAcciones && (accionRecargar == null || accionGolpe == null || accionInteractuar == null))
            {
                proximaBusquedaAcciones = Time.unscaledTime + 1f;
                accionRecargar = accionRecargar ?? TeclasHud.Buscar("Recargar");
                accionGolpe = accionGolpe ?? TeclasHud.Buscar("Atacar");
                accionInteractuar = accionInteractuar ?? TeclasHud.Buscar("Interactuar");
            }
            ContextoAyudaH10 c = Contexto();
            List<AyudaH10> ayudas = ReglaAyudasH10.Ayudas(c);
            // Fila 0 = la de abajo. Con dos ayudas la primera va arriba; con una sola, abajo.
            for (int i = 0; i < textosAyuda.Length; i++)
            {
                int cual = ayudas.Count == 2 ? 1 - i : (i == 0 ? 0 : -1);
                bool hay = cual >= 0 && cual < ayudas.Count;
                Poner(textosAyuda[i], hay ? "[" + ayudas[cual].Tecla + "] " + ayudas[cual].Texto : string.Empty);
            }
        }

        /// <summary>Lo que ve el HUD ahora, para decidir las ayudas.</summary>
        public ContextoAyudaH10 Contexto()
        {
            ContextoAyudaH10 c = new ContextoAyudaH10
            {
                Modo = jugador != null ? jugador.Modo : ModoJugador.Mecha,
                TeclaRecargar = TeclasHud.Texto(accionRecargar, "R"),
                TeclaGolpe = TeclasHud.Texto(accionGolpe, "Clic"),
                TeclaInteractuar = TeclasHud.Texto(accionInteractuar, "E"),
            };
            if (armas != null)
            {
                c.ArmaDeFuego = armas.ArmaActiva != ArmaAPie.Punos;
                c.Balas = armas.Balas;
                c.Cargador = armas.TamanoCargador;
                c.Recargando = armas.Recargando;
            }
            if (interaccion != null)
            {
                switch (interaccion.TipoCandidato)
                {
                    case TipoCandidatoAPie.Puerta:
                        c.Candidato = TipoCandidatoInteraccion.Puerta;
                        ILugar lugar = interaccion.LugarCandidato;
                        c.NombreCandidato = lugar != null ? lugar.Nombre : null;
                        break;
                    case TipoCandidatoAPie.Montable:
                        c.Candidato = TipoCandidatoInteraccion.Montable;
                        c.NombreCandidato = interaccion.MontableCandidato != null ? interaccion.MontableCandidato.Nombre : null;
                        break;
                    case TipoCandidatoAPie.Interactuable:
                        c.Candidato = TipoCandidatoInteraccion.Interactuable;
                        c.TextoInteractuable = interaccion.InteractuableCandidato != null ? interaccion.InteractuableCandidato.Texto : null;
                        c.PuedeUsarInteractuable = interaccion.InteractuableCandidato != null && interaccion.InteractuableCandidato.PuedeUsar;
                        break;
                }
            }
            return c;
        }

        // ---- Muñequito ----

        private void AlImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (objetivo != null || jugador == null || jugador.Raiz == null || info.Cantidad <= 0f || resultado.DanioAplicado <= 0f)
            {
                return;
            }
            Vector3 relativo = info.Punto - jugador.Raiz.position;
            Vector3 plano = new Vector3(relativo.x, 0f, relativo.z);
            if (plano.magnitude > 3f || relativo.y < -0.5f || relativo.y > 3f)
            {
                return;
            }
            ultimaZona = ReglaCuerpoH10.Zona(relativo.y, Vector3.Dot(relativo, jugador.Raiz.right));
            segundosGolpe = 0f;
        }

        private void ActualizarCuerpo()
        {
            if (segundosGolpe < float.MaxValue)
            {
                segundosGolpe += Time.unscaledDeltaTime;
            }
            IImplantes implantes = Registro<IImplantes>.Actual;
            bool vidaBaja = vida != null && FormatoHud.EsVidaBaja(vida.Actual, vida.Maxima, UmbralVidaBaja);
            float pulso = vidaBaja ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            for (int z = 0; z < ConfigHudH10.Zonas; z++)
            {
                ZonaCuerpoH10 zona = (ZonaCuerpoH10)z;
                float rojo = zona == ultimaZona ? ReglaCuerpoH10.Intensidad(segundosGolpe) : 0f;
                if (vidaBaja)
                {
                    rojo = Mathf.Max(rojo, 0.8f + 0.2f * pulso);
                }
                bool implante = implantes != null && ImplanteDe(implantes, zona) > 0;
                Color linea = Color.Lerp(new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.85f), Rojo, rojo);
                cuerpoLinea[z].color = linea;
                float alfaRelleno = Mathf.Max(implante ? 0.30f : 0f, rojo * 0.45f);
                cuerpoRelleno[z].color = new Color(linea.r, linea.g, linea.b, alfaRelleno);
            }
        }

        private static int ImplanteDe(IImplantes implantes, ZonaCuerpoH10 zona)
        {
            switch (zona)
            {
                case ZonaCuerpoH10.Cabeza:
                    return implantes.Nivel(EspacioImplante.Ojos);
                case ZonaCuerpoH10.Brazos:
                    return implantes.Nivel(EspacioImplante.Brazos);
                case ZonaCuerpoH10.Piernas:
                    return implantes.Nivel(EspacioImplante.Piernas);
                default:
                    return 0;
            }
        }
    }
}
