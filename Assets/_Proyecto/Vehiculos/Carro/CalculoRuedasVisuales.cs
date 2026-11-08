using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>T-742: números de las ruedas visuales (cuánto giran y cuánto doblan las delanteras). Lógica pura, sin Unity.</summary>
    public static class CalculoRuedasVisuales
    {
        private const float GradosPorRadian = 180f / (float)Math.PI;

        /// <summary>Cuántos grados rueda una llanta de <paramref name="radio"/> (m) en <paramref name="dt"/> a <paramref name="velocidad"/> (m/s, negativa en reversa).</summary>
        public static float AvanceGrados(float velocidad, float radio, float dt)
        {
            if (radio <= 0f)
            {
                return 0f;
            }
            return velocidad / radio * GradosPorRadian * dt;
        }

        /// <summary>
        /// Cuánto doblan las ruedas delanteras (°) de un carro que avanza a <paramref name="velocidad"/> (m/s) y gira <paramref name="giroGradosPorSegundo"/> (°/s),
        /// con <paramref name="distanciaEjes"/> (m) entre ejes (modelo de bicicleta). Casi parado no se puede saber: devuelve 0. Nunca pasa de <paramref name="maximo"/>.
        /// </summary>
        public static float AnguloPorGiro(float distanciaEjes, float velocidad, float giroGradosPorSegundo, float velocidadMinima, float maximo)
        {
            if (Math.Abs(velocidad) < velocidadMinima || distanciaEjes <= 0f)
            {
                return 0f;
            }
            float giroRadianes = giroGradosPorSegundo / GradosPorRadian;
            float angulo = (float)Math.Atan(distanciaEjes * giroRadianes / velocidad) * GradosPorRadian;
            return Math.Clamp(angulo, -maximo, maximo);
        }

        /// <summary>
        /// Cuánto doblan las ruedas delanteras (°) con el volante en <paramref name="volante"/> (-1 a 1): el máximo a baja velocidad y menos a toda velocidad,
        /// como el giro del carro (se interpola con la rapidez entre <paramref name="anguloParado"/> y <paramref name="anguloRapido"/>).
        /// </summary>
        public static float AnguloPorVolante(float volante, float rapidez, float rapidezMaxima, float anguloParado, float anguloRapido)
        {
            float fraccion = rapidezMaxima > 0f ? Math.Clamp(Math.Abs(rapidez) / rapidezMaxima, 0f, 1f) : 0f;
            float maximo = anguloParado + (anguloRapido - anguloParado) * fraccion;
            return Math.Clamp(volante, -1f, 1f) * maximo;
        }

        /// <summary>Acerca <paramref name="actual"/> a <paramref name="objetivo"/> sin pasar de <paramref name="gradosPorSegundo"/> (°/s) en <paramref name="dt"/>.</summary>
        public static float Acercar(float actual, float objetivo, float gradosPorSegundo, float dt)
        {
            float paso = Math.Max(0f, gradosPorSegundo) * dt;
            if (Math.Abs(objetivo - actual) <= paso)
            {
                return objetivo;
            }
            return actual + Math.Sign(objetivo - actual) * paso;
        }
    }
}
