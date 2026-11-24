using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Casino.Meseras
{
    /// <summary>
    /// T-voces-personajes-23nov: cuándo habla una mesera robot del casino VivaCoin (Assets/Arte/Audio/19_Voces/Mesera_Robot). Saluda al entrar el jugador
    /// al casino (la más cercana) y cuando pasa junto a ella; dice SERVIR al detenerse en su punto con el jugador cerca; SUERTE cuando el jugador echa a
    /// andar una tragamonedas cerca de ella; y se despide cuando sale del casino. Una sola mesera habla a la vez y hay enfriamientos. Lógica pura.
    /// </summary>
    public static class ReglaVozMesera
    {
        public const string Saludo = "MESERA_ROBOT_SALUDO";
        public const string Servir = "MESERA_ROBOT_SERVIR";
        public const string Suerte = "MESERA_ROBOT_SUERTE";
        public const string Despedida = "MESERA_ROBOT_DESPEDIDA";

        /// <summary>El jugador a menos de esto (m) de una mesera: lo saluda al pasar.</summary>
        public const float DistanciaSaludo = 3f;
        /// <summary>Al detenerse en su punto, dice SERVIR si el jugador está a menos de esto (m).</summary>
        public const float DistanciaServir = 6f;
        /// <summary>La mesera más cercana a la tragamonedas, a menos de esto (m), le desea suerte.</summary>
        public const float DistanciaSuerte = 12f;
        /// <summary>Al entrar o salir del casino habla la mesera más cercana al jugador, si está a menos de esto (m).</summary>
        public const float DistanciaPuerta = 30f;

        /// <summary>Segundos mínimos entre dos frases de cualquier mesera (una sola a la vez, sin encimarse).</summary>
        public const float SegundosEntreFrases = 3f;
        /// <summary>Segundos mínimos entre dos saludos al pasar de la misma mesera.</summary>
        public const float SegundosSaludoPorMesera = 40f;
        /// <summary>Segundos mínimos entre dos SERVIR de la misma mesera.</summary>
        public const float SegundosServirPorMesera = 25f;
        /// <summary>Segundos mínimos entre dos SUERTE (de cualquier mesera).</summary>
        public const float SegundosEntreSuertes = 30f;
        /// <summary>Segundos mínimos entre dos saludos (o dos despedidas) de puerta.</summary>
        public const float SegundosEntrePuerta = 20f;

        /// <summary>True si pasó <paramref name="segundos"/> desde <paramref name="ultimo"/>.</summary>
        public static bool Toca(float ahora, float ultimo, float segundos) => ahora - ultimo >= segundos;

        /// <summary>El índice de la posición más cercana a <paramref name="punto"/> (en el plano) a menos de <paramref name="maxima"/> m, o -1.</summary>
        public static int MasCercana(IReadOnlyList<Vector3> posiciones, Vector3 punto, float maxima)
        {
            int mejor = -1;
            float mejorDistancia = maxima * maxima;
            for (int i = 0; i < posiciones.Count; i++)
            {
                Vector3 d = posiciones[i] - punto;
                d.y = 0f;
                float distancia = d.sqrMagnitude;
                if (distancia <= mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>Si dice SERVIR: acaba de detenerse (caminaba y ya no), con el jugador cerca y fuera de su enfriamiento.</summary>
        public static bool TocaServir(bool caminabaAntes, bool caminaAhora, float distanciaJugador, float ahora, float ultimoServir) =>
            caminabaAntes && !caminaAhora && distanciaJugador <= DistanciaServir && Toca(ahora, ultimoServir, SegundosServirPorMesera);

        /// <summary>Si saluda al pasar: el jugador está muy cerca y pasó su enfriamiento.</summary>
        public static bool TocaSaludo(float distanciaJugador, float ahora, float ultimoSaludo) =>
            distanciaJugador <= DistanciaSaludo && Toca(ahora, ultimoSaludo, SegundosSaludoPorMesera);
    }
}
