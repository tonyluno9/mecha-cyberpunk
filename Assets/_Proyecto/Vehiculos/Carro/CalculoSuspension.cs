namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>Resorte con amortiguador de una rueda. Lógica pura.</summary>
    public static class CalculoSuspension
    {
        /// <summary>
        /// Fuerza hacia arriba (N) que la rueda le hace al carro. Nunca es negativa: la rueda no jala al carro hacia el piso.
        /// </summary>
        /// <param name="compresion">Cuánto se comprimió el resorte (m). Con cero o menos no hay contacto.</param>
        /// <param name="velocidadCompresion">Rapidez a la que se comprime (m/s); negativa si se estira.</param>
        /// <param name="rigidez">Rigidez del resorte (N/m).</param>
        /// <param name="amortiguamiento">Amortiguamiento (N·s/m).</param>
        public static float Fuerza(float compresion, float velocidadCompresion, float rigidez, float amortiguamiento)
        {
            if (compresion <= 0f)
            {
                return 0f;
            }
            float fuerza = rigidez * compresion + amortiguamiento * velocidadCompresion;
            return fuerza > 0f ? fuerza : 0f;
        }
    }
}
