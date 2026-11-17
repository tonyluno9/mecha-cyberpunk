using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1321: la velocidad que ve el animador del policía. Antes recibía la velocidad que el cerebro pedía, aunque el cuerpo no avanzara (pared, otro policía) o
    /// avanzara distinto; con el árbol de apuntar caminando eso se veía como deslizarse. Ahora recibe la velocidad real del cuerpo en el plano.
    /// </summary>
    public static class ReglaAnimacionPolicia
    {
        /// <summary>Por debajo de esto (m/s) lo pedido cuenta como quieto.</summary>
        public const float PedidoQuieto = 0.01f;

        /// <summary>
        /// <paramref name="real"/> es la velocidad del cuerpo en el plano (m/s) y <paramref name="pedida"/> la que pide el cerebro (m/s).
        /// Si no pide moverse devuelve 0 (no arrastra el último impulso); si pide, devuelve la real, nunca más que lo pedido.
        /// </summary>
        public static float VelocidadVista(float real, float pedida)
        {
            if (pedida < PedidoQuieto)
            {
                return 0f;
            }
            return Mathf.Min(Mathf.Max(0f, real), pedida);
        }
    }
}
