using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Un grupo de pandilleros a pie alrededor de su punto (T-195, D-tony-169). Solo de noche: aparecen al anochecer y se retiran al
    /// amanecer cuando nadie pelea. Reparte los turnos de ataque (como máximo N a la vez). Cada vencido suelta su dinero y vuelve la
    /// noche siguiente. Los pandilleros son hijos del prefab; sus puntos son donde están al arrancar.
    /// </summary>
    public class PandillaAPie : MonoBehaviour
    {
        [SerializeField] private DatosPandilleroAPie datos;

        private PandilleroAPie[] pandilleros;
        private TurnosPandillaAPie turnos;
        private IRelojMundo reloj;
        private bool retiroPendiente;

        public DatosPandilleroAPie Datos => datos;
        public TurnosPandillaAPie Turnos => turnos;
        public PandilleroAPie[] Pandilleros => pandilleros;

        /// <summary>Cuántos pandilleros hay en pie ahora.</summary>
        public int Presentes
        {
            get
            {
                int cuenta = 0;
                foreach (PandilleroAPie p in pandilleros)
                {
                    if (p.gameObject.activeSelf && !p.EstaKO)
                    {
                        cuenta++;
                    }
                }
                return cuenta;
            }
        }

        private void Awake()
        {
            pandilleros = GetComponentsInChildren<PandilleroAPie>(true);
            if (datos == null || pandilleros.Length == 0)
            {
                Debug.LogError("[IA] Faltan referencias en PandillaAPie de " + name, this);
                enabled = false;
                return;
            }
            turnos = new TurnosPandillaAPie(datos.MaxAtacando);
            foreach (PandilleroAPie p in pandilleros)
            {
                p.Configurar(p.transform.position, turnos);
                p.Vencido += AlVencerUno;
                p.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (!enabled)
            {
                return;
            }
            Registro<IRelojMundo>.Agregado += AlAgregarReloj;
            Registro<IRelojMundo>.Quitado += AlQuitarReloj;
            Vincular(Registro<IRelojMundo>.Actual);
        }

        private void OnDisable()
        {
            Registro<IRelojMundo>.Agregado -= AlAgregarReloj;
            Registro<IRelojMundo>.Quitado -= AlQuitarReloj;
            Desvincular();
        }

        private void OnDestroy()
        {
            if (pandilleros == null)
            {
                return;
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                if (p != null)
                {
                    p.Vencido -= AlVencerUno;
                }
            }
        }

        private void Update()
        {
            if (!retiroPendiente)
            {
                return;
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                if (p.gameObject.activeSelf && p.EnPelea)
                {
                    return;
                }
            }
            retiroPendiente = false;
            Retirar();
        }

        private void AlAgregarReloj(IRelojMundo nuevo) => Vincular(nuevo);

        private void AlQuitarReloj(IRelojMundo quitado)
        {
            if (ReferenceEquals(quitado, reloj))
            {
                Desvincular();
                Vincular(Registro<IRelojMundo>.Actual);
            }
        }

        private void Vincular(IRelojMundo nuevo)
        {
            if (nuevo == null || ReferenceEquals(nuevo, reloj))
            {
                return;
            }
            Desvincular();
            reloj = nuevo;
            reloj.FranjaCambio += AlCambiarFranja;
            if (reloj.EsDeNoche)
            {
                Aparecer();
            }
        }

        private void Desvincular()
        {
            if (reloj != null)
            {
                reloj.FranjaCambio -= AlCambiarFranja;
                reloj = null;
            }
        }

        private void AlCambiarFranja(FranjaDia franja)
        {
            if (franja == FranjaDia.Noche)
            {
                retiroPendiente = false;
                Aparecer();
            }
            else
            {
                retiroPendiente = true;
            }
        }

        private void Aparecer()
        {
            foreach (PandilleroAPie p in pandilleros)
            {
                // Un pandillero que sigue en pie de la noche anterior no se reinicia a media pelea.
                if (p.gameObject.activeSelf && !p.EstaKO)
                {
                    continue;
                }
                p.gameObject.SetActive(true);
                p.Reaparecer();
            }
        }

        private void Retirar()
        {
            foreach (PandilleroAPie p in pandilleros)
            {
                turnos.Soltar(p);
                p.gameObject.SetActive(false);
            }
        }

        private void AlVencerUno(PandilleroAPie vencido)
        {
            ICartera cartera = Registro<ICartera>.Actual;
            cartera?.Abonar(datos.Dinero, MotivoDinero.Otro);
            Debug.Log("[IA] Pandillero vencido: +" + datos.Dinero);
        }
    }
}
