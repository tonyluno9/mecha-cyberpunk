using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Cómo se ve el jugador. Son índices al catálogo de Tony (<c>CatalogoApariencia</c>, Personaje/, T-123); nunca son
    /// negativos y Alan no los interpreta: para mostrar al jugador coloca <c>Personaje/Prefabs/CuerpoJugador.prefab</c> (Tony),
    /// que se pinta solo con el perfil registrado. Se compara por valor.
    /// </summary>
    public readonly struct AparienciaJugador : IEquatable<AparienciaJugador>
    {
        /// <summary>Índice de la cara.</summary>
        public int Cara { get; }

        /// <summary>Índice de la ropa.</summary>
        public int Ropa { get; }

        /// <summary>Índice del implante visible; 0 es sin implante visible.</summary>
        public int Implante { get; }

        /// <summary>Crea una apariencia con los tres índices.</summary>
        public AparienciaJugador(int cara, int ropa, int implante)
        {
            Cara = cara;
            Ropa = ropa;
            Implante = implante;
        }

        /// <summary>Apariencia de respaldo: cara 0, ropa 0, sin implante visible.</summary>
        public static AparienciaJugador Predeterminada => new AparienciaJugador(0, 0, 0);

        /// <summary>Igualdad por valor.</summary>
        public bool Equals(AparienciaJugador otra) => Cara == otra.Cara && Ropa == otra.Ropa && Implante == otra.Implante;

        /// <summary>Igualdad por valor con cualquier objeto.</summary>
        public override bool Equals(object obj) => obj is AparienciaJugador otra && Equals(otra);

        /// <summary>Código hash por valor.</summary>
        public override int GetHashCode() => HashCode.Combine(Cara, Ropa, Implante);

        /// <summary>Igualdad por valor.</summary>
        public static bool operator ==(AparienciaJugador a, AparienciaJugador b) => a.Equals(b);

        /// <summary>Desigualdad por valor.</summary>
        public static bool operator !=(AparienciaJugador a, AparienciaJugador b) => !a.Equals(b);
    }
}
