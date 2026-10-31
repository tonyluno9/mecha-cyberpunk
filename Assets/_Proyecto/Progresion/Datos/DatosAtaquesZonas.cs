using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Números de los ataques rivales a tus zonas (T-427, D-tony-297). Asset: DatosAtaquesZonas_Base.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de ataques a zonas", fileName = "DatosAtaquesZonas_Nuevo")]
    public class DatosAtaquesZonas : ScriptableObject
    {
        [Tooltip("Espera entre ataques: un valor al azar entre mínimo y máximo (s), antes de acortarla por zonas y calor.")]
        [Min(10f)] [SerializeField] private float segundosMinimo = 420f;
        [Min(10f)] [SerializeField] private float segundosMaximo = 780f;
        [Tooltip("Cuánto se acorta la espera por cada zona tuya además de la primera (0.35 = 35 % más seguido).")]
        [Min(0f)] [SerializeField] private float masPorZona = 0.35f;
        [Tooltip("Cuánto se acorta la espera por cada nivel de calor.")]
        [Min(0f)] [SerializeField] private float masPorCalor = 0.2f;
        [Tooltip("Tiempo para llegar a la zona atacada (s).")]
        [Min(10f)] [SerializeField] private float segundosParaLlegar = 150f;
        [Tooltip("Tiempo para vencer la oleada una vez que llegas (s).")]
        [Min(10f)] [SerializeField] private float segundosOleada = 180f;
        [Min(0)] [SerializeField] private int premioBiits = 300;
        [Tooltip("Respeto que sube la zona al defenderla.")]
        [Min(0)] [SerializeField] private int premioRespeto = 20;
        [Tooltip("Respeto que baja la zona si no la defiendes.")]
        [Min(0)] [SerializeField] private int castigoRespeto = 45;
        [Tooltip("Si el respeto de la zona queda por debajo, pasa al rival.")]
        [Min(0)] [SerializeField] private int umbralPerder = 40;
        [Tooltip("Respeto con el que la zona queda en manos del rival.")]
        [Min(0)] [SerializeField] private int respetoAlPerder = 30;
        [Tooltip("La zona que nunca se ataca (tu barrio).")]
        [SerializeField] private string zonaProtegida = "fosa";

        public float SegundosMinimo => segundosMinimo;
        public float SegundosMaximo => Mathf.Max(segundosMinimo, segundosMaximo);
        public float MasPorZona => masPorZona;
        public float MasPorCalor => masPorCalor;
        public float SegundosParaLlegar => segundosParaLlegar;
        public float SegundosOleada => segundosOleada;
        public int PremioBiits => premioBiits;
        public int PremioRespeto => premioRespeto;
        public int CastigoRespeto => castigoRespeto;
        public int UmbralPerder => umbralPerder;
        public int RespetoAlPerder => respetoAlPerder;
        public string ZonaProtegida => zonaProtegida;
    }
}
