namespace MechaCyberpunk.Progresion
{
    /// <summary>La opción que se ofrece en un espacio: el implante, su estado, lo que costaría y la liga que lo abre (T-191).</summary>
    public readonly struct OfertaImplante
    {
        /// <summary>El implante ofrecido; null si ya tienes lo mejor.</summary>
        public readonly DatosImplante Implante;
        public readonly EstadoOfertaImplante Estado;
        /// <summary>Lo que se cobraría ($): el precio, o la diferencia si ya tienes el nivel anterior.</summary>
        public readonly int Costo;
        /// <summary>La liga que lo abre (solo con LigaBaja; 0 en los demás estados).</summary>
        public readonly int LigaQueAbre;

        public OfertaImplante(DatosImplante implante, EstadoOfertaImplante estado, int costo, int ligaQueAbre)
        {
            Implante = implante;
            Estado = estado;
            Costo = costo;
            LigaQueAbre = ligaQueAbre;
        }
    }
}
