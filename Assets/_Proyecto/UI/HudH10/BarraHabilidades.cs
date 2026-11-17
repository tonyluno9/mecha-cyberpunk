using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-992 (T-1011: como el mockup M31_HabilidadesProtesis.png; T-1302: en columna): la barra de habilidades de prótesis, en el lateral izquierdo del HUD H10,
    /// de arriba abajo en el orden de sus teclas. Cada casilla lleva a la izquierda el dibujo de línea de la prótesis (nivel I o II), a la derecha la tecla real
    /// y el nombre ("[Q] PIERNAS") y debajo un medidor. Lista: turquesa
    /// y llena; activa: amarilla y se vacía; recargando: rosa y se llena; sin implante: gris con "—". Las tres primeras (piernas, brazos, cabeza)
    /// siempre están; las de ataque (láser, misiles) solo salen cuando tienes la prótesis. Publica los avisos "PIERNAS ACTIVAS" y
    /// "NECESITAS EXO DE PIERNAS". Solo se ve a pie. Lo agrega <see cref="HudAPie"/> al nacer; su canvas es del HUD, así que la rueda y los menús lo apagan.
    /// </summary>
    public class BarraHabilidades : MonoBehaviour
    {
        public const int OrdenCanvas = 12;
        public const float Ancho = 236f;
        public const float Alto = 72f;
        public const float Hueco = 6f;
        /// <summary>Distancia al borde izquierdo: la misma que el minimapa y la ayuda (<see cref="DisposicionHud.MargenH10"/>).</summary>
        public const float MargenIzquierdo = DisposicionHud.MargenH10;
        /// <summary>
        /// Distancia del borde de arriba a la primera casilla (px de 1080): debajo de vida, calor, Biits y renta (arriba a la izquierda) y, con las cinco
        /// casillas, termina arriba de la ayuda de controles (abajo a la izquierda).
        /// </summary>
        public const float DesdeArriba = 388f;
        public const string CarpetaIconos = "Protesis/";
        private const int Fijas = 3;
        private const float AltoMedidor = 10f;
        private const float LadoIcono = 64f;
        private const float XTexto = LadoIcono + 10f;
        private const float AnchoTexto = Ancho - XTexto - 8f;
        private const float AltoEtiqueta = 32f;
        private const float SegundosAviso = 1.6f;
        private const float SegundosTeclas = 1f;
        private const string Apagada = "—";

        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color TurquesaSuave = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color Amarillo = new Color(1f, 0.92f, 0.25f, 1f);
        private static readonly Color Rosa = new Color(0.93f, 0.40f, 0.46f, 1f);
        private static readonly Color Gris = new Color(0.55f, 0.60f, 0.62f, 1f);
        private static readonly Color FondoCasilla = new Color(0f, 0.02f, 0.03f, 0.62f);
        private static readonly Color FondoActiva = new Color(0.05f, 0.045f, 0f, 0.75f);

        private static readonly int Total = Enum.GetValues(typeof(HabilidadProtesis)).Length;

        private sealed class Casilla
        {
            public RectTransform Raiz;
            public Image Fondo;
            public Image[] Borde;
            public Image Icono;
            public Text Etiqueta;
            public RectTransform Relleno;
            public Image RellenoImagen;
            public Color Color;
            public string Tecla = string.Empty;
            public string Nombre = string.Empty;
            public int NivelIcono = -1;
        }

        private readonly Casilla[] casillas = new Casilla[Total];
        private readonly List<int> visibles = new List<int>();
        private readonly Dictionary<string, Sprite> iconos = new Dictionary<string, Sprite>();
        private GameObject grupo;
        private HabilidadesProtesis habilidades;
        private Font fuente;
        private float siguienteTecla;

        /// <summary>True si la barra se está dibujando.</summary>
        public bool Visible => grupo != null && grupo.activeSelf;

        /// <summary>La tecla que se ve en la casilla (sin corchetes).</summary>
        public string TeclaMostrada(HabilidadProtesis h) => casillas[(int)h].Tecla;

        /// <summary>El nombre que se ve en la casilla ("—" si no hay implante).</summary>
        public string NombreMostrado(HabilidadProtesis h) => casillas[(int)h].Nombre;

        /// <summary>El texto de la casilla, como "[Q] PIERNAS".</summary>
        public string EtiquetaMostrada(HabilidadProtesis h) => casillas[(int)h].Etiqueta.text;

        /// <summary>El color con que se pinta la casilla (turquesa lista, amarillo activa, rosa recargando, gris sin implante).</summary>
        public Color ColorMostrado(HabilidadProtesis h) => casillas[(int)h].Color;

        /// <summary>Cuánto del medidor se ve (0 a 1).</summary>
        public float MedidorMostrado(HabilidadProtesis h) => casillas[(int)h].Relleno.anchorMax.x;

        /// <summary>True si la casilla de la habilidad está en la barra ahora.</summary>
        public bool CasillaVisible(HabilidadProtesis h) => casillas[(int)h].Raiz.gameObject.activeSelf;

        /// <summary>El dibujo de la prótesis que lleva la casilla (null si no lo hay).</summary>
        public Sprite IconoMostrado(HabilidadProtesis h) => casillas[(int)h].Icono.sprite;

        /// <summary>La casilla (raíz) de la habilidad, para capturas y pruebas.</summary>
        public RectTransform CasillaDe(HabilidadProtesis h) => casillas[(int)h].Raiz;

        public static string NombreDe(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return "PIERNAS";
                case HabilidadProtesis.Brazos: return "BRAZOS";
                case HabilidadProtesis.Cabeza: return "CABEZA";
                default: return NombreAtaque(h);
            }
        }

        /// <summary>Lo que falta cuando se pulsa la tecla sin el implante.</summary>
        public static string AvisoSinImplante(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return "NECESITAS EXO DE PIERNAS";
                case HabilidadProtesis.Brazos: return "NECESITAS EXO DE BRAZOS";
                case HabilidadProtesis.Cabeza: return "NECESITAS EL VISOR";
                default: return "NECESITAS LA PRÓTESIS " + NombreAtaque(h);
            }
        }

        public static string AvisoActivada(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return "PIERNAS ACTIVAS";
                case HabilidadProtesis.Brazos: return "BRAZOS ACTIVOS";
                case HabilidadProtesis.Cabeza: return "CABEZA ACTIVA";
                default: return NombreAtaque(h) + " LISTO";
            }
        }

        // Las prótesis de ataque (T-1017) se llaman por su nombre en el enum: Laser → LÁSER, Misiles → MISILES.
        private static string NombreAtaque(HabilidadProtesis h)
        {
            string nombre = h.ToString().ToUpperInvariant();
            return nombre == "LASER" ? "LÁSER" : nombre;
        }

        /// <summary>T-1302: la tecla de siempre de cada habilidad (Q, C y X; láser y misiles en 4 y 5), para cuando el Input System no responde.</summary>
        public static string TeclaPorOmision(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return "Q";
                case HabilidadProtesis.Brazos: return "C";
                case HabilidadProtesis.Cabeza: return "X";
                default: return ((int)h + 1).ToString();
            }
        }

        private void Awake()
        {
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            fuente = cfg != null && cfg.Fuente != null ? cfg.Fuente : KitUI.Estilo.Fuente;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasHabilidades", OrdenCanvas);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            grupo = new GameObject("Habilidades", typeof(RectTransform));
            grupo.transform.SetParent(canvas.transform, false);
            KitUI.Estirar((RectTransform)grupo.transform);
            for (int i = 0; i < casillas.Length; i++)
            {
                casillas[i] = Armar(i);
            }
            grupo.SetActive(false);
        }

        private Casilla Armar(int indice)
        {
            Casilla c = new Casilla();
            c.Raiz = Nuevo(grupo.transform, "Habilidad" + indice, new Vector2(0f, 1f), new Vector2(MargenIzquierdo, -DesdeArriba), new Vector2(Ancho, Alto));
            c.Fondo = c.Raiz.gameObject.AddComponent<Image>();
            c.Fondo.raycastTarget = false;
            GameObject borde = KitUI.CrearBorde(c.Raiz, 2f, Color.white);
            c.Borde = borde.GetComponentsInChildren<Image>();

            // A la izquierda: el dibujo de la prótesis.
            RectTransform icono = Nuevo(c.Raiz, "Icono", new Vector2(0f, 1f), new Vector2(4f, -4f), new Vector2(LadoIcono, LadoIcono));
            c.Icono = icono.gameObject.AddComponent<Image>();
            c.Icono.preserveAspect = true;
            c.Icono.raycastTarget = false;

            // A la derecha, arriba: "[Q] PIERNAS".
            c.Etiqueta = Texto(c.Raiz, "Etiqueta", new Vector2(0f, 1f), new Vector2(XTexto, -8f), new Vector2(AnchoTexto, AltoEtiqueta), 24, TextAnchor.MiddleLeft);

            // A la derecha, abajo: el medidor.
            RectTransform pista = Nuevo(c.Raiz, "Medidor", new Vector2(0f, 1f), new Vector2(XTexto, -(Alto - AltoMedidor - 10f)), new Vector2(AnchoTexto, AltoMedidor));
            Image fondoMedidor = pista.gameObject.AddComponent<Image>();
            fondoMedidor.color = new Color(0f, 0f, 0f, 0.55f);
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
            Outline sombra = r.gameObject.AddComponent<Outline>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.7f);
            sombra.effectDistance = new Vector2(1.2f, -1.2f);
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
            Acomodar();
            bool refrescarTeclas = Time.unscaledTime >= siguienteTecla;
            if (refrescarTeclas)
            {
                siguienteTecla = Time.unscaledTime + SegundosTeclas;
            }
            for (int i = 0; i < casillas.Length; i++)
            {
                if (casillas[i].Raiz.gameObject.activeSelf)
                {
                    Pintar((HabilidadProtesis)i, casillas[i], refrescarTeclas);
                }
            }
        }

        // Las tres fijas siempre; las de ataque solo si tienes la prótesis. En columna a la izquierda, de arriba abajo en el orden de sus teclas.
        private void Acomodar()
        {
            int cuantas = 0;
            bool cambio = false;
            for (int i = 0; i < Total; i++)
            {
                bool va = i < Fijas || habilidades.Nivel((HabilidadProtesis)i) > 0;
                if (va)
                {
                    cambio |= cuantas >= visibles.Count || visibles[cuantas] != i;
                    cuantas++;
                }
            }
            cambio |= cuantas != visibles.Count;
            if (!cambio)
            {
                return;
            }
            visibles.Clear();
            for (int i = 0; i < Total; i++)
            {
                bool va = i < Fijas || habilidades.Nivel((HabilidadProtesis)i) > 0;
                casillas[i].Raiz.gameObject.SetActive(va);
                if (va)
                {
                    visibles.Add(i);
                }
            }
            for (int k = 0; k < visibles.Count; k++)
            {
                casillas[visibles[k]].Raiz.anchoredPosition = new Vector2(MargenIzquierdo, -(DesdeArriba + k * (Alto + Hueco)));
            }
        }

        private void Pintar(HabilidadProtesis h, Casilla c, bool refrescarTeclas)
        {
            int nivel = habilidades.Nivel(h);
            bool tiene = nivel > 0;
            EstadoHabilidad estado = habilidades.Estado(h);
            bool activa = estado == EstadoHabilidad.Activa;
            Color color = !tiene ? Gris : activa ? Amarillo : estado == EstadoHabilidad.Recargando ? Rosa : Turquesa;
            c.Color = color;
            c.Fondo.color = activa ? FondoActiva : FondoCasilla;
            Color borde = new Color(color.r, color.g, color.b, tiene ? 1f : 0.45f);
            foreach (Image tira in c.Borde)
            {
                tira.color = borde;
            }
            if (c.Tecla.Length == 0 || refrescarTeclas)
            {
                c.Tecla = TeclasHud.Texto(TeclasHud.Buscar(MechaCyberpunk.Personaje.ConfigHabilidadesProtesis.NombreAccion(h)), TeclaPorOmision(h));
            }
            c.Nombre = tiene ? NombreDe(h) : Apagada;
            string etiqueta = "[" + c.Tecla + "] " + (tiene ? c.Nombre : NombreDe(h));
            if (c.Etiqueta.text != etiqueta)
            {
                c.Etiqueta.text = etiqueta;
            }
            c.Etiqueta.color = !tiene ? Gris : activa ? Amarillo : TurquesaSuave;

            int nivelIcono = Mathf.Max(1, nivel);
            if (c.NivelIcono != nivelIcono)
            {
                c.NivelIcono = nivelIcono;
                c.Icono.sprite = Icono(h, nivelIcono);
                c.Icono.enabled = c.Icono.sprite != null;
            }
            c.Icono.color = !tiene ? new Color(Gris.r, Gris.g, Gris.b, 0.45f) : activa ? Amarillo : Turquesa;

            float medidor = tiene ? habilidades.Medidor(h) : 0f;
            c.Relleno.anchorMax = new Vector2(Mathf.Clamp01(medidor), 1f);
            c.RellenoImagen.color = color;
        }

        // El dibujo de línea de la prótesis: UI/Resources/Protesis/Protesis_<Habilidad>_<nivel>.png (si no hay del nivel II, el del I).
        private Sprite Icono(HabilidadProtesis h, int nivel)
        {
            string nombre = CarpetaIconos + "Protesis_" + h + "_" + nivel;
            if (!iconos.TryGetValue(nombre, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>(nombre);
                if (sprite == null && nivel > 1)
                {
                    sprite = Resources.Load<Sprite>(CarpetaIconos + "Protesis_" + h + "_1");
                }
                iconos[nombre] = sprite;
            }
            return sprite;
        }
    }
}
