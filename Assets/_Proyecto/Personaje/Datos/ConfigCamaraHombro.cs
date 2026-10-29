using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Cámara de hombro del jugador a pie (T-400): posición normal y apuntando, FOV, inclinación y suavizado.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config cámara de hombro", fileName = "ConfigCamaraHombro_Nuevo")]
    public class ConfigCamaraHombro : ScriptableObject
    {
        [Header("Normal")]
        [Tooltip("Desplazamiento a la derecha del hombro (m).")]
        [SerializeField] private float lateralNormal = 0.55f;
        [Tooltip("Altura del punto de giro sobre los pies (m).")]
        [Min(0.1f)] [SerializeField] private float alturaNormal = 1.55f;
        [Tooltip("Distancia hacia atrás (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaNormal = 3.2f;
        [Tooltip("Campo de visión (°).")]
        [Range(20f, 120f)] [SerializeField] private float fovNormal = 60f;

        [Header("Apuntando")]
        [SerializeField] private float lateralApuntando = 0.7f;
        [Min(0.1f)] [SerializeField] private float alturaApuntando = 1.5f;
        [Min(0.1f)] [SerializeField] private float distanciaApuntando = 1.6f;
        [Range(20f, 120f)] [SerializeField] private float fovApuntando = 48f;

        [Header("Mirada y choque")]
        [Tooltip("Inclinación mínima (°, negativo mira hacia arriba).")]
        [Range(-89f, 0f)] [SerializeField] private float inclinacionMinima = -40f;
        [Tooltip("Inclinación máxima (°, positivo mira hacia abajo).")]
        [Range(0f, 89f)] [SerializeField] private float inclinacionMaxima = 60f;
        [Tooltip("Rapidez del suavizado de la cámara (1/s).")]
        [Min(0.1f)] [SerializeField] private float suavizado = 12f;
        [Tooltip("Radio de la esfera que evita que la cámara atraviese muros (m).")]
        [Min(0.01f)] [SerializeField] private float radioColision = 0.2f;
        [Tooltip("Giro del cuerpo hacia donde camina (°/s).")]
        [Min(1f)] [SerializeField] private float giroCuerpo = 720f;

        public float LateralNormal => lateralNormal;
        public float AlturaNormal => alturaNormal;
        public float DistanciaNormal => distanciaNormal;
        public float FovNormal => fovNormal;
        public float LateralApuntando => lateralApuntando;
        public float AlturaApuntando => alturaApuntando;
        public float DistanciaApuntando => distanciaApuntando;
        public float FovApuntando => fovApuntando;
        public float InclinacionMinima => inclinacionMinima;
        public float InclinacionMaxima => inclinacionMaxima;
        public float Suavizado => suavizado;
        public float RadioColision => radioColision;
        public float GiroCuerpo => giroCuerpo;
    }
}
