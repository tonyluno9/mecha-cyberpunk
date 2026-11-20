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

        /// <summary>
        /// T-1488: igual que <see cref="Grados"/> pero la inclinación entra poco a poco con la rapidez: cero bajo <paramref name="rapidezInicio"/> y completa desde
        /// <paramref name="rapidezPlena"/>. A baja velocidad el giro del cuerpo es ruido y la moto no debe moverse sola.
        /// </summary>
        public static float GradosEstables(float velocidadAdelante, float velocidadGiro, float gravedad, float factor, float maximoGrados,
            float rapidezInicio, float rapidezPlena)
        {
            float ancho = Math.Max(0.01f, rapidezPlena - rapidezInicio);
            float entrada = Math.Clamp((Math.Abs(velocidadAdelante) - rapidezInicio) / ancho, 0f, 1f);
            return Grados(velocidadAdelante, velocidadGiro, gravedad, factor, maximoGrados) * entrada;
        }

        /// <summary>T-1488: mueve <paramref name="actual"/> hacia <paramref name="objetivo"/> a lo más <paramref name="maximoPorSegundo"/> por segundo (sin saltos).</summary>
        public static float LimitarCambio(float actual, float objetivo, float maximoPorSegundo, float dt)
        {
            float paso = Math.Max(0f, maximoPorSegundo) * Math.Max(0f, dt);
            return actual + Math.Clamp(objetivo - actual, -paso, paso);
        }

        /// <summary>T-1488: histéresis de «en marcha»: sube sobre <paramref name="umbralSubir"/> y solo baja bajo <paramref name="umbralBajar"/>.</summary>
        public static bool EnMarcha(bool previo, float rapidez, float umbralSubir, float umbralBajar)
        {
            return previo ? rapidez > umbralBajar : rapidez > umbralSubir;
        }

        /// <summary>Acerca <paramref name="actual"/> a <paramref name="objetivo"/> de forma exponencial (independiente de los cuadros por segundo).</summary>
        public static float Suavizar(float actual, float objetivo, float respuesta, float dt)
        {
            float t = 1f - (float)Math.Exp(-Math.Max(0f, respuesta) * Math.Max(0f, dt));
            return actual + (objetivo - actual) * t;
        }
    }
}
