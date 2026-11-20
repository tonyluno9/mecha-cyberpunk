using MechaCyberpunk.Core;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Qué hace una pulsación de la tecla de una ranura de consumibles.</summary>
    public enum AccionRanura
    {
        Nada,
        Usar,
        Cambiar
    }

    /// <summary>T-1492: reglas puras del uso de las dos ranuras de consumibles y de sus choques de teclas.</summary>
    public static class ReglaUsoConsumibles
    {
        /// <summary>
        /// Decide qué hace la pulsación. <paramref name="ranura"/> 0 es la de E y 1 la de C (a pie) o la de Z y X (en carro). Con Alt apretado cambia el
        /// objeto de la ranura en vez de usarlo. La ranura de E no hace nada si hay algo con qué interactuar enfrente (esa E es de interactuar): así no se
        /// gasta un objeto al abrir una puerta ni se sube a un carro con la bebida en la mano. Solo a pie o en carro.
        /// </summary>
        public static AccionRanura Decidir(int ranura, bool pulsada, bool alt, ModoJugador modo, bool hayInteraccion)
        {
            if (!pulsada || (modo != ModoJugador.APie && modo != ModoJugador.Carro))
            {
                return AccionRanura.Nada;
            }
            if (modo == ModoJugador.APie && ranura == 0 && hayInteraccion)
            {
                return AccionRanura.Nada;
            }
            return alt ? AccionRanura.Cambiar : AccionRanura.Usar;
        }
    }
}
