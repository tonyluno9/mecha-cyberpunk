using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El minimapa (T-409, canvas propio de orden 25). Desde T-700 es CUADRADO, de 260 px, arriba a la derecha como H10_HUD.png, con borde turquesa
    /// fino, la ruta punteada al objetivo y el cono de vista del jugador; muestra 60 m a la redonda y gira con la cámara (arriba es hacia donde miras). Calles en gris sobre negro,
    /// manzanas del color de su dueño al 55 %, tú como flecha blanca fija en el centro, el objetivo como rombo naranja (pegado al borde si está
    /// lejos), enemigos en rojo (el jefe más grande), aliados del color de tu pandilla, los iconos de los lugares y el punto marcado en el mapa
    /// grande con su distancia. Se esconde en el mecha y con el mapa grande abierto. Las cuentas están en <see cref="CalculoMinimapa"/>.
    /// </summary>
    public class Minimapa : MonoBehaviour
    {
        public const int OrdenCanvas = 25;
        /// <summary>Lado (px) del cono de vista del jugador (T-700): la punta queda en el centro y el cono sube.</summary>
        private const float LadoCono = 150f;
        private const float GrosorBordePx = 2f;

        [Tooltip("Metros del mundo del centro al borde del círculo (m).")]
        [Min(5f)] [SerializeField] private float radioMetros = 60f;
        // T-522: las manzanas solo se tiñen un poco del color del dueño (antes 55 %: una zona magenta se veía como un relleno plano) y el
        // fondo es azul muy oscuro, para que se lean las calles como en el mockup. Constantes: el valor viejo seguía guardado en la escena.
        private const float AlfaManzanas = 0.08f;
        // T-523: las calles claras (gris azulado con un toque de turquesa) y las manzanas casi negras, para que se lean las calles como en el mockup.
        private static readonly Color ColorCalles = new Color(0.50f, 0.85f, 0.86f, 0.20f);
        private const float AlfaFondo = 0.55f;
        private static readonly Color FondoOscuro = new Color(0.03f, 0.04f, 0.08f, 1f);
        [Tooltip("Lado de los iconos de lugar (px a 1920x1080).")]
        [Min(8f)] [SerializeField] private float ladoIcono = 30f;
        // T-523: el jugador y el objetivo, más grandes que antes (22 y 24) para verse sobre las calles claras; constantes por el valor viejo del prefab.
        private const float ladoFlecha = 28f;
        private const float diagonalRombo = 30f;
        [Tooltip("Lado del punto marcado (px).")]
        [Min(4f)] [SerializeField] private float ladoPunto = 22f;
        [Tooltip("Lo que se pega al borde queda a esta distancia de él, hacia dentro (px).")]
        [Min(0f)] [SerializeField] private float margenPegado = 12f;
        [Tooltip("Separación entre el punto marcado y su distancia (px).")]
        [Min(0f)] [SerializeField] private float separacionTexto = 16f;

        private Canvas canvas;
        private RectTransform raiz;
        private RectTransform mundo;
        private RectTransform desplazado;
        private RectTransform capaMarcas;
        private RectTransform flecha;
        private RectTransform rombo;
        private Image punto;
        private Text textoPunto;
        private Image[] manzanas;
        private Image calles;
        // T-632: ruta tipo GPS por las calles hasta el objetivo; se recalcula al moverte más de MetrosParaRecalcular o si el objetivo cambia.
        private const float MetrosParaRecalcular = 2f;
        private const float GrosorRutaPx = 4f;
        // T-700: la ruta va punteada y turquesa clara, como en H10.
        private static readonly Color ColorRuta = new Color(0.62f, 1f, 0.97f, 0.95f);
        private RectTransform capaRuta;
        private CirculoBusquedaMapa circuloBusqueda;
        private readonly List<Image> segmentosRuta = new List<Image>();
        private List<Vector3> ruta = new List<Vector3>();
        private Vector3 origenRuta;
        private Vector3 destinoRuta;
        private bool rutaCalculada;
        private int recalculosRuta;
        private readonly List<Image> marcas = new List<Image>();
        private readonly List<TipoMarcaMapa> tiposMarcas = new List<TipoMarcaMapa>();
        private readonly List<Image> aliados = new List<Image>();
        private readonly List<Image> destinos = new List<Image>();
        private int aliadosVisibles;
        private float escala;

        public bool Visible => canvas != null && canvas.enabled;
        public RectTransform Raiz => raiz;
        /// <summary>Lo que gira con la cámara (calles y manzanas). Su rotación en Z es el rumbo de la cámara.</summary>
        public RectTransform Mundo => mundo;
        public RectTransform Flecha => flecha;
        public RectTransform RomboObjetivo => rombo;
        public bool RomboVisible => rombo != null && rombo.gameObject.activeSelf;
        public RectTransform PuntoMarcado => punto != null ? punto.rectTransform : null;
        /// <summary>El círculo de búsqueda de la policía (T-665).</summary>
        public CirculoBusquedaMapa CirculoBusqueda => circuloBusqueda;
        public bool PuntoVisible => punto != null && punto.gameObject.activeSelf;
        public Text TextoPunto => textoPunto;
        public float RadioPx => DisposicionHud.LadoMinimapa * 0.5f;
        public float RadioMetros => radioMetros;
        /// <summary>Píxeles por metro.</summary>
        public float Escala => escala;
        public int AliadosVisibles => aliadosVisibles;
        /// <summary>Cuántos puntos de aliado hay creados (los que no salen están apagados).</summary>
        public int AliadosCreados => aliados.Count;
        public Image Aliado(int indice) => aliados[indice];
        /// <summary>Puntos de la ruta GPS (mundo), vacía si no hay objetivo.</summary>
        public IReadOnlyList<Vector3> Ruta => ruta;
        /// <summary>Cuántos tramos de la ruta se dibujan ahora.</summary>
        public int SegmentosRutaVisibles { get; private set; }
        /// <summary>Cuántas veces se calculó la ruta.</summary>
        public int RecalculosRuta => recalculosRuta;

        /// <summary>La marca dibujada de <paramref name="marca"/>, o null si no sale ahora.</summary>
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
            canvas = KitUI.CrearCanvas(transform, "CanvasMinimapa", OrdenCanvas);
            raiz = new GameObject("Minimapa", typeof(RectTransform)).GetComponent<RectTransform>();
            raiz.SetParent(canvas.transform, false);
            RanuraHud ranura = DisposicionHud.Minimapa;
            KitUI.Colocar(raiz, ranura, DisposicionHud.LadoMinimapa);

            Image mascara = KitUI.CrearImagen(raiz, "Mascara", new Color(FondoOscuro.r, FondoOscuro.g, FondoOscuro.b, AlfaFondo));
            // T-700: sin sprite la máscara es el cuadrado entero.
            KitUI.Estirar(mascara.rectTransform);
            mascara.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            mundo = Centrado(mascara.transform, "Mundo");
            desplazado = Centrado(mundo, "Desplazado");
            calles = KitUI.CrearImagen(desplazado, "Calles", ColorCalles);
            manzanas = new Image[GeometriaBarrio.Cantidad];
            for (int i = 0; i < manzanas.Length; i++)
            {
                manzanas[i] = KitUI.CrearImagen(desplazado, "Manzana_" + (i + 1), FondoOscuro);
            }

            circuloBusqueda = new CirculoBusquedaMapa(desplazado);
            capaRuta = Centrado(desplazado, "RutaGps");
            capaMarcas = Centrado(mascara.transform, "Marcas");
            rombo = (RectTransform)KitUI.CrearRombo(capaMarcas, "Objetivo", diagonalRombo, e.Naranja).rectTransform.parent;
            rombo.gameObject.SetActive(false);
            punto = IconosMapa.CrearPuntoMarcado(capaMarcas, ladoPunto);
            punto.gameObject.SetActive(false);

            // T-700: el cono de vista (la punta en el centro, hacia donde miras) bajo la flecha del jugador, y el borde fino cuadrado.
            ConfigHudH10 estilo = ConfigHudH10.Actual;
            if (estilo != null && estilo.ConoMinimapa != null)
            {
                Image cono = KitUI.CrearImagenSprite(raiz, "ConoVista", estilo.ConoMinimapa, false);
                KitUI.Colocar(cono.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(LadoCono, LadoCono));
                cono.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                cono.color = new Color(e.Turquesa.r, e.Turquesa.g, e.Turquesa.b, 0.9f);
                cono.raycastTarget = false;
            }
            KitUI.CrearBorde(raiz, GrosorBordePx, new Color(e.Turquesa.r, e.Turquesa.g, e.Turquesa.b, 0.9f));
            flecha = (RectTransform)KitUI.CrearTriangulo(raiz, "Jugador", ladoFlecha, Color.white).rectTransform.parent;
            textoPunto = KitUI.CrearTexto(raiz, "DistanciaPunto", RolTexto.Hud, TextAnchor.MiddleLeft);
            KitUI.Colocar(textoPunto.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(DisposicionHud.LadoMinimapa, KitUI.Estilo.AltoFilaHud));
            textoPunto.rectTransform.pivot = new Vector2(0f, 0.5f);
            textoPunto.color = e.Turquesa;
            textoPunto.gameObject.SetActive(false);
            canvas.enabled = false;
        }

        private static RectTransform Centrado(Transform padre, string nombre)
        {
            RectTransform rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(padre, false);
            KitUI.Colocar(rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private void LateUpdate() => Actualizar();

        /// <summary>Redibuja con lo de ahora (lo llama LateUpdate; las pruebas pueden llamarlo para no esperar un cuadro).</summary>
        public void Actualizar()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            // En el mecha no va, y con el mapa grande abierto sobra.
            bool ver = jugador != null && jugador.Raiz != null && jugador.Modo != ModoJugador.Mecha && !PantallaMapa.HayAbierto;
            if (canvas.enabled != ver)
            {
                canvas.enabled = ver;
            }
            if (!ver)
            {
                return;
            }
            Vector3 centro = jugador.Raiz.position;
            Transform vista = jugador.Camara != null ? jugador.Camara.transform : jugador.Raiz;
            float rumbo = vista.eulerAngles.y;
            float radio = RadioPx;
            float radioPegado = radio - margenPegado;
            escala = CalculoMinimapa.Escala(radio, radioMetros, 1f);

            mundo.localRotation = Quaternion.Euler(0f, 0f, rumbo);
            desplazado.anchoredPosition = -GeometriaBarrio.AMapa(centro) * escala;
            calles.rectTransform.sizeDelta = GeometriaBarrio.TamanoConCalles * escala;
            PintarManzanas();
            circuloBusqueda.Actualizar(escala);

            // El objetivo (rombo naranja), pegado al borde si está lejos.
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            Vector3 destino = Vector3.zero;
            bool hayObjetivo = objetivos != null && objetivos.HayObjetivo
                && UbicacionObjetivo.Resolver(objetivos.Actual, Registro<ILugar>.Todos, out destino);
            Vector2 px = Vector2.zero;
            hayObjetivo = hayObjetivo && CalculoMinimapa.Ubicar(centro, rumbo, destino, escala, radioPegado, true, out px);
            Mostrar(rombo.gameObject, hayObjetivo);
            rombo.anchoredPosition = px;
            // T-668: lo que marcaste manda sobre el objetivo de la misión; la ruta GPS va a uno o a otro. Al llegar a lo marcado se quita.
            if (PuntoMarcadoMapa.Llego(centro))
            {
                PuntoMarcadoMapa.Quitar();
            }
            ActualizarRuta(centro, PuntoMarcadoMapa.Hay || hayObjetivo, PuntoMarcadoMapa.Hay ? PuntoMarcadoMapa.Posicion : destino);

            // El punto marcado en el mapa grande, también pegado al borde, con su distancia.
            bool hayPunto = PuntoMarcadoMapa.Hay
                && CalculoMinimapa.Ubicar(centro, rumbo, PuntoMarcadoMapa.Posicion, escala, radioPegado, true, out px);
            Mostrar(punto.gameObject, hayPunto);
            Mostrar(textoPunto.gameObject, hayPunto);
            if (hayPunto)
            {
                punto.rectTransform.anchoredPosition = px;
                textoPunto.rectTransform.anchoredPosition = px + Vector2.right * separacionTexto;
                textoPunto.text = CalculoFlechaObjetivo.TextoDistancia(CalculoMinimapa.DistanciaPlana(centro, PuntoMarcadoMapa.Posicion));
            }

            DibujarMarcas(centro, rumbo, radio);
            DibujarDestinos(centro, rumbo, radio);
            DibujarAliados(centro, rumbo, radio);
            rombo.SetAsLastSibling();
            punto.rectTransform.SetAsLastSibling();
        }

        private void ActualizarRuta(Vector3 centro, bool hayObjetivo, Vector3 destino)
        {
            if (!hayObjetivo)
            {
                rutaCalculada = false;
                ruta.Clear();
            }
            else if (!rutaCalculada || CalculoMinimapa.DistanciaPlana(centro, origenRuta) > MetrosParaRecalcular
                || CalculoMinimapa.DistanciaPlana(destino, destinoRuta) > 0.5f)
            {
                ruta = CalculoRutaGps.Ruta(centro, destino);
                origenRuta = centro;
                destinoRuta = destino;
                rutaCalculada = true;
                recalculosRuta++;
            }
            int tramos = Mathf.Max(0, ruta.Count - 1);
            while (segmentosRuta.Count < tramos)
            {
                Image seg = KitUI.CrearImagen(capaRuta, "Tramo_" + segmentosRuta.Count, ColorRuta);
                ConfigHudH10 estilo = ConfigHudH10.Actual;
                if (estilo != null && estilo.PuntoRuta != null)
                {
                    // Punteado: el sprite de un punto se repite a lo largo del tramo.
                    seg.sprite = estilo.PuntoRuta;
                    seg.type = Image.Type.Tiled;
                }
                KitUI.Colocar(seg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one);
                seg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                segmentosRuta.Add(seg);
            }
            for (int i = 0; i < segmentosRuta.Count; i++)
            {
                bool usar = i < tramos;
                Mostrar(segmentosRuta[i].gameObject, usar);
                if (!usar)
                {
                    continue;
                }
                Vector2 a = GeometriaBarrio.AMapa(ruta[i]) * escala;
                Vector2 b = GeometriaBarrio.AMapa(ruta[i + 1]) * escala;
                Vector2 d = b - a;
                RectTransform rect = segmentosRuta[i].rectTransform;
                rect.anchoredPosition = (a + b) * 0.5f;
                rect.sizeDelta = new Vector2(d.magnitude + GrosorRutaPx * 0.5f, GrosorRutaPx);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            SegmentosRutaVisibles = tramos;
        }

        private void PintarManzanas()
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            float lado = GeometriaBarrio.LadoManzana * escala;
            for (int i = 0; i < manzanas.Length; i++)
            {
                string zona = GeometriaBarrio.Zona(i, territorio);
                Color dueno = zona.Length > 0 ? FormatoTerritorio.ColorDe(territorio.Dueno(zona)) : KitUI.Estilo.Desactivado;
                manzanas[i].color = Color.Lerp(FondoOscuro, dueno, AlfaManzanas);
                RectTransform rect = manzanas[i].rectTransform;
                rect.anchoredPosition = GeometriaBarrio.Centros[i] * escala;
                rect.sizeDelta = Vector2.one * lado;
            }
        }

        // Enemigos, jefes y lugares: solo los que caen dentro del círculo.
        private void DibujarMarcas(Vector3 centro, float rumbo, float radio)
        {
            IReadOnlyList<IMarcaMapa> todas = Registro<IMarcaMapa>.Todos;
            for (int i = 0; i < todas.Count; i++)
            {
                IMarcaMapa marca = todas[i];
                if (i == marcas.Count)
                {
                    marcas.Add(IconosMapa.Crear(capaMarcas, marca.Tipo, ladoIcono));
                    tiposMarcas.Add(marca.Tipo);
                }
                else if (tiposMarcas[i] != marca.Tipo)
                {
                    Destroy(marcas[i].gameObject);
                    marcas[i] = IconosMapa.Crear(capaMarcas, marca.Tipo, ladoIcono);
                    tiposMarcas[i] = marca.Tipo;
                }
                Vector2 px = Vector2.zero;
                bool dentro = marca.Visible && CalculoMinimapa.Ubicar(centro, rumbo, marca.Posicion, escala, radio, false, out px);
                Mostrar(marcas[i].gameObject, dentro);
                if (dentro)
                {
                    marcas[i].rectTransform.anchoredPosition = px;
                }
            }
            for (int i = todas.Count; i < marcas.Count; i++)
            {
                Mostrar(marcas[i].gameObject, false);
            }
        }

        // T-667: los lugares a los que se puede ir (casa, El Doc, Tino, mecánico, tianguis, casino, mercado, misiones), solo los que caen dentro del círculo.
        // Los que ya dibuja una marca de lugar de T-409 (casa, El Doc, mercado) no se repiten.
        private void DibujarDestinos(Vector3 centro, float rumbo, float radio)
        {
            IReadOnlyList<DestinoMapa> catalogo = CatalogoDestinosMapa.Todos;
            while (destinos.Count < catalogo.Count)
            {
                Image icono = IconosMapa.CrearDestino(capaMarcas, catalogo[destinos.Count], ladoIcono, false);
                icono.gameObject.SetActive(false);
                destinos.Add(icono);
            }
            for (int i = 0; i < catalogo.Count; i++)
            {
                Vector2 px = Vector2.zero;
                bool dentro = !CatalogoDestinosMapa.LoDibujaUnaMarca(catalogo[i].Id)
                    && CatalogoDestinosMapa.Posicion(catalogo[i].Id, out Vector3 donde)
                    && CalculoMinimapa.Ubicar(centro, rumbo, donde, escala, radio, false, out px);
                Mostrar(destinos[i].gameObject, dentro);
                if (dentro)
                {
                    destinos[i].rectTransform.anchoredPosition = px;
                }
            }
        }

        /// <summary>El icono del lugar del catálogo si sale ahora en el minimapa (T-667), o null.</summary>
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

        private void DibujarAliados(Vector3 centro, float rumbo, float radio)
        {
            IPandillaJugador pandilla = Registro<IPandillaJugador>.Actual;
            int cantidad = pandilla != null ? pandilla.Aliados.Count : 0;
            Color color = FormatoTerritorio.ColorDe(IdPandilla.Jugador);
            while (aliados.Count < cantidad)
            {
                aliados.Add(IconosMapa.CrearAliado(capaMarcas, ladoIcono, color));
            }
            aliadosVisibles = 0;
            for (int i = 0; i < aliados.Count; i++)
            {
                Vector2 px = Vector2.zero;
                Transform aliado = i < cantidad ? pandilla.Aliados[i] : null;
                bool dentro = aliado != null && CalculoMinimapa.Ubicar(centro, rumbo, aliado.position, escala, radio, false, out px);
                Mostrar(aliados[i].gameObject, dentro);
                if (dentro)
                {
                    aliados[i].color = color;
                    aliados[i].rectTransform.anchoredPosition = px;
                    aliadosVisibles++;
                }
            }
        }

        private static void Mostrar(GameObject objeto, bool ver)
        {
            if (objeto.activeSelf != ver)
            {
                objeto.SetActive(ver);
            }
        }
    }
}
