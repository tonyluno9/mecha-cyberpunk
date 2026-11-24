using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-historia-libre-23nov: dónde puede aparecer un refuerzo del modo libre. Lejos (al menos <c>metrosMinimos</c> en el plano, también a tus espaldas)
    /// y fuera de la vista (más de <c>gradosVista</c> entre hacia dónde miras y el punto). Lógica pura: recibe posiciones, no lee la cámara.
    /// </summary>
    public static class ReglaPuntoRefuerzo
    {
        /// <summary>True si el refuerzo puede aparecer en <paramref name="punto"/>.</summary>
        /// <param name="jugador">Posición del jugador (o de la cámara).</param>
        /// <param name="adelante">Hacia dónde mira (solo cuenta en el plano).</param>
        /// <param name="metrosMinimos">Distancia mínima en el plano (m).</param>
        /// <param name="gradosVista">Medio ángulo de la vista (°): dentro de él el punto se ve.</param>
        public static bool Vale(Vector3 jugador, Vector3 adelante, Vector3 punto, float metrosMinimos, float gradosVista)
        {
            Vector3 hacia = punto - jugador;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < metrosMinimos * metrosMinimos)
            {
                return false;
            }
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f)
            {
                return true; // sin dirección de vista solo cuenta la distancia
            }
            return Vector3.Angle(adelante, hacia) > gradosVista;
        }
    }
}
