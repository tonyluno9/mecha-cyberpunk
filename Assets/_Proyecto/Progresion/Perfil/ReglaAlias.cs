namespace MechaCyberpunk.Progresion
{
    /// <summary>Regla pura del alias (y del nombre) del jugador: sin espacios de los lados, máximo 16 caracteres.</summary>
    public static class ReglaAlias
    {
        public const int LargoMaximo = 16;

        /// <summary>Devuelve el texto limpio, o <paramref name="respaldo"/> si queda vacío o es null.</summary>
        public static string Normalizar(string texto, string respaldo)
        {
            string limpio = texto == null ? string.Empty : texto.Trim();
            if (limpio.Length > LargoMaximo)
            {
                limpio = limpio.Substring(0, LargoMaximo).TrimEnd();
            }
            return limpio.Length == 0 ? respaldo : limpio;
        }
    }
}
