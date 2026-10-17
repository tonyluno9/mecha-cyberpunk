using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Mecha.Danio;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Cabina
{
    /// <summary>
    /// Cuartea el cristal de la cabina según la vida: el grupo Grietas1 desde el nivel 1 y Grietas2 (más el 1) en el nivel 2.
    /// Se actualiza con el aviso Cambio de VidaMecha.
    /// </summary>
    public class CristalCuarteado : MonoBehaviour
    {
        [SerializeField] private ConfigMechaDaniado config;
        [SerializeField] private GameObject grietas1;
        [SerializeField] private GameObject grietas2;

        private VidaMecha vida;

        /// <summary>0 sin grietas, 1 grietas leves, 2 grietas fuertes.</summary>
        public int Nivel { get; private set; }

        private void Awake()
        {
            vida = GetComponentInParent<VidaMecha>();
            if (config == null || grietas1 == null || grietas2 == null || vida == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en CristalCuarteado de " + name, this);
                enabled = false;
                return;
            }
            Aplicar(0);
        }

        private void OnEnable()
        {
            if (vida == null)
            {
                return;
            }
            vida.Cambio += Reevaluar;
            Reevaluar();
        }

        private void OnDisable()
        {
            if (vida != null)
            {
                vida.Cambio -= Reevaluar;
            }
        }

        private void Reevaluar()
        {
            Aplicar(ReglaMechaDaniado.NivelGrietas(vida.Actual, vida.Maxima, config.UmbralGrietas1, config.UmbralGrietas2));
        }

        private void Aplicar(int nivel)
        {
            Nivel = nivel;
            grietas1.SetActive(nivel >= 1);
            grietas2.SetActive(nivel >= 2);
        }
    }
}
