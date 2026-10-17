using System;

namespace MechaCyberpunk.Mecha.Estado
{
    /// <summary>Cuenta pura de la vida con la que empieza una pelea o se carga una partida (daño persistente).</summary>
    public static class ReglaVidaPersistente
    {
        /// <summary>El mayor entre la vida actual y el piso (<c>maxima × fraccionPiso</c>).</summary>
        public static float AlPreparar(float actual, float maxima, float fraccionPiso)
        {
            return Math.Max(actual, maxima * fraccionPiso);
        }

        /// <summary>
        /// Escala la vida guardada a la vida máxima actual y la limita a [piso, máxima actual].
        /// Con máxima guardada &lt;= 0 (datos inválidos) devuelve la máxima actual.
        /// </summary>
        public static float AlCargar(float vidaGuardada, float maximaGuardada, float maximaActual, float fraccionPiso)
        {
            if (maximaGuardada <= 0f || float.IsNaN(vidaGuardada) || float.IsNaN(maximaGuardada))
            {
                return maximaActual;
            }
            float escalada = vidaGuardada / maximaGuardada * maximaActual;
            float piso = maximaActual * fraccionPiso;
            return Math.Min(Math.Max(escalada, piso), maximaActual);
        }

        /// <summary>
        /// La vida (puntos) que queda al fijar una <paramref name="fraccion"/> de la máxima: no baja del piso
        /// (<paramref name="fraccionPiso"/> de la máxima) ni pasa de la máxima.
        /// </summary>
        public static float AlFijarFraccion(float maxima, float fraccion, float fraccionPiso)
        {
            if (float.IsNaN(fraccion))
            {
                return maxima;
            }
            return Math.Min(Math.Max(maxima * fraccion, maxima * fraccionPiso), maxima);
        }
    }
}
