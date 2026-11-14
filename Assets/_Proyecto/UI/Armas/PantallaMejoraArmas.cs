using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// Menú de mejora de armas del banco de Tino (T-814). A la izquierda las armas que traes con su ícono holográfico (H01-H04); al centro
    /// el holograma del arma (<see cref="VistaHologramaArma"/>), que se gira arrastrando y se acerca con la rueda; a la derecha las tres
    /// piezas (cargador, cañón, mira = niveles I, II y III, en ese orden), la tabla antes/después y el botón de mejorar. ← → cambian de
    /// arma, ↑ ↓ de pieza, Enter (o clic) compra la que sigue con la <see cref="ICartera"/> y mete el nivel nuevo al
    /// <see cref="ServicioInventario"/>, Esc sale. Si compraste algo, al salir sale el letrero ARMA MEJORADA. Toda la UI se arma en código
    /// con el kit (<see cref="KitUI"/>). Quien la abre la cierra con <see cref="Ocultar"/>.
    /// </summary>
    public class PantallaMejoraArmas : MonoBehaviour
    {
        private const int OrdenCanvas = 50;
        private const int OrdenLetrero = 35;
        private const float Margen = TrazoH10.MargenPantalla;
        private const float AltoCabecera = 104f;
        private const float AnchoLateral = 440f;
        private const float AltoLateral = 876f;
        private const float AbajoLateral = 88f;
        private const float RellenoLateral = 20f;
        private const float AltoPestana = 40f;
        private const float AltoCategoria = 52f;
        private const float AltoCelda = 176f;
        private const float AltoFilaPieza = 84f;
        private const float LadoIconoPieza = 72f;
        private const float XTextoPieza = 138f;
        private const string RutaIconosPieza = "Menus/Pieza_";
        private const string RutaIconosCategoria = "Menus/Cat_";
        private const string TextoFiltroVacio = "NINGUNA ARMA CON ESTE FILTRO";
        private const string TituloFicha = "ESTADO ACTUAL";
        private const string TituloTrasCompra = "TE QUEDARÍA";
        private const string TituloProgreso = "PROGRESO DEL ARMA";
        private const string EtiquetasProgreso = "PIEZAS INSTALADAS\nFALTA POR INVERTIR";
        private const float AltoBoton = 70f;
        private const float AnchoHolo = 900f;
        private const float AltoHolo = 520f;
        private const float SubeHolo = 82f;
        private const float AltoPanelTabla = 262f;
        private const float AltoFilaTabla = 40f;
        private const float SobreHolo = 86f;
        private const float GrosorLinea = TrazoH10.GrosorLinea;
        private const float GrosorMarca = TrazoH10.GrosorLinea;
        private const float LargoMarca = 24f;
        private const float AlfaDestelloPantalla = 0.55f;
        private const float AlfaBarraAntes = 0.45f;
        private const float AlfaIconoBloqueado = 0.35f;
        private const float AlfaCategoriaSinArma = 0.22f;
        private const float DiametroPunto = 10f;
        private const float SegundosDestello = 0.35f;
        private const int TamanoTituloGrande = TrazoH10.TamanoTitulo;
        private const int TamanoNombreArma = TrazoH10.TamanoCifra;
        private static readonly string[] Romanos = { "", "I", "II", "III" };

        private sealed class FilaArma
        {
            public Image Fondo;
            public GameObject Borde;
            public Image Filo;
            public Text Nombre;
            public Text Nivel;
            public Image[] Marcas;
        }

        private sealed class BotonFiltro
        {
            public Image Linea;
            public Text Texto;
            public RawImage Icono;
            public Button Boton;
            public ArmaAPie Arma;
        }

        private sealed class FilaPieza
        {
            public Image Fondo;
            public GameObject Borde;
            public Text Numeral;
            public Image Icono;
            public Text Nombre;
            public Text Detalle;
            public Text Etiqueta;
        }

        private sealed class FilaTabla
        {
            public Text Nombre;
            public Text Antes;
            public Text Flecha;
            public Text Despues;
            public Text Diferencia;
            public Image BarraAntes;
            public Image BarraDespues;
            public RectTransform Barra;
        }

        [SerializeField] private ConfigMenuArmas config;
        [Tooltip("Arma anterior (←).")]
        [SerializeField] private InputAction izquierda = new InputAction("ArmasIzquierda", InputActionType.Button, "<Keyboard>/leftArrow");
        [Tooltip("Arma siguiente (→).")]
        [SerializeField] private InputAction derecha = new InputAction("ArmasDerecha", InputActionType.Button, "<Keyboard>/rightArrow");
        [Tooltip("Pieza de arriba (↑).")]
        [SerializeField] private InputAction arriba = new InputAction("ArmasArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [Tooltip("Pieza de abajo (↓).")]
        [SerializeField] private InputAction abajo = new InputAction("ArmasAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [Tooltip("Mejorar la pieza elegida (Enter).")]
        [SerializeField] private InputAction confirmar = new InputAction("ArmasEnter", InputActionType.Button, "<Keyboard>/enter");
        [Tooltip("Salir (Esc).")]
        [SerializeField] private InputAction salir = new InputAction("ArmasEsc", InputActionType.Button, "<Keyboard>/escape");
        [Tooltip("Arrastrar para girar el holograma (clic izquierdo).")]
        [SerializeField] private InputAction arrastrar = new InputAction("ArmasArrastrar", InputActionType.Button, "<Mouse>/leftButton");
        [Tooltip("Movimiento del ratón al arrastrar (px).")]
        [SerializeField] private InputAction movimientoRaton = new InputAction("ArmasRaton", InputActionType.Value, "<Mouse>/delta");
        [Tooltip("Rueda del ratón para acercar.")]
        [SerializeField] private InputAction rueda = new InputAction("ArmasRueda", InputActionType.Value, "<Mouse>/scroll/y");

        private readonly List<EntradaArmaMenu> armas = new List<EntradaArmaMenu>();
        private readonly List<EntradaArmaMenu> armasTodas = new List<EntradaArmaMenu>();
        private readonly List<BotonFiltro> pestanas = new List<BotonFiltro>();
        private readonly List<BotonFiltro> categorias = new List<BotonFiltro>();
        private readonly List<FilaArma> filasArmas = new List<FilaArma>();
        private readonly FilaPieza[] filasPiezas = new FilaPieza[ReglaMejoraArma.CantidadPiezas];
        private readonly List<FilaTabla> filasTabla = new List<FilaTabla>();
        private readonly List<FilaEstadistica> comparacion = new List<FilaEstadistica>();
        private readonly Dictionary<PiezaArma, EstadoPieza> estados = new Dictionary<PiezaArma, EstadoPieza>();

        private Canvas canvas;
        private Color Acento => TrazoH10.Turquesa;
        private Image panelArmas;
        private Image panelPiezas;
        private Image panelTabla;
        private Image lineaBoton;
        private Image lineaGuia;
        private Image puntoGuia;
        private Text textoFichaNombres;
        private Text textoFichaValores;
        private Text textoTrasCompra;
        private Text textoProgreso;
        private FiltroArmas filtro;
        private ArmaAPie? categoria;
        private GameObject contenido;
        private RectTransform listaArmas;
        private RectTransform areaHolo;
        private RawImage imagenHolo;
        private Image destelloHolo;
        private Text textoTitulo;
        private Text textoSubtitulo;
        private Text textoDinero;
        private Text textoArma;
        private Text textoNivelArma;
        private Text textoPiezaElegida;
        private Text textoAyuda;
        private Text textoAyudaRaton;
        private Text textoAviso;
        private Text textoBoton;
        private Image fondoBoton;
        private Text textoSinArmas;
        private Canvas canvasLetrero;
        private CanvasGroup grupoLetrero;
        private Text textoLetrero;
        private Text textoLetreroArma;
        private VistaHologramaArma vista;

        private Action alSalir;
        private bool construida;
        private bool visible;
        private int indiceArma;
        private PiezaArma pieza;
        private bool arrastrando;
        private float destello;
        private float animacionNumeros = 1f;
        private bool esperandoEscaneo;
        private float tiempoLetrero;
        private int cuadroAbierta = -1;
        private string ultimaMejora = string.Empty;
        private bool comproAlgo;
        private CursorLockMode cursorAntes;
        private bool cursorVisibleAntes;

        public bool Visible => visible;
        public int CantidadArmas => armas.Count;
        public FiltroArmas Filtro => filtro;
        public ArmaAPie? Categoria => categoria;
        /// <summary>El color del filo de rareza de la casilla del arma.</summary>
        public Color ColorFilo(int indice) => filasArmas[indice].Filo.color;
        public int IndiceArma => indiceArma;
        public PiezaArma PiezaElegida => pieza;
        public EntradaArmaMenu ArmaElegida => indiceArma >= 0 && indiceArma < armas.Count ? armas[indiceArma] : null;
        public VistaHologramaArma Vista => vista;
        public Canvas Canvas => canvas;
        public Canvas CanvasLetrero => canvasLetrero;
        public ConfigMenuArmas Config => config;
        public string Dinero => textoDinero != null ? textoDinero.text : string.Empty;
        public string Aviso => textoAviso != null && textoAviso.gameObject.activeSelf ? textoAviso.text : string.Empty;
        public string TextoBoton => textoBoton != null ? textoBoton.text : string.Empty;
        /// <summary>True mientras se ve el letrero ARMA MEJORADA.</summary>
        public bool LetreroVisible => canvasLetrero != null && canvasLetrero.gameObject.activeSelf;
        public string Letrero => textoLetrero != null ? textoLetrero.text + " " + textoLetreroArma.text : string.Empty;

        /// <summary>El estado de la pieza en el arma elegida.</summary>
        public EstadoPieza Estado(PiezaArma p) => estados.TryGetValue(p, out EstadoPieza e) ? e : EstadoPieza.Bloqueada;

        /// <summary>La etiqueta de la fila de la pieza (precio, INSTALADA o PRIMERO EL ...).</summary>
        public string EtiquetaPieza(PiezaArma p) => filasPiezas[(int)p] != null ? filasPiezas[(int)p].Etiqueta.text : string.Empty;

        /// <summary>Los números de la tabla en una fila: "DAÑO 25 → 30 +5".</summary>
        public string TextoTabla(int fila)
        {
            FilaTabla f = filasTabla[fila];
            return f.Nombre.text + " " + f.Antes.text + " " + f.Flecha.text + " " + f.Despues.text + " " + f.Diferencia.text;
        }

        public int FilasTabla => filasTabla.Count;

        /// <summary>Le da la config si no la trae (el banco crea la pantalla en código).</summary>
        public void Configurar(ConfigMenuArmas configuracion)
        {
            if (config == null)
            {
                config = configuracion;
            }
        }

        private void OnDisable()
        {
            if (visible)
            {
                Ocultar();
            }
            DesactivarEntrada();
        }

        /// <summary>T-865: arma la pantalla escondida (para que el primer Mostrar no tarde). Sin config no hace nada.</summary>
        public void Precargar()
        {
            if (config != null)
            {
                Construir();
            }
        }

        /// <summary>Abre el menú con las armas que traes. <paramref name="alSalirMenu"/> avisa Esc.</summary>
        public void Mostrar(Action alSalirMenu)
        {
            if (config == null)
            {
                Debug.LogError("[UI] Falta ConfigMenuArmas en PantallaMejoraArmas de " + name, this);
                return;
            }
            Construir();
            alSalir = alSalirMenu;
            comproAlgo = false;
            ultimaMejora = string.Empty;
            animacionNumeros = 1f;
            destello = 0f;
            canvasLetrero.gameObject.SetActive(false);
            filtro = FiltroArmas.Todas;
            categoria = null;
            BuscarArmas();
            visible = true;
            cuadroAbierta = Time.frameCount;
            contenido.SetActive(true);
            PilaPantallas.Abrir(this);
            KitUI.AsegurarEventSystem();
            cursorAntes = Cursor.lockState;
            cursorVisibleAntes = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            textoAviso.gameObject.SetActive(false);
            ElegirArma(0, false);
            AnimarEntrada();
            izquierda.Enable();
            derecha.Enable();
            arriba.Enable();
            abajo.Enable();
            confirmar.Enable();
            salir.Enable();
            arrastrar.Enable();
            movimientoRaton.Enable();
            rueda.Enable();
        }

        /// <summary>Cierra el menú; si se compró algo, sale el letrero ARMA MEJORADA.</summary>
        public void Ocultar()
        {
            bool estaba = visible;
            visible = false;
            arrastrando = false;
            PilaPantallas.Cerrar(this);
            DesactivarEntrada();
            if (vista != null)
            {
                vista.Ocultar();
            }
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
            if (estaba)
            {
                Cursor.lockState = cursorAntes;
                Cursor.visible = cursorVisibleAntes;
                if (comproAlgo && canvasLetrero != null && isActiveAndEnabled)
                {
                    MostrarLetrero();
                }
            }
        }

        private void DesactivarEntrada()
        {
            izquierda.Disable();
            derecha.Disable();
            arriba.Disable();
            abajo.Disable();
            confirmar.Disable();
            salir.Disable();
            arrastrar.Disable();
            movimientoRaton.Disable();
            rueda.Disable();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            AnimarLetrero(dt);
            if (!visible)
            {
                return;
            }
            AnimarCompra(dt);
            if (!PilaPantallas.EsArriba(this) || Time.frameCount == cuadroAbierta)
            {
                return;
            }
            if (salir.WasPressedThisFrame())
            {
                Sonidos.Reproducir(config.SonidoCancelar);
                alSalir?.Invoke();
                return;
            }
            if (armas.Count > 0)
            {
                if (izquierda.WasPressedThisFrame())
                {
                    ElegirArma((indiceArma + armas.Count - 1) % armas.Count, true);
                }
                else if (derecha.WasPressedThisFrame())
                {
                    ElegirArma((indiceArma + 1) % armas.Count, true);
                }
                if (arriba.WasPressedThisFrame())
                {
                    ElegirPieza(PiezaVecina(-1), true);
                }
                else if (abajo.WasPressedThisFrame())
                {
                    ElegirPieza(PiezaVecina(1), true);
                }
                if (confirmar.WasPressedThisFrame())
                {
                    Comprar();
                }
            }
            GirarConRaton();
        }

        private void LateUpdate()
        {
            if (visible)
            {
                ActualizarGuia();
            }
        }

        // La línea guía va de la pieza elegida, donde esté en el holograma, a su rótulo.
        private void ActualizarGuia()
        {
            bool hay = armas.Count > 0 && vista != null && vista.Camara != null && vista.CentroPieza(pieza, out Vector3 _);
            lineaGuia.enabled = hay;
            puntoGuia.enabled = hay;
            if (!hay)
            {
                return;
            }
            vista.CentroPieza(pieza, out Vector3 centro);
            Vector3 v = vista.Camara.WorldToViewportPoint(centro);
            Rect uv = imagenHolo.uvRect;
            Vector2 punto = new Vector2(v.x * AnchoHolo, (v.y - uv.y) / uv.height * AltoHolo);
            TrazoH10.Unir(lineaGuia.rectTransform, punto, new Vector2(AnchoHolo - 24f, AltoHolo - 52f));
            puntoGuia.rectTransform.anchoredPosition = punto;
        }

        private void AnimarEntrada()
        {
            float d = AnimH10.DesplazamientoEntrada;
            AnimH10.EntrarPanel(panelArmas.rectTransform, new Vector2(-d, 0f));
            AnimH10.EntrarPanel(panelPiezas.rectTransform, new Vector2(d, 0f), AnimH10.SegundosEscalon);
            AnimH10.EntrarPanel(panelTabla.rectTransform, new Vector2(0f, -d), AnimH10.SegundosEscalon * 2f);
            AnimH10.EntrarPanel(areaHolo, new Vector2(0f, d), AnimH10.SegundosEscalon * 3f);
        }

        private void GirarConRaton()
        {
            if (vista == null || Mouse.current == null)
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
                vista.Girar(movimientoRaton.ReadValue<Vector2>());
            }
            float scroll = rueda.ReadValue<float>();
            if (encima && Mathf.Abs(scroll) > 0f)
            {
                vista.Acercar(scroll);
            }
        }

        private Camera CamaraDelCanvas() => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        // ------------------------------------------------------------------ Lógica

        private void BuscarArmas()
        {
            armasTodas.Clear();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            foreach (EntradaArmaMenu entrada in config.Armas)
            {
                if (entrada != null && arsenal != null && arsenal.Tiene(entrada.Arma))
                {
                    armasTodas.Add(entrada);
                }
            }
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            armas.Clear();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            foreach (EntradaArmaMenu entrada in armasTodas)
            {
                bool categoriaBien = !categoria.HasValue || categoria.Value == entrada.Arma;
                if (categoriaBien && ReglaMejoraArma.PasaFiltro(filtro, arsenal != null ? arsenal.Nivel(entrada.Arma) : 0, entrada.NivelMaximo))
                {
                    armas.Add(entrada);
                }
            }
            textoSinArmas.text = armasTodas.Count == 0 ? config.TextoSinArmas : TextoFiltroVacio;
            ConstruirFilasArmas();
        }

        /// <summary>Cambia la pestaña de la lista (TODAS, MEJORABLES, AL MÁXIMO).</summary>
        public void FijarFiltro(FiltroArmas nuevo)
        {
            filtro = nuevo;
            AplicarFiltros();
            ElegirArma(0, true);
        }

        /// <summary>Filtra por tipo de arma; elegir el mismo otra vez quita el filtro.</summary>
        public void FijarCategoria(ArmaAPie arma)
        {
            categoria = categoria == arma ? (ArmaAPie?)null : arma;
            AplicarFiltros();
            ElegirArma(0, true);
        }

        // El nivel más alto del arma elegida (2 la escopeta, que no lleva mira; 3 las demás).
        private int MaximoActual() => ArmaElegida != null ? ArmaElegida.NivelMaximo : ReglaMejoraArma.NivelMaximo;

        // La pieza que sigue en la lista saltando la mira del arma que no la lleva.
        private PiezaArma PiezaVecina(int paso)
        {
            PiezaArma candidata = pieza;
            for (int i = 0; i < ReglaMejoraArma.CantidadPiezas; i++)
            {
                candidata = (PiezaArma)(((int)candidata + paso + ReglaMejoraArma.CantidadPiezas) % ReglaMejoraArma.CantidadPiezas);
                if (candidata != PiezaArma.Mira || ArmaElegida == null || ArmaElegida.TieneMira)
                {
                    return candidata;
                }
            }
            return pieza;
        }

        // Lo que dice la fila de la mira en el arma que no la lleva (la fila queda apagada para no romper la lista).
        private const string TextoSinPieza = "SIN MIRA";

        private int NivelActual()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            EntradaArmaMenu entrada = ArmaElegida;
            return arsenal != null && entrada != null ? arsenal.Nivel(entrada.Arma) : -1;
        }

        private DatosArma DatosNivel(int nivel)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            EntradaArmaMenu entrada = ArmaElegida;
            return arsenal != null && arsenal.Catalogo != null && entrada != null ? arsenal.Catalogo.Buscar(entrada.Arma, nivel) : null;
        }

        /// <summary>Elige el arma de la lista (también con clic).</summary>
        public void ElegirArma(int indice, bool conSonido)
        {
            if (armas.Count == 0)
            {
                indiceArma = 0;
                textoSinArmas.gameObject.SetActive(true);
                vista.Ocultar();
                imagenHolo.enabled = false;
                Pintar();
                return;
            }
            textoSinArmas.gameObject.SetActive(false);
            indiceArma = Mathf.Clamp(indice, 0, armas.Count - 1);
            vista.Mostrar(ArmaElegida, config);
            imagenHolo.texture = vista.Textura;
            imagenHolo.enabled = true;
            pieza = ReglaMejoraArma.Siguiente(NivelActual(), MaximoActual());
            animacionNumeros = 1f;
            textoAviso.gameObject.SetActive(false);
            if (conSonido)
            {
                Sonidos.Reproducir(config.SonidoMover);
            }
            Pintar();
            AnimH10.Destellar(filasArmas[indiceArma].Fondo, TrazoH10.TurquesaTenue, SegundosDestello);
        }

        /// <summary>Elige la pieza (también con clic): se separa del arma y la tabla muestra su mejora.</summary>
        public void ElegirPieza(PiezaArma nueva, bool conSonido)
        {
            if (nueva == PiezaArma.Mira && ArmaElegida != null && !ArmaElegida.TieneMira)
            {
                return;
            }
            pieza = nueva;
            animacionNumeros = 1f;
            textoAviso.gameObject.SetActive(false);
            if (conSonido)
            {
                Sonidos.Reproducir(config.SonidoMover);
            }
            Pintar();
            AnimH10.Destellar(filasPiezas[(int)pieza].Fondo, TrazoH10.TurquesaTenue, SegundosDestello);
        }

        /// <summary>Compra la pieza elegida si es la que sigue (Enter o el botón).</summary>
        public void Comprar()
        {
            if (!visible || armas.Count == 0)
            {
                return;
            }
            Sonidos.Reproducir(config.SonidoAceptar);
            int nivel = NivelActual();
            EstadoPieza estado = ReglaMejoraArma.Estado(pieza, nivel, MaximoActual());
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            DatosArma siguiente = DatosNivel(ReglaMejoraArma.NivelDe(pieza));
            if (!ReglaMejoraArma.HaySiguiente(nivel, MaximoActual()))
            {
                MostrarAviso(config.TextoAlMaximo, Color.white);
            }
            else if (estado == EstadoPieza.Instalada)
            {
                MostrarAviso(config.TextoInstalada, TrazoH10.Turquesa);
            }
            else if (estado == EstadoPieza.Bloqueada)
            {
                Sonidos.Reproducir(config.SonidoCancelar);
                MostrarAviso(TextoBloqueada(nivel), TrazoH10.TextoTenue);
            }
            else if (siguiente == null || inventario == null || cartera == null || !inventario.PuedeAgregar(siguiente, 1))
            {
                MostrarAviso(config.TextoNoDisponible, TrazoH10.Coral);
            }
            else if (!cartera.IntentarPagar(ReglaMejoraArma.Precio(siguiente), MotivoDinero.Compra))
            {
                Sonidos.Reproducir(config.SonidoSinDinero);
                MostrarAviso(config.TextoNoAlcanza, TrazoH10.Coral);
            }
            else
            {
                inventario.Agregar(siguiente, 1);
                Sonidos.Reproducir(config.SonidoCompra);
                comproAlgo = true;
                ultimaMejora = ArmaElegida.Nombre + " " + Romanos[siguiente.Nivel];
                // T-895: primero el escaneo y la pieza regresando; el destello y los números suben cuando termina.
                animacionNumeros = 0f;
                esperandoEscaneo = true;
                vista.Escanear(() =>
                {
                    esperandoEscaneo = false;
                    destello = 1f;
                });
                MostrarAviso(string.Format(config.FormatoComprado, config.Pieza(pieza).Nombre, ArmaElegida.Nombre), TrazoH10.Amarillo);
            }
            Pintar();
        }

        private string TextoBloqueada(int nivel)
        {
            return string.Format(config.FormatoBloqueada, config.Pieza(ReglaMejoraArma.PiezaQueFalta(pieza, nivel, MaximoActual())).Nombre);
        }

        private void MostrarAviso(string texto, Color color)
        {
            textoAviso.text = texto;
            textoAviso.color = color;
            textoAviso.gameObject.SetActive(true);
        }

        private void AnimarCompra(float dt)
        {
            bool repintar = false;
            if (esperandoEscaneo && !vista.Escaneando)
            {
                esperandoEscaneo = false; // se cambió de arma a la mitad: el escaneo se canceló
            }
            if (animacionNumeros < 1f && !esperandoEscaneo)
            {
                animacionNumeros = Mathf.Min(1f, animacionNumeros + dt / config.DuracionNumeros);
                repintar = true;
            }
            if (destello > 0f)
            {
                destello = Mathf.Max(0f, destello - dt / config.DuracionDestello);
                Color c = config.ColorHolograma;
                destelloHolo.color = new Color(c.r, c.g, c.b, destello * AlfaDestelloPantalla);
                repintar = true;
            }
            if (repintar)
            {
                PintarPiezas(NivelActual());
                PintarTabla(NivelActual());
            }
        }

        // ------------------------------------------------------------------ Pintar

        private void Pintar()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            textoDinero.text = FormatoDinero.Texto(cartera != null ? cartera.Dinero : 0);
            int nivel = NivelActual();
            for (int i = 0; i < filasArmas.Count; i++)
            {
                PintarFilaArma(i);
            }
            EntradaArmaMenu entrada = ArmaElegida;
            bool hay = entrada != null && armas.Count > 0;
            textoArma.text = hay ? entrada.Nombre : string.Empty;
            textoNivelArma.text = hay ? TextoNivel(nivel, entrada.NivelMaximo) : string.Empty;
            estados.Clear();
            for (int i = 0; i < ReglaMejoraArma.CantidadPiezas; i++)
            {
                estados[(PiezaArma)i] = hay ? ReglaMejoraArma.Estado((PiezaArma)i, nivel, entrada.NivelMaximo) : EstadoPieza.Bloqueada;
            }
            EntradaPiezaMenu datosPieza = config.Pieza(pieza);
            textoPiezaElegida.text = hay ? Romanos[ReglaMejoraArma.NivelDe(pieza)] + " · " + datosPieza.Nombre + " · " + TextoMejora(pieza) : string.Empty;
            PintarPiezas(nivel);
            PintarTabla(nivel);
            PintarBoton(nivel, cartera);
            PintarFiltros();
            PintarFicha(nivel, cartera);
            if (hay)
            {
                vista.ElegirPieza(pieza, estados);
            }
        }

        /// <summary>
        /// Lo que mejora la pieza en el arma elegida: su texto (MÁS PRECISIÓN) si su estadística cambia; si no (la mira de la pistola), la
        /// estadística que de verdad mejora con ese nivel (MEJORA DAÑO), con los números de <see cref="DatosArma"/>.
        /// </summary>
        public string TextoMejora(PiezaArma p)
        {
            EntradaPiezaMenu datos = config.Pieza(p);
            if (armas.Count == 0)
            {
                return datos.Mejora;
            }
            int nivelPieza = ReglaMejoraArma.NivelDe(p);
            EstadisticaArma real = ReglaMejoraArma.Resaltada(EstadisticasArma.De(DatosNivel(nivelPieza - 1)), EstadisticasArma.De(DatosNivel(nivelPieza)),
                datos.Estadistica);
            return real == datos.Estadistica ? datos.Mejora : string.Format(config.FormatoMejoraReal, config.NombreEstadistica(real));
        }

        private string TextoNivel(int nivel, int maximo) => nivel <= 0 ? config.TextoBase : string.Format(config.FormatoNivel, Romanos[Mathf.Min(nivel, maximo)]);

        private void PintarFilaArma(int i)
        {
            FilaArma f = filasArmas[i];
            bool elegida = i == indiceArma;
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            int nivel = arsenal != null ? arsenal.Nivel(armas[i].Arma) : 0;
            f.Fondo.color = elegida ? TrazoH10.TurquesaFondo : TrazoH10.FondoPanel;
            f.Borde.SetActive(elegida);
            f.Filo.color = TrazoH10.Rareza(nivel);
            f.Nombre.color = TrazoH10.Texto;
            f.Nivel.text = TextoNivel(nivel, armas[i].NivelMaximo);
            for (int m = 0; m < f.Marcas.Length; m++)
            {
                f.Marcas[m].gameObject.SetActive(m < armas[i].NivelMaximo);
                f.Marcas[m].color = m < nivel ? TrazoH10.Turquesa : TrazoH10.ConAlfa(TrazoH10.TextoTenue, 0.25f);
            }
        }

        private void PintarFiltros()
        {
            for (int i = 0; i < pestanas.Count; i++)
            {
                bool activa = (int)filtro == i;
                pestanas[i].Linea.color = activa ? TrazoH10.Turquesa : TrazoH10.TurquesaTenue;
                pestanas[i].Texto.color = activa ? TrazoH10.Turquesa : TrazoH10.TextoTenue;
            }
            foreach (BotonFiltro c in categorias)
            {
                bool tiene = armasTodas.Exists(e => e.Arma == c.Arma);
                bool activa = categoria == c.Arma;
                c.Boton.interactable = tiene;
                c.Linea.color = activa ? TrazoH10.Turquesa : TrazoH10.TurquesaTenue;
                c.Icono.color = !tiene ? TrazoH10.ConAlfa(TrazoH10.TextoTenue, AlfaCategoriaSinArma) : activa ? TrazoH10.Turquesa : TrazoH10.Texto;
            }
        }

        private void PintarProgreso(int nivel)
        {
            if (armas.Count == 0)
            {
                textoProgreso.text = "—\n—";
                return;
            }
            int falta = 0;
            for (int n = nivel + 1; n <= MaximoActual(); n++)
            {
                DatosArma datos = DatosNivel(n);
                falta += datos != null ? ReglaMejoraArma.Precio(datos) : 0;
            }
            textoProgreso.text = Mathf.Clamp(nivel, 0, MaximoActual()) + " / " + MaximoActual() + "\n" + FormatoDinero.Texto(falta);
        }

        // Lo que el arma hace hoy y cómo queda el dinero si se compra la pieza elegida.
        private void PintarFicha(int nivel, ICartera cartera)
        {
            DatosArma actual = armas.Count > 0 ? DatosNivel(nivel) : null;
            string nombres = string.Empty;
            string valores = string.Empty;
            if (actual != null)
            {
                EstadisticasArma e = EstadisticasArma.De(actual);
                foreach (EstadisticaArma estadistica in ReglaMejoraArma.OrdenEstadisticas)
                {
                    nombres += config.NombreEstadistica(estadistica) + "\n";
                    valores += ReglaMejoraArma.Texto(estadistica, e.Valor(estadistica)) + "\n";
                }
            }
            textoFichaNombres.text = nombres;
            textoFichaValores.text = valores;
            PintarProgreso(nivel);
            bool disponible = armas.Count > 0 && Estado(pieza) == EstadoPieza.Disponible && cartera != null;
            if (!disponible)
            {
                textoTrasCompra.text = "—";
                textoTrasCompra.color = TrazoH10.TextoTenue;
                return;
            }
            int resto = cartera.Dinero - ReglaMejoraArma.Precio(DatosNivel(ReglaMejoraArma.NivelDe(pieza)));
            textoTrasCompra.text = FormatoDinero.Texto(Mathf.Max(0, resto));
            textoTrasCompra.color = resto >= 0 ? TrazoH10.Texto : TrazoH10.Coral;
        }

        private void PintarPiezas(int nivel)
        {
            EstiloCyberfix estilo = KitUI.Estilo;
            ICartera cartera = Registro<ICartera>.Actual;
            for (int i = 0; i < filasPiezas.Length; i++)
            {
                PiezaArma p = (PiezaArma)i;
                FilaPieza f = filasPiezas[i];
                EstadoPieza estado = Estado(p);
                bool elegida = p == pieza && armas.Count > 0;
                EntradaPiezaMenu datos = config.Pieza(p);
                float brillo = elegida && destello > 0f ? destello : 0f;
                Color colorFila = TrazoH10.Texto;
                f.Fondo.color = elegida ? TrazoH10.ConAlfa(TrazoH10.Turquesa, TrazoH10.TurquesaFondo.a + brillo * 0.2f) : TrazoH10.FondoPanel;
                f.Borde.SetActive(elegida);
                f.Numeral.text = Romanos[ReglaMejoraArma.NivelDe(p)];
                f.Numeral.color = estado == EstadoPieza.Instalada ? TrazoH10.Turquesa : estado == EstadoPieza.Bloqueada ? estilo.Desactivado : colorFila;
                f.Icono.color = estado == EstadoPieza.Bloqueada ? new Color(1f, 1f, 1f, AlfaIconoBloqueado) : Color.white;
                f.Nombre.text = datos.Nombre;
                f.Nombre.color = estado == EstadoPieza.Bloqueada && !elegida ? estilo.Desactivado : colorFila;
                f.Detalle.text = estado == EstadoPieza.Bloqueada ? string.Empty : TextoMejora(p);
                f.Detalle.color = estado == EstadoPieza.Bloqueada ? estilo.Desactivado : new Color(estilo.Texto.r, estilo.Texto.g, estilo.Texto.b, 0.75f);
                switch (estado)
                {
                    case EstadoPieza.Instalada:
                        f.Etiqueta.text = config.TextoInstalada;
                        f.Etiqueta.color = TrazoH10.Turquesa;
                        f.Etiqueta.fontSize = TrazoH10.TamanoDato;
                        break;
                    case EstadoPieza.Disponible:
                        int precio = ReglaMejoraArma.Precio(DatosNivel(ReglaMejoraArma.NivelDe(p)));
                        f.Etiqueta.text = FormatoDinero.Texto(precio);
                        f.Etiqueta.color = cartera != null && cartera.PuedePagar(precio) ? TrazoH10.Amarillo : TrazoH10.Coral;
                        f.Etiqueta.fontSize = TrazoH10.TamanoDato;
                        break;
                    default:
                        f.Etiqueta.text = armas.Count == 0 ? string.Empty
                            : ReglaMejoraArma.NivelDe(p) > MaximoActual() ? TextoSinPieza
                            : string.Format(config.FormatoBloqueada, config.Pieza(ReglaMejoraArma.PiezaQueFalta(p, nivel, MaximoActual())).Nombre);
                        f.Etiqueta.color = TrazoH10.TextoTenue;
                        f.Etiqueta.fontSize = TrazoH10.TamanoMicro;
                        break;
                }
            }
        }

        private void PintarTabla(int nivel)
        {
            EstiloCyberfix estilo = KitUI.Estilo;
            int nivelPieza = ReglaMejoraArma.NivelDe(pieza);
            DatosArma antes = armas.Count > 0 ? DatosNivel(nivelPieza - 1) : null;
            DatosArma despues = armas.Count > 0 ? DatosNivel(nivelPieza) : null;
            DatosArma referencia = armas.Count > 0 ? DatosNivel(MaximoActual()) : null;
            EstadisticasArma a = EstadisticasArma.De(antes);
            EstadisticasArma d = EstadisticasArma.De(despues);
            EstadisticasArma r = EstadisticasArma.De(referencia);
            ReglaMejoraArma.Comparar(a, d, config.Pieza(pieza).Estadistica, comparacion);
            bool bloqueada = Estado(pieza) == EstadoPieza.Bloqueada;
            float t = Mathf.SmoothStep(0f, 1f, animacionNumeros);
            for (int i = 0; i < filasTabla.Count; i++)
            {
                FilaTabla f = filasTabla[i];
                bool hay = antes != null && despues != null && i < comparacion.Count;
                f.Nombre.gameObject.transform.parent.gameObject.SetActive(hay);
                if (!hay)
                {
                    continue;
                }
                FilaEstadistica fila = comparacion[i];
                float mostrado = Mathf.Lerp(fila.Antes, fila.Despues, t);
                f.Nombre.text = config.NombreEstadistica(fila.Estadistica);
                f.Antes.text = ReglaMejoraArma.Texto(fila.Estadistica, fila.Antes);
                f.Despues.text = ReglaMejoraArma.Texto(fila.Estadistica, mostrado);
                f.Diferencia.text = ReglaMejoraArma.TextoDiferencia(fila.Estadistica, fila.Antes, fila.Despues);
                Color colorCambio = fila.Cambio > 0 ? TrazoH10.Amarillo : fila.Cambio < 0 ? TrazoH10.Coral : estilo.Desactivado;
                Color colorNombre = fila.Resaltada ? Acento : estilo.Texto;
                if (bloqueada)
                {
                    colorCambio = estilo.Desactivado;
                    colorNombre = fila.Resaltada ? colorNombre : estilo.Desactivado;
                }
                f.Nombre.color = colorNombre;
                f.Antes.color = bloqueada ? estilo.Desactivado : new Color(estilo.Texto.r, estilo.Texto.g, estilo.Texto.b, 0.7f);
                f.Flecha.color = fila.Resaltada ? Acento : estilo.Desactivado;
                f.Despues.color = fila.Cambio != 0 ? colorCambio : f.Antes.color;
                f.Diferencia.color = colorCambio;
                float fa = ReglaMejoraArma.Fraccion(fila.Estadistica, fila.Antes, r.Valor(fila.Estadistica));
                float fd = ReglaMejoraArma.Fraccion(fila.Estadistica, mostrado, r.Valor(fila.Estadistica));
                Llenar(f.BarraAntes, Mathf.Min(fa, fd));
                Llenar(f.BarraDespues, Mathf.Max(fa, fd));
                Color barra = fila.Resaltada ? Acento : config.ColorHolograma;
                f.BarraAntes.color = new Color(barra.r, barra.g, barra.b, AlfaBarraAntes);
                f.BarraDespues.color = fila.Cambio != 0 && !bloqueada ? colorCambio : new Color(barra.r, barra.g, barra.b, AlfaBarraAntes);
            }
        }

        private static void Llenar(Image barra, float fraccion)
        {
            RectTransform rect = barra.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(fraccion), 1f);
        }

        private void PintarBoton(int nivel, ICartera cartera)
        {
            EstiloCyberfix estilo = KitUI.Estilo;
            EstadoPieza estado = Estado(pieza);
            Color color;
            if (armas.Count == 0)
            {
                textoBoton.text = config.TextoSinArmas;
                color = estilo.Desactivado;
            }
            else if (!ReglaMejoraArma.HaySiguiente(nivel, MaximoActual()))
            {
                textoBoton.text = config.TextoAlMaximo;
                color = TrazoH10.Turquesa;
            }
            else if (estado == EstadoPieza.Disponible)
            {
                int precio = ReglaMejoraArma.Precio(DatosNivel(ReglaMejoraArma.NivelDe(pieza)));
                textoBoton.text = string.Format(config.FormatoMejorar, FormatoDinero.Texto(precio));
                color = cartera != null && cartera.PuedePagar(precio) ? TrazoH10.Amarillo : TrazoH10.Coral;
            }
            else if (estado == EstadoPieza.Instalada)
            {
                textoBoton.text = config.TextoInstalada;
                color = TrazoH10.Turquesa;
            }
            else
            {
                textoBoton.text = TextoBloqueada(nivel);
                color = estilo.Desactivado;
            }
            textoBoton.color = color;
            lineaBoton.color = color;
            fondoBoton.color = new Color(color.r, color.g, color.b, estado == EstadoPieza.Disponible ? 0.14f : 0.05f);
        }

        // ------------------------------------------------------------------ Letrero al salir

        private void MostrarLetrero()
        {
            canvasLetrero.gameObject.SetActive(true);
            textoLetreroArma.text = string.Format(config.FormatoLetrero, ultimaMejora);
            tiempoLetrero = 0f;
            grupoLetrero.alpha = 0f;
            Sonidos.Reproducir(config.SonidoAceptar);
        }

        private void AnimarLetrero(float dt)
        {
            if (canvasLetrero == null || !canvasLetrero.gameObject.activeSelf)
            {
                return;
            }
            const float Entrada = 0.18f;
            const float Salida = 0.5f;
            const float Golpe = 0.25f;
            tiempoLetrero += dt;
            float duracion = config.DuracionLetrero;
            float alfa = tiempoLetrero < Entrada ? tiempoLetrero / Entrada
                : tiempoLetrero > duracion - Salida ? Mathf.Clamp01((duracion - tiempoLetrero) / Salida) : 1f;
            grupoLetrero.alpha = alfa;
            float escala = 1f + Golpe * Mathf.Exp(-tiempoLetrero * 10f);
            textoLetrero.rectTransform.localScale = Vector3.one * escala;
            if (tiempoLetrero >= duracion)
            {
                canvasLetrero.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ Construir

        private void Construir()
        {
            if (construida)
            {
                return;
            }
            construida = true;
            vista = gameObject.AddComponent<VistaHologramaArma>();
            EstiloCyberfix estilo = KitUI.Estilo;
            canvas = KitUI.CrearCanvas(transform, "LienzoMejoraArmas", OrdenCanvas);
            contenido = canvas.gameObject;

            Image fondo = KitUI.CrearImagen(canvas.transform, "Fondo", config.ColorFondo);
            KitUI.Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;
            Lineas(canvas.transform);

            // Cabecera
            textoTitulo = Texto(canvas.transform, "Titulo", RolTexto.Titulo, TextAnchor.MiddleLeft, new Vector2(0f, 1f),
                new Vector2(Margen, -14f), new Vector2(900f, 60f), config.Titulo, TrazoH10.Texto);
            textoTitulo.fontSize = TamanoTituloGrande;
            textoSubtitulo = Texto(canvas.transform, "Subtitulo", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f),
                new Vector2(Margen + 2f, -72f), new Vector2(900f, 28f), config.Subtitulo, TrazoH10.Turquesa);
            TrazoH10.Recta(canvas.transform, "TiraCabecera", TrazoH10.TurquesaTenue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -AltoCabecera), new Vector2(-2f * Margen, GrosorLinea));
            RectTransform regla = TrazoH10.Regla(canvas.transform, "Regla", 41, 12f, 10f, TrazoH10.TurquesaTenue);
            TrazoH10.Punto(regla, new Vector2(1f, 1f), new Vector2(-Margen, -AltoCabecera - 2f), regla.sizeDelta);
            Texto(canvas.transform, "EtiquetaDinero", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(1f, 1f),
                new Vector2(-Margen, -72f), new Vector2(400f, 28f), "TU DINERO", TrazoH10.TextoTenue);
            textoDinero = Texto(canvas.transform, "Dinero", RolTexto.Titulo, TextAnchor.MiddleRight, new Vector2(1f, 1f),
                new Vector2(-Margen, -14f), new Vector2(500f, 60f), string.Empty, TrazoH10.Turquesa);
            textoDinero.fontSize = TrazoH10.TamanoCifra;

            ConstruirLista(estilo);
            ConstruirHolograma(estilo);
            ConstruirPiezas(estilo);
            ConstruirTabla(estilo);

            textoAyuda = Texto(canvas.transform, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(1400f, 32f), config.TextoAyuda, TrazoH10.TextoTenue);
            TrazoH10.Recta(canvas.transform, "TiraPie", TrazoH10.TurquesaTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 72f), new Vector2(-2f * Margen, GrosorLinea));

            ConstruirLetrero(estilo);
            PonerFuentesH10();
            contenido.SetActive(false);
        }

        // T-887: la letra del HUD H10 (Rajdhani) en todo el menú y su letrero; la mediana en títulos y números grandes.
        private void PonerFuentesH10()
        {
            if (config.FuenteTexto == null)
            {
                return;
            }
            foreach (Canvas c in new[] { canvas, canvasLetrero })
            {
                foreach (Text t in c.GetComponentsInChildren<Text>(true))
                {
                    bool grande = t.fontSize >= TamanoNombreArma - 20;
                    t.font = grande && config.FuenteTitulo != null ? config.FuenteTitulo : config.FuenteTexto;
                    t.fontStyle = FontStyle.Normal;
                }
            }
        }

        private static Image PanelH10(Transform padre, string nombre) => TrazoH10.Panel(padre, nombre, TrazoH10.TurquesaTenue);

        private void ConstruirLista(EstiloCyberfix estilo)
        {
            panelArmas = PanelH10(canvas.transform, "PanelArmas");
            Image panel = panelArmas;
            KitUI.Colocar(panel.rectTransform, new Vector2(0f, 0f), new Vector2(Margen, AbajoLateral), new Vector2(AnchoLateral, AltoLateral));
            float ancho = AnchoLateral - 2f * RellenoLateral;
            TrazoH10.Seccion(panel.transform, "Encabezado", config.EncabezadoArmas, new Vector2(RellenoLateral, -12f), ancho);
            ConstruirFiltros(panel.transform, ancho);
            GameObject lista = new GameObject("Lista", typeof(RectTransform));
            lista.transform.SetParent(panel.transform, false);
            listaArmas = (RectTransform)lista.transform;
            float altoLista = 2f * AltoCelda + TrazoH10.SeparacionModulo;
            KitUI.Colocar(listaArmas, new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(ancho, altoLista));
            textoSinArmas = Texto(panel.transform, "SinArmas", RolTexto.Texto, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -172f - altoLista * 0.5f),
                new Vector2(ancho, 80f), config.TextoSinArmas, TrazoH10.TextoTenue);
            textoSinArmas.gameObject.SetActive(false);
            float yFicha = -172f - altoLista - 24f;
            TrazoH10.Seccion(panel.transform, "TituloFicha", TituloFicha, new Vector2(RellenoLateral, yFicha), ancho);
            textoFichaNombres = TrazoH10.Etiqueta(panel.transform, "FichaNombres", string.Empty, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.UpperLeft);
            TrazoH10.Punto(textoFichaNombres.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoLateral, yFicha - 40f), new Vector2(ancho, 200f));
            textoFichaValores = TrazoH10.Etiqueta(panel.transform, "FichaValores", string.Empty, TrazoH10.TamanoDato, TrazoH10.Texto, TextAnchor.UpperRight);
            TrazoH10.Punto(textoFichaValores.rectTransform, new Vector2(1f, 1f), new Vector2(-RellenoLateral, yFicha - 40f), new Vector2(ancho, 200f));
        }

        private void ConstruirFiltros(Transform panel, float ancho)
        {
            string[] textos = { "TODAS", "MEJORABLES", "AL MÁXIMO" };
            float anchoPestana = (ancho - 2f * 8f) / textos.Length;
            for (int i = 0; i < textos.Length; i++)
            {
                FiltroArmas valor = (FiltroArmas)i;
                BotonFiltro b = CrearBotonFiltro(panel, "Pestana_" + valor, new Vector2(RellenoLateral + i * (anchoPestana + 8f), -56f), new Vector2(anchoPestana, AltoPestana));
                b.Boton.onClick.AddListener(() => FijarFiltro(valor));
                b.Texto = TrazoH10.Etiqueta(b.Linea.transform.parent, "Texto", textos[i], TrazoH10.TamanoMicro, TrazoH10.TextoTenue, TextAnchor.MiddleCenter);
                TrazoH10.Estirar(b.Texto.rectTransform);
                pestanas.Add(b);
            }
            int cuantas = config.Armas.Count;
            float anchoCategoria = (ancho - (cuantas - 1) * 8f) / Mathf.Max(1, cuantas);
            for (int i = 0; i < cuantas; i++)
            {
                ArmaAPie arma = config.Armas[i].Arma;
                BotonFiltro b = CrearBotonFiltro(panel, "Categoria_" + arma, new Vector2(RellenoLateral + i * (anchoCategoria + 8f), -108f), new Vector2(anchoCategoria, AltoCategoria));
                b.Arma = arma;
                b.Boton.onClick.AddListener(() => FijarCategoria(arma));
                b.Icono = new GameObject("Icono", typeof(RectTransform)).AddComponent<RawImage>();
                b.Icono.transform.SetParent(b.Linea.transform.parent, false);
                b.Icono.texture = Resources.Load<Texture2D>(RutaIconosCategoria + arma);
                b.Icono.enabled = b.Icono.texture != null;
                b.Icono.raycastTarget = false;
                KitUI.Colocar(b.Icono.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(AltoCategoria - 6f, AltoCategoria - 6f));
                categorias.Add(b);
            }
        }

        private static BotonFiltro CrearBotonFiltro(Transform panel, string nombre, Vector2 posicion, Vector2 tamano)
        {
            Image fondo = TrazoH10.Panel(panel, nombre, TrazoH10.TurquesaTenue, TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.3f));
            fondo.raycastTarget = true;
            TrazoH10.Punto(fondo.rectTransform, new Vector2(0f, 1f), posicion, tamano);
            BotonFiltro b = new BotonFiltro { Linea = fondo.transform.Find("Linea").GetComponent<Image>() };
            b.Boton = fondo.gameObject.AddComponent<Button>();
            b.Boton.transition = Selectable.Transition.None;
            return b;
        }

        private void ConstruirFilasArmas()
        {
            foreach (Transform hijo in listaArmas)
            {
                Destroy(hijo.gameObject);
            }
            filasArmas.Clear();
            float anchoCelda = (listaArmas.sizeDelta.x - TrazoH10.SeparacionModulo) * 0.5f;
            for (int i = 0; i < armas.Count; i++)
            {
                int indice = i;
                EntradaArmaMenu entrada = armas[i];
                FilaArma f = new FilaArma();
                f.Fondo = PanelH10(listaArmas, "Arma_" + entrada.Arma);
                f.Fondo.raycastTarget = true;
                int fila = i / 2;
                int columna = i % 2;
                TrazoH10.Punto(f.Fondo.rectTransform, new Vector2(0f, 1f), new Vector2(columna * (anchoCelda + TrazoH10.SeparacionModulo), -fila * (AltoCelda + TrazoH10.SeparacionModulo)),
                    new Vector2(anchoCelda, AltoCelda));
                Button boton = f.Fondo.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => ElegirArma(indice, true));
                Image linea = f.Fondo.transform.Find("Linea").GetComponent<Image>();
                HoverH10.Agregar(f.Fondo.gameObject, linea, TrazoH10.TurquesaTenue, TrazoH10.Turquesa);
                Image borde = TrazoH10.Imagen(f.Fondo.transform, "Elegida", TrazoH10.Turquesa, TrazoH10.Linea);
                TrazoH10.Estirar(borde.rectTransform);
                f.Borde = borde.gameObject;
                f.Filo = TrazoH10.Recta(f.Fondo.transform, "FiloRareza", TrazoH10.TextoTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 3f));
                RawImage icono = new GameObject("Icono", typeof(RectTransform)).AddComponent<RawImage>();
                icono.transform.SetParent(f.Fondo.transform, false);
                icono.texture = entrada.Icono;
                icono.material = config.MaterialAditivoUI;
                icono.raycastTarget = false;
                icono.enabled = entrada.Icono != null;
                float anchoIcono = anchoCelda - 28f;
                float altoIcono = anchoIcono * (entrada.Icono != null && entrada.Icono.width > 0 ? (float)entrada.Icono.height / entrada.Icono.width : 0.56f);
                KitUI.Colocar(icono.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(anchoIcono, Mathf.Min(altoIcono, 100f)));
                f.Nombre = TrazoH10.Etiqueta(f.Fondo.transform, "Nombre", entrada.Nombre, TrazoH10.TamanoDato, TrazoH10.Texto);
                TrazoH10.Punto(f.Nombre.rectTransform, new Vector2(0f, 0f), new Vector2(12f, 34f), new Vector2(anchoCelda - 24f, 28f));
                f.Nivel = TrazoH10.Etiqueta(f.Fondo.transform, "Nivel", string.Empty, TrazoH10.TamanoMicro, TrazoH10.TextoTenue);
                TrazoH10.Punto(f.Nivel.rectTransform, new Vector2(0f, 0f), new Vector2(12f, 8f), new Vector2(anchoCelda * 0.6f, 24f));
                f.Marcas = new Image[ReglaMejoraArma.NivelMaximo];
                for (int m = 0; m < f.Marcas.Length; m++)
                {
                    f.Marcas[m] = TrazoH10.Imagen(f.Fondo.transform, "Marca_" + m, TrazoH10.TextoTenue);
                    TrazoH10.Punto(f.Marcas[m].rectTransform, new Vector2(1f, 0f), new Vector2(-12f - (f.Marcas.Length - 1 - m) * 18f, 14f), new Vector2(14f, 6f));
                }
                filasArmas.Add(f);
            }
        }

        private void ConstruirHolograma(EstiloCyberfix estilo)
        {
            GameObject area = new GameObject("Holograma", typeof(RectTransform));
            area.transform.SetParent(canvas.transform, false);
            areaHolo = (RectTransform)area.transform;

            KitUI.Colocar(areaHolo, new Vector2(0.5f, 0.5f), new Vector2(0f, SubeHolo), new Vector2(AnchoHolo, AltoHolo));
            areaHolo.gameObject.AddComponent<CanvasGroup>();
            Image resplandor = KitUI.CrearImagen(areaHolo, "Resplandor", new Color(config.ColorHolograma.r, config.ColorHolograma.g, config.ColorHolograma.b, 0.015f));
            KitUI.Estirar(resplandor.rectTransform);
            Rejilla(areaHolo);
            imagenHolo = new GameObject("Imagen", typeof(RectTransform)).AddComponent<RawImage>();
            imagenHolo.transform.SetParent(areaHolo, false);
            KitUI.Estirar(imagenHolo.rectTransform);
            // La textura es de la proporción de antes (ancho / alto de la config): se recorta arriba y abajo para no deformar el arma.
            float proporcion = (AltoHolo / AnchoHolo) / ((float)config.AltoTextura / config.AnchoTextura);
            imagenHolo.uvRect = new Rect(0f, (1f - proporcion) * 0.5f, 1f, proporcion);
            vista.FijarFraccionAltoVisible(proporcion);
            imagenHolo.material = config.MaterialAditivoUI;
            imagenHolo.raycastTarget = true;
            destelloHolo = KitUI.CrearImagen(areaHolo, "Destello", Color.clear);
            KitUI.Estirar(destelloHolo.rectTransform);
            destelloHolo.material = config.MaterialAditivoUI;
            Marcas(areaHolo, TrazoH10.Turquesa);
            RectTransform reglaHolo = TrazoH10.Regla(areaHolo, "ReglaHolo", 76, 12f, 10f, TrazoH10.TurquesaTenue);
            TrazoH10.Punto(reglaHolo, new Vector2(0f, 0f), new Vector2(0f, 2f), reglaHolo.sizeDelta);
            lineaGuia = TrazoH10.Imagen(areaHolo, "LineaGuia", TrazoH10.Amarillo);
            puntoGuia = TrazoH10.Imagen(areaHolo, "PuntoGuia", TrazoH10.Amarillo, SpritesPunto());
            puntoGuia.rectTransform.anchorMin = Vector2.zero;
            puntoGuia.rectTransform.anchorMax = Vector2.zero;
            puntoGuia.rectTransform.sizeDelta = new Vector2(DiametroPunto, DiametroPunto);

            textoArma = Texto(areaHolo, "Arma", RolTexto.Titulo, TextAnchor.LowerLeft, new Vector2(0f, 1f), new Vector2(0f, SobreHolo),
                new Vector2(AnchoHolo * 0.6f, 80f), string.Empty, TrazoH10.Texto);
            textoArma.fontSize = TamanoNombreArma;
            textoNivelArma = Texto(areaHolo, "NivelArma", RolTexto.Encabezado, TextAnchor.LowerRight, new Vector2(1f, 1f), new Vector2(0f, SobreHolo),
                new Vector2(AnchoHolo * 0.4f, 70f), string.Empty, TrazoH10.Turquesa);
            textoPiezaElegida = Texto(areaHolo, "PiezaElegida", RolTexto.Ayuda, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-24f, -12f),
                new Vector2(AnchoHolo * 0.8f, 34f), string.Empty, TrazoH10.Amarillo);
            // T-887: la ayuda del ratón dentro del holograma, abajo (debajo ya va la tabla).
            textoAyudaRaton = Texto(areaHolo, "AyudaRaton", RolTexto.Ayuda, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, 16f),
                new Vector2(AnchoHolo, 30f), config.TextoAyudaRaton, TrazoH10.TextoTenue);
            textoAyudaRaton.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        private void ConstruirPiezas(EstiloCyberfix estilo)
        {
            panelPiezas = PanelH10(canvas.transform, "PanelPiezas");
            Image panel = panelPiezas;
            KitUI.Colocar(panel.rectTransform, new Vector2(1f, 0f), new Vector2(-Margen, AbajoLateral), new Vector2(AnchoLateral, AltoLateral));
            float ancho = AnchoLateral - 2f * RellenoLateral;
            TrazoH10.Seccion(panel.transform, "Encabezado", config.EncabezadoPiezas, new Vector2(RellenoLateral, -12f), ancho);
            float y = -56f;
            for (int i = 0; i < filasPiezas.Length; i++)
            {
                PiezaArma p = (PiezaArma)i;
                FilaPieza f = new FilaPieza();
                f.Fondo = PanelH10(panel.transform, "Pieza_" + p);
                f.Fondo.raycastTarget = true;
                KitUI.Colocar(f.Fondo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(ancho, AltoFilaPieza));
                Button boton = f.Fondo.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => ElegirPieza(p, true));
                HoverH10.Agregar(f.Fondo.gameObject, f.Fondo.transform.Find("Linea").GetComponent<Image>(), TrazoH10.TurquesaTenue, TrazoH10.Turquesa);
                Image bordePieza = TrazoH10.Imagen(f.Fondo.transform, "Elegida", TrazoH10.Turquesa, TrazoH10.Linea);
                TrazoH10.Estirar(bordePieza.rectTransform);
                f.Borde = bordePieza.gameObject;
                f.Numeral = Texto(f.Fondo.transform, "Numeral", RolTexto.Encabezado, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(4f, 0f),
                    new Vector2(56f, AltoFilaPieza), string.Empty, estilo.Texto);
                // T-895: el ícono H34 de la pieza real de la W01.
                f.Icono = KitUI.CrearImagen(f.Fondo.transform, "Icono", Color.white);
                f.Icono.sprite = Resources.Load<Sprite>(RutaIconosPieza + p);
                f.Icono.preserveAspect = true;
                f.Icono.raycastTarget = false;
                f.Icono.enabled = f.Icono.sprite != null;
                KitUI.Colocar(f.Icono.rectTransform, new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(LadoIconoPieza, LadoIconoPieza));
                f.Nombre = Texto(f.Fondo.transform, "Nombre", RolTexto.Boton, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(XTextoPieza, 18f),
                    new Vector2(ancho - XTextoPieza - 8f, 40f), string.Empty, estilo.Texto);
                f.Detalle = Texto(f.Fondo.transform, "Detalle", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(XTextoPieza, -20f),
                    new Vector2(ancho - XTextoPieza - 8f, 28f), string.Empty, estilo.Texto);
                f.Etiqueta = Texto(f.Fondo.transform, "Etiqueta", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-14f, -20f),
                    new Vector2(ancho * 0.6f, 40f), string.Empty, Acento);
                f.Etiqueta.fontSize = estilo.Tamano(RolTexto.Texto);
                filasPiezas[i] = f;
                y -= AltoFilaPieza + 8f;
            }

            TrazoH10.Seccion(panel.transform, "TituloTrasCompra", TituloTrasCompra, new Vector2(RellenoLateral, y - 8f), ancho);
            textoTrasCompra = TrazoH10.Etiqueta(panel.transform, "TrasCompra", string.Empty, TrazoH10.TamanoCifra, TrazoH10.Texto);
            TrazoH10.Punto(textoTrasCompra.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoLateral, y - 48f), new Vector2(ancho, 56f));

            TrazoH10.Seccion(panel.transform, "TituloProgreso", TituloProgreso, new Vector2(RellenoLateral, y - 120f), ancho);
            Text etiquetasProgreso = TrazoH10.Etiqueta(panel.transform, "EtiquetasProgreso", EtiquetasProgreso, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.UpperLeft);
            TrazoH10.Punto(etiquetasProgreso.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoLateral, y - 160f), new Vector2(ancho, 70f));
            textoProgreso = TrazoH10.Etiqueta(panel.transform, "Progreso", string.Empty, TrazoH10.TamanoDato, TrazoH10.Texto, TextAnchor.UpperRight);
            TrazoH10.Punto(textoProgreso.rectTransform, new Vector2(1f, 1f), new Vector2(-RellenoLateral, y - 160f), new Vector2(ancho, 70f));

            textoAviso = Texto(panel.transform, "Aviso", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, AltoBoton + 30f),
                new Vector2(ancho, 30f), string.Empty, estilo.Turquesa);
            textoAviso.rectTransform.pivot = new Vector2(0.5f, 0f);
            fondoBoton = TrazoH10.Panel(panel.transform, "BotonMejorar", TrazoH10.Amarillo, Color.clear);
            lineaBoton = fondoBoton.transform.Find("Linea").GetComponent<Image>();
            fondoBoton.raycastTarget = true;
            KitUI.Colocar(fondoBoton.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(ancho, AltoBoton));
            Button mejorar = fondoBoton.gameObject.AddComponent<Button>();
            mejorar.transition = Selectable.Transition.None;
            mejorar.onClick.AddListener(Comprar);
            textoBoton = KitUI.CrearTexto(fondoBoton.transform, "Texto", RolTexto.Boton, TextAnchor.MiddleCenter);
            textoBoton.fontSize = TrazoH10.TamanoFila;
            HoverH10.Agregar(fondoBoton.gameObject, null, Color.clear, Color.clear);
        }

        // T-887: la tabla ANTES → DESPUÉS va al centro, debajo del holograma (antes se apretaba bajo las piezas, a la derecha).
        private void ConstruirTabla(EstiloCyberfix estilo)
        {
            panelTabla = PanelH10(canvas.transform, "PanelEstadisticas");
            Image panel = panelTabla;
            KitUI.Colocar(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, AbajoLateral), new Vector2(AnchoHolo, AltoPanelTabla));
            float ancho = AnchoHolo - 60f;
            float y = -14f;
            Texto(panel.transform, "EncabezadoTabla", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(30f, y),
                new Vector2(ancho, 30f), config.EncabezadoEstadisticas, TrazoH10.Turquesa);
            y -= 38f;
            foreach (EstadisticaArma estadistica in ReglaMejoraArma.OrdenEstadisticas)
            {
                FilaTabla f = new FilaTabla();
                GameObject fila = new GameObject("Fila_" + estadistica, typeof(RectTransform));
                fila.transform.SetParent(panel.transform, false);
                RectTransform rf = (RectTransform)fila.transform;
                KitUI.Colocar(rf, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(ancho, AltoFilaTabla));
                f.Nombre = Texto(rf, "Nombre", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(220f, 32f), string.Empty, estilo.Texto);
                f.Antes = Texto(rf, "Antes", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(0f, 1f), new Vector2(ancho * 0.42f, 0f), new Vector2(110f, 32f), string.Empty, estilo.Texto);
                f.Flecha = Texto(rf, "Flecha", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(ancho * 0.42f + 116f, 0f), new Vector2(40f, 32f), "→", estilo.Desactivado);
                f.Despues = Texto(rf, "Despues", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(ancho * 0.42f + 162f, 0f), new Vector2(110f, 32f), string.Empty, estilo.Texto);
                f.Diferencia = Texto(rf, "Diferencia", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(1f, 1f), Vector2.zero, new Vector2(90f, 32f), string.Empty, estilo.Texto);
                Image fondoBarra = KitUI.CrearImagen(rf, "Barra", new Color(1f, 1f, 1f, 0.08f));
                f.Barra = fondoBarra.rectTransform;
                KitUI.Colocar(f.Barra, new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(ancho, 3f));
                f.BarraDespues = KitUI.CrearImagen(f.Barra, "Despues", Color.white);
                Barra(f.BarraDespues.rectTransform);
                f.BarraAntes = KitUI.CrearImagen(f.Barra, "Antes", Color.white);
                Barra(f.BarraAntes.rectTransform);
                filasTabla.Add(f);
                y -= AltoFilaTabla;
            }
        }

        private static void Barra(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ConstruirLetrero(EstiloCyberfix estilo)
        {
            canvasLetrero = KitUI.CrearCanvas(transform, "LetreroArmaMejorada", OrdenLetrero);
            grupoLetrero = canvasLetrero.gameObject.AddComponent<CanvasGroup>();
            grupoLetrero.blocksRaycasts = false;
            grupoLetrero.interactable = false;
            Image franja = KitUI.CrearImagen(canvasLetrero.transform, "Franja", new Color(0f, 0f, 0f, 0.6f));
            KitUI.Colocar(franja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(1920f, 200f));
            Image lineaArriba = KitUI.CrearImagen(franja.transform, "LineaArriba", Acento);
            KitUI.Colocar(lineaArriba.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920f, 3f));
            Image lineaAbajo = KitUI.CrearImagen(franja.transform, "LineaAbajo", estilo.Turquesa);
            KitUI.Colocar(lineaAbajo.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 3f));
            textoLetrero = Texto(franja.transform, "Titulo", RolTexto.Titulo, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 22f),
                new Vector2(1400f, 120f), config.TextoLetrero, TrazoH10.Amarillo);
            textoLetrero.fontSize = TrazoH10.TamanoTitulo * 2;
            textoLetreroArma = Texto(franja.transform, "Arma", RolTexto.Encabezado, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -62f),
                new Vector2(1400f, 50f), string.Empty, estilo.Turquesa);
            canvasLetrero.gameObject.SetActive(false);
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
        private static void Marcas(RectTransform area, Color color)
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
        private void Lineas(Transform padre)
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

        private static Sprite spritePunto;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarPunto() => spritePunto = null;

        private static Sprite SpritesPunto() => spritePunto != null ? spritePunto : spritePunto = MechaCyberpunk.UI.Inicio.SpritesInicio.Circulo;

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
