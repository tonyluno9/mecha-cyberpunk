using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Sensación de los controles a pie: ratón, aceleración, gravedad, altura de los ojos y cabeceo.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config controles a pie", fileName = "ConfigControlesAPie_Nuevo")]
    public class ConfigControlesAPie : ScriptableObject
    {
        [Tooltip("Grados que gira la vista por cada unidad de delta del ratón (°/unidad).")]
        [Min(0.001f)] [SerializeField] private float sensibilidadRaton = 0.1f;

        [Tooltip("Límite de mirar arriba y abajo (° desde el horizonte).")]
        [Range(1f, 89f)] [SerializeField] private float limiteMirarVertical = 85f;

        [Tooltip("Aceleración en el piso al pedir movimiento (m/s²).")]
        [Min(0.1f)] [SerializeField] private float aceleracionPiso = 30f;

        [Tooltip("Frenado en el piso al soltar el movimiento (m/s²).")]
        [Min(0.1f)] [SerializeField] private float frenadoPiso = 40f;

        [Tooltip("Qué tanto se controla el movimiento en el aire (0 nada, 1 igual que en el piso).")]
        [Range(0f, 1f)] [SerializeField] private float controlAire = 0.3f;

        [Tooltip("Gravedad del jugador a pie (m/s²).")]
        [Min(0.1f)] [SerializeField] private float gravedad = 20f;

        [Tooltip("Altura de los ojos sobre los pies (m).")]
        [Min(0.1f)] [SerializeField] private float alturaOjos = 1.65f;

        [Tooltip("Altura del vaivén de la cámara al caminar (m).")]
        [Min(0f)] [SerializeField] private float amplitudCabeceo = 0.04f;

        [Tooltip("Pasos por segundo al caminar (pasos/s).")]
        [Min(0.1f)] [SerializeField] private float frecuenciaCabeceo = 1.8f;

        public float SensibilidadRaton => sensibilidadRaton;
        public float LimiteMirarVertical => limiteMirarVertical;
        public float AceleracionPiso => aceleracionPiso;
        public float FrenadoPiso => frenadoPiso;
        public float ControlAire => controlAire;
        public float Gravedad => gravedad;
        public float AlturaOjos => alturaOjos;
        public float AmplitudCabeceo => amplitudCabeceo;
        public float FrecuenciaCabeceo => frecuenciaCabeceo;
    }
}
