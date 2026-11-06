using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// La ruta tipo GPS del minimapa (T-632): del jugador al objetivo por las calles del barrio. UI no conoce las calles: se las pide a quien esté
    /// registrado como <see cref="IRutaCalles"/> (Trafico, T-669). Sin ninguno, o si no encuentra camino, traza una recta.
    /// </summary>
    public static class CalculoRutaGps
    {
        /// <summary>
        /// Puntos de la ruta (plano XZ, y = 0): empieza en <paramref name="origen"/>, termina en <paramref name="destino"/> y entre los dos va por las
        /// calles si hay quien las trace.
        /// </summary>
        public static List<Vector3> Ruta(Vector3 origen, Vector3 destino)
        {
            origen.y = 0f;
            destino.y = 0f;
            IRutaCalles calles = Registro<IRutaCalles>.Actual;
            List<Vector3> ruta = calles != null ? calles.Calcular(origen, destino) : null;
            return ruta != null && ruta.Count > 0 ? ruta : new List<Vector3> { origen, destino };
        }

        /// <summary>Largo (m) de una ruta.</summary>
        public static float Largo(IReadOnlyList<Vector3> ruta)
        {
            float total = 0f;
            for (int i = 1; i < ruta.Count; i++)
            {
                total += Vector3.Distance(ruta[i - 1], ruta[i]);
            }
            return total;
        }
    }
}
