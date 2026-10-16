namespace MechaCyberpunk.Pelea.Flujo
{
    /// <summary>Etapa en la que va una pelea.</summary>
    public enum EstadoFlujoPelea
    {
        Preparando,
        Conteo,
        Peleando,
        KO,
        Resultado,
        /// <summary>Presentador anunciando la pelea, antes del conteo. Va al final para no cambiar los números de los demás.</summary>
        Presentacion
    }
}
