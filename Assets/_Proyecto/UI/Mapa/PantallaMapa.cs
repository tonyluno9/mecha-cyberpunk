using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El mapa del barrio con M (T-404, D-tony-220; T-409 con el kit): vista desde arriba con las 6 manzanas del color del dueño de su zona,
    /// su nombre y respeto, tú como flecha blanca, tus aliados, los enemigos, el objetivo y los iconos de El Mercado, I+D+I, el CIC y la casa.
    /// La rueda hace zoom de 1x a 3x centrado en el cursor; un clic izquierdo marca un punto (que sale en el minimapa) y otro clic sobre él lo
    /// quita. A la derecha va la leyenda. M o Esc lo cierran. Mientras está abierto el jugador a pie no se mueve ni dispara y el cursor se ve.
    /// </summary>
    public class PantallaMapa : MonoBehaviour
    {
        /// <summary>Título del mapa (documento del juego, 29-oct).</summary>
        public const string NombreCiudad = "MEGALOPOLIS";
        public const string TextoAyuda = "M O ESC PARA CERRAR · RUEDA: ZOOM · CLIC: MARCAR UN PUNTO";
        public const int OrdenCanvas = 40;
        public const float ZoomMinimo = 1f;
        public const float ZoomMaximo = 3f;
        public const string SonidoAbrir = "UI_MAPA_ABRIR";
        public const string SonidoCerrar = "UI_MAPA_CERRAR";
        public static readonly string[] EtiquetasFijasLeyenda =
            { "SKULLS", "SNAKES", "TÚ", "ALIADOS", "ENEMIGOS", "OBJETIVO", "EL MERCADO", "I+D+I", "CIC", "CASA", "PUNTO MARCADO" };

        [Tooltip("Píxeles de pantalla (referencia 1920x1080) por metro del mundo, con zoom 1.")]
        [Min(0.1f)] [SerializeField] private float pixelesPorMetro = 2.6f;
        [Tooltip("Tamaño de la vista del mapa (px).")]
        [SerializeField] private Vector2 tamanoVista = new Vector2(800f, 560f);
        [Tooltip("Ancho de la leyenda (px).")]
        [Min(100f)] [SerializeField] private float anchoLeyenda = 340f;
        [Tooltip("Alto de cada fila de la leyenda (px).")]
        [Min(20f)] [SerializeField] private float altoFilaLeyenda = 40f;
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
        private readonly List<Image> puntosAliados = new List<Image>();
        private readonly List<Image> marcas = new List<Image>();
        private readonly List<TipoMarcaMapa> tiposMarcas = new List<TipoMarcaMapa>();
        private Image[] manzanas;
        private Text[] etiquetas;
        private Text titulo;
        private RectTransform romboObjetivo;
        private Image puntoMarcado;
        private GameObject fondoPantalla;
        private ControladorAPie controlCongelado;
        private Text nombrePandilla;
        private Image colorPandilla;
        private Image colorAliados;
        private readonly List<Text> textosLeyenda = new List<Text>();
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
        public float Zoom => zoom;
        public RectTransform Vista => vista;
        public RectTransform Contenido => contenido;

        /// <summary>El rombo del objetivo (T-408), en px del dibujo a zoom 1 (x, z del mundo por píxeles por metro).</summary>
        public RectTransform RomboObjetivo => romboObjetivo;
        public bool RomboObjetivoVisible => romboObjetivo != null && romboObjetivo.gameObject.activeSelf;
        public float PixelesPorMetro => pixelesPorMetro;
        public RectTransform PuntoMarcado => puntoMarcado != null ? puntoMarcado.rectTransform : null;

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
            EstiloCyberfix e = KitUI.Estilo;
            float relleno = e.Relleno;
            float altoTitulo = e.Tamano(RolTexto.Encabezado) + relleno;
            float altoAyuda = e.AltoFilaHud;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasMapa", OrdenCanvas);
            fondoPantalla = KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla").gameObject;
            fondoPantalla.SetActive(false);
            Vector2 tamanoPanel = new Vector2(tamanoVista.x + anchoLeyenda + 3f * relleno, tamanoVista.y + altoTitulo + altoAyuda + 4f * relleno);
            RectTransform rectPanel = KitUI.CrearPanel(canvas.transform, "Panel", tamanoPanel);
            panel = rectPanel.gameObject;

            titulo = KitUI.CrearTexto(rectPanel, "Titulo", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -relleno), new Vector2(tamanoPanel.x, altoTitulo));
            titulo.text = NombreCiudad;
            titulo.color = e.Naranja;

            // La vista recorta el dibujo con zoom.
            vista = KitUI.CrearImagen(rectPanel, "Vista", Color.black).rectTransform;
            KitUI.Colocar(vista, new Vector2(0f, 1f), new Vector2(relleno, -(relleno + altoTitulo)), tamanoVista);
            vista.gameObject.AddComponent<RectMask2D>();
            contenido = new GameObject("Contenido", typeof(RectTransform)).GetComponent<RectTransform>();
            contenido.SetParent(vista, false);
            KitUI.Colocar(contenido, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            contenido.pivot = new Vector2(0.5f, 0.5f);

            Image calles = KitUI.CrearImagen(contenido, "Calles", e.Calles);
            KitUI.Colocar(calles.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, GeometriaBarrio.TamanoConCalles * pixelesPorMetro);
            float lado = GeometriaBarrio.LadoManzana * pixelesPorMetro;
            manzanas = new Image[GeometriaBarrio.Cantidad];
            etiquetas = new Text[GeometriaBarrio.Cantidad];
            for (int i = 0; i < manzanas.Length; i++)
            {
                manzanas[i] = KitUI.CrearImagen(contenido, "Manzana_" + (i + 1), Color.gray);
                KitUI.Colocar(manzanas[i].rectTransform, new Vector2(0.5f, 0.5f), GeometriaBarrio.Centros[i] * pixelesPorMetro, Vector2.one * lado);
                etiquetas[i] = KitUI.CrearTexto(manzanas[i].transform, "Etiqueta", RolTexto.Ayuda, TextAnchor.MiddleCenter);
                etiquetas[i].color = Color.black;
                etiquetas[i].GetComponent<Shadow>().enabled = false;
            }

            puntoMarcado = IconosMapa.CrearPuntoMarcado(contenido, ladoIcono);
            puntoMarcado.gameObject.SetActive(false);
            romboObjetivo = (RectTransform)KitUI.CrearRombo(contenido, "Objetivo", diagonalRombo, e.Naranja).rectTransform.parent;
            romboObjetivo.gameObject.SetActive(false);
            puntoJugador = (RectTransform)KitUI.CrearTriangulo(contenido, "Jugador", ladoFlecha, Color.white).rectTransform.parent;

            CrearLeyenda(rectPanel, new Vector2(-relleno, -(relleno + altoTitulo)));

            Text ayuda = KitUI.CrearTexto(rectPanel, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(tamanoPanel.x, altoAyuda));
            ayuda.text = TextoAyuda;
            ayuda.color = e.Turquesa;
            panel.SetActive(false);
        }

        // A la derecha: tu pandilla, SKULLS, SNAKES, tú, aliados, enemigos, objetivo, los cuatro lugares y el punto marcado.
        private void CrearLeyenda(RectTransform padre, Vector2 esquina)
        {
            EstiloCyberfix e = KitUI.Estilo;
            RectTransform leyenda = new GameObject("Leyenda", typeof(RectTransform)).GetComponent<RectTransform>();
            leyenda.SetParent(padre, false);
            KitUI.Colocar(leyenda, new Vector2(1f, 1f), esquina, new Vector2(anchoLeyenda, tamanoVista.y));
            float cuadro = altoFilaLeyenda * 0.6f;
            int fila = 0;
            colorPandilla = IconosMapa.Pieza(FilaVacia(leyenda, fila++, string.Empty, out nombrePandilla), "Cuadro", null, Color.white, cuadro);
            IconosMapa.Pieza(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[0], out _), "Cuadro", null, FormatoTerritorio.ColorDe(IdPandilla.Calaveras), cuadro);
            IconosMapa.Pieza(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[1], out _), "Cuadro", null, FormatoTerritorio.ColorDe(IdPandilla.Viboras), cuadro);
            KitUI.CrearTriangulo(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[2], out _), "Muestra", ladoFlecha * 0.8f, Color.white);
            colorAliados = IconosMapa.CrearAliado(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[3], out _), ladoIcono, FormatoTerritorio.ColorDe(IdPandilla.Jugador));
            IconosMapa.Crear(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[4], out _), TipoMarcaMapa.Enemigo, ladoIcono);
            KitUI.CrearRombo(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[5], out _), "Muestra", ladoIcono * 0.8f, e.Naranja);
            TipoMarcaMapa[] lugares = { TipoMarcaMapa.Mercado, TipoMarcaMapa.Clinica, TipoMarcaMapa.Cic, TipoMarcaMapa.Casa };
            for (int i = 0; i < lugares.Length; i++)
            {
                IconosMapa.Crear(FilaVacia(leyenda, fila++, EtiquetasFijasLeyenda[6 + i], out _), lugares[i], ladoIcono * 0.85f);
            }
            IconosMapa.CrearPuntoMarcado(FilaVacia(leyenda, fila, EtiquetasFijasLeyenda[10], out _), ladoIcono * 0.8f);
        }

        // Una fila con el hueco de la muestra (a la izquierda) y su texto; devuelve el hueco.
        private Transform FilaVacia(RectTransform leyenda, int fila, string etiqueta, out Text texto)
        {
            RectTransform rect = new GameObject("Fila_" + fila, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(leyenda, false);
            KitUI.Colocar(rect, new Vector2(0f, 1f), new Vector2(0f, -fila * altoFilaLeyenda), new Vector2(anchoLeyenda, altoFilaLeyenda));
            RectTransform hueco = new GameObject("Muestra", typeof(RectTransform)).GetComponent<RectTransform>();
            hueco.SetParent(rect, false);
            KitUI.Colocar(hueco, new Vector2(0f, 0.5f), new Vector2(altoFilaLeyenda * 0.5f, 0f), Vector2.zero);
            hueco.pivot = new Vector2(0.5f, 0.5f);
            texto = KitUI.CrearTexto(rect, "Texto", RolTexto.Ayuda, TextAnchor.MiddleLeft);
            texto.rectTransform.offsetMin = new Vector2(altoFilaLeyenda + KitUI.Estilo.Relleno * 0.5f, 0f);
            texto.text = etiqueta;
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
                if (teclado.mKey.wasPressedThisFrame)
                {
                    Abrir();
                }
                return;
            }
            if (teclado.mKey.wasPressedThisFrame || teclado.escapeKey.wasPressedThisFrame)
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
                etiquetas[i].text = zona.Length > 0 ? territorio.Nombre(zona) + "\nRESPETO " + territorio.Respeto(zona) : string.Empty;
            }
            Color tuColor = FormatoTerritorio.ColorDe(IdPandilla.Jugador);
            nombrePandilla.text = FormatoTerritorio.NombreDe(IdPandilla.Jugador);
            colorPandilla.color = tuColor;
            colorAliados.color = tuColor;

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

            DibujarMarcas(contraZoom);
            DibujarAliados(tuColor, contraZoom);
            puntoMarcado.rectTransform.SetAsLastSibling();
            romboObjetivo.SetAsLastSibling();
            puntoJugador.SetAsLastSibling();
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
                if (marcas[i].gameObject.activeSelf != marca.Visible)
                {
                    marcas[i].gameObject.SetActive(marca.Visible);
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

        // T-405: un punto por aliado, del color de tu pandilla.
        private void DibujarAliados(Color color, Vector3 contraZoom)
        {
            IPandillaJugador pandilla = Registro<IPandillaJugador>.Actual;
            int cantidad = pandilla != null ? pandilla.Aliados.Count : 0;
            while (puntosAliados.Count < cantidad)
            {
                puntosAliados.Add(IconosMapa.CrearAliado(contenido, ladoIcono, color));
            }
            for (int i = 0; i < puntosAliados.Count; i++)
            {
                bool hay = i < cantidad && pandilla.Aliados[i] != null;
                puntosAliados[i].gameObject.SetActive(hay);
                if (hay)
                {
                    puntosAliados[i].color = color;
                    puntosAliados[i].rectTransform.anchoredPosition = GeometriaBarrio.AMapa(pandilla.Aliados[i].position) * pixelesPorMetro;
                    puntosAliados[i].rectTransform.localScale = contraZoom;
                }
            }
        }
    }
}
