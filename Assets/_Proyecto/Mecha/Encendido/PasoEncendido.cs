namespace MechaCyberpunk.Mecha.Encendido
{
    /// <summary>En qué va el encendido de la cabina.</summary>
    public enum PasoEncendido
    {
        /// <summary>Todavía no empezó (nadie ha subido).</summary>
        Apagado,
        /// <summary>Confirmar la entrada (tecla F).</summary>
        Paso1,
        /// <summary>Levantar el mecha (mantener Espacio).</summary>
        Paso2,
        /// <summary>Desbloquear el cristal (tecla Q).</summary>
        Paso3,
        /// <summary>Cabina encendida: el piloto tiene el control.</summary>
        Encendido
    }
}
