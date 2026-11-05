using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>Inclinación de una moto en curva (T-592). Lógica pura.</summary>
    public static class CalculoInclinacionMoto
    {
        private const float RadianesAGrados = (float)(180.0 / Math.PI);

        /// <summary>
        /// Grados que se inclina la moto hacia el lado de la curva (positivo: a la derecha). Con la aceleración lateral v·w y la gravedad,
        /// una moto real se inclina atan(v·w/g); <paramref name="factor"/> la suaviza para el visual y <paramref name="maximoGrados"/> la acota.
        /// </summary>
        public static float Grados(float velocidadAdelante, float velocidadGiro, float gravedad, float factor, float maximoGrados)
        {
            if (gravedad <= 0f)
            {
                return 0f;
            }
            float natural = (float)Math.Atan(velocidadAdelante * velocidadGiro / gravedad) * RadianesAGrados;
            return Math.Clamp(natural * factor, -maximoGrados, maximoGrados);
        }

        /// <summary>Acerca <paramref name="actual"/> a <paramref name="objetivo"/> de forma exponencial (independiente de los cuadros por segundo).</summary>
        public static float Suavizar(float actual, float objetivo, float respuesta, float dt)
        {
            float t = 1f - (float)Math.Exp(-Math.Max(0f, respuesta) * Math.Max(0f, dt));
            return actual + (objetivo - actual) * t;
        }
    }
}
