using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-656: reglas puras de subir y bajar del carro. El personaje primero se acomoda en el punto de la puerta (pegado al carro, sin flotar),
    /// luego juega el clip y termina sentado; al bajar es lo contrario.
    /// </summary>
    public static class ReglasEntradaCarro
    {
        /// <summary>Rapidez con la que el personaje se acomoda junto a la puerta (m/s).</summary>
        public const float VelocidadAcercar = 4f;
        /// <summary>Lo menos que dura acomodarse junto a la puerta (s).</summary>
        public const float SegundosMinimosAcercar = 0.08f;
        /// <summary>Lo más que dura acomodarse junto a la puerta (s): la animación no debe volverse lenta.</summary>
        public const float SegundosMaximosAcercar = 0.3f;
        /// <summary>Cuánto se arrima el punto de la puerta hacia el asiento, desde el punto de subida (m): queda casi pegado a la carrocería.</summary>
        public const float RetiroPuerta = 0.8f;

        /// <summary>Segundos que tarda en llegar a la puerta desde <paramref name="distancia"/> metros.</summary>
        public static float SegundosAcercar(float distancia)
        {
            return Mathf.Clamp(distancia / VelocidadAcercar, SegundosMinimosAcercar, SegundosMaximosAcercar);
        }

        /// <summary>El punto junto al carro donde se para el personaje: el punto de subida (en el piso) arrimado hacia el asiento, en horizontal.</summary>
        public static Vector3 PuntoPuerta(Vector3 puntoSubida, Vector3 asiento)
        {
            Vector3 hacia = asiento - puntoSubida;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            if (distancia < 1e-4f)
            {
                return puntoSubida;
            }
            return puntoSubida + hacia / distancia * Mathf.Min(RetiroPuerta, distancia);
        }

        /// <summary>De dos puertas, la más cercana (en horizontal) a <paramref name="desde"/>.</summary>
        public static Vector3 PuertaMasCercana(Vector3 desde, Vector3 a, Vector3 b)
        {
            return Plano(a - desde).sqrMagnitude <= Plano(b - desde).sqrMagnitude ? a : b;
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
