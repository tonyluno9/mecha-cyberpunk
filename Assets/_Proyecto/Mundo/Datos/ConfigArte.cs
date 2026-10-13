using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Ajustes del arte importado de Blender: cuánto brilla el neón y qué tan transparente es el vidrio.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config arte", fileName = "ConfigArte_Nuevo")]
    public class ConfigArte : ScriptableObject
    {
        [Tooltip("Multiplica color × fuerza de emisión del JSON de Blender. Con 1 el neón sale como en Blender; más alto, más bloom.")]
        [SerializeField, Min(0f)] private float factorEmision = 1f;

        [Tooltip("Opacidad de los materiales Vidrio* cuando Blender los trae opacos (0 = invisible, 1 = opaco).")]
        [SerializeField, Range(0.05f, 1f)] private float opacidadVidrio = 0.35f;

        public float FactorEmision => factorEmision;
        public float OpacidadVidrio => opacidadVidrio;
    }
}
