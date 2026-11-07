using System;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>Cómo fue el choque contra un carro del tráfico (T-703).</summary>
    public enum NivelChoque
    {
        /// <summary>Un roce: ni se entera.</summary>
        Ninguno,
        /// <summary>Choque leve: pita, se queda quieto unos segundos y sigue.</summary>
        Leve,
        /// <summary>Choque fuerte: pita, se queda quieto y el conductor se baja enojado.</summary>
        Fuerte,
    }

    /// <summary>
    /// Reglas puras de la reacción de un carro del tráfico al chocar (T-703): según la rapidez relativa del golpe decide el nivel, cuánto se queda
    /// detenido y si el conductor se baja. El umbral leve queda por encima de los roces; el fuerte es un golpe de carro a buena velocidad.
    /// </summary>
    public static class ReglaChoqueTrafico
    {
        /// <summary>Rapidez relativa (m/s) desde la que cuenta como choque leve.</summary>
        public const float UmbralLeve = 3f;
        /// <summary>Rapidez relativa (m/s) desde la que cuenta como choque fuerte (el conductor se baja).</summary>
        public const float UmbralFuerte = 9f;
        /// <summary>Segundos detenido tras un choque leve.</summary>
        public const float SegundosLeve = 4f;
        /// <summary>Segundos detenido tras un choque fuerte (el conductor sale, reclama y vuelve a subir).</summary>
        public const float SegundosFuerte = 9f;
        /// <summary>Segundos que el conductor enojado se queda afuera antes de volver al carro.</summary>
        public const float SegundosConductorAfuera = 6f;
        /// <summary>Segundos mínimos entre dos choques que reaccionan (el mismo golpe rebota varias veces).</summary>
        public const float SegundosEntreChoques = 1.5f;

        public static NivelChoque Nivel(float rapidezRelativa)
        {
            if (rapidezRelativa >= UmbralFuerte)
            {
                return NivelChoque.Fuerte;
            }
            return rapidezRelativa >= UmbralLeve ? NivelChoque.Leve : NivelChoque.Ninguno;
        }

        /// <summary>Segundos que se queda detenido el carro según el choque.</summary>
        public static float SegundosDetenido(NivelChoque nivel)
        {
            switch (nivel)
            {
                case NivelChoque.Leve: return SegundosLeve;
                case NivelChoque.Fuerte: return SegundosFuerte;
                default: return 0f;
            }
        }

        /// <summary>True si el conductor se baja a reclamar.</summary>
        public static bool ConductorSeBaja(NivelChoque nivel) => nivel == NivelChoque.Fuerte;

        /// <summary>True si ya pasó el tiempo de espera entre choques (<paramref name="ahora"/> y <paramref name="ultimo"/> en segundos).</summary>
        public static bool PuedeReaccionar(float ahora, float ultimo) => ahora - ultimo >= SegundosEntreChoques;

        /// <summary>Un número de azar entre 0 y 1 lo vuelve una velocidad de crucero dentro del rango [minima, maxima].</summary>
        public static float VelocidadDeCarro(float minima, float maxima, double azar01)
        {
            float bajo = Math.Min(minima, maxima);
            float alto = Math.Max(minima, maxima);
            return bajo + (alto - bajo) * (float)Math.Max(0.0, Math.Min(1.0, azar01));
        }
    }
}
