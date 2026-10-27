namespace MechaCyberpunk.Mecha.Taller
{
    /// <summary>Qué tanto se repara en el banco del taller (T-149).</summary>
    public enum TipoReparacion
    {
        /// <summary>Todo el daño, fiado: el dinero puede quedar negativo.</summary>
        Todo,
        /// <summary>La mitad del daño (hacia arriba), fiado.</summary>
        Mitad,
        /// <summary>Lo que alcanza con el dinero que tienes, sin deuda.</summary>
        ConLoQueTienes,
        /// <summary>Abre las piezas del mecha (T-168); no repara ni cobra.</summary>
        Piezas
    }

    /// <summary>Una salida de la pantalla del banco: texto, puntos de vida que sube, costo en $ y si se puede elegir.</summary>
    public readonly struct OpcionReparacion
    {
        public OpcionReparacion(TipoReparacion tipo, string texto, float puntos, int costo, bool puedeElegir)
        {
            Tipo = tipo;
            Texto = texto;
            Puntos = puntos;
            Costo = costo;
            PuedeElegir = puedeElegir;
        }

        public TipoReparacion Tipo { get; }
        public string Texto { get; }
        public float Puntos { get; }
        public int Costo { get; }
        public bool PuedeElegir { get; }
    }
}
