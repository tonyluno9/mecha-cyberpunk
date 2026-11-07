using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Cable colgante entre dos puntos (docs/plan-look-ciudad.md §2.5): curva que baja <c>flecha</c> metros en medio, como un cable de luz
    /// entre postes. Se aproxima con una parábola (para flechas chicas es casi igual a la catenaria). Lógica pura.
    /// </summary>
    public static class CalculoCatenaria
    {
        /// <summary>Punto del cable en la fracción <paramref name="t"/> (0 en <paramref name="a"/>, 1 en <paramref name="b"/>).</summary>
        public static Vector3 Punto(Vector3 a, Vector3 b, float flecha, float t)
        {
            t = Mathf.Clamp01(t);
            return Vector3.Lerp(a, b, t) + Vector3.down * (flecha * 4f * t * (1f - t));
        }

        /// <summary><paramref name="tramos"/> + 1 puntos del cable, de a a b.</summary>
        public static Vector3[] Puntos(Vector3 a, Vector3 b, float flecha, int tramos)
        {
            tramos = Mathf.Max(1, tramos);
            Vector3[] puntos = new Vector3[tramos + 1];
            for (int i = 0; i <= tramos; i++)
            {
                puntos[i] = Punto(a, b, flecha, (float)i / tramos);
            }
            return puntos;
        }

        /// <summary>Flecha natural de un cable de largo horizontal <paramref name="largo"/> (m): ~3 % del claro, con un mínimo.</summary>
        public static float FlechaNatural(float largo, float proporcion, float minima)
        {
            return Mathf.Max(minima, largo * proporcion);
        }
    }
}
