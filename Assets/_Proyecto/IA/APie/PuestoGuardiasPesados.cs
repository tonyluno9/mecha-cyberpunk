using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1109: la raíz del puesto de guardias pesados de la torre de Nova. Guarda a los dos guardias para que el recinto del jefe final (T-1110)
    /// los provoque cuando el jugador entra, los vuelva a su puesto si se resetea y los apague cuando el jefe cae.
    /// </summary>
    public class PuestoGuardiasPesados : MonoBehaviour
    {
        [Tooltip("Los guardias pesados del puesto (uno a cada lado de la entrada de la torre).")]
        [SerializeField] private ConductaGuardiaPesado[] guardias;

        public ConductaGuardiaPesado[] Guardias => guardias;

        /// <summary>Provoca a todos los guardias en pie.</summary>
        public void ProvocarATodos()
        {
            foreach (ConductaGuardiaPesado g in guardias)
            {
                if (g != null && g.gameObject.activeInHierarchy)
                {
                    g.Provocar();
                }
            }
        }
    }
}
