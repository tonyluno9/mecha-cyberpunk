using UnityEngine;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>
    /// Tono del bucle del motor (T-595): sube con la rapidez y lleva una oscilación lenta distinta en cada carro (ruido de Perlin), para que el
    /// ralentí no suene como una grabación pegada y dos carros juntos no suenen idénticos.
    /// </summary>
    public static class ReglaMotorSonido
    {
        /// <summary>Tono base por rapidez: mínimo con el carro parado, máximo a la rapidez en que se llega a él.</summary>
        public static float TonoBase(float rapidez, float tonoMinimo, float tonoMaximo, float metrosPorSegundoPorTono)
        {
            return Mathf.Clamp(tonoMinimo + Mathf.Max(0f, rapidez) / metrosPorSegundoPorTono, tonoMinimo, tonoMaximo);
        }

        /// <summary>Tono con oscilación: <paramref name="amplitud"/> es la fracción (0.04 = +-4 %), <paramref name="fase"/> distingue a cada carro.</summary>
        public static float Tono(float rapidez, float tiempo, float fase, float tonoMinimo, float tonoMaximo, float metrosPorSegundoPorTono, float amplitud, float frecuencia)
        {
            float oscila = (Mathf.PerlinNoise(tiempo * frecuencia, fase) - 0.5f) * 2f;
            return TonoBase(rapidez, tonoMinimo, tonoMaximo, metrosPorSegundoPorTono) * (1f + oscila * amplitud);
        }
    }
}
