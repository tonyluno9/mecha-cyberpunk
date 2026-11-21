namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>T-1535: reglas puras de los sonidos de la vida del jugador: el latido con vida baja y el pitido de oído tras una explosión.</summary>
    public static class ReglaSonidoVida
    {
        public const string Latido = "JUG_LATIDO_VIDA_BAJA";
        public const string ZumbidoOido = "JUG_ZUMBIDO_EXPLOSION";

        /// <summary>Cada cuántos segundos se repite el latido con vida baja (el sonido dura 4.25 s) (s).</summary>
        public const float SegundosEntreLatidos = 4.5f;

        /// <summary>Si toca un latido: vivo, con la vida por debajo de la fracción baja y pasado el intervalo desde el último.</summary>
        public static bool TocaLatido(float vida, float maxima, float fraccionBaja, bool estaKO, float ahora, float ultimo)
        {
            return !estaKO && maxima > 0f && vida < maxima * fraccionBaja && ahora - ultimo >= SegundosEntreLatidos;
        }
    }
}
