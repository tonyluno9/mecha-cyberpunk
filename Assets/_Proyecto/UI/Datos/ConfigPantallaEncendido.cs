using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Colores y tiempos de la pantalla del encendido de la cabina.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de encendido", fileName = "ConfigPantallaEncendido_Nueva")]
    public class ConfigPantallaEncendido : ScriptableObject
    {
        [Tooltip("Color del texto y la barra.")]
        [SerializeField] private Color colorNormal = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color del texto al pulsar una tecla equivocada.")]
        [SerializeField] private Color colorError = new Color(1f, 0.15f, 0.2f, 1f);

        [Tooltip("Color del fondo de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.04f, 0.06f, 0.92f);

        [Tooltip("Color del hueco de la barra.")]
        [SerializeField] private Color colorHueco = new Color(0.1f, 0.15f, 0.18f, 1f);

        [Tooltip("Cuánto dura el parpadeo rojo tras una tecla equivocada (s).")]
        [Min(0f)] [SerializeField] private float segundosParpadeo = 0.5f;

        [Tooltip("Cambios de color por segundo durante el parpadeo.")]
        [Min(1f)] [SerializeField] private float cambiosPorSegundo = 10f;

        [Tooltip("Cuánto se muestra SISTEMAS EN LÍNEA al terminar el encendido (s).")]
        [Min(0f)] [SerializeField] private float segundosEnLinea = 1f;

        public Color ColorNormal => colorNormal;
        public Color ColorError => colorError;
        public Color ColorFondo => colorFondo;
        public Color ColorHueco => colorHueco;
        public float SegundosParpadeo => segundosParpadeo;
        public float CambiosPorSegundo => cambiosPorSegundo;
        public float SegundosEnLinea => segundosEnLinea;
    }
}
