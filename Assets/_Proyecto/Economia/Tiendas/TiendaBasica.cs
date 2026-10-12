using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Economia
{
    /// <summary>Tienda de lógica pura construida con un <see cref="DatosTienda"/>.</summary>
    public class TiendaBasica : ITienda
    {
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

        public ResultadoCompra Comprar(DatosOferta oferta, ICartera cartera)
        {
            if (!EstaDisponible(oferta) || cartera == null) return ResultadoCompra.NoDisponible;

            var motivo = datos.Lugar == IdLugar.Clinica ? MotivoDinero.Clinica : MotivoDinero.Compra;
            // Precio 0 es gratis: no se cobra (IntentarPagar rechaza cantidades menores o iguales a 0).
            if (oferta.Precio > 0 && !cartera.IntentarPagar(oferta.Precio, motivo)) return ResultadoCompra.SinDinero;

            Comprado?.Invoke(oferta);
            return ResultadoCompra.Comprado;
        }
    }
}
