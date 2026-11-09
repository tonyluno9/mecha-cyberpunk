using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>T-910: duraciones del ciclo de los semáforos de las esquinas (verde, ámbar y rojo) y el brillo de la luz encendida.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config semáforo", fileName = "ConfigSemaforo_Nuevo")]
    public class ConfigSemaforo : ScriptableObject
    {
        [Tooltip("Tiempo en verde (s).")]
        [SerializeField, Min(0.5f)] private float segundosVerde = 8f;

        [Tooltip("Tiempo en ámbar (s).")]
        [SerializeField, Min(0.5f)] private float segundosAmbar = 2f;

        [Tooltip("Intensidad HDR de la emisión de la luz encendida.")]
        [SerializeField, Min(0f)] private float intensidadEmision = 3f;

        [Tooltip("Cuánto se apaga la luz que no toca (fracción del color).")]
        [SerializeField, Range(0f, 1f)] private float nivelApagada = 0.12f;

        public float SegundosVerde => segundosVerde;
        public float SegundosAmbar => segundosAmbar;
        /// <summary>El rojo dura verde + ámbar: así el semáforo de la calle cruzada, a medio ciclo de diferencia, nunca coincide en verde.</summary>
        public float SegundosRojo => segundosVerde + segundosAmbar;
        public float Ciclo => segundosVerde + segundosAmbar + SegundosRojo;
        public float IntensidadEmision => intensidadEmision;
        public float NivelApagada => nivelApagada;
    }
}
