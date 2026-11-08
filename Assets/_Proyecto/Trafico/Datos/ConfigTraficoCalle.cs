using MechaCyberpunk.Trafico.Calle;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>Los números del tráfico de la calle (T-451): cuántos carros hay de día y de noche, su velocidad, cómo frenan y pitan.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config del tráfico de calle", fileName = "ConfigTraficoCalle_Nueva")]
    public class ConfigTraficoCalle : ScriptableObject
    {
        [Header("Cuántos")]
        [Min(0)] [Tooltip("Carros en la calle de día.")]
        [SerializeField] private int carrosDeDia = 6;
        [Min(0)] [Tooltip("Carros en la calle de noche.")]
        [SerializeField] private int carrosDeNoche = 3;

        [Header("Manejo")]
        [Min(1f)] [Tooltip("T-703: velocidad normal más baja de un carro (m/s). 8 m/s son unos 29 km/h.")]
        [SerializeField] private float velocidadMinima = 8f;
        [Min(1f)] [Tooltip("T-703: velocidad normal más alta de un carro (m/s). 12 m/s son unos 43 km/h. Cada carro toma una al azar entre las dos.")]
        [SerializeField] private float velocidadMaxima = 12f;
        [Min(0.5f)] [Tooltip("Aceleración al librarse (m/s²).")]
        [SerializeField] private float aceleracion = 3f;
        [Min(0.5f)] [Tooltip("Frenado (m/s²).")]
        [SerializeField] private float frenado = 9f;
        [Min(1f)] [Tooltip("A esta distancia de un obstáculo empieza a frenar (m).")]
        [SerializeField] private float distanciaFrenado = 14f;
        [Min(0.5f)] [Tooltip("A esta distancia o menos se detiene (m).")]
        [SerializeField] private float distanciaParada = 5f;
        [Min(0.5f)] [Tooltip("Separación del eje de la calle, hacia la derecha de su marcha (m).")]
        [SerializeField] private float separacionCarril = 2.4f;
        [Min(1f)] [Tooltip("Cuánto gira para seguir la calle (°/s).")]
        [SerializeField] private float giroMaximo = 150f;
        [Min(0.5f)] [Tooltip("Distancia a la que llega a un nodo de la calle y toma el siguiente (m).")]
        [SerializeField] private float margenNodo = 3f;

        [Header("Claxon y susto")]
        [Min(0.5f)] [Tooltip("Bloqueado este tiempo, pita (s).")]
        [SerializeField] private float segundosParaPitar = 1.5f;
        [Min(0.5f)] [Tooltip("Mínimo entre dos pitidos de un mismo carro (s).")]
        [SerializeField] private float segundosEntrePitidos = 4f;
        [Min(1f)] [Tooltip("Un disparo a menos de esto lo asusta (m).")]
        [SerializeField] private float radioSusto = 45f;
        [Min(0.5f)] [Tooltip("Cuánto se queda detenido por un susto (s).")]
        [SerializeField] private float segundosSusto = 3f;
        [Min(1f)] [Tooltip("Un carro que pasa a menos de esta distancia del jugador a buena velocidad hace el sonido de pasar cerca (m).")]
        [SerializeField] private float distanciaPasaCerca = 6f;
        [Min(1f)] [Tooltip("T-588: un carro atorado tanto tiempo (por algo fijo o por otro carro) vuelve a salir en otra calle si nadie lo ve (s).")]
        [SerializeField] private float segundosParaDestrabar = 10f;
        [Min(1f)] [Tooltip("T-588: atorado tanto tiempo vuelve a salir en otra calle aunque se vea (s).")]
        [SerializeField] private float segundosDestrabarForzado = 30f;

        public int CarrosDeDia => carrosDeDia;
        public int CarrosDeNoche => carrosDeNoche;
        public int Maximo => Mathf.Max(carrosDeDia, carrosDeNoche);
        public int Cantidad(bool esDeNoche) => esDeNoche ? carrosDeNoche : carrosDeDia;
        public float VelocidadMinima => velocidadMinima;
        public float VelocidadMaxima => velocidadMaxima;
        /// <summary>La velocidad media del rango (m/s).</summary>
        public float VelocidadCrucero => (velocidadMinima + velocidadMaxima) * 0.5f;
        public float SeparacionCarril => separacionCarril;
        public float GiroMaximo => giroMaximo;
        public float MargenNodo => margenNodo;
        public float DistanciaFrenado => distanciaFrenado;
        public float RadioSusto => radioSusto;
        public float SegundosSusto => segundosSusto;
        public float DistanciaPasaCerca => distanciaPasaCerca;
        public float SegundosParaDestrabar => segundosParaDestrabar;
        public float SegundosDestrabarForzado => segundosDestrabarForzado;

        /// <summary>Arma las reglas de manejo de un carro con estos números.</summary>
        public CalculoTraficoCalle Crear()
        {
            return Crear(VelocidadCrucero);
        }

        /// <summary>T-728: igual, pero con una aceleración propia (al menos la del tráfico): un carro que huye arranca fuerte en vez de tardar 5 s en alcanzar su velocidad.</summary>
        public CalculoTraficoCalle Crear(float velocidadDelCarro, float aceleracionMinima)
        {
            return new CalculoTraficoCalle(velocidadDelCarro, Mathf.Max(aceleracion, aceleracionMinima), frenado, distanciaFrenado, distanciaParada, segundosParaPitar, segundosEntrePitidos);
        }

        /// <summary>T-703: igual, con la velocidad de crucero propia de ese carro.</summary>
        public CalculoTraficoCalle Crear(float velocidadDelCarro)
        {
            return new CalculoTraficoCalle(velocidadDelCarro, aceleracion, frenado, distanciaFrenado, distanciaParada, segundosParaPitar, segundosEntrePitidos);
        }
    }
}
