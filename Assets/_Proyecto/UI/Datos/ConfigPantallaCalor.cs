using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Colores y texto de la pantalla de calor policial de la cabina (T-067).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de calor", fileName = "ConfigPantallaCalor_Nueva")]
    public class ConfigPantallaCalor : ScriptableObject
    {
        [Tooltip("Título de la pantalla.")]
        [SerializeField] private string titulo = "CALOR";

        [Tooltip("Color del título y de la barra sin calor (nivel 0).")]
        [SerializeField] private Color colorNivel0 = new Color(0.25f, 0.4f, 0.45f, 1f);

        [Tooltip("Color de marcas, título y barra en el nivel 1 (naranja).")]
        [SerializeField] private Color colorNivel1 = new Color(1f, 0.6f, 0.1f, 1f);

        [Tooltip("Color en el nivel 2 (naranja rojizo).")]
        [SerializeField] private Color colorNivel2 = new Color(1f, 0.35f, 0.1f, 1f);

        [Tooltip("Color en el nivel 3 (rojo).")]
        [SerializeField] private Color colorNivel3 = new Color(1f, 0.1f, 0.15f, 1f);

        [Tooltip("Color de las marcas que todavía no se llenan.")]
        [SerializeField] private Color colorMarcaApagada = new Color(0.1f, 0.15f, 0.18f, 1f);

        [Tooltip("Color del fondo de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.04f, 0.06f, 1f);

        [Tooltip("Color del hueco de la barra.")]
        [SerializeField] private Color colorHueco = new Color(0.1f, 0.15f, 0.18f, 1f);

        public string Titulo => titulo;
        public Color ColorMarcaApagada => colorMarcaApagada;
        public Color ColorFondo => colorFondo;
        public Color ColorHueco => colorHueco;

        /// <summary>Color del nivel 0 a 3 (los valores fuera de rango se recortan).</summary>
        public Color ColorNivel(int nivel)
        {
            switch (Mathf.Clamp(nivel, 0, 3))
            {
                case 1: return colorNivel1;
                case 2: return colorNivel2;
                case 3: return colorNivel3;
                default: return colorNivel0;
            }
        }
    }
}
