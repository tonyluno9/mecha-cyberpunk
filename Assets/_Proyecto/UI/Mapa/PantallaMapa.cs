using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El mapa del barrio con M (T-404, D-tony-220): vista desde arriba en el centro de la pantalla. Las 6 manzanas van pintadas del color del
    /// dueño de su zona, con el nombre y el respeto de la zona, y un punto blanco eres tú. M o Esc lo cierran. Mientras está abierto el
    /// jugador a pie no se mueve ni dispara (se le quita el control). Crea su propio canvas. Los aliados de T-405 irán como más puntos en <c>PuntosExtra</c>.
    /// </summary>
    public class PantallaMapa : MonoBehaviour
    {
        // Centros de las 6 manzanas del barrio (80 m de lado, paso 94 m), de oeste a este y de norte (arriba) a sur.
        private static readonly Vector2[] CentrosManzanas =
        {
            new Vector2(-94f, 47f), new Vector2(0f, 47f), new Vector2(94f, 47f),
            new Vector2(-94f, -47f), new Vector2(0f, -47f), new Vector2(94f, -47f),
        };

        /// <summary>Título del mapa (documento del juego, 29-oct).</summary>
        public const string NombreCiudad = "MEGALOPOLIS";
        public const string TextoAyuda = "M O ESC PARA CERRAR";
        public const int OrdenCanvas = 40;

        [Tooltip("Lado de cada manzana en el mapa (m del mundo).")]
        [Min(1f)] [SerializeField] private float ladoManzana = 80f;
        [Tooltip("Píxeles de pantalla (referencia 1920x1080) por metro del mundo.")]
        [Min(0.1f)] [SerializeField] private float pixelesPorMetro = 2.6f;
        [SerializeField] private Color colorJugador = Color.white;
        [Tooltip("Tamaño del rombo del objetivo de punta a punta (px a 1920x1080).")]
        [Min(4f)] [SerializeField] private float diagonalRombo = 36f;

        private GameObject panel;
        private RectTransform contenedor;
        private RectTransform puntoJugador;
        private readonly System.Collections.Generic.List<RectTransform> puntosAliados = new System.Collections.Generic.List<RectTransform>();
        private readonly System.Collections.Generic.List<Image> imagenesAliados = new System.Collections.Generic.List<Image>();
        private Image[] manzanas;
        private Text[] etiquetas;
        private Text titulo;
        private RectTransform romboObjetivo;
        private GameObject fondoPantalla;
        private ControladorAPie controlCongelado;

        public bool Abierto => panel != null && panel.activeSelf;
        public Image[] Manzanas => manzanas;
        public Text[] Etiquetas => etiquetas;
        public Text Titulo => titulo;

        /// <summary>El rombo del objetivo (T-408), en px del mapa desde su centro (x, z del mundo por píxeles por metro).</summary>
        public RectTransform RomboObjetivo => romboObjetivo;
        public bool RomboObjetivoVisible => romboObjetivo != null && romboObjetivo.gameObject.activeSelf;
        public float PixelesPorMetro => pixelesPorMetro;

        private void Awake()
        {
            // T-408: canvas, panel, textos y rombo del kit de UI.
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasMapa", OrdenCanvas);
            fondoPantalla = KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla").gameObject;
            fondoPantalla.SetActive(false);
            RectTransform rectPanel = KitUI.CrearPanel(canvas.transform, "Panel",
                new Vector2(3f * 94f * pixelesPorMetro + 120f, 2f * 94f * pixelesPorMetro + 160f));
            panel = rectPanel.gameObject;
            contenedor = rectPanel;
            manzanas = new Image[CentrosManzanas.Length];
            etiquetas = new Text[CentrosManzanas.Length];
            for (int i = 0; i < CentrosManzanas.Length; i++)
            {
                GameObject objeto = new GameObject("Manzana_" + (i + 1), typeof(RectTransform));
                objeto.transform.SetParent(contenedor, false);
                RectTransform rect = (RectTransform)objeto.transform;
                rect.sizeDelta = Vector2.one * ladoManzana * pixelesPorMetro;
                rect.anchoredPosition = CentrosManzanas[i] * pixelesPorMetro;
                manzanas[i] = objeto.AddComponent<Image>();
                manzanas[i].raycastTarget = false;
                etiquetas[i] = FormatoTerritorio.CrearTexto(objeto.transform, "Etiqueta", KitUI.Estilo.Tamano(RolTexto.Ayuda), TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ladoManzana * pixelesPorMetro, 60f));
                ((RectTransform)etiquetas[i].transform).pivot = new Vector2(0.5f, 0.5f);
                etiquetas[i].color = Color.black;
            }
            GameObject punto = new GameObject("Jugador", typeof(RectTransform));
            punto.transform.SetParent(contenedor, false);
            puntoJugador = (RectTransform)punto.transform;
            puntoJugador.sizeDelta = Vector2.one * 14f;
            Image imagenPunto = punto.AddComponent<Image>();
            imagenPunto.color = colorJugador;
            imagenPunto.raycastTarget = false;
            romboObjetivo = (RectTransform)KitUI.CrearRombo(contenedor, "Objetivo", diagonalRombo, KitUI.Estilo.Naranja).rectTransform.parent;
            romboObjetivo.gameObject.SetActive(false);
            float relleno = KitUI.Estilo.Relleno;
            titulo = FormatoTerritorio.CrearTexto(contenedor, "Titulo", KitUI.Estilo.Tamano(RolTexto.Texto), TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -relleno), new Vector2(600f, 40f));
            ((RectTransform)titulo.transform).pivot = new Vector2(0.5f, 1f);
            titulo.text = NombreCiudad;
            titulo.color = KitUI.Estilo.Naranja;
            Text ayuda = FormatoTerritorio.CrearTexto(contenedor, "Ayuda", KitUI.Estilo.Tamano(RolTexto.Ayuda), TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, relleno), new Vector2(600f, 30f));
            ((RectTransform)ayuda.transform).pivot = new Vector2(0.5f, 0f);
            ayuda.text = TextoAyuda;
            ayuda.color = KitUI.Estilo.Turquesa;
            panel.SetActive(false);
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
            Refrescar();
        }

        public void Abrir()
        {
            if (Abierto)
            {
                return;
            }
            panel.SetActive(true);
            fondoPantalla.SetActive(true);
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
            Refrescar();
        }

        public void Cerrar()
        {
            if (panel != null)
            {
                panel.SetActive(false);
                fondoPantalla.SetActive(false);
            }
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }

        private void Refrescar()
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            for (int i = 0; i < CentrosManzanas.Length; i++)
            {
                if (territorio == null)
                {
                    continue;
                }
                string zona = territorio.ZonaEn(new Vector3(CentrosManzanas[i].x, 0f, CentrosManzanas[i].y));
                manzanas[i].color = zona.Length > 0 ? FormatoTerritorio.ColorDe(territorio.Dueno(zona)) : Color.gray;
                etiquetas[i].text = zona.Length > 0 ? territorio.Nombre(zona) + "\nRESPETO " + territorio.Respeto(zona) : string.Empty;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null)
            {
                Vector3 p = jugador.Raiz.position;
                puntoJugador.anchoredPosition = new Vector2(p.x, p.z) * pixelesPorMetro;
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
            if (hayObjetivo)
            {
                romboObjetivo.anchoredPosition = new Vector2(destino.x, destino.z) * pixelesPorMetro;
                romboObjetivo.SetAsLastSibling();
            }
            // T-405: un punto por aliado, del color de tu pandilla.
            IPandillaJugador pandilla = Registro<IPandillaJugador>.Actual;
            int cantidad = pandilla != null ? pandilla.Aliados.Count : 0;
            while (puntosAliados.Count < cantidad)
            {
                GameObject nuevo = new GameObject("Aliado", typeof(RectTransform));
                nuevo.transform.SetParent(contenedor, false);
                RectTransform rect = (RectTransform)nuevo.transform;
                rect.sizeDelta = Vector2.one * 10f;
                Image imagen = nuevo.AddComponent<Image>();
                imagen.raycastTarget = false;
                puntosAliados.Add(rect);
                imagenesAliados.Add(imagen);
            }
            for (int i = 0; i < puntosAliados.Count; i++)
            {
                imagenesAliados[i].color = FormatoTerritorio.ColorDe(IdPandilla.Jugador);
                puntosAliados[i].gameObject.SetActive(i < cantidad);
                if (i < cantidad)
                {
                    Vector3 p = pandilla.Aliados[i].position;
                    puntosAliados[i].anchoredPosition = new Vector2(p.x, p.z) * pixelesPorMetro;
                }
            }
        }
    }
}
