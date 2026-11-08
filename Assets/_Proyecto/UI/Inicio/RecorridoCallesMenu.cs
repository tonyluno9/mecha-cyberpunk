using UnityEngine;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// T-898 (C1): la cámara real del fondo del menú de inicio. Vive en la escena Menu_Calles (la ciudad sin lógica de juego) y pasa despacio por
    /// K01, el tianguis, Neón Alto y Nova: cada <see cref="SegundosPorCalle"/> corta a la calle siguiente. La cuenta la lleva <see cref="CalculoFondoCalles"/>.
    /// </summary>
    public class RecorridoCallesMenu : MonoBehaviour
    {
        [SerializeField] private Camera camara;
        [Tooltip("Dónde empieza la cámara en cada calle.")]
        [SerializeField] private Transform[] desde = new Transform[0];
        [Tooltip("Hacia dónde mira y avanza en cada calle.")]
        [SerializeField] private Transform[] hacia = new Transform[0];
        [Min(0.1f)] [Tooltip("Tiempo en cada calle antes del corte (s).")]
        [SerializeField] private float segundosPorCalle = 8f;
        [Min(0f)] [Tooltip("Velocidad de la cámara por la calle (m/s).")]
        [SerializeField] private float velocidad = 1.1f;

        private CalculoFondoCalles calculo;
        private float enCalle;

        public static RecorridoCallesMenu Actual { get; private set; }
        public int Calle => calculo != null ? calculo.Actual : 0;
        public int Calles => desde.Length;
        public float SegundosPorCalle => segundosPorCalle;
        public Camera Camara => camara;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Actual = null;

        /// <summary>Para el creador de la escena.</summary>
        public void Configurar(Camera nuevaCamara, Transform[] nuevosDesde, Transform[] nuevosHacia)
        {
            camara = nuevaCamara;
            desde = nuevosDesde;
            hacia = nuevosHacia;
        }

        private void Awake()
        {
            if (camara == null || desde.Length == 0 || desde.Length != hacia.Length)
            {
                Debug.LogError("[UI] RecorridoCallesMenu sin cámara o sin tomas en " + name, this);
                enabled = false;
                return;
            }
            Actual = this;
            calculo = new CalculoFondoCalles(desde.Length, segundosPorCalle);
            Colocar();
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            enCalle += dt;
            if (calculo.Avanzar(dt))
            {
                enCalle = 0f;
            }
            Colocar();
        }

        private void Colocar()
        {
            Vector3 inicio = desde[Calle].position;
            Vector3 mira = hacia[Calle].position;
            Vector3 direccion = (mira - inicio).normalized;
            Vector3 suelo = new Vector3(direccion.x, 0f, direccion.z).normalized;
            camara.transform.SetPositionAndRotation(inicio + suelo * (velocidad * enCalle), Quaternion.LookRotation(direccion, Vector3.up));
        }
    }
}
