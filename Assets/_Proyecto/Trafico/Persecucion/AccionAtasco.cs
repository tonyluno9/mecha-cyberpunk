namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T-1006: qué debe hacer una patrulla según el <see cref="DetectorAtasco"/>.</summary>
    public enum AccionAtasco
    {
        /// <summary>Todo bien: sigue manejando.</summary>
        Ninguna,
        /// <summary>No avanza aunque acelera: da reversa con giro y vuelve a intentar.</summary>
        Reversa,
        /// <summary>Ya intentó varias veces o lleva demasiado atorada: hay que ponerla en otro lugar de la calle.</summary>
        Recolocar,
    }
}
