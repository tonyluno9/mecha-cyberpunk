using MechaCyberpunk.Core.Mundo;
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

        [Header("T-1409: por zona")]
        [Tooltip("Pesos de visuales por zona (Malecon, Puerto, Cerro...). Vacío = lista general de arriba en toda la ciudad.")]
        [SerializeField] private ConfigPeatonesZona configZona;

        public ConfigPeatonesZona ConfigZona => configZona;

        public void ConfigurarZona(ConfigPeatonesZona config)
        {
            configZona = config;
        }

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

        private string nombreVariante;

        /// <summary>T-1533: el nombre del visual que se ve ahora (Visual_NP12_Abuela_a, el de la persona o "robot"); decide la voz de dolor.</summary>
        public string NombreVisual => EsRobot ? "robot" : (VarianteElegida != null ? nombreVariante : (persona != null ? persona.name : string.Empty));
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
            // T-1409: con config por zona, el visual se elige en Start (el grupo coloca al peatón después de crearlo, y antes no se sabe su zona).
            if (!EsRobot && configZona == null)
            {
                ElegirVariante(Random.value, Random.value);
            }
        }

        private void Start()
        {
            if (!EsRobot && configZona != null)
            {
                ElegirPorZona(ZonasMundo.NombreEn(transform.position), Random.value, Random.value, Random.value);
            }
        }

        /// <summary>
        /// T-1409: elige el visual según la zona. Si la zona no está en la config (o no hay zona), queda como siempre (lista general).
        /// </summary>
        public void ElegirPorZona(string zona, float tiradaZona, float tirada, float cual)
        {
            if (VarianteElegida != null)
            {
                return;
            }
            bool hayZona = false;
            GameObject visual = null;
            if (configZona != null)
            {
                visual = configZona.Elegir(zona, tiradaZona, out hayZona);
            }
            if (!hayZona)
            {
                ElegirVariante(tirada, cual);
                return;
            }
            if (visual != null)
            {
                CrearVariante(visual);
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
            CrearVariante(variantes[indice]);
        }

        private void CrearVariante(GameObject visual)
        {
            nombreVariante = visual.name;
            VarianteElegida = Instantiate(visual, transform);
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
