using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// T-1487: sensación del golpe cuerpo a cuerpo del jugador a pie: asistencia de objetivo, combo, hitstop, temblor de cámara y empuje.
    /// Si el jugador no tiene uno asignado, <see cref="PorDefecto"/> trae estos mismos valores.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config golpe a pie", fileName = "ConfigGolpeAPie_Nuevo")]
    public class ConfigGolpeAPie : ScriptableObject
    {
        [Header("Asistencia de objetivo")]
        [Tooltip("Alcance de la asistencia: enemigos a esta distancia o menos pueden ser elegidos (m).")]
        [Min(0f)] [SerializeField] private float alcanceAsistencia = 3f;
        [Tooltip("Medio ángulo del cono frontal de la asistencia (°).")]
        [Range(0f, 180f)] [SerializeField] private float anguloAsistencia = 70f;
        [Tooltip("Velocidad con que el cuerpo gira hacia el enemigo elegido (°/s). Bajo = giro suave.")]
        [Min(1f)] [SerializeField] private float velocidadGiro = 900f;
        [Tooltip("Lo máximo que se acerca al enemigo en un golpe (m).")]
        [Min(0f)] [SerializeField] private float acercamientoMaximo = 1.2f;
        [Tooltip("Distancia a la que se detiene del enemigo, para no atravesarlo (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaParada = 1f;
        [Tooltip("Rapidez del acercamiento (m/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezAcercamiento = 7f;

        [Header("Combo")]
        [Tooltip("Golpes del combo; el último es el golpe final, más fuerte.")]
        [Min(1)] [SerializeField] private int golpesCombo = 3;
        [Tooltip("Cuánto se recuerda un clic dado antes de poder golpear (s).")]
        [Min(0f)] [SerializeField] private float segundosBuffer = 0.3f;
        [Tooltip("Parte de la recuperación (0 a 1) desde la cual ya se puede encadenar el siguiente golpe.")]
        [Range(0f, 1f)] [SerializeField] private float ventanaCombo = 0.45f;
        [Tooltip("Segundos sin golpear tras los que el combo vuelve al primer golpe (s).")]
        [Min(0.05f)] [SerializeField] private float segundosReinicioCombo = 0.7f;

        [Header("Impacto")]
        [Tooltip("Congelado de la animación al conectar un golpe normal (s).")]
        [Range(0f, 0.15f)] [SerializeField] private float hitstop = 0.05f;
        [Tooltip("Congelado al conectar el golpe final del combo (s).")]
        [Range(0f, 0.15f)] [SerializeField] private float hitstopFinal = 0.08f;
        [Tooltip("Fuerza del impulso de cámara de un golpe normal (TemblorCamara.Generar, T-1485; 0.8 es el golpe base).")]
        [Min(0f)] [SerializeField] private float impulsoCamara = 0.8f;
        [Tooltip("Fuerza del impulso de cámara del golpe final.")]
        [Min(0f)] [SerializeField] private float impulsoCamaraFinal = 1.5f;
        [Tooltip("Velocidad inicial con que sale empujado el enemigo en un golpe normal (m/s).")]
        [Min(0f)] [SerializeField] private float empuje = 2.5f;
        [Tooltip("Velocidad inicial del empuje del golpe final (m/s).")]
        [Min(0f)] [SerializeField] private float empujeFinal = 6f;
        [Tooltip("Cuánto dura el empuje (s).")]
        [Min(0.05f)] [SerializeField] private float segundosEmpuje = 0.25f;

        public float AlcanceAsistencia => alcanceAsistencia;
        public float AnguloAsistencia => anguloAsistencia;
        public float VelocidadGiro => velocidadGiro;
        public float AcercamientoMaximo => acercamientoMaximo;
        public float DistanciaParada => distanciaParada;
        public float RapidezAcercamiento => rapidezAcercamiento;
        public int GolpesCombo => golpesCombo;
        public float SegundosBuffer => segundosBuffer;
        public float VentanaCombo => ventanaCombo;
        public float SegundosReinicioCombo => segundosReinicioCombo;
        public float Hitstop(bool final) => final ? hitstopFinal : hitstop;
        public float ImpulsoCamara(bool final) => final ? impulsoCamaraFinal : impulsoCamara;
        public float Empuje(bool final) => final ? empujeFinal : empuje;
        public float SegundosEmpuje => segundosEmpuje;

        private static ConfigGolpeAPie porDefecto;

        /// <summary>Config con los valores de código, para cuando el jugador no tiene un asset asignado.</summary>
        public static ConfigGolpeAPie PorDefecto
        {
            get
            {
                if (porDefecto == null)
                {
                    porDefecto = CreateInstance<ConfigGolpeAPie>();
                    porDefecto.hideFlags = HideFlags.HideAndDontSave;
                }
                return porDefecto;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico() => porDefecto = null;
    }
}
