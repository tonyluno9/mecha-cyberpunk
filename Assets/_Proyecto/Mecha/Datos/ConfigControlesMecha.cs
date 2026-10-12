using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Sensación de los controles del mecha: ratón, límite de mirada y cabeceo al caminar.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config controles del mecha", fileName = "ConfigControlesMecha_Nuevo")]
    public class ConfigControlesMecha : ScriptableObject
    {
        [Tooltip("Grados que gira la vista por cada píxel que se mueve el ratón (°/píxel).")]
        [Min(0.001f)] [SerializeField] private float sensibilidadRaton = 0.1f;

        [Tooltip("Límite de mirar arriba y abajo (° desde el horizonte).")]
        [Range(1f, 89f)] [SerializeField] private float limiteMirarVertical = 35f;

        [Tooltip("Altura del vaivén de la cámara al caminar a toda velocidad (m).")]
        [Min(0f)] [SerializeField] private float amplitudCabeceo = 0.08f;

        [Tooltip("Pasos por segundo al caminar a toda velocidad (pasos/s).")]
        [Min(0.1f)] [SerializeField] private float pasosPorSegundo = 1.6f;

        public float SensibilidadRaton => sensibilidadRaton;
        public float LimiteMirarVertical => limiteMirarVertical;
        public float AmplitudCabeceo => amplitudCabeceo;
        public float PasosPorSegundo => pasosPorSegundo;
    }
}
