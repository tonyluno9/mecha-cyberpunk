using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
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
        [SerializeField] private InputAction arriba = new InputAction("TiendaArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [SerializeField] private InputAction abajo = new InputAction("TiendaAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [SerializeField] private InputAction izquierda = new InputAction("TiendaIzquierda", InputActionType.Button, "<Keyboard>/leftArrow");
        [SerializeField] private InputAction derecha = new InputAction("TiendaDerecha", InputActionType.Button, "<Keyboard>/rightArrow");
        [SerializeField] private InputAction confirmar = new InputAction("TiendaEnter", InputActionType.Button, "<Keyboard>/enter");
        [SerializeField] private InputAction salir = new InputAction("TiendaEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly List<Text> textosFilas = new List<Text>();
        private readonly List<DatosObjeto> objetos = new List<DatosObjeto>();
        private readonly List<DatosObjeto> vendibles = new List<DatosObjeto>();
        private readonly List<TipoObjeto> compra = new List<TipoObjeto>();
        private ITienda tienda;
        private Action alSalir;
        private PestanaTienda pestana;
        private int indice;
        private bool visible;

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
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        /// <summary>Abre la pantalla en COMPRAR con la lista de objetos que la tienda vende. <paramref name="alSalir"/> avisa Esc.</summary>
        public void Mostrar(ITienda tienda, IReadOnlyList<DatosObjeto> objetosEnVenta, Action alSalir,
            IReadOnlyList<TipoObjeto> tiposQueCompra = null)
        {
            compra.Clear();
            if (tiposQueCompra != null)
            {
                compra.AddRange(tiposQueCompra);
            }
            this.tienda = tienda;
            this.alSalir = alSalir;
            objetos.Clear();
            objetos.AddRange(objetosEnVenta);
            pestana = PestanaTienda.Comprar;
            indice = 0;

            fondo.color = config.ColorFondo;
            Preparar(textoTitulo, string.Format(config.FormatoTitulo, tienda.Nombre.ToUpperInvariant()), config.ColorElegido, config.TamanoTitulo);
            Preparar(textoAviso, string.Empty, config.ColorAviso, config.TamanoTexto);
            textoAviso.gameObject.SetActive(false);
            Pintar();
            visible = true;
            contenido.SetActive(true);
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
            if (!visible)
            {
                return;
            }
            if (salir.WasPressedThisFrame())
            {
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
            }
            if (confirmar.WasPressedThisFrame())
            {
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
                switch (tienda.Comprar(objeto.Oferta, cartera))
                {
                    case ResultadoCompra.Comprado:
                        inventario.Agregar(objeto, 1);
                        MostrarAviso(string.Format(config.FormatoComprado, objeto.Nombre.ToUpperInvariant()));
                        break;
                    case ResultadoCompra.SinDinero:
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
                string fila = string.Format(config.FormatoFila, objeto.Nombre.ToUpperInvariant(), FormatoDinero.Texto(objeto.Oferta.Precio),
                    lleva, objeto.Maximo);
                bool elegida = i == indice;
                AgregarFila(elegida ? string.Format(config.FormatoFilaElegida, fila) : fila, elegida ? config.ColorElegido : config.ColorNormal);
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

        private void AgregarFila(string contenidoFila, Color color)
        {
            Text texto = Instantiate(plantillaFila, listaFilas);
            texto.gameObject.SetActive(true);
            texto.fontSize = config.TamanoFilas;
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
