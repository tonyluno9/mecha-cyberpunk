namespace MechaCyberpunk.Mecha.Llamado
{
    /// <summary>Qué pasó al pulsar M para llamar al mecha.</summary>
    public enum ResultadoLlamado
    {
        /// <summary>No pasó nada (el mecha ya está regresando al taller).</summary>
        Nada,
        /// <summary>El mecha sale del taller y viene al punto indicado.</summary>
        Llega,
        /// <summary>El jugador está fuera de toda zona permitida: el mecha no llega.</summary>
        ZonaProhibida,
        /// <summary>El mecha ya viene en camino.</summary>
        YaViene,
        /// <summary>El mecha está vacío en la calle: M lo manda de vuelta al taller.</summary>
        Regresa,
        /// <summary>Alguien va dentro del mecha.</summary>
        Ocupado
    }
}
