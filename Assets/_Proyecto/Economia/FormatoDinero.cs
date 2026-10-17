using System.Globalization;

namespace MechaCyberpunk.Economia
{
    /// <summary>Texto del dinero: "$500", "$1,250", "-$150". Sin decimales.</summary>
    public static class FormatoDinero
    {
        public static string Texto(int dinero)
        {
            long magnitud = dinero < 0 ? -(long)dinero : dinero;
            string cifras = magnitud.ToString("N0", CultureInfo.InvariantCulture);
            return (dinero < 0 ? "-$" : "$") + cifras;
        }
    }
}
