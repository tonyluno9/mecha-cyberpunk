namespace MechaCyberpunk.Mecha.Taller
{
    /// <summary>Lo que ofrece el banco del taller en este momento.</summary>
    public enum EstadoBanco
    {
        /// <summary>Hay renta vencida y el dinero alcanza: se puede pagar.</summary>
        PagarRenta,
        /// <summary>Hay renta vencida y el dinero no alcanza: el taller está bloqueado.</summary>
        Bloqueado,
        /// <summary>El mecha no está en el taller.</summary>
        MechaFuera,
        /// <summary>El mecha no tiene daño que reparar.</summary>
        SinDanio,
        /// <summary>Se puede reparar (aunque el dinero no alcance: queda en deuda).</summary>
        Reparar,
        /// <summary>Sin daño ni renta vencida y con taller de piezas: se pueden comprar y cambiar piezas (T-168).</summary>
        Piezas
    }
}
