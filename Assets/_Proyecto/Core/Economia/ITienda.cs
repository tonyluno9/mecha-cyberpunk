using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Una tienda de un lugar. La implementa Tony (Economia/, <c>TiendaBasica</c>) y se busca en <c>Registro&lt;ITienda&gt;</c>.
    /// Por ahora nadie la registra en <c>Registro&lt;ITienda&gt;</c>: lo hará la tarea de la fase T-F4/T-F5 de Tony;
    /// no la leas de <c>Registro&lt;ITienda&gt;</c> todavía.
    /// El mundo decide si el lugar está abierto antes de mostrar la tienda; la tienda solo cobra y avisa.
    /// </summary>
    public interface ITienda
    {
        /// <summary>Lugar al que pertenece la tienda.</summary>
        IdLugar Lugar { get; }

        /// <summary>Nombre de la tienda, tal como se muestra al jugador.</summary>
        string Nombre { get; }

        /// <summary>Todo lo que vende, en orden de pantalla.</summary>
        IReadOnlyList<DatosOferta> Ofertas { get; }

        /// <summary>True si la oferta se vende en esta tienda.</summary>
        bool EstaDisponible(DatosOferta oferta);

        /// <summary>
        /// Cobra con la cartera. NoDisponible si es null o no está en la tienda; SinDinero si no alcanza.
        /// Una oferta de precio 0 es gratis: se entrega sin cobrar nada y devuelve Comprado.
        /// </summary>
        ResultadoCompra Comprar(DatosOferta oferta, ICartera cartera);

        /// <summary>Se avisa una vez por cada compra cobrada.</summary>
        event Action<DatosOferta> Comprado;
    }
}
