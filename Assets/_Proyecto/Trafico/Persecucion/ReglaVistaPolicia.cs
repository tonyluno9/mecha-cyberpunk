using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Reglas puras de la persecución realista (T-587): a dónde se adelanta una patrulla que corta el paso, cuándo el jugador ya está perdido
    /// y si un punto cae dentro de lo que ve una cámara (para aparecer y retirarse fuera de la vista).
    /// </summary>
    public static class ReglaVistaPolicia
    {
        private const float MargenVistaPorDefecto = 0.1f;
        private const float AlturaDelPunto = 1f;

        /// <summary>
        /// Donde estará el jugador en <paramref name="segundos"/> s si sigue a su velocidad; si va más lento que <paramref name="velocidadMinima"/>
        /// (m/s) se queda donde está. Solo cuenta el plano.
        /// </summary>
        public static Vector3 PuntoDeCorte(Vector3 posicion, Vector3 velocidad, float segundos, float velocidadMinima)
        {
            velocidad.y = 0f;
            return velocidad.magnitude < velocidadMinima ? posicion : posicion + velocidad * segundos;
        }

        /// <summary>True si ya pasaron <paramref name="umbral"/> s o más sin que nadie lo vea.</summary>
        public static bool Perdido(float segundosSinVer, float umbral) => segundosSinVer >= umbral;

        /// <summary>Calor extra que baja en <paramref name="dt"/> s: solo si ya lo perdieron.</summary>
        public static float Enfriamiento(float segundosSinVer, float umbral, float porSegundo, float dt)
        {
            return Perdido(segundosSinVer, umbral) && dt > 0f ? porSegundo * dt : 0f;
        }

        /// <summary>True si <paramref name="punto"/> cae dentro de lo que ve la cámara (con un margen) a menos de <paramref name="alcance"/> m.</summary>
        public static bool LoVe(Camera camara, Vector3 punto, float alcance)
        {
            if (camara == null)
            {
                return false;
            }
            Vector3 v = camara.WorldToViewportPoint(punto + Vector3.up * AlturaDelPunto);
            return v.z > 0f && v.z < alcance && v.x > -MargenVistaPorDefecto && v.x < 1f + MargenVistaPorDefecto
                && v.y > -MargenVistaPorDefecto && v.y < 1f + MargenVistaPorDefecto;
        }
    }
}
