using MechaCyberpunk.Core;

namespace MechaCyberpunk.Combate.Bloqueo
{
    /// <summary>Lógica pura del bloqueo: dado el daño y el estado de la guardia, cuánto entra y qué tipo de bloqueo fue.</summary>
    public static class CalculoBloqueo
    {
        /// <param name="danio">Daño pedido (puntos).</param>
        /// <param name="guardiaArriba">Si la guardia ya terminó de subir.</param>
        /// <param name="segundosArriba">Tiempo que lleva arriba (s).</param>
        /// <param name="ventanaPerfecta">Si lleva menos que esto arriba, el bloqueo es perfecto (s).</param>
        /// <param name="reduccion">Parte del daño que quita un bloqueo normal (de 0 a 1).</param>
        public static (float Danio, TipoBloqueo Tipo) Calcular(float danio, bool guardiaArriba, float segundosArriba,
            float ventanaPerfecta, float reduccion)
        {
            if (!guardiaArriba)
            {
                return (danio, TipoBloqueo.Ninguno);
            }
            if (segundosArriba < ventanaPerfecta)
            {
                return (0f, TipoBloqueo.Perfecto);
            }
            return (danio * (1f - reduccion), TipoBloqueo.Normal);
        }
    }
}
