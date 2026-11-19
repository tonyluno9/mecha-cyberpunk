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
    }
}
