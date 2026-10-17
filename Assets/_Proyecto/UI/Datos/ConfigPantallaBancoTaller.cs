using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores y tamaños de la pantalla del banco del taller (T-149).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla banco del taller", fileName = "ConfigPantallaBancoTaller_Nueva")]
    public class ConfigPantallaBancoTaller : ScriptableObject
    {
        [Tooltip("Color de la opción elegida (neón).")]
        [SerializeField] private Color colorElegido = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de las opciones no elegidas.")]
        [SerializeField] private Color colorNormal = new Color(0.8f, 0.85f, 0.95f, 1f);

        [Tooltip("Color del dinero positivo.")]
        [SerializeField] private Color colorDinero = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Color del dinero negativo (deuda).")]
        [SerializeField] private Color colorDeuda = new Color(1f, 0.25f, 0.3f, 1f);

        [Tooltip("Fondo semitransparente de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.02f, 0.06f, 0.82f);

        [Tooltip("Tamaño de letra del título (puntos).")]
        [Min(8)] [SerializeField] private int tamanoTitulo = 90;
        [Tooltip("Tamaño de letra de la vida y el dinero (puntos).")]
        [Min(8)] [SerializeField] private int tamanoTexto = 48;
        [Tooltip("Tamaño de letra de las opciones (puntos).")]
        [Min(8)] [SerializeField] private int tamanoOpciones = 56;
        [Tooltip("Tamaño de letra de la ayuda (puntos).")]
        [Min(8)] [SerializeField] private int tamanoAyuda = 28;

        [SerializeField] private string textoTitulo = "BANCO DEL TALLER";
        [Tooltip("{0} = vida actual, {1} = vida máxima (enteros).")]
        [SerializeField] private string formatoVida = "VIDA {0} / {1}";
        [Tooltip("{0} = dinero con $ (negativo: -$150).")]
        [SerializeField] private string formatoDinero = "DINERO {0}";
        [Tooltip("{0} = texto de la opción elegida.")]
        [SerializeField] private string formatoElegido = "[ {0} ]";
        [SerializeField] private string textoAyuda = "↑ ↓ elegir · Enter reparar · Esc salir";

        public Color ColorElegido => colorElegido;
        public Color ColorNormal => colorNormal;
        public Color ColorDinero => colorDinero;
        public Color ColorDeuda => colorDeuda;
        public Color ColorFondo => colorFondo;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoTexto => tamanoTexto;
        public int TamanoOpciones => tamanoOpciones;
        public int TamanoAyuda => tamanoAyuda;
        public string TextoTitulo => textoTitulo;
        public string FormatoVida => formatoVida;
        public string FormatoDinero => formatoDinero;
        public string FormatoElegido => formatoElegido;
        public string TextoAyuda => textoAyuda;
    }
}
