using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Tiempos, colores y textos del fundido a negro y de la pantalla de "continuará" (T-125).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantallas de historia", fileName = "ConfigPantallasHistoria_Nueva")]
    public class ConfigPantallasHistoria : ScriptableObject
    {
        [Tooltip("Duración por defecto del fundido hacia negro (s).")]
        [Min(0f)] [SerializeField] private float segundosSalida = 0.6f;

        [Tooltip("Duración por defecto del fundido de vuelta a transparente (s).")]
        [Min(0f)] [SerializeField] private float segundosEntrada = 0.6f;

        [Tooltip("Segundos que pasan antes de que las teclas de la pantalla de continuará funcionen.")]
        [Min(0f)] [SerializeField] private float segundosMinimos = 4f;

        [Tooltip("Color del título CONTINUARÁ (neón).")]
        [SerializeField] private Color colorTitulo = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color del resumen y de las teclas.")]
        [SerializeField] private Color colorTexto = new Color(0.9f, 0.95f, 1f, 1f);

        [Tooltip("Tamaño de letra del título.")]
        [Min(8)] [SerializeField] private int tamanoTitulo = 120;

        [Tooltip("Tamaño de letra del resumen y de las teclas.")]
        [Min(8)] [SerializeField] private int tamanoTexto = 40;

        [SerializeField] private string textoTitulo = "CONTINUARÁ…";
        [SerializeField] private string textoGano = "Ganaste tu primera pelea casera";
        [SerializeField] private string textoPerdio = "Perdiste tu primera pelea casera, vuelve cuando lo repares";
        [SerializeField] private string textoTeclas = "ENTER · seguir jugando   ESC · salir al título";

        public float SegundosSalida => segundosSalida;
        public float SegundosEntrada => segundosEntrada;
        public float SegundosMinimos => segundosMinimos;
        public Color ColorTitulo => colorTitulo;
        public Color ColorTexto => colorTexto;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoTexto => tamanoTexto;
        public string TextoTitulo => textoTitulo;
        public string TextoGano => textoGano;
        public string TextoPerdio => textoPerdio;
        public string TextoTeclas => textoTeclas;
    }
}
