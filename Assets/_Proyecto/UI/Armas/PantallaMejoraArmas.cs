using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
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
        private const float Margen = 48f;
        private const float AltoCabecera = 150f;
        private const float AnchoLateral = 440f;
        private const float AltoLateral = 790f;
        private const float AbajoLateral = 110f;
        private const float AltoFilaArma = 150f;
        private const float AnchoIcono = 190f;
        private const float AltoFilaPieza = 84f;
        private const float AltoFilaEstadistica = 48f;
        private const float AltoBoton = 70f;
        private const float AnchoHolo = 900f;
        private const float AltoHolo = 562f;
        private const float GrosorMarca = 3f;
        private const float LargoMarca = 46f;
        private const float AlfaFilaElegida = 0.22f;
        private const float AlfaFilaNormal = 0.06f;
        private const float AlfaDestelloPantalla = 0.55f;
        private const float AlfaBarraAntes = 0.45f;
        private const int TamanoTituloGrande = 84;
        private const int TamanoNombreArma = 60;
        private static readonly string[] Romanos = { "", "I", "II", "III" };

        private sealed class FilaArma
        {
            public Image Fondo;
            public GameObject Borde;
            public Text Nombre;
            public Text Nivel;
            public Image[] Marcas;
        }

        private sealed class FilaPieza
        {
            public Image Fondo;
            public GameObject Borde;
            public Text Numeral;
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
        private readonly List<FilaArma> filasArmas = new List<FilaArma>();
        private readonly FilaPieza[] filasPiezas = new FilaPieza[ReglaMejoraArma.CantidadPiezas];
        private readonly List<FilaTabla> filasTabla = new List<FilaTabla>();
        private readonly List<FilaEstadistica> comparacion = new List<FilaEstadistica>();
        private readonly Dictionary<PiezaArma, EstadoPieza> estados = new Dictionary<PiezaArma, EstadoPieza>();

        private Canvas canvas;
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
        private float tiempoLetrero;
        private int cuadroAbierta = -1;
        private string ultimaMejora = string.Empty;
        private bool comproAlgo;
        private CursorLockMode cursorAntes;
        private bool cursorVisibleAntes;

        public bool Visible => visible;
        public int CantidadArmas => armas.Count;
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
                    ElegirPieza((PiezaArma)(((int)pieza + ReglaMejoraArma.CantidadPiezas - 1) % ReglaMejoraArma.CantidadPiezas), true);
                }
                else if (abajo.WasPressedThisFrame())
                {
                    ElegirPieza((PiezaArma)(((int)pieza + 1) % ReglaMejoraArma.CantidadPiezas), true);
                }
                if (confirmar.WasPressedThisFrame())
                {
                    Comprar();
                }
            }
            GirarConRaton();
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
            armas.Clear();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            foreach (EntradaArmaMenu entrada in config.Armas)
            {
                if (entrada != null && arsenal != null && arsenal.Tiene(entrada.Arma))
                {
                    armas.Add(entrada);
                }
            }
            ConstruirFilasArmas();
        }

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
            pieza = ReglaMejoraArma.Siguiente(NivelActual());
            animacionNumeros = 1f;
            textoAviso.gameObject.SetActive(false);
            if (conSonido)
            {
                Sonidos.Reproducir(config.SonidoMover);
            }
            Pintar();
        }

        /// <summary>Elige la pieza (también con clic): se separa del arma y la tabla muestra su mejora.</summary>
        public void ElegirPieza(PiezaArma nueva, bool conSonido)
        {
            pieza = nueva;
            animacionNumeros = 1f;
            textoAviso.gameObject.SetActive(false);
            if (conSonido)
            {
                Sonidos.Reproducir(config.SonidoMover);
            }
            Pintar();
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
            EstadoPieza estado = ReglaMejoraArma.Estado(pieza, nivel);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            DatosArma siguiente = DatosNivel(ReglaMejoraArma.NivelDe(pieza));
            if (!ReglaMejoraArma.HaySiguiente(nivel))
            {
                MostrarAviso(config.TextoAlMaximo, Color.white);
            }
            else if (estado == EstadoPieza.Instalada)
            {
                MostrarAviso(config.TextoInstalada, KitUI.Estilo.Turquesa);
            }
            else if (estado == EstadoPieza.Bloqueada)
            {
                Sonidos.Reproducir(config.SonidoCancelar);
                MostrarAviso(TextoBloqueada(nivel), KitUI.Estilo.Desactivado);
            }
            else if (siguiente == null || inventario == null || cartera == null || !inventario.PuedeAgregar(siguiente, 1))
            {
                MostrarAviso(config.TextoNoDisponible, KitUI.Estilo.Magenta);
            }
            else if (!cartera.IntentarPagar(ReglaMejoraArma.Precio(siguiente), MotivoDinero.Compra))
            {
                Sonidos.Reproducir(config.SonidoSinDinero);
                MostrarAviso(config.TextoNoAlcanza, KitUI.Estilo.Magenta);
            }
            else
            {
                inventario.Agregar(siguiente, 1);
                Sonidos.Reproducir(config.SonidoCompra);
                comproAlgo = true;
                ultimaMejora = ArmaElegida.Nombre + " " + Romanos[siguiente.Nivel];
                destello = 1f;
                animacionNumeros = 0f;
                vista.Destellar();
                MostrarAviso(string.Format(config.FormatoComprado, config.Pieza(pieza).Nombre, ArmaElegida.Nombre), config.ColorMejora);
            }
            Pintar();
        }

        private string TextoBloqueada(int nivel)
        {
            return string.Format(config.FormatoBloqueada, config.Pieza(ReglaMejoraArma.PiezaQueFalta(pieza, nivel)).Nombre);
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
            if (animacionNumeros < 1f)
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
            textoNivelArma.text = hay ? TextoNivel(nivel) : string.Empty;
            estados.Clear();
            for (int i = 0; i < ReglaMejoraArma.CantidadPiezas; i++)
            {
                estados[(PiezaArma)i] = hay ? ReglaMejoraArma.Estado((PiezaArma)i, nivel) : EstadoPieza.Bloqueada;
            }
            EntradaPiezaMenu datosPieza = config.Pieza(pieza);
            textoPiezaElegida.text = hay ? Romanos[ReglaMejoraArma.NivelDe(pieza)] + " · " + datosPieza.Nombre + " · " + datosPieza.Mejora : string.Empty;
            PintarPiezas(nivel);
            PintarTabla(nivel);
            PintarBoton(nivel, cartera);
            if (hay)
            {
                vista.ElegirPieza(pieza, estados);
            }
        }

        private string TextoNivel(int nivel) => nivel <= 0 ? config.TextoBase : string.Format(config.FormatoNivel, Romanos[Mathf.Min(nivel, ReglaMejoraArma.NivelMaximo)]);

        private void PintarFilaArma(int i)
        {
            FilaArma f = filasArmas[i];
            bool elegida = i == indiceArma;
            EstiloCyberfix estilo = KitUI.Estilo;
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            int nivel = arsenal != null ? arsenal.Nivel(armas[i].Arma) : 0;
            f.Fondo.color = elegida ? new Color(estilo.Naranja.r, estilo.Naranja.g, estilo.Naranja.b, AlfaFilaElegida)
                : new Color(1f, 1f, 1f, AlfaFilaNormal);
            f.Borde.SetActive(elegida);
            f.Nombre.color = elegida ? estilo.Naranja : estilo.Texto;
            f.Nivel.text = TextoNivel(nivel);
            for (int m = 0; m < f.Marcas.Length; m++)
            {
                f.Marcas[m].color = m < nivel ? estilo.Turquesa : new Color(1f, 1f, 1f, 0.15f);
            }
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
                Color colorFila = elegida ? estilo.Naranja : estilo.Texto;
                f.Fondo.color = elegida ? new Color(estilo.Naranja.r, estilo.Naranja.g, estilo.Naranja.b, AlfaFilaElegida + brillo * 0.2f)
                    : new Color(1f, 1f, 1f, AlfaFilaNormal);
                f.Borde.SetActive(elegida);
                f.Numeral.text = Romanos[ReglaMejoraArma.NivelDe(p)];
                f.Numeral.color = estado == EstadoPieza.Instalada ? estilo.Turquesa : estado == EstadoPieza.Bloqueada ? estilo.Desactivado : colorFila;
                f.Nombre.text = datos.Nombre;
                f.Nombre.color = estado == EstadoPieza.Bloqueada && !elegida ? estilo.Desactivado : colorFila;
                f.Detalle.text = datos.Mejora;
                f.Detalle.color = estado == EstadoPieza.Bloqueada ? estilo.Desactivado : new Color(estilo.Texto.r, estilo.Texto.g, estilo.Texto.b, 0.75f);
                switch (estado)
                {
                    case EstadoPieza.Instalada:
                        f.Etiqueta.text = config.TextoInstalada;
                        f.Etiqueta.color = estilo.Turquesa;
                        f.Etiqueta.fontSize = estilo.Tamano(RolTexto.Texto);
                        break;
                    case EstadoPieza.Disponible:
                        int precio = ReglaMejoraArma.Precio(DatosNivel(ReglaMejoraArma.NivelDe(p)));
                        f.Etiqueta.text = FormatoDinero.Texto(precio);
                        f.Etiqueta.color = cartera != null && cartera.PuedePagar(precio) ? estilo.Naranja : estilo.Magenta;
                        f.Etiqueta.fontSize = estilo.Tamano(RolTexto.Texto);
                        break;
                    default:
                        f.Etiqueta.text = armas.Count > 0 ? string.Format(config.FormatoBloqueada,
                            config.Pieza(ReglaMejoraArma.PiezaQueFalta(p, nivel)).Nombre) : string.Empty;
                        f.Etiqueta.color = estilo.Desactivado;
                        f.Etiqueta.fontSize = estilo.Tamano(RolTexto.Ayuda);
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
            DatosArma referencia = armas.Count > 0 ? DatosNivel(ReglaMejoraArma.NivelMaximo) : null;
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
                Color colorCambio = fila.Cambio > 0 ? config.ColorMejora : fila.Cambio < 0 ? config.ColorPeor : estilo.Desactivado;
                Color colorNombre = fila.Resaltada ? estilo.Naranja : estilo.Texto;
                if (bloqueada)
                {
                    colorCambio = estilo.Desactivado;
                    colorNombre = fila.Resaltada ? colorNombre : estilo.Desactivado;
                }
                f.Nombre.color = colorNombre;
                f.Antes.color = bloqueada ? estilo.Desactivado : new Color(estilo.Texto.r, estilo.Texto.g, estilo.Texto.b, 0.7f);
                f.Flecha.color = fila.Resaltada ? estilo.Naranja : estilo.Desactivado;
                f.Despues.color = fila.Cambio != 0 ? colorCambio : f.Antes.color;
                f.Diferencia.color = colorCambio;
                float fa = ReglaMejoraArma.Fraccion(fila.Estadistica, fila.Antes, r.Valor(fila.Estadistica));
                float fd = ReglaMejoraArma.Fraccion(fila.Estadistica, mostrado, r.Valor(fila.Estadistica));
                Llenar(f.BarraAntes, Mathf.Min(fa, fd));
                Llenar(f.BarraDespues, Mathf.Max(fa, fd));
                Color barra = fila.Resaltada ? estilo.Naranja : config.ColorHolograma;
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
            else if (!ReglaMejoraArma.HaySiguiente(nivel))
            {
                textoBoton.text = config.TextoAlMaximo;
                color = estilo.Turquesa;
            }
            else if (estado == EstadoPieza.Disponible)
            {
                int precio = ReglaMejoraArma.Precio(DatosNivel(ReglaMejoraArma.NivelDe(pieza)));
                textoBoton.text = string.Format(config.FormatoMejorar, FormatoDinero.Texto(precio));
                color = cartera != null && cartera.PuedePagar(precio) ? estilo.Naranja : estilo.Magenta;
            }
            else if (estado == EstadoPieza.Instalada)
            {
                textoBoton.text = config.TextoInstalada;
                color = estilo.Turquesa;
            }
            else
            {
                textoBoton.text = TextoBloqueada(nivel);
                color = estilo.Desactivado;
            }
            textoBoton.color = color;
            fondoBoton.color = new Color(color.r, color.g, color.b, estado == EstadoPieza.Disponible ? 0.3f : 0.1f);
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
            textoTitulo = Texto(canvas.transform, "Titulo", RolTexto.Titulo, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(Margen, -Margen * 0.5f), new Vector2(900f, 100f), config.Titulo, estilo.Naranja);
            textoTitulo.fontSize = TamanoTituloGrande;
            textoSubtitulo = Texto(canvas.transform, "Subtitulo", RolTexto.Encabezado, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(Margen + 4f, -Margen * 0.5f - 88f), new Vector2(900f, 50f), config.Subtitulo, estilo.Turquesa);
            textoSubtitulo.fontSize = estilo.Tamano(RolTexto.Texto);
            Image tiraCabecera = KitUI.CrearImagen(canvas.transform, "TiraCabecera", new Color(estilo.Naranja.r, estilo.Naranja.g, estilo.Naranja.b, 0.6f));
            KitUI.Colocar(tiraCabecera.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -AltoCabecera + 6f), new Vector2(1920f - 2f * Margen, 2f));
            Texto(canvas.transform, "EtiquetaDinero", RolTexto.Ayuda, TextAnchor.UpperRight, new Vector2(1f, 1f),
                new Vector2(-Margen, -Margen * 0.5f - 6f), new Vector2(400f, 30f), "TU DINERO", estilo.Desactivado);
            textoDinero = Texto(canvas.transform, "Dinero", RolTexto.Titulo, TextAnchor.UpperRight, new Vector2(1f, 1f),
                new Vector2(-Margen, -Margen * 0.5f + 0f), new Vector2(500f, 110f), string.Empty, estilo.Turquesa);
            textoDinero.fontSize = TamanoTituloGrande;

            ConstruirLista(estilo);
            ConstruirHolograma(estilo);
            ConstruirPiezas(estilo);

            textoAyuda = Texto(canvas.transform, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f),
                new Vector2(0f, 34f), new Vector2(1400f, 36f), config.TextoAyuda, estilo.Texto);
            Image tiraPie = KitUI.CrearImagen(canvas.transform, "TiraPie", new Color(estilo.Turquesa.r, estilo.Turquesa.g, estilo.Turquesa.b, 0.35f));
            KitUI.Colocar(tiraPie.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1920f - 2f * Margen, 2f));

            ConstruirLetrero(estilo);
            contenido.SetActive(false);
        }

        private void ConstruirLista(EstiloCyberfix estilo)
        {
            Image panel = KitUI.CrearPanelMarco(canvas.transform, "PanelArmas", 0.85f);
            KitUI.Colocar(panel.rectTransform, new Vector2(0f, 0f), new Vector2(Margen, AbajoLateral), new Vector2(AnchoLateral, AltoLateral));
            Texto(panel.transform, "Encabezado", RolTexto.Encabezado, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(28f, -14f),
                new Vector2(AnchoLateral - 56f, 56f), config.EncabezadoArmas, estilo.Naranja);
            GameObject lista = new GameObject("Lista", typeof(RectTransform));
            lista.transform.SetParent(panel.transform, false);
            listaArmas = (RectTransform)lista.transform;
            KitUI.Colocar(listaArmas, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(AnchoLateral - 40f, AltoLateral - 110f));
            textoSinArmas = Texto(panel.transform, "SinArmas", RolTexto.Texto, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(AnchoLateral - 40f, 80f), config.TextoSinArmas, estilo.Desactivado);
            textoSinArmas.gameObject.SetActive(false);
        }

        private void ConstruirFilasArmas()
        {
            foreach (Transform hijo in listaArmas)
            {
                Destroy(hijo.gameObject);
            }
            filasArmas.Clear();
            EstiloCyberfix estilo = KitUI.Estilo;
            float ancho = listaArmas.sizeDelta.x;
            for (int i = 0; i < armas.Count; i++)
            {
                int indice = i;
                EntradaArmaMenu entrada = armas[i];
                FilaArma f = new FilaArma();
                f.Fondo = KitUI.CrearImagen(listaArmas, "Arma_" + entrada.Arma, Color.clear);
                f.Fondo.raycastTarget = true;
                KitUI.Colocar(f.Fondo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -i * (AltoFilaArma + 12f)), new Vector2(ancho, AltoFilaArma));
                Button boton = f.Fondo.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => ElegirArma(indice, true));
                f.Borde = KitUI.CrearBorde(f.Fondo.rectTransform, estilo.BordeElegido, estilo.Naranja);
                RawImage icono = new GameObject("Icono", typeof(RectTransform)).AddComponent<RawImage>();
                icono.transform.SetParent(f.Fondo.transform, false);
                icono.texture = entrada.Icono;
                icono.material = config.MaterialAditivoUI;
                icono.raycastTarget = false;
                icono.enabled = entrada.Icono != null;
                float altoIcono = AnchoIcono * (entrada.Icono != null && entrada.Icono.width > 0 ? (float)entrada.Icono.height / entrada.Icono.width : 0.56f);
                KitUI.Colocar(icono.rectTransform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(AnchoIcono, Mathf.Min(altoIcono, AltoFilaArma - 12f)));
                float x = AnchoIcono + 20f;
                f.Nombre = Texto(f.Fondo.transform, "Nombre", RolTexto.Boton, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(x, 26f),
                    new Vector2(ancho - x - 8f, 44f), entrada.Nombre, estilo.Texto);
                f.Nivel = Texto(f.Fondo.transform, "Nivel", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(x, -12f),
                    new Vector2(ancho - x - 8f, 30f), string.Empty, estilo.Turquesa);
                f.Marcas = new Image[ReglaMejoraArma.NivelMaximo];
                for (int m = 0; m < f.Marcas.Length; m++)
                {
                    f.Marcas[m] = KitUI.CrearImagen(f.Fondo.transform, "Marca_" + m, Color.white);
                    KitUI.Colocar(f.Marcas[m].rectTransform, new Vector2(0f, 0.5f), new Vector2(x + m * 30f, -44f), new Vector2(24f, 8f));
                }
                filasArmas.Add(f);
            }
        }

        private void ConstruirHolograma(EstiloCyberfix estilo)
        {
            GameObject area = new GameObject("Holograma", typeof(RectTransform));
            area.transform.SetParent(canvas.transform, false);
            areaHolo = (RectTransform)area.transform;

            KitUI.Colocar(areaHolo, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(AnchoHolo, AltoHolo));
            Image resplandor = KitUI.CrearImagen(areaHolo, "Resplandor", new Color(config.ColorHolograma.r, config.ColorHolograma.g, config.ColorHolograma.b, 0.05f));
            KitUI.Estirar(resplandor.rectTransform);
            Rejilla(areaHolo);
            imagenHolo = new GameObject("Imagen", typeof(RectTransform)).AddComponent<RawImage>();
            imagenHolo.transform.SetParent(areaHolo, false);
            KitUI.Estirar(imagenHolo.rectTransform);
            imagenHolo.material = config.MaterialAditivoUI;
            imagenHolo.raycastTarget = true;
            destelloHolo = KitUI.CrearImagen(areaHolo, "Destello", Color.clear);
            KitUI.Estirar(destelloHolo.rectTransform);
            destelloHolo.material = config.MaterialAditivoUI;
            Marcas(areaHolo, estilo.Turquesa);

            textoArma = Texto(areaHolo, "Arma", RolTexto.Titulo, TextAnchor.LowerLeft, new Vector2(0f, 1f), new Vector2(24f, 84f),
                new Vector2(AnchoHolo * 0.6f, 80f), string.Empty, estilo.Texto);
            textoArma.fontSize = TamanoNombreArma;
            textoNivelArma = Texto(areaHolo, "NivelArma", RolTexto.Encabezado, TextAnchor.LowerRight, new Vector2(1f, 1f), new Vector2(-24f, 84f),
                new Vector2(AnchoHolo * 0.4f, 70f), string.Empty, estilo.Turquesa);
            textoPiezaElegida = Texto(areaHolo, "PiezaElegida", RolTexto.Encabezado, TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(0f, -14f),
                new Vector2(AnchoHolo, 56f), string.Empty, estilo.Naranja);
            textoPiezaElegida.rectTransform.pivot = new Vector2(0.5f, 1f);
            textoAyudaRaton = Texto(areaHolo, "AyudaRaton", RolTexto.Ayuda, TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(0f, -72f),
                new Vector2(AnchoHolo, 30f), config.TextoAyudaRaton, estilo.Desactivado);
            textoAyudaRaton.rectTransform.pivot = new Vector2(0.5f, 1f);
        }

        private void ConstruirPiezas(EstiloCyberfix estilo)
        {
            Image panel = KitUI.CrearPanelMarco(canvas.transform, "PanelPiezas", 0.85f);
            KitUI.Colocar(panel.rectTransform, new Vector2(1f, 0f), new Vector2(-Margen, AbajoLateral), new Vector2(AnchoLateral, AltoLateral));
            float ancho = AnchoLateral - 40f;
            Texto(panel.transform, "Encabezado", RolTexto.Encabezado, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(28f, -14f),
                new Vector2(ancho, 56f), config.EncabezadoPiezas, estilo.Naranja);
            float y = -80f;
            for (int i = 0; i < filasPiezas.Length; i++)
            {
                PiezaArma p = (PiezaArma)i;
                FilaPieza f = new FilaPieza();
                f.Fondo = KitUI.CrearImagen(panel.transform, "Pieza_" + p, Color.clear);
                f.Fondo.raycastTarget = true;
                KitUI.Colocar(f.Fondo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(ancho, AltoFilaPieza));
                Button boton = f.Fondo.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => ElegirPieza(p, true));
                f.Borde = KitUI.CrearBorde(f.Fondo.rectTransform, estilo.BordeElegido, estilo.Naranja);
                f.Numeral = Texto(f.Fondo.transform, "Numeral", RolTexto.Encabezado, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(4f, 0f),
                    new Vector2(64f, AltoFilaPieza), string.Empty, estilo.Texto);
                f.Nombre = Texto(f.Fondo.transform, "Nombre", RolTexto.Boton, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(76f, 18f),
                    new Vector2(ancho - 84f, 40f), string.Empty, estilo.Texto);
                f.Detalle = Texto(f.Fondo.transform, "Detalle", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(76f, -20f),
                    new Vector2(ancho - 84f, 28f), string.Empty, estilo.Texto);
                f.Etiqueta = Texto(f.Fondo.transform, "Etiqueta", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-14f, 18f),
                    new Vector2(ancho * 0.6f, 40f), string.Empty, estilo.Naranja);
                f.Etiqueta.fontSize = estilo.Tamano(RolTexto.Texto);
                filasPiezas[i] = f;
                y -= AltoFilaPieza + 8f;
            }

            y -= 8f;
            Texto(panel.transform, "EncabezadoTabla", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(28f, y),
                new Vector2(ancho, 30f), config.EncabezadoEstadisticas, estilo.Desactivado);
            y -= 34f;
            foreach (EstadisticaArma estadistica in ReglaMejoraArma.OrdenEstadisticas)
            {
                FilaTabla f = new FilaTabla();
                GameObject fila = new GameObject("Fila_" + estadistica, typeof(RectTransform));
                fila.transform.SetParent(panel.transform, false);
                RectTransform rf = (RectTransform)fila.transform;
                KitUI.Colocar(rf, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(ancho, AltoFilaEstadistica));
                f.Nombre = Texto(rf, "Nombre", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(8f, 0f), new Vector2(150f, 32f), string.Empty, estilo.Texto);
                f.Antes = Texto(rf, "Antes", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(0f, 1f), new Vector2(150f, 0f), new Vector2(82f, 32f), string.Empty, estilo.Texto);
                f.Flecha = Texto(rf, "Flecha", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(234f, 0f), new Vector2(30f, 32f), "→", estilo.Desactivado);
                f.Despues = Texto(rf, "Despues", RolTexto.Ayuda, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(266f, 0f), new Vector2(70f, 32f), string.Empty, estilo.Texto);
                f.Diferencia = Texto(rf, "Diferencia", RolTexto.Ayuda, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-8f, 0f), new Vector2(56f, 32f), string.Empty, estilo.Texto);
                Image fondoBarra = KitUI.CrearImagen(rf, "Barra", new Color(1f, 1f, 1f, 0.08f));
                f.Barra = fondoBarra.rectTransform;
                KitUI.Colocar(f.Barra, new Vector2(0f, 0f), new Vector2(8f, 6f), new Vector2(ancho - 16f, 5f));
                f.BarraDespues = KitUI.CrearImagen(f.Barra, "Despues", Color.white);
                Barra(f.BarraDespues.rectTransform);
                f.BarraAntes = KitUI.CrearImagen(f.Barra, "Antes", Color.white);
                Barra(f.BarraAntes.rectTransform);
                filasTabla.Add(f);
                y -= AltoFilaEstadistica;
            }

            textoAviso = Texto(panel.transform, "Aviso", RolTexto.Ayuda, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, AltoBoton + 30f),
                new Vector2(ancho, 30f), string.Empty, estilo.Turquesa);
            textoAviso.rectTransform.pivot = new Vector2(0.5f, 0f);
            fondoBoton = KitUI.CrearImagen(panel.transform, "BotonMejorar", Color.clear);
            fondoBoton.raycastTarget = true;
            KitUI.Colocar(fondoBoton.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(ancho, AltoBoton));
            Button mejorar = fondoBoton.gameObject.AddComponent<Button>();
            mejorar.transition = Selectable.Transition.None;
            mejorar.onClick.AddListener(Comprar);
            KitUI.CrearBorde(fondoBoton.rectTransform, estilo.BordeElegido * 0.67f, new Color(1f, 1f, 1f, 0.25f));
            textoBoton = KitUI.CrearTexto(fondoBoton.transform, "Texto", RolTexto.Boton, TextAnchor.MiddleCenter);
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
            Image lineaArriba = KitUI.CrearImagen(franja.transform, "LineaArriba", estilo.Naranja);
            KitUI.Colocar(lineaArriba.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920f, 3f));
            Image lineaAbajo = KitUI.CrearImagen(franja.transform, "LineaAbajo", estilo.Turquesa);
            KitUI.Colocar(lineaAbajo.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 3f));
            textoLetrero = Texto(franja.transform, "Titulo", RolTexto.Titulo, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 22f),
                new Vector2(1400f, 120f), config.TextoLetrero, estilo.Naranja);
            textoLetrero.fontSize = 110;
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

        private static Text Texto(Transform padre, string nombre, RolTexto rol, TextAnchor alineacion, Vector2 ancla, Vector2 posicion, Vector2 tamano,
            string contenidoTexto, Color color)
        {
            Text t = KitUI.CrearTexto(padre, nombre, rol, alineacion);
            KitUI.Colocar(t.rectTransform, ancla, posicion, tamano);
            t.text = contenidoTexto;
            t.color = color;
            return t;
        }
    }
}
