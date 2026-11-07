namespace MechaCyberpunk.Economia
{
    /// <summary>Clase de objeto del inventario. Solo se agregan valores al final (se guardan como número en los assets).</summary>
    public enum TipoObjeto
    {
        Pieza,
        Chatarra,
        KitReparacion,
        Municion,
        Arma,
        /// <summary>Se usa con una tecla y da un efecto (T-685).</summary>
        Consumible
    }
}
