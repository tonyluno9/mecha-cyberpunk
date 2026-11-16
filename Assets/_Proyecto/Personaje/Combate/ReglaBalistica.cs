using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1013: tiro parabólico para el lanzagranadas y la granada (lógica pura). Sin rozamiento: p(t) = origen + v t + g t² / 2.
    /// </summary>
    public static class ReglaBalistica
    {
        /// <summary>La posición a los <paramref name="t"/> segundos de salir con velocidad <paramref name="velocidad"/>.</summary>
        public static Vector3 Posicion(Vector3 origen, Vector3 velocidad, Vector3 gravedad, float t) => origen + velocidad * t + gravedad * (0.5f * t * t);

        /// <summary>La velocidad a los <paramref name="t"/> segundos.</summary>
        public static Vector3 Velocidad(Vector3 velocidad, Vector3 gravedad, float t) => velocidad + gravedad * t;

        /// <summary>
        /// La dirección (unitaria) para que algo que sale de <paramref name="origen"/> a <paramref name="rapidez"/> m/s caiga en <paramref name="blanco"/>,
        /// con el ángulo bajo (tiro tenso). Si no alcanza, devuelve false y la dirección a 45° hacia el blanco (lo más lejos que llega).
        /// </summary>
        public static bool DireccionHacia(Vector3 origen, Vector3 blanco, float rapidez, float gravedad, out Vector3 direccion)
        {
            Vector3 delta = blanco - origen;
            Vector3 plano = new Vector3(delta.x, 0f, delta.z);
            float x = plano.magnitude;
            float y = delta.y;
            Vector3 frente = x > 0.0001f ? plano / x : Vector3.forward;
            float g = Mathf.Abs(gravedad);
            if (g < 0.0001f || x < 0.0001f)
            {
                direccion = delta.sqrMagnitude > 0.0001f ? delta.normalized : frente;
                return true;
            }
            float v2 = rapidez * rapidez;
            float discriminante = v2 * v2 - g * (g * x * x + 2f * y * v2);
            if (discriminante < 0f)
            {
                direccion = (frente + Vector3.up).normalized;
                return false;
            }
            float angulo = Mathf.Atan((v2 - Mathf.Sqrt(discriminante)) / (g * x));
            direccion = (frente * Mathf.Cos(angulo) + Vector3.up * Mathf.Sin(angulo)).normalized;
            return true;
        }

        /// <summary>Llena <paramref name="puntos"/> con el arco de 0 a <paramref name="segundos"/> s, en partes iguales de tiempo.</summary>
        public static void Arco(Vector3 origen, Vector3 velocidad, Vector3 gravedad, float segundos, Vector3[] puntos)
        {
            int n = puntos.Length;
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? segundos * i / (n - 1) : 0f;
                puntos[i] = Posicion(origen, velocidad, gravedad, t);
            }
        }

        /// <summary>Gira la dirección <paramref name="grados"/> hacia arriba (sobre el eje horizontal que la cruza); sin pasar de la vertical.</summary>
        public static Vector3 Elevar(Vector3 direccion, float grados)
        {
            Vector3 eje = Vector3.Cross(Vector3.up, direccion);
            if (eje.sqrMagnitude < 0.0001f)
            {
                return direccion.normalized;
            }
            Vector3 girada = Quaternion.AngleAxis(-grados, eje.normalized) * direccion.normalized;
            return Vector3.Dot(girada, Vector3.up) > 0.999f ? Vector3.up : girada.normalized;
        }

        /// <summary>El daño de una explosión a <paramref name="distancia"/> m del centro: completo en el centro, <paramref name="enElBorde"/> en el borde y 0 fuera.</summary>
        public static float DanioExplosion(float danio, float radio, float distancia, float enElBorde)
        {
            if (radio <= 0f || distancia > radio)
            {
                return 0f;
            }
            return danio * Mathf.Lerp(1f, enElBorde, Mathf.Clamp01(distancia / radio));
        }
    }
}
