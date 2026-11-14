using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
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
    /// T-991: la rueda radial de armas y objetos (docs/diseno/rueda-y-habilidades.md). Al mantener su tecla (acción "Rueda", Tab) a pie, vivo y con control,
    /// el tiempo baja a 0.25, el fondo se oscurece y el ratón mueve un cursor virtual (no gira la cámara ni dispara). Al soltar equipa el arma resaltada
    /// o usa el objeto resaltado; sin nada resaltado solo cierra. Se cierra sola si mueres, subes a un carro, hay una cinemática o se abre un menú o la pausa
    /// (la pausa conserva la escala de tiempo que había antes de la rueda). La lógica de casillas está en <see cref="ReglaRuedaArmas"/>.
    /// La agrega <see cref="HudAPie"/> al nacer y arma su canvas en código con los colores y la fuente del HUD H10.
    /// </summary>
    public class RuedaArmasObjetos : MonoBehaviour
    {
        public const int OrdenCanvas = 90; // por encima de todos los canvas del HUD (H10 12, historia 20, mapa 40, pausa 60 queda debajo: la rueda no abre con ella)
        public const string NombreAccion = "Rueda";

        private const float RadioAnillo = 300f;
        private const float LadoCasilla = 124f;
        private const float AlfaFondo = 0.92f; // el diseño pide al menos 0.55; con mezcla lineal 0.8 apenas se notaba
        private const float AlfaApagada = 0.28f;

        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color TurquesaSuave = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color Blanco = new Color(0.93f, 0.97f, 0.97f, 1f);
        private static readonly Color FondoCasilla = new Color(0.02f, 0.07f, 0.11f, 0.62f);
        private static readonly Color FondoResaltada = new Color(0.10f, 0.40f, 0.42f, 0.9f);
        private static readonly string[] NombresArmas = { "PUÑOS", "PISTOLA", "ESCOPETA", "FUSIL", "FRANCOTIRADOR" };

        private sealed class Casilla
        {
            public RectTransform Raiz;
            public Image Fondo;
            public Image[] Borde;
            public Image Icono;
            public Text Nombre;
            public Text Cantidad;
            public CanvasGroup Grupo;
        }

        private static bool abierta;
        private static float escalaAnterior = 1f;
        private static int aperturas;

        private readonly List<Canvas> ocultos = new List<Canvas>();
        private Canvas canvas;
        private GameObject raiz;
        private readonly Casilla[] casillas = new Casilla[ReglaRuedaArmas.Casillas];
        private Text textoNombre;
        private Text textoDetalle;
        private RectTransform punto;
        private InputAction accion;
        private bool estabaPulsada;
        private Vector2 movimientoPendiente;
        private Vector2 cursor;
        private int resaltada = -1;
        private float fixedAnterior = 0.02f;
        private Transform raizJugador;
        private ControladorAPie controlador;
        private ArmasAPie armas;
        private PistolaAPie pistola;
        private ConsumiblesJugador consumibles;
        private VidaAPie vida;

        /// <summary>True mientras la rueda está abierta.</summary>
        public static bool Abierta => abierta;

        /// <summary>La escala de tiempo que había antes de abrir la rueda (la que se devuelve al cerrar).</summary>
        public static float EscalaAnterior => escalaAnterior;

        /// <summary>Cuántas veces se ha abierto la rueda (el tutorial lo lee para dar por vista la tecla aunque la abran y cierren de golpe).</summary>
        public static int Aperturas => aperturas;

        /// <summary>La casilla resaltada ahora (0 a 9) o -1.</summary>
        public int Resaltada => resaltada;

        /// <summary>El cursor virtual (px de referencia desde el centro, +y arriba).</summary>
        public Vector2 Cursor => cursor;

        public bool Armada => raiz != null;

        public Text TextoNombre => textoNombre;
        public Text TextoDetalle => textoDetalle;
        public bool CasillaApagada(int casilla) => casillas[casilla].Grupo.alpha < 0.5f;
        public string TextoCantidad(int casilla) => casillas[casilla].Cantidad.text;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico()
        {
            abierta = false;
            escalaAnterior = 1f;
            aperturas = 0;
        }

        private void Start()
        {
            Armar();
            accion = BuscarAccion();
            accion?.Enable();
            Pausa.Cambio += AlCambiarPausa;
            InputSystem.onAfterUpdate += AcumularMovimiento;
        }

        private void OnDisable()
        {
            if (abierta && raiz != null)
            {
                Cerrar(false);
            }
            accion?.Disable();
            accion = null;
        }

        private void OnDestroy()
        {
            Pausa.Cambio -= AlCambiarPausa;
            InputSystem.onAfterUpdate -= AcumularMovimiento;
        }

        // El movimiento del ratón se suma después de cada actualización del Input System (no se lee en Update): así no se pierde si la actualización llegó entre cuadros.
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
                Cerrar(false);
            }
        }

        private void Update()
        {
            if (raiz == null)
            {
                return;
            }
            if (accion == null)
            {
                accion = BuscarAccion();
                accion?.Enable();
                if (accion == null)
                {
                    return;
                }
            }
            // El flanco se detecta a mano (no con WasPressedThisFrame): así una pulsación simulada que se procesa entre cuadros tampoco se pierde.
            bool pulsada = accion.IsPressed();
            bool flanco = pulsada && !estabaPulsada;
            estabaPulsada = pulsada;
            if (!abierta)
            {
                if (flanco && PuedeAbrir())
                {
                    Abrir();
                }
                return;
            }
            if (!PuedeSeguir())
            {
                Cerrar(false);
                return;
            }
            if (!accion.IsPressed())
            {
                Cerrar(true);
                return;
            }
            cursor = ReglaRuedaArmas.MoverCursor(cursor, movimientoPendiente);
            movimientoPendiente = Vector2.zero;
            resaltada = ReglaRuedaArmas.CasillaResaltada(cursor, Disponible);
            Dibujar();
        }

        // La acción del asset del jugador (la de ControlesHud, la misma que usan los demás componentes); si no está, la que ya haya cargado la escena.
        private static InputAction BuscarAccion()
        {
            ControlesHud controles = ControlesHud.Actual;
            return (controles != null ? controles.AccionAPie(NombreAccion) : null) ?? TeclasHud.Buscar(NombreAccion);
        }

        // El jugador del registro; si no hay (una prueba lo limpia), el que lleva este HUD anidado.
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
            armas = r != null ? r.GetComponentInChildren<ArmasAPie>() : null;
            pistola = r != null ? r.GetComponentInChildren<PistolaAPie>() : null;
            consumibles = r != null ? r.GetComponentInChildren<ConsumiblesJugador>() : null;
            vida = r != null ? r.GetComponentInChildren<VidaAPie>() : null;
        }

        // A pie, vivo, con control, sin menú ni pausa abiertos y con el tiempo corriendo.
        private bool PuedeAbrir()
        {
            Vincular();
            IJugador j = JugadorActual();
            return j != null && j.Modo == ModoJugador.APie && controlador != null && controlador.ControlActivo && armas != null
                && vida != null && !vida.EstaKO && !Pausa.Activa && PilaPantallas.Vacia && MenuCyberfix.Abiertos == 0
                && Time.timeScale > 0.01f;
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
            escalaAnterior = Time.timeScale;
            fixedAnterior = Time.fixedDeltaTime;
            Time.timeScale = ReglaRuedaArmas.EscalaTiempo * escalaAnterior;
            Time.fixedDeltaTime = fixedAnterior * ReglaRuedaArmas.EscalaTiempo;
            controlador.EntradaBloqueada = true;
            cursor = Vector2.zero;
            movimientoPendiente = Vector2.zero;
            resaltada = -1;
            OcultarHud();
            raiz.SetActive(true);
            Dibujar();
        }

        private void Cerrar(bool elegir)
        {
            // El flanco se detecta a mano (no con WasPressedThisFrame): así una pulsación simulada que se procesa entre cuadros tampoco se pierde.
            bool pulsada = accion.IsPressed();
            bool flanco = pulsada && !estabaPulsada;
            estabaPulsada = pulsada;
            if (!abierta)
            {
                return;
            }
            abierta = false;
            // La pausa dueña del tiempo (timeScale en 0): la rueda no la pisa; solo devuelve el paso de la física.
            if (!Pausa.Activa)
            {
                Time.timeScale = escalaAnterior;
            }
            Time.fixedDeltaTime = fixedAnterior;
            raiz.SetActive(false);
            RestaurarHud();
            if (controlador != null)
            {
                controlador.EntradaBloqueada = false;
            }
            int casilla = resaltada;
            resaltada = -1;
            if (!elegir)
            {
                return;
            }
            SeleccionRueda eleccion = ReglaRuedaArmas.Resultado(casilla);
            if (eleccion.Tipo == TipoSeleccionRueda.Arma && armas != null && armas.PuedeCambiar)
            {
                armas.Equipar((ArmaAPie)eleccion.Indice);
            }
            else if (eleccion.Tipo == TipoSeleccionRueda.Objeto && consumibles != null && eleccion.Indice < consumibles.Catalogo.Count)
            {
                consumibles.Usar(consumibles.Catalogo[eleccion.Indice]);
            }
        }

        // Mientras la rueda está abierta el resto del HUD (tutorial, avisos, ayudas, objetos, minimapa, subtítulos: todo canvas que no sea del mundo) se apaga para que nada se encime.
        private void OcultarHud()
        {
            ocultos.Clear();
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.enabled && c != canvas && !canvas.transform.IsChildOf(c.transform) && !c.transform.IsChildOf(canvas.transform) && c.renderMode != RenderMode.WorldSpace)
                {
                    c.enabled = false;
                    ocultos.Add(c);
                }
            }
        }

        private void RestaurarHud()
        {
            foreach (Canvas c in ocultos)
            {
                if (c != null)
                {
                    c.enabled = true;
                }
            }
            ocultos.Clear();
        }

        private bool Disponible(int casilla)
        {
            if (ReglaRuedaArmas.EsArma(casilla))
            {
                ArmaAPie arma = (ArmaAPie)casilla;
                return arma == ArmaAPie.Punos || (armas != null && armas.TieneArma(arma));
            }
            return CantidadDe(casilla) > 0;
        }

        private DatosConsumible ObjetoDe(int casilla)
        {
            int lugar = casilla - ReglaRuedaArmas.CasillasArma;
            return consumibles != null && lugar >= 0 && lugar < consumibles.Catalogo.Count ? consumibles.Catalogo[lugar] : null;
        }

        private int CantidadDe(int casilla)
        {
            DatosConsumible c = ObjetoDe(casilla);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            return c != null && inventario != null ? inventario.Cantidad(c) : 0;
        }

        private string BalasDe(ArmaAPie arma)
        {
            CargadorPistola c = pistola != null ? pistola.CargadorDe(arma) : null;
            if (c == null)
            {
                return string.Empty;
            }
            return c.Fuente != null ? c.Balas + "|" + c.Fuente.Disponibles : c.Balas + "|" + c.Tamano;
        }

        // ---- Dibujo ----

        private void Dibujar()
        {
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            PiezasUI piezas = PiezasUI.Actual;
            for (int i = 0; i < casillas.Length; i++)
            {
                Casilla c = casillas[i];
                bool ok = Disponible(i);
                bool resalto = i == resaltada;
                c.Grupo.alpha = ok ? 1f : AlfaApagada;
                c.Fondo.color = resalto ? FondoResaltada : FondoCasilla;
                Color colorBorde = resalto ? Blanco : new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.85f);
                foreach (Image tira in c.Borde)
                {
                    tira.color = colorBorde;
                }
                c.Raiz.localScale = resalto ? Vector3.one * 1.12f : Vector3.one;
                Sprite icono = null;
                Material material = null;
                string cantidad = string.Empty;
                string nombre;
                if (ReglaRuedaArmas.EsArma(i))
                {
                    nombre = NombresArmas[i];
                    if (piezas != null && piezas.EsHolograma(i))
                    {
                        icono = piezas.ArmaHud(i);
                        material = piezas.MaterialArmaHud(i);
                    }
                    cantidad = ok ? BalasDe((ArmaAPie)i) : string.Empty;
                }
                else
                {
                    DatosConsumible o = ObjetoDe(i);
                    nombre = o != null ? o.Nombre.ToUpperInvariant() : "—";
                    icono = o != null && cfg != null ? cfg.Icono(o.Id) : null;
                    cantidad = "x" + CantidadDe(i);
                }
                c.Icono.sprite = icono;
                c.Icono.material = material != null ? material : c.Icono.defaultMaterial;
                c.Icono.enabled = icono != null;
                c.Nombre.text = nombre;
                c.Cantidad.text = cantidad;
            }
            punto.anchoredPosition = cursor;
            if (resaltada < 0)
            {
                textoNombre.text = string.Empty;
                textoDetalle.text = "SUELTA PARA CERRAR";
                return;
            }
            if (ReglaRuedaArmas.EsArma(resaltada))
            {
                textoNombre.text = NombresArmas[resaltada];
                string balas = BalasDe((ArmaAPie)resaltada);
                textoDetalle.text = balas.Length > 0 ? "BALAS " + balas.Replace("|", " | ") : "CUERPO A CUERPO";
            }
            else
            {
                DatosConsumible o = ObjetoDe(resaltada);
                textoNombre.text = o != null ? o.Nombre.ToUpperInvariant() : string.Empty;
                textoDetalle.text = o != null ? o.Descripcion.ToUpperInvariant() : string.Empty;
            }
        }

        // ---- Armado ----

        private void Armar()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasRueda", OrdenCanvas);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            raiz = new GameObject("Rueda", typeof(RectTransform));
            raiz.transform.SetParent(canvas.transform, false);
            KitUI.Estirar((RectTransform)raiz.transform);

            Image fondo = KitUI.CrearImagen(raiz.transform, "Fondo", new Color(0f, 0f, 0f, AlfaFondo));
            fondo.raycastTarget = false;
            KitUI.Estirar(fondo.rectTransform);

            // Un disco tenue con aro turquesa detrás de las casillas, para que el anillo se lea como rueda.
            Image disco = KitUI.CrearImagen(raiz.transform, "Disco", Color.white);
            disco.sprite = SpriteDisco();
            disco.raycastTarget = false;
            KitUI.Colocar(disco.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (RadioAnillo * 2f + LadoCasilla + 40f));

            for (int i = 0; i < casillas.Length; i++)
            {
                casillas[i] = ArmarCasilla(i);
            }
            textoNombre = Texto(raiz.transform, "Nombre", new Vector2(0f, 26f), new Vector2(420f, 60f), 46, TurquesaSuave);
            textoDetalle = Texto(raiz.transform, "Detalle", new Vector2(0f, -30f), new Vector2(420f, 80f), 28, Blanco);
            textoDetalle.verticalOverflow = VerticalWrapMode.Truncate;
            textoDetalle.horizontalOverflow = HorizontalWrapMode.Wrap;

            punto = (RectTransform)KitUI.CrearRombo(raiz.transform, "Cursor", 18f, Blanco).rectTransform.parent;
            punto.anchorMin = punto.anchorMax = new Vector2(0.5f, 0.5f);
            raiz.SetActive(false);
        }

        private static Sprite SpriteDisco()
        {
            const int lado = 256;
            Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            float mitad = lado / 2f;
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(mitad, mitad)) / mitad;
                    float alfa = d > 1f ? 0f : d > 0.985f ? 0.4f : 0.07f;
                    t.SetPixel(x, y, new Color(Turquesa.r, Turquesa.g, Turquesa.b, alfa));
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
        }

        private Casilla ArmarCasilla(int indice)
        {
            Casilla c = new Casilla();
            GameObject g = new GameObject("Casilla" + indice, typeof(RectTransform));
            g.transform.SetParent(raiz.transform, false);
            c.Raiz = (RectTransform)g.transform;
            float angulo = ReglaRuedaArmas.AnguloDeCasilla(indice) * Mathf.Deg2Rad;
            KitUI.Colocar(c.Raiz, new Vector2(0.5f, 0.5f), new Vector2(Mathf.Sin(angulo), Mathf.Cos(angulo)) * RadioAnillo, new Vector2(LadoCasilla, LadoCasilla));
            c.Grupo = g.AddComponent<CanvasGroup>();
            c.Fondo = g.AddComponent<Image>();
            c.Fondo.color = FondoCasilla;
            c.Fondo.raycastTarget = false;
            GameObject borde = KitUI.CrearBorde(c.Raiz, 2f, Turquesa);
            c.Borde = borde.GetComponentsInChildren<Image>();
            RectTransform ri = Nuevo(c.Raiz, "Icono", new Vector2(0f, 20f), new Vector2(64f, 64f));
            c.Icono = ri.gameObject.AddComponent<Image>();
            c.Icono.preserveAspect = true;
            c.Icono.raycastTarget = false;
            c.Icono.color = Turquesa;
            c.Nombre = Texto(c.Raiz, "Nombre", new Vector2(0f, -28f), new Vector2(LadoCasilla, 26f), 17, TurquesaSuave);
            c.Cantidad = Texto(c.Raiz, "Cantidad", new Vector2(0f, -50f), new Vector2(LadoCasilla, 24f), 22, Blanco);
            return c;
        }

        private static RectTransform Nuevo(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform));
            g.transform.SetParent(padre, false);
            RectTransform r = (RectTransform)g.transform;
            KitUI.Colocar(r, new Vector2(0.5f, 0.5f), posicion, tamano);
            return r;
        }

        private static Text Texto(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, int letra, Color color)
        {
            RectTransform r = Nuevo(padre, nombre, posicion, tamano);
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
            Outline sombra = r.gameObject.AddComponent<Outline>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.75f);
            sombra.effectDistance = new Vector2(1.3f, -1.3f);
            return t;
        }
    }
}
