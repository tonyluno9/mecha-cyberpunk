using UnityEngine;

namespace MechaCyberpunk.Combate.Datos
{
    /// <summary>
    /// Poses y velocidad de los brazos de la cabina. Cada pose es un desplazamiento (m) y un giro (°) respecto a la pose de reposo
    /// del pivote del brazo derecho. El brazo izquierdo usa la pose de guardia reflejada.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config brazos", fileName = "ConfigBrazos_Nuevo")]
    public class ConfigBrazos : ScriptableObject
    {
        [Header("Preparación del golpe (brazo derecho echado atrás)")]
        [Tooltip("Desplazamiento del pivote respecto al reposo (m, x lateral, y arriba, z adelante).")]
        [SerializeField] private Vector3 desplazamientoPreparacion = new Vector3(0.05f, 0.08f, -0.25f);
        [Tooltip("Giro respecto al reposo (°).")]
        [SerializeField] private Vector3 giroPreparacion = new Vector3(-20f, 0f, 0f);

        [Header("Impacto (brazo derecho al frente)")]
        [Tooltip("Desplazamiento del pivote respecto al reposo (m).")]
        [SerializeField] private Vector3 desplazamientoImpacto = new Vector3(-0.1f, 0.1f, 0.6f);
        [Tooltip("Giro respecto al reposo (°).")]
        [SerializeField] private Vector3 giroImpacto = Vector3.zero;

        [Header("Guardia (brazo derecho; el izquierdo se refleja)")]
        [Tooltip("Desplazamiento del pivote respecto al reposo (m).")]
        [SerializeField] private Vector3 desplazamientoGuardia = new Vector3(-0.42f, 0.12f, 0.1f);
        [Tooltip("Giro respecto al reposo (°). El giro en Y cruza el brazo frente al cristal.")]
        [SerializeField] private Vector3 giroGuardia = new Vector3(-12f, -45f, 0f);

        [Header("Movimiento")]
        [Tooltip("Qué tan rápido siguen los brazos a su pose (1/s). Más alto, más seco.")]
        [Min(0.1f)] [SerializeField] private float suavizado = 30f;

        public Vector3 DesplazamientoPreparacion => desplazamientoPreparacion;
        public Vector3 GiroPreparacion => giroPreparacion;
        public Vector3 DesplazamientoImpacto => desplazamientoImpacto;
        public Vector3 GiroImpacto => giroImpacto;
        public Vector3 DesplazamientoGuardia => desplazamientoGuardia;
        public Vector3 GiroGuardia => giroGuardia;
        public float Suavizado => suavizado;
    }
}
