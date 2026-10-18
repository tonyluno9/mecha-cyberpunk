using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Montaje;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Taller
{
    /// <summary>Lo que pasó al elegir una opción del taller de piezas (T-168).</summary>
    public readonly struct ResultadoTallerPiezas
    {
        public ResultadoTallerPiezas(bool hecho, DatosPieza quitada, int vendida)
        {
            Hecho = hecho;
            Quitada = quitada;
            Vendida = vendida;
        }

        /// <summary>True si se cambió la pieza.</summary>
        public bool Hecho { get; }
        /// <summary>La pieza que se quitó del mecha (null si no hubo cambio).</summary>
        public DatosPieza Quitada { get; }
        /// <summary>$ pagados por la pieza quitada cuando no cupo en el inventario (0 si entró al inventario).</summary>
        public int Vendida { get; }
        public bool SeVendio => Vendida > 0;
    }

    /// <summary>
    /// Comprar piezas y cambiarlas por las del inventario en el banco del taller (T-168). Va en el mecha estacionado, junto al
    /// <see cref="BancoTaller"/> y el <see cref="EquipoMecha"/>. Solo actúa con el mecha en el taller, vacío y con la renta al corriente.
    /// </summary>
    [RequireComponent(typeof(EquipoMecha))]
    [RequireComponent(typeof(BancoTaller))]
    [RequireComponent(typeof(MontajeMecha))]
    public class TallerPiezas : MonoBehaviour
    {
        private EquipoMecha equipo;
        private BancoTaller banco;
        private MontajeMecha montaje;

        private void Awake()
        {
            Resolver();
        }

        private void Resolver()
        {
            if (equipo == null) equipo = GetComponent<EquipoMecha>();
            if (banco == null) banco = GetComponent<BancoTaller>();
            if (montaje == null) montaje = GetComponent<MontajeMecha>();
        }

        /// <summary>Liga actual; sin servicio de ligas registrado es 1.</summary>
        public static int LigaActual
        {
            get
            {
                IProgresoLigas ligas = Registro<IProgresoLigas>.Actual;
                return ligas != null ? ligas.LigaActual : 1;
            }
        }

        /// <summary>True si se pueden mostrar las piezas: mecha en el taller, vacío, banco activo y con catálogo.</summary>
        public bool Disponible
        {
            get
            {
                Resolver();
                return banco != null && banco.enabled && montaje != null && !montaje.Ocupado && banco.EnElTaller
                    && equipo != null && equipo.Catalogo != null && equipo.Puestas.Count > 0;
            }
        }

        /// <summary>True si la renta vencida bloquea el taller.</summary>
        public bool Bloqueado => banco != null && banco.Adeudo > 0;

        /// <summary>Las opciones para la ranura (vacío si no está disponible). No cambia nada.</summary>
        public List<OpcionPieza> Opciones(RanuraPieza ranura)
        {
            if (!Disponible) return new List<OpcionPieza>();
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            var delInventario = new List<DatosPieza>();
            if (inventario != null)
            {
                foreach (DatosObjeto o in inventario.Todos)
                {
                    if (o is DatosPieza p) delInventario.Add(p);
                }
            }
            return ReglaTallerPiezas.Opciones(ranura, equipo.Puesta(ranura), delInventario, equipo.Catalogo.De(ranura), LigaActual,
                cartera != null ? cartera.Dinero : 0, Bloqueado);
        }

        /// <summary>
        /// Cobra (si es compra) y pone la pieza. No hace nada si la opción ya no es válida, si no alcanza el dinero o si falla
        /// quitarla del inventario. La pieza que se quita entra al inventario; si no cabe, se paga su valor de venta.
        /// </summary>
        public ResultadoTallerPiezas Elegir(OpcionPieza opcion)
        {
            var nada = new ResultadoTallerPiezas(false, null, 0);
            if (opcion.Pieza == null || !opcion.PuedeElegir) return nada;
            bool vigente = false;
            foreach (OpcionPieza actual in Opciones(opcion.Pieza.Ranura))
            {
                if (actual.Pieza == opcion.Pieza && actual.Tipo == opcion.Tipo && actual.PuedeElegir)
                {
                    vigente = true;
                    break;
                }
            }
            if (!vigente) return nada;

            ICartera cartera = Registro<ICartera>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            switch (opcion.Tipo)
            {
                case TipoOpcionPieza.Comprar:
                    if (cartera == null || !cartera.IntentarPagar(opcion.Precio, MotivoDinero.Compra)) return nada;
                    break;
                case TipoOpcionPieza.Poner:
                    if (inventario == null || !inventario.Quitar(opcion.Pieza, 1)) return nada;
                    break;
                default:
                    return nada;
            }

            DatosPieza quitada = equipo.Equipar(opcion.Pieza);
            int vendida = 0;
            if (quitada != null)
            {
                bool entro = inventario != null && inventario.Agregar(quitada, 1) > 0;
                if (!entro && cartera != null && quitada.ValorVenta > 0)
                {
                    cartera.Abonar(quitada.ValorVenta, MotivoDinero.Venta);
                    vendida = quitada.ValorVenta;
                }
            }
            return new ResultadoTallerPiezas(true, quitada, vendida);
        }
    }
}
