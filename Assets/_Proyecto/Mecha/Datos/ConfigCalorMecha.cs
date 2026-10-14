using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Cuánto calor policial da usar el mecha en la calle (D-tony-31).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config calor del mecha", fileName = "ConfigCalorMecha_Nueva")]
    public class ConfigCalorMecha : ScriptableObject
    {
        [Tooltip("Calor que suma llamar al mecha cuando llega (puntos).")]
        [Min(0f)] [SerializeField] private float calorAlLlamar = 10f;

        [Tooltip("Calor por segundo mientras el jugador maneja el mecha en la calle (puntos/s).")]
        [Min(0f)] [SerializeField] private float calorPorSegundo = 2f;

        public float CalorAlLlamar => calorAlLlamar;
        public float CalorPorSegundo => calorPorSegundo;
    }
}
