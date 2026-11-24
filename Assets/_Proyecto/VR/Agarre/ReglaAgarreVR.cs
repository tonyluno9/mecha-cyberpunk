using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR3: números y cálculos puros del agarre con el implante de BRAZOS: a qué distancia agarra la mano, con cuánta fuerza
    /// sale lanzado el enemigo al soltarlo o al golpearlo con la otra mano, cuánto daño le hace chocar y cuándo termina su vuelo.
    /// </summary>
    public static class ReglaAgarreVR
    {
        /// <summary>Distancia máxima (m) de la palma a la superficie del enemigo para agarrarlo.</summary>
        public const float RadioAgarre = 0.4f;

        /// <summary>Distancia máxima (m) de la palma de la otra mano al enemigo sostenido para que cuente como golpe.</summary>
        public const float RadioGolpe = 0.2f;

        /// <summary>Rapidez mínima (m/s, relativa al rig) de la otra mano para golpear al enemigo sostenido.</summary>
        public const float UmbralGolpe = 1.5f;

        /// <summary>Multiplicador de la velocidad de la mano al lanzar con BRAZOS nivel 1.</summary>
        public const float MultiplicadorNivel1 = 3f;

        /// <summary>Multiplicador de la velocidad de la mano al lanzar con BRAZOS nivel 2 o más.</summary>
        public const float MultiplicadorNivel2 = 4f;

        /// <summary>Tope (m/s) de la velocidad con que sale un enemigo.</summary>
        public const float VelocidadMaxima = 30f;

        /// <summary>Rapidez mínima (m/s) con que sale un enemigo golpeado mientras se sostiene.</summary>
        public const float VelocidadMinimaGolpe = 12f;

        /// <summary>Cuánto se eleva (fracción hacia arriba) la dirección del golpe para que salga volando y no a ras del piso.</summary>
        public const float ElevacionGolpe = 0.25f;

        /// <summary>Daño (puntos) del golpe con la otra mano al enemigo sostenido, antes del multiplicador del implante.</summary>
        public const float DanioGolpeBase = 20f;

        /// <summary>Rapidez de choque (m/s) por debajo de la cual chocar no hace daño.</summary>
        public const float UmbralChoque = 5f;

        /// <summary>Puntos de daño por cada m/s de choque por encima del umbral.</summary>
        public const float DanioPorMetroSegundo = 4f;

        /// <summary>Tope (puntos) del daño de un choque.</summary>
        public const float DanioMaximoChoque = 80f;

        /// <summary>Segundos mínimos entre dos choques que hacen daño (evita contar varias veces el mismo golpe contra la pared).</summary>
        public const float SegundosEntreChoques = 0.2f;

        /// <summary>Rapidez (m/s) por debajo de la cual el cuerpo cuenta como quieto.</summary>
        public const float RapidezQuieto = 0.5f;

        /// <summary>Segundos quieto para dar por terminado el vuelo.</summary>
        public const float SegundosQuieto = 0.3f;

        /// <summary>Segundos de vuelo antes de empezar a contar si está quieto (al soltarlo sin fuerza todavía va cayendo).</summary>
        public const float SegundosVueloMinimo = 0.5f;

        /// <summary>Segundos máximos de vuelo: pasado esto el enemigo vuelve a su IA aunque siga moviéndose.</summary>
        public const float SegundosVueloMaximo = 3f;

        /// <summary>Multiplicador del lanzamiento según el nivel de BRAZOS (0 sin implante: no se puede agarrar).</summary>
        public static float Multiplicador(int nivelBrazos)
        {
            if (nivelBrazos <= 0)
            {
                return 0f;
            }
            return nivelBrazos == 1 ? MultiplicadorNivel1 : MultiplicadorNivel2;
        }

        /// <summary>Velocidad (m/s) con que sale el enemigo al soltar el grip: la de la mano por el multiplicador, con tope.</summary>
        public static Vector3 VelocidadLanzamiento(Vector3 velocidadMano, int nivelBrazos)
        {
            return Vector3.ClampMagnitude(velocidadMano * Multiplicador(nivelBrazos), VelocidadMaxima);
        }

        /// <summary>Velocidad (m/s) con que sale el enemigo golpeado con la otra mano: en la dirección del golpe, un poco hacia arriba, fuerte.</summary>
        public static Vector3 VelocidadGolpe(Vector3 velocidadOtraMano, int nivelBrazos)
        {
            if (velocidadOtraMano.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }
            Vector3 direccion = (velocidadOtraMano.normalized + Vector3.up * ElevacionGolpe).normalized;
            float rapidez = Mathf.Max(VelocidadMinimaGolpe, velocidadOtraMano.magnitude * Multiplicador(nivelBrazos));
            return direccion * Mathf.Min(rapidez, VelocidadMaxima);
        }

        /// <summary>True si la otra mano golpea al sostenido: está a menos de <see cref="RadioGolpe"/> y va a más de <see cref="UmbralGolpe"/>.</summary>
        public static bool EsGolpe(float distancia, float rapidezMano) => distancia <= RadioGolpe && rapidezMano > UmbralGolpe;

        /// <summary>Daño (puntos) del golpe con la otra mano, con el multiplicador de golpe a pie del implante.</summary>
        public static float DanioGolpe(float multiplicadorGolpeAPie) => DanioGolpeBase * Mathf.Max(1f, multiplicadorGolpeAPie);

        /// <summary>Daño (puntos) de chocar a <paramref name="rapidez"/> (m/s): 0 bajo el umbral y con tope.</summary>
        public static float DanioChoque(float rapidez)
        {
            if (rapidez <= UmbralChoque)
            {
                return 0f;
            }
            return Mathf.Min(DanioMaximoChoque, (rapidez - UmbralChoque) * DanioPorMetroSegundo);
        }

        /// <summary>True si el vuelo terminó: se quedó quieto (tras el vuelo mínimo) o pasó el vuelo máximo.</summary>
        public static bool VueloTermino(float segundosVuelo, float segundosQuieto)
        {
            return segundosVuelo >= SegundosVueloMaximo || (segundosVuelo >= SegundosVueloMinimo && segundosQuieto >= SegundosQuieto);
        }
    }
}
