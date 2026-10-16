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
    }
}
