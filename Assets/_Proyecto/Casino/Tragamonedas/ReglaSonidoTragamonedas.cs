namespace MechaCyberpunk.Casino.Tragamonedas
{
    /// <summary>T-1535: qué sonido pide la tragamonedas. Pura, para probarla sin escena.</summary>
    public static class ReglaSonidoTragamonedas
    {
        /// <summary>Palanca, giro y paro de los carretes (5 s).</summary>
        public const string Giro = "CAS_TRAGAMONEDAS";
        public const string PremioChico = "CAS_PREMIO_CHICO";
        public const string PremioGrande = "CAS_PREMIO_GRANDE";
        /// <summary>Fichas que se cambian por dinero o dinero por fichas en la caja.</summary>
        public const string Fichas = "CAS_FICHAS";

        /// <summary>Premio de tantas apuestas o más que cuenta como grande (×). Con los datos base: campana, siete y neón.</summary>
        public const int VecesApuestaPremioGrande = 15;

        /// <summary>El sonido del premio: null si no ganó, el chico por debajo de <paramref name="vecesGrande"/> apuestas, el grande desde ahí.</summary>
        public static string Premio(int premio, int apuesta, int vecesGrande)
        {
            if (premio <= 0) return null;
            return apuesta > 0 && premio >= (long)apuesta * vecesGrande ? PremioGrande : PremioChico;
        }
    }
}
