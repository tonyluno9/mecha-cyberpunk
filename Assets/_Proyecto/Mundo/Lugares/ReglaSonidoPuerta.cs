namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// T-1474: qué sonido de puerta toca una hoja. Lógica pura. Primero manda el nombre (reja, cristal, metal, madera); si el nombre no dice nada,
    /// la forma: corrediza = cristal, con bisagra = madera, sube al dintel (cortina o portón) = metal.
    /// </summary>
    public static class ReglaSonidoPuerta
    {
        public const string Madera = "INT_PUERTA_MADERA";
        public const string Metal = "INT_PUERTA_METAL";
        public const string Cristal = "INT_PUERTA_CRISTAL";
        public const string Reja = "INT_PUERTA_REJA";

        private static readonly string[] NombresReja = { "reja", "cancel", "enrejad", "barrote" };
        private static readonly string[] NombresCristal = { "cristal", "vidrio", "glass" };
        private static readonly string[] NombresMetal = { "metal", "acero", "porton", "cortina", "garage", "garaje", "persiana" };
        private static readonly string[] NombresMadera = { "madera", "wood" };

        public static string Id(string nombre, bool conBisagra, bool corrediza)
        {
            string n = string.IsNullOrEmpty(nombre) ? string.Empty : nombre.ToLowerInvariant();
            if (Contiene(n, NombresReja))
            {
                return Reja;
            }
            if (Contiene(n, NombresCristal))
            {
                return Cristal;
            }
            if (Contiene(n, NombresMetal))
            {
                return Metal;
            }
            if (Contiene(n, NombresMadera))
            {
                return Madera;
            }
            if (corrediza)
            {
                return Cristal;
            }
            return conBisagra ? Madera : Metal;
        }

        private static bool Contiene(string texto, string[] fragmentos)
        {
            foreach (string f in fragmentos)
            {
                if (texto.Contains(f))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
