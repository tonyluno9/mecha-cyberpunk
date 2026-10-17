using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Valores de los kits de reparación en plena pelea (T-148, D-tony-86).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config kit de reparación", fileName = "ConfigKitReparacion_Nueva")]
    public class ConfigKitReparacion : ScriptableObject
    {
        [Tooltip("Parte de la vida máxima que cura un kit (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float fraccionCuracion = 0.2f;

        [Tooltip("Cuánto tarda en repararse (s).")]
        [Min(0.1f)] [SerializeField] private float segundosCuracion = 1.5f;

        [Tooltip("Cuántos kits se pueden usar en una misma pelea (entero).")]
        [Min(1)] [SerializeField] private int topePorPelea = 2;

        public float FraccionCuracion => fraccionCuracion;
        public float SegundosCuracion => segundosCuracion;
        public int TopePorPelea => topePorPelea;
    }
}
