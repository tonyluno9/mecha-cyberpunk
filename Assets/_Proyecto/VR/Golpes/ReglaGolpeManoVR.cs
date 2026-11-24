using System;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR2/VR35: reglas del golpe con la mano (lógica pura). La mano pega si va a más de <see cref="VelocidadMinima"/> m/s (sin contar lo que camina el
    /// jugador; se toma la mayor rapidez de los últimos <see cref="SegundosPico"/> s, porque al llegar al blanco el brazo ya frena); el daño del puño
    /// se multiplica por la rapidez: x1 hasta <see cref="VelocidadPlena"/> m/s y sube hasta x<see cref="FactorMaximo"/>; con el implante de BRAZOS,
    /// x<see cref="FactorImplante"/> más. Cada mano le pega al mismo enemigo a lo más una vez cada <see cref="SegundosEnfriamiento"/> s.
    /// </summary>
    public static class ReglaGolpeManoVR
    {
        public const float VelocidadMinima = 0.8f;
        public const float VelocidadPlena = 3f;
        public const float FactorMaximo = 2f;
        public const float FactorImplante = 2f;
        public const float SegundosEnfriamiento = 0.3f;
        /// <summary>Ventana (s) en la que se busca la rapidez más alta de la mano.</summary>
        public const float SegundosPico = 0.1f;

        /// <summary>True si a esa rapidez (m/s) la mano golpea.</summary>
        public static bool Golpea(float velocidad) => velocidad > VelocidadMinima;

        /// <summary>Multiplicador del daño del puño a esa rapidez: clamp(v / 3, 1, 2).</summary>
        public static float Factor(float velocidad) => Math.Max(1f, Math.Min(FactorMaximo, velocidad / VelocidadPlena));

        /// <summary>Daño del golpe: el del puño por el factor de rapidez, y x2 con el implante de BRAZOS.</summary>
        public static float Danio(float danioPuno, float velocidad, bool conImplante) => danioPuno * Factor(velocidad) * (conImplante ? FactorImplante : 1f);

        /// <summary>
        /// VR35: si se le puede pegar a un enemigo agarrado. Al que va volando no (el choque ya le hace daño); al sostenido solo con la mano que no lo
        /// sostiene.
        /// </summary>
        public static bool PuedePegarAlAgarrado(bool sostenido, bool loSostieneEstaMano) => sostenido && !loSostieneEstaMano;
    }
}
