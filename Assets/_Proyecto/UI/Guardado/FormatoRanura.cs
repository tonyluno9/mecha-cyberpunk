using System.Globalization;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;

namespace MechaCyberpunk.UI.Guardado
{
    /// <summary>
    /// Los textos de una ranura (T-412): "RANURA 2 · 29 OCT 21:14 · EL TIANGUIS · Ƀ340", "AUTOGUARDADO · ..." o "RANURA 1 · VACÍA".
    /// </summary>
    public static class FormatoRanura
    {
        public const string Auto = "AUTOGUARDADO";
        public const string Ranura = "RANURA ";
        public const string Vacia = "VACÍA";
        public const string Separador = " · ";
        private static readonly string[] Meses = { "ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO", "SEP", "OCT", "NOV", "DIC" };

        public static string Nombre(int ranura) => ranura == ServicioGuardado.RanuraAuto ? Auto : Ranura + ranura;

        /// <summary>"29 OCT 21:14".</summary>
        public static string Fecha(System.DateTime fecha)
        {
            return fecha.Day.ToString(CultureInfo.InvariantCulture) + " " + Meses[fecha.Month - 1] + " " + fecha.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>La línea de arriba de la ranura.</summary>
        public static string Linea(ResumenRanura resumen)
        {
            string nombre = Nombre(resumen.Ranura);
            if (resumen.Vacia)
            {
                return nombre + Separador + Vacia;
            }
            string zona = string.IsNullOrEmpty(resumen.Zona) ? string.Empty : Separador + resumen.Zona;
            return nombre + Separador + Fecha(resumen.Fecha) + zona + Separador + Moneda.Texto(resumen.Dinero);
        }
    }
}
