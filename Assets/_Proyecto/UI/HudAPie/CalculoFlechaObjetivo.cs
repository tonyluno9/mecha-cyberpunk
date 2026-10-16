using System.Globalization;
using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Lógica pura de la flecha del objetivo: ángulo hacia él y texto de distancia.</summary>
    public static class CalculoFlechaObjetivo
    {
        private const float MetrosPorKilometro = 1000f;
        private const float MargenDegenerado = 0.0001f;

        /// <summary>
        /// Ángulo (°) en el plano horizontal entre hacia dónde mira la cámara y el objetivo: 0 enfrente,
        /// positivo a la derecha, negativo a la izquierda, ±180 atrás. Ignora la altura. Devuelve 0 si no se puede medir
        /// (cámara mirando recto arriba o abajo, o el objetivo justo debajo).
        /// </summary>
        public static float Angulo(Vector3 posicionCamara, Vector3 adelanteCamara, Vector3 posicionObjetivo)
        {
            Vector3 adelante = new Vector3(adelanteCamara.x, 0f, adelanteCamara.z);
            Vector3 haciaObjetivo = new Vector3(posicionObjetivo.x - posicionCamara.x, 0f, posicionObjetivo.z - posicionCamara.z);
            if (adelante.sqrMagnitude < MargenDegenerado || haciaObjetivo.sqrMagnitude < MargenDegenerado)
            {
                return 0f;
            }
            return Vector3.SignedAngle(adelante, haciaObjetivo, Vector3.up);
        }

        /// <summary>"45 m" por debajo de 1000 m y "1.2 km" desde ahí.</summary>
        public static string TextoDistancia(float metros)
        {
            int redondeados = Mathf.RoundToInt(Mathf.Max(0f, metros));
            if (redondeados < MetrosPorKilometro)
            {
                return redondeados + " m";
            }
            return (Mathf.Max(0f, metros) / MetrosPorKilometro).ToString("0.0", CultureInfo.InvariantCulture) + " km";
        }
    }
}
