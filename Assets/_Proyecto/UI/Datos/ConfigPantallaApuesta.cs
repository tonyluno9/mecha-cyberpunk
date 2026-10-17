using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores y tamaños de la pantalla de apuesta del mostrador de peleas (T-145).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de apuesta", fileName = "ConfigPantallaApuesta_Nueva")]
    public class ConfigPantallaApuesta : ScriptableObject
    {
        [Tooltip("Color de la fila elegida (neón).")]
        [SerializeField] private Color colorElegido = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de lo que no está elegido.")]
        [SerializeField] private Color colorNormal = new Color(0.8f, 0.85f, 0.95f, 1f);

        [Tooltip("Color de los montos que el jugador no puede pagar.")]
        [SerializeField] private Color colorApagado = new Color(0.35f, 0.38f, 0.45f, 1f);

        [Tooltip("Color del premio y de la pieza en juego.")]
        [SerializeField] private Color colorPremio = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Color del aviso (no alcanza).")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.3f, 0.55f, 1f);

        [Tooltip("Fondo semitransparente de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.02f, 0.06f, 0.82f);

        [Tooltip("Tamaño de letra del título (puntos).")]
        [Min(8)] [SerializeField] private int tamanoTitulo = 90;
        [Tooltip("Tamaño de letra del rival, premio, momio y aviso (puntos).")]
        [Min(8)] [SerializeField] private int tamanoTexto = 48;
        [Tooltip("Tamaño de letra de la fila de montos (puntos).")]
        [Min(8)] [SerializeField] private int tamanoMontos = 56;
        [Tooltip("Tamaño de letra de la ayuda (puntos).")]
        [Min(8)] [SerializeField] private int tamanoAyuda = 28;

        [SerializeField] private string textoTitulo = "PELEA CASERA";
        [Tooltip("{0} = alias del rival.")]
        [SerializeField] private string formatoRival = "CONTRA {0}";
        [Tooltip("{0} = premio de ganar (con $).")]
        [SerializeField] private string formatoPremio = "PREMIO {0}";
        [Tooltip("{0} = momio (1 = paga uno a uno).")]
        [SerializeField] private string formatoMomio = "PAGA {0} A 1";
        [SerializeField] private string textoSinApuesta = "SIN APUESTA";
        [Tooltip("{0} = monto con $.")]
        [SerializeField] private string formatoMonto = "{0}";
        [Tooltip("{0} = opción elegida (monto con $ o SIN APUESTA).")]
        [SerializeField] private string formatoElegido = "[ {0} ]";
        [Tooltip("{0} = nombre de la pieza en juego, {1} = apuesta mínima con $.")]
        [SerializeField] private string formatoPieza = "+ SU {0} si apuestas {1} o más";
        [SerializeField] private string textoAyuda = "← → apuesta · Enter pelear · Esc salir";
        [SerializeField] private string textoNoAlcanza = "NO ALCANZA PARA ESA APUESTA";

        public Color ColorElegido => colorElegido;
        public Color ColorNormal => colorNormal;
        public Color ColorApagado => colorApagado;
        public Color ColorPremio => colorPremio;
        public Color ColorAviso => colorAviso;
        public Color ColorFondo => colorFondo;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoTexto => tamanoTexto;
        public int TamanoMontos => tamanoMontos;
        public int TamanoAyuda => tamanoAyuda;
        public string TextoTitulo => textoTitulo;
        public string FormatoRival => formatoRival;
        public string FormatoPremio => formatoPremio;
        public string FormatoMomio => formatoMomio;
        public string TextoSinApuesta => textoSinApuesta;
        public string FormatoMonto => formatoMonto;
        public string FormatoElegido => formatoElegido;
        public string FormatoPieza => formatoPieza;
        public string TextoAyuda => textoAyuda;
        public string TextoNoAlcanza => textoNoAlcanza;
    }
}
