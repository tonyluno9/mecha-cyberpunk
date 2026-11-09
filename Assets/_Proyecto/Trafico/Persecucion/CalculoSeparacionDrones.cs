using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-938: dónde se pone cada dron para que no se atraviesen. Cada uno tiene su punto propio alrededor del jugador (en círculo, por índice) y,
    /// si aun así dos quedan a menos de la distancia mínima, se empujan entre sí. Lógica pura, sin Unity en juego.
    /// </summary>
    public static class CalculoSeparacionDrones
    {
        /// <summary>
        /// El punto del dron número <paramref name="indice"/> de <paramref name="total"/> sobre un círculo de <paramref name="radio"/> m alrededor de
        /// <paramref name="centro"/>, a <paramref name="altura"/> m por encima. <paramref name="anguloBase"/> (grados) gira todo el círculo.
        /// </summary>
        public static Vector3 PuntoEnCirculo(Vector3 centro, int indice, int total, float radio, float altura, float anguloBase = 0f)
        {
            int n = Mathf.Max(1, total);
            float grados = anguloBase + 360f * (Mathf.Abs(indice) % n) / n;
            float rad = grados * Mathf.Deg2Rad;
            return centro + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radio + Vector3.up * altura;
        }

        /// <summary>
        /// Cuánto debe moverse un dron en <paramref name="propia"/> para quedar a al menos <paramref name="distanciaMinima"/> m de cada uno de
        /// <paramref name="otras"/>. Cada pareja se reparte el empuje (la mitad cada uno), así que si todos lo aplican quedan separados.
        /// Si dos coinciden exactamente, el empuje va hacia <paramref name="direccionSiCoinciden"/>.
        /// </summary>
        public static Vector3 Repulsion(Vector3 propia, IReadOnlyList<Vector3> otras, float distanciaMinima, Vector3 direccionSiCoinciden)
        {
            Vector3 empuje = Vector3.zero;
            for (int i = 0; i < otras.Count; i++)
            {
                Vector3 desde = propia - otras[i];
                float d = desde.magnitude;
                if (d >= distanciaMinima)
                {
                    continue;
                }
                Vector3 direccion = d > 0.0001f ? desde / d : direccionSiCoinciden.normalized;
                empuje += direccion * ((distanciaMinima - d) * 0.5f);
            }
            return empuje;
        }
    }
}
