namespace MechaCyberpunk.Progresion
{
    /// <summary>Dónde se ofrece una misión (T-183). Solo se agregan valores al final.</summary>
    public enum OrigenMision
    {
        /// <summary>En la calle, con un vecino (punto Encargo).</summary>
        Calle,
        /// <summary>Por teléfono.</summary>
        Telefono,
        /// <summary>En el bar.</summary>
        Bar
    }

    /// <summary>En qué parte del día se ofrece una misión. Solo se agregan valores al final.</summary>
    public enum FranjaMision
    {
        Cualquiera,
        Dia,
        Noche
    }

    /// <summary>Qué hay que hacer en un paso de misión. Solo se agregan valores al final.</summary>
    public enum TipoPaso
    {
        /// <summary>Llegar a un punto.</summary>
        IrA,
        /// <summary>Recoger algo en un punto.</summary>
        Recoger,
        /// <summary>Entregar algo en un punto.</summary>
        Entregar,
        /// <summary>Ganar la pelea que se arma en un punto.</summary>
        Pelear,
        /// <summary>Perder al que te persigue (el calor baja al nivel 0).</summary>
        Escapar
    }

    /// <summary>Por qué terminó mal una misión. <see cref="Ninguno"/> si se cumplió.</summary>
    public enum MotivoFalloMision
    {
        Ninguno,
        /// <summary>Se acabó el límite de tiempo.</summary>
        Tiempo,
        /// <summary>El jugador perdió una pelea.</summary>
        KO,
        /// <summary>El jugador la dejó.</summary>
        Abandono
    }
}
