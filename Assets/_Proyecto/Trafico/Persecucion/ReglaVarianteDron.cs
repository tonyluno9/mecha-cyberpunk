using System.Collections.Generic;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1498: qué variante de dron sale en la persecución. Lógica pura: cada variante pide unas estrellas mínimas; con las estrellas de ahora son elegibles las
    /// que las cumplen y los drones salen por turnos empezando por las más fuertes (el dron 0 es siempre la variante más fuerte que ya se puede).
    /// </summary>
    public static class ReglaVarianteDron
    {
        /// <summary>
        /// Índice (en <paramref name="minimos"/>) de la variante del dron número <paramref name="indice"/> con <paramref name="estrellas"/>; -1 si ninguna es elegible.
        /// </summary>
        public static int Elegir(IReadOnlyList<int> minimos, int estrellas, int indice)
        {
            if (minimos == null)
            {
                return -1;
            }
            List<int> elegibles = new List<int>();
            for (int i = 0; i < minimos.Count; i++)
            {
                if (minimos[i] <= estrellas)
                {
                    elegibles.Add(i);
                }
            }
            if (elegibles.Count == 0)
            {
                return -1;
            }
            // De más fuerte (más estrellas pedidas) a más débil; a igualdad, la de menor índice primero.
            elegibles.Sort((a, b) => minimos[a] != minimos[b] ? minimos[b].CompareTo(minimos[a]) : a.CompareTo(b));
            int vuelta = indice < 0 ? 0 : indice;
            return elegibles[vuelta % elegibles.Count];
        }
    }
}
