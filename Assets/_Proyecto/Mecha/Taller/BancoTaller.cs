using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Taller
{
    /// <summary>
    /// El banco del taller (T-109): con E se repara el mecha completo pagando según el daño (puede quedar en deuda) o se paga
    /// la renta vencida. Vive en el mecha y solo se ofrece si el mecha arrancó Estacionado, hay un taller y hay cartera.
    /// </summary>
    [RequireComponent(typeof(MechaPersistente))]
    [RequireComponent(typeof(MontajeMecha))]
    public class BancoTaller : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ConfigBancoTaller config;
        [Tooltip("Precios de la reparación. Si está vacío se usan los de la cartera de la escena.")]
        [SerializeField] private DatosEconomia datos;

        private MechaPersistente persistente;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private ITallerMecha taller;
        private bool registrado;
        private bool avisoCarteraDado;

        /// <summary>True si el banco está en Registro&lt;IInteractuable&gt;.</summary>
        public bool Registrado => registrado;

        public Vector3 Posicion => taller != null ? taller.PuntoInteraccion : transform.position;

        public bool PuedeUsar => ReglaBancoTaller.PuedeUsar(Estado);

        public string Texto
        {
            get
            {
                switch (Estado)
                {
                    case EstadoBanco.PagarRenta: return string.Format(config.TextoPagarRenta, FormatoDinero.Texto(Adeudo));
                    case EstadoBanco.Bloqueado: return string.Format(config.TextoBloqueado, FormatoDinero.Texto(Adeudo));
                    case EstadoBanco.MechaFuera: return config.TextoMechaFuera;
                    case EstadoBanco.SinDanio: return config.TextoSinDanio;
                    default: return string.Format(config.TextoReparar, FormatoDinero.Texto(Costo));
                }
            }
        }

        /// <summary>Lo que ofrece el banco ahora. No cambia nada.</summary>
        public EstadoBanco Estado
        {
            get
            {
                ICartera cartera = Registro<ICartera>.Actual;
                return ReglaBancoTaller.Decidir(Adeudo, cartera != null ? cartera.Dinero : 0, MechaEnTaller, persistente.Danio);
            }
        }

        /// <summary>Lo que cuesta reparar ahora ($).</summary>
        public int Costo
        {
            get
            {
                DatosEconomia precios = Precios();
                return precios == null ? 0
                    : ReglaReparacion.Costo(persistente.Danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            }
        }

        private int Adeudo
        {
            get
            {
                IRenta renta = Registro<IRenta>.Actual;
                return renta != null ? renta.Adeudo : 0;
            }
        }

        private bool MechaEnTaller
        {
            get
            {
                if (montaje.Ocupado) return false;
                if (llamado != null) return llamado.Estado == EstadoLlamado.EnTaller;
                if (taller == null) return false;
                Vector3 delta = transform.position - taller.Estacionamiento.position;
                delta.y = 0f;
                return delta.magnitude < config.RadioEnTaller;
            }
        }

        private void Awake()
        {
            persistente = GetComponent<MechaPersistente>();
            montaje = GetComponent<MontajeMecha>();
            llamado = GetComponent<LlamadoMecha>();
            if (config == null)
            {
                Debug.LogError("[Mecha] BancoTaller sin ConfigBancoTaller en " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<ITallerMecha>.Agregado += AlCambiarTaller;
            Registro<ITallerMecha>.Quitado += AlCambiarTaller;
            Registro<ICartera>.Agregado += AlCambiarCartera;
            Registro<ICartera>.Quitado += AlCambiarCartera;
        }

        private void Start()
        {
            Reevaluar();
        }

        private void OnDisable()
        {
            Registro<ITallerMecha>.Agregado -= AlCambiarTaller;
            Registro<ITallerMecha>.Quitado -= AlCambiarTaller;
            Registro<ICartera>.Agregado -= AlCambiarCartera;
            Registro<ICartera>.Quitado -= AlCambiarCartera;
            Quitar();
        }

        private void AlCambiarTaller(ITallerMecha _) => Reevaluar();

        private void AlCambiarCartera(ICartera _) => Reevaluar();

        private void Reevaluar()
        {
            taller = Registro<ITallerMecha>.Actual;
            bool debe = montaje.Estacionado && taller != null;
            if (debe && Registro<ICartera>.Actual == null)
            {
                if (!avisoCarteraDado)
                {
                    avisoCarteraDado = true;
                    Debug.LogWarning("[Mecha] BancoTaller no encontró ICartera; no se ofrece el banco.", this);
                }
                debe = false;
            }
            if (debe && !registrado)
            {
                Registro<IInteractuable>.Agregar(this);
                registrado = true;
            }
            else if (!debe)
            {
                Quitar();
            }
        }

        private void Quitar()
        {
            if (!registrado) return;
            Registro<IInteractuable>.Quitar(this);
            registrado = false;
        }

        private DatosEconomia Precios()
        {
            if (datos != null) return datos;
            ServicioCartera servicio = Registro<ICartera>.Actual as ServicioCartera;
            return servicio != null ? servicio.Datos : null;
        }

        public void Usar()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            if (cartera == null) return;
            switch (Estado)
            {
                case EstadoBanco.PagarRenta:
                    Registro<IRenta>.Actual?.PagarAdeudo();
                    break;
                case EstadoBanco.Reparar:
                    cartera.CargarDeuda(Costo, MotivoDinero.Reparacion);
                    persistente.Reparar();
                    break;
            }
        }
    }
}
