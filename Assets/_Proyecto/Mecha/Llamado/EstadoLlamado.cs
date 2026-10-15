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
        Regresando
    }
}
