namespace MechaCyberpunk.Combate.Lectura
{
    /// <summary>Un cambio de nivel de una adaptación al terminar una pelea (T-199). Sirve para "APRENDIÓ" y "OLVIDÓ" en pantalla.</summary>
    public readonly struct CambioAdaptacion
    {
        public TipoAdaptacion Tipo { get; }
        public int NivelAntes { get; }
        public int NivelDespues { get; }
        public bool Subio => NivelDespues > NivelAntes;
        public bool Bajo => NivelDespues < NivelAntes;

        public CambioAdaptacion(TipoAdaptacion tipo, int nivelAntes, int nivelDespues)
        {
            Tipo = tipo;
            NivelAntes = nivelAntes;
            NivelDespues = nivelDespues;
        }
    }
}
