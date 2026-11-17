namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>Qué hace un civil con respecto a una agresión (T-1305).</summary>
    public enum EstadoCivil
    {
        /// <summary>Pasea o se detiene como siempre.</summary>
        Normal,
        /// <summary>Se defiende: se voltea hacia el jugador y le pega si está a su alcance.</summary>
        Revancha,
        /// <summary>Huye asustado.</summary>
        Huyendo
    }
}
