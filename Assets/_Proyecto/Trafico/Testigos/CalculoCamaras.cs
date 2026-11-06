using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>Dónde van las cámaras de seguridad (T-662), sin escena: en las esquinas de los cruces de calles, mirando hacia el cruce.</summary>
    public static class CalculoCamaras
    {
        private const float GradosPorVuelta = 360f;
        // Esquinas en orden: primero dos opuestas (así una cámara cubre lo que la otra no ve), luego las otras dos.
        private static readonly Vector2[] Esquinas4 = { new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f) };

        public readonly struct Colocacion
        {
            public Colocacion(Vector3 posicion, float giro)
            {
                Posicion = posicion;
                Giro = giro;
            }

            public Vector3 Posicion { get; }

            /// <summary>Giro en Y (°) para que el brazo de la cámara apunte hacia el centro del cruce.</summary>
            public float Giro { get; }
        }

        /// <summary>
        /// Una colocación por cada esquina elegida de cada cruce (nodo con tres o cuatro calles). <paramref name="porCruce"/> va de 1 a 4 y
        /// <paramref name="desfase"/> es lo que se aleja el poste del centro del cruce en X y en Z (m: mitad de la calle más la banqueta).
        /// </summary>
        public static List<Colocacion> Colocaciones(GrafoCalles calles, int porCruce, float desfase)
        {
            List<Colocacion> resultado = new List<Colocacion>();
            int cuantas = Mathf.Clamp(porCruce, 0, Esquinas4.Length);
            for (int i = 0; i < calles.Cantidad; i++)
            {
                if (calles.Vecinos(i).Count < 3)
                {
                    continue;
                }
                Vector3 centro = calles.Nodo(i);
                for (int e = 0; e < cuantas; e++)
                {
                    Vector3 poste = centro + new Vector3(Esquinas4[e].x * desfase, 0f, Esquinas4[e].y * desfase);
                    Vector3 hacia = centro - poste;
                    float giro = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
                    resultado.Add(new Colocacion(poste, (giro + GradosPorVuelta) % GradosPorVuelta));
                }
            }
            return resultado;
        }
    }
}
