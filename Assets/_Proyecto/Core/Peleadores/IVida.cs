using System;

namespace MechaCyberpunk.Core
{
    /// <summary>Vida de un peleador.</summary>
    public interface IVida
    {
        /// <summary>Puntos de vida actuales.</summary>
        float Actual { get; }
        /// <summary>Puntos de vida al empezar la pelea.</summary>
        float Maxima { get; }
        /// <summary>True cuando la vida llegó a 0.</summary>
        bool EstaKO { get; }
        /// <summary>Se avisa cuando cambia <see cref="Actual"/>.</summary>
        event Action Cambio;
        /// <summary>Se avisa una sola vez por pelea, al llegar a 0.</summary>
        event Action KO;
    }
}
