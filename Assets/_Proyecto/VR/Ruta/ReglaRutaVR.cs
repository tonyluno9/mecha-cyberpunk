using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.VR.Ruta
{
    /// <summary>
    /// VR41: lógica pura de la ruta al objetivo en VR, la línea turquesa de antes (la GuiaHistoria de PC que quitó T-51): se apaga al llegar
    /// (&lt; <see cref="MetrosLlegada"/> m en el plano) y la ruta se pasa a puntos sin tramos más cortos que <see cref="MetrosMinimosEntrePuntos"/> m,
    /// recortada a <see cref="MetrosMaximos"/> m desde el jugador. (Los chevrones de VR30 se quitaron.)
    /// </summary>
    public static class ReglaRutaVR
    {
        /// <summary>A menos de esto (m, en el plano) del objetivo la ruta se apaga.</summary>
        public const float MetrosLlegada = 3f;
        /// <summary>Largo máximo de la línea desde el jugador (m), como la de antes.</summary>
        public const float MetrosMaximos = 90f;
        /// <summary>Puntos de la ruta más juntos que esto (m) se saltan (salvo el último).</summary>
        public const float MetrosMinimosEntrePuntos = 1.5f;
        /// <summary>Sin calles ni NavMesh, la recta que apunta al objetivo mide como mucho esto (m).</summary>
        public const float MetrosRectaCorta = 25f;

        /// <summary>True si ya llegó (la ruta se apaga).</summary>
        public static bool Llego(Vector3 jugador, Vector3 objetivo)
        {
            jugador.y = 0f;
            objetivo.y = 0f;
            return Vector3.Distance(jugador, objetivo) < MetrosLlegada;
        }

        /// <summary>
        /// Llena <paramref name="salida"/> con los puntos de <paramref name="ruta"/> (ya sobre el piso): se salta los que quedan a menos de
        /// <see cref="MetrosMinimosEntrePuntos"/> del anterior (el último siempre va) y corta la línea a <see cref="MetrosMaximos"/> m. Devuelve cuántos puso.
        /// </summary>
        public static int Recortar(IReadOnlyList<Vector3> ruta, List<Vector3> salida)
        {
            salida.Clear();
            if (ruta == null)
            {
                return 0;
            }
            float acumulado = 0f;
            Vector3 anterior = default;
            for (int i = 0; i < ruta.Count; i++)
            {
                Vector3 p = ruta[i];
                if (salida.Count > 0)
                {
                    float d = Vector3.Distance(anterior, p);
                    if (d < MetrosMinimosEntrePuntos && i < ruta.Count - 1)
                    {
                        continue;
                    }
                    if (acumulado + d > MetrosMaximos)
                    {
                        salida.Add(Vector3.Lerp(anterior, p, (MetrosMaximos - acumulado) / Mathf.Max(0.01f, d)));
                        return salida.Count;
                    }
                    acumulado += d;
                }
                salida.Add(p);
                anterior = p;
            }
            return salida.Count;
        }

        /// <summary>El final de la recta corta del jugador hacia el objetivo (en el plano del jugador), cuando no hay ni calles ni NavMesh.</summary>
        public static Vector3 FinRectaCorta(Vector3 jugador, Vector3 objetivo)
        {
            Vector3 hacia = objetivo - jugador;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 1e-6f)
            {
                return jugador;
            }
            return jugador + hacia.normalized * Mathf.Min(MetrosRectaCorta, hacia.magnitude);
        }
    }
}
