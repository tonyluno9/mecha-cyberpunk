using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T-710: la geometría del tiro del dron: qué tan cerca pasa la bala del jugador y si eso hace sonar el silbido de la bala rozando.</summary>
    public static class CalculoTiroDron
    {
        /// <summary>Una bala que pasa a menos de estos metros del jugador (y no le pega) silba.</summary>
        public const float RadioRoce = 2.5f;

        /// <summary>Distancia (m) del punto a la bala (rayo desde el origen hacia la dirección); <paramref name="masCercano"/> es el punto del rayo más próximo.</summary>
        public static float DistanciaALaBala(Vector3 origen, Vector3 direccion, Vector3 punto, out Vector3 masCercano)
        {
            Vector3 d = direccion.sqrMagnitude > 0f ? direccion.normalized : Vector3.forward;
            float t = Mathf.Max(0f, Vector3.Dot(punto - origen, d));
            masCercano = origen + d * t;
            return Vector3.Distance(masCercano, punto);
        }

        /// <summary>True si la bala pasó lo bastante cerca como para silbar.</summary>
        public static bool Roza(float distancia, float radio) => distancia <= radio;
    }
}
