using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>Reglas puras de la pose de moto (T-562): inclinación al girar y avance de subir y bajar. Sin escena.</summary>
    public static class ReglasMoto
    {
        /// <summary>Inclinación máxima del conductor al tomar una curva (°).</summary>
        public const float InclinacionMaxima = 25f;
        /// <summary>Cuánto se echa el torso hacia adelante al manejar para alcanzar el manubrio (°).</summary>
        public const float InclinacionAdelante = 25f;
        /// <summary>Velocidad (m/s) a la que la moto se inclina todo lo que puede; más lento se inclina menos.</summary>
        public const float VelocidadInclinacionPlena = 12f;
        /// <summary>Lo que dura el fundido corto de subir o bajar (s).</summary>
        public const float SegundosSubir = 0.3f;

        /// <summary>
        /// Inclinación en grados (positiva a la derecha) a partir del giro normalizado (-1 a 1, positivo a la derecha) y la velocidad.
        /// Quieta no se inclina.
        /// </summary>
        public static float Inclinacion(float giro, float velocidad)
        {
            float fuerza = Mathf.Clamp01(velocidad / VelocidadInclinacionPlena);
            return Mathf.Clamp(giro, -1f, 1f) * fuerza * InclinacionMaxima;
        }

        /// <summary>Avance (0 a 1) de la transición de subir (hacia 1) o bajar (hacia 0) en el tiempo transcurrido.</summary>
        public static float Avance(float actual, bool subir, float dt, float segundos = SegundosSubir)
        {
            float paso = segundos > 0f ? dt / segundos : 1f;
            return Mathf.MoveTowards(actual, subir ? 1f : 0f, paso);
        }

        /// <summary>Suavizado de 0 a 1 (más lento al principio y al final) para el fundido de la transición.</summary>
        public static float Suave(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }
}
