namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>Regla pura (T-096): cuándo debe estar lloviendo. Sin Unity, para probarla en EditMode.</summary>
    public static class CalculoLluvia
    {
        /// <summary>Llueve si el interruptor está activo, el jugador no está en un interior y es de noche (o la config permite lluvia de día).</summary>
        public static bool DebeLlover(bool interruptorActivo, bool jugadorEnInterior, bool esDeNoche, bool lluviaDeDia)
        {
            return interruptorActivo && !jugadorEnInterior && (esDeNoche || lluviaDeDia);
        }

        /// <summary>T-512: qué tanto llueve (0 a 1): de noche toda la lluvia; de día solo la fracción de la config (lluvia ligera).</summary>
        public static float Intensidad(bool esDeNoche, float fraccionDeDia)
        {
            return esDeNoche ? 1f : (fraccionDeDia < 0f ? 0f : (fraccionDeDia > 1f ? 1f : fraccionDeDia));
        }
    }
}
