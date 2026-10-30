using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores y tamaños de la pantalla de tienda del mercado negro (T-146).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de tienda", fileName = "ConfigPantallaTienda_Nueva")]
    public class ConfigPantallaTienda : ScriptableObject
    {
        [Tooltip("Color de la fila y la pestaña elegidas (neón).")]
        [SerializeField] private Color colorElegido = new Color(1f, 0.1f, 0.7f, 1f);

        [Tooltip("Color de lo que no está elegido.")]
        [SerializeField] private Color colorNormal = new Color(0.75f, 0.8f, 0.9f, 1f);

        [Tooltip("Color de la pestaña que no está abierta.")]
        [SerializeField] private Color colorApagado = new Color(0.35f, 0.38f, 0.45f, 1f);

        [Tooltip("Color del dinero.")]
        [SerializeField] private Color colorDinero = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Color del aviso (no alcanza, máximo).")]
        [SerializeField] private Color colorAviso = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Fondo semitransparente de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.04f, 0.01f, 0.06f, 0.88f);

        [Tooltip("Tamaño de letra del título (puntos).")]
        [Min(8)] [SerializeField] private int tamanoTitulo = 80;
        [Tooltip("Tamaño de letra del dinero, las pestañas y el aviso (puntos).")]
        [Min(8)] [SerializeField] private int tamanoTexto = 46;
        [Tooltip("Tamaño de letra de las filas de la lista (puntos).")]
        [Min(8)] [SerializeField] private int tamanoFilas = 50;
        [Tooltip("Tamaño de letra de la ayuda (puntos).")]
        [Min(8)] [SerializeField] private int tamanoAyuda = 28;

        [Tooltip("{0} = nombre de la tienda (en mayúsculas).")]
        [SerializeField] private string formatoTitulo = "{0}";
        [Tooltip("{0} = dinero con Ƀ.")]
        [SerializeField] private string formatoDinero = "TIENES {0}";
        [SerializeField] private string textoComprar = "COMPRAR";
        [SerializeField] private string textoVender = "VENDER";
        [Tooltip("Pestaña elegida: {0} = nombre de la pestaña.")]
        [SerializeField] private string formatoPestanaElegida = "[ {0} ]";
        [Tooltip("{0} = nombre del objeto (mayúsculas), {1} = precio con Ƀ, {2} = cuántos lleva, {3} = máximo.")]
        [SerializeField] private string formatoFila = "{0}     {1}     LLEVAS {2}/{3}";
        [Tooltip("Fila elegida: {0} = texto de la fila.")]
        [SerializeField] private string formatoFilaElegida = "> {0}";
        [Tooltip("Pestaña VENDER: {0} = nombre (mayúsculas), {1} = lo que paga con Ƀ, {2} = cuántos tiene.")]
        [SerializeField] private string formatoFilaVenta = "{0}     {1} c/u     TIENES {2}";
        [SerializeField] private string textoNadaQueVender = "NO TIENES NADA QUE LE INTERESE";
        [Tooltip("{0} = nombre del objeto (mayúsculas), {1} = lo que pagaron con Ƀ.")]
        [SerializeField] private string formatoVendido = "VENDISTE {0} POR {1}";
        [SerializeField] private string textoAyudaVender = "↑ ↓ elegir · ← → pestaña · Enter vender · Esc salir";
        [SerializeField] private string textoSinOfertas = "NO HAY NADA A LA VENTA";
        [Tooltip("{0} = nombre del objeto (mayúsculas).")]
        [SerializeField] private string formatoComprado = "COMPRASTE {0}";
        [SerializeField] private string textoMaximo = "LLEVAS EL MÁXIMO";
        [SerializeField] private string textoNoAlcanza = "NO TE ALCANZA";
        [SerializeField] private string textoNoDisponible = "NO SE VENDE AQUÍ";
        [SerializeField] private string textoAyuda = "↑ ↓ elegir · ← → pestaña · Enter comprar · Esc salir";
        [Tooltip("Precio con el favor de descuento: {0} = precio normal con Ƀ, {1} = precio rebajado con Ƀ.")]
        [SerializeField] private string formatoPrecioRebajado = "{0} → {1}";
        [Tooltip("Línea del favor de descuento: {0} = porcentaje de rebaja, {1} = favores que quedan.")]
        [SerializeField] private string formatoFavorDescuento = "FAVOR · −{0} % EN ESTA COMPRA · TE QUEDAN {1}";
        [Tooltip("Tamaño de letra de la línea del favor (puntos).")]
        [Min(8)] [SerializeField] private int tamanoFavor = 34;
        [Tooltip("Color de la línea del favor.")]
        [SerializeField] private Color colorFavor = new Color(1f, 0.85f, 0.2f, 1f);

        public Color ColorElegido => colorElegido;
        public Color ColorNormal => colorNormal;
        public Color ColorApagado => colorApagado;
        public Color ColorDinero => colorDinero;
        public Color ColorAviso => colorAviso;
        public Color ColorFondo => colorFondo;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoTexto => tamanoTexto;
        public int TamanoFilas => tamanoFilas;
        public int TamanoAyuda => tamanoAyuda;
        public string FormatoTitulo => formatoTitulo;
        public string FormatoDinero => formatoDinero;
        public string TextoComprar => textoComprar;
        public string TextoVender => textoVender;
        public string FormatoPestanaElegida => formatoPestanaElegida;
        public string FormatoFila => formatoFila;
        public string FormatoFilaElegida => formatoFilaElegida;
        public string FormatoFilaVenta => formatoFilaVenta;
        public string TextoNadaQueVender => textoNadaQueVender;
        public string FormatoVendido => formatoVendido;
        public string TextoAyudaVender => textoAyudaVender;
        public string TextoSinOfertas => textoSinOfertas;
        public string FormatoComprado => formatoComprado;
        public string TextoMaximo => textoMaximo;
        public string TextoNoAlcanza => textoNoAlcanza;
        public string TextoNoDisponible => textoNoDisponible;
        public string TextoAyuda => textoAyuda;
        public string FormatoPrecioRebajado => formatoPrecioRebajado;
        public string FormatoFavorDescuento => formatoFavorDescuento;
        public int TamanoFavor => tamanoFavor;
        public Color ColorFavor => colorFavor;
    }
}
