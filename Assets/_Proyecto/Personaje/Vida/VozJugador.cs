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
    }
}
