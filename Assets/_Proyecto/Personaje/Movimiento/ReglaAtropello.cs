using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>T-1327: lógica pura del atropello: cuándo un carro derriba al jugador y a dónde lo empuja.</summary>
    public static class ReglaAtropello
    {
        /// <summary>Rapidez del carro (m/s) a partir de la cual tira al jugador; más lento solo lo roza (~14 km/h).</summary>
        public const float RapidezMinima = 4f;

        /// <summary>True si un carro con esa velocidad (m/s) viene hacia el jugador (que está en <paramref name="haciaJugador"/> respecto al carro) y rápido.</summary>
        public static bool Derriba(Vector3 velocidadCarro, Vector3 haciaJugador, float rapidezMinima = RapidezMinima)
        {
            Vector3 v = new Vector3(velocidadCarro.x, 0f, velocidadCarro.z);
            if (v.magnitude < rapidezMinima)
            {
                return false;
            }
            Vector3 h = new Vector3(haciaJugador.x, 0f, haciaJugador.z);
            // Si el jugador está justo encima del centro del carro no hay "hacia": cuenta.
            return h.sqrMagnitude < 0.0001f || Vector3.Dot(v.normalized, h.normalized) > 0f;
        }

        /// <summary>Dirección horizontal del empujón: la del carro (si no hay, la contraria a donde está el carro).</summary>
        public static Vector3 DireccionEmpuje(Vector3 velocidadCarro, Vector3 haciaJugador)
        {
            Vector3 v = new Vector3(velocidadCarro.x, 0f, velocidadCarro.z);
            if (v.sqrMagnitude > 0.0001f)
            {
                return v.normalized;
            }
            Vector3 h = new Vector3(haciaJugador.x, 0f, haciaJugador.z);
            return h.sqrMagnitude > 0.0001f ? h.normalized : Vector3.forward;
        }
    }
}
