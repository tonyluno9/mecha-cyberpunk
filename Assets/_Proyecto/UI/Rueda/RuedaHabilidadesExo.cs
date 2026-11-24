using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Rueda
{
    /// <summary>
    /// T-1329 y T-57: el menú de habilidades del EXO. Se mantiene Tab (acción "RuedaExo"), el tiempo baja a 0.25 y al centro aparece una silueta holográfica
    /// del cuerpo con las zonas PIERNAS, BRAZOS y CABEZA (y LÁSER y MISILES a un lado). El ratón mueve un cursor virtual; la zona bajo el cursor se ilumina y
    /// muestra su nombre y estado; el clic izquierdo (acción "RuedaExoElegir") o soltar Tab sobre ella la activa. Las zonas sin implante se ven apagadas. Ya no hay una
    /// tecla suelta por habilidad. No abre a la vez que la rueda de armas (botón medio). Reusa la lógica de sectores de <see cref="ReglaRuedaArmas"/>.
    /// La agrega <see cref="HudAPie"/> al nacer y arma su canvas en código con los colores del HUD H10.
    /// </summary>
    public class RuedaHabilidadesExo : MonoBehaviour
    {
        public const int OrdenCanvas = 91;
        public const string NombreAccion = "RuedaExo";
        public const string NombreAccionElegir = "RuedaExoElegir";
        /// <summary>VR20: el texto del centro en VR (sin Tab ni ratón: se elige con la mano y se suelta).</summary>
        public const string TextoCentroVR = "ELIGE UNA ZONA Y SUELTA";

        /// <summary>T-57: hasta dónde llega el cursor virtual desde el centro (px de referencia), en cada eje.</summary>
        public static readonly Vector2 LimiteCursor = new Vector2(520f, 340f);
        private static readonly Vector2 TamanoCasilla = new Vector2(250f, 96f);

        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color Amarillo = new Color(1f, 0.92f, 0.25f, 1f);
        private static readonly Color Rosa = new Color(1f, 0.35f, 0.6f, 1f);
        private static readonly Color Gris = new Color(0.5f, 0.55f, 0.58f, 1f);
        private static readonly Color Blanco = new Color(0.93f, 0.97f, 0.97f, 1f);
        private static readonly Color FondoCasilla = new Color(0.02f, 0.08f, 0.11f, 0.8f);
        private static readonly Color FondoResaltada = new Color(0.07f, 0.06f, 0.0f, 0.85f);

        private sealed class Casilla
        {
            public RectTransform Raiz;
            public Image Fondo;
            public Image[] Partes;
            public Image[] Lineas;
            public Text Nombre;
            public Text Estado;
        }

        private static bool abierta;
        private static float escalaAnterior = 1f;
        private static int aperturas;
        private static int activaciones;

        private readonly List<Casilla> casillas = new List<Casilla>();
        private Canvas canvas;
        private GameObject raiz;
        private RectTransform punto;
        private Text textoCentro;
        private InputAction accion;
        private InputAction accionElegir;
        private bool estabaPulsada;
        private bool clicEstabaPulsado;
        private bool activadaEnEstaApertura;
        private Vector2 movimientoPendiente;
        private Vector2 cursor;
        private int resaltada = -1;
        private Transform raizJugador;
        private ControladorAPie controlador;
        private HabilidadesProtesis habilidades;
        private VidaAPie vida;

        /// <summary>True mientras la rueda está abierta.</summary>
        public static bool Abierta => abierta;

        /// <summary>La escala de tiempo que había antes de abrir la rueda.</summary>
        public static float EscalaAnterior => escalaAnterior;

        /// <summary>Cuántas veces se ha abierto.</summary>
        public static int Aperturas => aperturas;

        /// <summary>Cuántas habilidades se han activado desde la rueda.</summary>
        public static int Activaciones => activaciones;

        public static RuedaHabilidadesExo Actual { get; private set; }

        /// <summary>La habilidad resaltada ahora (valor de <see cref="HabilidadProtesis"/>) o -1.</summary>
        public int Resaltada => resaltada;

        public Vector2 Cursor => cursor;

        public bool Armada => raiz != null;

        /// <summary>T90: cierra sin activar nada (al empezar una cinemática): el tiempo vuelve en el acto. False si no estaba abierta.</summary>
        public bool CerrarSinElegir()
        {
            if (!abierta || raiz == null)
            {
                return false;
            }
            Cerrar();
            return true;
        }

        /// <summary>T-57: el centro de la zona (px de referencia desde el centro, +y arriba): sobre la silueta, o la casilla de LÁSER y MISILES.</summary>
        public static Vector2 PosicionDe(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return new Vector2(37f, -150f);
                case HabilidadProtesis.Brazos: return new Vector2(89f, 66f);
                case HabilidadProtesis.Cabeza: return new Vector2(0f, 247f);
                case HabilidadProtesis.Laser: return new Vector2(-380f, 120f);
                default: return new Vector2(-380f, -20f);
            }
        }

        // Dónde se escribe el nombre y el estado de cada zona.
        private static Vector2 PosicionEtiqueta(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return new Vector2(320f, -140f);
                case HabilidadProtesis.Brazos: return new Vector2(320f, 66f);
                case HabilidadProtesis.Cabeza: return new Vector2(320f, 247f);
                default: return PosicionDe(h);
            }
        }

        // Las partes de la silueta de cada zona: centro y tamaño (px de referencia).
        private static (Vector2 centro, Vector2 tamano)[] Partes(HabilidadProtesis h)
        {
            switch (h)
            {
                // T-1760: regiones de los sprites del cuerpo del HUD (139x353) escalados a la silueta de 236x600 px.
                case HabilidadProtesis.Piernas: return new[] { (new Vector2(-37f, -150f), new Vector2(64f, 280f)), (new Vector2(37f, -150f), new Vector2(64f, 280f)) };
                case HabilidadProtesis.Brazos: return new[] { (new Vector2(-89f, 66f), new Vector2(62f, 257f)), (new Vector2(89f, 66f), new Vector2(62f, 257f)) };
                case HabilidadProtesis.Cabeza: return new[] { (new Vector2(0f, 247f), new Vector2(110f, 110f)) };
                default: return new[] { (PosicionDe(h), TamanoCasilla) };
            }
        }

        /// <summary>La zona bajo un punto del cursor (valor de <see cref="HabilidadProtesis"/>) o -1 (T-57).</summary>
        public static int ZonaEn(Vector2 punto)
        {
            for (int i = 0; i < Cantidad; i++)
            {
                foreach ((Vector2 centro, Vector2 tamano) parte in Partes((HabilidadProtesis)i))
                {
                    Vector2 d = punto - parte.centro;
                    if (Mathf.Abs(d.x) <= parte.tamano.x * 0.5f && Mathf.Abs(d.y) <= parte.tamano.y * 0.5f)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private static int Cantidad => System.Enum.GetValues(typeof(HabilidadProtesis)).Length;

        public static string NombreDe(HabilidadProtesis h)
        {
            string nombre = h.ToString().ToUpperInvariant();
            return nombre == "LASER" ? "LÁSER" : nombre;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico()
        {
            abierta = false;
            escalaAnterior = 1f;
            aperturas = 0;
            activaciones = 0;
            Actual = null;
        }

        private void Start()
        {
            Armar();
            Actual = this;
            BuscarAcciones();
            Pausa.Cambio += AlCambiarPausa;
            InputSystem.onAfterUpdate += AcumularMovimiento;
        }

        private void OnDisable()
        {
            if (abierta && raiz != null)
            {
                Cerrar();
            }
            ControlTiempo.Soltar(this);
            accion?.Disable();
            accionElegir?.Disable();
            accion = null;
            accionElegir = null;
        }

        private void OnDestroy()
        {
            ControlTiempo.Soltar(this);
            if (Actual == this)
            {
                Actual = null;
            }
            Pausa.Cambio -= AlCambiarPausa;
            InputSystem.onAfterUpdate -= AcumularMovimiento;
        }

        private void AcumularMovimiento()
        {
            if (abierta && Mouse.current != null)
            {
                movimientoPendiente += Mouse.current.delta.ReadValue();
            }
        }

        private void AlCambiarPausa(bool activa)
        {
            if (activa && abierta)
            {
                Cerrar();
            }
        }

        private void BuscarAcciones()
        {
            ControlesHud controles = ControlesHud.Actual;
            accion = (controles != null ? controles.AccionAPie(NombreAccion) : null) ?? TeclasHud.Buscar(NombreAccion);
            accionElegir = (controles != null ? controles.AccionAPie(NombreAccionElegir) : null) ?? TeclasHud.Buscar(NombreAccionElegir);
            accion?.Enable();
            accionElegir?.Enable();
        }

        private void Update()
        {
            if (raiz == null)
            {
                return;
            }
            if (accion == null || accionElegir == null)
            {
                BuscarAcciones();
                if (accion == null || accionElegir == null)
                {
                    return;
                }
            }
            // El flanco se detecta a mano: así una pulsación simulada que se procesa entre cuadros tampoco se pierde.
            bool pulsada = accion.IsPressed();
            bool flanco = pulsada && !estabaPulsada;
            estabaPulsada = pulsada;
            bool clic = accionElegir.IsPressed();
            bool clicNuevo = clic && !clicEstabaPulsado;
            clicEstabaPulsado = clic;
            if (!abierta)
            {
                if (flanco && PuedeAbrir())
                {
                    Abrir();
                }
                return;
            }
            if (!PuedeSeguir() || !pulsada)
            {
                // T-57: soltar Tab sobre una zona la activa (si el clic no la activó ya).
                if (!pulsada && PuedeSeguir() && !activadaEnEstaApertura && resaltada >= 0 && habilidades != null
                    && habilidades.Pulsar((HabilidadProtesis)resaltada))
                {
                    activaciones++;
                    Sonidos.Reproducir("UI_SELECCIONAR");
                }
                Cerrar();
                return;
            }
            cursor = new Vector2(Mathf.Clamp(cursor.x + movimientoPendiente.x, -LimiteCursor.x, LimiteCursor.x),
                Mathf.Clamp(cursor.y + movimientoPendiente.y, -LimiteCursor.y, LimiteCursor.y));
            movimientoPendiente = Vector2.zero;
            int antes = resaltada;
            resaltada = ZonaEn(cursor);
            if (resaltada >= 0 && resaltada != antes)
            {
                Sonidos.Reproducir("UI_MOVER"); // T-1535: la rueda suena al pasar de una habilidad a otra
            }
            if (clicNuevo && resaltada >= 0 && habilidades != null && habilidades.Pulsar((HabilidadProtesis)resaltada))
            {
                activaciones++;
                activadaEnEstaApertura = true;
                Sonidos.Reproducir("UI_SELECCIONAR"); // T-1535: elegir la habilidad (los servos los pide la propia habilidad)
            }
            Dibujar();
        }

        private IJugador JugadorActual() => Registro<IJugador>.Actual ?? GetComponentInParent<JugadorAPie>();

        private void Vincular()
        {
            IJugador j = JugadorActual();
            Transform r = j != null ? j.Raiz : null;
            if (r == raizJugador && (r == null || controlador != null))
            {
                return;
            }
            raizJugador = r;
            controlador = r != null ? r.GetComponentInChildren<ControladorAPie>() : null;
            habilidades = r != null ? r.GetComponentInChildren<HabilidadesProtesis>() : null;
            vida = r != null ? r.GetComponentInChildren<VidaAPie>() : null;
        }

        private bool PuedeAbrir()
        {
            Vincular();
            IJugador j = JugadorActual();
            return j != null && j.Modo == ModoJugador.APie && controlador != null && controlador.ControlActivo && habilidades != null
                && vida != null && !vida.EstaKO && !Pausa.Activa && PilaPantallas.Vacia && MenuCyberfix.Abiertos == 0
                && !RuedaArmasObjetos.Abierta && Time.timeScale > 0.01f;
        }

        private bool PuedeSeguir()
        {
            IJugador j = JugadorActual();
            return j != null && j.Raiz == raizJugador && j.Modo == ModoJugador.APie && controlador != null && controlador.ControlActivo
                && vida != null && !vida.EstaKO && !Pausa.Activa && PilaPantallas.Vacia && MenuCyberfix.Abiertos == 0;
        }

        private void Abrir()
        {
            abierta = true;
            aperturas++;
            escalaAnterior = ControlTiempo.Escala;
            ControlTiempo.Pedir(this, ReglaRuedaArmas.EscalaTiempo);
            controlador.EntradaBloqueada = true;
            cursor = Vector2.zero;
            movimientoPendiente = Vector2.zero;
            resaltada = -1;
            activadaEnEstaApertura = false;
            Sonidos.Reproducir(RuedaArmasObjetos.IdCamaraLenta); // T-57: el mismo sonido de cámara lenta que la rueda de armas
            raiz.SetActive(true);
            Dibujar();
        }

        private void Cerrar()
        {
            if (!abierta)
            {
                return;
            }
            abierta = false;
            ControlTiempo.Soltar(this);
            raiz.SetActive(false);
            Sonidos.Reproducir(RuedaArmasObjetos.IdCamaraLentaSalir);
            if (controlador != null)
            {
                controlador.EntradaBloqueada = false;
            }
            resaltada = -1;
        }

        // Solo se puede elegir la habilidad cuyo implante o prótesis tienes.
        private bool Elegible(int casilla) => habilidades != null && habilidades.Nivel((HabilidadProtesis)casilla) > 0;

        private void Dibujar()
        {
            for (int i = 0; i < casillas.Count; i++)
            {
                HabilidadProtesis h = (HabilidadProtesis)i;
                Casilla c = casillas[i];
                bool tiene = Elegible(i);
                bool resalto = i == resaltada;
                EstadoHabilidad estado = habilidades != null ? habilidades.Estado(h) : EstadoHabilidad.Lista;
                Color color = !tiene ? Gris : estado == EstadoHabilidad.Activa ? Amarillo : estado == EstadoHabilidad.Recargando ? Rosa : Turquesa;
                c.Fondo.color = resalto ? FondoResaltada : FondoCasilla;
                Color parte = !tiene ? new Color(Gris.r, Gris.g, Gris.b, 0.18f) : resalto ? new Color(Amarillo.r, Amarillo.g, Amarillo.b, 0.75f) : new Color(color.r, color.g, color.b, 0.4f);
                foreach (Image p in c.Partes)
                {
                    p.color = parte;
                }
                Color linea = !tiene ? new Color(Gris.r, Gris.g, Gris.b, 0.45f) : resalto ? Amarillo : new Color(color.r, color.g, color.b, 0.95f);
                foreach (Image l in c.Lineas)
                {
                    l.color = linea;
                }
                c.Nombre.color = resalto ? Amarillo : color;
                c.Estado.color = resalto ? Amarillo : color;
                c.Nombre.text = NombreDe(h);
                c.Estado.text = !tiene ? "SIN IMPLANTE" : estado == EstadoHabilidad.Activa ? "ACTIVA" : estado == EstadoHabilidad.Recargando ? "RECARGANDO" : "LISTA";
            }
            punto.anchoredPosition = cursor;
            textoCentro.text = resaltada < 0 ? (ModoVR.Activo ? TextoCentroVR : "ELIGE UNA ZONA: CLIC O SUELTA TAB") : NombreDe((HabilidadProtesis)resaltada);
        }

        private void Armar()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasRuedaExo", OrdenCanvas);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            raiz = new GameObject("RuedaExo", typeof(RectTransform));
            raiz.transform.SetParent(canvas.transform, false);
            KitUI.Estirar((RectTransform)raiz.transform);

            Image fondo = KitUI.CrearImagen(raiz.transform, "Fondo", new Color(0f, 0.01f, 0.03f, 0.62f));
            fondo.raycastTarget = false;
            KitUI.Estirar(fondo.rectTransform);

            textoCentro = Texto(Nuevo(raiz.transform, "Centro", new Vector2(0f, -400f), new Vector2(900f, 60f)), 30, Amarillo);

            // T-1760 (T57): la silueta holográfica es la misma del muñequito del HUD (sprites de ConfigHudH10), grande al centro.
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            RectTransform silueta = Nuevo(raiz.transform, "Silueta", Vector2.zero, TamanoSilueta);
            CapaCuerpo(silueta, "TorsoRelleno", cfg != null ? cfg.CuerpoRelleno(ZonaCuerpoH10.Torso) : null, new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.15f));
            CapaCuerpo(silueta, "TorsoLinea", cfg != null ? cfg.CuerpoLinea(ZonaCuerpoH10.Torso) : null, new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.7f));

            for (int i = 0; i < Cantidad; i++)
            {
                HabilidadProtesis h = (HabilidadProtesis)i;
                Casilla c = new Casilla();
                bool propia = h == HabilidadProtesis.Laser || h == HabilidadProtesis.Misiles;
                c.Partes = new Image[0];
                c.Lineas = new Image[0];
                if (!propia)
                {
                    ZonaCuerpoH10 zona = h == HabilidadProtesis.Piernas ? ZonaCuerpoH10.Piernas : h == HabilidadProtesis.Brazos ? ZonaCuerpoH10.Brazos : ZonaCuerpoH10.Cabeza;
                    c.Partes = new[] { CapaCuerpo(silueta, "Relleno" + zona, cfg != null ? cfg.CuerpoRelleno(zona) : null, Turquesa) };
                    c.Lineas = new[] { CapaCuerpo(silueta, "Linea" + zona, cfg != null ? cfg.CuerpoLinea(zona) : null, Turquesa) };
                }
                c.Raiz = Nuevo(raiz.transform, "Casilla" + i, PosicionEtiqueta(h), TamanoCasilla);
                c.Fondo = c.Raiz.gameObject.AddComponent<Image>();
                c.Fondo.raycastTarget = false;
                c.Fondo.color = FondoCasilla;
                KitUI.CrearBorde(c.Raiz, 3f, Turquesa);
                c.Nombre = Texto(Nuevo(c.Raiz, "Nombre", new Vector2(0f, 16f), new Vector2(TamanoCasilla.x - 10f, 40f)), 34, Turquesa);
                c.Estado = Texto(Nuevo(c.Raiz, "Estado", new Vector2(0f, -22f), new Vector2(TamanoCasilla.x - 10f, 30f)), 22, Turquesa);
                casillas.Add(c);
            }

            punto = (RectTransform)KitUI.CrearRombo(raiz.transform, "Cursor", 18f, Blanco).rectTransform.parent;
            punto.anchorMin = punto.anchorMax = new Vector2(0.5f, 0.5f);
            raiz.SetActive(false);
        }

        private static readonly Vector2 TamanoSilueta = new Vector2(236f, 600f);

        // Una capa del cuerpo a tamaño completo de la silueta (todas las capas del HUD comparten el mismo lienzo de 139x353).
        private static Image CapaCuerpo(RectTransform silueta, string nombre, Sprite sprite, Color color)
        {
            Image im = KitUI.CrearImagen(silueta, nombre, color);
            im.sprite = sprite;
            im.preserveAspect = true;
            im.raycastTarget = false;
            KitUI.Estirar(im.rectTransform);
            return im;
        }

        private static RectTransform Nuevo(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform));
            g.transform.SetParent(padre, false);
            RectTransform r = (RectTransform)g.transform;
            KitUI.Colocar(r, new Vector2(0.5f, 0.5f), posicion, tamano);
            return r;
        }

        private static Text Texto(RectTransform r, int letra, Color color)
        {
            Text t = r.gameObject.AddComponent<Text>();
            ConfigHudH10 estilo = ConfigHudH10.Actual;
            t.font = estilo != null && estilo.Fuente != null ? estilo.Fuente : KitUI.Estilo.Fuente;
            t.fontSize = letra;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = string.Empty;
            return t;
        }
    }
}
