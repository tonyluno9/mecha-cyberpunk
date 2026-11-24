namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// VR39: cuándo suena el golpe de un cuerpo (enemigo lanzado o que cae) contra una superficie. Antes sonaba en cada choque que entraba, y un cuerpo
    /// muerto que rodaba o rebotaba en el piso lo repetía sin parar. Ahora: solo choques fuertes (rapidez mínima contra la superficie), con enfriamiento,
    /// un tope por vuelo, un cuerpo derrotado suena una sola vez y, quieto, nunca.
    /// </summary>
    public static class ReglaSonidoChoqueCuerpo
    {
        /// <summary>Rapidez mínima (m/s, contra la superficie) para que el choque suene.</summary>
        public const float RapidezMinima = 3f;

        /// <summary>Segundos mínimos entre dos sonidos de choque del mismo cuerpo.</summary>
        public const float SegundosEnfriamiento = 0.6f;

        /// <summary>Sonidos máximos de choque en un vuelo (un cuerpo vivo lanzado contra varias paredes).</summary>
        public const int MaximoPorVuelo = 3;

        /// <summary>
        /// True si el choque debe sonar. <paramref name="ultimo"/> es el Time.time del último sonido (negativo si no ha sonado);
        /// <paramref name="sonados"/>, cuántos van en este cuerpo; <paramref name="derrotado"/>, si el cuerpo está muerto o caído;
        /// <paramref name="quieto"/>, si el cuerpo ya está quieto.
        /// </summary>
        public static bool DebeSonar(float rapidez, float ahora, float ultimo, int sonados, bool derrotado, bool quieto)
        {
            if (quieto || rapidez < RapidezMinima)
            {
                return false;
            }
            if (sonados >= (derrotado ? 1 : MaximoPorVuelo))
            {
                return false;
            }
            return ultimo < 0f || ahora - ultimo >= SegundosEnfriamiento;
        }
    }
}
