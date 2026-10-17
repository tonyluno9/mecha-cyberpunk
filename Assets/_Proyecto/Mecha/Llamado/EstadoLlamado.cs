namespace MechaCyberpunk.Mecha.Llamado
{
    /// <summary>Dónde está el mecha respecto al llamado.</summary>
    public enum EstadoLlamado
    {
        /// <summary>Descansa en su taller (o en su pose inicial si no hay taller).</summary>
        EnTaller,
        /// <summary>Viene en camino: oculto y luego bajando del cielo. No se puede subir.</summary>
        Llegando,
        /// <summary>Está en la calle.</summary>
        EnCalle,
        /// <summary>Vuelve al taller, subiendo al cielo. No se puede subir.</summary>
        Regresando,
        /// <summary>Está en una arena peleando con el jugador dentro (T-128). Sin calor, sin regreso por lejanía y M no lo regresa.
        /// Va al final para no cambiar los números de los demás.</summary>
        EnArena
    }
}
