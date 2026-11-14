using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-992: la barra de las tres habilidades de prótesis, abajo al centro del HUD H10 (sobre el aviso del tutorial, entre los objetos y el arma).
    /// Cada casilla lleva la tecla real (Input System, <see cref="TeclasHud"/>), el nombre y un medidor. Lista: turquesa y llena; activa: brilla y se
    /// vacía; recargando: rosa y se llena; sin implante: gris con "—". Además publica los avisos "PIERNAS ACTIVAS" y "NECESITAS EXO DE PIERNAS" en la
    /// cola de avisos. Solo se ve a pie. Lo agrega <see cref="HudAPie"/> al nacer; su canvas es del HUD, así que la rueda y los menús lo apagan.
    /// </summary>
    public class BarraHabilidades : MonoBehaviour
    {
        public const int OrdenCanvas = 12;
        public const float Lado = 96f;
        public const float Hueco = 14f;
        public const float MargenAbajo = 96f;
        private const float AltoMedidor = 8f;
        private const float SegundosAviso = 1.6f;
        private const float SegundosTeclas = 1f;
        private const string Apagada = "—";

        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color TurquesaSuave = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color Rosa = new Color(0.93f, 0.40f, 0.46f, 1f);
        private static readonly Color Gris = new Color(0.55f, 0.60f, 0.62f, 1f);
        private static readonly Color FondoCasilla = new Color(0.02f, 0.07f, 0.11f, 0.38f);
        private static readonly Color FondoActiva = new Color(0.10f, 0.36f, 0.38f, 0.75f);

        private sealed class Casilla
        {
            public Image Fondo;
            public Image[] Borde;
            public Text Tecla;
            public Text Nombre;
            public RectTransform Relleno;
            public Image RellenoImagen;
            public Color Color;
            public string TextoNombre = string.Empty;
        }

        private readonly Casilla[] casillas = new Casilla[3];
        private GameObject grupo;
        private HabilidadesProtesis habilidades;
        private Font fuente;
        private float siguienteTecla;

        /// <summary>True si la barra se está dibujando.</summary>
        public bool Visible => grupo != null && grupo.activeSelf;

        /// <summary>La tecla que se ve en la casilla.</summary>
        public string TeclaMostrada(HabilidadProtesis h) => casillas[(int)h].Tecla.text;

        /// <summary>El nombre que se ve en la casilla ("—" si no hay implante).</summary>
        public string NombreMostrado(HabilidadProtesis h) => casillas[(int)h].Nombre.text;

        /// <summary>El color con que se pinta la casilla (turquesa lista, rosa recargando, gris sin implante).</summary>
        public Color ColorMostrado(HabilidadProtesis h) => casillas[(int)h].Color;

        /// <summary>Cuánto del medidor se ve (0 a 1).</summary>
        public float MedidorMostrado(HabilidadProtesis h) => casillas[(int)h].Relleno.anchorMax.x;

        public static string NombreDe(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return "PIERNAS";
                case HabilidadProtesis.Brazos: return "BRAZOS";
                default: return "CABEZA";
            }
        }

        /// <summary>Lo que falta cuando se pulsa la tecla sin el implante.</summary>
        public static string AvisoSinImplante(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return "NECESITAS EXO DE PIERNAS";
                case HabilidadProtesis.Brazos: return "NECESITAS EXO DE BRAZOS";
                default: return "NECESITAS EL VISOR";
            }
        }

        public static string AvisoActivada(HabilidadProtesis h) => NombreDe(h) + " ACTIVAS";

        private static string TeclaPorOmision(HabilidadProtesis h) => ((int)h + 1).ToString();

        private void Awake()
        {
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            fuente = cfg != null && cfg.Fuente != null ? cfg.Fuente : KitUI.Estilo.Fuente;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasHabilidades", OrdenCanvas);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            grupo = new GameObject("Habilidades", typeof(RectTransform));
            grupo.transform.SetParent(canvas.transform, false);
            KitUI.Estirar((RectTransform)grupo.transform);
            float ancho = 3f * Lado + 2f * Hueco;
            for (int i = 0; i < casillas.Length; i++)
            {
                casillas[i] = Armar(i, -ancho * 0.5f + i * (Lado + Hueco));
            }
            grupo.SetActive(false);
        }

        private Casilla Armar(int indice, float x)
        {
            Casilla c = new Casilla();
            RectTransform raiz = Nuevo(grupo.transform, "Habilidad" + indice, new Vector2(0.5f, 0f), new Vector2(x, MargenAbajo), new Vector2(Lado, Lado));
            raiz.pivot = new Vector2(0f, 0f);
            raiz.anchoredPosition = new Vector2(x, MargenAbajo);
            c.Fondo = raiz.gameObject.AddComponent<Image>();
            c.Fondo.raycastTarget = false;
            GameObject borde = KitUI.CrearBorde(raiz, 2f, Color.white);
            c.Borde = borde.GetComponentsInChildren<Image>();
            c.Tecla = Texto(raiz, "Tecla", new Vector2(0f, 1f), new Vector2(8f, -4f), new Vector2(40f, 28f), 24, TextAnchor.MiddleLeft);
            c.Nombre = Texto(raiz, "Nombre", new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(Lado, 40f), 22, TextAnchor.MiddleCenter);
            c.Nombre.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            RectTransform pista = Nuevo(raiz, "Medidor", new Vector2(0f, 0f), new Vector2(8f, 8f), new Vector2(Lado - 16f, AltoMedidor));
            Image fondoMedidor = pista.gameObject.AddComponent<Image>();
            fondoMedidor.color = new Color(0f, 0f, 0f, 0.5f);
            fondoMedidor.raycastTarget = false;
            GameObject relleno = new GameObject("Relleno", typeof(RectTransform));
            relleno.transform.SetParent(pista, false);
            c.Relleno = (RectTransform)relleno.transform;
            c.Relleno.anchorMin = Vector2.zero;
            c.Relleno.anchorMax = Vector2.one;
            c.Relleno.offsetMin = Vector2.zero;
            c.Relleno.offsetMax = Vector2.zero;
            c.RellenoImagen = relleno.AddComponent<Image>();
            c.RellenoImagen.raycastTarget = false;
            return c;
        }

        private static RectTransform Nuevo(Transform padre, string nombre, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform));
            g.transform.SetParent(padre, false);
            RectTransform r = (RectTransform)g.transform;
            KitUI.Colocar(r, ancla, posicion, tamano);
            return r;
        }

        private Text Texto(Transform padre, string nombre, Vector2 ancla, Vector2 posicion, Vector2 tamano, int letra, TextAnchor alineacion)
        {
            RectTransform r = Nuevo(padre, nombre, ancla, posicion, tamano);
            Text t = r.gameObject.AddComponent<Text>();
            t.font = fuente;
            t.fontSize = letra;
            t.alignment = alineacion;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private void OnDisable()
        {
            Desenganchar();
        }

        private void Desenganchar()
        {
            if (habilidades != null)
            {
                habilidades.Activada -= AlActivar;
                habilidades.SinImplante -= AlFaltarImplante;
                habilidades = null;
            }
        }

        private void AlActivar(HabilidadProtesis h) => ColaAvisos.Publicar(AvisoActivada(h), PrioridadAviso.Normal, TipoAviso.Bueno, SegundosAviso);

        private void AlFaltarImplante(HabilidadProtesis h) => ColaAvisos.Publicar(AvisoSinImplante(h), PrioridadAviso.Normal, TipoAviso.Malo, SegundosAviso, "habilidad-sin-implante");

        private void Update()
        {
            HabilidadesProtesis actual = HabilidadesProtesis.Actual;
            if (actual != habilidades)
            {
                Desenganchar();
                habilidades = actual;
                if (habilidades != null)
                {
                    habilidades.Activada += AlActivar;
                    habilidades.SinImplante += AlFaltarImplante;
                }
            }
            IJugador jugador = Registro<IJugador>.Actual;
            bool ver = habilidades != null && jugador != null && jugador.Modo == ModoJugador.APie;
            if (grupo.activeSelf != ver)
            {
                grupo.SetActive(ver);
            }
            if (!ver)
            {
                return;
            }
            for (int i = 0; i < casillas.Length; i++)
            {
                Pintar((HabilidadProtesis)i, casillas[i]);
            }
        }

        private void Pintar(HabilidadProtesis h, Casilla c)
        {
            bool tiene = habilidades.Nivel(h) > 0;
            EstadoHabilidad estado = habilidades.Estado(h);
            Color color = !tiene ? Gris : (estado == EstadoHabilidad.Recargando ? Rosa : Turquesa);
            bool activa = estado == EstadoHabilidad.Activa;
            c.Color = color;
            c.Fondo.color = activa ? FondoActiva : FondoCasilla;
            Color borde = new Color(color.r, color.g, color.b, activa ? 1f : (tiene ? 0.85f : 0.4f));
            foreach (Image tira in c.Borde)
            {
                tira.color = borde;
            }
            c.Tecla.color = new Color(color.r, color.g, color.b, tiene ? 1f : 0.6f);
            c.Nombre.color = activa ? Color.white : (tiene ? TurquesaSuave : Gris);
            if (c.Tecla.text.Length == 0 || Time.unscaledTime >= siguienteTecla)
            {
                c.Tecla.text = TeclasHud.Texto(TeclasHud.Buscar(ConfigHabilidadesProtesisNombre(h)), TeclaPorOmision(h));
                if (h == HabilidadProtesis.Cabeza)
                {
                    siguienteTecla = Time.unscaledTime + SegundosTeclas;
                }
            }
            string nombre = tiene ? NombreDe(h) : Apagada;
            if (c.Nombre.text != nombre)
            {
                c.Nombre.text = nombre;
            }
            float medidor = tiene ? habilidades.Medidor(h) : 0f;
            c.Relleno.anchorMax = new Vector2(Mathf.Clamp01(medidor), 1f);
            c.RellenoImagen.color = activa ? Color.white : color;
        }

        private static string ConfigHabilidadesProtesisNombre(HabilidadProtesis h) => MechaCyberpunk.Personaje.ConfigHabilidadesProtesis.NombreAccion(h);
    }
}
