using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Cuentas del minimapa redondo (T-409), sin Unity vivo: de un punto del mundo a píxeles desde el centro del círculo, girando para que
    /// hacia donde miras quede arriba, y qué pasa con lo que cae fuera del radio (el objetivo y el punto marcado se pegan al borde; los
    /// enemigos y los iconos no salen).
    /// </summary>
    public static class CalculoMinimapa
    {
        /// <summary>Píxeles por metro: el radio del círculo (px) entre los metros que muestra, por el zoom.</summary>
        public static float Escala(float radioPx, float radioMetros, float zoom) => radioMetros > 0f ? radioPx / radioMetros * zoom : 0f;

        /// <summary>
        /// Gira un vector del mapa (x este, y norte) para que el rumbo <paramref name="rumbo"/> quede arriba. El rumbo va en grados como el
        /// eulerAngles.y de la cámara: 0 al norte, 90 al este. Es el mismo giro (en Z, contra el reloj) que lleva el dibujo del minimapa.
        /// </summary>
        public static Vector2 Girar(Vector2 enMapa, float rumbo)
        {
            float radianes = rumbo * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radianes);
            float sen = Mathf.Sin(radianes);
            return new Vector2(enMapa.x * cos - enMapa.y * sen, enMapa.x * sen + enMapa.y * cos);
        }

        /// <summary>
        /// Dónde sale <paramref name="punto"/> en el minimapa (px desde el centro). Devuelve false si queda más lejos que <paramref name="radioPx"/>
        /// y no se pega al borde; si <paramref name="pegarAlBorde"/>, lo de fuera queda justo en el borde, en su dirección.
        /// </summary>
        public static bool Ubicar(Vector3 jugador, float rumbo, Vector3 punto, float escala, float radioPx, bool pegarAlBorde, out Vector2 px)
        {
            Vector2 relativo = new Vector2(punto.x - jugador.x, punto.z - jugador.z) * escala;
            px = Girar(relativo, rumbo);
            float distancia = px.magnitude;
            if (distancia <= radioPx)
            {
                return true;
            }
            if (!pegarAlBorde)
            {
                return false;
            }
            px = px / distancia * radioPx;
            return true;
        }

        /// <summary>Distancia en el suelo (m, solo X y Z).</summary>
        public static float DistanciaPlana(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }
}
