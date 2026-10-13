using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>Valores de sensación y física de un carro (T-074).</summary>
    [CreateAssetMenu(fileName = "ConfigCarro", menuName = "Mecha Cyberpunk/Config carro")]
    public class ConfigCarro : ScriptableObject
    {
        [Header("Cuerpo")]
        [Tooltip("Masa del carro (kg).")]
        [Min(100f)] [SerializeField] private float masa = 1100f;
        [Tooltip("Altura del centro de masa sobre el origen del carro (m). Bajo para que no se voltee.")]
        [Range(0f, 1f)] [SerializeField] private float alturaCentroMasa = 0.5f;
        [Tooltip("Torque que endereza el carro cuando se inclina de lado o de frente (N·m por radián).")]
        [Min(0f)] [SerializeField] private float fuerzaAntivuelco = 4000f;

        [Header("Motor y freno")]
        [Tooltip("Velocidad máxima hacia adelante (m/s).")]
        [Min(1f)] [SerializeField] private float velocidadMaxima = 16f;
        [Tooltip("Velocidad máxima en reversa (m/s).")]
        [Min(1f)] [SerializeField] private float velocidadReversa = 5f;
        [Tooltip("Velocidad bajo la cual S da reversa en vez de frenar (m/s).")]
        [Min(0.1f)] [SerializeField] private float umbralReversa = 0.5f;
        [Tooltip("Fuerza del motor parado, baja al acercarse a la velocidad máxima (N).")]
        [Min(0f)] [SerializeField] private float fuerzaMotor = 6200f;
        [Tooltip("Fuerza del motor en reversa (N).")]
        [Min(0f)] [SerializeField] private float fuerzaReversa = 3500f;
        [Tooltip("Fuerza del freno de pie (N).")]
        [Min(0f)] [SerializeField] private float fuerzaFreno = 9000f;
        [Tooltip("Fuerza extra del freno de mano (N).")]
        [Min(0f)] [SerializeField] private float fuerzaFrenoMano = 6000f;
        [Tooltip("Resistencia al avance cuando no se toca nada (N).")]
        [Min(0f)] [SerializeField] private float resistenciaRodadura = 500f;
        [Tooltip("Qué tan fuerte se agarra el carro cuando no hay control (1/s).")]
        [Min(0f)] [SerializeField] private float gananciaEstacionamiento = 40f;

        [Header("Dirección y agarre")]
        [Tooltip("Ángulo de las ruedas con el carro parado (°).")]
        [Range(1f, 45f)] [SerializeField] private float anguloDireccionParado = 32f;
        [Tooltip("Ángulo de las ruedas a la velocidad máxima (°).")]
        [Range(1f, 45f)] [SerializeField] private float anguloDireccionMaximo = 10f;
        [Tooltip("Qué tan rápido baja el ángulo con la velocidad; menor que 1 lo baja pronto (sin unidad).")]
        [Range(0.1f, 2f)] [SerializeField] private float exponenteDireccion = 0.5f;
        [Tooltip("Distancia entre el eje delantero y el trasero (m).")]
        [Min(1f)] [SerializeField] private float distanciaEntreEjes = 2.7f;
        [Tooltip("Aceleración lateral máxima que aguantan las llantas (m/s²).")]
        [Min(1f)] [SerializeField] private float aceleracionLateralMaxima = 9f;
        [Tooltip("Qué tan rápido se cancela el derrape (1/s).")]
        [Min(0f)] [SerializeField] private float agarreLateral = 8f;
        [Tooltip("Fracción del agarre que queda con el freno de mano a tope (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float factorAgarreFrenoMano = 0.25f;
        [Tooltip("Qué tan rápido el carro alcanza la velocidad de giro pedida (1/s).")]
        [Min(0.1f)] [SerializeField] private float respuestaGiro = 8f;

        [Header("Suspensión")]
        [Tooltip("Distancia del anclaje a la rueda con el carro en reposo (m).")]
        [Min(0.05f)] [SerializeField] private float largoReposo = 0.35f;
        [Tooltip("Distancia del anclaje a la rueda con el resorte estirado del todo (m).")]
        [Min(0.1f)] [SerializeField] private float largoMaximo = 0.5f;
        [Tooltip("Radio de la rueda (m).")]
        [Min(0.1f)] [SerializeField] private float radioRueda = 0.35f;
        [Tooltip("Rigidez de cada resorte (N/m).")]
        [Min(0f)] [SerializeField] private float rigidez = 18000f;
        [Tooltip("Amortiguamiento de cada resorte (N·s/m).")]
        [Min(0f)] [SerializeField] private float amortiguamiento = 3500f;

        [Header("Bajar")]
        [Tooltip("Rapidez bajo la cual el conductor puede bajar (m/s).")]
        [Min(0.01f)] [SerializeField] private float rapidezMaximaParaBajar = 0.5f;
        [Tooltip("Radio de la cápsula de persona que debe caber en la salida (m).")]
        [Min(0.1f)] [SerializeField] private float radioSalida = 0.4f;
        [Tooltip("Alto de la cápsula de persona que debe caber en la salida (m).")]
        [Min(0.5f)] [SerializeField] private float alturaSalida = 1.8f;
        [Tooltip("Cuánto se levanta la cápsula del piso al revisar la salida, para no chocar con el piso (m).")]
        [Min(0f)] [SerializeField] private float holguraSalida = 0.05f;

        public float RapidezMaximaParaBajar => rapidezMaximaParaBajar;
        public float RadioSalida => radioSalida;
        public float AlturaSalida => alturaSalida;
        public float HolguraSalida => holguraSalida;
        public float Masa => masa;
        public float AlturaCentroMasa => alturaCentroMasa;
        public float FuerzaAntivuelco => fuerzaAntivuelco;
        public float VelocidadMaxima => velocidadMaxima;
        public float RespuestaGiro => respuestaGiro;
        public float LargoReposo => largoReposo;
        public float LargoMaximo => largoMaximo;
        public float RadioRueda => radioRueda;
        public float Rigidez => rigidez;
        public float Amortiguamiento => amortiguamiento;

        /// <summary>Valores de manejo para la lógica pura.</summary>
        public ParametrosManejo Parametros => new ParametrosManejo
        {
            Masa = masa,
            VelocidadMaxima = velocidadMaxima,
            VelocidadReversa = velocidadReversa,
            UmbralReversa = umbralReversa,
            FuerzaMotor = fuerzaMotor,
            FuerzaReversa = fuerzaReversa,
            FuerzaFreno = fuerzaFreno,
            FuerzaFrenoMano = fuerzaFrenoMano,
            ResistenciaRodadura = resistenciaRodadura,
            AnguloDireccionParado = anguloDireccionParado,
            AnguloDireccionMaximo = anguloDireccionMaximo,
            ExponenteDireccion = exponenteDireccion,
            DistanciaEntreEjes = distanciaEntreEjes,
            AceleracionLateralMaxima = aceleracionLateralMaxima,
            AgarreLateral = agarreLateral,
            FactorAgarreFrenoMano = factorAgarreFrenoMano,
            GananciaEstacionamiento = gananciaEstacionamiento,
        };
    }
}
