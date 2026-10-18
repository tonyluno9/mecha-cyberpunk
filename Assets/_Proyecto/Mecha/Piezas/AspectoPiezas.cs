using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>
    /// Lo que compraste se ve (T-170, greybox): los brazos de la cabina toman el material de la rama de su pieza y encienden de 0 a 4
    /// franjas (una por nivel) en el antebrazo; el marco de la cabina toma el de la pieza de cabina; el cuerpo exterior, el del torso.
    /// Escucha <see cref="EquipoMecha.Cambio"/> y aplica al empezar. Sin EquipoMecha (mecha Directo) no hace nada.
    /// Solo cambia el material compartido de cada renderer: no modifica ningún asset de material.
    /// </summary>
    public class AspectoPiezas : MonoBehaviour
    {
        [SerializeField] private ConfigAspectoPiezas config;
        [Tooltip("Brazo, Antebrazo y Puno del brazo izquierdo de la cabina.")]
        [SerializeField] private Renderer[] partesBrazoIzquierdo;
        [Tooltip("Franjas de nivel del antebrazo izquierdo, de la 1 a la 4.")]
        [SerializeField] private Renderer[] franjasBrazoIzquierdo;
        [Tooltip("Brazo, Antebrazo y Puno del brazo derecho de la cabina.")]
        [SerializeField] private Renderer[] partesBrazoDerecho;
        [Tooltip("Franjas de nivel del antebrazo derecho, de la 1 a la 4.")]
        [SerializeField] private Renderer[] franjasBrazoDerecho;
        [Tooltip("Postes y viga del marco de la cabina.")]
        [SerializeField] private Renderer[] marcoCabina;
        [Tooltip("El cuerpo exterior del mecha.")]
        [SerializeField] private Renderer cuerpo;

        private EquipoMecha equipo;

        private void Awake()
        {
            equipo = GetComponent<EquipoMecha>();
            if (equipo == null)
            {
                enabled = false;
                return;
            }
            if (config == null || !config.EstaCompleta)
            {
                Debug.LogError("[Mecha] AspectoPiezas sin ConfigAspectoPiezas completa en " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            equipo.Cambio += Aplicar;
            Aplicar();
        }

        private void OnDisable()
        {
            if (equipo != null)
            {
                equipo.Cambio -= Aplicar;
            }
        }

        /// <summary>Pone materiales y franjas según las piezas puestas.</summary>
        public void Aplicar()
        {
            DatosPieza izquierdo = equipo.Puesta(RanuraPieza.BrazoIzquierdo);
            DatosPieza derecho = equipo.Puesta(RanuraPieza.BrazoDerecho);
            DatosPieza torso = equipo.Puesta(RanuraPieza.Torso);
            DatosPieza cabina = equipo.Puesta(RanuraPieza.Cabina);

            AplicarBrazo(izquierdo, partesBrazoIzquierdo, franjasBrazoIzquierdo);
            AplicarBrazo(derecho, partesBrazoDerecho, franjasBrazoDerecho);
            Pintar(marcoCabina, config.MarcoDeRama(RamaDe(cabina)));
            if (cuerpo != null)
            {
                cuerpo.sharedMaterial = config.DeRama(RamaDe(torso));
            }
        }

        private void AplicarBrazo(DatosPieza pieza, Renderer[] partes, Renderer[] franjas)
        {
            Pintar(partes, config.DeRama(RamaDe(pieza)));
            int nivel = pieza != null ? pieza.Nivel : 0;
            if (franjas == null)
            {
                return;
            }
            for (int i = 0; i < franjas.Length; i++)
            {
                if (franjas[i] != null)
                {
                    franjas[i].enabled = i < nivel;
                }
            }
        }

        private static RamaPieza RamaDe(DatosPieza pieza)
        {
            return pieza != null ? pieza.Rama : RamaPieza.DeSerie;
        }

        private static void Pintar(Renderer[] partes, Material material)
        {
            if (partes == null)
            {
                return;
            }
            foreach (Renderer parte in partes)
            {
                if (parte != null)
                {
                    parte.sharedMaterial = material;
                }
            }
        }
    }
}
