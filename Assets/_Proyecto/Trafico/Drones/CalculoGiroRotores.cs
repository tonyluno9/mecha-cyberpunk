namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>T-1397: lógica pura del giro de los rotores de un dron o nave.</summary>
    public static class CalculoGiroRotores
    {
        /// <summary>
        /// Grados que gira el rotor <paramref name="indice"/> en <paramref name="dt"/> segundos a <paramref name="velocidad"/> °/s.
        /// Si <paramref name="alternar"/> es true los rotores de índice impar giran al revés (pares contrarrotantes).
        /// </summary>
        public static float GradosEnPaso(float velocidad, float dt, int indice, bool alternar)
        {
            float signo = alternar && indice % 2 != 0 ? -1f : 1f;
            return velocidad * dt * signo;
        }

        /// <summary>T-1398: true si el dron está lo bastante cerca para girar los rotores. Con <paramref name="distanciaMaxima"/> en 0 o menos no hay corte.</summary>
        public static bool DentroDeAlcance(float distanciaCuadrada, float distanciaMaxima)
        {
            return distanciaMaxima <= 0f || distanciaCuadrada <= distanciaMaxima * distanciaMaxima;
        }
    
        /// <summary>T-1554: el eje del disco de un rotor = el eje más delgado del tamaño de su malla (local). Con empate gana Y.</summary>
        public static UnityEngine.Vector3 EjeDelDisco(UnityEngine.Vector3 tamano)
        {
            if (tamano.y <= tamano.x && tamano.y <= tamano.z)
            {
                return UnityEngine.Vector3.up;
            }
            return tamano.z <= tamano.x ? UnityEngine.Vector3.forward : UnityEngine.Vector3.right;
        }
}
}
