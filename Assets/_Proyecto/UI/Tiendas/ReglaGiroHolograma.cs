using UnityEngine;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>
    /// T-1326: las reglas para girar el arma 3D de las tiendas sin ratón. Las teclas Q (izquierda) y E (derecha) dan el mismo
    /// "arrastre" en px que daría el ratón, así que el holograma los gira igual (<c>VistaHologramaArma.Girar</c>).
    /// </summary>
    public static class ReglaGiroHolograma
    {
        /// <summary>Px de arrastre por segundo con una tecla de giro apretada.</summary>
        public const float PxPorSegundo = 360f;

        /// <summary>El arrastre de un cuadro: <paramref name="direccion"/> es -1 (Q), 0 o 1 (E); cualquier otro valor se recorta a ese rango.</summary>
        public static Vector2 ArrastrePorTeclas(float direccion, float segundos)
        {
            return new Vector2(Mathf.Clamp(direccion, -1f, 1f) * PxPorSegundo * Mathf.Max(0f, segundos), 0f);
        }

        /// <summary>La dirección de las dos teclas: E suma, Q resta, las dos juntas se anulan.</summary>
        public static float Direccion(bool izquierda, bool derecha) => (derecha ? 1f : 0f) - (izquierda ? 1f : 0f);
    }
}
