using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Colores de la pantalla de vida de la cabina.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de vida", fileName = "ConfigPantallaVida_Nueva")]
    public class ConfigPantallaVida : ScriptableObject
    {
        [Tooltip("Color de la barra y el texto con vida suficiente.")]
        [SerializeField] private Color colorNormal = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de la barra y el texto con vida baja.")]
        [SerializeField] private Color colorBajo = new Color(1f, 0.15f, 0.2f, 1f);

        [Tooltip("Color del fondo de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.04f, 0.06f, 1f);

        [Tooltip("Color del hueco de la barra.")]
        [SerializeField] private Color colorHueco = new Color(0.1f, 0.15f, 0.18f, 1f);

        [Tooltip("Debajo de esta parte de la vida máxima se usa el color bajo (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float umbralVidaBaja = 0.3f;

        public Color ColorNormal => colorNormal;
        public Color ColorBajo => colorBajo;
        public Color ColorFondo => colorFondo;
        public Color ColorHueco => colorHueco;
        public float UmbralVidaBaja => umbralVidaBaja;
    }
}
