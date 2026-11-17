using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1309: puntos de un rayo de plasma con zigzag leve. Los extremos quedan fijos (boca y punto de impacto) y los de en medio se desvían de la línea
    /// con ruido que cambia con el tiempo, más cerca del centro que de las puntas. Sin estado; solo usa <see cref="Mathf.PerlinNoise"/>.
    /// </summary>
    public static class ReglaZigzagRayo
    {
        /// <summary>Velocidad con que el ruido cambia en el tiempo (ciclos por segundo).</summary>
        public const float VelocidadRuido = 14f;

        /// <summary>
        /// Llena <paramref name="salida"/> con una línea de <paramref name="origen"/> a <paramref name="fin"/>. El primer y el último punto son exactamente los
        /// extremos; los demás se desvían como máximo <paramref name="amplitud"/> metros (perpendicular al rayo).
        /// </summary>
        public static void Puntos(Vector3 origen, Vector3 fin, float tiempo, float amplitud, int semilla, Vector3[] salida)
        {
            int n = salida.Length;
            if (n == 0)
            {
                return;
            }
            salida[0] = origen;
            if (n == 1)
            {
                return;
            }
            Vector3 eje = fin - origen;
            Vector3 dir = eje.sqrMagnitude > 0.000001f ? eje.normalized : Vector3.forward;
            Vector3 referencia = Mathf.Abs(dir.y) > 0.98f ? Vector3.right : Vector3.up;
            Vector3 lado = Vector3.Cross(dir, referencia).normalized;
            Vector3 arriba = Vector3.Cross(lado, dir).normalized;
            for (int i = 1; i < n - 1; i++)
            {
                float f = i / (float)(n - 1);
                float envolvente = Mathf.Sin(f * Mathf.PI);
                float ruidoLado = Mathf.PerlinNoise(i * 1.73f + semilla, tiempo * VelocidadRuido) * 2f - 1f;
                float ruidoArriba = Mathf.PerlinNoise(i * 2.11f + semilla + 50f, tiempo * VelocidadRuido + 7.3f) * 2f - 1f;
                salida[i] = origen + eje * f + (lado * ruidoLado + arriba * ruidoArriba) * (amplitud * envolvente * 0.7071f);
            }
            salida[n - 1] = fin;
        }
    }
}
