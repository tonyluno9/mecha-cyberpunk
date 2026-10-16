using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Sensación del seguimiento del objetivo: cuándo se considera que el jugador llegó o se aleja (D-tony-21).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config de seguimiento de objetivo", fileName = "ConfigSeguimientoObjetivo_Nuevo")]
    public class ConfigSeguimientoObjetivo : ScriptableObject
    {
        [Tooltip("Distancia horizontal al objetivo a la que se cuenta como llegada (m).")]
        [Min(0.1f)] [SerializeField] private float radioLlegada = 4f;

        [Tooltip("Cuánto más lejos que la mejor distancia alcanzada hay que estar para avisar que se aleja (m).")]
        [Min(0f)] [SerializeField] private float margenAlejarse = 25f;

        [Tooltip("Cuánto más lejos que la mejor distancia alcanzada se deja de avisar que se aleja (m). Menor que el margen para alejarse.")]
        [Min(0f)] [SerializeField] private float margenVolver = 10f;

        [Tooltip("Tiempo seguido pasado el margen antes de avisar que se aleja (s).")]
        [Min(0f)] [SerializeField] private float tiempoMinimoAlejandose = 1.5f;

        public float RadioLlegada => radioLlegada;
        public float MargenAlejarse => margenAlejarse;
        public float MargenVolver => margenVolver;
        public float TiempoMinimoAlejandose => tiempoMinimoAlejandose;

        private void OnValidate()
        {
            if (margenVolver > margenAlejarse) margenVolver = margenAlejarse;
        }
    }
}
