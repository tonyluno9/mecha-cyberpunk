using MechaCyberpunk.Combate.Voces;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje
{
    /// <summary>La voz del jugador según su personaje (T-597): la cara 0 del perfil es la mujer y la 1 el hombre.</summary>
    public static class VozJugador
    {
        /// <summary>El género de la voz del jugador ahora (masculina si no hay perfil).</summary>
        public static GeneroVoz Genero()
        {
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            return perfil != null ? EtiquetaVoz.DeCara(perfil.Apariencia.Cara) : GeneroVoz.Masculina;
        }

        /// <summary>El ID con la etiqueta de la voz del jugador, por ejemplo "JUG_DANIO@F".</summary>
        public static string Id(string id) => EtiquetaVoz.De(id, Genero());

        // T-voces-personajes-23nov: las voces grabadas del jugador (Assets/Arte/Audio/19_Voces/Jugador). Suenan en 2D.
        public const string Esfuerzo = "JUG_ESFUERZO";
        public const string Salto = "JUG_SALTO";
        public const string DolorLeve = "JUG_DOLOR_LEVE";
        public const string DolorFuerte = "JUG_DOLOR_FUERTE";
        public const string Muerte = "JUG_MUERTE";
        public const string Quemado = "JUG_QUEMADO";
        public const string Tos = "JUG_TOS";
        public const string RespiracionCansado = "JUG_RESPIRACION_CANSADO";

        /// <summary>Fracción de la vida máxima desde la que un golpe duele fuerte (abajo, dolor leve).</summary>
        public const float FraccionDolorFuerte = 0.15f;

        /// <summary>El dolor según el daño que entró: fuerte desde <see cref="FraccionDolorFuerte"/> de la vida máxima (o una explosión), si no leve. "" sin daño.</summary>
        public static string IdDolor(float aplicado, float vidaMaxima, bool explosion)
        {
            if (aplicado <= 0f) return string.Empty;
            return explosion || (vidaMaxima > 0f && aplicado >= vidaMaxima * FraccionDolorFuerte) ? DolorFuerte : DolorLeve;
        }
    }
}
