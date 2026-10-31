using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Opciones;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Titulo
{
    /// <summary>
    /// El título (T-413, D-tony-245), hecho con el kit en un canvas de orden 80, sobre la ciudad de noche que recorre CamaraTitulo:
    /// el nombre del juego (<see cref="NombreJuego"/>) de 96 px en naranja con sombra magenta (o el PNG del logo si se pone en
    /// <see cref="logo"/>), y NUEVA PARTIDA, CONTINUAR, OPCIONES y SALIR. CONTINUAR carga la ranura más reciente y está desactivado si no
    /// hay ninguna; si hay, abajo sale su resumen en turquesa. Solo se muestra si <see cref="FlujoPartida.EnTitulo"/>: entonces apaga los
    /// demás canvas (HUD, minimapa, marcador...), deja quieto y oculto al jugador y se pone en <see cref="PilaPantallas"/>, así Esc no
    /// pausa ni M abre el mapa. Nada se restaura: EMPEZAR y CONTINUAR recargan la escena.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class PantallaTitulo : MonoBehaviour
    {
        public const int OrdenCanvas = 80;
        public const string TextoNueva = "NUEVA PARTIDA";
        public const string TextoContinuar = "CONTINUAR";
        public const string TextoOpciones = "OPCIONES";
        public const string TextoCreditos = "CRÉDITOS";
        public const string TextoSalir = "SALIR";
        public const string TextoAyuda = "↑ ↓ ELEGIR · ENTER ACEPTAR";
        public const string TextoSinPartidas = "SIN PARTIDAS GUARDADAS";
        public const string SonidoInicio = "UI_TITULO_INICIO";
        private const int CantidadBotones = 5;
        private const float SegundosRevisar = 0.5f;

        [Header("Logo")]
        [Tooltip("PNG del logo cuando llegue; vacío usa el nombre del juego en texto.")]
        [SerializeField] private Sprite logo;
        [Tooltip("Tamaño del logo o del nombre (px a 1920x1080).")]
        [SerializeField] private Vector2 tamanoLogo = new Vector2(1000f, 140f);
        [Tooltip("Distancia del logo al borde de arriba (px).")]
        [Min(0f)] [SerializeField] private float margenLogo = 150f;
        [Tooltip("Corrimiento de la sombra magenta del nombre (px).")]
        [SerializeField] private Vector2 sombraLogo = new Vector2(5f, -5f);

        [Header("Medidas (px a 1920x1080)")]
        [Tooltip("Ancho del panel del menú (el resumen de CONTINUAR cabe en él).")]
        [Min(0f)] [SerializeField] private float anchoPanel = 760f;
        [Tooltip("Alto del renglón del resumen bajo CONTINUAR.")]
        [Min(0f)] [SerializeField] private float altoResumen = 30f;
        [Tooltip("Cuánto baja el panel del centro de la pantalla (px).")]
        [SerializeField] private float bajarPanel = 60f;
        [Tooltip("Opacidad del oscurecido de toda la pantalla (0 a 1), para leer sobre la ciudad.")]
        [Range(0f, 1f)] [SerializeField] private float alfaVelo = 0.3f;
        [Tooltip("Opacidad del panel del menú (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float alfaPanel = 0.9f;

        [Header("Piezas (las arma el creador con el kit)")]
        [SerializeField] private GameObject contenido;
        [SerializeField] private MenuCyberfix menu;
        [SerializeField] private Text nombre;
        [SerializeField] private Image imagenLogo;
        [SerializeField] private BotonCyberfix nueva;
        [SerializeField] private BotonCyberfix continuar;
        [SerializeField] private Text resumen;
        [SerializeField] private BotonCyberfix opciones;
        [SerializeField] private BotonCyberfix creditos;
        [SerializeField] private BotonCyberfix salir;

        private PantallaCreacionPandilla creacion;
        private PantallaOpciones pantallaOpciones;
        private int ranuraContinuar = -1;
        private BotonCyberfix volverA;
        private float relojRevisar;
        private bool activo;
        private readonly List<Transform> propios = new List<Transform>();
        private readonly List<Canvas> apagados = new List<Canvas>();

        public bool Abierta => contenido != null && contenido.activeSelf;
        /// <summary>True mientras la escena está en el título (aunque encima esté la creación o las opciones).</summary>
        public bool Activo => activo;
        public MenuCyberfix Menu => menu;
        public Text Nombre => nombre;
        public BotonCyberfix Nueva => nueva;
        public BotonCyberfix Continuar => continuar;
        public Text Resumen => resumen;
        public BotonCyberfix Opciones => opciones;
        public BotonCyberfix Creditos => creditos;
        public BotonCyberfix Salir => salir;

        /// <summary>
        /// Se dispara al pulsar CRÉDITOS; el argumento es lo que hay que llamar al cerrar la pantalla de créditos. La pantalla de créditos
        /// (ensamblado aparte que depende de UI) se engancha desde fuera para no hacer un ciclo de ensamblados (T-440).
        /// </summary>
        public static event System.Action<System.Action> PidioCreditos;
        public int RanuraContinuar => ranuraContinuar;
        public Canvas Canvas => contenido != null ? contenido.GetComponent<Canvas>() : null;

        public void Armar()
        {
            EstiloCyberfix e = KitUI.Estilo;
            float relleno = e.Relleno;
            float separacion = e.SeparacionBotones;
            Vector2 boton = e.TamanoBotonMenu;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasTitulo", OrdenCanvas);
            contenido = canvas.gameObject;
            Image velo = KitUI.CrearFondoPantalla(canvas.transform, "Velo");
            Color colorVelo = velo.color;
            colorVelo.a = alfaVelo;
            velo.color = colorVelo;

            nombre = KitUI.CrearTexto(canvas.transform, "Nombre", RolTexto.Titulo, TextAnchor.MiddleCenter);
            KitUI.Colocar(nombre.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -margenLogo), tamanoLogo);
            nombre.text = NombreJuego.Nombre;
            nombre.color = e.Naranja;
            Shadow magenta = nombre.gameObject.AddComponent<Shadow>();
            magenta.effectColor = e.Magenta;
            magenta.effectDistance = sombraLogo;
            imagenLogo = KitUI.CrearImagen(canvas.transform, "Logo", Color.white);
            KitUI.Colocar(imagenLogo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -margenLogo), tamanoLogo);
            imagenLogo.preserveAspect = true;
            imagenLogo.gameObject.SetActive(false);

            float altoBotones = CantidadBotones * boton.y + (CantidadBotones - 1) * separacion + altoResumen;
            Vector2 tamanoPanel = new Vector2(anchoPanel, relleno + altoBotones + e.SeparacionBloques + e.AltoFilaHud + relleno);
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", tamanoPanel);
            panel.anchoredPosition = new Vector2(0f, -bajarPanel);
            Image fondo = panel.GetComponent<Image>();
            Color colorPanel = fondo.color;
            colorPanel.a = Mathf.Max(colorPanel.a, alfaPanel);
            fondo.color = colorPanel;
            menu = panel.gameObject.AddComponent<MenuCyberfix>();

            float y = -relleno;
            nueva = Boton(panel, TextoNueva, ref y);
            continuar = Boton(panel, TextoContinuar, ref y);
            y += separacion;
            resumen = KitUI.CrearTexto(panel, "Resumen", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(resumen.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(anchoPanel - 2f * relleno, altoResumen));
            resumen.color = e.Turquesa;
            y -= altoResumen + separacion;
            opciones = Boton(panel, TextoOpciones, ref y);
            creditos = Boton(panel, TextoCreditos, ref y);
            salir = Boton(panel, TextoSalir, ref y);

            Text ayuda = KitUI.CrearTexto(panel, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(anchoPanel, e.AltoFilaHud));
            ayuda.text = TextoAyuda;
            ayuda.color = e.Turquesa;
            contenido.SetActive(false);
        }

        private static BotonCyberfix Boton(RectTransform panel, string texto, ref float y)
        {
            EstiloCyberfix e = KitUI.Estilo;
            BotonCyberfix b = KitUI.CrearBoton(panel, texto, null);
            b.Texto.alignment = TextAnchor.MiddleCenter;
            KitUI.Colocar((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), e.TamanoBotonMenu);
            y -= e.TamanoBotonMenu.y + e.SeparacionBotones;
            return b;
        }

        private void Awake()
        {
            if (contenido == null || menu == null || nueva == null || continuar == null || opciones == null || creditos == null || salir == null)
            {
                Armar();
            }
            nueva.AlPulsar.AddListener(AbrirCreacion);
            continuar.AlPulsar.AddListener(ContinuarPartida);
            opciones.AlPulsar.AddListener(AbrirOpciones);
            creditos.AlPulsar.AddListener(AbrirCreditos);
            salir.AlPulsar.AddListener(SalirDelJuego);
            contenido.SetActive(false);
        }

        private void Start()
        {
            if (!FlujoPartida.EnTitulo)
            {
                enabled = false;
                return;
            }
            activo = true;
            creacion = FindFirstObjectByType<PantallaCreacionPandilla>(FindObjectsInactive.Include);
            if (creacion != null)
            {
                creacion.Cerrada += AlCerrarEncima;
            }
            pantallaOpciones = FindFirstObjectByType<PantallaOpciones>(FindObjectsInactive.Include);
            if (pantallaOpciones != null)
            {
                pantallaOpciones.Cerrada += AlCerrarEncima;
            }
            propios.Clear();
            propios.Add(transform);
            if (creacion != null) propios.Add(creacion.transform);
            if (pantallaOpciones != null) propios.Add(pantallaOpciones.transform);
            if (logo != null)
            {
                imagenLogo.sprite = logo;
                imagenLogo.gameObject.SetActive(true);
                nombre.gameObject.SetActive(false);
            }
            PilaPantallas.Abrir(this);
            Apagar();
            Mostrar();
            Sonidos.Reproducir(SonidoInicio);
            Debug.Log("[UI] Título abierto.");
        }

        private void OnDestroy()
        {
            if (creacion != null)
            {
                creacion.Cerrada -= AlCerrarEncima;
            }
            if (pantallaOpciones != null)
            {
                pantallaOpciones.Cerrada -= AlCerrarEncima;
            }
            PilaPantallas.Cerrar(this);
        }

        private void Update()
        {
            if (!activo)
            {
                return;
            }
            relojRevisar -= Time.unscaledDeltaTime;
            if (relojRevisar <= 0f)
            {
                relojRevisar = SegundosRevisar;
                Apagar();
            }
        }

        /// <summary>Muestra el menú del título con CONTINUAR al día.</summary>
        public void Mostrar()
        {
            RefrescarContinuar();
            contenido.SetActive(true);
            menu.enabled = true;
            menu.Recargar();
        }

        private void RefrescarContinuar()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            ranuraContinuar = servicio != null ? servicio.RanuraMasReciente() : -1;
            continuar.interactable = ranuraContinuar >= 0;
            resumen.text = ranuraContinuar >= 0 ? FormatoRanura.Linea(servicio.ResumenDe(ranuraContinuar)) : TextoSinPartidas;
            resumen.color = ranuraContinuar >= 0 ? KitUI.Estilo.Turquesa : KitUI.Estilo.Desactivado;
        }

        // HUD, minimapa, marcador y demás canvas fuera; el jugador quieto, sin interactuar ni pelear, y sin verse.
        private void Apagar()
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Los letreros del mundo (World Space) se quedan: son parte de la ciudad de fondo.
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace && !EsPropio(canvas.transform) && !apagados.Contains(canvas))
                {
                    apagados.Add(canvas);
                }
            }
            ApagarCanvas();
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return;
            }
            Transform raiz = jugador.Raiz;
            ControladorAPie controlador = raiz.GetComponentInChildren<ControladorAPie>();
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
            }
            Apagar(raiz.GetComponentsInChildren<InteraccionAPie>());
            Apagar(raiz.GetComponentsInChildren<GolpeAPie>());
            Apagar(raiz.GetComponentsInChildren<PistolaAPie>());
            Apagar(raiz.GetComponentsInChildren<ArmasAPie>());
            foreach (Renderer render in raiz.GetComponentsInChildren<Renderer>())
            {
                render.enabled = false;
            }
        }

        // Cada cuadro, después de todos (orden 1000): hay HUD que prenden su canvas solos (el minimapa).
        private void LateUpdate()
        {
            if (activo)
            {
                ApagarCanvas();
            }
        }

        private void ApagarCanvas()
        {
            for (int i = apagados.Count - 1; i >= 0; i--)
            {
                Canvas canvas = apagados[i];
                if (canvas == null)
                {
                    apagados.RemoveAt(i);
                }
                else if (canvas.enabled)
                {
                    canvas.enabled = false;
                }
            }
        }

        private static void Apagar(MonoBehaviour[] componentes)
        {
            foreach (MonoBehaviour c in componentes)
            {
                c.enabled = false;
            }
        }

        private bool EsPropio(Transform t)
        {
            foreach (Transform propio in propios)
            {
                if (propio != null && t.IsChildOf(propio))
                {
                    return true;
                }
            }
            return false;
        }

        private void AbrirCreacion()
        {
            if (creacion == null)
            {
                Debug.LogWarning("[UI] PantallaTitulo sin PantallaCreacionPandilla en la escena.", this);
                return;
            }
            volverA = nueva;
            Esconder();
            creacion.Abrir();
        }

        private void ContinuarPartida()
        {
            if (ranuraContinuar < 0)
            {
                return;
            }
            Debug.Log("[UI] CONTINUAR carga " + FormatoRanura.Nombre(ranuraContinuar) + ".");
            menu.enabled = false;
            FlujoPartida.IrACargar(ranuraContinuar);
        }

        private void AbrirOpciones()
        {
            if (pantallaOpciones == null)
            {
                return;
            }
            // Las opciones (orden 70) van debajo del título (80): el título se esconde mientras.
            volverA = opciones;
            pantallaOpciones.Abrir();
            Esconder();
        }

        private void AbrirCreditos()
        {
            if (PidioCreditos == null)
            {
                return;
            }
            volverA = creditos;
            Esconder();
            PidioCreditos(AlCerrarEncima);
        }

        private void Esconder()
        {
            menu.enabled = false;
            contenido.SetActive(false);
        }

        // La creación o las opciones se cerraron: vuelve el menú con el botón que las abrió elegido.
        private void AlCerrarEncima()
        {
            if (!activo || !FlujoPartida.EnTitulo || Abierta)
            {
                return;
            }
            Mostrar();
            if (volverA != null)
            {
                volverA.Elegir();
                volverA = null;
            }
        }

        private static void SalirDelJuego()
        {
            Debug.Log("[UI] SALIR desde el título.");
            Application.Quit();
        }
    }
}
