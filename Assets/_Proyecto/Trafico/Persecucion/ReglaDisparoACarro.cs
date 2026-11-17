namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1322: cuándo el daño a un carro cuenta como que el jugador le disparó delante de la policía. Pura, sin Unity: el carro perdió vida justo después de un
    /// disparo (un choque entre carros no cuenta) y el jugador está cerca del carro.
    /// </summary>
    public static class ReglaDisparoACarro
    {
        /// <summary>Segundos máximos entre el disparo y la pérdida de vida del carro.</summary>
        public const float SegundosVentana = 0.75f;
        /// <summary>Distancia máxima (m) entre el jugador y el carro para que cuente.</summary>
        public const float DistanciaMaxima = 80f;
        private const float MargenVida = 0.01f;

        public static bool Cuenta(float segundosDesdeDisparo, float vidaAntes, float vidaAhora, float distanciaJugador)
        {
            return segundosDesdeDisparo >= 0f && segundosDesdeDisparo <= SegundosVentana
                && vidaAhora < vidaAntes - MargenVida
                && distanciaJugador <= DistanciaMaxima;
        }
    }
}
