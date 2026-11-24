using System;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR2: reglas del golpe con la mano (lógica pura). La mano pega si va a más de <see cref="VelocidadMinima"/> m/s (sin contar lo que camina el
    /// jugador); el daño del puño se multiplica por la rapidez: x1 hasta <see cref="VelocidadPlena"/> m/s y sube hasta x<see cref="FactorMaximo"/>.
    /// Cada mano espera <see cref="SegundosEnfriamiento"/> s entre golpes.
    /// </summary>
    public static class ReglaGolpeManoVR
    {
        public const float VelocidadMinima = 1.5f;
        public const float VelocidadPlena = 3f;
        public const float FactorMaximo = 2f;
        public const float SegundosEnfriamiento = 0.35f;

        /// <summary>True si a esa rapidez (m/s) la mano golpea.</summary>
        public static bool Golpea(float velocidad) => velocidad > VelocidadMinima;

        /// <summary>Multiplicador del daño del puño a esa rapidez: clamp(v / 3, 1, 2).</summary>
        public static float Factor(float velocidad) => Math.Max(1f, Math.Min(FactorMaximo, velocidad / VelocidadPlena));
    }
}
