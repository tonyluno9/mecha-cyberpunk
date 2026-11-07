using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-708: dónde sale una patrulla nueva para que llegue con un tiempo real. Se mide por la ruta de las calles (no en línea recta): sale en un punto
    /// permitido (fuera de la vista y sin otra patrulla encima) cuya ruta al jugador toma al menos <c>segundosMinimos</c> a la velocidad de la patrulla,
    /// el más cercano a <c>segundosObjetivo</c>. Si ninguno llega al mínimo sale en el que tarde más.
    /// </summary>
    public static class CalculoLlegadaPatrulla
    {
        /// <summary>Metros por las calles de <paramref name="desde"/> a <paramref name="hasta"/>; float.MaxValue si no hay ruta.</summary>
        public static float MetrosDeRuta(GrafoCalles calles, int desde, int hasta)
        {
            List<int> camino = calles.Camino(desde, hasta);
            if (camino.Count == 0)
            {
                return float.MaxValue;
            }
            float total = 0f;
            for (int i = 1; i < camino.Count; i++)
            {
                total += Vector3.Distance(calles.Nodo(camino[i - 1]), calles.Nodo(camino[i]));
            }
            return total;
        }

        /// <summary>Índice del punto de salida elegido, o -1 si ninguno está permitido.</summary>
        public static int Elegir(IReadOnlyList<float> metrosDeRuta, IReadOnlyList<bool> permitido, float velocidad, float segundosObjetivo, float segundosMinimos)
        {
            int mejor = -1;
            float errorMejor = float.MaxValue;
            int masLejano = -1;
            float segundosMasLejano = -1f;
            for (int i = 0; i < metrosDeRuta.Count; i++)
            {
                if (!permitido[i] || metrosDeRuta[i] == float.MaxValue)
                {
                    continue;
                }
                float segundos = metrosDeRuta[i] / Mathf.Max(0.01f, velocidad);
                if (segundos > segundosMasLejano)
                {
                    segundosMasLejano = segundos;
                    masLejano = i;
                }
                if (segundos >= segundosMinimos && Mathf.Abs(segundos - segundosObjetivo) < errorMejor)
                {
                    errorMejor = Mathf.Abs(segundos - segundosObjetivo);
                    mejor = i;
                }
            }
            return mejor >= 0 ? mejor : masLejano;
        }
    }
}
