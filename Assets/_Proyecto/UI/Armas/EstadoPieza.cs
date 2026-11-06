namespace MechaCyberpunk.UI.Armas
{
    /// <summary>Cómo está una pieza del arma en el banco (T-814).</summary>
    public enum EstadoPieza
    {
        /// <summary>Ya se compró (su nivel es el del arma o menor).</summary>
        Instalada,
        /// <summary>Es la que sigue: se puede comprar.</summary>
        Disponible,
        /// <summary>Falta comprar antes otra pieza (o no se tiene el arma).</summary>
        Bloqueada
    }
}
