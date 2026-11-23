using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1714: lógica pura para que los pandilleros no se apilen. Reparte a cada miembro de una riña su propio punto en un anillo alrededor del rival,
    /// empuja suavemente a los que están a menos de <see cref="RadioSeparacion"/> de otro, y saca de encima al que quedó parado sobre la cápsula de otro.
    /// </summary>
    public static class ReglaSeparacionPandilla
    {
        /// <summary>Por debajo de esta distancia (m) un pandillero empuja a su vecino.</summary>
        public const float RadioSeparacion = 1.2f;
        /// <summary>Velocidad máxima (m/s) del empuje suave.</summary>
        public const float VelocidadEmpuje = 2.2f;
        /// <summary>Velocidad (m/s) con la que se baja de encima de otro pandillero.</summary>
        public const float VelocidadBajar = 4f;
        /// <summary>Radio mínimo y máximo (m) del anillo de la riña alrededor del rival.</summary>
        public const float AnilloMin = 1.5f;
        public const float AnilloMax = 3f;
        /// <summary>Si el pie está más de esta altura (m) sobre el de otro pandillero a menos de <see cref="RadioEncima"/> (m), está encima de él.</summary>
        public const float AlturaEncima = 0.5f;
        public const float RadioEncima = 0.9f;
        private const float AnguloAureo = 137.508f;
        private const int Radios = 4;

        /// <summary>Punto del anillo de <paramref name="indice"/> (ángulo distinto por índice, radio entre <see cref="AnilloMin"/> y <see cref="AnilloMax"/>), relativo al rival.</summary>
        public static Vector3 PuntoAnillo(Vector3 rival, int indice)
        {
            float angulo = indice * AnguloAureo * Mathf.Deg2Rad;
            float radio = AnilloMin + (AnilloMax - AnilloMin) * ((indice % Radios) / (float)(Radios - 1));
            return rival + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radio;
        }

        /// <summary>True si <paramref name="yo"/> está de pie sobre <paramref name="otro"/>.</summary>
        public static bool EstaEncima(Vector3 yo, Vector3 otro)
        {
            Vector3 plano = yo - otro;
            plano.y = 0f;
            return plano.magnitude < RadioEncima && yo.y - otro.y > AlturaEncima;
        }

        /// <summary>
        /// Velocidad plana (m/s) que separa a <paramref name="yo"/> de <paramref name="otros"/>. Cada vecino a menos de <see cref="RadioSeparacion"/> empuja
        /// más cuanto más cerca; con uno exactamente en el mismo punto se usa <paramref name="semilla"/> para elegir hacia dónde.
        /// </summary>
        public static Vector3 Empuje(Vector3 yo, IReadOnlyList<Vector3> otros, int semilla)
        {
            Vector3 suma = Vector3.zero;
            bool encima = false;
            for (int i = 0; i < otros.Count; i++)
            {
                Vector3 d = yo - otros[i];
                d.y = 0f;
                float dist = d.magnitude;
                if (dist >= RadioSeparacion)
                {
                    continue;
                }
                if (EstaEncima(yo, otros[i]))
                {
                    encima = true;
                }
                if (dist < 0.01f)
                {
                    float a = (semilla * AnguloAureo + i * 53f) * Mathf.Deg2Rad;
                    d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    dist = 0f;
                }
                else
                {
                    d /= dist;
                }
                suma += d * (1f - dist / RadioSeparacion);
            }
            if (suma.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }
            float tope = encima ? VelocidadBajar : VelocidadEmpuje;
            return suma.normalized * Mathf.Min(tope, suma.magnitude * tope * 2f + (encima ? VelocidadBajar : 0f));
        }
    }
}
