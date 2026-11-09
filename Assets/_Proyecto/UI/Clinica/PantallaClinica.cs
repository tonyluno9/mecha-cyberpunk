using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>
    /// Pantalla de la clínica (T-193; presentación holográfica de T-815). Los 4 espacios (lo instalado y la opción siguiente con su precio, en gris
    /// si la liga no la abre y en naranja suave si no alcanza), la descripción de la elegida y la vista previa del cuerpo con esa opción puesta
    /// de prueba. ↑ ↓ eligen; Enter pide confirmar y el segundo Enter avisa <c>alInstalar</c>; Esc sale. Quien la abre cobra y la cierra con
    /// <see cref="Ocultar"/>. La tecla que la abre en el mismo cuadro no cuenta.
    /// T-815: lo que se ve es un menú como el banco de armas, armado en código con el kit: a la izquierda los espacios, al centro el holograma
    /// del cuerpo (<see cref="VistaHologramaCuerpo"/>, se gira arrastrando y se acerca con la rueda) con el espacio elegido resaltado y su
    /// marcador, a la derecha el implante con su efecto, la tabla antes/después y el botón. Los textos de T-193 (<see cref="TextoEspacio"/>,
    /// <see cref="TextoOpcion"/>, <see cref="Aviso"/>...) siguen siendo el modelo de la pantalla y se llenan igual; su lienzo viejo ya no se enciende.
    /// Al instalar, el holograma destella y la pantalla se desvanece en <see cref="ConfigPantallaClinica.DuracionDestello"/>.
    /// </summary>
    public class PantallaClinica : MonoBehaviour
    {
        private const int Espacios = 4;
        private const int NivelesImplante = 2;
        private const int MaxFilasEfectos = 5;
        private const int OrdenLienzo = 1;
        private const float Margen = TrazoH10.MargenPantalla;
        private const float AltoCabecera = 104f;
        private const float AnchoLateral = 460f;
        private const float AltoLateral = 876f;
        private const float AbajoLateral = 88f;
        private const float RellenoLateral = 20f;
        private const float AltoFilaEspacio = 188f;
        private const float AltoFilaEfecto = 40f;
        private const float AltoBoton = 70f;
        private const float AnchoHolo = 880f;
        private const float AltoHolo = 876f;
        private const float AbajoHolo = 88f;
        private const float GrosorMarca = TrazoH10.GrosorLinea;
        private const float LargoMarca = 24f;
        private const float AlfaIconoSinInstalar = 0.55f;
        private const float AlfaDestelloPantalla = 0.6f;
        private const float LargoLineaMarcador = 120f;
        private const float LadoIconoEspacio = 116f;
        private const float XTextoEspacio = 136f;
        private const float GrosorConector = TrazoH10.GrosorLinea;
        private const string RutaIconos = "Menus/Implante_";
        private const string RutaIconoGenerico = "Menus/Cat_Implantes";
        private const string TituloTrasCompra = "TE QUEDARÍA";
        private const string TituloOcupados = "ESPACIOS OCUPADOS";
        private const float TamanoRombo = 16f;
        private const int TamanoTituloGrande = TrazoH10.TamanoTitulo;
        private const int TamanoNombreImplante = TrazoH10.TamanoCifra;
        private const int TamanoInstalando = TrazoH10.TamanoTitulo * 2;
        private static readonly string[] Romanos = { "", "I", "II", "III" };

        private sealed class FilaEspacio
        {
            public Image Fondo;
            public GameObject Borde;
            public Image Filo;
            public Image Icono;
            public Text Nombre;
            public Text Instalado;
            public Text Etiqueta;
            public Image[] Marcas;
        }

        private sealed class FilaTabla
        {
            public GameObject Raiz;
            public Text Nombre;
            public Text Antes;
            public Text Flecha;
            public Text Despues;
            public Text Diferencia;
        }

        [SerializeField] private ConfigPantallaClinica config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoDinero;
        [Tooltip("Una por espacio, en el orden de EspacioImplante: lo instalado.")]
        [SerializeField] private Text[] textosEspacio;
        [Tooltip("Una por espacio, en el orden de EspacioImplante: la opción siguiente.")]
        [SerializeField] private Text[] textosOpcion;
        [SerializeField] private Text textoDescripcion;
        [SerializeField] private Text textoAviso;
        [SerializeField] private Text textoAyuda;
        [SerializeField] private RawImage imagenVista;
        [SerializeField] private VistaPreviaApariencia vista;
        [Tooltip("Elige la fila de arriba (↑).")]
        [SerializeField] private InputAction arriba = new InputAction("ClinicaArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [Tooltip("Elige la fila de abajo (↓).")]
        [SerializeField] private InputAction abajo = new InputAction("ClinicaAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [Tooltip("Pide confirmar y confirma (Enter).")]
        [SerializeField] private InputAction confirmar = new InputAction("ClinicaEnter", InputActionType.Button, "<Keyboard>/enter");
        [Tooltip("Cancela la confirmación o sale (Esc).")]
        [SerializeField] private InputAction salir = new InputAction("ClinicaEsc", InputActionType.Button, "<Keyboard>/escape");
        [Tooltip("Arrastrar para girar el holograma (clic izquierdo).")]
        [SerializeField] private InputAction arrastrar = new InputAction("ClinicaArrastrar", InputActionType.Button, "<Mouse>/leftButton");
        [Tooltip("Movimiento del ratón al arrastrar (px).")]
        [SerializeField] private InputAction movimientoRaton = new InputAction("ClinicaRaton", InputActionType.Value, "<Mouse>/delta");
        [Tooltip("Rueda del ratón para acercar.")]
        [SerializeField] private InputAction rueda = new InputAction("ClinicaRueda", InputActionType.Value, "<Mouse>/scroll/y");

        private readonly FilaEspacio[] filasEspacio = new FilaEspacio[Espacios];
        private readonly List<FilaTabla> filasTabla = new List<FilaTabla>();
        private readonly List<FilaEfectoImplante> efectos = new List<FilaEfectoImplante>();
        private readonly bool[] conImplante = new bool[Espacios];
        private readonly int[] nivelesInstalados = new int[Espacios];

        private ServicioImplantes servicio;
        private Action<DatosImplante> alInstalar;
        private Action alSalir;
        private bool visible;
        private bool confirmando;
        private int indice;
        private int cuadroAbierta;

        // T-815: el lienzo holográfico.
        private VistaHologramaCuerpo holograma;
        private Canvas lienzo;
        private CanvasGroup grupo;
        private RectTransform areaHolo;
        private RawImage imagenHolo;
        private Image destelloHolo;
        private RectTransform marcador;
        private RectTransform conector;
        private Text textoMarcador;
        private Text textoHoloDinero;
        private Text textoEspacioElegido;
        private Text textoNombreImplante;
        private Text textoNivelImplante;
        private Text textoEfecto;
        private Text textoHoloAviso;
        private Text textoBoton;
        private Image fondoBoton;
        private Text textoInstalando;
        private readonly RectTransform[] lineasEspacio = new RectTransform[Espacios];
        private Image panelEspacios;
        private Image panelImplante;
        private Image lineaBoton;
        private Text textoTrasCompra;
        private Text textoOcupados;
        private float anchoImagen;
        private Sprite iconoGenerico;
        private bool construida;
        private bool arrastrando;
        private bool instalando;
        private float destello;
        private float desvanecer;
        private CursorLockMode cursorAntes;
        private bool cursorVisibleAntes;

        public bool Visible => visible;
        public bool Confirmando => confirmando;
        public int Indice => indice;
        public int CantidadFilas => Espacios;
        public ConfigPantallaClinica Config => config;
        public VistaPreviaApariencia Vista => vista;
        public string Titulo => textoTitulo != null ? textoTitulo.text : string.Empty;
        public string Dinero => textoDinero != null ? textoDinero.text : string.Empty;
        public string Descripcion => textoDescripcion != null ? textoDescripcion.text : string.Empty;
        public string Aviso => textoAviso != null && textoAviso.gameObject.activeSelf ? textoAviso.text : string.Empty;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;
        public Canvas Canvas => GetComponent<Canvas>();

        /// <summary>El holograma del cuerpo (T-815).</summary>
        public VistaHologramaCuerpo Holograma => holograma;

        /// <summary>El lienzo holográfico (T-815), o null si no se ha armado.</summary>
        public Canvas Lienzo => lienzo;

        /// <summary>True mientras se ve el lienzo holográfico (también los instantes del destello al instalar).</summary>
        public bool LienzoVisible => lienzo != null && lienzo.gameObject.activeSelf;

        /// <summary>True durante el destello de la instalación (la pantalla ya cerró y se desvanece).</summary>
        public bool Destellando => desvanecer > 0f;

        /// <summary>Los textos del lienzo holográfico: dinero, implante elegido, botón y nombre del marcador.</summary>
        public string DineroHolograma => textoHoloDinero != null ? textoHoloDinero.text : string.Empty;
        public string NombreImplante => textoNombreImplante != null ? textoNombreImplante.text : string.Empty;
        public string TextoBoton => textoBoton != null ? textoBoton.text : string.Empty;
        public string Marcador => textoMarcador != null ? textoMarcador.text : string.Empty;

        /// <summary>La posición del marcador del espacio elegido sobre el holograma (px del área, centro = 0).</summary>
        public Vector2 PosicionMarcador => marcador != null ? marcador.anchoredPosition : Vector2.zero;

        /// <summary>T-896: los extremos de la línea de luz y los puntos que debe unir (casilla elegida y marcador), en el mundo del lienzo.</summary>
        public (Vector3 inicio, Vector3 fin, Vector3 casilla, Vector3 punto) Conector
        {
            get
            {
                RectTransform fila = filasEspacio[indice].Fondo.rectTransform;
                return (conector.position, conector.TransformPoint(new Vector3(conector.rect.width, 0f, 0f)),
                    fila.TransformPoint(new Vector3(fila.rect.xMax, fila.rect.center.y, 0f)), marcador.position);
            }
        }

        /// <summary>Las líneas tenues encendidas: una por casilla que no es la elegida.</summary>
        public int LineasEspacioVisibles
        {
            get
            {
                int cuantas = 0;
                foreach (RectTransform linea in lineasEspacio)
                {
                    cuantas += linea != null && linea.gameObject.activeSelf ? 1 : 0;
                }
                return cuantas;
            }
        }

        /// <summary>Filas de la tabla antes/después que se ven.</summary>
        public int FilasEfectos => efectos.Count;

        /// <summary>Una fila de la tabla: "GOLPE A PIE 100% → 150% +50%".</summary>
        public string TextoEfecto(int fila)
        {
            FilaTabla f = filasTabla[fila];
            return f.Nombre.text + " " + f.Antes.text + " " + f.Flecha.text + " " + f.Despues.text + " " + f.Diferencia.text;
        }

        /// <summary>Lo instalado en la fila: "OJOS · OJOS DE LECHUZA" o "OJOS · —".</summary>
        public string TextoEspacio(int fila) => textosEspacio[fila].text;

        /// <summary>La opción siguiente de la fila (con la marca si es la elegida).</summary>
        public string TextoOpcion(int fila) => textosOpcion[fila].text;

        public Color ColorOpcion(int fila) => textosOpcion[fila].color;

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || textoDinero == null || textoDescripcion == null
                || textoAviso == null || textoAyuda == null || imagenVista == null || vista == null
                || textosEspacio == null || textosEspacio.Length != Espacios || textosOpcion == null || textosOpcion.Length != Espacios)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaClinica de " + name, this);
                enabled = false;
                return;
            }
            contenido.SetActive(false);
            // Se arma al cargar la escena (no al abrir) para que abrir no dé un tirón.
            Construir();
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += AlDibujarLienzos;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= AlDibujarLienzos;
            Ocultar();
            TerminarDestello();
        }

        // Las líneas dependen del acomodo final del lienzo: se tienden justo antes de dibujarlo.
        private void AlDibujarLienzos()
        {
            if (visible)
            {
                ColocarMarcador();
            }
        }

        /// <summary>Abre la pantalla con el primer espacio elegido. <paramref name="alInstalar"/> recibe el implante confirmado.</summary>
        public void Mostrar(ServicioImplantes servicio, Action<DatosImplante> alInstalar, Action alSalir)
        {
            this.servicio = servicio;
            this.alInstalar = alInstalar;
            this.alSalir = alSalir;
            indice = 0;
            confirmando = false;
            instalando = false;
            TerminarDestello();
            KitUI.VestirPantalla(fondo, config.ColorFondo);
            Preparar(textoTitulo, config.TextoTitulo, config.ColorElegido, config.TamanoTitulo);
            Preparar(textoAyuda, config.TextoAyuda, config.ColorNormal, config.TamanoAyuda);
            textoAviso.gameObject.SetActive(false);
            vista.Mostrar(true);
            imagenVista.texture = vista.Textura;
            visible = true;
            cuadroAbierta = Time.frameCount;
            MostrarHolograma();
            Pintar();
            arriba.Enable();
            abajo.Enable();
            confirmar.Enable();
            salir.Enable();
            arrastrar.Enable();
            movimientoRaton.Enable();
            rueda.Enable();
        }

        public void Ocultar()
        {
            bool estaba = visible;
            visible = false;
            confirmando = false;
            arrastrando = false;
            if (arriba != null)
            {
                arriba.Disable();
                abajo.Disable();
                confirmar.Disable();
                salir.Disable();
                arrastrar.Disable();
                movimientoRaton.Disable();
                rueda.Disable();
            }
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
            if (vista != null && vista.Cuerpo != null && vista.EstaEncendida)
            {
                vista.Cuerpo.QuitarPrueba();
                vista.Mostrar(false);
            }
            if (estaba)
            {
                PilaPantallas.Cerrar(this);
                Cursor.lockState = cursorAntes;
                Cursor.visible = cursorVisibleAntes;
                // Si se acaba de instalar, el holograma destella y se desvanece; si no, se va ya.
                if (instalando && isActiveAndEnabled && lienzo != null)
                {
                    desvanecer = config.DuracionDestello;
                    marcador.gameObject.SetActive(false);
                    // Lo recién instalado ya se ve en verde y el dinero ya bajó mientras se desvanece.
                    EspacioImplante elegido = (EspacioImplante)indice;
                    PintarHolograma(servicio.Opcion(elegido), servicio.ImplanteInstalado(elegido));
                    // T-896: el implante se ensambla, destella y entonces la pantalla se va.
                    desvanecer += config.DuracionEnsamble;
                    conector.gameObject.SetActive(false);
                    foreach (RectTransform linea in lineasEspacio)
                    {
                        linea.gameObject.SetActive(false);
                    }
                    holograma.Ensamblar(elegido, nivelesInstalados, () =>
                    {
                        destello = 1f;
                        textoInstalando.gameObject.SetActive(true);
                    });
                }
                else
                {
                    TerminarDestello();
                }
            }
            instalando = false;
        }

        /// <summary>Muestra un aviso en la línea de abajo, en naranja suave.</summary>
        public void MostrarAviso(string texto)
        {
            confirmando = false;
            instalando = false;
            Preparar(textoAviso, texto, config.ColorAviso, config.TamanoOpcion);
            textoAviso.gameObject.SetActive(true);
            PintarAviso();
            PintarBoton();
        }

        private void Update()
        {
            AnimarDestello(Time.unscaledDeltaTime);
            if (!visible)
            {
                return;
            }
            ColocarMarcador();
            if (Time.frameCount == cuadroAbierta)
            {
                return;
            }
            if (salir.WasPressedThisFrame())
            {
                Sonidos.Reproducir(config.SonidoCancelar);
                if (confirmando)
                {
                    confirmando = false;
                    textoAviso.gameObject.SetActive(false);
                    PintarAviso();
                    PintarBoton();
                }
                else
                {
                    alSalir?.Invoke();
                }
                return;
            }
            if (confirmar.WasPressedThisFrame())
            {
                AlConfirmar();
                return;
            }
            int paso = (abajo.WasPressedThisFrame() ? 1 : 0) - (arriba.WasPressedThisFrame() ? 1 : 0);
            if (paso != 0)
            {
                int nuevo = Mathf.Clamp(indice + paso, 0, Espacios - 1);
                confirmando = false;
                textoAviso.gameObject.SetActive(false);
                if (nuevo != indice)
                {
                    indice = nuevo;
                    Sonidos.Reproducir(config.SonidoMover);
                }
                Pintar();
            }
            GirarConRaton();
        }

        private void AlConfirmar()
        {
            OfertaImplante oferta = servicio.Opcion((EspacioImplante)indice);
            if (confirmando)
            {
                confirmando = false;
                textoAviso.gameObject.SetActive(false);
                PintarAviso();
                PintarBoton();
                if (oferta.Estado == EstadoOfertaImplante.Instalar)
                {
                    // El destello va antes de avisar: si se instala, quien abrió cierra la pantalla y esta se desvanece destellando.
                    instalando = true;
                    Sonidos.Reproducir(config.SonidoCompra);
                    alInstalar?.Invoke(oferta.Implante);
                }
                return;
            }
            switch (oferta.Estado)
            {
                case EstadoOfertaImplante.Instalar:
                    confirmando = true;
                    Sonidos.Reproducir(config.SonidoAceptar);
                    Preparar(textoAviso, string.Format(config.FormatoConfirmar, FormatoDinero.Texto(oferta.Costo)), config.ColorPrecio, config.TamanoOpcion);
                    textoAviso.gameObject.SetActive(true);
                    break;
                case EstadoOfertaImplante.SinDinero:
                    Sonidos.Reproducir(config.SonidoSinDinero);
                    MostrarAviso(config.TextoAvisoSinDinero);
                    break;
                default:
                    Sonidos.Reproducir(config.SonidoCancelar);
                    break;
            }
            PintarAviso();
            PintarBoton();
        }

        private void Pintar()
        {
            Preparar(textoDinero, string.Format(config.FormatoDinero, FormatoDinero.Texto(Registro<ICartera>.Actual?.Dinero ?? 0)),
                config.ColorNormal, config.TamanoDinero);
            for (int i = 0; i < Espacios; i++)
            {
                EspacioImplante espacio = (EspacioImplante)i;
                string instalado = servicio.Nivel(espacio) > 0 ? servicio.Nombre(espacio) : config.TextoVacio;
                OfertaImplante oferta = servicio.Opcion(espacio);
                bool elegida = i == indice;
                Preparar(textosEspacio[i], string.Format(config.FormatoEspacio, config.NombreEspacio(i), instalado),
                    elegida ? config.ColorElegido : config.ColorNormal, config.TamanoEspacio);
                string texto = TextoDeOferta(oferta, servicio.Nivel(espacio));
                Color color = oferta.Estado == EstadoOfertaImplante.LigaBaja ? config.ColorApagado
                    : oferta.Estado == EstadoOfertaImplante.SinDinero ? config.ColorAviso
                    : elegida ? config.ColorElegido : config.ColorNormal;
                Preparar(textosOpcion[i], elegida ? string.Format(config.FormatoElegido, texto) : texto, color, config.TamanoOpcion);
            }

            EspacioImplante elegido = (EspacioImplante)indice;
            OfertaImplante ofertaElegida = servicio.Opcion(elegido);
            DatosImplante mostrado = ofertaElegida.Implante != null ? ofertaElegida.Implante : servicio.ImplanteInstalado(elegido);
            Preparar(textoDescripcion, mostrado != null ? mostrado.Descripcion : string.Empty, config.ColorNormal, config.TamanoDescripcion);

            // La vista previa lleva puesta la opción elegida de prueba (se quita al mover la elección o al salir).
            vista.Cuerpo.QuitarPrueba();
            if (ofertaElegida.Implante != null)
            {
                vista.Cuerpo.MostrarPrueba(elegido, ofertaElegida.Implante.Nivel);
            }
            PintarHolograma(ofertaElegida, mostrado);
        }

        private string TextoDeOferta(OfertaImplante oferta, int nivelInstalado)
        {
            switch (oferta.Estado)
            {
                case EstadoOfertaImplante.YaLoMejor:
                    return config.TextoLoMejor;
                case EstadoOfertaImplante.LigaBaja:
                    return string.Format(config.FormatoLiga, oferta.Implante.Nombre, oferta.LigaQueAbre);
                case EstadoOfertaImplante.SinDinero:
                    return string.Format(config.FormatoSinDinero, oferta.Implante.Nombre);
                default:
                    string precio = string.Format(config.FormatoPrecio, oferta.Implante.Nombre, FormatoDinero.Texto(oferta.Costo));
                    return nivelInstalado > 0 ? precio + " · " + config.TextoDiferencia : precio;
            }
        }

        private static void Preparar(Text texto, string contenidoTexto, Color color, int tamano)
        {
            texto.text = contenidoTexto;
            texto.color = color;
            texto.fontSize = tamano;
        }

        // ------------------------------------------------------------------ T-815: holograma

        private void MostrarHolograma()
        {
            Construir();
            lienzo.gameObject.SetActive(true);
            grupo.alpha = 1f;
            textoInstalando.gameObject.SetActive(false);
            destelloHolo.color = Color.clear;
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            holograma.Mostrar(config, config.Cuerpo(perfil != null ? perfil.Apariencia.Cara : 0));
            imagenHolo.texture = holograma.Textura;
            PilaPantallas.Abrir(this);
            KitUI.AsegurarEventSystem();
            cursorAntes = Cursor.lockState;
            cursorVisibleAntes = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            AnimarEntrada();
        }

        private void AnimarEntrada()
        {
            float d = AnimH10.DesplazamientoEntrada;
            AnimH10.EntrarPanel(panelEspacios.rectTransform, new Vector2(-d, 0f));
            AnimH10.EntrarPanel(panelImplante.rectTransform, new Vector2(d, 0f), AnimH10.SegundosEscalon);
            AnimH10.EntrarPanel(areaHolo, new Vector2(0f, d), AnimH10.SegundosEscalon * 2f);
        }

        private void TerminarDestello()
        {
            desvanecer = 0f;
            destello = 0f;
            if (holograma != null)
            {
                holograma.Ocultar();
            }
            if (lienzo != null)
            {
                lienzo.gameObject.SetActive(false);
            }
        }

        private void AnimarDestello(float dt)
        {
            if (destello > 0f && destelloHolo != null)
            {
                destello = Mathf.Max(0f, destello - dt / config.DuracionDestello);
                Color c = config.ColorHolograma;
                destelloHolo.color = new Color(c.r, c.g, c.b, destello * AlfaDestelloPantalla);
            }
            if (desvanecer > 0f)
            {
                desvanecer -= dt;
                grupo.alpha = Mathf.Clamp01(desvanecer / config.DuracionDestello);
                if (desvanecer <= 0f)
                {
                    TerminarDestello();
                }
            }
        }

        private void GirarConRaton()
        {
            if (Mouse.current == null || holograma == null)
            {
                return;
            }
            Vector2 puntero = Mouse.current.position.ReadValue();
            bool encima = RectTransformUtility.RectangleContainsScreenPoint(areaHolo, puntero, CamaraDelCanvas());
            if (arrastrar.WasPressedThisFrame())
            {
                arrastrando = encima;
            }
            if (!arrastrar.IsPressed())
            {
                arrastrando = false;
            }
            if (arrastrando)
            {
                holograma.Girar(movimientoRaton.ReadValue<Vector2>());
            }
            float scroll = rueda.ReadValue<float>();
            if (encima && Mathf.Abs(scroll) > 0f)
            {
                holograma.Acercar(scroll);
            }
        }

        private Camera CamaraDelCanvas()
        {
            Canvas raiz = lienzo != null ? lienzo.rootCanvas : null;
            return raiz != null && raiz.renderMode != RenderMode.ScreenSpaceOverlay ? raiz.worldCamera : null;
        }

        // El rombo y la línea del marcador siguen al espacio elegido sobre el holograma.
        private void ColocarMarcador()
        {
            if (marcador == null)
            {
                return;
            }
            if (holograma == null || holograma.Camara == null || !holograma.ConCuerpo)
            {
                marcador.gameObject.SetActive(false);
                return;
            }
            Vector3 vp = holograma.Camara.WorldToViewportPoint(holograma.CentroEspacio(holograma.Elegido));
            marcador.gameObject.SetActive(vp.z > 0f);
            marcador.anchoredPosition = new Vector2((vp.x - 0.5f) * anchoImagen, (vp.y - 0.5f) * AltoHolo);
            ColocarConector();
            ColocarLineasEspacios();
        }

        // Cada casilla está unida a su lugar en el cuerpo; la elegida usa el conector, más brillante.
        private void ColocarLineasEspacios()
        {
            for (int i = 0; i < Espacios; i++)
            {
                RectTransform linea = lineasEspacio[i];
                Vector3 vp = holograma.Camara.WorldToViewportPoint(holograma.CentroEspacio((EspacioImplante)i));
                bool ver = i != indice && vp.z > 0f;
                linea.gameObject.SetActive(ver);
                if (!ver)
                {
                    continue;
                }
                RectTransform fila = filasEspacio[i].Fondo.rectTransform;
                Vector3 casilla = fila.TransformPoint(new Vector3(fila.rect.xMax, fila.rect.center.y, 0f));
                Vector3 punto = areaHolo.TransformPoint(new Vector3((vp.x - 0.5f) * anchoImagen, vp.y * AltoHolo, 0f));
                TenderLinea(linea, casilla, punto);
            }
        }

        private void TenderLinea(RectTransform linea, Vector3 desde, Vector3 hasta)
        {
            RectTransform raiz = (RectTransform)lienzo.transform;
            Vector2 tramo = raiz.InverseTransformPoint(hasta) - raiz.InverseTransformPoint(desde);
            linea.position = desde;
            linea.sizeDelta = new Vector2(tramo.magnitude, GrosorConector);
            linea.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tramo.y, tramo.x) * Mathf.Rad2Deg);
        }

        // T-896: la línea de luz que une la casilla elegida con su lugar en el cuerpo.
        private void ColocarConector()
        {
            RectTransform fila = filasEspacio[indice].Fondo.rectTransform;
            Vector3 casilla = fila.TransformPoint(new Vector3(fila.rect.xMax, fila.rect.center.y, 0f));
            conector.gameObject.SetActive(marcador.gameObject.activeSelf);
            TenderLinea(conector, casilla, marcador.position);
        }

        private void PintarHolograma(OfertaImplante oferta, DatosImplante mostrado)
        {
            if (!construida)
            {
                return;
            }
            EstiloCyberfix estilo = KitUI.Estilo;
            ICartera cartera = Registro<ICartera>.Actual;
            textoHoloDinero.text = FormatoDinero.Texto(cartera != null ? cartera.Dinero : 0);
            for (int i = 0; i < Espacios; i++)
            {
                EspacioImplante espacio = (EspacioImplante)i;
                FilaEspacio f = filasEspacio[i];
                bool elegida = i == indice;
                int nivel = servicio.Nivel(espacio);
                conImplante[i] = nivel > 0;
                nivelesInstalados[i] = nivel;
                f.Fondo.color = elegida ? TrazoH10.TurquesaFondo : TrazoH10.FondoPanel;
                f.Filo.color = TrazoH10.Rareza(nivel);
                f.Borde.SetActive(elegida);
                OfertaImplante ofertaFila = servicio.Opcion(espacio);
                int nivelIcono = Mathf.Max(nivel, ofertaFila.Implante != null ? ofertaFila.Implante.Nivel : 0);
                Sprite icono = Resources.Load<Sprite>(RutaIconos + espacio + "_" + Mathf.Clamp(nivelIcono, 1, NivelesImplante));
                f.Icono.sprite = icono != null ? icono : IconoGenerico();
                f.Icono.enabled = f.Icono.sprite != null;
                f.Icono.color = nivel > 0 ? Color.white : new Color(1f, 1f, 1f, AlfaIconoSinInstalar);
                f.Nombre.text = config.NombreEspacio(i);
                f.Nombre.color = TrazoH10.Texto;
                f.Instalado.text = nivel > 0 ? servicio.Nombre(espacio) : config.TextoSinImplante;
                f.Instalado.color = nivel > 0 ? TrazoH10.Turquesa : TrazoH10.TextoTenue;
                for (int m = 0; m < f.Marcas.Length; m++)
                {
                    f.Marcas[m].color = m < nivel ? TrazoH10.Turquesa : TrazoH10.ConAlfa(TrazoH10.TextoTenue, 0.25f);
                }
                PintarEtiqueta(f.Etiqueta, servicio.Opcion(espacio), cartera);
            }

            EspacioImplante elegido = (EspacioImplante)indice;
            string nombreEspacio = config.NombreEspacio(indice);
            textoMarcador.text = nombreEspacio;
            textoEspacioElegido.text = nombreEspacio + " · " + (mostrado != null ? mostrado.Nombre : config.TextoSinImplante);
            textoNombreImplante.text = mostrado != null ? mostrado.Nombre : config.TextoSinImplante;
            textoNombreImplante.color = oferta.Estado == EstadoOfertaImplante.LigaBaja ? TrazoH10.TextoTenue : TrazoH10.Texto;
            textoNivelImplante.text = mostrado != null
                ? string.Format(config.FormatoNivel, Romanos[Mathf.Clamp(mostrado.Nivel, 0, Romanos.Length - 1)])
                    + (oferta.Estado == EstadoOfertaImplante.YaLoMejor ? " · " + config.TextoInstalado : string.Empty)
                : string.Empty;
            textoEfecto.text = mostrado != null ? mostrado.Descripcion : string.Empty;

            DatosImplante instalado = servicio.ImplanteInstalado(elegido);
            EfectosImplantes antes = instalado != null ? instalado.Efectos : EfectosImplantes.Ninguno;
            EfectosImplantes despues = oferta.Implante != null ? oferta.Implante.Efectos : antes;
            ReglaMenuImplantes.Comparar(antes, despues, efectos);
            bool apagada = oferta.Estado == EstadoOfertaImplante.LigaBaja;
            for (int i = 0; i < filasTabla.Count; i++)
            {
                FilaTabla f = filasTabla[i];
                bool hay = i < efectos.Count;
                f.Raiz.SetActive(hay);
                if (!hay)
                {
                    continue;
                }
                FilaEfectoImplante fila = efectos[i];
                f.Nombre.text = config.NombreEfecto((int)fila.Efecto);
                f.Antes.text = ReglaMenuImplantes.Texto(fila.Efecto, fila.Antes, config.TextoSi, config.TextoNo);
                f.Despues.text = ReglaMenuImplantes.Texto(fila.Efecto, fila.Despues, config.TextoSi, config.TextoNo);
                f.Diferencia.text = ReglaMenuImplantes.TextoDiferencia(fila.Efecto, fila.Antes, fila.Despues);
                Color cambio = apagada ? TrazoH10.TextoTenue : fila.Cambio > 0 ? TrazoH10.Amarillo : fila.Cambio < 0 ? TrazoH10.Coral : TrazoH10.TextoTenue;
                f.Nombre.color = apagada ? estilo.Desactivado : estilo.Texto;
                f.Antes.color = new Color(estilo.Texto.r, estilo.Texto.g, estilo.Texto.b, 0.7f);
                f.Flecha.color = fila.Cambio != 0 && !apagada ? TrazoH10.Turquesa : TrazoH10.TextoTenue;
                f.Despues.color = fila.Cambio != 0 ? cambio : f.Antes.color;
                f.Diferencia.color = cambio;
            }
            PintarResumen(oferta, cartera);
            PintarAviso();
            PintarBoton();
            holograma.Elegir(elegido, conImplante);
            int nivelPrueba = oferta.Estado != EstadoOfertaImplante.YaLoMejor && oferta.Implante != null ? oferta.Implante.Nivel : 0;
            holograma.MostrarImplantes(nivelesInstalados, elegido, nivelPrueba);
        }

        // H32: el ícono de línea para un espacio sin arte propio (Sinapsis no tiene pieza visible).
        private Sprite IconoGenerico()
        {
            if (iconoGenerico == null)
            {
                Texture2D textura = Resources.Load<Texture2D>(RutaIconoGenerico);
                if (textura != null)
                {
                    iconoGenerico = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f));
                }
            }
            return iconoGenerico;
        }

        // Datos reales del pie del panel: cuántos espacios llevas y cómo queda tu dinero con esta compra.
        private void PintarResumen(OfertaImplante oferta, ICartera cartera)
        {
            int ocupados = 0;
            foreach (bool con in conImplante)
            {
                ocupados += con ? 1 : 0;
            }
            textoOcupados.text = ocupados + " / " + Espacios;
            bool comprable = oferta.Estado == EstadoOfertaImplante.Instalar || oferta.Estado == EstadoOfertaImplante.SinDinero;
            if (!comprable || cartera == null)
            {
                textoTrasCompra.text = "—";
                textoTrasCompra.color = TrazoH10.TextoTenue;
                return;
            }
            int resto = cartera.Dinero - oferta.Costo;
            textoTrasCompra.text = FormatoDinero.Texto(Mathf.Max(0, resto));
            textoTrasCompra.color = resto >= 0 ? TrazoH10.Texto : TrazoH10.Coral;
        }

        private void PintarEtiqueta(Text etiqueta, OfertaImplante oferta, ICartera cartera)
        {
            switch (oferta.Estado)
            {
                case EstadoOfertaImplante.YaLoMejor:
                    etiqueta.text = config.TextoLoMejor;
                    etiqueta.color = TrazoH10.Turquesa;
                    break;
                case EstadoOfertaImplante.LigaBaja:
                    etiqueta.text = string.Format(config.FormatoEtiquetaLiga, oferta.LigaQueAbre);
                    etiqueta.color = TrazoH10.TextoTenue;
                    break;
                case EstadoOfertaImplante.SinDinero:
                    etiqueta.text = FormatoDinero.Texto(oferta.Costo);
                    etiqueta.color = TrazoH10.Coral;
                    break;
                default:
                    etiqueta.text = FormatoDinero.Texto(oferta.Costo);
                    etiqueta.color = cartera != null && cartera.PuedePagar(oferta.Costo) ? TrazoH10.Amarillo : TrazoH10.Coral;
                    break;
            }
        }

        private void PintarAviso()
        {
            if (!construida)
            {
                return;
            }
            bool hay = textoAviso.gameObject.activeSelf && !string.IsNullOrEmpty(textoAviso.text);
            textoHoloAviso.gameObject.SetActive(hay);
            textoHoloAviso.text = textoAviso.text;
            textoHoloAviso.color = textoAviso.color;
        }

        private void PintarBoton()
        {
            if (!construida || servicio == null)
            {
                return;
            }
            OfertaImplante oferta = servicio.Opcion((EspacioImplante)indice);
            Color color;
            switch (oferta.Estado)
            {
                case EstadoOfertaImplante.Instalar:
                    textoBoton.text = string.Format(config.FormatoBoton, FormatoDinero.Texto(oferta.Costo));
                    color = TrazoH10.Amarillo;
                    break;
                case EstadoOfertaImplante.SinDinero:
                    textoBoton.text = config.TextoAvisoSinDinero;
                    color = TrazoH10.Coral;
                    break;
                case EstadoOfertaImplante.LigaBaja:
                    textoBoton.text = string.Format(config.FormatoEtiquetaLiga, oferta.LigaQueAbre);
                    color = TrazoH10.TextoTenue;
                    break;
                default:
                    textoBoton.text = config.TextoLoMejor;
                    color = TrazoH10.Turquesa;
                    break;
            }
            textoBoton.color = color;
            lineaBoton.color = color;
            bool fuerte = oferta.Estado == EstadoOfertaImplante.Instalar;
            fondoBoton.color = new Color(color.r, color.g, color.b, confirmando ? 0.3f : fuerte ? 0.14f : 0.05f);
        }

        // ------------------------------------------------------------------ T-815: armar el lienzo

        private void Construir()
        {
            if (construida)
            {
                return;
            }
            construida = true;
            holograma = gameObject.AddComponent<VistaHologramaCuerpo>();
            EstiloCyberfix estilo = KitUI.Estilo;
            lienzo = KitUI.CrearCanvas(transform, "LienzoImplantes", OrdenLienzo);
            // Anidado con su propio orden: va en el del lienzo de la pantalla (encima del HUD), no en 1.
            Canvas propio = GetComponent<Canvas>();
            lienzo.sortingOrder = propio != null ? propio.sortingOrder + OrdenLienzo : OrdenLienzo;
            grupo = lienzo.gameObject.AddComponent<CanvasGroup>();

            Image fondoMenu = KitUI.CrearImagen(lienzo.transform, "Fondo", config.ColorFondoMenu);
            KitUI.Estirar(fondoMenu.rectTransform);
            fondoMenu.raycastTarget = true;
            Lineas(lienzo.transform);

            // Cabecera
            Text titulo = Texto(lienzo.transform, "Titulo", RolTexto.Titulo, TextAnchor.MiddleLeft, new Vector2(0f, 1f),
                new Vector2(Margen, -14f), new Vector2(900f, 60f), config.TextoTitulo, TrazoH10.Texto);
            titulo.fontSize = TamanoTituloGrande;
            Texto(lienzo.transform, "Subtitulo", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f),
                new Vector2(Margen + 2f, -72f), new Vector2(900f, 28f), config.Subtitulo, TrazoH10.Turquesa);
            TrazoH10.Recta(lienzo.transform, "TiraCabecera", TrazoH10.TurquesaTenue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -AltoCabecera), new Vector2(-2f * Margen, TrazoH10.GrosorLinea));
            RectTransform regla = TrazoH10.Regla(lienzo.transform, "Regla", 41, 12f, 10f, TrazoH10.TurquesaTenue);
            TrazoH10.Punto(regla, new Vector2(1f, 1f), new Vector2(-Margen, -AltoCabecera - 2f), regla.sizeDelta);
            textoHoloDinero = Texto(lienzo.transform, "Dinero", RolTexto.Titulo, TextAnchor.MiddleRight, new Vector2(1f, 1f),
                new Vector2(-Margen, -14f), new Vector2(600f, 60f), string.Empty, TrazoH10.Turquesa);
            textoHoloDinero.fontSize = TrazoH10.TamanoCifra;
            Texto(lienzo.transform, "EtiquetaDinero", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(1f, 1f),
                new Vector2(-Margen, -72f), new Vector2(400f, 28f), config.EtiquetaDinero, TrazoH10.TextoTenue);

            ConstruirEspacios(estilo);
            ConstruirHolograma(estilo);
            ConstruirImplante(estilo);

            Texto(lienzo.transform, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(1400f, 32f), config.TextoAyuda.ToUpperInvariant(), TrazoH10.TextoTenue);
            TrazoH10.Recta(lienzo.transform, "TiraPie", TrazoH10.TurquesaTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 72f), new Vector2(-2f * Margen, TrazoH10.GrosorLinea));

            textoInstalando = Texto(lienzo.transform, "Instalando", RolTexto.Titulo, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1400f, 140f), config.TextoInstalando, TrazoH10.Amarillo);
            textoInstalando.fontSize = TamanoInstalando;
            textoInstalando.gameObject.SetActive(false);
            lienzo.gameObject.SetActive(false);
        }

        private void ConstruirEspacios(EstiloCyberfix estilo)
        {
            panelEspacios = TrazoH10.Panel(lienzo.transform, "PanelEspacios", TrazoH10.TurquesaTenue);
            Image panel = panelEspacios;
            KitUI.Colocar(panel.rectTransform, new Vector2(0f, 0f), new Vector2(Margen, AbajoLateral), new Vector2(AnchoLateral, AltoLateral));
            float ancho = AnchoLateral - 2f * RellenoLateral;
            TrazoH10.Seccion(panel.transform, "Encabezado", config.EncabezadoEspacios, new Vector2(RellenoLateral, -12f), ancho);
            float y = -56f;
            for (int i = 0; i < Espacios; i++)
            {
                int fila = i;
                FilaEspacio f = new FilaEspacio();
                f.Fondo = TrazoH10.Panel(panel.transform, "Espacio_" + (EspacioImplante)i, TrazoH10.TurquesaTenue);
                f.Fondo.raycastTarget = true;
                KitUI.Colocar(f.Fondo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(ancho, AltoFilaEspacio));
                Button boton = f.Fondo.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => ElegirFila(fila));
                HoverH10.Agregar(f.Fondo.gameObject, f.Fondo.transform.Find("Linea").GetComponent<Image>(), TrazoH10.TurquesaTenue, TrazoH10.Turquesa);
                Image bordeFila = TrazoH10.Imagen(f.Fondo.transform, "Elegida", TrazoH10.Turquesa, TrazoH10.Linea);
                TrazoH10.Estirar(bordeFila.rectTransform);
                f.Borde = bordeFila.gameObject;
                f.Filo = TrazoH10.Recta(f.Fondo.transform, "FiloRareza", TrazoH10.TextoTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 3f));
                // T-896: el ícono H30 (nivel 1) o H33 (nivel 2) del implante; al pasar el ratón se elige la casilla.
                EventTrigger disparador = f.Fondo.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry entrar = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entrar.callback.AddListener(_ => ElegirFila(fila));
                disparador.triggers.Add(entrar);
                f.Icono = KitUI.CrearImagen(f.Fondo.transform, "Icono", Color.white);
                f.Icono.preserveAspect = true;
                f.Icono.raycastTarget = false;
                KitUI.Colocar(f.Icono.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(LadoIconoEspacio, LadoIconoEspacio));
                f.Nombre = Texto(f.Fondo.transform, "Nombre", RolTexto.Encabezado, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(XTextoEspacio, 44f),
                    new Vector2(ancho - XTextoEspacio - 12f, 44f), string.Empty, TrazoH10.Texto);
                f.Instalado = Texto(f.Fondo.transform, "Instalado", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(XTextoEspacio, 4f),
                    new Vector2(ancho - XTextoEspacio - 12f, 32f), string.Empty, TrazoH10.TextoTenue);
                f.Etiqueta = Texto(f.Fondo.transform, "Etiqueta", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(XTextoEspacio, -36f),
                    new Vector2(ancho - XTextoEspacio - 12f, 32f), string.Empty, TrazoH10.Turquesa);
                f.Marcas = new Image[NivelesImplante];
                for (int m = 0; m < f.Marcas.Length; m++)
                {
                    f.Marcas[m] = TrazoH10.Imagen(f.Fondo.transform, "Marca_" + m, TrazoH10.TextoTenue);
                    KitUI.Colocar(f.Marcas[m].rectTransform, new Vector2(0f, 0.5f), new Vector2(XTextoEspacio + m * 24f, -70f), new Vector2(18f, 6f));
                }
                filasEspacio[i] = f;
                y -= AltoFilaEspacio + TrazoH10.SeparacionModulo;
            }
        }

        private void ConstruirHolograma(EstiloCyberfix estilo)
        {
            GameObject area = new GameObject("Holograma", typeof(RectTransform));
            area.transform.SetParent(lienzo.transform, false);
            areaHolo = (RectTransform)area.transform;
            areaHolo.anchorMin = areaHolo.anchorMax = new Vector2(0.5f, 0f);
            areaHolo.pivot = new Vector2(0.5f, 0f);
            areaHolo.anchoredPosition = new Vector2(0f, AbajoHolo);
            areaHolo.sizeDelta = new Vector2(AnchoHolo, AltoHolo);
            areaHolo.gameObject.AddComponent<CanvasGroup>();
            Image resplandor = KitUI.CrearImagen(areaHolo, "Resplandor", new Color(config.ColorHolograma.r, config.ColorHolograma.g, config.ColorHolograma.b, 0.015f));
            KitUI.Estirar(resplandor.rectTransform);
            Rejilla(areaHolo);
            imagenHolo = new GameObject("Imagen", typeof(RectTransform)).AddComponent<RawImage>();
            imagenHolo.transform.SetParent(areaHolo, false);
            anchoImagen = AltoHolo * config.AnchoTextura / config.AltoTextura;
            imagenHolo.rectTransform.anchorMin = imagenHolo.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            imagenHolo.rectTransform.pivot = new Vector2(0.5f, 0f);
            imagenHolo.rectTransform.anchoredPosition = Vector2.zero;
            imagenHolo.rectTransform.sizeDelta = new Vector2(anchoImagen, AltoHolo);
            imagenHolo.material = config.MaterialAditivoUI;
            imagenHolo.raycastTarget = true;
            destelloHolo = KitUI.CrearImagen(areaHolo, "Destello", Color.clear);
            KitUI.Estirar(destelloHolo.rectTransform);
            destelloHolo.material = config.MaterialAditivoUI;
            Escuadras(areaHolo, TrazoH10.Turquesa);
            RectTransform reglaHolo = TrazoH10.Regla(areaHolo, "ReglaHolo", 74, 12f, 10f, TrazoH10.TurquesaTenue);
            TrazoH10.Punto(reglaHolo, new Vector2(0f, 0f), new Vector2(0f, 2f), reglaHolo.sizeDelta);

            // Marcador: un rombo naranja en el espacio elegido y una línea hacia la derecha con su nombre.
            GameObject m = new GameObject("Marcador", typeof(RectTransform));
            m.transform.SetParent(areaHolo, false);
            marcador = (RectTransform)m.transform;
            marcador.anchorMin = marcador.anchorMax = new Vector2(0.5f, 0.5f);
            marcador.sizeDelta = Vector2.zero;
            KitUI.CrearRombo(marcador, "Rombo", TamanoRombo, TrazoH10.Amarillo);
            for (int i = 0; i < Espacios; i++)
            {
                Image lineaTenue = TrazoH10.Imagen(lienzo.transform, "LineaEspacio_" + i, TrazoH10.TurquesaTenue);
                lineasEspacio[i] = lineaTenue.rectTransform;
                lineasEspacio[i].anchorMin = lineasEspacio[i].anchorMax = new Vector2(0.5f, 0.5f);
                lineasEspacio[i].pivot = new Vector2(0f, 0.5f);
            }
            Image lineaConector = TrazoH10.Imagen(lienzo.transform, "Conector", TrazoH10.Amarillo);
            lineaConector.raycastTarget = false;
            conector = lineaConector.rectTransform;
            conector.anchorMin = conector.anchorMax = new Vector2(0.5f, 0.5f);
            conector.pivot = new Vector2(0f, 0.5f);
            Image linea = KitUI.CrearImagen(marcador, "Linea", new Color(estilo.Turquesa.r, estilo.Turquesa.g, estilo.Turquesa.b, 0.8f));
            Anclar(linea.rectTransform, new Vector2(TamanoRombo * 0.5f, 0f), new Vector2(LargoLineaMarcador, 2f));
            textoMarcador = KitUI.CrearTexto(marcador, "Nombre", RolTexto.Boton, TextAnchor.MiddleLeft);
            Anclar(textoMarcador.rectTransform, new Vector2(TamanoRombo * 0.5f + LargoLineaMarcador + 8f, 0f), new Vector2(260f, 44f));
            textoMarcador.color = estilo.Turquesa;
            // T-896: el conector a la casilla sustituye a la etiqueta, que se salía sobre el panel de la derecha; el texto queda como modelo.
            textoMarcador.enabled = false;
            linea.enabled = false;

            textoEspacioElegido = Texto(areaHolo, "EspacioElegido", RolTexto.Ayuda, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -12f),
                new Vector2(AnchoHolo, 34f), string.Empty, TrazoH10.Amarillo);
            Texto(areaHolo, "AyudaRaton", RolTexto.Ayuda, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -46f),
                new Vector2(AnchoHolo, 30f), config.TextoAyudaRaton, TrazoH10.TextoTenue);
        }

        // Centrado verticalmente en su padre y creciendo hacia la derecha desde x.
        private static void Anclar(RectTransform rect, Vector2 posicion, Vector2 tamano)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
        }

        private void ConstruirImplante(EstiloCyberfix estilo)
        {
            panelImplante = TrazoH10.Panel(lienzo.transform, "PanelImplante", TrazoH10.TurquesaTenue);
            Image panel = panelImplante;
            KitUI.Colocar(panel.rectTransform, new Vector2(1f, 0f), new Vector2(-Margen, AbajoLateral), new Vector2(AnchoLateral, AltoLateral));
            float ancho = AnchoLateral - 2f * RellenoLateral;
            TrazoH10.Seccion(panel.transform, "Encabezado", config.EncabezadoImplante, new Vector2(RellenoLateral, -12f), ancho);
            textoNombreImplante = Texto(panel.transform, "Nombre", RolTexto.Titulo, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(RellenoLateral, -50f),
                new Vector2(ancho, 56f), string.Empty, TrazoH10.Texto);
            textoNombreImplante.fontSize = TamanoNombreImplante;
            textoNivelImplante = Texto(panel.transform, "Nivel", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(RellenoLateral, -108f),
                new Vector2(ancho, 30f), string.Empty, TrazoH10.Turquesa);
            textoEfecto = Texto(panel.transform, "Efecto", RolTexto.Ayuda, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(RellenoLateral, -146f),
                new Vector2(ancho, 110f), string.Empty, TrazoH10.Texto);
            textoEfecto.horizontalOverflow = HorizontalWrapMode.Wrap;

            float y = -264f;
            TrazoH10.Seccion(panel.transform, "EncabezadoTabla", config.EncabezadoEfectos, new Vector2(RellenoLateral, y), ancho);
            y -= 38f;
            for (int i = 0; i < MaxFilasEfectos; i++)
            {
                FilaTabla f = new FilaTabla();
                f.Raiz = new GameObject("Efecto_" + i, typeof(RectTransform));
                f.Raiz.transform.SetParent(panel.transform, false);
                RectTransform rf = (RectTransform)f.Raiz.transform;
                KitUI.Colocar(rf, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(ancho, AltoFilaEfecto));
                f.Nombre = Texto(rf, "Nombre", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(208f, 34f), string.Empty, TrazoH10.Texto);
                f.Antes = Texto(rf, "Antes", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(0f, 1f), new Vector2(212f, 0f), new Vector2(54f, 34f), string.Empty, TrazoH10.Texto);
                f.Flecha = Texto(rf, "Flecha", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(268f, 0f), new Vector2(24f, 34f), "→", TrazoH10.TextoTenue);
                f.Despues = Texto(rf, "Despues", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(294f, 0f), new Vector2(62f, 34f), string.Empty, TrazoH10.Texto);
                f.Diferencia = Texto(rf, "Diferencia", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(1f, 1f), Vector2.zero, new Vector2(60f, 34f), string.Empty, TrazoH10.Texto);
                Image raya = KitUI.CrearImagen(rf, "Raya", TrazoH10.ConAlfa(TrazoH10.TurquesaTenue, 0.2f));
                KitUI.Colocar(raya.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(ancho, 1f));
                filasTabla.Add(f);
                y -= AltoFilaEfecto;
            }

            y -= 10f;
            TrazoH10.Seccion(panel.transform, "TituloTrasCompra", TituloTrasCompra, new Vector2(RellenoLateral, y), ancho);
            textoTrasCompra = TrazoH10.Etiqueta(panel.transform, "TrasCompra", string.Empty, TrazoH10.TamanoCifra, TrazoH10.Texto);
            TrazoH10.Punto(textoTrasCompra.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoLateral, y - 38f), new Vector2(ancho, 54f));
            Text etiquetaOcupados = TrazoH10.Etiqueta(panel.transform, "EtiquetaOcupados", TituloOcupados, TrazoH10.TamanoDato, TrazoH10.TextoTenue);
            TrazoH10.Punto(etiquetaOcupados.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoLateral, y - 98f), new Vector2(ancho, 30f));
            textoOcupados = TrazoH10.Etiqueta(panel.transform, "Ocupados", string.Empty, TrazoH10.TamanoDato, TrazoH10.Texto, TextAnchor.MiddleRight);
            TrazoH10.Punto(textoOcupados.rectTransform, new Vector2(1f, 1f), new Vector2(-RellenoLateral, y - 98f), new Vector2(ancho, 30f));

            textoHoloAviso = Texto(panel.transform, "Aviso", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, AltoBoton + 30f),
                new Vector2(ancho, 30f), string.Empty, TrazoH10.Turquesa);
            textoHoloAviso.rectTransform.pivot = new Vector2(0.5f, 0f);
            textoHoloAviso.gameObject.SetActive(false);
            fondoBoton = TrazoH10.Panel(panel.transform, "BotonInstalar", TrazoH10.Amarillo, Color.clear);
            lineaBoton = fondoBoton.transform.Find("Linea").GetComponent<Image>();
            fondoBoton.raycastTarget = true;
            KitUI.Colocar(fondoBoton.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(ancho, AltoBoton));
            Button instalar = fondoBoton.gameObject.AddComponent<Button>();
            instalar.transition = Selectable.Transition.None;
            instalar.onClick.AddListener(AlClicBoton);
            textoBoton = KitUI.CrearTexto(fondoBoton.transform, "Texto", RolTexto.Boton, TextAnchor.MiddleCenter);
            textoBoton.fontSize = TrazoH10.TamanoFila;
            HoverH10.Agregar(fondoBoton.gameObject, null, Color.clear, Color.clear);
        }

        private void AlClicBoton()
        {
            if (visible)
            {
                AlConfirmar();
            }
        }

        private void ElegirFila(int fila)
        {
            if (!visible || fila == indice)
            {
                return;
            }
            indice = fila;
            confirmando = false;
            textoAviso.gameObject.SetActive(false);
            Sonidos.Reproducir(config.SonidoMover);
            Pintar();
        }

        // Rejilla tenue detrás del holograma (líneas cada 60 px).
        private void Rejilla(RectTransform area)
        {
            const float Paso = 60f;
            Color color = new Color(config.ColorHolograma.r, config.ColorHolograma.g, config.ColorHolograma.b, 0.06f);
            for (float x = Paso; x < AnchoHolo; x += Paso)
            {
                Image l = KitUI.CrearImagen(area, "RejillaV", color);
                KitUI.Colocar(l.rectTransform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(1f, AltoHolo));
            }
            for (float y = Paso; y < AltoHolo; y += Paso)
            {
                Image l = KitUI.CrearImagen(area, "RejillaH", color);
                KitUI.Colocar(l.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(AnchoHolo, 1f));
            }
        }

        // Escuadras en las cuatro esquinas del holograma.
        private static void Escuadras(RectTransform area, Color color)
        {
            Vector2[] esquinas = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            foreach (Vector2 e in esquinas)
            {
                Image h = KitUI.CrearImagen(area, "Escuadra", color);
                KitUI.Colocar(h.rectTransform, e, Vector2.zero, new Vector2(LargoMarca, GrosorMarca));
                Image v = KitUI.CrearImagen(area, "Escuadra", color);
                KitUI.Colocar(v.rectTransform, e, Vector2.zero, new Vector2(GrosorMarca, LargoMarca));
            }
        }

        // Líneas finas de fondo, al estilo de la cabina.
        private static void Lineas(Transform padre)
        {
            const int Cantidad = 18;
            Color color = new Color(1f, 1f, 1f, 0.018f);
            for (int i = 0; i < Cantidad; i++)
            {
                Image l = KitUI.CrearImagen(padre, "LineaFondo", color);
                KitUI.Colocar(l.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, i * (1080f / Cantidad)), new Vector2(1920f, 2f));
            }
        }

        private static int TamanoDe(RolTexto rol)
        {
            switch (rol)
            {
                case RolTexto.Titulo: return TrazoH10.TamanoTitulo;
                case RolTexto.Encabezado: return TrazoH10.TamanoSeccion;
                case RolTexto.Boton:
                case RolTexto.Texto: return TrazoH10.TamanoFila;
                case RolTexto.Hud:
                case RolTexto.Ayuda: return TrazoH10.TamanoDato;
                default: return TrazoH10.TamanoMicro;
            }
        }

        private static Text Texto(Transform padre, string nombre, RolTexto rol, TextAnchor alineacion, Vector2 ancla, Vector2 posicion, Vector2 tamano,
            string contenidoTexto, Color color)
        {
            Text t = KitUI.CrearTexto(padre, nombre, rol, alineacion);
            t.fontSize = TamanoDe(rol);
            KitUI.Colocar(t.rectTransform, ancla, posicion, tamano);
            t.text = contenidoTexto;
            t.color = color;
            return t;
        }
    }
}
