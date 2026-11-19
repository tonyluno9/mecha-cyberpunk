using UnityEngine;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// Lógica pura (T-1462): qué fracción de la densidad de niebla se conserva según la altura de la cámara sobre el piso.
    /// A nivel de calle (hasta <see cref="AlturaCalle"/>) es 1, como siempre; a 40 y 80 m ronda el 35-45 %; desde 150 m baja lo bastante para leer la zona.
    /// </summary>
    public static class CalculoNieblaAltura
    {
        /// <summary>Altura (m) hasta la que la niebla es la de siempre.</summary>
        public const float AlturaCalle = 12f;

        private static readonly float[] Alturas = { AlturaCalle, 40f, 80f, 150f, 250f };
        private static readonly float[] Factores = { 1f, 0.45f, 0.35f, 0.15f, 0.1f };

        /// <summary>Factor (0 a 1) que multiplica la densidad de niebla a esa altura (m) sobre el piso. Interpolación lineal entre puntos.</summary>
        public static float Factor(float alturaSobrePiso)
        {
            if (alturaSobrePiso <= Alturas[0])
            {
                return Factores[0];
            }
            for (int i = 1; i < Alturas.Length; i++)
            {
                if (alturaSobrePiso <= Alturas[i])
                {
                    float t = (alturaSobrePiso - Alturas[i - 1]) / (Alturas[i] - Alturas[i - 1]);
                    return Mathf.Lerp(Factores[i - 1], Factores[i], t);
                }
            }
            return Factores[Factores.Length - 1];
        }
    }
}
