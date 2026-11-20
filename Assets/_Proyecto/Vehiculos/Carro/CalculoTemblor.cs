using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>T-1488: fuerza del temblor de cámara según el impacto. Lógica pura.</summary>
    public static class CalculoTemblor
    {
        /// <summary>
        /// Fuerza (0 a <paramref name="maxima"/>) del impulso para un choque con <paramref name="rapidez"/> m/s relativos. Cero hasta
        /// <paramref name="umbral"/>; sube lineal hasta <paramref name="rapidezMaxima"/> (donde vale la máxima).
        /// </summary>
        public static float Fuerza(float rapidez, float umbral, float rapidezMaxima, float maxima)
        {
            if (rapidez <= umbral || rapidezMaxima <= umbral)
            {
                return 0f;
            }
            float t = Math.Clamp((rapidez - umbral) / (rapidezMaxima - umbral), 0f, 1f);
            return maxima * t;
        }
    }
}
