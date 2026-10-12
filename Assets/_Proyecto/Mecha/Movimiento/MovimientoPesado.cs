using UnityEngine;

namespace MechaCyberpunk.Mecha.Movimiento
{
    /// <summary>Lógica pura del caminar con inercia: arranca y frena de forma gradual.</summary>
    public static class MovimientoPesado
    {
        /// <summary>
        /// Devuelve la nueva velocidad horizontal (x lateral, y adelante, en m/s).
        /// </summary>
        /// <param name="entrada">Entrada de movimiento (cada eje entre -1 y 1).</param>
        /// <param name="velocidadActual">Velocidad horizontal actual (m/s).</param>
        /// <param name="dt">Tiempo del frame (s).</param>
        /// <param name="velocidadMaxima">Velocidad máxima (m/s).</param>
        /// <param name="aceleracion">Aceleración al haber entrada (m/s²).</param>
        /// <param name="desaceleracion">Frenado sin entrada (m/s²).</param>
        public static Vector2 Calcular(Vector2 entrada, Vector2 velocidadActual, float dt,
            float velocidadMaxima, float aceleracion, float desaceleracion)
        {
            if (dt <= 0f)
            {
                return velocidadActual;
            }

            Vector2 resultado;
            if (entrada.sqrMagnitude > 0f)
            {
                Vector2 objetivo = Vector2.ClampMagnitude(entrada, 1f) * velocidadMaxima;
                resultado = Vector2.MoveTowards(velocidadActual, objetivo, aceleracion * dt);
            }
            else
            {
                resultado = Vector2.MoveTowards(velocidadActual, Vector2.zero, desaceleracion * dt);
            }

            return Vector2.ClampMagnitude(resultado, velocidadMaxima);
        }
    }
}
