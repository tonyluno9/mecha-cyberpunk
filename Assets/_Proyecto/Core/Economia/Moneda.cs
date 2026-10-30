using System.Globalization;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// La moneda del juego: Biits (D-tony-250). Está en Core porque la leen áreas que no se ven entre sí (IA, UI, Economia).
    /// Los precios no cambian: 1 $ viejo = Ƀ1.
    /// </summary>
    public static class Moneda
    {
        public const string Nombre = "BIITS";
        public const string Simbolo = "Ƀ";

        /// <summary>"Ƀ0", "Ƀ1,250", "-Ƀ150". Sin decimales, cultura invariante.</summary>
        public static string Texto(int cantidad)
        {
            long magnitud = cantidad < 0 ? -(long)cantidad : cantidad;
            string cifras = magnitud.ToString("N0", CultureInfo.InvariantCulture);
            return (cantidad < 0 ? "-" : "") + Simbolo + cifras;
        }
    }
}
