namespace MechaCyberpunk.Progresion
{
    /// <summary>Estado de la opción que ofrece la clínica en un espacio (T-191).</summary>
    public enum EstadoOfertaImplante
    {
        Instalar,
        SinDinero,
        LigaBaja,
        /// <summary>Ya tienes el nivel más alto de ese espacio.</summary>
        YaLoMejor,
        /// <summary>T-78: la primera visita al Doc solo deja instalar un espacio; los demás salen bloqueados (lo pone la pantalla, no el servicio).</summary>
        Bloqueado
    }
}
