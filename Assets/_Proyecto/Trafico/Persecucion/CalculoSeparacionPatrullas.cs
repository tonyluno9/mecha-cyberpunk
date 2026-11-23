using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1660 (T42): cuentas puras para que las patrullas no se choquen entre ellas. Reparte los puntos de destino alrededor del jugador (cada una su lugar a lo ancho de la
    /// línea por donde llega) y empuja la dirección de manejo lejos de las patrullas que tiene muy cerca.
    /// </summary>
    public static class CalculoSeparacionPatrullas
    {
        /// <summary>
        /// El punto al que va la patrulla número <paramref name="indice"/> de <paramref name="total"/>: el jugador, corrido de lado (perpendicular a la línea patrulla-jugador)
        /// para que cada una tenga su sitio. Con una sola patrulla, o ya muy cerca, es el jugador mismo.
        /// </summary>
        public static Vector3 PuntoDeAcople(Vector3 patrulla, Vector3 jugador, int indice, int total, float separacion, float distanciaMinima)
        {
            Vector3 hacia = jugador - patrulla;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            if (total <= 1 || distancia <= distanciaMinima || separacion <= 0f)
            {
                return jugador;
            }
            Vector3 lado = Vector3.Cross(Vector3.up, hacia / distancia);
            float posicion = indice - (total - 1) * 0.5f;
            return jugador + lado * (posicion * separacion);
        }

        /// <summary>
        /// Suma a <paramref name="direccion"/> (unitaria, en el plano) un empuje lejos de cada patrulla a menos de <paramref name="radio"/> m, más fuerte cuanto más cerca,
        /// y la devuelve unitaria. Sin patrullas cerca no cambia nada.
        /// </summary>
        public static Vector3 Separar(Vector3 yo, Vector3 direccion, IReadOnlyList<Vector3> otras, float radio, float fuerza)
        {
            Vector3 empuje = Vector3.zero;
            for (int i = 0; i < otras.Count; i++)
            {
                Vector3 d = yo - otras[i];
                d.y = 0f;
                float distancia = d.magnitude;
                if (distancia >= radio || distancia < 0.001f)
                {
                    continue;
                }
                empuje += d / distancia * (1f - distancia / radio);
            }
            if (empuje.sqrMagnitude < 0.0001f)
            {
                return direccion;
            }
            Vector3 suma = direccion + empuje * fuerza;
            suma.y = 0f;
            return suma.sqrMagnitude < 0.0001f ? direccion : suma.normalized;
        }
    }
}
