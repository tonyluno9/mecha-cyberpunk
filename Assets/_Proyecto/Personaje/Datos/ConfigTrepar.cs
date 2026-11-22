using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Sensación de trepar escaleras y tubos y de agarrarse de bordes (contenedores, bardas, pretiles, techos).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config trepar", fileName = "ConfigTrepar_Nuevo")]
    public class ConfigTrepar : ScriptableObject
    {
        [Tooltip("Velocidad de subida en escaleras (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadEscalera = 2.2f;

        [Tooltip("Velocidad de subida en tubos (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadTubo = 1.5f;

        [Tooltip("Altura mínima del borde sobre los pies para agarrarlo (m).")]
        [Min(0.1f)] [SerializeField] private float alturaMinimaAgarre = 0.5f;

        [Tooltip("Altura máxima del borde sobre los pies para agarrarlo (m).")]
        [Min(0.2f)] [SerializeField] private float alturaMaximaAgarre = 2.3f;

        [Tooltip("Qué tan lejos delante de los pies se busca la pared (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaSondeo = 0.7f;

        [Tooltip("Duración base del agarre (s).")]
        [Min(0.05f)] [SerializeField] private float segundosAgarreBase = 0.35f;

        [Tooltip("Segundos extra del agarre por cada metro de altura del borde (s/m).")]
        [Min(0f)] [SerializeField] private float segundosAgarrePorMetro = 0.2f;

        [Tooltip("Ángulo máximo entre hacia donde mira el jugador y la escalera para engancharse (°).")]
        [Range(1f, 180f)] [SerializeField] private float anguloMaximoEnganche = 60f;

        [Tooltip("Empuje hacia atrás al soltarse de una escalera con salto (m/s).")]
        [Min(0f)] [SerializeField] private float empujeSoltar = 2f;

        [Tooltip("Si es verdadero, en el aire con W se agarra solo de un borde al alcance.")]
        [SerializeField] private bool agarreAutomaticoEnAire = true;

        [Tooltip("Normal vertical mínima de una superficie para poder pararse encima (1 = plana).")]
        [Range(0.1f, 1f)] [SerializeField] private float normalMinimaSuelo = 0.7f;

        [Tooltip("Segundos sin poder agarrarse de nuevo tras soltarse o terminar un agarre (s).")]
        [Min(0f)] [SerializeField] private float segundosEnfriamiento = 0.4f;

        [Tooltip("Cuánto sube el rayo de sondeo sobre la altura máxima de agarre (m).")]
        [Min(0.05f)] [SerializeField] private float sobrealturaSondeo = 0.3f;

        [Tooltip("Holgura al colocar los pies sobre la superficie y al medir el espacio libre (m).")]
        [Min(0f)] [SerializeField] private float margenSuperficie = 0.05f;

        [Tooltip("Desnivel máximo entre los puntos de apoyo de los pies arriba (m). Un pretil delgado no da apoyo.")]
        [Min(0.01f)] [SerializeField] private float toleranciaApoyo = 0.15f;

        [Header("Colgarse (T-1628)")]
        [Tooltip("Si es verdadero, un borde alto a la altura de las manos cuelga al jugador (PJ01_Colgado_Idle) en vez de treparse de un tirón.")]
        [SerializeField] private bool colgarseActivo = true;

        [Tooltip("Altura mínima del borde sobre los pies para colgarse de él (m). Más bajo se trepa directo.")]
        [Min(0.5f)] [SerializeField] private float alturaMinimaColgarse = 1.8f;

        [Tooltip("Altura de las manos sobre los pies en la pose colgada (m): los pies quedan esta distancia bajo el borde.")]
        [Min(0.5f)] [SerializeField] private float alturaManosColgado = 2f;

        [Tooltip("Separación de los pies a la pared al colgarse (m).")]
        [Min(0f)] [SerializeField] private float distanciaParedColgado = 0.2f;

        [Tooltip("Lo que tarda en llegar a la pose colgada desde donde saltó (s).")]
        [Min(0.05f)] [SerializeField] private float segundosEntradaColgado = 0.35f;

        [Tooltip("Tiempo colgado antes de aceptar subir o soltarse (s).")]
        [Min(0f)] [SerializeField] private float segundosMinimosColgado = 0.5f;

        [Tooltip("Lo que dura subir al borde desde colgado (s): PJ01_Colgado_Subir (116 cuadros a 30 fps) a 1.5x.")]
        [Min(0.2f)] [SerializeField] private float segundosSubirDeColgado = 2.4f;

        public bool ColgarseActivo => colgarseActivo;
        public float AlturaMinimaColgarse => alturaMinimaColgarse;
        public float AlturaManosColgado => alturaManosColgado;
        public float DistanciaParedColgado => distanciaParedColgado;
        public float SegundosEntradaColgado => segundosEntradaColgado;
        public float SegundosMinimosColgado => segundosMinimosColgado;
        public float SegundosSubirDeColgado => segundosSubirDeColgado;

        public float VelocidadEscalera => velocidadEscalera;
        public float VelocidadTubo => velocidadTubo;
        public float AlturaMinimaAgarre => alturaMinimaAgarre;
        public float AlturaMaximaAgarre => alturaMaximaAgarre;
        public float DistanciaSondeo => distanciaSondeo;
        public float SegundosAgarreBase => segundosAgarreBase;
        public float SegundosAgarrePorMetro => segundosAgarrePorMetro;
        public float AnguloMaximoEnganche => anguloMaximoEnganche;
        public float EmpujeSoltar => empujeSoltar;
        public bool AgarreAutomaticoEnAire => agarreAutomaticoEnAire;
        public float NormalMinimaSuelo => normalMinimaSuelo;
        public float SegundosEnfriamiento => segundosEnfriamiento;
        public float SobrealturaSondeo => sobrealturaSondeo;
        public float MargenSuperficie => margenSuperficie;
        public float ToleranciaApoyo => toleranciaApoyo;
    }
}
