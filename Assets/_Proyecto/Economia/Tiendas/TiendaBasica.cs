using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Economia
{
    /// <summary>Tienda de lógica pura construida con un <see cref="DatosTienda"/>.</summary>
    public class TiendaBasica : ITienda
    {
        private const int DecimalesRebaja = 4;
        private readonly DatosTienda datos;

        public TiendaBasica(DatosTienda datos)
        {
            this.datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        public IdLugar Lugar => datos.Lugar;
        public string Nombre => datos.Nombre;
        public IReadOnlyList<DatosOferta> Ofertas => datos.Ofertas;

        public event Action<DatosOferta> Comprado;

        public bool EstaDisponible(DatosOferta oferta)
        {
            if (oferta == null) return false;
            foreach (var o in datos.Ofertas)
                if (o == oferta) return true;
            return false;
        }

        /// <summary>
        /// El precio con una rebaja de 0 a 1 (0.3 = 30 %): redondea al peso más cercano y un precio mayor que 0 nunca baja de $1.
        /// Un precio de 0 sigue siendo 0.
        /// </summary>
        public static int PrecioCon(DatosOferta oferta, float rebaja)
        {
            if (oferta == null) throw new ArgumentNullException(nameof(oferta));
            if (oferta.Precio <= 0) return 0;
            double fraccion = Math.Round(Math.Min(1.0, Math.Max(0.0, rebaja)), DecimalesRebaja);
            int precio = (int)Math.Round(oferta.Precio * (1.0 - fraccion), MidpointRounding.AwayFromZero);
            return Math.Max(1, precio);
        }

        public ResultadoCompra Comprar(DatosOferta oferta, ICartera cartera) => Comprar(oferta, cartera, 0f);

        /// <summary>Compra cobrando el precio rebajado (<see cref="PrecioCon"/>).</summary>
        public ResultadoCompra Comprar(DatosOferta oferta, ICartera cartera, float rebaja)
        {
            if (!EstaDisponible(oferta) || cartera == null) return ResultadoCompra.NoDisponible;

            var motivo = datos.Lugar == IdLugar.Clinica ? MotivoDinero.Clinica : MotivoDinero.Compra;
            int precio = PrecioCon(oferta, rebaja);
            // Precio 0 es gratis: no se cobra (IntentarPagar rechaza cantidades menores o iguales a 0).
            if (precio > 0 && !cartera.IntentarPagar(precio, motivo)) return ResultadoCompra.SinDinero;

            Comprado?.Invoke(oferta);
            return ResultadoCompra.Comprado;
        }
    }
}
