using MechaCyberpunk.Casino.Fichas;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Casino.Cajera
{
    /// <summary>
    /// La cajera del casino (T-302). Ofrece dos cosas con E, en dos puntos del mostrador: "Cambiar $100 por fichas" y "Cobrar fichas".
    /// El jugador usa la más cercana. El cambio lo hace <see cref="ServicioFichas"/> con <see cref="ICartera"/> y <see cref="MotivoDinero.Casino"/>.
    /// </summary>
    public class CajeraMundo : MonoBehaviour
    {
        [Tooltip("Dónde se para el jugador para cambiar dinero por fichas (m, relativo a la cajera).")]
        [SerializeField] private Vector3 puntoCambiar = new Vector3(-0.5f, 0f, 0.9f);
        [Tooltip("Dónde se para el jugador para cobrar fichas (m, relativo a la cajera).")]
        [SerializeField] private Vector3 puntoCobrar = new Vector3(0.5f, 0f, 0.9f);
        [SerializeField] private string textoCambiar = "Cambiar ${0} por {1} fichas";
        [SerializeField] private string textoSinDinero = "Sin dinero · ${0} por {1} fichas";
        [SerializeField] private string textoCobrar = "Cobrar {0} fichas · ${1}";
        [SerializeField] private string textoSinFichas = "Sin fichas para cobrar";

        private ICartera carteraInyectada;
        private ServicioFichas fichasInyectadas;
        private OpcionCajera cambiar;
        private OpcionCajera cobrar;

        private ICartera Cartera => carteraInyectada ?? Registro<ICartera>.Actual;
        private ServicioFichas Servicio => fichasInyectadas != null ? fichasInyectadas : Registro<IFichas>.Actual as ServicioFichas;

        public IInteractuable OpcionCambiar => cambiar;
        public IInteractuable OpcionCobrar => cobrar;

        /// <summary>Para pruebas y escenas sin servicios: usa esta cartera y estas fichas en vez de las registradas.</summary>
        public void Inyectar(ICartera cartera, ServicioFichas fichas)
        {
            carteraInyectada = cartera;
            fichasInyectadas = fichas;
        }

        private void Awake()
        {
            cambiar = new OpcionCajera(this, true);
            cobrar = new OpcionCajera(this, false);
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(cambiar);
            Registro<IInteractuable>.Agregar(cobrar);
        }

        private void OnDisable()
        {
            Registro<IInteractuable>.Quitar(cambiar);
            Registro<IInteractuable>.Quitar(cobrar);
        }

        private sealed class OpcionCajera : IInteractuable
        {
            private readonly CajeraMundo cajera;
            private readonly bool esCambiar;

            public OpcionCajera(CajeraMundo cajera, bool esCambiar)
            {
                this.cajera = cajera;
                this.esCambiar = esCambiar;
            }

            public Vector3 Posicion => cajera.transform.TransformPoint(esCambiar ? cajera.puntoCambiar : cajera.puntoCobrar);

            public bool PuedeUsar
            {
                get
                {
                    ServicioFichas servicio = cajera.Servicio;
                    if (servicio == null || servicio.Datos == null)
                    {
                        return false;
                    }
                    return esCambiar ? servicio.PuedeComprar(cajera.Cartera) : servicio.LotesCobrables > 0;
                }
            }

            public string Texto
            {
                get
                {
                    ServicioFichas servicio = cajera.Servicio;
                    if (servicio == null || servicio.Datos == null)
                    {
                        return string.Empty;
                    }
                    DatosFichas d = servicio.Datos;
                    if (esCambiar)
                    {
                        return string.Format(servicio.PuedeComprar(cajera.Cartera) ? cajera.textoCambiar : cajera.textoSinDinero, d.DineroPorLote, d.FichasPorLote);
                    }
                    int lotes = servicio.LotesCobrables;
                    return lotes > 0 ? string.Format(cajera.textoCobrar, lotes * d.FichasPorLote, lotes * d.DineroPorLote) : cajera.textoSinFichas;
                }
            }

            public void Usar()
            {
                ServicioFichas servicio = cajera.Servicio;
                if (servicio == null || !PuedeUsar)
                {
                    return;
                }
                if (esCambiar)
                {
                    servicio.Comprar(cajera.Cartera);
                }
                else
                {
                    servicio.Cobrar(cajera.Cartera);
                }
            }
        }
    }
}
