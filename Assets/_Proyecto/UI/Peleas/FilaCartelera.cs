namespace MechaCyberpunk.UI.Peleas
{
    /// <summary>Una fila de la pantalla de la cartelera (T-160): su texto, el premio que paga, si se puede elegir y si es la del jefe.</summary>
    public readonly struct FilaCartelera
    {
        public string Texto { get; }
        public int Premio { get; }
        public bool Elegible { get; }
        public bool EsJefe { get; }

        public FilaCartelera(string texto, int premio, bool elegible, bool esJefe)
        {
            Texto = texto;
            Premio = premio;
            Elegible = elegible;
            EsJefe = esJefe;
        }
    }
}
