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

        // Medidas en px de referencia (1920 x 1080), tomadas del mockup M30_RuedaArmas.png.
        private const float RadioInterior = 212f;
        private const float RadioExterior = 430f;
        private const float RadioIcono = 318f;
        private const float SeparacionTexto = 50f;
        private const float ApotemaCentro = 172f;
        private const float HuecoSectores = 9f;
        private const float GrosorBorde = 3f;
        private const float BrilloBorde = 9f;
        private const float AlfaFondo = 0.62f;
        private const float AlfaApagada = 0.28f;

        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color TurquesaSuave = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color Amarillo = new Color(1f, 0.92f, 0.25f, 1f);
        private static readonly Color Blanco = new Color(0.93f, 0.97f, 0.97f, 1f);
        private static readonly Color FondoCasilla = new Color(0.02f, 0.08f, 0.11f, 0.72f);
        private static readonly Color FondoResaltada = new Color(0.05f, 0.045f, 0.0f, 0.78f);
        private static readonly Color FondoCentro = new Color(0.02f, 0.06f, 0.09f, 0.80f);

        /// <summary>Nombre en la rueda de cada arma.</summary>
        public static string NombreArma(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Punos: return "PUÑOS";
                case ArmaAPie.Pistola: return "PISTOLA";
                case ArmaAPie.Escopeta: return "ESCOPETA";
                case ArmaAPie.Fusil: return "FUSIL";
                case ArmaAPie.Sniper: return "FRANCOTIRADOR";
                case ArmaAPie.Subfusil: return "SUBFUSIL";
                case ArmaAPie.Revolver: return "REVÓLVER";
                case ArmaAPie.Lanzagranadas: return "LANZAGRANADAS";
                case ArmaAPie.Granada: return "GRANADA";
                case ArmaAPie.RiflePlasma: return "RIFLE DE PLASMA";
                default: return arma.ToString().ToUpperInvariant();
            }
        }

        private sealed class Casilla
        {
            public RectTransform Raiz;
            public Image Fondo;
            public Image Borde;
            public Image Icono;
            public Text Nombre;
            public Text Cantidad;
            public CanvasGroup Grupo;
        }

        private static bool abierta;
        private static float escalaAnterior = 1f;
        private static int aperturas;

        private readonly List<Canvas> ocultos = new List<Canvas>();
        private readonly Dictionary<CanvasGroup, float> grupos = new Dictionary<CanvasGroup, float>();
        private Canvas canvas;
        private GameObject raiz;
        private readonly List<Casilla> casillas = new List<Casilla>();
        private List<int> disposicion = new List<int>();
        private RectTransform anillo;
        private Image bordeCentro;
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

        /// <summary>La rueda del jugador (la última que se armó), o null.</summary>
        public static RuedaArmasObjetos Actual { get; private set; }

        /// <summary>La casilla resaltada ahora (id de <see cref="ReglaRuedaArmas"/>) o -1.</summary>
        public int Resaltada => resaltada;

        /// <summary>Cuántos sectores tiene el anillo ahora.</summary>
        public int Sectores => disposicion.Count;

        /// <summary>Los ids de casilla de cada sector, en el sentido del reloj desde arriba (<see cref="ReglaRuedaArmas.Vacia"/> = vacío).</summary>
        public IReadOnlyList<int> Disposicion => disposicion;

        /// <summary>True si la casilla está en la rueda ahora (arma que tienes, objeto con piezas).</summary>
        public bool CasillaVisible(int casilla) => disposicion.IndexOf(casilla) >= 0;

        /// <summary>El ángulo (° en el sentido del reloj desde arriba) del sector de la casilla, o NaN si no está en la rueda.</summary>
        public float AnguloDe(int casilla)
        {
            int posicion = disposicion.IndexOf(casilla);
            return posicion < 0 ? float.NaN : ReglaRuedaArmas.AnguloDeSector(posicion, disposicion.Count);
        }

        /// <summary>El cursor virtual (px de referencia desde el centro, +y arriba).</summary>
        public Vector2 Cursor => cursor;

        public bool Armada => raiz != null;

        public Text TextoNombre => textoNombre;
        public Text TextoDetalle => textoDetalle;
        /// <summary>True si la casilla no se puede elegir (no está en la rueda o está apagada).</summary>
        public bool CasillaApagada(int casilla)
        {
            int posicion = disposicion.IndexOf(casilla);
            return posicion < 0 || casillas[posicion].Grupo.alpha < 0.5f;
        }

        /// <summary>El texto de cantidad de la casilla ("x2" en los objetos), o vacío si no está en la rueda.</summary>
        public string TextoCantidad(int casilla)
        {
            int posicion = disposicion.IndexOf(casilla);
            return posicion < 0 ? string.Empty : casillas[posicion].Cantidad.text;
        }

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
            Actual = this;
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
            if (Actual == this)
            {
                Actual = null;
            }
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
            resaltada = ReglaRuedaArmas.CasillaResaltada(cursor, disposicion, Disponible);
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
            Disponer();
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
            OcultarHermanos();
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
            foreach (KeyValuePair<CanvasGroup, float> g in grupos)
            {
                if (g.Key != null)
                {
                    g.Key.alpha = g.Value;
                }
            }
            grupos.Clear();
        }

        // T-1010: la rueda vive dentro del canvas del HUD, que no se puede apagar sin apagarla a ella: lo demás de ese canvas (minimapa, calavera,
        // barras) se vuelve transparente mientras la rueda está abierta.
        private void OcultarHermanos()
        {
            Canvas tope = null;
            for (Transform t = canvas.transform.parent; t != null; t = t.parent)
            {
                if (t.GetComponent<Canvas>() != null)
                {
                    tope = t.GetComponent<Canvas>();
                }
            }
            if (tope == null)
            {
                return;
            }
            // En cada nivel, del canvas de la rueda hasta el canvas de más arriba, se apagan los hermanos.
            for (Transform t = canvas.transform; t != tope.transform && t.parent != null; t = t.parent)
            {
                foreach (Transform hermano in t.parent)
                {
                    if (hermano == t || !hermano.gameObject.activeSelf)
                    {
                        continue;
                    }
                    CanvasGroup g = hermano.GetComponent<CanvasGroup>();
                    if (g == null)
                    {
                        g = hermano.gameObject.AddComponent<CanvasGroup>();
                    }
                    if (!grupos.ContainsKey(g))
                    {
                        grupos.Add(g, g.alpha);
                        g.alpha = 0f;
                    }
                }
            }
        }

        private bool Disponible(int casilla)
        {
            if (casilla == ReglaRuedaArmas.Vacia)
            {
                return false;
            }
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
            for (int i = 0; i < casillas.Count; i++)
            {
                Casilla c = casillas[i];
                int id = disposicion[i];
                bool ok = Disponible(id);
                bool resalto = id >= 0 && id == resaltada;
                c.Grupo.alpha = id == ReglaRuedaArmas.Vacia ? 0.6f : ok ? 1f : AlfaApagada;
                c.Fondo.color = resalto ? FondoResaltada : FondoCasilla;
                c.Borde.color = resalto ? Amarillo : new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.9f);
                Sprite icono = null;
                Material material = null;
                string cantidad = string.Empty;
                string nombre = string.Empty;
                if (ReglaRuedaArmas.EsArma(id))
                {
                    nombre = NombreArma((ArmaAPie)id);
                    if (piezas != null)
                    {
                        icono = piezas.ArmaHud(id);
                        material = piezas.MaterialArmaHud(id);
                    }
                }
                else if (ReglaRuedaArmas.EsObjeto(id))
                {
                    DatosConsumible o = ObjetoDe(id);
                    nombre = o != null ? o.Nombre.ToUpperInvariant() : "—";
                    icono = o != null && cfg != null ? cfg.Icono(o.Id) : null;
                    cantidad = "x" + CantidadDe(id);
                }
                c.Icono.sprite = icono;
                c.Icono.material = material != null ? material : c.Icono.defaultMaterial;
                c.Icono.color = resalto ? Amarillo : Turquesa;
                c.Icono.enabled = icono != null;
                c.Nombre.text = nombre;
                c.Nombre.color = resalto ? Amarillo : TurquesaSuave;
                c.Cantidad.text = cantidad;
                c.Cantidad.color = resalto ? Amarillo : Blanco;
            }
            // El sector resaltado va encima de los demás para que su brillo no quede tapado.
            int lugar = disposicion.IndexOf(resaltada);
            if (lugar >= 0)
            {
                casillas[lugar].Raiz.SetAsLastSibling();
            }
            punto.anchoredPosition = cursor;
            bordeCentro.color = resaltada >= 0 ? Amarillo : Turquesa;
            textoNombre.color = Amarillo;
            textoDetalle.color = Amarillo;
            if (resaltada < 0)
            {
                textoNombre.text = string.Empty;
                textoDetalle.color = TurquesaSuave;
                textoDetalle.fontSize = 24;
                textoDetalle.text = "SUELTA PARA CERRAR";
                return;
            }
            if (ReglaRuedaArmas.EsArma(resaltada))
            {
                textoNombre.text = NombreArma((ArmaAPie)resaltada);
                string balas = BalasDe((ArmaAPie)resaltada);
                textoDetalle.fontSize = balas.Length > 0 ? 40 : 24;
                textoDetalle.text = balas.Length > 0 ? balas.Replace("|", " / ") : "CUERPO A CUERPO";
            }
            else
            {
                DatosConsumible o = ObjetoDe(resaltada);
                textoNombre.text = o != null ? o.Nombre.ToUpperInvariant() : string.Empty;
                textoDetalle.fontSize = 22;
                textoDetalle.text = o != null ? o.Descripcion.ToUpperInvariant() : string.Empty;
            }
            textoNombre.fontSize = textoNombre.text.Length > 10 ? 34 : 46;
        }

        // ---- Armado ----

        private void Armar()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasRueda", OrdenCanvas);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            raiz = new GameObject("Rueda", typeof(RectTransform));
            raiz.transform.SetParent(canvas.transform, false);
            KitUI.Estirar((RectTransform)raiz.transform);

            Image fondo = KitUI.CrearImagen(raiz.transform, "Fondo", new Color(0f, 0.01f, 0.03f, AlfaFondo));
            fondo.raycastTarget = false;
            KitUI.Estirar(fondo.rectTransform);

            anillo = Nuevo(raiz.transform, "Anillo", Vector2.zero, Vector2.zero);

            // El octágono del centro: nombre y balas de lo resaltado (M30).
            DibujoRueda.Par octagono = DibujoRueda.Octagono(ApotemaCentro, GrosorBorde, BrilloBorde);
            Figura(raiz.transform, "CentroRelleno", octagono.Relleno, FondoCentro);
            bordeCentro = Figura(raiz.transform, "CentroBorde", octagono.Borde, Turquesa);

            textoNombre = Texto(raiz.transform, "Nombre", new Vector2(0f, 30f), new Vector2(330f, 60f), 46, Amarillo);
            textoDetalle = Texto(raiz.transform, "Detalle", new Vector2(0f, -32f), new Vector2(300f, 90f), 40, Amarillo);
            textoDetalle.verticalOverflow = VerticalWrapMode.Truncate;
            textoDetalle.horizontalOverflow = HorizontalWrapMode.Wrap;

            punto = (RectTransform)KitUI.CrearRombo(raiz.transform, "Cursor", 18f, Blanco).rectTransform.parent;
            punto.anchorMin = punto.anchorMax = new Vector2(0.5f, 0.5f);
            raiz.SetActive(false);
        }

        // Arma la lista de sectores con lo que el jugador lleva ahora y rehace el anillo si cambió.
        private void Disponer()
        {
            Vincular();
            List<int> nueva = ReglaRuedaArmas.Disposicion(Disponible);
            bool igual = nueva.Count == disposicion.Count && casillas.Count == nueva.Count;
            for (int i = 0; igual && i < nueva.Count; i++)
            {
                igual = nueva[i] == disposicion[i];
            }
            if (igual)
            {
                return;
            }
            disposicion = nueva;
            foreach (Casilla c in casillas)
            {
                Destroy(c.Raiz.gameObject);
            }
            casillas.Clear();
            DibujoRueda.Par sector = DibujoRueda.Sector(RadioInterior, RadioExterior, ReglaRuedaArmas.GradosPorSector(disposicion.Count),
                HuecoSectores, GrosorBorde, BrilloBorde);
            for (int i = 0; i < disposicion.Count; i++)
            {
                casillas.Add(ArmarCasilla(i, disposicion.Count, sector));
            }
        }

        private Casilla ArmarCasilla(int posicion, int total, DibujoRueda.Par sector)
        {
            Casilla c = new Casilla();
            float grados = ReglaRuedaArmas.AnguloDeSector(posicion, total);
            GameObject g = new GameObject("Casilla" + posicion, typeof(RectTransform));
            g.transform.SetParent(anillo, false);
            c.Raiz = (RectTransform)g.transform;
            KitUI.Colocar(c.Raiz, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            c.Grupo = g.AddComponent<CanvasGroup>();
            c.Grupo.blocksRaycasts = false;
            c.Fondo = Figura(c.Raiz, "Relleno", sector.Relleno, FondoCasilla);
            c.Borde = Figura(c.Raiz, "Borde", sector.Borde, Turquesa);
            c.Fondo.rectTransform.localEulerAngles = new Vector3(0f, 0f, -grados);
            c.Borde.rectTransform.localEulerAngles = new Vector3(0f, 0f, -grados);

            float rad = grados * Mathf.Deg2Rad;
            Vector2 centro = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * RadioIcono;
            // Como el mockup: en los sectores de arriba el nombre va sobre el icono; en los demás, debajo.
            float lado = Mathf.Cos(rad) > 0.5f ? 1f : -1f;
            float ancho = 2f * RadioIcono * Mathf.Sin(Mathf.PI / total) - 14f;
            RectTransform ri = Nuevo(c.Raiz, "Icono", centro - new Vector2(0f, lado * 10f), new Vector2(Mathf.Min(150f, ancho), 74f));
            c.Icono = ri.gameObject.AddComponent<Image>();
            c.Icono.preserveAspect = true;
            c.Icono.raycastTarget = false;
            c.Icono.color = Turquesa;
            int letra = total > 10 ? 21 : 28;
            c.Nombre = Texto(c.Raiz, "Nombre", centro + new Vector2(0f, lado * SeparacionTexto), new Vector2(ancho + 30f, 26f), letra, TurquesaSuave);
            c.Cantidad = Texto(c.Raiz, "Cantidad", centro + new Vector2(ancho * 0.32f, -lado * 34f - 6f), new Vector2(60f, 28f), 22, Blanco);
            return c;
        }

        // Una imagen con un sprite de DibujoRueda, del tamaño del sprite y con el pivote en el centro de la rueda.
        private static Image Figura(Transform padre, string nombre, Sprite sprite, Color color)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform));
            g.transform.SetParent(padre, false);
            RectTransform r = (RectTransform)g.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = sprite.rect.size;
            r.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            r.anchoredPosition = Vector2.zero;
            Image imagen = g.AddComponent<Image>();
            imagen.sprite = sprite;
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
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
