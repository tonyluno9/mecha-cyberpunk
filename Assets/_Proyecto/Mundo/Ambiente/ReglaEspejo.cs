using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Cuentas del espejo de verdad (T-854), sin MonoBehaviour: el ojo reflejado en el plano del vidrio y el campo de visión que cubre el vidrio
    /// visto desde ahí.
    /// </summary>
    public static class ReglaEspejo
    {
        /// <summary>El punto reflejado en el plano que pasa por <paramref name="enPlano"/> con normal <paramref name="normal"/>.</summary>
        public static Vector3 Reflejar(Vector3 punto, Vector3 enPlano, Vector3 normal)
        {
            Vector3 n = normal.normalized;
            return punto - 2f * Vector3.Dot(punto - enPlano, n) * n;
        }

        /// <summary>
        /// Campo de visión vertical (°) de una cámara en <paramref name="ojo"/> que mira hacia <paramref name="adelante"/> y con aspecto 1 ve todas las
        /// esquinas, más el margen; entre <paramref name="minimo"/> y <paramref name="maximo"/>.
        /// </summary>
        public static float CampoParaCubrir(Vector3 ojo, Vector3 adelante, Vector3[] esquinas, float margen, float minimo, float maximo)
        {
            Vector3 f = adelante.normalized;
            float angulo = 0f;
            foreach (Vector3 e in esquinas)
            {
                angulo = Mathf.Max(angulo, Vector3.Angle(f, e - ojo));
            }
            // Con aspecto 1 la esquina más abierta cabe si el medio campo cubre su ángulo (la diagonal queda dentro).
            return Mathf.Clamp(angulo * 2f + margen, minimo, maximo);
        }
    }
}
