using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
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
        private const int FilasQueCaben = 5;
        private const float MinimoEscalaFilas = 0.3f;
        private const int MinimoLetraFilas = 12;

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

        public bool Visible => visible;
        public PestanaTienda Pestana => pestana;
        public int IndiceElegido => indice;
        public int CantidadFilas => textosFilas.Count;
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
        public string TextoFila(int fila) => textosFilas[fila].text;

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

            fondo.color = config.ColorFondo;
            Preparar(textoTitulo, string.Format(config.FormatoTitulo, tienda.Nombre.ToUpperInvariant()), config.ColorElegido, config.TamanoTitulo);
            Preparar(textoAviso, string.Empty, config.ColorAviso, config.TamanoTexto);
            textoAviso.gameObject.SetActive(false);
            Pintar();
            visible = true;
            contenido.SetActive(true);
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
            else if (!inventario.PuedeAgregar(objeto, 1))
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
                        inventario.Agregar(objeto, 1);
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
            textoAviso.gameObject.SetActive(true);
        }

        private void Pintar()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            Preparar(textoDinero, string.Format(config.FormatoDinero, FormatoDinero.Texto(cartera != null ? cartera.Dinero : 0)),
                config.ColorDinero, config.TamanoTexto);
            PintarPestana(textoPestanaComprar, config.TextoComprar, pestana == PestanaTienda.Comprar);
            textoPestanaVender.gameObject.SetActive(PuedeVender);
            PintarPestana(textoPestanaVender, config.TextoVender, pestana == PestanaTienda.Vender);
            Preparar(textoAyuda, pestana == PestanaTienda.Vender ? config.TextoAyudaVender : config.TextoAyuda, config.ColorNormal,
                config.TamanoAyuda);

            PintarFavor();

            foreach (Text viejo in textosFilas)
            {
                if (viejo != null)
                {
                    Destroy(viejo.gameObject);
                }
            }
            textosFilas.Clear();

            if (pestana == PestanaTienda.Vender)
            {
                PintarVender();
                return;
            }
            if (objetos.Count == 0)
            {
                AgregarFila(config.TextoSinOfertas, config.ColorApagado);
                return;
            }
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            for (int i = 0; i < objetos.Count; i++)
            {
                DatosObjeto objeto = objetos[i];
                int lleva = inventario != null ? inventario.Cantidad(objeto) : 0;
                string precio = FormatoDinero.Texto(objeto.Oferta.Precio);
                float rebaja = RebajaPara(objeto.Oferta);
                if (rebaja > 0f)
                {
                    precio = string.Format(config.FormatoPrecioRebajado, precio, FormatoDinero.Texto(TiendaBasica.PrecioCon(objeto.Oferta, rebaja)));
                }
                string fila = string.Format(config.FormatoFila, objeto.Nombre.ToUpperInvariant(), precio, lleva, objeto.Maximo);
                bool elegida = i == indice;
                AgregarFila(elegida ? string.Format(config.FormatoFilaElegida, fila) : fila, elegida ? config.ColorElegido : config.ColorNormal);
            }
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
                Preparar(textoFavor, string.Format(config.FormatoFavorDescuento, Mathf.RoundToInt(rebaja * 100f), quedan),
                    config.ColorFavor, config.TamanoFavor);
            }
        }

        private void PintarVender()
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ReglaVenta.Vendibles(inventario != null ? inventario.Todos : null, inventario, compra, vendibles);
            if (vendibles.Count == 0)
            {
                indice = 0;
                AgregarFila(config.TextoNadaQueVender, config.ColorApagado);
                return;
            }
            indice = Mathf.Clamp(indice, 0, vendibles.Count - 1);
            for (int i = 0; i < vendibles.Count; i++)
            {
                DatosObjeto objeto = vendibles[i];
                string fila = string.Format(config.FormatoFilaVenta, objeto.Nombre.ToUpperInvariant(),
                    FormatoDinero.Texto(ReglaVenta.Pago(objeto, compra)), inventario.Cantidad(objeto));
                bool elegida = i == indice;
                AgregarFila(elegida ? string.Format(config.FormatoFilaElegida, fila) : fila, elegida ? config.ColorElegido : config.ColorNormal);
            }
        }

        private void PintarPestana(Text texto, string nombre, bool elegida)
        {
            texto.text = elegida ? string.Format(config.FormatoPestanaElegida, nombre) : nombre;
            texto.color = elegida ? config.ColorElegido : config.ColorApagado;
            texto.fontSize = config.TamanoTexto;
        }

        // T-416: con muchas filas (El Mercado con 5 armas) se achican la letra y el alto para que quepan en la lista.
        private float EscalaFilas(int cantidad)
        {
            LayoutElement plantilla = plantillaFila.GetComponent<LayoutElement>();
            VerticalLayoutGroup grupo = listaFilas.GetComponent<VerticalLayoutGroup>();
            if (plantilla == null || grupo == null || cantidad <= FilasQueCaben)
            {
                return 1f;
            }
            float necesario = cantidad * plantilla.preferredHeight + (cantidad - 1) * grupo.spacing;
            float disponible = FilasQueCaben * plantilla.preferredHeight + (FilasQueCaben - 1) * grupo.spacing;
            return Mathf.Clamp(disponible / necesario, MinimoEscalaFilas, 1f);
        }

        private void AgregarFila(string contenidoFila, Color color)
        {
            Text texto = Instantiate(plantillaFila, listaFilas);
            texto.gameObject.SetActive(true);
            float escala = EscalaFilas(pestana == PestanaTienda.Vender ? vendibles.Count : objetos.Count);
            texto.fontSize = Mathf.Max(MinimoLetraFilas, Mathf.RoundToInt(config.TamanoFilas * escala));
            LayoutElement elemento = texto.GetComponent<LayoutElement>();
            if (elemento != null && escala < 1f)
            {
                elemento.minHeight = elemento.preferredHeight = elemento.preferredHeight * escala;
            }
            texto.text = contenidoFila;
            texto.color = color;
            textosFilas.Add(texto);
        }

        private static void Preparar(Text texto, string contenidoTexto, Color color, int tamano)
        {
            texto.text = contenidoTexto;
            texto.color = color;
            texto.fontSize = tamano;
        }
    }
}
