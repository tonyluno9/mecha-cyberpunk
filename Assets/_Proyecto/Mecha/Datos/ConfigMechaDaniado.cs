using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Reglas del daño persistente del mecha del jugador (D-tony-42).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config mecha dañado", fileName = "ConfigMechaDaniado_Nueva")]
    public class ConfigMechaDaniado : ScriptableObject
    {
        [Tooltip("Vida mínima con la que empieza una pelea, como fracción de la vida máxima (0.15 = 15 %). " +
                 "Si el mecha quedó por debajo (o en KO), la siguiente pelea lo levanta a este piso.")]
        [Range(0.01f, 1f)] [SerializeField] private float pisoVidaPersistente = 0.15f;

        [Tooltip("Con la vida en esta fracción de la máxima o menos (0.3 = 30 %), el mecha se siente dañado: más lento y con alarma.")]
        [Range(0.01f, 1f)] [SerializeField] private float umbralVidaBaja = 0.3f;
        [Tooltip("Multiplica la velocidad máxima con vida baja (0.7 = 70 %).")]
        [Range(0.1f, 1f)] [SerializeField] private float factorVelocidad = 0.7f;
        [Tooltip("Multiplica la aceleración y el frenado con vida baja.")]
        [Range(0.1f, 1f)] [SerializeField] private float factorAceleracion = 0.6f;
        [Tooltip("Multiplica la velocidad máxima de giro con vida baja.")]
        [Range(0.1f, 1f)] [SerializeField] private float factorGiro = 0.75f;
        [Tooltip("Parpadeos de la alarma por segundo (Hz): la mitad del periodo prendida y la mitad apagada.")]
        [Min(0.1f)] [SerializeField] private float frecuenciaAlarma = 2f;
        [Tooltip("Color de la luz y del LED de la alarma.")]
        [SerializeField] private Color colorAlarma = new Color(1f, 0.05f, 0.05f);
        [Tooltip("Intensidad de la luz de la alarma prendida.")]
        [Min(0f)] [SerializeField] private float intensidadLuzAlarma = 1.5f;
        [Tooltip("Alcance de la luz de la alarma (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceLuzAlarma = 2.5f;

        public float PisoVidaPersistente => pisoVidaPersistente;
        public float UmbralVidaBaja => umbralVidaBaja;
        public float FactorVelocidad => factorVelocidad;
        public float FactorAceleracion => factorAceleracion;
        public float FactorGiro => factorGiro;
        public float FrecuenciaAlarma => frecuenciaAlarma;
        public Color ColorAlarma => colorAlarma;
        public float IntensidadLuzAlarma => intensidadLuzAlarma;
        public float AlcanceLuzAlarma => alcanceLuzAlarma;
    }
}
