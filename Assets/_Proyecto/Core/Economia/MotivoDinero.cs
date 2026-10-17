namespace MechaCyberpunk.Core
{
    /// <summary>Por qué cambió el dinero del jugador. Viaja con cada movimiento de la cartera.</summary>
    public enum MotivoDinero
    {
        Premio,
        Apuesta,
        Reparacion,
        Renta,
        Compra,
        Venta,
        Casino,
        Clinica,
        Otro,
        /// <summary>El dinero cambió porque se cargó una partida; no es un movimiento real y el HUD no lo anuncia.</summary>
        Carga
    }
}
