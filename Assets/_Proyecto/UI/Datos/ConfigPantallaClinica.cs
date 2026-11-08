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

        [Header("Holograma del cuerpo (T-815)")]
        [Tooltip("El shader holográfico del banco de armas (HologramaArma.mat).")]
        [SerializeField] private Material materialHolograma;
        [Tooltip("Material aditivo de la UI (UIAditivo.mat): el holograma se suma sobre el fondo oscuro.")]
        [SerializeField] private Material materialAditivoUI;
        [Tooltip("Cuerpo horneado por personaje, en el orden de Apariencia.Cara (0 = mujer, 1 = hombre); 4 submallas en el orden de EspacioImplante.")]
        [SerializeField] private Mesh[] cuerpos = new Mesh[0];
        [Tooltip("T-896: la malla de cada implante de PJ01 por espacio y nivel, horneada en el mismo espacio que el cuerpo.")]
        [SerializeField] private MechaCyberpunk.UI.Clinica.MallaImplanteHolograma[] mallasImplante = new MechaCyberpunk.UI.Clinica.MallaImplanteHolograma[0];
        [Min(0.05f)] [Tooltip("Lo que tarda el barrido en revelar un implante de abajo hacia arriba (s).")]
        [SerializeField] private float duracionRevelar = 0.7f;
        [Min(0.05f)] [Tooltip("Lo que tarda el implante en ensamblarse al instalarlo (s).")]
        [SerializeField] private float duracionEnsamble = 0.9f;
        [Min(0f)] [Tooltip("Qué tan lejos del cuerpo empieza el implante al ensamblarse (m).")]
        [SerializeField] private float distanciaEnsamble = 0.45f;
        [Tooltip("Dónde se arma el set del holograma, lejos de la ciudad (m). Distinto del banco de armas.")]
        [SerializeField] private Vector3 posicionSet = new Vector3(40f, -500f, 0f);
        [Tooltip("Ancho de la RenderTexture (px).")]
        [Min(64)] [SerializeField] private int anchoTextura = 768;
        [Tooltip("Alto de la RenderTexture (px).")]
        [Min(64)] [SerializeField] private int altoTextura = 800;
        [Tooltip("Campo de visión de la cámara del holograma (°).")]
        [Range(10f, 60f)] [SerializeField] private float campoVision = 30f;
        [Tooltip("Altura del cuerpo en el holograma (m).")]
        [Min(0.5f)] [SerializeField] private float alturaCuerpo = 1.8f;
        [Tooltip("Distancia inicial de la cámara (m).")]
        [Min(0.5f)] [SerializeField] private float distancia = 4.1f;
        [Tooltip("Lo más cerca con la rueda (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaMinima = 1.6f;
        [Tooltip("Lo más lejos con la rueda (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaMaxima = 5.5f;
        [Tooltip("Cuánto se acerca cada paso de la rueda (m).")]
        [Min(0f)] [SerializeField] private float pasoRueda = 0.35f;
        [Tooltip("Giro por píxel arrastrado (°/px).")]
        [Min(0f)] [SerializeField] private float sensibilidad = 0.35f;
        [Tooltip("Lo más que se inclina con el arrastre (°).")]
        [Range(0f, 60f)] [SerializeField] private float inclinacionMaxima = 25f;
        [Tooltip("Giro solo cuando no se toca (°/s).")]
        [Min(0f)] [SerializeField] private float giroSolo = 16f;
        [Tooltip("Espera tras soltar antes de volver a girar solo (s).")]
        [Min(0f)] [SerializeField] private float esperaGiroSolo = 2.5f;
        [Tooltip("Cuánto se va la mira de la cámara hacia el espacio elegido (0 = el centro del cuerpo, 1 = el espacio).")]
        [Range(0f, 1f)] [SerializeField] private float enfoqueEspacio = 0.3f;
        [Tooltip("Rapidez con que la cámara sigue al espacio elegido (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezEnfoque = 5f;
        [Tooltip("Pulsos por segundo del espacio elegido (Hz).")]
        [Min(0f)] [SerializeField] private float pulsoEspacio = 1.4f;
        [Tooltip("Lo que dura el destello al instalar; la pantalla se queda así de tiempo antes de irse (s).")]
        [Min(0.05f)] [SerializeField] private float duracionDestello = 0.8f;
        [Tooltip("Cian del holograma.")]
        [SerializeField] private Color colorHolograma = new Color(0.15f, 0.85f, 1f, 1f);
        [Tooltip("Espacio elegido en el cuerpo (naranja).")]
        [SerializeField] private Color colorEspacio = new Color(1f, 0.45f, 0.08f, 1f);
        [Tooltip("Espacio con implante instalado en el cuerpo y mejoras en la tabla (verde).")]
        [SerializeField] private Color colorInstalado = new Color(0.35f, 1f, 0.55f, 1f);
        [Tooltip("Lo que empeora en la tabla (rojo).")]
        [SerializeField] private Color colorPeor = new Color(1f, 0.3f, 0.3f, 1f);
        [Tooltip("Fondo de toda la pantalla (tapa la ciudad).")]
        [SerializeField] private Color colorFondoMenu = new Color(0.012f, 0.02f, 0.04f, 1f);
        [SerializeField] private string subtitulo = "IMPLANTES DEL DOC";
        [SerializeField] private string encabezadoEspacios = "TU CUERPO";
        [SerializeField] private string encabezadoImplante = "IMPLANTE";
        [SerializeField] private string encabezadoEfectos = "ANTES  →  DESPUÉS";
        [SerializeField] private string etiquetaDinero = "TU DINERO";
        [Tooltip("{0} = nivel en romanos (I).")]
        [SerializeField] private string formatoNivel = "NIVEL {0}";
        [SerializeField] private string textoSinImplante = "SIN IMPLANTE";
        [SerializeField] private string textoInstalado = "INSTALADO";
        [Tooltip("{0} = liga.")]
        [SerializeField] private string formatoEtiquetaLiga = "LIGA {0}";
        [Tooltip("{0} = precio con Ƀ.")]
        [SerializeField] private string formatoBoton = "ENTER · INSTALAR  {0}";
        [SerializeField] private string textoInstalando = "INSTALANDO…";
        [SerializeField] private string textoAyudaRaton = "ARRASTRA PARA GIRAR · RUEDA PARA ACERCAR";
        [SerializeField] private string textoSi = "SÍ";
        [SerializeField] private string textoNo = "NO";
        [Tooltip("Nombre de cada efecto, en el orden de EfectoImplante.")]
        [SerializeField] private string[] nombresEfectos =
        {
            "GOLPE A PIE", "CORRER", "SALTO", "ESPERA EN EL MECHA", "VIDA DEL RIVAL", "VIDA EN NÚMEROS", "AVISO DE ATAQUE",
            "ALCANCE AL APUNTAR", "RETROCESO", "ESPERA DE RODADA", "RECARGA"
        };
        [Header("Sonidos de la pantalla (ids de GestorSonidos)")]
        [SerializeField] private string sonidoMover = "UI_MOVER";
        [SerializeField] private string sonidoAceptar = "UI_ACEPTAR";
        [SerializeField] private string sonidoCancelar = "UI_CANCELAR";
        [SerializeField] private string sonidoCompra = "ECO_COMPRA";
        [SerializeField] private string sonidoSinDinero = "ECO_SIN_DINERO";

        public float DuracionRevelar => duracionRevelar;
        public float DuracionEnsamble => duracionEnsamble;
        public float DistanciaEnsamble => distanciaEnsamble;

        /// <summary>La malla horneada del implante (null si ese espacio y nivel no tienen pieza visible).</summary>
        public Mesh MallaImplante(MechaCyberpunk.Core.EspacioImplante espacio, int nivel)
        {
            foreach (MechaCyberpunk.UI.Clinica.MallaImplanteHolograma m in mallasImplante)
            {
                if (m.Espacio == espacio && m.Nivel == nivel)
                {
                    return m.Malla;
                }
            }
            return null;
        }

        public Material MaterialHolograma => materialHolograma;
        public Material MaterialAditivoUI => materialAditivoUI;
        public Vector3 PosicionSet => posicionSet;
        public int AnchoTextura => anchoTextura;
        public int AltoTextura => altoTextura;
        public float CampoVision => campoVision;
        public float AlturaCuerpo => alturaCuerpo;
        public float Distancia => distancia;
        public float DistanciaMinima => distanciaMinima;
        public float DistanciaMaxima => distanciaMaxima;
        public float PasoRueda => pasoRueda;
        public float Sensibilidad => sensibilidad;
        public float InclinacionMaxima => inclinacionMaxima;
        public float GiroSolo => giroSolo;
        public float EsperaGiroSolo => esperaGiroSolo;
        public float EnfoqueEspacio => enfoqueEspacio;
        public float RapidezEnfoque => rapidezEnfoque;
        public float PulsoEspacio => pulsoEspacio;
        public float DuracionDestello => duracionDestello;
        public Color ColorHolograma => colorHolograma;
        public Color ColorEspacio => colorEspacio;
        public Color ColorInstalado => colorInstalado;
        public Color ColorPeor => colorPeor;
        public Color ColorFondoMenu => colorFondoMenu;
        public string Subtitulo => subtitulo;
        public string EncabezadoEspacios => encabezadoEspacios;
        public string EncabezadoImplante => encabezadoImplante;
        public string EncabezadoEfectos => encabezadoEfectos;
        public string EtiquetaDinero => etiquetaDinero;
        public string FormatoNivel => formatoNivel;
        public string TextoSinImplante => textoSinImplante;
        public string TextoInstalado => textoInstalado;
        public string FormatoEtiquetaLiga => formatoEtiquetaLiga;
        public string FormatoBoton => formatoBoton;
        public string TextoInstalando => textoInstalando;
        public string TextoAyudaRaton => textoAyudaRaton;
        public string TextoSi => textoSi;
        public string TextoNo => textoNo;
        public string SonidoMover => sonidoMover;
        public string SonidoAceptar => sonidoAceptar;
        public string SonidoCancelar => sonidoCancelar;
        public string SonidoCompra => sonidoCompra;
        public string SonidoSinDinero => sonidoSinDinero;

        /// <summary>El cuerpo horneado para esa cara (fuera de rango, el primero); null si no hay ninguno.</summary>
        public Mesh Cuerpo(int cara)
        {
            if (cuerpos == null || cuerpos.Length == 0)
            {
                return null;
            }
            return cara >= 0 && cara < cuerpos.Length && cuerpos[cara] != null ? cuerpos[cara] : cuerpos[0];
        }

        /// <summary>Nombre del efecto en la tabla (GOLPE A PIE).</summary>
        public string NombreEfecto(int efecto) => nombresEfectos != null && efecto >= 0 && efecto < nombresEfectos.Length ? nombresEfectos[efecto] : string.Empty;

        /// <summary>Nombre del espacio por su número en <c>EspacioImplante</c>; vacío si no hay.</summary>
        public string NombreEspacio(int espacio) => nombresEspacio != null && espacio >= 0 && espacio < nombresEspacio.Length ? nombresEspacio[espacio] : string.Empty;
    }
}
