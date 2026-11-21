namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>T-1535: reglas puras de los sonidos de la vida del jugador: el latido con vida baja y el pitido de oído tras una explosión.</summary>
    public static class ReglaSonidoVida
    {
        public const string Latido = "JUG_LATIDO_VIDA_BAJA";
        public const string ZumbidoOido = "JUG_ZUMBIDO_EXPLOSION";

        /// <summary>Cada cuántos segundos se repite el latido con vida baja (el sonido dura 4.25 s) (s).</summary>
        public const float SegundosEntreLatidos = 4.5f;

        /// <summary>Margen de la fracción baja (fracción de la vida máxima) para que el límite exacto no cuente como vida baja.</summary>
        public const float MargenFraccion = 0.001f;

        /// <summary>Si toca un latido: vivo, con la vida por debajo de la fracción baja y pasado el intervalo desde el último.</summary>
        public static bool TocaLatido(float vida, float maxima, float fraccionBaja, bool estaKO, float ahora, float ultimo)
        {
            // Con un margen de 1 milésima: en Mono la cuenta va en doble precisión y justo en el límite (40 de 100 con 0.4f) contaba como baja.
            return !estaKO && maxima > 0f && vida < maxima * fraccionBaja - maxima * MargenFraccion && ahora - ultimo >= SegundosEntreLatidos;
        }
    }
}
