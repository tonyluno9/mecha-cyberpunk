using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Lógica pura del cielo (T14): en qué barrio cae un punto (con los límites medidos: Suburbios x menor que límite oeste, Mercado entre los dos
    /// límites, Neón Alto x mayor que el límite este con z arriba del límite de Nova, Nova abajo) y cómo se acerca un color a su objetivo con el tiempo.
    /// </summary>
    public static class CalculoCieloRealista
    {
        public static BarrioCielo BarrioEn(float x, float z, float limiteOeste, float limiteEste, float limiteNovaZ)
        {
            if (x < limiteOeste)
            {
                return BarrioCielo.Suburbios;
            }
            if (x <= limiteEste)
            {
                return BarrioCielo.Mercado;
            }
            return z > limiteNovaZ ? BarrioCielo.NeonAlto : BarrioCielo.Nova;
        }

        /// <summary>Acerca <paramref name="actual"/> a <paramref name="objetivo"/> a <paramref name="velocidad"/> (1/s) con el paso <paramref name="dt"/> (s).</summary>
        public static Color Suavizar(Color actual, Color objetivo, float velocidad, float dt)
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, velocidad) * Mathf.Max(0f, dt));
            return Color.Lerp(actual, objetivo, t);
        }
    }
}
