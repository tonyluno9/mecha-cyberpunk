using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>Pestañas de la pantalla de tienda.</summary>
    public enum PestanaTienda
    {
        Comprar,
        Vender
    }

    /// <summary>
    /// Pantalla de tienda del mercado negro (T-146): Canvas Overlay con el dinero, las pestañas COMPRAR y VENDER (← →), la lista de
    /// objetos (↑ ↓), Enter para comprar y Esc para salir. Compra con <see cref="ITienda.Comprar"/> y mete el objeto al
    /// <see cref="ServicioInventario"/>. VENDER (T-147) lista lo que el mostrador compra (<see cref="ReglaVenta"/>) y Enter vende uno; sin tipos que comprar no aparece. Quien la abre la cierra con <see cref="Ocultar"/>.
    /// </summary>
    public class PantallaTienda : MonoBehaviour
    {
        // T-903: la composición H10 (cabecera, pestañas, lista a la izquierda y detalle a la derecha, barra de teclas).
        private const float AnchoLista = 1000f;
        private const float AltoPestana = 48f;
        private const float AnchoPestana = 220f;
        private const float AltoFila = 52f;
        private const float SeparacionFila = 4f;
        private const float RellenoPanel = 18f;
        private const float RellenoFila = 16f;
        private const float AnchoPrecio = 260f;
        private const float AnchoLleva = 190f;
        private const float AltoDato = 44f;
        private const int DatosDelDetalle = 4;
        private float pasoFila = AltoFila + SeparacionFila;
        private float altoFilaActual = AltoFila;
        private const float AlfaLineaFila = 0.35f;
        private const string TextoDetalle = "DETALLE";
        private const string TextoPrecio = "PRECIO";
        private const string TextoPaga = "TE PAGAN";
        private const string TextoLleva = "LLEVAS";
        private const string TextoTienes = "TIENES";
        private const string TextoQuedaria = "TE QUEDARÍA";
        private const string TextoBalas = "BALAS POR PAQUETE";

        [SerializeField] private ConfigPantallaTienda config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoDinero;
        [SerializeField] private Text textoPestanaComprar;
        [SerializeField] private Text textoPestanaVender;
        [SerializeField] private RectTransform listaFilas;
        [Tooltip("Texto que se copia por cada fila de la lista (debe estar apagado).")]
        [SerializeField] private Text plantillaFila;
        [SerializeField] private Text textoAviso;
        [SerializeField] private Text textoAyuda;
        [Tooltip("Línea del favor de descuento (T-187; opcional).")]
        [SerializeField] private Text textoFavor;
        [SerializeField] private InputAction arriba = new InputAction("TiendaArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [SerializeField] private InputAction abajo = new InputAction("TiendaAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [SerializeField] private InputAction izquierda = new InputAction("TiendaIzquierda", InputActionType.Button, "<Keyboard>/leftArrow");
        [SerializeField] private InputAction derecha = new InputAction("TiendaDerecha", InputActionType.Button, "<Keyboard>/rightArrow");
        [SerializeField] private InputAction confirmar = new InputAction("TiendaEnter", InputActionType.Button, "<Keyboard>/enter");
        [SerializeField] private InputAction salir = new InputAction("TiendaEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly List<Text> textosFilas = new List<Text>();
        private readonly List<DatosObjeto> todos = new List<DatosObjeto>();
        private readonly List<DatosObjeto> objetos = new List<DatosObjeto>();
        private readonly List<DatosObjeto> vendibles = new List<DatosObjeto>();
        private readonly List<TipoObjeto> compra = new List<TipoObjeto>();
        private ITienda tienda;
        private Action alSalir;
        private PestanaTienda pestana;
        private int indice;
        private bool visible;
        private bool aceptaFavorDescuento;
        private readonly List<string> cadenasFilas = new List<string>();
        private readonly List<GameObject> contenedoresFilas = new List<GameObject>();
        private readonly Text[] etiquetasDato = new Text[DatosDelDetalle];
        private readonly Text[] valoresDato = new Text[DatosDelDetalle];
        private bool vestida;
        private RectTransform panelLista;
        private RectTransform panelDetalle;
        private Text detalleNombre;
        private Text detalleTipo;
        private Text detalleDescripcion;
        private Image lineaPestanaComprar;
        private Image lineaPestanaVender;

        public bool Visible => visible;
        public PestanaTienda Pestana => pestana;
        public int IndiceElegido => indice;
        public int CantidadFilas => textosFilas.Count;
        public RectTransform PanelLista => panelLista;
        public RectTransform PanelDetalle => panelDetalle;
        /// <summary>El texto del detalle (nombre, tipo, descripción) y sus datos, para las pruebas.</summary>
        public string DetalleNombre => detalleNombre != null ? detalleNombre.text : string.Empty;
        /// <summary>El color del precio del detalle: amarillo si alcanza, coral si no.</summary>
        public Color ColorDetallePrecio => valoresDato[0].color;
        public string DetalleDescripcion => detalleDescripcion != null ? detalleDescripcion.text : string.Empty;
        public string DetalleDato(int indiceDato) => valoresDato[indiceDato] != null && valoresDato[indiceDato].gameObject.activeSelf ? valoresDato[indiceDato].text : string.Empty;
        /// <summary>True si el mostrador compra algo (la pestaña VENDER existe).</summary>
        public bool PuedeVender => compra.Count > 0;
        public string Titulo => textoTitulo != null ? textoTitulo.text : string.Empty;
        public string Dinero => textoDinero != null ? textoDinero.text : string.Empty;
        public string TextoPestanaComprar => textoPestanaComprar != null ? textoPestanaComprar.text : string.Empty;
        public string TextoPestanaVender => textoPestanaVender != null ? textoPestanaVender.text : string.Empty;
        public string Aviso => textoAviso != null && textoAviso.gameObject.activeSelf ? textoAviso.text : string.Empty;
        /// <summary>La línea del favor de descuento; vacía si no se ve.</summary>
        public string Favor => textoFavor != null && textoFavor.gameObject.activeSelf ? textoFavor.text : string.Empty;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;
        public Canvas Canvas => GetComponent<Canvas>();

        /// <summary>Texto de una fila de la lista.</summary>
        public string TextoFila(int fila) => cadenasFilas[fila];

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || textoDinero == null
                || textoPestanaComprar == null || textoPestanaVender == null || listaFilas == null || plantillaFila == null
                || textoAviso == null || textoAyuda == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaTienda de " + name, this);
                enabled = false;
                return;
            }
            plantillaFila.gameObject.SetActive(false);
            contenido.SetActive(false);
        }

        private void OnDisable()
        {
            DesactivarEntrada();
            visible = false;
            PilaPantallas.Cerrar(this);
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        /// <summary>Abre la pantalla en COMPRAR con la lista de objetos que la tienda vende. <paramref name="alSalir"/> avisa Esc.</summary>
        public void Mostrar(ITienda tienda, IReadOnlyList<DatosObjeto> objetosEnVenta, Action alSalir,
            IReadOnlyList<TipoObjeto> tiposQueCompra = null, bool aceptaFavorDescuento = false)
        {
            this.aceptaFavorDescuento = aceptaFavorDescuento;
            compra.Clear();
            if (tiposQueCompra != null)
            {
                compra.AddRange(tiposQueCompra);
            }
            this.tienda = tienda;
            this.alSalir = alSalir;
            objetos.Clear();
            todos.Clear();
            todos.AddRange(objetosEnVenta);
            Refiltrar();
            pestana = PestanaTienda.Comprar;
            indice = 0;

            VestirH10();
            textoTitulo.text = string.Format(config.FormatoTitulo, tienda.Nombre.ToUpperInvariant());
            textoAviso.text = string.Empty;
            textoAviso.gameObject.SetActive(false);
            Pintar();
            visible = true;
            contenido.SetActive(true);
            AnimH10.EntrarPanel(panelLista, new Vector2(-AnimH10.DesplazamientoEntrada, 0f));
            AnimH10.EntrarPanel(panelDetalle, new Vector2(AnimH10.DesplazamientoEntrada, 0f), AnimH10.SegundosEscalon);
            PilaPantallas.Abrir(this);
            arriba.Enable();
            abajo.Enable();
            izquierda.Enable();
            derecha.Enable();
            confirmar.Enable();
            salir.Enable();
        }

        public void Ocultar()
        {
            visible = false;
            PilaPantallas.Cerrar(this);
            DesactivarEntrada();
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        private void DesactivarEntrada()
        {
            arriba.Disable();
            abajo.Disable();
            izquierda.Disable();
            derecha.Disable();
            confirmar.Disable();
            salir.Disable();
        }

        private void Update()
        {
            // T-410: si otra pantalla está encima, esa atiende las teclas.
            if (!visible || !PilaPantallas.EsArriba(this))
            {
                return;
            }
            if (salir.WasPressedThisFrame())
            {
                Sonidos.Reproducir("UI_CANCELAR");
                alSalir?.Invoke();
                return;
            }
            bool cambio = false;
            if (izquierda.WasPressedThisFrame() && pestana != PestanaTienda.Comprar)
            {
                CambiarPestana(PestanaTienda.Comprar);
                cambio = true;
            }
            else if (derecha.WasPressedThisFrame() && pestana != PestanaTienda.Vender && PuedeVender)
            {
                CambiarPestana(PestanaTienda.Vender);
                cambio = true;
            }
            int cantidad = pestana == PestanaTienda.Comprar ? objetos.Count : vendibles.Count;
            if (cantidad > 0)
            {
                if (arriba.WasPressedThisFrame())
                {
                    indice = (indice + cantidad - 1) % cantidad;
                    cambio = true;
                }
                if (abajo.WasPressedThisFrame())
                {
                    indice = (indice + 1) % cantidad;
                    cambio = true;
                }
            }
            if (cambio)
            {
                textoAviso.gameObject.SetActive(false);
                Sonidos.Reproducir("UI_MOVER");
            }
            if (confirmar.WasPressedThisFrame())
            {
                Sonidos.Reproducir("UI_ACEPTAR");
                if (pestana == PestanaTienda.Vender)
                {
                    Vender();
                }
                else
                {
                    Comprar();
                }
                return;
            }
            if (cambio)
            {
                Pintar();
            }
        }

        // T-403: de cada arma solo se ofrece la mejora siguiente (el nivel n pide tener el n-1).
        private void Refiltrar()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            objetos.Clear();
            foreach (DatosObjeto objeto in todos)
            {
                if (objeto is DatosArma arma && arsenal != null && !arsenal.EsSiguiente(arma))
                {
                    continue;
                }
                // T-720: las balas de un arma solo se ofrecen a quien ya la tiene.
                if (objeto is DatosMunicionArma balas && (arsenal == null || !arsenal.Tiene(balas.Arma)))
                {
                    continue;
                }
                objetos.Add(objeto);
            }
            indice = Mathf.Clamp(indice, 0, Mathf.Max(0, objetos.Count - 1));
        }

        private void CambiarPestana(PestanaTienda nueva)
        {
            pestana = nueva;
            indice = 0;
        }

        private void Comprar()
        {
            if (pestana != PestanaTienda.Comprar || objetos.Count == 0)
            {
                return;
            }
            DatosObjeto objeto = objetos[indice];
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            if (inventario == null || cartera == null)
            {
                MostrarAviso(config.TextoNoDisponible);
            }
            else if (objeto is DatosMunicionArma && Registro<ReservaBalas>.Actual == null)
            {
                MostrarAviso(config.TextoNoDisponible);
            }
            else if (objeto is DatosMunicionArma pide ? Registro<ReservaBalas>.Actual.Espacio(pide.Arma) <= 0 : !inventario.PuedeAgregar(objeto, 1))
            {
                MostrarAviso(config.TextoMaximo);
            }
            else
            {
                float rebaja = RebajaPara(objeto.Oferta);
                ResultadoCompra resultado = rebaja > 0f && tienda is TiendaBasica basica
                    ? basica.Comprar(objeto.Oferta, cartera, rebaja)
                    : tienda.Comprar(objeto.Oferta, cartera);
                switch (resultado)
                {
                    case ResultadoCompra.Comprado:
                        if (rebaja > 0f)
                        {
                            // El favor se gasta solo si se cobró.
                            Registro<ServicioFavores>.Actual.Gastar(TipoFavor.Descuento);
                        }
                        if (objeto is DatosMunicionArma compradas)
                        {
                            // T-720: las balas van a la reserva del arma, no al inventario.
                            Registro<ReservaBalas>.Actual.Agregar(compradas.Arma, compradas.BalasPorPaquete);
                        }
                        else
                        {
                            inventario.Agregar(objeto, 1);
                        }
                        Sonidos.Reproducir("ECO_COMPRA");
                        Refiltrar();
                        MostrarAviso(string.Format(config.FormatoComprado, objeto.Nombre.ToUpperInvariant()));
                        break;
                    case ResultadoCompra.SinDinero:
                        Sonidos.Reproducir("ECO_SIN_DINERO");
                        MostrarAviso(config.TextoNoAlcanza);
                        break;
                    default:
                        MostrarAviso(config.TextoNoDisponible);
                        break;
                }
            }
            Pintar();
        }

        private void Vender()
        {
            if (vendibles.Count == 0)
            {
                return;
            }
            DatosObjeto objeto = vendibles[Mathf.Clamp(indice, 0, vendibles.Count - 1)];
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            int pago = ReglaVenta.Pago(objeto, compra);
            if (inventario == null || cartera == null || pago <= 0 || !inventario.Quitar(objeto, 1))
            {
                MostrarAviso(config.TextoNoDisponible);
            }
            else
            {
                cartera.Abonar(pago, MotivoDinero.Venta);
                MostrarAviso(string.Format(config.FormatoVendido, objeto.Nombre.ToUpperInvariant(), FormatoDinero.Texto(pago)));
            }
            Pintar();
        }

        // La rebaja que aplica a esta oferta: la del favor de descuento si el mostrador la acepta, hay favor y rebaja de verdad.
        private float RebajaPara(DatosOferta oferta)
        {
            float rebaja = RebajaDelFavor();
            return rebaja > 0f && TiendaBasica.PrecioCon(oferta, rebaja) < oferta.Precio ? rebaja : 0f;
        }

        private float RebajaDelFavor()
        {
            if (!aceptaFavorDescuento)
            {
                return 0f;
            }
            ServicioFavores favores = Registro<ServicioFavores>.Actual;
            if (favores == null || favores.Cantidad(TipoFavor.Descuento) <= 0)
            {
                return 0f;
            }
            DatosFavor datosFavor = favores.Datos(TipoFavor.Descuento);
            return datosFavor != null ? datosFavor.Valor : 0f;
        }

        private void MostrarAviso(string texto)
        {
            textoAviso.text = texto;
            // Coral solo en lo que no se pudo hacer; lo hecho va en turquesa.
            bool problema = texto == config.TextoNoAlcanza || texto == config.TextoMaximo || texto == config.TextoNoDisponible;
            textoAviso.color = problema ? TrazoH10.Coral : TrazoH10.Turquesa;
            textoAviso.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ H10

        // Viste la pantalla una sola vez, sin tocar el prefab: velo, cabecera, pestañas, panel de lista, panel de detalle y barra de teclas.
        private void VestirH10()
        {
            if (vestida)
            {
                return;
            }
            vestida = true;
            Transform raiz = contenido.transform;
            fondo.sprite = TrazoH10.Blanco;
            fondo.type = Image.Type.Simple;
            fondo.color = TrazoH10.FondoPantalla;
            foreach (Text texto in new[] { textoTitulo, textoDinero, textoAyuda, textoPestanaComprar, textoPestanaVender, textoAviso, textoFavor, plantillaFila })
            {
                QuitarSombras(texto);
            }
            textoTitulo.gameObject.SetActive(false);
            textoDinero.gameObject.SetActive(false);
            textoAyuda.gameObject.SetActive(false);
            textoDinero = VestidoH10.Cabecera(raiz, string.Empty, null, string.Empty, out textoTitulo);
            textoAyuda = VestidoH10.Pie(raiz, string.Empty);
            raiz.Find("EtiquetaDinero").gameObject.SetActive(false);

            float margen = TrazoH10.MargenPantalla;
            float separacion = TrazoH10.SeparacionModulo;
            float yPestanas = VestidoH10.AltoCabecera + separacion;
            float yPaneles = yPestanas + AltoPestana + separacion;
            float altoPaneles = KitUI.AltoReferencia - yPaneles - VestidoH10.AltoPie - separacion;
            lineaPestanaComprar = CrearPestana(textoPestanaComprar, new Vector2(margen, -yPestanas));
            lineaPestanaVender = CrearPestana(textoPestanaVender, new Vector2(margen + AnchoPestana + separacion, -yPestanas));

            // La lista, a la izquierda, sobre el marco de la tienda (el prefab ya lo trae).
            Transform marco = raiz.Find("MarcoTienda");
            Image imagenLista = marco != null ? marco.GetComponent<Image>() : null;
            if (imagenLista == null)
            {
                imagenLista = TrazoH10.Panel(raiz, "MarcoTienda", TrazoH10.TurquesaTenue);
            }
            VestidoH10.Panel(imagenLista);
            panelLista = imagenLista.rectTransform;
            TrazoH10.Colocar(panelLista, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(margen, -yPaneles), new Vector2(AnchoLista, altoPaneles));
            VerticalLayoutGroup grupo = listaFilas.GetComponent<VerticalLayoutGroup>();
            if (grupo != null)
            {
                grupo.enabled = false;
            }
            ContentSizeFitter ajuste = listaFilas.GetComponent<ContentSizeFitter>();
            if (ajuste != null)
            {
                ajuste.enabled = false;
            }
            listaFilas.SetParent(panelLista, false);
            TrazoH10.Colocar(listaFilas, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -RellenoPanel), new Vector2(-2f * RellenoPanel, altoPaneles - 2f * RellenoPanel));

            // El detalle, a la derecha.
            float anchoDetalle = KitUI.AnchoReferencia - 2f * margen - AnchoLista - separacion;
            panelDetalle = TrazoH10.Panel(raiz, "PanelDetalle", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(panelDetalle, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-margen, -yPaneles), new Vector2(anchoDetalle, altoPaneles));
            float ancho = anchoDetalle - 2f * RellenoPanel;
            TrazoH10.Seccion(panelDetalle, "Seccion", TextoDetalle, new Vector2(RellenoPanel, -12f), ancho);
            detalleNombre = TrazoH10.Etiqueta(panelDetalle, "Nombre", string.Empty, TrazoH10.TamanoCifra, TrazoH10.Texto);
            TrazoH10.Punto(detalleNombre.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -56f), new Vector2(ancho, 56f));
            detalleTipo = TrazoH10.Etiqueta(panelDetalle, "Tipo", string.Empty, TrazoH10.TamanoDato, TrazoH10.Turquesa);
            TrazoH10.Punto(detalleTipo.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -114f), new Vector2(ancho, 30f));
            detalleDescripcion = TrazoH10.Etiqueta(panelDetalle, "Descripcion", string.Empty, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.UpperLeft);
            detalleDescripcion.horizontalOverflow = HorizontalWrapMode.Wrap;
            TrazoH10.Punto(detalleDescripcion.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -152f), new Vector2(ancho, 80f));
            float yDatos = 250f;
            for (int i = 0; i < DatosDelDetalle; i++)
            {
                etiquetasDato[i] = TrazoH10.Etiqueta(panelDetalle, "DatoNombre" + i, string.Empty, TrazoH10.TamanoDato, TrazoH10.TextoTenue);
                TrazoH10.Punto(etiquetasDato[i].rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -(yDatos + i * AltoDato)), new Vector2(ancho * 0.6f, AltoDato));
                valoresDato[i] = TrazoH10.Etiqueta(panelDetalle, "DatoValor" + i, string.Empty, TrazoH10.TamanoFila, TrazoH10.Texto, TextAnchor.MiddleRight);
                TrazoH10.Punto(valoresDato[i].rectTransform, new Vector2(1f, 1f), new Vector2(-RellenoPanel, -(yDatos + i * AltoDato)), new Vector2(ancho * 0.6f, AltoDato));
            }
            TrazoH10.Recta(panelDetalle, "ReglaDatos", TrazoH10.TurquesaTenue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -(yDatos - 8f)), new Vector2(-2f * RellenoPanel, TrazoH10.GrosorLinea));

            // El favor y el aviso viven abajo del detalle.
            textoFavor = ReubicarAbajo(textoFavor, ancho, 100f, TrazoH10.Amarillo);
            textoAviso = ReubicarAbajo(textoAviso, ancho, 48f, TrazoH10.Turquesa);
            VestidoH10.Fuente(raiz);
        }

        private static void QuitarSombras(Text texto)
        {
            if (texto == null)
            {
                return;
            }
            foreach (Shadow sombra in texto.GetComponents<Shadow>())
            {
                Destroy(sombra);
            }
        }

        // Una pestaña de la barra: el mismo recuadro con corte del banco; la elegida va en turquesa y las demás tenues.
        private Image CrearPestana(Text texto, Vector2 posicion)
        {
            texto.rectTransform.SetParent(contenido.transform, false);
            TrazoH10.Colocar(texto.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), posicion, new Vector2(AnchoPestana, AltoPestana));
            texto.alignment = TextAnchor.MiddleCenter;
            texto.fontSize = TrazoH10.TamanoFila;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            Image linea = TrazoH10.Imagen(texto.transform, "Linea", TrazoH10.TurquesaTenue, TrazoH10.Linea);
            TrazoH10.Estirar(linea.rectTransform);
            return linea;
        }

        // Saca un texto del prefab, lo pone al pie del panel de detalle (que se llena abajo hacia arriba) y le da el estilo H10.
        private Text ReubicarAbajo(Text texto, float ancho, float alto, Color color)
        {
            if (texto == null)
            {
                return null;
            }
            texto.rectTransform.SetParent(panelDetalle, false);
            float baseY = texto == textoAviso ? RellenoPanel : RellenoPanel + 56f;
            TrazoH10.Colocar(texto.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(RellenoPanel, baseY), new Vector2(ancho, alto));
            texto.alignment = TextAnchor.LowerLeft;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.fontSize = TrazoH10.TamanoFila;
            texto.color = color;
            texto.resizeTextForBestFit = false;
            return texto;
        }

        // El precio que se cobra de verdad: con el favor de descuento si aplica.
        private int PrecioCobrado(DatosObjeto objeto)
        {
            float rebaja = RebajaPara(objeto.Oferta);
            return rebaja > 0f ? TiendaBasica.PrecioCon(objeto.Oferta, rebaja) : objeto.Oferta.Precio;
        }

        private int Lleva(DatosObjeto objeto)
        {
            if (objeto is DatosMunicionArma balasArma)
            {
                ReservaBalas reserva = Registro<ReservaBalas>.Actual;
                return reserva != null ? reserva.Cantidad(balasArma.Arma) : 0;
            }
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            return inventario != null ? inventario.Cantidad(objeto) : 0;
        }

        private void Pintar()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            textoDinero.text = string.Format(config.FormatoDinero, FormatoDinero.Texto(cartera != null ? cartera.Dinero : 0));
            textoDinero.color = TrazoH10.Turquesa;
            PintarPestana(textoPestanaComprar, lineaPestanaComprar, config.TextoComprar, pestana == PestanaTienda.Comprar);
            textoPestanaVender.gameObject.SetActive(PuedeVender);
            PintarPestana(textoPestanaVender, lineaPestanaVender, config.TextoVender, pestana == PestanaTienda.Vender);
            textoAyuda.text = pestana == PestanaTienda.Vender ? config.TextoAyudaVender : config.TextoAyuda;
            textoAyuda.color = TrazoH10.TextoTenue;

            PintarFavor();

            foreach (GameObject viejo in contenedoresFilas)
            {
                if (viejo != null)
                {
                    Destroy(viejo);
                }
            }
            contenedoresFilas.Clear();
            textosFilas.Clear();
            cadenasFilas.Clear();
            // Con muchas filas (El Mercado con balas) el paso se achica para que quepan en el panel.
            int cuantas = Mathf.Max(1, pestana == PestanaTienda.Vender ? vendibles.Count : objetos.Count);
            pasoFila = Mathf.Min(AltoFila + SeparacionFila, listaFilas.rect.height / cuantas);
            altoFilaActual = pasoFila - SeparacionFila;

            if (pestana == PestanaTienda.Vender)
            {
                PintarVender(cartera);
                return;
            }
            if (objetos.Count == 0)
            {
                AgregarFilaVacia(config.TextoSinOfertas);
                PintarDetalle(null, cartera);
                return;
            }
            for (int i = 0; i < objetos.Count; i++)
            {
                DatosObjeto objeto = objetos[i];
                int lleva = Lleva(objeto);
                string precio = FormatoDinero.Texto(objeto.Oferta.Precio);
                float rebaja = RebajaPara(objeto.Oferta);
                if (rebaja > 0f)
                {
                    precio = string.Format(config.FormatoPrecioRebajado, precio, FormatoDinero.Texto(TiendaBasica.PrecioCon(objeto.Oferta, rebaja)));
                }
                string fila = string.Format(config.FormatoFila, objeto.Nombre.ToUpperInvariant(), precio, lleva, objeto.Maximo);
                bool elegida = i == indice;
                bool alcanza = cartera != null && cartera.PuedePagar(PrecioCobrado(objeto));
                AgregarFila(i, elegida ? string.Format(config.FormatoFilaElegida, fila) : fila, objeto.Nombre.ToUpperInvariant(),
                    string.Format("{0} {1}/{2}", TextoLleva, lleva, objeto.Maximo), precio, alcanza ? TrazoH10.Amarillo : TrazoH10.Coral, elegida);
            }
            PintarDetalle(objetos[Mathf.Clamp(indice, 0, objetos.Count - 1)], cartera);
        }

        private void PintarFavor()
        {
            if (textoFavor == null)
            {
                return;
            }
            float rebaja = pestana == PestanaTienda.Comprar ? RebajaDelFavor() : 0f;
            textoFavor.gameObject.SetActive(rebaja > 0f);
            if (rebaja > 0f)
            {
                int quedan = Registro<ServicioFavores>.Actual.Cantidad(TipoFavor.Descuento);
                textoFavor.text = string.Format(config.FormatoFavorDescuento, Mathf.RoundToInt(rebaja * 100f), quedan);
                textoFavor.color = TrazoH10.Amarillo;
            }
        }

        private void PintarVender(ICartera cartera)
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ReglaVenta.Vendibles(inventario != null ? inventario.Todos : null, inventario, compra, vendibles);
            if (vendibles.Count == 0)
            {
                indice = 0;
                AgregarFilaVacia(config.TextoNadaQueVender);
                PintarDetalle(null, cartera);
                return;
            }
            indice = Mathf.Clamp(indice, 0, vendibles.Count - 1);
            for (int i = 0; i < vendibles.Count; i++)
            {
                DatosObjeto objeto = vendibles[i];
                string pago = FormatoDinero.Texto(ReglaVenta.Pago(objeto, compra));
                string fila = string.Format(config.FormatoFilaVenta, objeto.Nombre.ToUpperInvariant(), pago, inventario.Cantidad(objeto));
                bool elegida = i == indice;
                AgregarFila(i, elegida ? string.Format(config.FormatoFilaElegida, fila) : fila, objeto.Nombre.ToUpperInvariant(),
                    string.Format("{0} {1}", TextoTienes, inventario.Cantidad(objeto)), pago, TrazoH10.Amarillo, elegida);
            }
            PintarDetalle(vendibles[indice], cartera);
        }

        // El detalle de la fila elegida: lo que es, lo que cuesta (o paga) y cuánto te quedaría. Todo viene del objeto y la cartera reales.
        private void PintarDetalle(DatosObjeto objeto, ICartera cartera)
        {
            for (int i = 0; i < DatosDelDetalle; i++)
            {
                etiquetasDato[i].gameObject.SetActive(false);
                valoresDato[i].gameObject.SetActive(false);
            }
            if (objeto == null)
            {
                detalleNombre.text = string.Empty;
                detalleTipo.text = string.Empty;
                detalleDescripcion.text = string.Empty;
                return;
            }
            int dinero = cartera != null ? cartera.Dinero : 0;
            detalleNombre.text = objeto.Nombre.ToUpperInvariant();
            detalleTipo.text = System.Text.RegularExpressions.Regex.Replace(objeto.Tipo.ToString(), "(?<=[a-z])(?=[A-Z])", " ").ToUpperInvariant();
            detalleDescripcion.text = objeto.Oferta != null ? objeto.Oferta.Descripcion : string.Empty;
            int fila = 0;
            if (pestana == PestanaTienda.Vender)
            {
                int pago = ReglaVenta.Pago(objeto, compra);
                Dato(ref fila, TextoPaga, FormatoDinero.Texto(pago), TrazoH10.Amarillo);
                Dato(ref fila, TextoTienes, Lleva(objeto).ToString(), TrazoH10.Texto);
                Dato(ref fila, TextoQuedaria, FormatoDinero.Texto(dinero + pago), TrazoH10.Texto);
                return;
            }
            int precio = PrecioCobrado(objeto);
            bool alcanza = cartera != null && cartera.PuedePagar(precio);
            Dato(ref fila, TextoPrecio, FormatoDinero.Texto(precio), alcanza ? TrazoH10.Amarillo : TrazoH10.Coral);
            Dato(ref fila, TextoLleva, Lleva(objeto) + " / " + objeto.Maximo, TrazoH10.Texto);
            if (objeto is DatosMunicionArma balas)
            {
                Dato(ref fila, TextoBalas, balas.BalasPorPaquete.ToString(), TrazoH10.Texto);
            }
            Dato(ref fila, TextoQuedaria, FormatoDinero.Texto(dinero - precio), dinero - precio >= 0 ? TrazoH10.Texto : TrazoH10.Coral);
        }

        private void Dato(ref int fila, string etiqueta, string valor, Color color)
        {
            if (fila >= DatosDelDetalle)
            {
                return;
            }
            etiquetasDato[fila].text = etiqueta;
            etiquetasDato[fila].gameObject.SetActive(true);
            valoresDato[fila].text = valor;
            valoresDato[fila].color = color;
            valoresDato[fila].gameObject.SetActive(true);
            fila++;
        }

        private void PintarPestana(Text texto, Image linea, string nombre, bool elegida)
        {
            texto.text = nombre;
            texto.color = elegida ? TrazoH10.Turquesa : TrazoH10.TextoTenue;
            linea.color = elegida ? TrazoH10.Turquesa : TrazoH10.TurquesaTenue;
        }

        // Una fila de la lista: recuadro con línea (se enciende con el puntero y con la elección), nombre a la izquierda, cuánto llevas y el precio.
        private void AgregarFila(int numero, string cadena, string nombre, string lleva, string precio, Color colorPrecio, bool elegida)
        {
            RectTransform caja = TrazoH10.Nodo(listaFilas, "Fila_" + numero);
            TrazoH10.Colocar(caja, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -numero * pasoFila), new Vector2(0f, altoFilaActual));
            contenedoresFilas.Add(caja.gameObject);
            Image fondoFila = TrazoH10.Imagen(caja, "Fondo", elegida ? TrazoH10.TurquesaFondo : TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0f), TrazoH10.Relleno);
            fondoFila.raycastTarget = true;
            TrazoH10.Estirar(fondoFila.rectTransform);
            Color reposo = elegida ? TrazoH10.Turquesa : TrazoH10.ConAlfa(TrazoH10.TurquesaTenue, AlfaLineaFila);
            Image linea = TrazoH10.Imagen(caja, "Linea", reposo, TrazoH10.Linea);
            TrazoH10.Estirar(linea.rectTransform);
            HoverH10.Agregar(caja.gameObject, linea, reposo, TrazoH10.Turquesa);
            EventTrigger disparador = caja.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry clic = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clic.callback.AddListener(_ => ElegirFila(numero));
            disparador.triggers.Add(clic);

            Text texto = Instantiate(plantillaFila, caja);
            texto.gameObject.SetActive(true);
            texto.gameObject.name = "Nombre";
            texto.text = nombre;
            texto.color = TrazoH10.Texto;
            texto.fontSize = TrazoH10.TamanoFila;
            texto.alignment = TextAnchor.MiddleLeft;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.raycastTarget = false;
            LayoutElement elemento = texto.GetComponent<LayoutElement>();
            if (elemento != null)
            {
                elemento.ignoreLayout = true;
            }
            TrazoH10.Colocar(texto.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            texto.rectTransform.offsetMin = new Vector2(RellenoFila, 0f);
            texto.rectTransform.offsetMax = new Vector2(-(RellenoFila + AnchoPrecio + AnchoLleva), 0f);
            textosFilas.Add(texto);
            cadenasFilas.Add(cadena);

            Text llevas = TrazoH10.Etiqueta(caja, "Lleva", lleva, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.MiddleRight);
            TrazoH10.Punto(llevas.rectTransform, new Vector2(1f, 0.5f), new Vector2(-(RellenoFila + AnchoPrecio), 0f), new Vector2(AnchoLleva, AltoFila));
            Text costo = TrazoH10.Etiqueta(caja, "Precio", precio, TrazoH10.TamanoFila, colorPrecio, TextAnchor.MiddleRight);
            TrazoH10.Punto(costo.rectTransform, new Vector2(1f, 0.5f), new Vector2(-RellenoFila, 0f), new Vector2(AnchoPrecio, AltoFila));
            VestidoH10.Fuente(caja);
        }

        // Una fila de aviso cuando no hay nada (sin ofertas, nada que vender).
        private void AgregarFilaVacia(string mensaje)
        {
            RectTransform caja = TrazoH10.Nodo(listaFilas, "Fila_vacia");
            TrazoH10.Colocar(caja, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, AltoFila));
            contenedoresFilas.Add(caja.gameObject);
            Text texto = Instantiate(plantillaFila, caja);
            texto.gameObject.SetActive(true);
            texto.gameObject.name = "Nombre";
            texto.text = mensaje;
            texto.color = TrazoH10.TextoTenue;
            texto.fontSize = TrazoH10.TamanoFila;
            texto.alignment = TextAnchor.MiddleLeft;
            texto.raycastTarget = false;
            LayoutElement elemento = texto.GetComponent<LayoutElement>();
            if (elemento != null)
            {
                elemento.ignoreLayout = true;
            }
            TrazoH10.Colocar(texto.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            texto.rectTransform.offsetMin = new Vector2(RellenoFila, 0f);
            texto.rectTransform.offsetMax = Vector2.zero;
            textosFilas.Add(texto);
            cadenasFilas.Add(mensaje);
            VestidoH10.Fuente(caja);
        }

        // Un clic sobre una fila la elige.
        private void ElegirFila(int numero)
        {
            int cantidad = pestana == PestanaTienda.Comprar ? objetos.Count : vendibles.Count;
            if (!visible || !PilaPantallas.EsArriba(this) || numero < 0 || numero >= cantidad || numero == indice)
            {
                return;
            }
            indice = numero;
            textoAviso.gameObject.SetActive(false);
            Sonidos.Reproducir("UI_MOVER");
            Pintar();
        }
    }
}
