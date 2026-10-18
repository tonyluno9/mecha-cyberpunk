namespace MechaCyberpunk.Mecha.Taller
{
    /// <summary>Qué hace una opción del taller de piezas (T-168).</summary>
    public enum TipoOpcionPieza
    {
        /// <summary>Poner una pieza que ya está en el inventario (gratis).</summary>
        Poner,
        /// <summary>Comprar una pieza de la tienda (se pone al instante).</summary>
        Comprar,
        /// <summary>Pieza de la tienda de un nivel que todavía no se abre: no se elige.</summary>
        Bloqueada
    }

    /// <summary>Por qué una opción no se puede elegir (T-168).</summary>
    public enum MotivoNoElegible
    {
        Ninguno,
        NoAlcanza,
        TallerBloqueado,
        NivelBloqueado
    }

    /// <summary>
    /// Una opción del taller de piezas: texto, pieza, precio, si se puede elegir y por qué no, y la diferencia de vida, daño y
    /// velocidad contra la pieza puesta (T-168).
    /// </summary>
    public readonly struct OpcionPieza
    {
        public OpcionPieza(TipoOpcionPieza tipo, string texto, DatosPieza pieza, int precio, bool puedeElegir, MotivoNoElegible motivo,
            float diferenciaVida, float diferenciaDanio, float diferenciaVelocidad)
        {
            Tipo = tipo;
            Texto = texto;
            Pieza = pieza;
            Precio = precio;
            PuedeElegir = puedeElegir;
            Motivo = motivo;
            DiferenciaVida = diferenciaVida;
            DiferenciaDanio = diferenciaDanio;
            DiferenciaVelocidad = diferenciaVelocidad;
        }

        public TipoOpcionPieza Tipo { get; }
        public string Texto { get; }
        public DatosPieza Pieza { get; }
        public int Precio { get; }
        public bool PuedeElegir { get; }
        public MotivoNoElegible Motivo { get; }
        public float DiferenciaVida { get; }
        public float DiferenciaDanio { get; }
        public float DiferenciaVelocidad { get; }
    }
}
