using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Una tienda de un lugar. Se busca en <c>Registro&lt;ITienda&gt;</c>.
    /// El mundo decide si el lugar está abierto antes de mostrar la tienda; la tienda solo cobra y avisa.
    /// </summary>
    public interface ITienda
    {
        IdLugar Lugar { get; }
        string Nombre { get; }
        IReadOnlyList<DatosOferta> Ofertas { get; }

        /// <summary>True si la oferta se vende en esta tienda.</summary>
        bool EstaDisponible(DatosOferta oferta);

        /// <summary>Cobra con la cartera. NoDisponible si es null o no está en la tienda; SinDinero si no alcanza.</summary>
        ResultadoCompra Comprar(DatosOferta oferta, ICartera cartera);

        /// <summary>Se avisa una vez por cada compra cobrada.</summary>
        event Action<DatosOferta> Comprado;
    }
}
