namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Qué está haciendo el jugador respecto a trepar.</summary>
    public enum EstadoTrepar
    {
        Libre,
        EnEscalera,
        Agarre,
        /// <summary>T-1628: colgado de un borde con las manos (espera subir o soltarse).</summary>
        Colgado
    }
}
