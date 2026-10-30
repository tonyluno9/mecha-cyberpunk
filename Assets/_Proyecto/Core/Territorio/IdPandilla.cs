namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Las pandillas del juego (D-tony-211). Contrato entre áreas: solo se agregan valores al final, nunca se reordenan
    /// (los guardados y los datos guardan el número).
    /// </summary>
    public enum IdPandilla
    {
        /// <summary>Nadie: sin dueño.</summary>
        Ninguna,
        /// <summary>La pandilla del jugador.</summary>
        Jugador,
        /// <summary>Los Skulls (magenta), centro de la ciudad.</summary>
        Calaveras,
        /// <summary>Los Snakes (verde ácido), este de la ciudad.</summary>
        Viboras
    }
}
