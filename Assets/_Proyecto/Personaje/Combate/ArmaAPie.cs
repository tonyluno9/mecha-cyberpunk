namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Arma que el jugador a pie tiene en la mano. T-1013: las nuevas van al final (los assets y la reserva de balas guardan el número):
    /// subfusil, revólver, lanzagranadas, granada (se lanza) y rifle de plasma.
    /// </summary>
    public enum ArmaAPie
    {
        Punos,
        Pistola,
        Escopeta,
        Fusil,
        Sniper,
        Subfusil,
        Revolver,
        Lanzagranadas,
        Granada,
        RiflePlasma
    }
}

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1014: reglas del tipo de arma para la animación y el agarre.</summary>
    public static class ReglasArmaAPie
    {
        /// <summary>El arma se maneja con la pose de pistola (una mano, se saca y se guarda): pistola, revólver y granada. Las demás armas nuevas y las viejas usan la pose de armas largas.</summary>
        public static bool UsaPoseDePistola(ArmaAPie arma) => arma == ArmaAPie.Pistola || arma == ArmaAPie.Revolver || arma == ArmaAPie.Granada;
    }
}
