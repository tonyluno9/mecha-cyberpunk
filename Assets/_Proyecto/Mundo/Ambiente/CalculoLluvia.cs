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
    }
}
