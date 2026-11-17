namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1321: cuándo el daño a una patrulla cuenta como delito del jugador. Antes cualquier baja de vida con el jugador a menos de 80 m lo ponía en búsqueda
    /// (una patrulla que choca sola con un barril o con un poste al aparecer te perseguía desde el inicio). Ahora no cuenta mientras la patrulla acaba de aparecer
    /// (<see cref="SegundosGracia"/>) ni justo después de un choque con algo que no es el jugador (<see cref="SegundosVentanaAjena"/>).
    /// </summary>
    public static class ReglaCulpaPatrulla
    {
        public const float SegundosGracia = 4f;
        public const float SegundosVentanaAjena = 1.5f;
        /// <summary>Rapidez relativa (m/s) desde la que un choque contra un barril lo revienta.</summary>
        public const float RapidezRompeBarril = 6f;

        public static bool CuentaComoDelito(float segundosDesdeAparecer, float segundosDesdeChoqueAjeno)
        {
            return segundosDesdeAparecer >= SegundosGracia && segundosDesdeChoqueAjeno >= SegundosVentanaAjena;
        }

        /// <summary>True si con el jugador a <paramref name="distancia"/> m ese daño se le debe sumar calor.</summary>
        public static bool CuentaComoDelito(float segundosDesdeAparecer, float segundosDesdeChoqueAjeno, float distancia, float distanciaMaxima)
        {
            return distancia <= distanciaMaxima && CuentaComoDelito(segundosDesdeAparecer, segundosDesdeChoqueAjeno);
        }
    }
}
