namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1526: un carro que va hacia un nodo sin salida (solo le queda el vecino de donde vino, por ejemplo el final de una calle junto al muro) y lo frena algo fijo
    /// (el muro, ya sólido) debe dar vuelta en U en vez de esperar: si no se queda parado y los demás se forman detrás. Pura: sin Unity.
    /// </summary>
    public static class ReglaCallejon
    {
        /// <summary>Segundos parado contra algo fijo antes de dar la vuelta.</summary>
        public const float SegundosParaVuelta = 1.5f;

        /// <summary>Distancia (m) al nodo sin salida desde la que cuenta como "llegando al final".</summary>
        public const float DistanciaAlFinal = 25f;

        /// <summary>true si debe dar la vuelta: parado contra algo fijo, yendo a un nodo sin salida y ya cerca de él.</summary>
        public static bool DebeDarVuelta(string causaDelFreno, float segundosBloqueado, int vecinosDelDestino, float distanciaAlDestino)
        {
            return causaDelFreno == "fisico" && segundosBloqueado >= SegundosParaVuelta && vecinosDelDestino <= 1 && distanciaAlDestino <= DistanciaAlFinal;
        }
    }
}
