namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>T-1709 (T74): tiempos y tamaño del parpadeo leve de una zona de glitch (lógica pura). Cada <see cref="EsperaMin"/> a <see cref="EsperaMax"/> s dura <see cref="DuracionMin"/> a <see cref="DuracionMax"/> s y se desplaza 1 a 3 cm.</summary>
    public static class ReglaGlitch
    {
        public const float EsperaMin = 1f;
        public const float EsperaMax = 4f;
        public const float DuracionMin = 0.05f;
        public const float DuracionMax = 0.15f;
        public const float DesplazamientoMin = 0.01f;
        public const float DesplazamientoMax = 0.03f;
        /// <summary>Sonido del zumbido de transformador (ya está en el catálogo, con su mezcla 3D y rolloff).</summary>
        public const string IdZumbido = "OBJ_TRANSFORMADOR";

        public static float Espera(float azar01) => Lerp(EsperaMin, EsperaMax, azar01);

        public static float Duracion(float azar01) => Lerp(DuracionMin, DuracionMax, azar01);

        public static float Desplazamiento(float azar01) => Lerp(DesplazamientoMin, DesplazamientoMax, azar01);

        private static float Lerp(float a, float b, float t) => a + (b - a) * (t < 0f ? 0f : t > 1f ? 1f : t);
    }
}
