using System.Collections.Generic;

namespace MechaCyberpunk.Economia
{
    /// <summary>Qué puede vender el jugador en un mostrador y cuánto le pagan (C# puro, T-147, D-tony-82).</summary>
    public static class ReglaVenta
    {
        /// <summary>True si el mostrador compra ese tipo de objeto y el objeto vale algo (valorVenta mayor a 0).</summary>
        public static bool PuedeVender(DatosObjeto objeto, IReadOnlyList<TipoObjeto> compra)
        {
            if (objeto == null || compra == null || objeto.ValorVenta <= 0)
            {
                return false;
            }
            for (int i = 0; i < compra.Count; i++)
            {
                if (compra[i] == objeto.Tipo)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Lo que paga el mostrador por uno: el valor de venta, o 0 si no lo compra.</summary>
        public static int Pago(DatosObjeto objeto, IReadOnlyList<TipoObjeto> compra)
        {
            return PuedeVender(objeto, compra) ? objeto.ValorVenta : 0;
        }

        /// <summary>De lo que lleva el jugador, lo que el mostrador compra y que lleva al menos uno, en el mismo orden.</summary>
        public static void Vendibles(IReadOnlyList<DatosObjeto> lleva, ServicioInventario inventario, IReadOnlyList<TipoObjeto> compra,
            List<DatosObjeto> resultado)
        {
            resultado.Clear();
            if (lleva == null || inventario == null)
            {
                return;
            }
            for (int i = 0; i < lleva.Count; i++)
            {
                if (PuedeVender(lleva[i], compra) && inventario.Cantidad(lleva[i]) > 0)
                {
                    resultado.Add(lleva[i]);
                }
            }
        }
    }
}
