using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores, tamaños y tiempos de la clínica de implantes (T-193).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de la clínica", fileName = "ConfigPantallaClinica_Nueva")]
    public class ConfigPantallaClinica : ScriptableObject
    {
        [Header("Colores")]
        [Tooltip("Fila elegida y título (turquesa).")]
        [SerializeField] private Color colorElegido = new Color(0f, 1f, 0.9f, 1f);
        [Tooltip("Filas normales.")]
        [SerializeField] private Color colorNormal = new Color(0.8f, 0.85f, 0.95f, 1f);
        [Tooltip("Opciones que no se abren todavía (gris).")]
        [SerializeField] private Color colorApagado = new Color(0.35f, 0.38f, 0.45f, 1f);
        [Tooltip("Avisos de que no alcanza (naranja suave).")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.62f, 0.25f, 1f);
        [Tooltip("Precios y la pregunta de confirmar (magenta).")]
        [SerializeField] private Color colorPrecio = new Color(1f, 0.3f, 0.7f, 1f);
        [Tooltip("Fondo semitransparente de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.03f, 0.06f, 0.9f);

        [Header("Tamaños de letra (puntos)")]
        [Min(8)] [SerializeField] private int tamanoTitulo = 80;
        [Min(8)] [SerializeField] private int tamanoDinero = 44;
        [Min(8)] [SerializeField] private int tamanoEspacio = 30;
        [Min(8)] [SerializeField] private int tamanoOpcion = 40;
        [Min(8)] [SerializeField] private int tamanoDescripcion = 44;
        [Min(8)] [SerializeField] private int tamanoAyuda = 28;

        [Header("Textos de la pantalla")]
        [SerializeField] private string textoTitulo = "I+D+I";
        [Tooltip("{0} = dinero con Ƀ.")]
        [SerializeField] private string formatoDinero = "TIENES {0}";
        [Tooltip("Nombre de cada espacio, en el orden de EspacioImplante: ojos, brazos, piernas, sistema nervioso.")]
        [SerializeField] private string[] nombresEspacio = { "OJOS", "BRAZOS", "PIERNAS", "SINAPSIS" };
        [Tooltip("{0} = espacio, {1} = lo instalado.")]
        [SerializeField] private string formatoEspacio = "{0} · {1}";
        [Tooltip("Lo instalado cuando no hay nada.")]
        [SerializeField] private string textoVacio = "—";
        [Tooltip("{0} = nombre del implante, {1} = precio con Ƀ.")]
        [SerializeField] private string formatoPrecio = "{0} · {1}";
        [Tooltip("Se agrega al precio cuando ya tienes el nivel anterior.")]
        [SerializeField] private string textoDiferencia = "PAGAS LA DIFERENCIA";
        [Tooltip("{0} = nombre del implante, {1} = liga que lo abre.")]
        [SerializeField] private string formatoLiga = "{0} · SE ABRE EN LA LIGA {1}";
        [Tooltip("{0} = nombre del implante.")]
        [SerializeField] private string formatoSinDinero = "{0} · NO TE ALCANZA";
        [SerializeField] private string textoAvisoSinDinero = "NO TE ALCANZA";
        [SerializeField] private string textoLoMejor = "LO MEJOR QUE HAY";
        [Tooltip("{0} = precio con Ƀ.")]
        [SerializeField] private string formatoConfirmar = "¿INSTALAR POR {0}? ENTER SÍ · ESC NO";
        [SerializeField] private string textoAyuda = "↑ ↓ elegir · Enter instalar · Esc salir";
        [Tooltip("Marca de la fila elegida; {0} = texto.")]
        [SerializeField] private string formatoElegido = "> {0}";

        [Header("Mostrador")]
        [Tooltip("El HUD le antepone \"E · \" cuando se puede usar.")]
        [SerializeField] private string textoMostrador = "I+D+I";
        [SerializeField] private string textoCerrado = "I+D+I · CERRADA";

        [Header("Al instalar")]
        [Tooltip("Lo que tarda el fundido a negro (s).")]
        [Min(0f)] [SerializeField] private float segundosFundidoNegro = 1.5f;
        [Tooltip("Lo que se queda en negro (s).")]
        [Min(0f)] [SerializeField] private float segundosEnNegro = 0.6f;
        [Tooltip("Lo que tarda en volver la imagen al despertar (s).")]
        [Min(0f)] [SerializeField] private float segundosFundidoVuelta = 1f;
        [Tooltip("Cuánto dura el subtítulo del doc (s).")]
        [Min(0f)] [SerializeField] private float segundosSubtitulo = 3f;
        [Tooltip("Horas que pasan en la operación (h).")]
        [Min(0f)] [SerializeField] private float horasOperacion = 2f;
        [SerializeField] private string textoSubtitulo = "EL DOC: Listo. No lo andes presumiendo.";

        public Color ColorElegido => colorElegido;
        public Color ColorNormal => colorNormal;
        public Color ColorApagado => colorApagado;
        public Color ColorAviso => colorAviso;
        public Color ColorPrecio => colorPrecio;
        public Color ColorFondo => colorFondo;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoDinero => tamanoDinero;
        public int TamanoEspacio => tamanoEspacio;
        public int TamanoOpcion => tamanoOpcion;
        public int TamanoDescripcion => tamanoDescripcion;
        public int TamanoAyuda => tamanoAyuda;
        public string TextoTitulo => textoTitulo;
        public string FormatoDinero => formatoDinero;
        public string FormatoEspacio => formatoEspacio;
        public string TextoVacio => textoVacio;
        public string FormatoPrecio => formatoPrecio;
        public string TextoDiferencia => textoDiferencia;
        public string FormatoLiga => formatoLiga;
        public string FormatoSinDinero => formatoSinDinero;
        public string TextoAvisoSinDinero => textoAvisoSinDinero;
        public string TextoLoMejor => textoLoMejor;
        public string FormatoConfirmar => formatoConfirmar;
        public string TextoAyuda => textoAyuda;
        public string FormatoElegido => formatoElegido;
        public string TextoMostrador => textoMostrador;
        public string TextoCerrado => textoCerrado;
        public float SegundosFundidoNegro => segundosFundidoNegro;
        public float SegundosEnNegro => segundosEnNegro;
        public float SegundosFundidoVuelta => segundosFundidoVuelta;
        public float SegundosSubtitulo => segundosSubtitulo;
        public float HorasOperacion => horasOperacion;
        public string TextoSubtitulo => textoSubtitulo;

        /// <summary>Nombre del espacio por su número en <c>EspacioImplante</c>; vacío si no hay.</summary>
        public string NombreEspacio(int espacio) => nombresEspacio != null && espacio >= 0 && espacio < nombresEspacio.Length ? nombresEspacio[espacio] : string.Empty;
    }
}
