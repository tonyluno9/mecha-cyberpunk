using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Peatón de la ciudad (T-510): 1 de cada 4 sale robot (RobotHombre) en lugar de la persona de color, y cada uno arranca su caminata en un
    /// punto distinto del ciclo para que no avancen todos al mismo paso. Va en la raíz del prefab Peaton, con las dos versiones como hijos.
    /// </summary>
    public class PeatonVariado : MonoBehaviour
    {
        [SerializeField] private GameObject persona;
        [SerializeField] private GameObject robot;
        [Range(0f, 1f)] [SerializeField] private float probabilidadRobot = 0.25f;

        [Header("T-1389b: personajes nuevos")]
        [Tooltip("Visuales (Visual_NPxx_*) de los que sale uno al azar en lugar de la persona de color. Vacío = solo persona y robot.")]
        [SerializeField] private GameObject[] variantes = new GameObject[0];
        [Tooltip("Probabilidad (0 a 1) de salir como una de las variantes cuando no sale robot.")]
        [Range(0f, 1f)] [SerializeField] private float probabilidadVariante;

        /// <summary>El visual nuevo que se eligió (null si salió persona o robot).</summary>
        public GameObject VarianteElegida { get; private set; }
        public System.Collections.Generic.IReadOnlyList<GameObject> Variantes => variantes;
        public float ProbabilidadVariante => probabilidadVariante;

        public void ConfigurarVariantes(GameObject[] visuales, float probabilidad)
        {
            variantes = visuales ?? new GameObject[0];
            probabilidadVariante = probabilidad;
        }

        public bool EsRobot { get; private set; }
        public GameObject Persona => persona;
        public GameObject Robot => robot;
        public float ProbabilidadRobot => probabilidadRobot;

        public void Configurar(GameObject laPersona, GameObject elRobot, float probabilidad)
        {
            persona = laPersona;
            robot = elRobot;
            probabilidadRobot = probabilidad;
        }

        private void Awake()
        {
            Elegir(Random.value < probabilidadRobot);
            if (!EsRobot)
            {
                ElegirVariante(Random.value, Random.value);
            }
        }

        /// <summary>
        /// T-1389b: con <paramref name="tirada"/> menor que la probabilidad, sale uno de los visuales nuevos (según <paramref name="cual"/>, 0 a 1) en lugar
        /// de la persona. Solo se crea el elegido, así no se cargan todos en cada peatón.
        /// </summary>
        public void ElegirVariante(float tirada, float cual)
        {
            if (variantes == null || variantes.Length == 0 || tirada >= probabilidadVariante || VarianteElegida != null)
            {
                return;
            }
            int indice = Mathf.Min(variantes.Length - 1, Mathf.FloorToInt(Mathf.Clamp01(cual) * variantes.Length));
            if (variantes[indice] == null)
            {
                return;
            }
            VarianteElegida = Instantiate(variantes[indice], transform);
            VarianteElegida.name = "VisualVariante";
            if (persona != null) { persona.SetActive(false); }
            Animator a = VarianteElegida.GetComponentInChildren<Animator>(true);
            if (a != null)
            {
                a.Play(0, 0, Random.value);
            }
        }

        /// <summary>Deja activa solo una de las dos versiones.</summary>
        public void Elegir(bool comoRobot)
        {
            EsRobot = comoRobot && robot != null;
            if (persona != null) { persona.SetActive(!EsRobot); }
            if (robot != null) { robot.SetActive(EsRobot); }
            GameObject activo = EsRobot ? robot : persona;
            Animator a = activo != null ? activo.GetComponentInChildren<Animator>(true) : null;
            if (a != null)
            {
                a.Play(0, 0, Random.value);
            }
        }
    }
}
