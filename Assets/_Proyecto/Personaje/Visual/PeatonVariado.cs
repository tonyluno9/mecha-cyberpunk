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
