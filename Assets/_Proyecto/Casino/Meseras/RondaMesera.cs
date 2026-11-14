using UnityEngine;

namespace MechaCyberpunk.Casino.Meseras
{
    /// <summary>T-892: mueve a una mesera robot del casino por su ronda y le dice al Animator si camina o está parada.</summary>
    public class RondaMesera : MonoBehaviour
    {
        // El mismo parámetro del controlador de personajes (ReglasVisual.Velocidad): Casino no referencia a Personaje.
        private const string ParametroVelocidad = "Velocidad";
        private const float GradosPorSegundo = 360f;

        [SerializeField] private Vector3[] puntos;
        [Tooltip("Velocidad al caminar (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidad = 1.1f;
        [Tooltip("Segundos parada en cada punto.")]
        [Min(0f)] [SerializeField] private float pausa = 2.5f;
        [Tooltip("Segundos antes de arrancar, para que no vayan todas al mismo paso.")]
        [Min(0f)] [SerializeField] private float esperaInicial;
        [SerializeField] private Animator animador;
        [Tooltip("Giro (°) del modelo respecto a su camino. La R01 de Blender (T-973) mira hacia -Z: con 0 caminaba de espaldas.")]
        [SerializeField] private float giroModelo = 180f;

        private CalculoRondaMesera ronda;

        public bool EstaCaminando => ronda != null && ronda.EstaCaminando;

        private void Awake()
        {
            if (puntos == null || puntos.Length < 2 || animador == null)
            {
                Debug.LogError("[Casino] RondaMesera en '" + name + "' necesita al menos dos puntos y su Animator.", this);
                enabled = false;
                return;
            }
            ronda = new CalculoRondaMesera(puntos, velocidad, pausa, esperaInicial);
            transform.position = ronda.Posicion;
            transform.rotation = Mirada();
        }

        /// <summary>Hacia dónde gira la raíz para que el modelo mire a su camino.</summary>
        private Quaternion Mirada() => Quaternion.LookRotation(ronda.Direccion) * Quaternion.Euler(0f, giroModelo, 0f);

        private void Update()
        {
            ronda.Avanzar(Time.deltaTime);
            transform.position = ronda.Posicion;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Mirada(), GradosPorSegundo * Time.deltaTime);
            animador.SetFloat(ParametroVelocidad, ronda.EstaCaminando ? velocidad : 0f);
        }
    }
}
