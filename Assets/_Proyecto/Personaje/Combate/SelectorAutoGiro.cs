using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1003: lógica pura del auto-giro del golpe. Entre los candidatos (posiciones en el plano XZ) elige el más cercano
    /// que esté dentro del radio y dentro del cono al frente.
    /// </summary>
    public static class SelectorAutoGiro
    {
        /// <summary>Índice del candidato elegido, o -1 si ninguno entra. El eje Y del Vector2 es la Z del mundo.</summary>
        public static int Elegir(Vector2 origen, Vector2 frente, IReadOnlyList<Vector2> candidatos, float radio, float conoMedioGrados)
        {
            int mejor = -1;
            float mejorDistancia = float.MaxValue;
            for (int i = 0; i < candidatos.Count; i++)
            {
                Vector2 delta = candidatos[i] - origen;
                float distancia = delta.magnitude;
                if (distancia > radio || distancia >= mejorDistancia)
                {
                    continue;
                }
                // Encima del jugador no hay dirección: se acepta sin mirar el cono.
                if (distancia > Mathf.Epsilon && Vector2.Angle(frente, delta) > conoMedioGrados)
                {
                    continue;
                }
                mejor = i;
                mejorDistancia = distancia;
            }
            return mejor;
        }

        /// <summary>Ángulo de giro en Y (°) para mirar hacia una dirección del plano XZ.</summary>
        public static float AnguloYaw(Vector2 direccion)
        {
            return Mathf.Atan2(direccion.x, direccion.y) * Mathf.Rad2Deg;
        }
    }
}
