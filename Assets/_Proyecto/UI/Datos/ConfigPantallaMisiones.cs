using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// Textos, colores y tamaños de las pantallas de misiones a pie (T-186, D-tony-154): la lista de ofertas (teléfono, contacto del bar y
    /// encargos de la calle), el teléfono con sus tres pestañas y los avisos del contacto y de los vecinos.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantallas de misiones", fileName = "ConfigPantallaMisiones_Nueva")]
    public class ConfigPantallaMisiones : ScriptableObject
    {
        [Header("Colores")]
        [Tooltip("Color de la fila elegida y de la pestaña elegida (turquesa neón).")]
        [SerializeField] private Color colorElegido = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de las filas disponibles que no están elegidas.")]
        [SerializeField] private Color colorNormal = new Color(0.8f, 0.85f, 0.95f, 1f);

        [Tooltip("Color de las filas que no se pueden aceptar y de las pestañas apagadas (gris).")]
        [SerializeField] private Color colorApagado = new Color(0.4f, 0.43f, 0.5f, 1f);

        [Tooltip("Color del título y de los avisos (magenta neón).")]
        [SerializeField] private Color colorTitulo = new Color(1f, 0.2f, 0.7f, 1f);

        [Tooltip("Color de los avisos (naranja neón).")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.55f, 0.1f, 1f);

        [Tooltip("Fondo semitransparente de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.02f, 0.06f, 0.88f);

        [Tooltip("Color de la barra de reputación cuando es positiva (turquesa neón).")]
        [SerializeField] private Color colorBarraPositiva = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de la barra de reputación cuando es negativa (magenta neón).")]
        [SerializeField] private Color colorBarraNegativa = new Color(1f, 0.1f, 0.6f, 1f);

        [Tooltip("Color del hueco de la barra de reputación.")]
        [SerializeField] private Color colorHuecoBarra = new Color(0.1f, 0.12f, 0.18f, 0.9f);

        [Header("Tamaños de letra (puntos)")]
        [Min(8)] [SerializeField] private int tamanoTitulo = 52;
        [Min(8)] [SerializeField] private int tamanoFilas = 34;
        [Min(8)] [SerializeField] private int tamanoPestanas = 38;
        [Min(8)] [SerializeField] private int tamanoTexto = 36;
        [Min(8)] [SerializeField] private int tamanoAyuda = 26;

        [Header("Lista de ofertas")]
        [Tooltip("Título cuando se abre desde el teléfono.")]
        [SerializeField] private string tituloContratos = "CONTRATOS";

        [Tooltip("Título cuando se abre con el contacto del bar.")]
        [SerializeField] private string tituloContacto = "CONTACTO DEL BAR";

        [Tooltip("Título cuando se abre con un vecino en la calle.")]
        [SerializeField] private string tituloEncargo = "ENCARGO DEL BARRIO";

        [Tooltip("Marca de la fila elegida; {0} es el texto de la fila.")]
        [SerializeField] private string formatoElegida = "> {0}";

        [Tooltip("Lo que une las partes de una fila.")]
        [SerializeField] private string separador = "  ·  ";

        [Tooltip("Efecto en reputación de una fila; {0} es el del barrio y {1} el de las corporaciones, con signo.")]
        [SerializeField] private string formatoReputacion = "BARRIO {0} · CORP. {1}";

        [Tooltip("Chatarra de una fila; {0} es la cantidad y {1} el nombre del objeto.")]
        [SerializeField] private string formatoChatarra = "{0}x {1}";

        [Tooltip("Favor que da una misión; {0} es su nombre.")]
        [SerializeField] private string formatoFavor = "FAVOR: {0}";

        [Tooltip("Favor que gasta una misión al aceptarla; {0} es su nombre.")]
        [SerializeField] private string formatoUsaFavor = "USA 1 {0}";

        [Tooltip("Fila que no se puede aceptar; {0} es el título y {1} el motivo.")]
        [SerializeField] private string formatoNoDisponible = "{0}  ·  {1}";

        [SerializeField] private string textoSinOfertas = "SIN OFERTAS";

        [SerializeField] private string textoAyudaLista = "↑ ↓ ELEGIR · ENTER ACEPTAR · ESC SALIR";

        [SerializeField] private string textoAyudaListaEmbebida = "← → PESTAÑA · ↑ ↓ ELEGIR · ENTER ACEPTAR · T / ESC CERRAR";

        [Header("Motivos por los que no se ofrece")]
        [SerializeField] private string textoDeDia = "DE DÍA";
        [SerializeField] private string textoDeNoche = "DE NOCHE";
        [SerializeField] private string textoBarrioNoTeHabla = "EL BARRIO NO TE HABLA";
        [SerializeField] private string textoCorporacionesNoLlaman = "YA NO TE LLAMAN";

        [Tooltip("Falta reputación en el barrio; {0} es el nombre del nivel que se pide.")]
        [SerializeField] private string formatoReputacionBarrio = "NECESITAS SER {0} EN EL BARRIO";

        [Tooltip("Falta reputación con las corporaciones; {0} es el nombre del nivel que se pide.")]
        [SerializeField] private string formatoReputacionCorporaciones = "NECESITAS SER {0} CON LAS CORPORACIONES";

        [SerializeField] private string textoReputacionBaja = "NECESITAS MÁS REPUTACIÓN";

        [Tooltip("Liga mínima; {0} es el número de la liga.")]
        [SerializeField] private string formatoLiga = "LIGA {0}";

        [Tooltip("Falta un favor; {0} es el nombre del favor.")]
        [SerializeField] private string formatoFaltaFavor = "NECESITAS 1 {0}";

        [Tooltip("Nombre de cada favor en el motivo, en el orden de TipoFavor (Descuento, Informacion, Escondite).")]
        [SerializeField] private string[] nombresFavor = { "DESCUENTO", "INFORMACIÓN", "ESCONDITE" };

        [Tooltip("Aviso al terminar una misión que daba un favor con el bolsillo lleno; {0} es el máximo.")]
        [SerializeField] private string formatoFavorPerdido = "FAVOR PERDIDO: YA TIENES {0}";

        [SerializeField] private string textoHechaHoy = "YA LO HICISTE HOY";
        [SerializeField] private string textoYaHecha = "YA LA HICISTE";
        [SerializeField] private string textoOtraActiva = "YA TIENES UNA MISIÓN";
        [SerializeField] private string textoFueraDeLibre = "TERMINA EL PRÓLOGO";
        [SerializeField] private string textoFaltanPuntos = "NO DISPONIBLE";

        [Header("Teléfono")]
        [SerializeField] private string tituloTelefono = "TELÉFONO";
        [SerializeField] private string textoPestanaContratos = "CONTRATOS";
        [SerializeField] private string textoPestanaMision = "MISIÓN";
        [SerializeField] private string textoPestanaReputacion = "REPUTACIÓN";
        [SerializeField] private string textoPestanaFavores = "FAVORES";

        [Tooltip("Línea de la pestaña FAVORES; {0} nombre, {1} cuántos lleva, {2} máximo, {3} descripción, {4} dónde se usa.")]
        [SerializeField] private string formatoFilaFavor = "{0}   {1}/{2}   ·   {3}\nSE USA EN: {4}\n";

        [Tooltip("Dónde se usa cada favor, en el orden de TipoFavor (Descuento, Informacion, Escondite).")]
        [SerializeField] private string[] dondeSeUsaFavor = { "MERCADO NEGRO", "CONTACTO DEL BAR", "ESCONDITES DEL BARRIO" };

        [SerializeField] private string textoSinFavores = "SIN FAVORES";

        [Tooltip("Marca de la pestaña elegida; {0} es su nombre.")]
        [SerializeField] private string formatoPestanaElegida = "[ {0} ]";

        [SerializeField] private string textoNadieContesta = "NADIE CONTESTA · LLAMA DE DÍA";
        [SerializeField] private string textoYaNoTeLlaman = "YA NO TE LLAMAN";
        [SerializeField] private string textoSinMision = "SIN MISIÓN";

        [Tooltip("Paso actual; {0} es el número del paso, {1} el total y {2} el texto del paso.")]
        [SerializeField] private string formatoPaso = "PASO {0}/{1} · {2}";

        [Tooltip("Tiempo que queda; {0} son los minutos y segundos (MM:SS).")]
        [SerializeField] private string formatoTiempo = "TIEMPO {0}";

        [SerializeField] private string textoAbandonar = "Enter · ABANDONAR";
        [SerializeField] private string textoConfirmarAbandonar = "¿ABANDONAR LA MISIÓN?   Enter · SÍ   Esc · NO";
        [SerializeField] private string textoAyudaTelefono = "← → PESTAÑA · T / ESC CERRAR";
        [SerializeField] private string textoAyudaMision = "← → PESTAÑA · ENTER ABANDONAR · T / ESC CERRAR";

        [Tooltip("Una barra de reputación; {0} es el nombre, {1} el valor con signo y {2} el nombre del nivel.")]
        [SerializeField] private string formatoBarra = "{0}   {1}   {2}";

        [SerializeField] private string nombreBarrio = "BARRIO";
        [SerializeField] private string nombreCorporaciones = "CORPORACIONES";

        [Header("Contacto del bar y vecinos")]
        [SerializeField] private string textoContacto = "HABLAR CON EL CONTACTO";
        [SerializeField] private string textoContactoDeDia = "EL CONTACTO LLEGA DE NOCHE";
        [SerializeField] private string textoContactoCerrado = "CERRADO";

        [Tooltip("Aviso de un vecino con una misión; {0} es el título de la misión.")]
        [SerializeField] private string formatoAyudar = "AYUDAR: {0}";

        [Header("Medidas de la barra de reputación (px de referencia)")]
        [Min(10f)] [SerializeField] private float anchoBarra = 760f;
        [Min(2f)] [SerializeField] private float altoBarra = 34f;

        public Color ColorElegido => colorElegido;
        public Color ColorNormal => colorNormal;
        public Color ColorApagado => colorApagado;
        public Color ColorTitulo => colorTitulo;
        public Color ColorAviso => colorAviso;
        public Color ColorFondo => colorFondo;
        public Color ColorBarraPositiva => colorBarraPositiva;
        public Color ColorBarraNegativa => colorBarraNegativa;
        public Color ColorHuecoBarra => colorHuecoBarra;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoFilas => tamanoFilas;
        public int TamanoPestanas => tamanoPestanas;
        public int TamanoTexto => tamanoTexto;
        public int TamanoAyuda => tamanoAyuda;
        public string TituloContratos => tituloContratos;
        public string TituloContacto => tituloContacto;
        public string TituloEncargo => tituloEncargo;
        public string FormatoElegida => formatoElegida;
        public string Separador => separador;
        public string FormatoReputacion => formatoReputacion;
        public string FormatoChatarra => formatoChatarra;
        public string FormatoFavor => formatoFavor;
        public string FormatoNoDisponible => formatoNoDisponible;
        public string TextoSinOfertas => textoSinOfertas;
        public string TextoAyudaLista => textoAyudaLista;
        public string TextoAyudaListaEmbebida => textoAyudaListaEmbebida;
        public string TextoDeDia => textoDeDia;
        public string TextoDeNoche => textoDeNoche;
        public string TextoBarrioNoTeHabla => textoBarrioNoTeHabla;
        public string TextoCorporacionesNoLlaman => textoCorporacionesNoLlaman;
        public string FormatoReputacionBarrio => formatoReputacionBarrio;
        public string FormatoReputacionCorporaciones => formatoReputacionCorporaciones;
        public string TextoReputacionBaja => textoReputacionBaja;
        public string FormatoLiga => formatoLiga;
        public string FormatoFaltaFavor => formatoFaltaFavor;
        public string FormatoUsaFavor => formatoUsaFavor;
        public string FormatoFavorPerdido => formatoFavorPerdido;
        public string TextoPestanaFavores => textoPestanaFavores;
        public string FormatoFilaFavor => formatoFilaFavor;
        public string TextoSinFavores => textoSinFavores;

        /// <summary>Dónde se usa un favor (por TipoFavor); si falta, vacío.</summary>
        public string DondeSeUsaFavor(int tipo)
        {
            return dondeSeUsaFavor != null && tipo >= 0 && tipo < dondeSeUsaFavor.Length ? dondeSeUsaFavor[tipo] : string.Empty;
        }
        public string TextoHechaHoy => textoHechaHoy;
        public string TextoYaHecha => textoYaHecha;
        public string TextoOtraActiva => textoOtraActiva;
        public string TextoFueraDeLibre => textoFueraDeLibre;
        public string TextoFaltanPuntos => textoFaltanPuntos;
        public string TituloTelefono => tituloTelefono;
        public string TextoPestanaContratos => textoPestanaContratos;
        public string TextoPestanaMision => textoPestanaMision;
        public string TextoPestanaReputacion => textoPestanaReputacion;
        public string FormatoPestanaElegida => formatoPestanaElegida;
        public string TextoNadieContesta => textoNadieContesta;
        public string TextoYaNoTeLlaman => textoYaNoTeLlaman;
        public string TextoSinMision => textoSinMision;
        public string FormatoPaso => formatoPaso;
        public string FormatoTiempo => formatoTiempo;
        public string TextoAbandonar => textoAbandonar;
        public string TextoConfirmarAbandonar => textoConfirmarAbandonar;
        public string TextoAyudaTelefono => textoAyudaTelefono;
        public string TextoAyudaMision => textoAyudaMision;
        public string FormatoBarra => formatoBarra;
        public string NombreBarrio => nombreBarrio;
        public string NombreCorporaciones => nombreCorporaciones;
        public string TextoContacto => textoContacto;
        public string TextoContactoDeDia => textoContactoDeDia;
        public string TextoContactoCerrado => textoContactoCerrado;
        public string FormatoAyudar => formatoAyudar;
        public float AnchoBarra => anchoBarra;
        public float AltoBarra => altoBarra;

        /// <summary>Nombre de un favor para el motivo "NECESITAS 1 INFORMACIÓN"; si falta, el nombre del enum en mayúsculas.</summary>
        public string NombreFavor(int tipo)
        {
            if (nombresFavor != null && tipo >= 0 && tipo < nombresFavor.Length && !string.IsNullOrEmpty(nombresFavor[tipo]))
            {
                return nombresFavor[tipo];
            }
            return tipo.ToString();
        }
    }
}
