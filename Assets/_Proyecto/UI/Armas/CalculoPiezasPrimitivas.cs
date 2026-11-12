using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// Cálculo puro del banco de armas: la separación de una pieza hacia abajo nunca cruza el pedestal. T-970 quitó el cargador y el cañón
    /// de primitivas de T-942: las armas traen sus piezas reales.
    /// </summary>
    public static class CalculoPiezasPrimitivas
    {
        /// <summary>
        /// Limita un desplazamiento vertical hacia abajo para que la pieza (con su parte más baja en <paramref name="minimoPieza"/>) no baje de
        /// <paramref name="techoPedestal"/> más el margen. Devuelve el factor (0 a 1) que se aplica a todo el desplazamiento.
        /// </summary>
        public static float FactorSobrePedestal(float desplazamientoY, float minimoPieza, float techoPedestal, float margen)
        {
            if (desplazamientoY >= 0f)
            {
                return 1f;
            }
            float cabe = minimoPieza - techoPedestal - margen;
            return Mathf.Clamp01(cabe / -desplazamientoY);
        }
    }
}
