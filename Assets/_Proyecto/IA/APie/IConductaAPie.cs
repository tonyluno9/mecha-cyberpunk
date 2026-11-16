namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1106: una conducta distinta para un <see cref="PandilleroAPie"/> (riña, jefe con cuchilla, guardia con escudo...).
    /// Va como componente en el mismo objeto. Mientras <see cref="Controla"/> es true, el pandillero le cede el turno cada cuadro
    /// (después de buscar al jugador) y no corre su IA normal.
    /// </summary>
    public interface IConductaAPie
    {
        /// <summary>True si la conducta lleva el turno; false deja que el pandillero haga su IA de siempre.</summary>
        bool Controla { get; }

        /// <summary>Un cuadro de la conducta, con el tiempo <paramref name="dt"/> (s).</summary>
        void Actualizar(PandilleroAPie p, float dt);
    }
}
