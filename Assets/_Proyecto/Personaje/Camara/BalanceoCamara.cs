using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1485: balanceo de la cámara al caminar o correr. La fase avanza con la distancia recorrida (ciclos por metro), así que
    /// el ritmo y la amplitud siguen la velocidad real. Quieto, apuntando o en el aire la intensidad baja a 0 suavemente.
    /// Lógica pura: recibe el tiempo como parámetro.
    /// </summary>
    public class BalanceoCamara
    {
        private const float DosPi = Mathf.PI * 2f;
        private float fase;
        private float intensidad;

        /// <summary>Intensidad actual, de 0 a 1.</summary>
        public float Intensidad => intensidad;

        /// <summary>Desplazamiento vertical actual (m).</summary>
        public float Vertical { get; private set; }

        /// <summary>Desplazamiento lateral actual (m).</summary>
        public float Lateral { get; private set; }

        public void Reiniciar()
        {
            fase = 0f;
            intensidad = 0f;
            Vertical = 0f;
            Lateral = 0f;
        }

        /// <param name="velocidad">Rapidez horizontal real (m/s).</param>
        /// <param name="velocidadReferencia">Rapidez a la que la intensidad vale 1 (m/s), p. ej. la de correr.</param>
        /// <param name="activo">False al estar apuntando, en el aire o sin control: la intensidad baja a 0.</param>
        /// <param name="ciclosPorMetro">Pasos completos (un ciclo = dos pasos) por metro.</param>
        /// <param name="amplitudVertical">Subida y bajada máxima (m).</param>
        /// <param name="amplitudLateral">Vaivén lateral máximo (m).</param>
        /// <param name="rapidezEntrada">Qué tan rápido sube o baja la intensidad (1/s).</param>
        public void Actualizar(float velocidad, float velocidadReferencia, bool activo, float ciclosPorMetro,
            float amplitudVertical, float amplitudLateral, float rapidezEntrada, float dt)
        {
            float objetivo = activo && velocidadReferencia > 0.01f ? Mathf.Clamp01(velocidad / velocidadReferencia) : 0f;
            intensidad = LogicaCamaraAPie.Suavizar(intensidad, objetivo, rapidezEntrada, dt);
            if (intensidad < 0.001f)
            {
                intensidad = 0f;
            }
            if (activo)
            {
                fase = (fase + velocidad * ciclosPorMetro * DosPi * dt) % DosPi;
            }
            // Vertical al doble de frecuencia que el lateral: sube y baja en cada paso, el vaivén lateral es un paso a cada lado.
            Vertical = Mathf.Sin(fase * 2f) * amplitudVertical * intensidad;
            Lateral = Mathf.Sin(fase) * amplitudLateral * intensidad;
        }
    }
}
