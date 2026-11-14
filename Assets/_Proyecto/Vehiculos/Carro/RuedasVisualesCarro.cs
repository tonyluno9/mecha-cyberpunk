using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-742: las cuatro ruedas del carro armado por piezas giran con la velocidad y las delanteras doblan con la dirección. Cada rueda es una cadena
    /// Rueda_xx / Direccion (dobla, eje Y) / Giro (rueda, eje X) / Modelo (la llanta RU01). Si el carro tiene control, la dirección sale del volante de
    /// <see cref="CarroViejo"/>; si lo mueve otro script (tráfico, patrulla), sale de cuánto gira el carro. No toca la física: solo mueve esos hijos.
    /// </summary>
    public class RuedasVisualesCarro : MonoBehaviour
    {
        [Header("Ruedas")]
        [Tooltip("Pivotes que doblan, de las ruedas delanteras (izquierda y derecha).")]
        [SerializeField] private Transform[] direccionDelantera = new Transform[0];
        [Tooltip("Pivotes que ruedan, de las cuatro ruedas.")]
        [SerializeField] private Transform[] giro = new Transform[0];
        [Tooltip("Radio de la llanta visible (m).")]
        [SerializeField, Min(0.05f)] private float radio = 0.3f;
        [Tooltip("Distancia entre el eje delantero y el trasero (m).")]
        [SerializeField, Min(0.1f)] private float distanciaEjes = 2.5f;

        [Header("Dirección")]
        [Tooltip("Cuánto doblan las ruedas con el volante a fondo y el carro parado (°).")]
        [SerializeField, Range(0f, 60f)] private float anguloParado = 32f;
        [Tooltip("Cuánto doblan a toda velocidad (°).")]
        [SerializeField, Range(0f, 60f)] private float anguloRapido = 12f;
        [Tooltip("Velocidad a la que se considera 'toda velocidad' para el ángulo de dirección (m/s).")]
        [SerializeField, Min(1f)] private float velocidadRapida = 16f;
        [Tooltip("Qué tan rápido siguen las ruedas a la dirección (°/s).")]
        [SerializeField, Min(1f)] private float velocidadDireccion = 240f;
        [Tooltip("Debajo de esta rapidez (m/s) un carro sin control no dobla las ruedas (no se sabe hacia dónde gira).")]
        [SerializeField, Min(0f)] private float velocidadMinimaDireccion = 0.5f;
        [Tooltip("Tiempo con el que se promedian la velocidad y el giro para doblar las ruedas (s). Evita que tiemblen si otro script mueve el carro a saltos (física a 50 Hz).")]
        [SerializeField, Min(0.01f)] private float suavizado = 0.12f;

        private CarroViejo carro;
        private Vector3 posicionAnterior;
        private float yawAnterior;
        private bool tieneAnterior;
        private float rodado;
        private float direccionActual;
        private float velocidadSuave;
        private float giroSuave;

        /// <summary>Cuánto han rodado las ruedas en total (°, de 0 a 360).</summary>
        public float RodadoGrados => rodado;

        /// <summary>Cuánto doblan ahora las ruedas delanteras (°, positivo a la derecha).</summary>
        public float DireccionGrados => direccionActual;

        /// <summary>Radio de la llanta (m).</summary>
        public float Radio => radio;

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
        }

        private void OnEnable()
        {
            tieneAnterior = false;
            velocidadSuave = 0f;
            giroSuave = 0f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }
            Vector3 posicion = transform.position;
            float yaw = transform.eulerAngles.y;
            if (!tieneAnterior)
            {
                posicionAnterior = posicion;
                yawAnterior = yaw;
                tieneAnterior = true;
                return;
            }
            float avance = Vector3.Dot(posicion - posicionAnterior, transform.forward);
            float giroGrados = Mathf.DeltaAngle(yawAnterior, yaw);
            posicionAnterior = posicion;
            yawAnterior = yaw;
            Avanzar(avance, giroGrados, dt);
        }

        /// <summary>Un paso de las ruedas: rueda lo que avanzó el carro (m hacia adelante, negativo en reversa) y dobla según el volante o lo que giró (°).</summary>
        public void Avanzar(float avanceMetros, float giroGrados, float dt)
        {
            float velocidad = avanceMetros / dt;
            rodado = Mathf.Repeat(rodado + CalculoRuedasVisuales.AvanceGrados(velocidad, radio, dt), 360f);
            float suavidad = 1f - Mathf.Exp(-dt / suavizado);
            velocidadSuave += (velocidad - velocidadSuave) * suavidad;
            giroSuave += (giroGrados / dt - giroSuave) * suavidad;
            float objetivo;
            if (carro != null && (carro.TieneControl || carro.ConIA))
            {
                objetivo = CalculoRuedasVisuales.AnguloPorVolante(carro.Volante, velocidadSuave, velocidadRapida, anguloParado, anguloRapido);
            }
            else
            {
                objetivo = CalculoRuedasVisuales.AnguloPorGiro(distanciaEjes, velocidadSuave, giroSuave, velocidadMinimaDireccion, anguloParado);
            }
            direccionActual = CalculoRuedasVisuales.Acercar(direccionActual, objetivo, velocidadDireccion, dt);
            Aplicar();
        }

        private void Aplicar()
        {
            Quaternion doblar = Quaternion.Euler(0f, direccionActual, 0f);
            for (int i = 0; i < direccionDelantera.Length; i++)
            {
                if (direccionDelantera[i] != null)
                {
                    direccionDelantera[i].localRotation = doblar;
                }
            }
            Quaternion rodar = Quaternion.Euler(rodado, 0f, 0f);
            for (int i = 0; i < giro.Length; i++)
            {
                if (giro[i] != null)
                {
                    giro[i].localRotation = rodar;
                }
            }
        }
    }
}
