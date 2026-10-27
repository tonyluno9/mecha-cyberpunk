using System;

namespace MechaCyberpunk.Economia
{
    /// <summary>Cuánto cuesta reparar el mecha. Función pura que usan el banco del taller y el HUD.</summary>
    public static class ReglaReparacion
    {
        /// <summary>Daño menor a esto (puntos de vida) se considera sin daño y no cuesta nada.</summary>
        public const float DanioMinimo = 0.5f;

        public static int Costo(float danio, int costoPorPunto, int costoMinimo)
        {
            if (danio < DanioMinimo) return 0;
            int porDanio = (int)Math.Ceiling(danio * costoPorPunto);
            return Math.Max(costoMinimo, porDanio);
        }

        /// <summary>Cuántos puntos de vida se reparan con ese dinero (T-149); 0 si no llega al costo mínimo.</summary>
        public static int PuntosPorDinero(int dinero, int costoPorPunto, int costoMinimo)
        {
            if (costoPorPunto <= 0 || dinero <= 0 || dinero < costoMinimo) return 0;
            return dinero / costoPorPunto;
        }

        /// <summary>Menos puntos que esto no se cobran por separado: ya valen el costo mínimo.</summary>
        public static int PuntosMinimos(int costoPorPunto, int costoMinimo)
        {
            if (costoPorPunto <= 0) return 0;
            return (costoMinimo + costoPorPunto - 1) / costoPorPunto;
        }

        /// <summary>La mitad del daño, redondeada hacia arriba (T-149). 0 si el daño no llega al doble del mínimo cobrable.</summary>
        public static int PuntosDeLaMitad(float danio, int costoPorPunto, int costoMinimo)
        {
            int minimos = PuntosMinimos(costoPorPunto, costoMinimo);
            if (danio < DanioMinimo || danio < 2 * Math.Max(1, minimos)) return 0;
            return (int)Math.Ceiling(danio / 2f);
        }
    }
}
