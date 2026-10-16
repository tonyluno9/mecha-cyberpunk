using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Sensación de la cámara libre de pruebas de Alan: velocidades, sensibilidad y gravedad.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config cámara libre", fileName = "ConfigCamaraLibre_Nuevo")]
    public class ConfigCamaraLibre : ScriptableObject
    {
        [Header("Velocidades")]
        [Tooltip("Velocidad caminando (m/s).")]
        [SerializeField, Min(0f)] private float velocidadAPie = 4.5f;

        [Tooltip("Velocidad corriendo con Shift (m/s).")]
        [SerializeField, Min(0f)] private float velocidadCorriendo = 8f;

        [Tooltip("Velocidad volando, sin colisiones (m/s).")]
        [SerializeField, Min(0f)] private float velocidadVolando = 20f;

        [Header("Mirar")]
        [Tooltip("Grados que gira la vista por cada pixel que se mueve el ratón (°/px).")]
        [SerializeField, Min(0f)] private float sensibilidad = 0.1f;

        [Tooltip("Inclinación máxima de la vista hacia arriba o hacia abajo (°).")]
        [SerializeField, Range(1f, 89f)] private float limiteInclinacion = 85f;

        [Header("Caída")]
        [Tooltip("Aceleración de caída al caminar (m/s²).")]
        [SerializeField, Min(0f)] private float gravedad = 20f;

        [Header("Interactuar")]
        [Tooltip("Alcance del rayo de la tecla E al frente (m).")]
        [SerializeField, Min(0.1f)] private float distanciaInteraccion = 3f;

        public float DistanciaInteraccion => distanciaInteraccion;
        public float VelocidadAPie => velocidadAPie;
        public float VelocidadCorriendo => velocidadCorriendo;
        public float VelocidadVolando => velocidadVolando;
        public float Sensibilidad => sensibilidad;
        public float LimiteInclinacion => limiteInclinacion;
        public float Gravedad => gravedad;
    }
}
