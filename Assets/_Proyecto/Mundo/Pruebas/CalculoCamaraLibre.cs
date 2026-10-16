using UnityEngine;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>Cuentas puras de la cámara libre (sin MonoBehaviour), para probarlas en EditMode.</summary>
    public static class CalculoCamaraLibre
    {
        /// <summary>Inclinación nueva (°) tras mirar <paramref name="delta"/> grados, sin pasar de ±<paramref name="limite"/>.</summary>
        public static float LimitarInclinacion(float actual, float delta, float limite)
        {
            return Mathf.Clamp(actual + delta, -limite, limite);
        }

        /// <summary>
        /// True si <paramref name="punto"/> está a <paramref name="distanciaMaxima"/> m o menos de <paramref name="desde"/> y a
        /// <paramref name="anguloMaximo"/>° o menos de hacia dónde mira (<paramref name="mirada"/>). Todo en el plano horizontal,
        /// para que mirar un poco arriba o abajo no cambie la respuesta. Con el punto encima de quien mira, siempre es true.
        /// </summary>
        public static bool EnRangoDeSubida(Vector3 desde, Vector3 mirada, Vector3 punto, float distanciaMaxima, float anguloMaximo)
        {
            Vector3 haciaPunto = punto - desde;
            haciaPunto.y = 0f;
            mirada.y = 0f;
            if (haciaPunto.sqrMagnitude > distanciaMaxima * distanciaMaxima)
            {
                return false;
            }
            if (haciaPunto.sqrMagnitude < 0.0001f || mirada.sqrMagnitude < 0.0001f)
            {
                return true;
            }
            return Vector3.Angle(mirada, haciaPunto) <= anguloMaximo;
        }

        /// <summary>Rapidez deseada (m/s): volando manda la de vuelo; si no, correr o caminar.</summary>
        public static float Rapidez(bool volando, bool corriendo, float aPie, float correr, float volar)
        {
            if (volando)
            {
                return volar;
            }
            return corriendo ? correr : aPie;
        }

        /// <summary>Dirección horizontal en el mundo para la entrada (x = lado, y = frente) con la vista girada <paramref name="yaw"/>°.</summary>
        public static Vector3 DireccionHorizontal(Vector2 entrada, float yaw)
        {
            Vector3 local = new Vector3(entrada.x, 0f, entrada.y);
            if (local.sqrMagnitude > 1f)
            {
                local.Normalize();
            }
            return Quaternion.Euler(0f, yaw, 0f) * local;
        }

        /// <summary>Velocidad vertical nueva al caminar: pegada al piso si está en él, o acelerando hacia abajo.</summary>
        public static float VelocidadVertical(float actual, bool enSuelo, float gravedad, float dt, float pegadoAlSuelo)
        {
            if (enSuelo && actual < 0f)
            {
                return -pegadoAlSuelo;
            }
            return actual - gravedad * dt;
        }
    }
}
