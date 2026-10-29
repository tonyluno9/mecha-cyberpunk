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
        [Tooltip("A qué pandilla pertenece el grupo.")]
        [SerializeField] private IdPandilla idPandilla = IdPandilla.Ninguna;

        private PandilleroAPie[] pandilleros;
        private TurnosPandillaAPie turnos;
        private IRelojMundo reloj;
        private bool retiroPendiente;
        private Vector3 centro;
        private bool alerta;
        private bool jugadorEnInterior;
        private float vacioDesde = -1f;

        public DatosPandilleroAPie Datos => datos;
        public IdPandilla Pandilla => idPandilla;
        /// <summary>Centro del territorio: el promedio de los puntos de los pandilleros.</summary>
        public Vector3 Centro => centro;
        public float RadioTerritorio => datos != null ? datos.RadioTerritorio : 0f;
        /// <summary>True mientras el grupo ataca al jugador por estar en su territorio.</summary>
        public bool EnAlerta => alerta;
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
                centro += p.transform.position / pandilleros.Length;
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
            EventosLugares.JugadorEntro += AlEntrarLugar;
            EventosLugares.JugadorSalio += AlSalirLugar;
            if (datos.Siempre)
            {
                Aparecer();
                return;
            }
            Registro<IRelojMundo>.Agregado += AlAgregarReloj;
            Registro<IRelojMundo>.Quitado += AlQuitarReloj;
            Vincular(Registro<IRelojMundo>.Actual);
        }

        private void OnDisable()
        {
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            EventosLugares.JugadorSalio -= AlSalirLugar;
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

        private void AlEntrarLugar(IdLugar lugar) => jugadorEnInterior = lugar != IdLugar.Ninguno && lugar != IdLugar.Calle;

        private void AlSalirLugar(IdLugar lugar) => jugadorEnInterior = false;

        // Territorio (T-402): ataca si el jugador a pie entra al radio y lo sigue hasta radio + margen. Reaparece a los N s si quedó vacío.
        private void ActualizarTerritorio()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            bool hayJugador = jugador != null && jugador.Raiz != null;
            float distancia = float.MaxValue;
            if (hayJugador)
            {
                Vector3 delta = jugador.Raiz.position - centro;
                delta.y = 0f;
                distancia = delta.magnitude;
            }
            bool valido = hayJugador && jugador.Modo == ModoJugador.APie && !jugadorEnInterior;
            if (!valido)
            {
                alerta = false;
            }
            else if (!alerta && distancia <= datos.RadioTerritorio)
            {
                alerta = true;
            }
            else if (alerta && distancia > datos.RadioTerritorio + datos.MargenPersecucion)
            {
                alerta = false;
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                p.Alerta = alerta;
            }

            if (Presentes > 0)
            {
                vacioDesde = -1f;
                return;
            }
            if (vacioDesde < 0f)
            {
                vacioDesde = Time.time;
            }
            if (Time.time - vacioDesde >= datos.SegundosReaparicion && distancia > datos.DistanciaReaparicion)
            {
                vacioDesde = -1f;
                Aparecer();
            }
        }

        private void Update()
        {
            if (datos.UsaPistola)
            {
                ActualizarTerritorio();
            }
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
            if (datos.CalorAlVencer > 0f)
            {
                Registro<ICalor>.Actual?.Sumar(datos.CalorAlVencer, MotivoCalor.Pelea);
            }
            Debug.Log("[IA] Pandillero vencido: +" + datos.Dinero);
        }
    }
}
