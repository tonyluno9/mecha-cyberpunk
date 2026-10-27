namespace MechaCyberpunk.Core
{
    /// <summary>Para qué sirve un punto de misión en el mundo. Solo se agregan valores al final.</summary>
    public enum TipoPuntoMision
    {
        /// <summary>Donde alguien ofrece una misión (un vecino, un teléfono).</summary>
        Encargo,
        /// <summary>Un lugar al que hay que llegar.</summary>
        Destino,
        /// <summary>Donde se recoge lo robado o encargado.</summary>
        Botin,
        /// <summary>Donde se para la pelea de la misión.</summary>
        Pelea,
        /// <summary>Un escondite (favor del barrio).</summary>
        Escondite
    }
}
