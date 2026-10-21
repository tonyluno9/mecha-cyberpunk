using MechaCyberpunk.Mecha;
using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Tiempos y colores de la pantalla de piezas de la cabina durante el encendido (T-170).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de piezas de la cabina", fileName = "ConfigPantallaPiezasCabina_Nueva")]
    public class ConfigPantallaPiezasCabina : ScriptableObject
    {
        [Tooltip("Cuánto sigue encendida después de terminar el encendido (s).")]
        [Min(0f)] [SerializeField] private float segundosDespues = 5f;

        [SerializeField] private Color colorDeSerie = new Color(0.7f, 0.72f, 0.76f, 1f);
        [SerializeField] private Color colorMilitar = new Color(0.45f, 0.65f, 0.9f, 1f);
        [SerializeField] private Color colorTech = new Color(0.1f, 1f, 0.9f, 1f);
        [SerializeField] private Color colorCuerpoACuerpo = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color colorFuego = new Color(1f, 0.2f, 0.75f, 1f);
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.04f, 0.06f, 1f);

        public float SegundosDespues => segundosDespues;
        public Color ColorFondo => colorFondo;

        /// <summary>El color del nombre de una pieza de esa rama.</summary>
        public Color ColorDeRama(RamaPieza rama)
        {
            switch (rama)
            {
                case RamaPieza.Militar: return colorMilitar;
                case RamaPieza.Tech: return colorTech;
                case RamaPieza.CuerpoACuerpo: return colorCuerpoACuerpo;
                case RamaPieza.Fuego: return colorFuego;
                default: return colorDeSerie;
            }
        }
    }
}
