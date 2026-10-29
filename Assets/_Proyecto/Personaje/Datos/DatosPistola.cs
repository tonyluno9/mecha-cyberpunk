using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Estadísticas de la pistola a pie. Se podrá mejorar con implantes y compras.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de la pistola", fileName = "DatosPistola_Nuevo")]
    public class DatosPistola : ScriptableObject
    {
        [Tooltip("Daño de cada bala (puntos).")]
        [Min(0f)] [SerializeField] private float danio = 25f;

        [Tooltip("Distancia máxima del tiro desde la cámara (m).")]
        [Min(1f)] [SerializeField] private float alcance = 60f;

        [Tooltip("Tiempo mínimo entre un disparo y el siguiente (s).")]
        [Min(0.01f)] [SerializeField] private float tiempoEntreDisparos = 0.25f;

        [Tooltip("Balas que caben en el cargador. En esta fase la reserva es infinita.")]
        [Min(1)] [SerializeField] private int balasPorCargador = 8;

        [Tooltip("Tiempo que tarda la recarga (s).")]
        [Min(0.01f)] [SerializeField] private float tiempoRecarga = 1.4f;

        [Tooltip("Calor policial que suma cada disparo en la calle (puntos de calor). Dentro de un interior no suma.")]
        [Min(0f)] [SerializeField] private float calorPorDisparo = 2f;

        [Tooltip("Cada cuántos segundos como máximo un disparo suma calor (s).")]
        [Min(0f)] [SerializeField] private float segundosEntreCalor = 0.5f;

        public float CalorPorDisparo => calorPorDisparo;
        public float SegundosEntreCalor => segundosEntreCalor;
        public float Danio => danio;
        public float Alcance => alcance;
        public float TiempoEntreDisparos => tiempoEntreDisparos;
        public int BalasPorCargador => balasPorCargador;
        public float TiempoRecarga => tiempoRecarga;
    }
}
