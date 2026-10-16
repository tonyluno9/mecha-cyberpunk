using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Sensación de la interacción a pie: qué tan cerca y qué tan de frente hay que estar para usar algo con E.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config interacción a pie", fileName = "ConfigInteraccionAPie_Nuevo")]
    public class ConfigInteraccionAPie : ScriptableObject
    {
        [Tooltip("Distancia máxima a lo interactuable, medida en el plano horizontal desde los pies (m).")]
        [Min(0.1f)] [SerializeField] private float alcance = 2.5f;

        [Tooltip("Ángulo máximo entre hacia dónde mira la cámara y lo interactuable (°).")]
        [Range(1f, 180f)] [SerializeField] private float anguloMaximo = 60f;

        public float Alcance => alcance;
        public float AnguloMaximo => anguloMaximo;
    }
}
