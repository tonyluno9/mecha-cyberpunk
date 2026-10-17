using System;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
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
    [RequireComponent(typeof(MontajeMecha))]
    public class BancoTaller : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ConfigBancoTaller config;
        [Tooltip("Precios de la reparación. Si está vacío se usan los de la cartera de la escena.")]
        [SerializeField] private DatosEconomia datos;

        private MechaPersistente persistente;
        private VidaMecha vidaMecha;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private ITallerMecha taller;
        private bool registrado;
        private bool avisoCarteraDado;

        /// <summary>True si el banco está en Registro&lt;IInteractuable&gt;.</summary>
        public bool Registrado => registrado;

        /// <summary>
        /// Avisa las opciones de reparación cuando se usa el banco con el estado Reparar (T-149). Si nadie escucha
        /// (no hay pantalla en la escena), el banco repara todo directamente.
        /// </summary>
        public event Action<IReadOnlyList<OpcionReparacion>> OpcionesPedidas;

        /// <summary>Vida actual del mecha (puntos).</summary>
        public float VidaActual => vidaMecha != null ? vidaMecha.Actual : 0f;

        /// <summary>Vida máxima del mecha (puntos).</summary>
        public float VidaMaxima => vidaMecha != null ? vidaMecha.Maxima : 0f;

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
            if (persistente == null)
            {
                // Mecha de arranque Directo: no guarda daño, no hay nada que reparar. No ofrece el banco.
                enabled = false;
                return;
            }
            montaje = GetComponent<MontajeMecha>();
            vidaMecha = GetComponent<VidaMecha>();
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
                    IReadOnlyList<OpcionReparacion> opciones = CalcularOpciones();
                    if (opciones.Count == 0) break;
                    if (OpcionesPedidas == null)
                    {
                        Elegir(opciones[0]);
                        break;
                    }
                    OpcionesPedidas.Invoke(opciones);
                    break;
            }
        }

        /// <summary>
        /// Las opciones de reparación ahora (T-149, D-tony-84): todo (fiado), la mitad del daño (fiado, solo si el daño es al
        /// menos el doble del mínimo cobrable) y lo que alcanza con el dinero (solo con dinero positivo menor al costo total).
        /// La primera siempre es "todo". Vacío si no hay nada que reparar.
        /// </summary>
        public IReadOnlyList<OpcionReparacion> CalcularOpciones()
        {
            List<OpcionReparacion> opciones = new List<OpcionReparacion>();
            DatosEconomia precios = Precios();
            ICartera cartera = Registro<ICartera>.Actual;
            float danio = persistente.Danio;
            if (precios == null || cartera == null || danio < ReglaReparacion.DanioMinimo) return opciones;

            int porPunto = precios.CostoReparacionPorPunto;
            int minimo = precios.CostoReparacionMinimo;
            int costoTodo = ReglaReparacion.Costo(danio, porPunto, minimo);
            opciones.Add(new OpcionReparacion(TipoReparacion.Todo, string.Format(config.FormatoOpcionTodo, FormatoDinero.Texto(costoTodo)),
                danio, costoTodo, true));

            int puntosMitad = ReglaReparacion.PuntosDeLaMitad(danio, porPunto, minimo);
            if (puntosMitad > 0)
            {
                int costoMitad = ReglaReparacion.Costo(puntosMitad, porPunto, minimo);
                opciones.Add(new OpcionReparacion(TipoReparacion.Mitad,
                    string.Format(config.FormatoOpcionMitad, FormatoDinero.Texto(costoMitad)), puntosMitad, costoMitad, true));
            }

            int dinero = cartera.Dinero;
            if (dinero > 0 && dinero < costoTodo)
            {
                int puntosAlcanza = ReglaReparacion.PuntosPorDinero(dinero, porPunto, minimo);
                if (puntosAlcanza > 0)
                {
                    int costoAlcanza = ReglaReparacion.Costo(puntosAlcanza, porPunto, minimo);
                    opciones.Add(new OpcionReparacion(TipoReparacion.ConLoQueTienes,
                        string.Format(config.FormatoOpcionConLoQueTienes, FormatoDinero.Texto(costoAlcanza), puntosAlcanza),
                        puntosAlcanza, costoAlcanza, true));
                }
            }
            return opciones;
        }

        /// <summary>
        /// Cobra y repara según la opción (T-149): todo y mitad con <c>CargarDeuda</c>; "con lo que tienes" con
        /// <c>IntentarPagar</c>. Devuelve false (sin reparar) si el banco ya no está en Reparar o no alcanzó el dinero.
        /// </summary>
        public bool Elegir(OpcionReparacion opcion)
        {
            ICartera cartera = Registro<ICartera>.Actual;
            if (cartera == null || !opcion.PuedeElegir || Estado != EstadoBanco.Reparar) return false;
            switch (opcion.Tipo)
            {
                case TipoReparacion.Todo:
                    cartera.CargarDeuda(opcion.Costo, MotivoDinero.Reparacion);
                    persistente.Reparar();
                    return true;
                case TipoReparacion.Mitad:
                    cartera.CargarDeuda(opcion.Costo, MotivoDinero.Reparacion);
                    persistente.RepararPuntos(opcion.Puntos);
                    return true;
                case TipoReparacion.ConLoQueTienes:
                    if (!cartera.IntentarPagar(opcion.Costo, MotivoDinero.Reparacion)) return false;
                    persistente.RepararPuntos(opcion.Puntos);
                    return true;
                default:
                    return false;
            }
        }
    }
}
