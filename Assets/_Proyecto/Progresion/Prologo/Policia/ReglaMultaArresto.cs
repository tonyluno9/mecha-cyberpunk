using System;

namespace MechaCyberpunk.Progresion.Prologo.Policia
{
    /// <summary>La multa del arresto (T-690): una parte del dinero que traes. Sin dinero (o con deuda) no pierdes nada.</summary>
    public static class ReglaMultaArresto
    {
        /// <summary>Porcentaje por defecto: el 25 % de tu dinero.</summary>
        public const float PorcentajePorDefecto = 0.25f;

        /// <summary>Redondeada hacia abajo, nunca mayor que el dinero ni negativa.</summary>
        public static int Multa(int dinero, float porcentaje)
        {
            if (dinero <= 0 || porcentaje <= 0f)
            {
                return 0;
            }
            int multa = (int)Math.Floor(dinero * (double)Math.Min(1f, porcentaje));
            return Math.Max(0, Math.Min(dinero, multa));
        }
    }
}
