using System;
using System.Linq;
using DG.Tweening;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    public enum EstadoInicio
    {
        Principal,
        Opciones,
        Creacion,
        Creditos,
        Cargando,
    }

    /// <summary>
    /// La pantalla de inicio: arma por código el logo, el menú (CONTINUAR, NUEVA PARTIDA o JUGAR, OPCIONES, CRÉDITOS y SALIR), las
    /// opciones y los adornos de interfaz sobre el fondo 3D (<see cref="FondoCiudad3D"/>), y lleva la máquina de estados entre ellos con
    /// transiciones. CONTINUAR carga la ranura más reciente; NUEVA PARTIDA abre la creación de personaje (la de siempre). Las cargas de
    /// <see cref="FlujoPartida"/> se interceptan para hacerlas asíncronas con la <see cref="PantallaCargando"/> (consejos y barra real).
    /// Funciona con teclado, ratón y control: la navegación la hace el EventSystem y Esc o B regresan.
    /// </summary>
    public class MenuInicio : MonoBehaviour
    {
        public const int OrdenCanvas = 20;
        public const string TextoContinuar = "CONTINUAR";
        public const string TextoNueva = "NUEVA PARTIDA";
        public const string TextoJugar = "JUGAR";
        public const string TextoCargar = "CARGAR";
        public const string TextoOpciones = "OPCIONES";
        public const string TextoCreditos = "CRÉDITOS";
        public const string TextoSalir = "SALIR";
        public const string TextoSinPartida = "SIN PARTIDA GUARDADA";
        public const string AyudaTeclado = "↑ ↓ ELEGIR     ENTER ACEPTAR     ESC VOLVER";
        public const string AyudaControl = "↑ ↓ ELEGIR     A ACEPTAR     B VOLVER";
        public const string SonidoInicio = "UI_TITULO_INICIO";

        private const float SegundosEntrada = 1.7f;
        private const float EsperaPrimerBoton = 0.9f;
        private const float MargenIzquierdo = TrazoH10.MargenPantalla;
        private const float AltoBoton = 58f;
        private const float AltoBotonConResumen = 86f;
        private const float AnchoBoton = 520f;
        private const float SeparacionBotones = 4f;
        private const int TamanoTextoBoton = 38;
        private const float BaseMenu = 100f;
        private const float RellenoMenu = 24f;
        private const float AltoCabeceraMenu = 40f;
        private const float AnchoLogo = 760f;
        private const float AnchoDatos = 560f;
        private const float AltoDatos = 250f;
        private const float SegundosAviso = 0.25f;
        private const float EscalonBotones = 0.05f;
        private const string RutaLogo = "Inicio/Logo";
        private const string TituloMenu = "MENÚ PRINCIPAL";
        private const string TituloPartida = "ÚLTIMA PARTIDA";
        private const string TituloConsejo = "CONSEJO";
        private const string SinPartidaDetalle = "Todavía no hay una partida guardada.";

        [SerializeField] private FondoCiudad3D fondo;
        [SerializeField] private PantallaCreacionPandilla creacion;

        private Canvas canvas;
        private CanvasGroup grupoMenu;
        private Image velo;
        private RectTransform raizPrincipal;
        private PanelAnimado panelPrincipal;
        private PanelAnimado panelOpciones;
        private PanelOpcionesInicio opciones;
        private LogoInicio logo;
        private Text ayuda;
        private Text reloj;
        private BotonInicio continuar;
        private BotonInicio nueva;
        private BotonInicio cargar;
        private FondoCallesInicio fondoCalles;
        private PantallaRanuras ranuras;
        private CanvasGroup aviso;
        private Text textoAviso;
        private Image panelMenu;
        private Image panelDatos;
        private Text datoPartida;
        private BotonInicio botonOpciones;
        private BotonInicio creditos;
        private BotonInicio salir;
        private BotonInicio ultimoElegido;
        private int ranuraContinuar = -1;
        private float tiempo;
        private bool primerBotonElegido;
        private bool controlActivo;
        private bool ayudaDeControl;
        private string rutaPedida;

        public EstadoInicio Estado { get; private set; }
        public BotonInicio Continuar => continuar;
        public BotonInicio Nueva => nueva;
        public BotonInicio Cargar => cargar;
        public FondoCallesInicio FondoCalles => fondoCalles;
        public bool AvisoVisible => aviso != null && aviso.gameObject.activeSelf;
        public string TextoDelAviso => textoAviso != null ? textoAviso.text : string.Empty;
        public BotonInicio BotonOpciones => botonOpciones;
        public BotonInicio Creditos => creditos;
        public BotonInicio Salir => salir;
        public PanelOpcionesInicio Opciones => opciones;
        public PantallaCreacionPandilla Creacion => creacion;
        public LogoInicio Logo => logo;
        public FondoCiudad3D Fondo => fondo;
        public int RanuraContinuar => ranuraContinuar;
        public bool HayPartida => ranuraContinuar >= 0;
        public Canvas Canvas => canvas;
        public Text Ayuda => ayuda;

        /// <summary>La escena que se pidió cargar (la intercepción de <see cref="FlujoPartida"/>); null si no se ha pedido ninguna.</summary>
        public string RutaPedida => rutaPedida;

        /// <summary>Pruebas: no carga la escena de verdad, solo anota qué se pidió.</summary>
        public bool SoloAnotarCarga { get; set; }

        /// <summary>Pruebas: evita cerrar el juego de verdad.</summary>
        public static Action<string> AlSalir;

        private void Awake()
        {
            KitUI.AsegurarEventSystem();
            Armar();
        }

        private void OnEnable()
        {
            FlujoPartida.Interceptar(AlPedirEscena);
        }

        private void OnDisable()
        {
            FlujoPartida.Interceptar(null);
        }

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (creacion != null)
            {
                creacion.Cerrada += AlCerrarCreacion;
            }
            RefrescarPartida();
            panelPrincipal.FijarYa(true);
            panelOpciones.FijarYa(false);
            AnimarEntrada();
            Sonidos.Reproducir(SonidoInicio);
            Debug.Log("[UI] Pantalla de inicio abierta.");
        }

        private void OnDestroy()
        {
            if (creacion != null)
            {
                creacion.Cerrada -= AlCerrarCreacion;
            }
        }

        // ------------------------------------------------------------------ armado

        private void Armar()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasInicio", OrdenCanvas);
            grupoMenu = canvas.gameObject.AddComponent<CanvasGroup>();
            Transform c = canvas.transform;

            // Ambiente de pantalla: sombra a la izquierda para leer el menú, viñetas arriba y abajo.
            Image sombra = UIInicio.Imagen(c, "SombraMenu", UIInicio.ConAlfa(UIInicio.Noche, 0.6f), SpritesInicio.DegradadoH);
            UIInicio.Anclar(sombra.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1000f, 0f));
            Image arriba = UIInicio.Imagen(c, "VinetaArriba", UIInicio.ConAlfa(Color.black, 0.55f), SpritesInicio.DegradadoV);
            UIInicio.Anclar(arriba.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 260f));
            Image abajo = UIInicio.Imagen(c, "VinetaAbajo", UIInicio.ConAlfa(Color.black, 0.65f), SpritesInicio.DegradadoVAbajo);
            UIInicio.Anclar(abajo.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 300f));
            ArmarMarco(c);
            fondoCalles = FondoCallesInicio.Crear(c);
            fondoCalles.CalleRealLista += () =>
            {
                if (fondo != null)
                {
                    fondo.gameObject.SetActive(false); // T-898: la ciudad real sustituye a la avenida generada
                }
            };

            logo = LogoInicio.Crear(c, NombreJuego.Nombre, 210, new Vector2(MargenIzquierdo, -96f));
            logo.AsignarSprite(Resources.Load<Sprite>(RutaLogo));
            logo.GetComponentsInChildren<Image>(true).First(i => i.name == "LogoPng").rectTransform.sizeDelta = new Vector2(AnchoLogo, 0f);

            raizPrincipal = UIInicio.Nodo(c, "Principal");
            UIInicio.Estirar(raizPrincipal);
            panelPrincipal = PanelAnimado.Agregar(raizPrincipal, new Vector2(-120f, 0f), 0.4f);
            ArmarPanel();
            continuar = Boton(TextoContinuar, TrazoH10.Turquesa);
            nueva = Boton(TextoNueva, TrazoH10.Turquesa);
            cargar = Boton(TextoCargar, TrazoH10.Turquesa);
            botonOpciones = Boton(TextoOpciones, TrazoH10.Turquesa);
            creditos = Boton(TextoCreditos, TrazoH10.Turquesa);
            salir = Boton(TextoSalir, TrazoH10.Turquesa);
            continuar.AgregarSubtexto(TextoSinPartida, TrazoH10.TamanoMicro, UIInicio.TextoApagado);
            continuar.AlPulsar.AddListener(ContinuarPartida);
            nueva.AlPulsar.AddListener(NuevaPartida);
            cargar.AlPulsar.AddListener(AbrirCargar);
            botonOpciones.AlPulsar.AddListener(AbrirOpciones);
            creditos.AlPulsar.AddListener(AbrirCreditos);
            salir.AlPulsar.AddListener(SalirDelJuego);
            foreach (BotonInicio b in new[] { continuar, nueva, cargar, botonOpciones, creditos, salir })
            {
                b.Elegido += AlElegir;
            }

            opciones = PanelOpcionesInicio.Crear(c);
            panelOpciones = PanelAnimado.Agregar((RectTransform)opciones.transform, new Vector2(140f, 0f), 0.4f);
            opciones.PidioVolver += CerrarOpciones;

            ArmarDatos();

            TrazoH10.Recta(c, "ReglaPie", TrazoH10.TurquesaTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 72f), new Vector2(-2f * MargenIzquierdo, TrazoH10.GrosorLinea));
            ayuda = TrazoH10.Etiqueta(c, "Ayuda", AyudaTeclado, TrazoH10.TamanoDato, TrazoH10.TextoTenue);
            UIInicio.Punto(ayuda.rectTransform, new Vector2(0f, 0f), new Vector2(MargenIzquierdo, 30f), new Vector2(900f, 34f));

            ArmarAviso(c);

            velo = UIInicio.Imagen(c, "Entrada", Color.black);
            UIInicio.Estirar(velo.rectTransform);
        }

        /// <summary>El panel compacto del menú, abajo a la izquierda; su alto se ajusta en <see cref="RefrescarPartida"/>.</summary>
        private void ArmarPanel()
        {
            panelMenu = TrazoH10.Panel(raizPrincipal, "PanelMenu", TrazoH10.TurquesaTenue);
            TrazoH10.Seccion(panelMenu.transform, "TituloMenu", TituloMenu, new Vector2(RellenoMenu, -RellenoMenu * 0.5f), AnchoBoton - RellenoMenu);
        }

        /// <summary>Datos reales a la derecha: la última partida guardada y un consejo del juego.</summary>
        private void ArmarDatos()
        {
            panelDatos = TrazoH10.Panel(raizPrincipal, "PanelDatos", TrazoH10.TurquesaTenue);
            UIInicio.Punto(panelDatos.rectTransform, new Vector2(1f, 0f), new Vector2(-MargenIzquierdo, BaseMenu), new Vector2(AnchoDatos, AltoDatos));
            float ancho = AnchoDatos - 2f * RellenoMenu;
            TrazoH10.Seccion(panelDatos.transform, "TituloPartida", TituloPartida, new Vector2(RellenoMenu, -RellenoMenu * 0.5f), ancho);
            datoPartida = TrazoH10.Etiqueta(panelDatos.transform, "DatoPartida", string.Empty, TrazoH10.TamanoDato, TrazoH10.Texto, TextAnchor.UpperLeft);
            UIInicio.Punto(datoPartida.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoMenu, -58f), new Vector2(ancho, 60f));
            datoPartida.horizontalOverflow = HorizontalWrapMode.Wrap;
            TrazoH10.Seccion(panelDatos.transform, "TituloConsejo", TituloConsejo, new Vector2(RellenoMenu, -128f), ancho);
            Text consejo = TrazoH10.Etiqueta(panelDatos.transform, "Consejo", ConsejosCarga.Obtener(DateTime.Now.DayOfYear), TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.UpperLeft);
            UIInicio.Punto(consejo.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoMenu, -166f), new Vector2(ancho, 64f));
            consejo.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        /// <summary>Recuadro de aviso al centro, en coral porque es una alerta. Se cierra con un clic.</summary>
        private void ArmarAviso(Transform c)
        {
            Image fondoAviso = TrazoH10.Panel(c, "Aviso", TrazoH10.Coral, TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.94f));
            UIInicio.Punto(fondoAviso.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 180f));
            fondoAviso.raycastTarget = true;
            textoAviso = TrazoH10.Etiqueta(fondoAviso.rectTransform, "Texto", string.Empty, TrazoH10.TamanoSeccion, TrazoH10.Texto, TextAnchor.MiddleCenter);
            UIInicio.Estirar(textoAviso.rectTransform);
            aviso = fondoAviso.gameObject.AddComponent<CanvasGroup>();
            fondoAviso.gameObject.AddComponent<Button>().onClick.AddListener(CerrarAviso);
            aviso.gameObject.SetActive(false);
        }

        /// <summary>T-893: muestra un aviso (por ejemplo, "LA PARTIDA GUARDADA ESTÁ DAÑADA").</summary>
        public void MostrarAviso(string texto)
        {
            textoAviso.text = texto;
            aviso.DOKill();
            aviso.gameObject.SetActive(true);
            aviso.alpha = 0f;
            DOTween.To(() => aviso.alpha, v => aviso.alpha = v, 1f, SegundosAviso).SetTarget(aviso).SetUpdate(true);
        }

        public void CerrarAviso()
        {
            aviso.DOKill();
            DOTween.To(() => aviso.alpha, v => aviso.alpha = v, 0f, SegundosAviso).SetTarget(aviso).SetUpdate(true).OnComplete(() => aviso.gameObject.SetActive(false));
        }

        /// <summary>T-893: CARGAR abre la pantalla de ranuras de siempre; al elegir una, <see cref="FlujoPartida"/> carga la escena.</summary>
        private void AbrirCargar()
        {
            if (Estado != EstadoInicio.Principal)
            {
                return;
            }
            if (ranuras == null)
            {
                ranuras = new GameObject("PantallaRanuras").AddComponent<PantallaRanuras>();
            }
            ranuras.Abrir(ModoRanuras.Cargar);
        }

        private void ArmarMarco(Transform c)
        {
            TrazoH10.Recta(c, "ReglaTitulo", TrazoH10.TurquesaTenue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -72f), new Vector2(-2f * MargenIzquierdo, TrazoH10.GrosorLinea));
            RectTransform regla = TrazoH10.Regla(c, "Regla", 41, 12f, 12f, TrazoH10.TurquesaTenue);
            UIInicio.Punto(regla, new Vector2(1f, 1f), new Vector2(-MargenIzquierdo, -74f), regla.sizeDelta);
            reloj = TrazoH10.Etiqueta(c, "Reloj", string.Empty, TrazoH10.TamanoDato, TrazoH10.Turquesa, TextAnchor.MiddleRight);
            UIInicio.Punto(reloj.rectTransform, new Vector2(1f, 1f), new Vector2(-MargenIzquierdo, -32f), new Vector2(620f, 34f));
        }

        private BotonInicio Boton(string texto, Color acento)
        {
            BotonInicio b = BotonInicio.Crear(raizPrincipal, "Boton" + texto, texto, new Vector2(AnchoBoton, AltoBoton), TamanoTextoBoton, TextAnchor.MiddleLeft, acento);
            return b;
        }

        // ------------------------------------------------------------------ partida guardada

        /// <summary>Mira la ranura más reciente y acomoda los botones (con partida: CONTINUAR y NUEVA PARTIDA; sin ella: JUGAR).</summary>
        public void RefrescarPartida()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            ranuraContinuar = servicio != null ? servicio.RanuraMasReciente() : -1;
            bool hay = ranuraContinuar >= 0;
            continuar.gameObject.SetActive(hay);
            cargar.gameObject.SetActive(hay);
            if (hay)
            {
                continuar.FijarSubtexto(FormatoRanura.Linea(servicio.ResumenDe(ranuraContinuar)), UIInicio.Cian);
            }
            nueva.Etiqueta = hay ? TextoNueva : TextoJugar;
            datoPartida.text = hay ? FormatoRanura.Linea(servicio.ResumenDe(ranuraContinuar)) : SinPartidaDetalle;
            BotonInicio[] todos = { continuar, nueva, cargar, botonOpciones, creditos, salir };
            float altoBotones = todos.Where(b => b.gameObject.activeSelf).Sum(b => (b == continuar ? AltoBotonConResumen : AltoBoton) + SeparacionBotones);
            float altoPanel = altoBotones + 2f * RellenoMenu + AltoCabeceraMenu;
            UIInicio.Punto(panelMenu.rectTransform, Vector2.zero, new Vector2(MargenIzquierdo, BaseMenu), new Vector2(AnchoBoton + 2f * RellenoMenu, altoPanel));
            float y = BaseMenu + altoPanel - RellenoMenu - AltoCabeceraMenu;
            foreach (BotonInicio b in todos)
            {
                if (!b.gameObject.activeSelf)
                {
                    continue;
                }
                float alto = b == continuar ? AltoBotonConResumen : AltoBoton;
                RectTransform r = (RectTransform)b.transform;
                r.DOKill();
                UIInicio.Punto(r, Vector2.zero, new Vector2(MargenIzquierdo + RellenoMenu, y - alto), new Vector2(AnchoBoton, alto));
                y -= alto + SeparacionBotones;
            }
            Navigation.Mode mode = Navigation.Mode.Automatic;
            foreach (BotonInicio b in new[] { continuar, nueva, cargar, botonOpciones, creditos, salir })
            {
                Navigation n = b.navigation;
                n.mode = mode;
                b.navigation = n;
            }
        }

        // Los paneles y cada opción llegan escalonados.
        private void AnimarEntrada()
        {
            AnimH10.EntrarPanel(panelMenu.rectTransform, new Vector2(-AnimH10.DesplazamientoEntrada, 0f));
            AnimH10.EntrarPanel(panelDatos.rectTransform, new Vector2(AnimH10.DesplazamientoEntrada, 0f), EscalonBotones * 2f);
            float demora = EscalonBotones;
            foreach (BotonInicio b in new[] { continuar, nueva, cargar, botonOpciones, creditos, salir })
            {
                if (b.gameObject.activeSelf)
                {
                    AnimH10.EntrarPanel((RectTransform)b.transform, new Vector2(-AnimH10.DesplazamientoEntrada, 0f), demora);
                    demora += EscalonBotones;
                }
            }
        }

        /// <summary>El primer botón del menú (CONTINUAR si hay partida; si no, NUEVA PARTIDA/JUGAR).</summary>
        public BotonInicio PrimerBoton => HayPartida ? continuar : nueva;

        // ------------------------------------------------------------------ acciones

        private void ContinuarPartida()
        {
            if (!HayPartida || Estado != EstadoInicio.Principal)
            {
                return;
            }
            Debug.Log("[UI] CONTINUAR carga la ranura " + ranuraContinuar + ".");
            Estado = EstadoInicio.Cargando;
            BloquearMenu();
            PantallaCargando.Mostrar();
            FlujoPartida.IrACargar(ranuraContinuar);
        }

        private void NuevaPartida()
        {
            if (Estado != EstadoInicio.Principal)
            {
                return;
            }
            if (creacion == null)
            {
                Estado = EstadoInicio.Cargando;
                BloquearMenu();
                PantallaCargando.Mostrar();
                FlujoPartida.IrANuevaPartida();
                return;
            }
            Estado = EstadoInicio.Creacion;
            panelPrincipal.Fijar(false);
            creacion.Abrir();
        }

        private void AlCerrarCreacion()
        {
            if (Estado != EstadoInicio.Creacion)
            {
                return;
            }
            Estado = EstadoInicio.Principal;
            RefrescarPartida();
            panelPrincipal.Fijar(true);
            nueva.ElegirSinSonido();
        }

        private void AbrirOpciones()
        {
            if (Estado != EstadoInicio.Principal)
            {
                return;
            }
            Estado = EstadoInicio.Opciones;
            panelPrincipal.Fijar(false);
            opciones.Refrescar();
            opciones.Mostrar(PestanaOpciones.Graficos);
            panelOpciones.Fijar(true, 0.12f);
            opciones.PrimerElegido.ElegirSinSonido();
        }

        /// <summary>Cierra las opciones y vuelve al menú con OPCIONES elegido.</summary>
        public void CerrarOpciones()
        {
            if (Estado != EstadoInicio.Opciones)
            {
                return;
            }
            Estado = EstadoInicio.Principal;
            Sonidos.Reproducir(BotonInicio.SonidoCancelar);
            panelOpciones.Fijar(false);
            panelPrincipal.Fijar(true, 0.12f);
            botonOpciones.ElegirSinSonido();
        }

        private void AbrirCreditos()
        {
            if (Estado != EstadoInicio.Principal || !PantallaTitulo.HayEnlaceCreditos)
            {
                return;
            }
            Estado = EstadoInicio.Creditos;
            PantallaTitulo.PedirCreditosExterno(AlCerrarCreditos);
        }

        private void AlCerrarCreditos()
        {
            if (Estado != EstadoInicio.Creditos)
            {
                return;
            }
            Estado = EstadoInicio.Principal;
            creditos.ElegirSinSonido();
        }

        private void SalirDelJuego()
        {
            Debug.Log("[UI] SALIR desde el inicio.");
            if (AlSalir != null)
            {
                AlSalir("salir");
                return;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BloquearMenu()
        {
            grupoMenu.interactable = false;
            grupoMenu.blocksRaycasts = false;
            EventSystem sistema = EventSystem.current;
            if (sistema != null)
            {
                sistema.SetSelectedGameObject(null);
            }
        }

        // FlujoPartida pide cargar la escena de la ciudad: se hace asíncrono, con la pantalla de carga siguiendo la operación real.
        private void AlPedirEscena(string ruta)
        {
            rutaPedida = ruta;
            if (SoloAnotarCarga)
            {
                return;
            }
            Estado = EstadoInicio.Cargando;
            BloquearMenu();
            PantallaCargando.Mostrar().Asignar(EscenaInicio.CargarAsincrono(ruta));
        }

        private void AlElegir(BotonInicio boton)
        {
            ultimoElegido = boton;
            if (primerBotonElegido && logo != null)
            {
                logo.ForzarRafaga();
            }
        }

        // ------------------------------------------------------------------ cuadro a cuadro

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            tiempo += dt;
            velo.color = new Color(0f, 0f, 0f, 1f - Mathf.SmoothStep(0f, 1f, tiempo / SegundosEntrada));
            if (velo.color.a <= 0.001f && velo.enabled)
            {
                velo.enabled = false;
            }
            if (!primerBotonElegido && tiempo >= EsperaPrimerBoton && Estado == EstadoInicio.Principal)
            {
                primerBotonElegido = true;
                BotonInicio primero = PrimerBoton;
                primero.ElegirSinSonido();
                EventSystem sistema = EventSystem.current;
                if (sistema != null)
                {
                    sistema.firstSelectedGameObject = primero.gameObject;
                }
            }
            reloj.text = DateTime.Now.ToString("HH:mm") + "  //  LLUVIA ÁCIDA  //  MEGACIUDAD";
            ActualizarAyuda();
            RecuperarSeleccion();
            Regresar();
            bool cubierto = Estado == EstadoInicio.Creacion || Estado == EstadoInicio.Creditos;
            grupoMenu.alpha = Mathf.MoveTowards(grupoMenu.alpha, cubierto ? 0f : 1f, dt / 0.25f);
        }

        // El teclado o el control siempre deben tener algo elegido: si se perdió (clic en el fondo), vuelve el último.
        private void RecuperarSeleccion()
        {
            EventSystem sistema = EventSystem.current;
            if (sistema == null || !primerBotonElegido || sistema.currentSelectedGameObject != null || !grupoMenu.interactable)
            {
                return;
            }
            BotonInicio objetivo = null;
            if (Estado == EstadoInicio.Principal)
            {
                objetivo = ultimoElegido != null && ultimoElegido.gameObject.activeInHierarchy ? ultimoElegido : PrimerBoton;
            }
            else if (Estado == EstadoInicio.Opciones)
            {
                objetivo = opciones.PrimerElegido;
            }
            if (objetivo != null)
            {
                bool teclado = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
                bool control = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
                if (teclado || control)
                {
                    objetivo.ElegirSinSonido();
                }
            }
        }

        private void ActualizarAyuda()
        {
            Gamepad control = Gamepad.current;
            Keyboard teclado = Keyboard.current;
            if (control != null && control.wasUpdatedThisFrame)
            {
                controlActivo = true;
            }
            else if (teclado != null && teclado.anyKey.wasPressedThisFrame)
            {
                controlActivo = false;
            }
            if (controlActivo != ayudaDeControl)
            {
                ayudaDeControl = controlActivo;
                ayuda.text = controlActivo ? AyudaControl : AyudaTeclado;
            }
        }

        private void Regresar()
        {
            if (Estado != EstadoInicio.Opciones)
            {
                return;
            }
            Keyboard teclado = Keyboard.current;
            Gamepad control = Gamepad.current;
            if ((teclado != null && teclado.escapeKey.wasPressedThisFrame) || (control != null && control.buttonEast.wasPressedThisFrame))
            {
                CerrarOpciones();
            }
        }
    }
}
