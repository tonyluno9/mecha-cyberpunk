using UnityEngine;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>La vista de una cámara de seguridad (T-662), sin escena: un punto cae en su cono de visión (sin contar paredes).</summary>
    public static class CalculoVistaCamara
    {
        /// <summary>True si <paramref name="punto"/> está a menos de <paramref name="alcance"/> m y dentro de <paramref name="semiAngulo"/>° de <paramref name="adelante"/>.</summary>
        public static bool EnCono(Vector3 origen, Vector3 adelante, float semiAngulo, float alcance, Vector3 punto)
        {
            Vector3 hacia = punto - origen;
            float distancia = hacia.magnitude;
            if (distancia > alcance)
            {
                return false;
            }
            if (distancia < 0.0001f)
            {
                return true;
            }
            return Vector3.Angle(adelante, hacia) <= semiAngulo;
        }
    }
}
