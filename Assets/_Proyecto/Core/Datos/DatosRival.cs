using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Estadísticas y comportamiento básico de un rival. El nombre del asset (<c>name</c>) es la llave con la que el rival recuerda
    /// al jugador entre peleas (IA, <c>AdaptacionRival</c>, T-200): renombrar el asset de un rival que se adapta le borra lo aprendido
    /// en las partidas guardadas.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del rival", fileName = "DatosRival_Nuevo")]
    public class DatosRival : ScriptableObject
    {
        /// <summary>Si la distancia mínima queda mal, se corrige a esta fracción de la de ataque.</summary>
        private const float FraccionDistanciaMinima = 0.9f;

        [Tooltip("Nombre del rival (texto).")]
        [SerializeField] private string alias = "Rival";

        [Tooltip("Las estadísticas de su mecha.")]
        [SerializeField] private DatosMecha mecha;

        [Tooltip("A esta distancia o menos decide golpear (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaAtaque = 2.6f;

        [Tooltip("No se acerca más que esto (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaMinima = 2f;

        [Tooltip("Espera mínima entre golpes (s).")]
        [Min(0.01f)] [SerializeField] private float esperaEntreAtaquesMin = 1.2f;

        [Tooltip("Espera máxima entre golpes (s).")]
        [Min(0.01f)] [SerializeField] private float esperaEntreAtaquesMax = 2.5f;

        public string Alias => alias;
        public DatosMecha Mecha => mecha;
        public float DistanciaAtaque => distanciaAtaque;
        public float DistanciaMinima => distanciaMinima;
        public float EsperaEntreAtaquesMin => esperaEntreAtaquesMin;
        public float EsperaEntreAtaquesMax => esperaEntreAtaquesMax;

        private void OnValidate()
        {
            if (esperaEntreAtaquesMax < esperaEntreAtaquesMin)
                esperaEntreAtaquesMax = esperaEntreAtaquesMin;

            if (distanciaMinima >= distanciaAtaque)
                distanciaMinima = distanciaAtaque * FraccionDistanciaMinima;

            if (mecha != null && distanciaAtaque > mecha.AlcanceGolpe)
                Debug.LogWarning($"[IA] {name}: distanciaAtaque ({distanciaAtaque} m) es mayor que el alcance de golpe de su mecha ({mecha.AlcanceGolpe} m).", this);
        }
    }
}
