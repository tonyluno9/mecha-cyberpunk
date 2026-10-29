using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>Los números de la persecución de drones y patrullas (T-340).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config de persecución", fileName = "ConfigPersecucion_Nueva")]
    public class ConfigPersecucion : ScriptableObject
    {
        [Tooltip("Drones en los niveles de calor 1, 2 y 3 (el primer elemento es el nivel 1).")]
        [SerializeField] private List<int> dronesPorNivel = new List<int> { 1, 2, 2 };
        [Tooltip("Patrullas en los mismos niveles.")]
        [SerializeField] private List<int> patrullasPorNivel = new List<int> { 0, 0, 1 };
        [Min(0.1f)] [Tooltip("Velocidad del dron (m/s).")]
        [SerializeField] private float velocidadDron = 9f;
        [Min(0.1f)] [Tooltip("Velocidad de la patrulla (m/s).")]
        [SerializeField] private float velocidadPatrulla = 12f;
        [Min(0f)] [Tooltip("Distancia a la que un perseguidor se queda del jugador (m).")]
        [SerializeField] private float distanciaSeguimiento = 6f;
        [Min(0f)] [Tooltip("Distancia a la que alcanza al jugador (m); menor que la de seguimiento.")]
        [SerializeField] private float distanciaCaptura = 3f;
        [Min(1f)] [Tooltip("Altura del dron sobre el jugador (m).")]
        [SerializeField] private float alturaDron = 7f;
        [Min(1f)] [Tooltip("A qué distancia del jugador aparece un dron (m).")]
        [SerializeField] private float distanciaAparicionDron = 40f;
        [Min(0.05f)] [Tooltip("Cada cuánto cambian de color las luces de la patrulla (s).")]
        [SerializeField] private float periodoSirena = 0.4f;

        [Header("Disparos del dron (T-402)")]
        [Min(0)] [Tooltip("Desde qué nivel de calor los drones disparan al jugador a pie.")]
        [SerializeField] private int nivelDisparoDron = 2;
        [Min(0.1f)] [Tooltip("Segundos entre disparos de cada dron (s).")]
        [SerializeField] private float segundosEntreTirosDron = 1.5f;
        [Min(0f)] [Tooltip("Daño de cada disparo del dron (puntos de vida).")]
        [SerializeField] private float danioDron = 4f;
        [Range(0f, 1f)] [Tooltip("Probabilidad de acertar (0 a 1).")]
        [SerializeField] private float aciertoDron = 0.7f;
        [Min(0.05f)] [Tooltip("Aviso antes del disparo, con la luz del dron en rojo (s).")]
        [SerializeField] private float avisoDron = 0.3f;

        public int NivelDisparoDron => nivelDisparoDron;
        public float SegundosEntreTirosDron => segundosEntreTirosDron;
        public float DanioDron => danioDron;
        public float AciertoDron => aciertoDron;
        public float AvisoDron => avisoDron;
        public float AlturaDron => alturaDron;
        public float DistanciaAparicionDron => distanciaAparicionDron;
        public float PeriodoSirena => periodoSirena;

        public CalculoPersecucion Crear()
        {
            return new CalculoPersecucion(dronesPorNivel, patrullasPorNivel, velocidadDron, velocidadPatrulla, distanciaSeguimiento, distanciaCaptura);
        }
    }
}
