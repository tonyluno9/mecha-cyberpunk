using UnityEngine;

namespace MechaCyberpunk.Mecha.Movimiento
{
    /// <summary>Lógica pura del giro del cuerpo con velocidad máxima.</summary>
    public static class GiroPesado
    {
        /// <summary>
        /// Devuelve cuántos grados gira el cuerpo este frame. Quien llama resta ese valor del giro pendiente.
        /// </summary>
        /// <param name="giroPendiente">Giro pedido por el ratón que falta por hacer (°).</param>
        /// <param name="dt">Tiempo del frame (s).</param>
        /// <param name="velocidadGiroMaxima">Giro máximo del cuerpo (°/s).</param>
        public static float Calcular(float giroPendiente, float dt, float velocidadGiroMaxima)
        {
            float maximo = Mathf.Max(0f, velocidadGiroMaxima * dt);
            return Mathf.Clamp(giroPendiente, -maximo, maximo);
        }
    }
}
