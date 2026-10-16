using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Sensación de las torretas de las patrullas corporativas: ritmo del parpadeo y fuerza de sus luces.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config torreta", fileName = "ConfigTorreta_Nuevo")]
    public class ConfigTorreta : ScriptableObject
    {
        [Tooltip("Tiempo que dura cada color antes de pasar al otro (s).")]
        [SerializeField, Min(0.05f)] private float periodo = 0.35f;

        [Tooltip("Intensidad de la luz puntual del color prendido (candelas de URP).")]
        [SerializeField, Min(0f)] private float intensidadLuz = 18f;

        [Tooltip("Alcance de la luz puntual (m).")]
        [SerializeField, Min(0.1f)] private float rangoLuz = 10f;

        [Tooltip("Cuánto brillo emisivo conserva el color apagado, de 0 (nada) a 1 (igual que prendido).")]
        [SerializeField, Range(0f, 1f)] private float brilloApagado = 0.08f;

        public float Periodo => periodo;
        public float IntensidadLuz => intensidadLuz;
        public float RangoLuz => rangoLuz;
        public float BrilloApagado => brilloApagado;
    }
}
