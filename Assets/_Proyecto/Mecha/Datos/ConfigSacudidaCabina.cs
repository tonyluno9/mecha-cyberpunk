using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Sensación de la sacudida de la cabina al recibir un impacto.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config sacudida de cabina", fileName = "ConfigSacudidaCabina_Nuevo")]
    public class ConfigSacudidaCabina : ScriptableObject
    {
        [Tooltip("Daño que da la sacudida máxima; con más daño no sacude más (puntos).")]
        [Min(0.1f)] [SerializeField] private float danioReferencia = 30f;

        [Tooltip("Intensidad de un impacto con 0 de daño, por ejemplo un bloqueo perfecto (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float intensidadMinima = 0.15f;

        [Tooltip("Intensidad con el daño de referencia o más (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float intensidadMaxima = 1f;

        [Tooltip("Tiempo que tarda la sacudida máxima en apagarse (s).")]
        [Min(0.05f)] [SerializeField] private float duracion = 0.5f;

        [Tooltip("Desplazamiento de la cabina con intensidad 1 (m).")]
        [Min(0f)] [SerializeField] private float amplitudPosicion = 0.06f;

        [Tooltip("Giro de la cabina con intensidad 1 (°).")]
        [Min(0f)] [SerializeField] private float amplitudRotacion = 2.5f;

        [Tooltip("Vibraciones por segundo (Hz).")]
        [Min(1f)] [SerializeField] private float frecuencia = 28f;

        public float DanioReferencia => danioReferencia;
        public float IntensidadMinima => intensidadMinima;
        public float IntensidadMaxima => intensidadMaxima;
        public float Duracion => duracion;
        public float AmplitudPosicion => amplitudPosicion;
        public float AmplitudRotacion => amplitudRotacion;
        public float Frecuencia => frecuencia;
    }
}
