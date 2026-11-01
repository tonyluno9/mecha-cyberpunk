using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El minimapa redondo abajo a la izquierda (T-409, canvas propio de orden 25): un círculo de 260 px a 48 px de la esquina, con máscara
    /// redonda y borde turquesa, que muestra 60 m a la redonda y gira con la cámara (arriba es hacia donde miras). Calles en gris sobre negro,
    /// manzanas del color de su dueño al 55 %, tú como flecha blanca fija en el centro, el objetivo como rombo naranja (pegado al borde si está
    /// lejos), enemigos en rojo (el jefe más grande), aliados del color de tu pandilla, los iconos de los lugares y el punto marcado en el mapa
    /// grande con su distancia. Se esconde en el mecha y con el mapa grande abierto. Las cuentas están en <see cref="CalculoMinimapa"/>.
    /// </summary>
    public class Minimapa : MonoBehaviour
    {
        public const int OrdenCanvas = 25;
        /// <summary>Lado (px) del anillo de UI11: su hueco mide 365 de 512, y se quiere de 232 px.</summary>
        private const float LadoAnillo = 325f;
        // El hueco del arte no está centrado en la imagen: se corrige para que quede sobre el mapa.
        private static readonly Vector2 DesplazamientoAnillo = new Vector2(-5.7f, -7.3f);

        [Tooltip("Metros del mundo del centro al borde del círculo (m).")]
        [Min(5f)] [SerializeField] private float radioMetros = 60f;
        // T-522: las manzanas solo se tiñen un poco del color del dueño (antes 55 %: una zona magenta se veía como un relleno plano) y el
        // fondo es azul muy oscuro, para que se lean las calles como en el mockup. Constantes: el valor viejo seguía guardado en la escena.
        private const float AlfaManzanas = 0.22f;
        private const float AlfaFondo = 0.9f;
        private static readonly Color FondoOscuro = new Color(0.03f, 0.04f, 0.08f, 1f);
        [Tooltip("Lado de los iconos de lugar (px a 1920x1080).")]
        [Min(8f)] [SerializeField] private float ladoIcono = 30f;
        [Tooltip("Lado de la flecha del jugador (px).")]
        [Min(4f)] [SerializeField] private float ladoFlecha = 22f;
        [Tooltip("Rombo del objetivo de punta a punta (px).")]
        [Min(4f)] [SerializeField] private float diagonalRombo = 24f;
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
        private readonly List<Image> marcas = new List<Image>();
        private readonly List<TipoMarcaMapa> tiposMarcas = new List<TipoMarcaMapa>();
        private readonly List<Image> aliados = new List<Image>();
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
            mascara.sprite = IconosMapa.Circulo;
            KitUI.Estirar(mascara.rectTransform);
            mascara.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            mundo = Centrado(mascara.transform, "Mundo");
            desplazado = Centrado(mundo, "Desplazado");
            calles = KitUI.CrearImagen(desplazado, "Calles", Color.Lerp(e.Calles, e.Turquesa, 0.3f));
            manzanas = new Image[GeometriaBarrio.Cantidad];
            for (int i = 0; i < manzanas.Length; i++)
            {
                manzanas[i] = KitUI.CrearImagen(desplazado, "Manzana_" + (i + 1), FondoOscuro);
            }

            capaMarcas = Centrado(mascara.transform, "Marcas");
            rombo = (RectTransform)KitUI.CrearRombo(capaMarcas, "Objetivo", diagonalRombo, e.Naranja).rectTransform.parent;
            rombo.gameObject.SetActive(false);
            punto = IconosMapa.CrearPuntoMarcado(capaMarcas, ladoPunto);
            punto.gameObject.SetActive(false);

            Image borde = KitUI.CrearImagen(raiz, "Borde", e.Turquesa);
            borde.sprite = IconosMapa.BordeMinimapa;
            KitUI.Estirar(borde.rectTransform);
            // T-521: el anillo de UI11 alrededor del círculo (su hueco, de 232 px, queda sobre el borde del mapa); el borde turquesa liso baja a un hilo.
            borde.color = new Color(e.Turquesa.r, e.Turquesa.g, e.Turquesa.b, 0.25f);
            Sprite anillo = PiezasUI.Actual != null ? PiezasUI.Actual.MarcoMinimapa : null;
            if (anillo != null)
            {
                Image ring = KitUI.CrearImagenSprite(raiz, "Anillo", anillo, false);
                KitUI.Colocar(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(DesplazamientoAnillo.x, DesplazamientoAnillo.y), new Vector2(LadoAnillo, LadoAnillo));
                ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
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

            // El objetivo (rombo naranja), pegado al borde si está lejos.
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            Vector3 destino = Vector3.zero;
            bool hayObjetivo = objetivos != null && objetivos.HayObjetivo
                && UbicacionObjetivo.Resolver(objetivos.Actual, Registro<ILugar>.Todos, out destino);
            Vector2 px = Vector2.zero;
            hayObjetivo = hayObjetivo && CalculoMinimapa.Ubicar(centro, rumbo, destino, escala, radioPegado, true, out px);
            Mostrar(rombo.gameObject, hayObjetivo);
            rombo.anchoredPosition = px;

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
            DibujarAliados(centro, rumbo, radio);
            rombo.SetAsLastSibling();
            punto.rectTransform.SetAsLastSibling();
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
