using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Inventario;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El mapa del barrio con M (T-404, D-tony-220; T-409 con el kit): vista desde arriba con las 6 manzanas del color del dueño de su zona,
    /// su nombre y respeto, tú como flecha blanca, los enemigos, el objetivo y los iconos de El Mercado, I+D+I, el CIC y la casa.
    /// La rueda hace zoom de 1x a 3x centrado en el cursor; un clic izquierdo marca un punto (que sale en el minimapa) y otro clic sobre él lo
    /// quita. A la derecha va la leyenda. M o Esc lo cierran. Mientras está abierto el jugador a pie no se mueve ni dispara y el cursor se ve.
    /// </summary>
    public class PantallaMapa : MonoBehaviour
    {
        /// <summary>Título del mapa (documento del juego, 29-oct).</summary>
        public const string NombreCiudad = "MEGALOPOLIS";
        public const string TextoLeyenda = "LEYENDA";
        public const string TextoAyuda = "M O ESC PARA CERRAR · RUEDA: ZOOM · CLIC: MARCAR UN PUNTO O UN LUGAR";
        public const int OrdenCanvas = 40;
        public const float ZoomMinimo = 1f;
        public const float ZoomMaximo = 3f;
        public const string SonidoAbrir = "UI_MAPA_ABRIR";
        public const string SonidoCerrar = "UI_MAPA_CERRAR";
        public static readonly string[] EtiquetasFijasLeyenda =
            { "SKULLS", "SNAKES", "NOVA", "TÚ", "ENEMIGOS", "OBJETIVO", "EL MERCADO", "I+D+I", "CIC", "CASA", "PUNTO MARCADO" };

        // T-747: la imagen de la ciudad entera (400 x 280 m) llena la vista de 800 x 560 con zoom 1. Constante: el valor viejo seguía guardado en la escena.
        private const float pixelesPorMetro = MapaImagen.PixelesVistaPorMetro;
        [Tooltip("Tamaño de la vista del mapa (px).")]
        [SerializeField] private Vector2 tamanoVista = new Vector2(800f, 560f);
        /// <summary>Filas de la sección NEGOCIOS (T-429): caben 8 negocios, una por renglón (T-902).</summary>
        public const int FilasNegocios = 8;
        // T-902: composición H10. El mapa es el protagonista y a la derecha va un panel angosto con la leyenda y los negocios.
        private const float AnchoLateral = 520f;
        private const float RellenoLateral = 18f;
        private const float AltoFilaLeyenda = 36f;
        private const float AltoFilaNegocios = 34f;
        private const float InicioLeyenda = 50f;
        /// <summary>Tu pandilla más las once etiquetas fijas.</summary>
        public static readonly int FilasLeyenda = EtiquetasFijasLeyenda.Length + 1;
        private const float EspacioSecciones = 14f;
        private const int TamanoTextoLateral = TrazoH10.TamanoDato;
        [Tooltip("Cuánto cambia el zoom por cada paso de la rueda (multiplica).")]
        [Min(1.01f)] [SerializeField] private float pasoZoom = 1.25f;
        [Tooltip("Un clic a menos de esto del punto marcado lo quita (px de la vista).")]
        [Min(1f)] [SerializeField] private float radioQuitar = 24f;
        [Tooltip("Tamaño del rombo del objetivo de punta a punta (px a 1920x1080).")]
        [Min(4f)] [SerializeField] private float diagonalRombo = 36f;
        [Tooltip("Lado de los iconos de lugar (px).")]
        [Min(8f)] [SerializeField] private float ladoIcono = 32f;
        [Tooltip("Lado de la flecha del jugador (px).")]
        [Min(4f)] [SerializeField] private float ladoFlecha = 22f;

        private static PantallaMapa abierta;

        private GameObject panel;
        private RectTransform vista;
        private RectTransform contenido;
        private RectTransform puntoJugador;
        private readonly List<Image> marcas = new List<Image>();
        private readonly List<TipoMarcaMapa> tiposMarcas = new List<TipoMarcaMapa>();
        private Image[] manzanas;
        private Text[] etiquetas;
        private readonly List<Text> nombresBarrios = new List<Text>();
        private const int TamanoNombreBarrio = 44;
        private Text titulo;
        private RectTransform romboObjetivo;
        private Image puntoMarcado;
        private CirculoBusquedaMapa circuloBusqueda;
        private readonly List<Image> destinos = new List<Image>();
        // T-668: la ruta GPS naranja hasta lo marcado, por las calles (como en el minimapa).
        private const float MetrosParaRecalcularRuta = 2f;
        private const float GrosorRutaPx = 4f;
        private static readonly Color ColorRuta = TrazoH10.Amarillo;
        // T-934: el mapa es transparente y solo lleva contornos finos en turquesa.
        private const float GrosorCallesPx = 1.5f;
        private const float GrosorEdificiosPx = 1f;
        private const float AlfaVelo = 0.76f;
        private const int TamanoEtiquetaZona = 14;
        private const float AlfaNombreBarrio = 0.55f;
        private const float AlfaEdificios = 0.34f;
        private const float AlfaCalles = 0.9f;
        private LineasMapa lineasCalles;
        private LineasMapa lineasEdificios;
        private RectTransform capaRuta;
        private readonly List<Image> segmentosRuta = new List<Image>();
        private List<Vector3> ruta = new List<Vector3>();
        private Vector3 origenRuta;
        private Vector3 destinoRuta;
        private bool rutaCalculada;
        private GameObject fondoPantalla;
        private RectTransform panelLateral;
        private Text textoDinero;
        private float escalaVista = 1f;
        private ControladorAPie controlCongelado;
        private Text nombrePandilla;
        private Image colorPandilla;
        private readonly List<Text> textosLeyenda = new List<Text>();
        private readonly List<Text> textosNegocios = new List<Text>();
        private Text encabezadoNegocios;
        private float zoom = ZoomMinimo;
        private Vector2 desplazamiento;
        private CursorLockMode bloqueoPrevio;
        private bool cursorPrevio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => abierta = null;

        /// <summary>True si hay un mapa abierto (la historia esconde su ayuda mientras tanto).</summary>
        public static bool HayAbierto => abierta != null && abierta.Abierto;

        public bool Abierto => panel != null && panel.activeSelf;
        public Image[] Manzanas => manzanas;
        public Text[] Etiquetas => etiquetas;
        public Text Titulo => titulo;
        public IReadOnlyList<Text> TextosLeyenda => textosLeyenda;
        /// <summary>Las filas de la sección NEGOCIOS (T-429), una por negocio; las sobrantes quedan vacías.</summary>
        public IReadOnlyList<Text> TextosNegocios => textosNegocios;
        public Text EncabezadoNegocios => encabezadoNegocios;
        public float Zoom => zoom;
        public RectTransform Vista => vista;
        public RectTransform Contenido => contenido;
        /// <summary>La flecha del jugador en el mapa grande (T-747).</summary>
        public RectTransform PuntoJugador => puntoJugador;

        /// <summary>El rombo del objetivo (T-408), en px del dibujo a zoom 1 (x, z del mundo por píxeles por metro).</summary>
        public RectTransform RomboObjetivo => romboObjetivo;
        public bool RomboObjetivoVisible => romboObjetivo != null && romboObjetivo.gameObject.activeSelf;
        public float PixelesPorMetro => pixelesPorMetro;
        public RectTransform PuntoMarcado => puntoMarcado != null ? puntoMarcado.rectTransform : null;
        /// <summary>El círculo de búsqueda de la policía (T-665).</summary>
        public CirculoBusquedaMapa CirculoBusqueda => circuloBusqueda;
        /// <summary>Puntos de la ruta GPS dibujada en el mapa grande (mundo), vacía si no hay nada marcado ni objetivo.</summary>
        public IReadOnlyList<Vector3> Ruta => ruta;
        /// <summary>Cuántos tramos de la ruta se dibujan ahora.</summary>
        public int SegmentosRutaVisibles { get; private set; }

        /// <summary>Dónde se ve en la pantalla (px) un punto del mundo, con el zoom de ahora.</summary>
        public Vector2 PantallaDe(Vector3 mundo)
        {
            Vector3 enMundoUI = contenido.TransformPoint(GeometriaBarrio.AMapa(mundo) * pixelesPorMetro);
            return RectTransformUtility.WorldToScreenPoint(null, enMundoUI);
        }

        /// <summary>La marca dibujada de <paramref name="marca"/> en el mapa, o null si no sale.</summary>
        public Image MarcaDe(IMarcaMapa marca)
        {
            IReadOnlyList<IMarcaMapa> todas = Registro<IMarcaMapa>.Todos;
            for (int i = 0; i < todas.Count && i < marcas.Count; i++)
            {
                if (ReferenceEquals(todas[i], marca))
                {
                    return marcas[i].gameObject.activeSelf ? marcas[i] : null;
                }
            }
            return null;
        }

        private void Awake()
        {
            // T-904: el inventario (tecla I) vive junto al mapa para que exista en cada escena que tiene mapa, sin tocar escenas.
            if (GetComponent<PantallaInventario>() == null)
            {
                gameObject.AddComponent<PantallaInventario>();
            }
            EstiloCyberfix e = KitUI.Estilo;
            float margen = TrazoH10.MargenPantalla;
            float separacion = TrazoH10.SeparacionModulo;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasMapa", OrdenCanvas);
            Image velo = KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");
            velo.color = TrazoH10.ConAlfa(TrazoH10.FondoPantalla, AlfaVelo);
            fondoPantalla = velo.gameObject;
            fondoPantalla.SetActive(false);
            RectTransform rectPanel = TrazoH10.Nodo(canvas.transform, "Panel");
            TrazoH10.Estirar(rectPanel);
            panel = rectPanel.gameObject;

            textoDinero = VestidoH10.Cabecera(rectPanel, NombreCiudad, null, "TU DINERO", out titulo);

            // El mapa ocupa todo el espacio entre la cabecera, la barra de teclas y el panel lateral.
            float arriba = VestidoH10.AltoCabecera + separacion;
            float areaAncho = KitUI.AnchoReferencia - 2f * margen - AnchoLateral - separacion;
            float areaAlto = KitUI.AltoReferencia - arriba - VestidoH10.AltoPie - separacion;
            escalaVista = Mathf.Min(areaAncho / tamanoVista.x, areaAlto / tamanoVista.y);
            Vector2 tamanoMarco = tamanoVista * escalaVista;

            // La vista recorta el dibujo con zoom.
            vista = KitUI.CrearImagen(rectPanel, "Vista", Color.clear).rectTransform;
            KitUI.Colocar(vista, new Vector2(0f, 1f), new Vector2(margen, -arriba), tamanoVista);
            vista.localScale = new Vector3(escalaVista, escalaVista, 1f);
            vista.gameObject.AddComponent<RectMask2D>();
            contenido = new GameObject("Contenido", typeof(RectTransform)).GetComponent<RectTransform>();
            contenido.SetParent(vista, false);
            KitUI.Colocar(contenido, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            contenido.pivot = new Vector2(0.5f, 0.5f);
            MarcoMapa(rectPanel, new Vector2(margen, -arriba), tamanoMarco);

            // T-934: sin imagen ni rellenos: los edificios y, encima, los bordes de las calles.
            lineasEdificios = LineasMapa.Crear(contenido, "ContornosEdificios", TrazoH10.ConAlfa(TrazoH10.Turquesa, AlfaEdificios));
            lineasCalles = LineasMapa.Crear(contenido, "ContornosCalles", TrazoH10.ConAlfa(TrazoH10.Turquesa, AlfaCalles));
            CrearNombresBarrios(e);
            float lado = GeometriaBarrio.LadoManzana * pixelesPorMetro;
            manzanas = new Image[GeometriaBarrio.Cantidad];
            etiquetas = new Text[GeometriaBarrio.Cantidad];
            for (int i = 0; i < manzanas.Length; i++)
            {
                manzanas[i] = KitUI.CrearImagen(contenido, "Manzana_" + (i + 1), Color.gray);
                // T-747: el dueño se ve en el borde, para dejar ver la imagen.
                manzanas[i].sprite = IconosMapa.Marco;
                manzanas[i].raycastTarget = false;
                manzanas[i].enabled = false; // T-934: sin rellenos ni bordes de color: el color del dueño va en su etiqueta
                KitUI.Colocar(manzanas[i].rectTransform, new Vector2(0.5f, 0.5f), GeometriaBarrio.Centros[i] * pixelesPorMetro, Vector2.one * lado);
                etiquetas[i] = KitUI.CrearTexto(manzanas[i].transform, "Etiqueta", RolTexto.Ayuda, TextAnchor.MiddleCenter);
                etiquetas[i].fontSize = TamanoEtiquetaZona;
                etiquetas[i].color = e.Texto;
            }

            circuloBusqueda = new CirculoBusquedaMapa(contenido);
            capaRuta = new GameObject("RutaGps", typeof(RectTransform)).GetComponent<RectTransform>();
            capaRuta.SetParent(contenido, false);
            KitUI.Colocar(capaRuta, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            capaRuta.pivot = new Vector2(0.5f, 0.5f);
            puntoMarcado = IconosMapa.CrearPuntoMarcado(contenido, ladoIcono);
            puntoMarcado.gameObject.SetActive(false);
            romboObjetivo = (RectTransform)KitUI.CrearRombo(contenido, "Objetivo", diagonalRombo, e.Naranja).rectTransform.parent;
            romboObjetivo.gameObject.SetActive(false);
            puntoJugador = (RectTransform)KitUI.CrearTriangulo(contenido, "Jugador", ladoFlecha, Color.white).rectTransform.parent;

            ConstruirLateral(rectPanel, arriba, areaAlto);
            VestidoH10.Pie(rectPanel, TextoAyuda);
            KitUI.VestirTurquesa(canvas.transform);
            VestidoH10.Fuente(canvas.transform);
            panel.SetActive(false);
        }

        // Cuatro rectas finas alrededor de la vista (la vista es un rectángulo recortado, por eso no lleva el corte).
        private static void MarcoMapa(RectTransform padre, Vector2 esquina, Vector2 tamano)
        {
            float g = TrazoH10.GrosorLinea;
            Vector2 ancla = new Vector2(0f, 1f);
            Color c = TrazoH10.TurquesaTenue;
            TrazoH10.Recta(padre, "MarcoArriba", c, ancla, ancla, ancla, esquina, new Vector2(tamano.x, g));
            TrazoH10.Recta(padre, "MarcoAbajo", c, ancla, ancla, ancla, esquina + new Vector2(0f, -tamano.y + g), new Vector2(tamano.x, g));
            TrazoH10.Recta(padre, "MarcoIzquierda", c, ancla, ancla, ancla, esquina, new Vector2(g, tamano.y));
            TrazoH10.Recta(padre, "MarcoDerecha", c, ancla, ancla, ancla, esquina + new Vector2(tamano.x - g, 0f), new Vector2(g, tamano.y));
        }

        // Panel lateral angosto: LEYENDA arriba y NEGOCIOS debajo, con datos reales.
        private void ConstruirLateral(RectTransform padre, float arriba, float alto)
        {
            panelLateral = TrazoH10.Panel(padre, "PanelLateral", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(panelLateral, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-TrazoH10.MargenPantalla, -arriba), new Vector2(AnchoLateral, alto));
            float ancho = AnchoLateral - 2f * RellenoLateral;
            TrazoH10.Seccion(panelLateral, "SeccionLeyenda", TextoLeyenda, new Vector2(RellenoLateral, -12f), ancho);
            CrearLeyenda(panelLateral, new Vector2(RellenoLateral, -InicioLeyenda), ancho);
            float yNegocios = InicioLeyenda + FilasLeyenda * AltoFilaLeyenda + EspacioSecciones;
            CrearNegocios(panelLateral, new Vector2(RellenoLateral, -yNegocios), ancho);
        }

        /// <summary>Los cuatro nombres grandes de los barrios (T-747), semitransparentes del color del barrio, como en M10.</summary>
        public IReadOnlyList<Text> NombresBarrios => nombresBarrios;
        /// <summary>T-934: las líneas de los bordes de las calles.</summary>
        public LineasMapa LineasCalles => lineasCalles;

        /// <summary>T-934: las líneas de los contornos de los edificios.</summary>
        public LineasMapa LineasEdificios => lineasEdificios;

        /// <summary>T-934: la vista no tiene fondo propio.</summary>
        public Color ColorVista => vista.GetComponent<Image>().color;

        private void CrearNombresBarrios(EstiloCyberfix e)
        {
            for (int i = 0; i < BarriosMapa.Todos.Length; i++)
            {
                BarriosMapa.Barrio b = BarriosMapa.Todos[i];
                Text t = KitUI.CrearTexto(contenido, "Barrio_" + b.Nombre, RolTexto.Encabezado, TextAnchor.MiddleCenter);
                KitUI.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), b.PuntoNombre * pixelesPorMetro, new Vector2(300f, 70f));
                t.fontSize = TamanoNombreBarrio;
                t.text = b.Nombre;
                t.color = TrazoH10.ConAlfa(TrazoH10.Turquesa, AlfaNombreBarrio);
                t.raycastTarget = false;
                nombresBarrios.Add(t);
            }
        }

        // A la derecha: tu pandilla, SKULLS, SNAKES, NOVA, tú, enemigos, objetivo, los cuatro lugares y el punto marcado.
        private void CrearLeyenda(RectTransform padre, Vector2 esquina, float ancho)
        {
            EstiloCyberfix e = KitUI.Estilo;
            RectTransform leyenda = TrazoH10.Nodo(padre, "Leyenda");
            KitUI.Colocar(leyenda, new Vector2(0f, 1f), esquina, new Vector2(ancho, FilasLeyenda * AltoFilaLeyenda));
            float cuadro = AltoFilaLeyenda * 0.6f;
            int fila = 0;
            colorPandilla = IconosMapa.Pieza(FilaVacia(leyenda, fila++, string.Empty, out nombrePandilla), "Cuadro", null, Color.white, cuadro);
            IconosMapa.Pieza(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[0], out _), "Cuadro", null, FormatoTerritorio.ColorDe(IdPandilla.Calaveras), cuadro);
            IconosMapa.Pieza(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[1], out _), "Cuadro", null, FormatoTerritorio.ColorDe(IdPandilla.Viboras), cuadro);
            IconosMapa.Pieza(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[2], out _), "Cuadro", null, FormatoTerritorio.ColorDe(IdPandilla.Nova), cuadro);
            KitUI.CrearTriangulo(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[3], out _), "Muestra", ladoFlecha * 0.8f, Color.white);
            IconosMapa.Crear(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[4], out _), TipoMarcaMapa.Enemigo, ladoIcono);
            KitUI.CrearRombo(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[5], out _), "Muestra", ladoIcono * 0.8f, e.Naranja);
            TipoMarcaMapa[] lugares = { TipoMarcaMapa.Mercado, TipoMarcaMapa.Clinica, TipoMarcaMapa.Cic, TipoMarcaMapa.Casa };
            for (int i = 0; i < lugares.Length; i++)
            {
                IconosMapa.Crear(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[6 + i], out _), lugares[i], ladoIcono * 0.85f);
            }
            IconosMapa.CrearPuntoMarcado(FilaVacia(leyenda, fila, EtiquetasFijasLeyenda[10], out _), ladoIcono * 0.8f);
        }

        // T-429: debajo de la leyenda, el encabezado NEGOCIOS y una fila por negocio.
        private void CrearNegocios(RectTransform padre, Vector2 esquina, float ancho)
        {
            encabezadoNegocios = TrazoH10.Seccion(padre, "NegociosEncabezado", "NEGOCIOS", esquina, ancho);
            for (int i = 0; i < FilasNegocios; i++)
            {
                Text t = TrazoH10.Etiqueta(padre, "Negocio_" + (i + 1), string.Empty, TamanoTextoLateral, TrazoH10.Texto);
                TrazoH10.Punto(t.rectTransform, new Vector2(0f, 1f), esquina + new Vector2(0f, -40f - i * AltoFilaNegocios), new Vector2(ancho, AltoFilaNegocios));
                textosNegocios.Add(t);
            }
        }

        /// <summary>La fila de un negocio en el mapa: "PUESTO DEL TIANGUIS · NIV 2 · Ƀ72/DÍA".</summary>
        public static string LineaNegocio(ServicioNegocios servicio, int i)
        {
            DatosNegocios.Negocio n = servicio.Definicion(i);
            int nivel = servicio.Nivel(n.id);
            if (nivel == 0)
            {
                return n.nombre + " · " + (servicio.ZonaTuya(n.id) ? "SIN TOMAR" : "ZONA AJENA");
            }
            return n.nombre + " · NIV " + nivel + " · " + (servicio.ZonaTuya(n.id) ? Moneda.Texto(servicio.IngresoDia(n.id)) + "/DÍA" : "ZONA PERDIDA");
        }

        private void RefrescarNegocios()
        {
            ServicioNegocios servicio = Registro<ServicioNegocios>.Actual;
            encabezadoNegocios.text = servicio == null ? "NEGOCIOS" : "NEGOCIOS · " + Moneda.Texto(servicio.TotalPorDia) + " POR DÍA";
            for (int i = 0; i < textosNegocios.Count; i++)
            {
                textosNegocios[i].text = servicio != null && i < servicio.Cantidad ? LineaNegocio(servicio, i) : string.Empty;
            }
        }

        // Una fila con el hueco de la muestra (a la izquierda) y su texto; devuelve el hueco.
        private Transform FilaVacia(RectTransform leyenda, int fila, string etiqueta, out Text texto)
        {
            float ancho = leyenda.sizeDelta.x;
            RectTransform rect = TrazoH10.Nodo(leyenda, "Fila_" + fila);
            KitUI.Colocar(rect, new Vector2(0f, 1f), new Vector2(0f, -fila * AltoFilaLeyenda), new Vector2(ancho, AltoFilaLeyenda));
            RectTransform hueco = TrazoH10.Nodo(rect, "Muestra");
            KitUI.Colocar(hueco, new Vector2(0f, 0.5f), new Vector2(AltoFilaLeyenda * 0.5f, 0f), Vector2.zero);
            hueco.pivot = new Vector2(0.5f, 0.5f);
            texto = TrazoH10.Etiqueta(rect, "Texto", etiqueta, TamanoTextoLateral, TrazoH10.Texto);
            TrazoH10.Estirar(texto.rectTransform);
            texto.rectTransform.offsetMin = new Vector2(AltoFilaLeyenda + KitUI.Estilo.Relleno * 0.5f, 0f);
            textosLeyenda.Add(texto);
            return hueco;
        }

        private void OnDisable() => Cerrar();

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado == null)
            {
                return;
            }
            if (!Abierto)
            {
                // T-410: M no abre en pausa ni encima de otra pantalla.
                if (teclado.mKey.wasPressedThisFrame && !Pausa.Activa && PilaPantallas.Vacia)
                {
                    Abrir();
                }
                return;
            }
            if (PilaPantallas.EsArriba(this) && (teclado.mKey.wasPressedThisFrame || teclado.escapeKey.wasPressedThisFrame))
            {
                Cerrar();
                return;
            }
            LeerRaton();
            Refrescar();
        }

        // La rueda hace zoom en el cursor y el clic marca o quita el punto.
        private void LeerRaton()
        {
            Mouse raton = Mouse.current;
            if (raton == null)
            {
                return;
            }
            Vector2 pantalla = raton.position.ReadValue();
            bool enVista = RectTransformUtility.RectangleContainsScreenPoint(vista, pantalla, null);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(vista, pantalla, null, out Vector2 enLaVista);
            float rueda = raton.scroll.ReadValue().y;
            if (rueda != 0f)
            {
                float nuevo = Mathf.Clamp(rueda > 0f ? zoom * pasoZoom : zoom / pasoZoom, ZoomMinimo, ZoomMaximo);
                // Desde el centro de la vista (su pivote está en la esquina de arriba a la izquierda).
                Vector2 cursor = enVista ? enLaVista - vista.rect.center : Vector2.zero;
                desplazamiento = CalculoMapa.DesplazamientoAlZoom(desplazamiento, cursor, zoom, nuevo, tamanoVista * 0.5f);
                zoom = nuevo;
                AplicarZoom();
            }
            if (enVista && raton.leftButton.wasPressedThisFrame)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(contenido, pantalla, null, out Vector2 local);
                // T-668: un clic sobre el icono de un lugar lo marca (o, si ya era el marcado, lo quita); el GPS traza la ruta hasta él.
                DestinoMapa lugar = CatalogoDestinosMapa.MasCercano(GeometriaBarrio.AMundo(local / pixelesPorMetro), radioQuitar / (pixelesPorMetro * zoom));
                if (lugar != null && CatalogoDestinosMapa.Posicion(lugar.Id, out Vector3 sitio))
                {
                    PuntoMarcadoMapa.AlternarDestino(lugar.Id, sitio);
                    return;
                }
                Vector2 marcado = GeometriaBarrio.AMapa(PuntoMarcadoMapa.Posicion) * pixelesPorMetro;
                if (PuntoMarcadoMapa.Hay && (local - marcado).magnitude * zoom <= radioQuitar)
                {
                    PuntoMarcadoMapa.Quitar();
                }
                else
                {
                    PuntoMarcadoMapa.Marcar(GeometriaBarrio.AMundo(local / pixelesPorMetro));
                }
            }
        }

        private void AplicarZoom()
        {
            contenido.localScale = new Vector3(zoom, zoom, 1f);
            contenido.anchoredPosition = desplazamiento;
            lineasCalles.FijarGrosor(GrosorCallesPx / zoom);
            lineasEdificios.FijarGrosor(GrosorEdificiosPx / zoom);
        }

        public void Abrir()
        {
            if (Abierto)
            {
                return;
            }
            panel.SetActive(true);
            fondoPantalla.SetActive(true);
            abierta = this;
            PilaPantallas.Abrir(this);
            IJugador jugador = Registro<IJugador>.Actual;
            controlCongelado = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<ControladorAPie>() : null;
            if (controlCongelado != null && controlCongelado.ControlActivo)
            {
                controlCongelado.ActivarControl(false);
            }
            else
            {
                controlCongelado = null;
            }
            bloqueoPrevio = Cursor.lockState;
            cursorPrevio = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Sonidos.Reproducir(SonidoAbrir);
            AnimH10.EntrarPanel(panelLateral, new Vector2(AnimH10.DesplazamientoEntrada, 0f));
            lineasCalles.Fijar(CatalogoCallesMapa.Tramos, pixelesPorMetro);
            lineasEdificios.Fijar(CatalogoCallesMapa.Edificios, pixelesPorMetro);
            AplicarZoom();
            Refrescar();
        }

        public void Cerrar()
        {
            if (!Abierto)
            {
                return;
            }
            panel.SetActive(false);
            fondoPantalla.SetActive(false);
            if (abierta == this)
            {
                abierta = null;
            }
            PilaPantallas.Cerrar(this);
            Cursor.lockState = bloqueoPrevio;
            Cursor.visible = cursorPrevio;
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
            Sonidos.Reproducir(SonidoCerrar);
        }

        private void Refrescar()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            textoDinero.text = Moneda.Texto(cartera != null ? cartera.Dinero : 0);
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            Vector3 contraZoom = new Vector3(1f / zoom, 1f / zoom, 1f);
            for (int i = 0; i < manzanas.Length; i++)
            {
                if (territorio == null)
                {
                    continue;
                }
                string zona = GeometriaBarrio.Zona(i, territorio);
                manzanas[i].color = zona.Length > 0 ? FormatoTerritorio.ColorDe(territorio.Dueno(zona)) : Color.gray;
                etiquetas[i].color = zona.Length > 0 ? manzanas[i].color : TrazoH10.TextoTenue;
                etiquetas[i].text = zona.Length > 0 ? territorio.Nombre(zona) + "\nRESPETO " + territorio.Respeto(zona) : string.Empty;
                etiquetas[i].rectTransform.localScale = contraZoom;
            }
            Color tuColor = FormatoTerritorio.ColorDe(IdPandilla.Jugador);
            nombrePandilla.text = FormatoTerritorio.NombreDe(IdPandilla.Jugador);
            colorPandilla.color = tuColor;

            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null)
            {
                Transform mira = jugador.Camara != null ? jugador.Camara.transform : jugador.Raiz;
                puntoJugador.anchoredPosition = GeometriaBarrio.AMapa(jugador.Raiz.position) * pixelesPorMetro;
                puntoJugador.localRotation = Quaternion.Euler(0f, 0f, -mira.eulerAngles.y);
                puntoJugador.localScale = contraZoom;
            }

            // T-408: el objetivo, con el mismo rombo que el marcador.
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            Vector3 destino = Vector3.zero;
            bool hayObjetivo = objetivos != null && objetivos.HayObjetivo
                && UbicacionObjetivo.Resolver(objetivos.Actual, Registro<ILugar>.Todos, out destino);
            if (romboObjetivo.gameObject.activeSelf != hayObjetivo)
            {
                romboObjetivo.gameObject.SetActive(hayObjetivo);
            }
            romboObjetivo.anchoredPosition = GeometriaBarrio.AMapa(destino) * pixelesPorMetro;
            romboObjetivo.localScale = contraZoom;

            if (puntoMarcado.gameObject.activeSelf != PuntoMarcadoMapa.Hay)
            {
                puntoMarcado.gameObject.SetActive(PuntoMarcadoMapa.Hay);
            }
            puntoMarcado.rectTransform.anchoredPosition = GeometriaBarrio.AMapa(PuntoMarcadoMapa.Posicion) * pixelesPorMetro;
            puntoMarcado.rectTransform.localScale = contraZoom;

            circuloBusqueda.Actualizar(pixelesPorMetro);
            ActualizarRuta(contraZoom);
            DibujarDestinos(contraZoom);
            RefrescarNegocios();
            DibujarMarcas(contraZoom);
            puntoMarcado.rectTransform.SetAsLastSibling();
            romboObjetivo.SetAsLastSibling();
            puntoJugador.SetAsLastSibling();
        }

        // T-667: los lugares del catálogo, cada uno con su icono y su nombre debajo; los que ya dibuja una marca de lugar de T-409 no se repiten.
        private void DibujarDestinos(Vector3 contraZoom)
        {
            IReadOnlyList<DestinoMapa> catalogo = CatalogoDestinosMapa.Todos;
            while (destinos.Count < catalogo.Count)
            {
                Image icono = IconosMapa.CrearDestino(contenido, catalogo[destinos.Count], ladoIcono, true);
                icono.gameObject.SetActive(false);
                destinos.Add(icono);
            }
            for (int i = 0; i < catalogo.Count; i++)
            {
                bool hay = (catalogo[i].Id == IdDestinoMapa.ElDoc || !CatalogoDestinosMapa.LoDibujaUnaMarca(catalogo[i].Id)) && CatalogoDestinosMapa.Posicion(catalogo[i].Id, out Vector3 donde);
                if (destinos[i].gameObject.activeSelf != hay)
                {
                    destinos[i].gameObject.SetActive(hay);
                }
                if (hay)
                {
                    CatalogoDestinosMapa.Posicion(catalogo[i].Id, out donde);
                    destinos[i].rectTransform.anchoredPosition = GeometriaBarrio.AMapa(donde) * pixelesPorMetro;
                    destinos[i].rectTransform.localScale = contraZoom;
                }
            }
            SepararNombres(catalogo);
        }

        // T-721: los nombres de los lugares no se tapan entre sí, ni a los iconos, ni a los nombres de los barrios ni a los de las manzanas.
        private readonly List<Vector2> centrosNombres = new List<Vector2>();
        private readonly List<Vector2> tamanosNombres = new List<Vector2>();
        private readonly List<bool> arribaNombres = new List<bool>();
        private readonly List<bool> visiblesNombres = new List<bool>();
        private readonly List<Rect> fijosNombres = new List<Rect>();
        private readonly Rect[] nombresColocados = new Rect[16];

        private void SepararNombres(IReadOnlyList<DestinoMapa> catalogo)
        {
            centrosNombres.Clear();
            tamanosNombres.Clear();
            arribaNombres.Clear();
            visiblesNombres.Clear();
            fijosNombres.Clear();
            // Todo en píxeles de la vista (con zoom): el icono y su nombre no crecen con el zoom, las posiciones sí.
            for (int i = 0; i < catalogo.Count; i++)
            {
                Text nombre = IconosMapa.NombreDe(destinos[i]);
                bool visible = destinos[i].gameObject.activeSelf && nombre != null;
                visiblesNombres.Add(visible);
                centrosNombres.Add(destinos[i].rectTransform.anchoredPosition * zoom);
                arribaNombres.Add(catalogo[i].EtiquetaArriba);
                tamanosNombres.Add(visible ? new Vector2(nombre.preferredWidth + 6f, nombre.fontSize * 1.25f) : Vector2.zero);
            }
            foreach (Text t in nombresBarrios)
            {
                fijosNombres.Add(CalculoEtiquetasMapa.Caja(t.rectTransform.anchoredPosition * zoom, new Vector2(t.preferredWidth, t.fontSize * 1.2f)));
            }
            for (int i = 0; i < etiquetas.Length; i++)
            {
                if (etiquetas[i].text.Length > 0)
                {
                    fijosNombres.Add(CalculoEtiquetasMapa.Caja(manzanas[i].rectTransform.anchoredPosition * zoom, new Vector2(etiquetas[i].preferredWidth, etiquetas[i].preferredHeight)));
                }
            }
            // Las marcas de lugar (CIC, casa, I+D+I, mercado), el objetivo y el jugador tampoco se tapan.
            foreach (Image marca in marcas)
            {
                if (marca.gameObject.activeSelf)
                {
                    fijosNombres.Add(CalculoEtiquetasMapa.Caja(marca.rectTransform.anchoredPosition * zoom, Vector2.one * ladoIcono));
                }
            }
            if (romboObjetivo.gameObject.activeSelf)
            {
                fijosNombres.Add(CalculoEtiquetasMapa.Caja(romboObjetivo.anchoredPosition * zoom, Vector2.one * diagonalRombo));
            }
            fijosNombres.Add(CalculoEtiquetasMapa.Caja(puntoJugador.anchoredPosition * zoom, Vector2.one * ladoFlecha));
            Vector2[] lugares = CalculoEtiquetasMapa.Colocar(centrosNombres, tamanosNombres, arribaNombres, visiblesNombres, fijosNombres, ladoIcono);
            for (int i = 0; i < catalogo.Count; i++)
            {
                if (!visiblesNombres[i])
                {
                    continue;
                }
                IconosMapa.NombreDe(destinos[i]).rectTransform.anchoredPosition = lugares[i];
                nombresColocados[i] = CalculoEtiquetasMapa.Caja(centrosNombres[i] + lugares[i], tamanosNombres[i]);
            }
        }

        /// <summary>El rectángulo del nombre del lugar en la vista (px desde el centro del dibujo, con el zoom de ahora), o null si no se dibuja (T-721).</summary>
        public Rect? NombreDeLugar(IdDestinoMapa id)
        {
            for (int i = 0; i < destinos.Count; i++)
            {
                if (CatalogoDestinosMapa.Todos[i].Id == id && destinos[i].gameObject.activeSelf)
                {
                    return nombresColocados[i];
                }
            }
            return null;
        }

        /// <summary>El icono del lugar del catálogo si sale ahora en el mapa grande (T-667), o null.</summary>
        public Image IconoDestino(IdDestinoMapa id)
        {
            for (int i = 0; i < destinos.Count; i++)
            {
                if (CatalogoDestinosMapa.Todos[i].Id == id)
                {
                    return destinos[i].gameObject.activeSelf ? destinos[i] : null;
                }
            }
            return null;
        }

        // T-668: la ruta GPS naranja hasta lo que marcaste (o, si no hay nada marcado, hasta el objetivo), por las calles. Se recalcula al moverte.
        private void ActualizarRuta(Vector3 contraZoom)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 destino = PuntoMarcadoMapa.Posicion;
            bool hayDestino = PuntoMarcadoMapa.Hay;
            if (!hayDestino)
            {
                IObjetivos objetivos = Registro<IObjetivos>.Actual;
                hayDestino = objetivos != null && objetivos.HayObjetivo && UbicacionObjetivo.Resolver(objetivos.Actual, Registro<ILugar>.Todos, out destino);
            }
            bool hayRuta = hayDestino && jugador != null && jugador.Raiz != null;
            if (!hayRuta)
            {
                ruta.Clear();
                rutaCalculada = false;
            }
            else
            {
                Vector3 desde = jugador.Raiz.position;
                if (!rutaCalculada || CalculoMinimapa.DistanciaPlana(desde, origenRuta) > MetrosParaRecalcularRuta
                    || CalculoMinimapa.DistanciaPlana(destino, destinoRuta) > 0.5f)
                {
                    ruta = CalculoRutaGps.Ruta(desde, destino);
                    origenRuta = desde;
                    destinoRuta = destino;
                    rutaCalculada = true;
                }
            }
            int tramos = Mathf.Max(0, ruta.Count - 1);
            while (segmentosRuta.Count < tramos)
            {
                Image seg = KitUI.CrearImagen(capaRuta, "Tramo_" + segmentosRuta.Count, ColorRuta);
                KitUI.Colocar(seg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one);
                seg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                segmentosRuta.Add(seg);
            }
            float grosor = GrosorRutaPx / zoom;
            for (int i = 0; i < segmentosRuta.Count; i++)
            {
                bool usar = i < tramos;
                if (segmentosRuta[i].gameObject.activeSelf != usar)
                {
                    segmentosRuta[i].gameObject.SetActive(usar);
                }
                if (!usar)
                {
                    continue;
                }
                Vector2 a = GeometriaBarrio.AMapa(ruta[i]) * pixelesPorMetro;
                Vector2 b = GeometriaBarrio.AMapa(ruta[i + 1]) * pixelesPorMetro;
                Vector2 d = b - a;
                RectTransform rect = segmentosRuta[i].rectTransform;
                rect.anchoredPosition = (a + b) * 0.5f;
                rect.sizeDelta = new Vector2(d.magnitude + grosor * 0.5f, grosor);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            SegmentosRutaVisibles = tramos;
        }

        // Enemigos, jefes y lugares: siempre en el mapa mientras estén visibles.
        private void DibujarMarcas(Vector3 contraZoom)
        {
            IReadOnlyList<IMarcaMapa> todas = Registro<IMarcaMapa>.Todos;
            for (int i = 0; i < todas.Count; i++)
            {
                IMarcaMapa marca = todas[i];
                if (i == marcas.Count)
                {
                    marcas.Add(IconosMapa.Crear(contenido, marca.Tipo, ladoIcono));
                    tiposMarcas.Add(marca.Tipo);
                }
                else if (tiposMarcas[i] != marca.Tipo)
                {
                    Destroy(marcas[i].gameObject);
                    marcas[i] = IconosMapa.Crear(contenido, marca.Tipo, ladoIcono);
                    tiposMarcas[i] = marca.Tipo;
                }
                // T-934: el I+D+I lo dibuja siempre su destino del catálogo, con icono y nombre.
                bool verMarca = marca.Visible && marca.Tipo != TipoMarcaMapa.Clinica;
                if (marcas[i].gameObject.activeSelf != verMarca)
                {
                    marcas[i].gameObject.SetActive(verMarca);
                }
                RectTransform rect = marcas[i].rectTransform;
                rect.anchoredPosition = GeometriaBarrio.AMapa(marca.Posicion) * pixelesPorMetro;
                rect.localScale = contraZoom;
            }
            for (int i = todas.Count; i < marcas.Count; i++)
            {
                marcas[i].gameObject.SetActive(false);
            }
        }
    }
}
