namespace MechaCyberpunk.Pelea.Flujo
{
    /// <summary>Arma el texto de una línea del presentador (lógica pura, T-155).</summary>
    public static class LineaPresentador
    {
        /// <summary>
        /// Cambia {alias}, {rival} y {liga} de la plantilla y la pasa a mayúsculas. Un valor null o vacío deja el hueco vacío
        /// (por ejemplo {liga} sin servicio de ligas).
        /// </summary>
        public static string Armar(string plantilla, string alias, string rival, string liga)
        {
            return (plantilla ?? string.Empty)
                .Replace("{alias}", alias ?? string.Empty)
                .Replace("{rival}", rival ?? string.Empty)
                .Replace("{liga}", liga ?? string.Empty)
                .ToUpperInvariant();
        }
    }
}
