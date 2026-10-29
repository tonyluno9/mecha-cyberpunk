using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Lógica pura de la mirada a pie: el ratón gira el cuerpo (horizontal) e inclina la cámara (vertical).</summary>
    public class MiradaAPie
    {
        /// <summary>Inclinación vertical de la cámara (°, positivo mira hacia abajo).</summary>
        public float Inclinacion { get; private set; }

        /// <summary>
        /// Aplica un delta del ratón. Devuelve el giro del cuerpo en este frame (°).
        /// Un delta Y positivo (ratón hacia arriba) mira hacia arriba.
        /// </summary>
        public float Actualizar(Vector2 delta, float sensibilidad, float limiteVertical)
        {
            Inclinacion = Mathf.Clamp(Inclinacion - delta.y * sensibilidad, -limiteVertical, limiteVertical);
            return delta.x * sensibilidad;
        }

        /// <summary>Limita la inclinación a un rango asimétrico (°; negativo mira hacia arriba).</summary>
        public void Limitar(float minima, float maxima)
        {
            Inclinacion = Mathf.Clamp(Inclinacion, minima, maxima);
        }

        public void Reiniciar()
        {
            Inclinacion = 0f;
        }
    }
}
