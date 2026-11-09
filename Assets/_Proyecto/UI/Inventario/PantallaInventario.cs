using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inventario
{
    /// <summary>
    /// El inventario (T-904), solo de consulta: la tecla I lo abre cuando no hay otra pantalla en <see cref="PilaPantallas"/> y I o Esc lo cierran.
    /// Muestra lo que el jugador trae de verdad (<see cref="FuenteInventario"/>): categorías con ícono a la izquierda, una cuadrícula con el filo de
    /// rareza de cada casilla al centro y el detalle de la casilla elegida a la derecha. El tiempo no se pausa, el cursor queda libre y el jugador a
    /// pie no se mueve mientras está abierto. ← → ↑ ↓ mueven la casilla, Q y E cambian de categoría; el ratón elige con clic.
    /// </summary>
    public class PantallaInventario : MonoBehaviour
    {
        public const int OrdenCanvas = 42;
        public const string Titulo = "INVENTARIO";
        public const string Subtitulo = "LO QUE TRAES";
        public const string TextoAyuda = "I O ESC CERRAR · ← → ↑ ↓ ELEGIR · Q E CATEGORÍA";
        public const string TextoVacio = "NO TRAES NADA EN ESTA CATEGORÍA";
        public const string TextoSinSeleccion = "ELIGE UNA CASILLA";
        public const string SonidoMover = "UI_MOVER";
        public const string SonidoAbrir = "UI_MAPA_ABRIR";
        public const string SonidoCerrar = "UI_MAPA_CERRAR";

        private const float AnchoCategorias = 520f;
        private const float AnchoDetalle = 520f;
        private const float RellenoPanel = 18f;
        private const float AltoCategoria = 64f;
        private const float SeparacionCategoria = 8f;
        private const float InicioCategorias = 62f;
        private const float LadoCasilla = 168f;
        private const float SeparacionCasilla = 12f;
        private const float LadoIconoCasilla = 72f;
        private const float LadoIconoCategoria = 40f;
        private const float LadoIconoDetalle = 96f;
        private const float AltoDato = 44f;
        private const int DatosMaximos = 6;
        private const float AlfaFilo = 0.9f;
        private const float AlfaSinIcono = 0.5f;

        [Tooltip("Tecla que abre el inventario.")]
        [SerializeField] private InputAction abrir = new InputAction("InventarioAbrir", InputActionType.Button, "<Keyboard>/i");
        [Tooltip("Tecla que lo cierra, además de la que lo abre.")]
        [SerializeField] private InputAction cerrar = new InputAction("InventarioCerrar", InputActionType.Button, "<Keyboard>/escape");
        [SerializeField] private InputAction izquierda = new InputAction("InventarioIzquierda", InputActionType.Button, "<Keyboard>/leftArrow");
        [SerializeField] private InputAction derecha = new InputAction("InventarioDerecha", InputActionType.Button, "<Keyboard>/rightArrow");
        [SerializeField] private InputAction arriba = new InputAction("InventarioArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [SerializeField] private InputAction abajo = new InputAction("InventarioAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [SerializeField] private InputAction categoriaAnterior = new InputAction("InventarioCategoriaAnterior", InputActionType.Button, "<Keyboard>/q");
        [SerializeField] private InputAction categoriaSiguiente = new InputAction("InventarioCategoriaSiguiente", InputActionType.Button, "<Keyboard>/e");

        private static readonly Dictionary<string, Texture2D> iconos = new Dictionary<string, Texture2D>();

        private sealed class Casilla
        {
            public RectTransform Caja;
            public Image Fondo;
            public Image Filo;
            public Image Seleccion;
            public RawImage Icono;
            public Text Inicial;
            public Text Nombre;
            public Text Cantidad;
        }

        private sealed class FilaCategoria
        {
            public Image Fondo;
            public Image Linea;
            public RawImage Icono;
            public Text Nombre;
            public Text Cuenta;
        }

        private readonly Casilla[] casillas = new Casilla[ReglaInventarioUI.Columnas * ReglaInventarioUI.FilasVisibles];
        private readonly FilaCategoria[] filasCategoria = new FilaCategoria[System.Enum.GetValues(typeof(CategoriaInventario)).Length];
        private readonly Text[] etiquetasDato = new Text[DatosMaximos];
        private readonly Text[] valoresDato = new Text[DatosMaximos];
        private List<EntradaInventario> todas = new List<EntradaInventario>();
        private List<EntradaInventario> visibles = new List<EntradaInventario>();
        private CategoriaInventario categoria = CategoriaInventario.Todo;
        private int indice;
        private int filaInicial;
        private int cuadroAbierto = -1;
        private bool construida;
        private bool abierta;
        private bool sucio;
        private Canvas canvas;
        private GameObject contenido;
        private RectTransform panelCategorias;
        private RectTransform panelCuadricula;
        private RectTransform panelDetalle;
        private RectTransform listaCasillas;
        private Text textoDinero;
        private Text textoVacio;
        private Text textoEncabezadoCuadricula;
        private RawImage detalleIcono;
        private Text detalleInicial;
        private Text detalleNombre;
        private Text detalleCategoria;
        private Text detalleDescripcion;
        private Text detalleCantidad;
        private ControladorAPie controlCongelado;
        private CursorLockMode bloqueoPrevio;
        private bool cursorPrevio;
        private ServicioInventario inventarioEscuchado;
        private ReservaBalas reservaEscuchada;

        public bool Abierta => abierta;
        public CategoriaInventario Categoria => categoria;
        public int IndiceElegido => indice;
        public IReadOnlyList<EntradaInventario> Visibles => visibles;
        public IReadOnlyList<EntradaInventario> Todas => todas;
        public EntradaInventario Elegida => indice >= 0 && indice < visibles.Count ? visibles[indice] : null;
        public string DetalleNombre => detalleNombre != null ? detalleNombre.text : string.Empty;
        public string Dinero => textoDinero != null ? textoDinero.text : string.Empty;
        public RectTransform PanelCuadricula => panelCuadricula;
        public RectTransform PanelDetalle => panelDetalle;
        public RectTransform PanelCategorias => panelCategorias;
        /// <summary>Las casillas dibujadas ahora (las que tienen una entrada).</summary>
        public int CasillasConEntrada { get; private set; }

        /// <summary>El filo de rareza de la casilla <paramref name="numero"/> (de las visibles), para las pruebas.</summary>
        public Color FiloDe(int numero) => casillas[numero].Filo.color;

        /// <summary>Si la casilla <paramref name="numero"/> tiene ícono dibujado.</summary>
        public bool TieneIcono(int numero) => casillas[numero].Icono.gameObject.activeSelf && casillas[numero].Icono.texture != null;

        /// <summary>Cuántos íconos de categoría salieron (Resources/Menus/Cat_*).</summary>
        public int IconosDeCategoria
        {
            get
            {
                int cuenta = 0;
                foreach (FilaCategoria f in filasCategoria)
                {
                    if (f.Icono.gameObject.activeSelf && f.Icono.texture != null)
                    {
                        cuenta++;
                    }
                }
                return cuenta;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => iconos.Clear();

        private void Awake()
        {
            Construir();
        }

        private void OnEnable()
        {
            abrir.Enable();
            cerrar.Enable();
            izquierda.Enable();
            derecha.Enable();
            arriba.Enable();
            abajo.Enable();
            categoriaAnterior.Enable();
            categoriaSiguiente.Enable();
        }

        private void OnDisable()
        {
            abrir.Disable();
            cerrar.Disable();
            izquierda.Disable();
            derecha.Disable();
            arriba.Disable();
            abajo.Disable();
            categoriaAnterior.Disable();
            categoriaSiguiente.Disable();
            Cerrar();
        }

        private void OnDestroy()
        {
            Desuscribir();
        }

        private void Update()
        {
            if (!abierta)
            {
                // La I no abre en pausa, encima de otra pantalla ni en el mismo cuadro en que otra se cerró.
                if (abrir.WasPressedThisFrame() && !Pausa.Activa && PilaPantallas.Vacia && !PilaPantallas.CambioEsteCuadro && MenuCyberfix.Abiertos == 0)
                {
                    Abrir();
                }
                return;
            }
            if (!PilaPantallas.EsArriba(this) || Time.frameCount == cuadroAbierto)
            {
                return;
            }
            if (abrir.WasPressedThisFrame() || cerrar.WasPressedThisFrame())
            {
                Cerrar();
                return;
            }
            if (categoriaAnterior.WasPressedThisFrame())
            {
                CambiarCategoria(ReglaInventarioUI.Siguiente(categoria, -1));
            }
            else if (categoriaSiguiente.WasPressedThisFrame())
            {
                CambiarCategoria(ReglaInventarioUI.Siguiente(categoria, 1));
            }
            int dx = (derecha.WasPressedThisFrame() ? 1 : 0) - (izquierda.WasPressedThisFrame() ? 1 : 0);
            int dy = (abajo.WasPressedThisFrame() ? 1 : 0) - (arriba.WasPressedThisFrame() ? 1 : 0);
            if (dx != 0 || dy != 0)
            {
                Elegir(ReglaInventarioUI.Mover(indice, dx, dy, visibles.Count));
            }
            if (sucio)
            {
                Reconstruir();
            }
        }

        public void Abrir()
        {
            if (abierta)
            {
                return;
            }
            Construir();
            abierta = true;
            cuadroAbierto = Time.frameCount;
            PilaPantallas.Abrir(this);
            IJugador jugador = Registro<IJugador>.Actual;
            controlCongelado = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<ControladorAPie>() : null;
            if (controlCongelado != null && controlCongelado.ControlActivo)
            {
                controlCongelado.ActivarControl(false);
            }
            else
            {
                controlCongelado = null;
            }
            bloqueoPrevio = Cursor.lockState;
            cursorPrevio = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            KitUI.AsegurarEventSystem();
            Suscribir();
            categoria = CategoriaInventario.Todo;
            indice = 0;
            filaInicial = 0;
            contenido.SetActive(true);
            Reconstruir();
            AnimH10.EntrarPanel(panelCategorias, new Vector2(-AnimH10.DesplazamientoEntrada, 0f));
            AnimH10.EntrarPanel(panelCuadricula, new Vector2(0f, -AnimH10.DesplazamientoEntrada), AnimH10.SegundosEscalon);
            AnimH10.EntrarPanel(panelDetalle, new Vector2(AnimH10.DesplazamientoEntrada, 0f), AnimH10.SegundosEscalon * 2f);
            Sonidos.Reproducir(SonidoAbrir);
        }

        public void Cerrar()
        {
            if (!abierta)
            {
                return;
            }
            abierta = false;
            Desuscribir();
            contenido.SetActive(false);
            PilaPantallas.Cerrar(this);
            Cursor.lockState = bloqueoPrevio;
            Cursor.visible = cursorPrevio;
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
            Sonidos.Reproducir(SonidoCerrar);
        }

        /// <summary>Elige una casilla de las visibles (con la rueda de la lista a la vista).</summary>
        public void Elegir(int nuevo)
        {
            if (visibles.Count == 0)
            {
                indice = 0;
                Pintar();
                return;
            }
            nuevo = Mathf.Clamp(nuevo, 0, visibles.Count - 1);
            if (nuevo != indice)
            {
                Sonidos.Reproducir(SonidoMover);
            }
            indice = nuevo;
            filaInicial = ReglaInventarioUI.FilaInicial(indice, filaInicial);
            Pintar();
        }

        public void CambiarCategoria(CategoriaInventario nueva)
        {
            if (nueva == categoria)
            {
                return;
            }
            categoria = nueva;
            Sonidos.Reproducir(SonidoMover);
            Reconstruir();
        }

        private void Suscribir()
        {
            Desuscribir();
            inventarioEscuchado = Registro<ServicioInventario>.Actual;
            reservaEscuchada = Registro<ReservaBalas>.Actual;
            if (inventarioEscuchado != null)
            {
                inventarioEscuchado.Cambio += MarcarSucio;
            }
            if (reservaEscuchada != null)
            {
                reservaEscuchada.Cambio += MarcarSucio;
            }
        }

        private void Desuscribir()
        {
            if (inventarioEscuchado != null)
            {
                inventarioEscuchado.Cambio -= MarcarSucio;
                inventarioEscuchado = null;
            }
            if (reservaEscuchada != null)
            {
                reservaEscuchada.Cambio -= MarcarSucio;
                reservaEscuchada = null;
            }
        }

        private void MarcarSucio() => sucio = true;

        // Vuelve a leer lo que el jugador trae y a filtrar por la categoría de ahora.
        private void Reconstruir()
        {
            sucio = false;
            todas = FuenteInventario.Leer();
            visibles = ReglaInventarioUI.Filtrar(todas, categoria);
            indice = Mathf.Clamp(indice, 0, Mathf.Max(0, visibles.Count - 1));
            filaInicial = ReglaInventarioUI.FilaInicial(indice, filaInicial);
            Pintar();
        }

        // ------------------------------------------------------------------ Pintar

        private void Pintar()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            textoDinero.text = Moneda.Texto(cartera != null ? cartera.Dinero : 0);
            for (int i = 0; i < filasCategoria.Length; i++)
            {
                CategoriaInventario cat = (CategoriaInventario)i;
                FilaCategoria f = filasCategoria[i];
                bool activa = cat == categoria;
                f.Fondo.color = activa ? TrazoH10.TurquesaFondo : TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0f);
                f.Linea.color = activa ? TrazoH10.Turquesa : TrazoH10.ConAlfa(TrazoH10.TurquesaTenue, 0.35f);
                f.Nombre.color = activa ? TrazoH10.Turquesa : TrazoH10.Texto;
                f.Icono.color = activa ? TrazoH10.Turquesa : TrazoH10.TextoTenue;
                f.Cuenta.text = ReglaInventarioUI.Contar(todas, cat).ToString();
                f.Cuenta.color = activa ? TrazoH10.Turquesa : TrazoH10.TextoTenue;
            }
            textoEncabezadoCuadricula.text = ReglaInventarioUI.Nombre(categoria);
            CasillasConEntrada = 0;
            for (int c = 0; c < casillas.Length; c++)
            {
                int posicion = filaInicial * ReglaInventarioUI.Columnas + c;
                Casilla casilla = casillas[c];
                bool hay = posicion < visibles.Count;
                casilla.Caja.gameObject.SetActive(hay);
                if (!hay)
                {
                    continue;
                }
                CasillasConEntrada++;
                PintarCasilla(casilla, visibles[posicion], posicion == indice);
            }
            textoVacio.gameObject.SetActive(visibles.Count == 0);
            PintarDetalle(Elegida);
        }

        private void PintarCasilla(Casilla casilla, EntradaInventario entrada, bool elegida)
        {
            casilla.Fondo.color = elegida ? TrazoH10.TurquesaFondo : TrazoH10.FondoPanel;
            casilla.Seleccion.gameObject.SetActive(elegida);
            casilla.Filo.color = TrazoH10.ConAlfa(TrazoH10.Rareza(entrada.Rareza), AlfaFilo);
            casilla.Nombre.text = entrada.Nombre;
            casilla.Cantidad.text = entrada.Cantidad > 1 || entrada.Categoria == CategoriaInventario.Municion ? "x" + entrada.Cantidad : string.Empty;
            PonerIcono(casilla.Icono, casilla.Inicial, entrada, TrazoH10.Texto);
        }

        private void PintarDetalle(EntradaInventario entrada)
        {
            for (int i = 0; i < DatosMaximos; i++)
            {
                etiquetasDato[i].gameObject.SetActive(false);
                valoresDato[i].gameObject.SetActive(false);
            }
            if (entrada == null)
            {
                detalleNombre.text = TextoSinSeleccion;
                detalleNombre.color = TrazoH10.TextoTenue;
                detalleCategoria.text = string.Empty;
                detalleDescripcion.text = string.Empty;
                detalleCantidad.text = string.Empty;
                detalleIcono.gameObject.SetActive(false);
                detalleInicial.gameObject.SetActive(false);
                return;
            }
            detalleNombre.text = entrada.Nombre;
            detalleNombre.color = TrazoH10.Texto;
            detalleCategoria.text = ReglaInventarioUI.Nombre(entrada.Categoria);
            detalleDescripcion.text = entrada.Descripcion;
            detalleCantidad.text = entrada.Maximo > 0 ? "x" + entrada.Cantidad + " / " + entrada.Maximo : entrada.Cantidad > 1 ? "x" + entrada.Cantidad : string.Empty;
            PonerIcono(detalleIcono, detalleInicial, entrada, TrazoH10.Turquesa);
            for (int i = 0; i < entrada.Datos.Count && i < DatosMaximos; i++)
            {
                etiquetasDato[i].text = entrada.Datos[i].Key;
                etiquetasDato[i].gameObject.SetActive(true);
                valoresDato[i].text = entrada.Datos[i].Value;
                valoresDato[i].gameObject.SetActive(true);
            }
        }

        // El ícono de la entrada; sin ícono se dibuja la inicial del nombre.
        private static void PonerIcono(RawImage icono, Text inicial, EntradaInventario entrada, Color color)
        {
            Texture2D textura = CargarIcono(entrada.Icono);
            icono.texture = textura;
            icono.color = color;
            icono.gameObject.SetActive(textura != null);
            inicial.text = entrada.Nombre.Length > 0 ? entrada.Nombre.Substring(0, 1) : string.Empty;
            inicial.gameObject.SetActive(textura == null);
        }

        private static Texture2D CargarIcono(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return null;
            }
            if (!iconos.TryGetValue(nombre, out Texture2D textura) || textura == null)
            {
                textura = Resources.Load<Texture2D>("Menus/" + nombre);
                iconos[nombre] = textura;
            }
            return textura;
        }

        // ------------------------------------------------------------------ Construir

        private void Construir()
        {
            if (construida)
            {
                return;
            }
            construida = true;
            canvas = KitUI.CrearCanvas(transform, "CanvasInventario", OrdenCanvas);
            contenido = canvas.gameObject;
            Image velo = KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");
            velo.color = TrazoH10.FondoPantalla;
            velo.raycastTarget = true;

            textoDinero = VestidoH10.Cabecera(canvas.transform, Titulo, Subtitulo, "TU DINERO", out _);
            VestidoH10.Pie(canvas.transform, TextoAyuda);

            float margen = TrazoH10.MargenPantalla;
            float separacion = TrazoH10.SeparacionModulo;
            float arriba = VestidoH10.AltoCabecera + separacion;
            float alto = KitUI.AltoReferencia - arriba - VestidoH10.AltoPie - separacion;
            float anchoCuadricula = KitUI.AnchoReferencia - 2f * margen - AnchoCategorias - AnchoDetalle - 2f * separacion;

            panelCategorias = TrazoH10.Panel(canvas.transform, "PanelCategorias", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(panelCategorias, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(margen, -arriba), new Vector2(AnchoCategorias, alto));
            ConstruirCategorias();

            panelCuadricula = TrazoH10.Panel(canvas.transform, "PanelCuadricula", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(panelCuadricula, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(margen + AnchoCategorias + separacion, -arriba), new Vector2(anchoCuadricula, alto));
            ConstruirCuadricula(anchoCuadricula);

            panelDetalle = TrazoH10.Panel(canvas.transform, "PanelDetalle", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(panelDetalle, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-margen, -arriba), new Vector2(AnchoDetalle, alto));
            ConstruirDetalle();

            contenido.SetActive(false);
        }

        private void ConstruirCategorias()
        {
            float ancho = AnchoCategorias - 2f * RellenoPanel;
            TrazoH10.Seccion(panelCategorias, "Seccion", "CATEGORÍAS", new Vector2(RellenoPanel, -12f), ancho);
            for (int i = 0; i < filasCategoria.Length; i++)
            {
                CategoriaInventario cat = (CategoriaInventario)i;
                FilaCategoria f = new FilaCategoria();
                RectTransform caja = TrazoH10.Nodo(panelCategorias, "Categoria_" + cat);
                TrazoH10.Colocar(caja, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -(InicioCategorias + i * (AltoCategoria + SeparacionCategoria))), new Vector2(ancho, AltoCategoria));
                f.Fondo = TrazoH10.Imagen(caja, "Fondo", TrazoH10.FondoPanel, TrazoH10.Relleno);
                f.Fondo.raycastTarget = true;
                TrazoH10.Estirar(f.Fondo.rectTransform);
                f.Linea = TrazoH10.Imagen(caja, "Linea", TrazoH10.TurquesaTenue, TrazoH10.Linea);
                TrazoH10.Estirar(f.Linea.rectTransform);
                HoverH10.Agregar(caja.gameObject, f.Linea, TrazoH10.ConAlfa(TrazoH10.TurquesaTenue, 0.35f), TrazoH10.Turquesa);
                string nombreIcono = ReglaInventarioUI.IconoDeCategoria(cat);
                f.Icono = CrearRawImage(caja, "Icono", new Vector2(RellenoPanel + LadoIconoCategoria * 0.5f, 0f), LadoIconoCategoria, new Vector2(0f, 0.5f));
                Texture2D textura = CargarIcono(nombreIcono);
                f.Icono.texture = textura;
                f.Icono.gameObject.SetActive(textura != null);
                f.Nombre = TrazoH10.Etiqueta(caja, "Nombre", ReglaInventarioUI.Nombre(cat), TrazoH10.TamanoFila, TrazoH10.Texto);
                TrazoH10.Estirar(f.Nombre.rectTransform);
                f.Nombre.rectTransform.offsetMin = new Vector2(RellenoPanel + LadoIconoCategoria + 14f, 0f);
                f.Nombre.rectTransform.offsetMax = new Vector2(-90f, 0f);
                f.Cuenta = TrazoH10.Etiqueta(caja, "Cuenta", "0", TrazoH10.TamanoFila, TrazoH10.TextoTenue, TextAnchor.MiddleRight);
                TrazoH10.Punto(f.Cuenta.rectTransform, new Vector2(1f, 0.5f), new Vector2(-RellenoPanel, 0f), new Vector2(80f, AltoCategoria));
                EventTrigger disparador = caja.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry clic = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                clic.callback.AddListener(_ => CambiarCategoria(cat));
                disparador.triggers.Add(clic);
                filasCategoria[i] = f;
            }
        }

        private void ConstruirCuadricula(float ancho)
        {
            float interior = ancho - 2f * RellenoPanel;
            textoEncabezadoCuadricula = TrazoH10.Seccion(panelCuadricula, "Seccion", "TODO", new Vector2(RellenoPanel, -12f), interior);
            listaCasillas = TrazoH10.Nodo(panelCuadricula, "Casillas");
            float anchoLista = ReglaInventarioUI.Columnas * LadoCasilla + (ReglaInventarioUI.Columnas - 1) * SeparacionCasilla;
            TrazoH10.Colocar(listaCasillas, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f),
                new Vector2(anchoLista, ReglaInventarioUI.FilasVisibles * (LadoCasilla + SeparacionCasilla)));
            for (int c = 0; c < casillas.Length; c++)
            {
                int numero = c;
                Casilla casilla = new Casilla();
                RectTransform caja = TrazoH10.Nodo(listaCasillas, "Casilla_" + c);
                int columna = c % ReglaInventarioUI.Columnas;
                int fila = c / ReglaInventarioUI.Columnas;
                TrazoH10.Colocar(caja, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(columna * (LadoCasilla + SeparacionCasilla), -fila * (LadoCasilla + SeparacionCasilla)), new Vector2(LadoCasilla, LadoCasilla));
                casilla.Caja = caja;
                casilla.Fondo = TrazoH10.Imagen(caja, "Fondo", TrazoH10.FondoPanel, TrazoH10.Relleno);
                casilla.Fondo.raycastTarget = true;
                TrazoH10.Estirar(casilla.Fondo.rectTransform);
                casilla.Filo = TrazoH10.Imagen(caja, "Filo", TrazoH10.TextoTenue, TrazoH10.Linea);
                TrazoH10.Estirar(casilla.Filo.rectTransform);
                casilla.Seleccion = TrazoH10.Imagen(caja, "Seleccion", TrazoH10.Turquesa, TrazoH10.Linea);
                TrazoH10.Estirar(casilla.Seleccion.rectTransform);
                casilla.Seleccion.gameObject.SetActive(false);
                Image brillo = TrazoH10.Imagen(caja, "Brillo", TrazoH10.ConAlfa(TrazoH10.Turquesa, 0f), TrazoH10.Linea);
                TrazoH10.Estirar(brillo.rectTransform);
                HoverH10.Agregar(caja.gameObject, brillo, TrazoH10.ConAlfa(TrazoH10.Turquesa, 0f), TrazoH10.Turquesa);
                casilla.Icono = CrearRawImage(caja, "Icono", new Vector2(0f, -22f - LadoIconoCasilla * 0.5f), LadoIconoCasilla, new Vector2(0.5f, 1f));
                casilla.Inicial = TrazoH10.Etiqueta(caja, "Inicial", string.Empty, TrazoH10.TamanoTitulo, TrazoH10.ConAlfa(TrazoH10.TextoTenue, AlfaSinIcono), TextAnchor.MiddleCenter);
                TrazoH10.Punto(casilla.Inicial.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(LadoIconoCasilla, LadoIconoCasilla));
                casilla.Nombre = TrazoH10.Etiqueta(caja, "Nombre", string.Empty, TrazoH10.TamanoMicro, TrazoH10.Texto, TextAnchor.LowerLeft);
                casilla.Nombre.horizontalOverflow = HorizontalWrapMode.Wrap;
                TrazoH10.Punto(casilla.Nombre.rectTransform, new Vector2(0f, 0f), new Vector2(10f, 8f), new Vector2(LadoCasilla - 20f, 48f));
                casilla.Cantidad = TrazoH10.Etiqueta(caja, "Cantidad", string.Empty, TrazoH10.TamanoDato, TrazoH10.Turquesa, TextAnchor.UpperRight);
                TrazoH10.Punto(casilla.Cantidad.rectTransform, new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(LadoCasilla * 0.6f, 30f));
                EventTrigger disparador = caja.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry clic = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                clic.callback.AddListener(_ => Elegir(filaInicial * ReglaInventarioUI.Columnas + numero));
                disparador.triggers.Add(clic);
                casillas[c] = casilla;
            }
            textoVacio = TrazoH10.Etiqueta(panelCuadricula, "Vacio", TextoVacio, TrazoH10.TamanoFila, TrazoH10.TextoTenue, TextAnchor.MiddleCenter);
            TrazoH10.Punto(textoVacio.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(interior, 60f));
            textoVacio.gameObject.SetActive(false);
        }

        private void ConstruirDetalle()
        {
            float ancho = AnchoDetalle - 2f * RellenoPanel;
            TrazoH10.Seccion(panelDetalle, "Seccion", "DETALLE", new Vector2(RellenoPanel, -12f), ancho);
            detalleIcono = CrearRawImage(panelDetalle, "Icono", new Vector2(RellenoPanel + LadoIconoDetalle * 0.5f, -62f - LadoIconoDetalle * 0.5f), LadoIconoDetalle, new Vector2(0f, 1f));
            detalleInicial = TrazoH10.Etiqueta(panelDetalle, "Inicial", string.Empty, TrazoH10.TamanoCifra, TrazoH10.ConAlfa(TrazoH10.TextoTenue, AlfaSinIcono), TextAnchor.MiddleCenter);
            TrazoH10.Punto(detalleInicial.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -62f), new Vector2(LadoIconoDetalle, LadoIconoDetalle));
            float xTexto = RellenoPanel + LadoIconoDetalle + 16f;
            float anchoTexto = ancho - LadoIconoDetalle - 16f;
            detalleNombre = TrazoH10.Etiqueta(panelDetalle, "Nombre", string.Empty, TrazoH10.TamanoSeccion, TrazoH10.Texto, TextAnchor.UpperLeft);
            detalleNombre.horizontalOverflow = HorizontalWrapMode.Wrap;
            TrazoH10.Punto(detalleNombre.rectTransform, new Vector2(0f, 1f), new Vector2(xTexto, -62f), new Vector2(anchoTexto, 80f));
            detalleCategoria = TrazoH10.Etiqueta(panelDetalle, "Categoria", string.Empty, TrazoH10.TamanoDato, TrazoH10.Turquesa);
            TrazoH10.Punto(detalleCategoria.rectTransform, new Vector2(0f, 1f), new Vector2(xTexto, -146f), new Vector2(anchoTexto * 0.5f, 30f));
            detalleCantidad = TrazoH10.Etiqueta(panelDetalle, "Cantidad", string.Empty, TrazoH10.TamanoFila, TrazoH10.Texto, TextAnchor.MiddleRight);
            TrazoH10.Punto(detalleCantidad.rectTransform, new Vector2(1f, 1f), new Vector2(-RellenoPanel, -144f), new Vector2(anchoTexto * 0.5f, 34f));
            detalleDescripcion = TrazoH10.Etiqueta(panelDetalle, "Descripcion", string.Empty, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.UpperLeft);
            detalleDescripcion.horizontalOverflow = HorizontalWrapMode.Wrap;
            TrazoH10.Punto(detalleDescripcion.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -184f), new Vector2(ancho, 110f));
            float yDatos = 312f;
            TrazoH10.Recta(panelDetalle, "ReglaDatos", TrazoH10.TurquesaTenue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -(yDatos - 8f)), new Vector2(-2f * RellenoPanel, TrazoH10.GrosorLinea));
            for (int i = 0; i < DatosMaximos; i++)
            {
                etiquetasDato[i] = TrazoH10.Etiqueta(panelDetalle, "DatoNombre" + i, string.Empty, TrazoH10.TamanoDato, TrazoH10.TextoTenue);
                TrazoH10.Punto(etiquetasDato[i].rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -(yDatos + i * AltoDato)), new Vector2(ancho * 0.5f, AltoDato));
                valoresDato[i] = TrazoH10.Etiqueta(panelDetalle, "DatoValor" + i, string.Empty, TrazoH10.TamanoFila, TrazoH10.Texto, TextAnchor.MiddleRight);
                TrazoH10.Punto(valoresDato[i].rectTransform, new Vector2(1f, 1f), new Vector2(-RellenoPanel, -(yDatos + i * AltoDato)), new Vector2(ancho * 0.5f, AltoDato));
            }
        }

        // Un RawImage de lado fijo, centrado en <paramref name="posicion"/> (el ancla fija de qué borde se mide).
        private static RawImage CrearRawImage(Transform padre, string nombre, Vector2 posicion, float lado, Vector2 ancla)
        {
            RectTransform rect = TrazoH10.Nodo(padre, nombre);
            RawImage imagen = rect.gameObject.AddComponent<RawImage>();
            imagen.raycastTarget = false;
            rect.anchorMin = ancla;
            rect.anchorMax = ancla;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = Vector2.one * lado;
            return imagen;
        }
    }
}
