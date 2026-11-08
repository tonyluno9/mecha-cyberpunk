using UnityEngine;

namespace MechaCyberpunk.Combate.Efectos
{
    /// <summary>
    /// T-745: las cuentas del trazo de una bala. Una estela corta viaja de la boca al destino; al llegar, la cola la alcanza y el trazo se acaba
    /// justo en el punto de impacto. Si no pegó en nada (máximo alcance) se desvanece mientras se acaba. Nunca queda flotando.
    /// </summary>
    public static class ReglaTrazo
    {
        /// <summary>Rapidez de la estela (m/s).</summary>
        public const float Velocidad = 260f;

        /// <summary>Largo de la estela en vuelo (m).</summary>
        public const float Cola = 3f;

        /// <summary>Segundos que dura el trazo de un recorrido de <paramref name="distancia"/> metros.</summary>
        public static float Duracion(float distancia) => (Mathf.Max(0f, distancia) + Cola) / Velocidad;

        /// <summary>Dónde están la cola y la cabeza (m desde la boca, siempre entre 0 y la distancia) a los <paramref name="segundos"/>.</summary>
        public static void Tramo(float distancia, float segundos, out float cola, out float cabeza)
        {
            float recorrido = Mathf.Max(0f, segundos) * Velocidad;
            cabeza = Mathf.Clamp(recorrido, 0f, distancia);
            cola = Mathf.Clamp(recorrido - Cola, 0f, distancia);
        }

        /// <summary>Opacidad de 0 a 1: plena si pegó en algo; si no, baja hasta 0 al terminar.</summary>
        public static float Opacidad(bool pego, float distancia, float segundos)
        {
            if (pego)
            {
                return 1f;
            }
            float total = Duracion(distancia);
            return total <= 0f ? 0f : Mathf.Clamp01(1f - segundos / total);
        }

        /// <summary>True cuando el trazo ya terminó.</summary>
        public static bool Termino(float distancia, float segundos) => segundos >= Duracion(distancia);
    }
}
