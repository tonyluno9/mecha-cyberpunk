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

        /// <summary>
        /// T-1488: igual que <see cref="Fuerza(float, float, float, float)"/> pero el amortiguador es <paramref name="factorRebote"/> veces más duro al estirarse
        /// (rebote) que al comprimirse: la carrocería sube rápido y se asienta sin oscilar eternamente.
        /// </summary>
        public static float Fuerza(float compresion, float velocidadCompresion, float rigidez, float amortiguamiento, float factorRebote)
        {
            float c = velocidadCompresion < 0f ? amortiguamiento * System.Math.Max(0f, factorRebote) : amortiguamiento;
            return Fuerza(compresion, velocidadCompresion, rigidez, c);
        }

        /// <summary>T-1488: rigidez (N/m) para que un carro de <paramref name="masa"/> kg con cuatro resortes se hunda <paramref name="caida"/> m en reposo.</summary>
        public static float RigidezParaCaida(float masa, float caida, float gravedad = 9.81f)
        {
            return caida <= 0f ? 0f : masa * gravedad / (4f * caida);
        }

        /// <summary>T-1488: amortiguamiento (N·s/m) de un resorte para una relación de amortiguamiento <paramref name="zeta"/> (1 es crítico; 0.3 a 0.6 rebota y se asienta).</summary>
        public static float AmortiguamientoParaRelacion(float masa, float rigidez, float zeta)
        {
            return 2f * zeta * (float)System.Math.Sqrt(System.Math.Max(0f, rigidez) * masa * 0.25f);
        }

        /// <summary>T-1488: relación de amortiguamiento de una esquina del carro (masa/4 sobre su resorte).</summary>
        public static float RelacionAmortiguamiento(float masa, float rigidez, float amortiguamiento)
        {
            float critico = 2f * (float)System.Math.Sqrt(System.Math.Max(0f, rigidez) * masa * 0.25f);
            return critico <= 0f ? 0f : amortiguamiento / critico;
        }
    }
}
