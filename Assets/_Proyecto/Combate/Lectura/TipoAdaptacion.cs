namespace MechaCyberpunk.Combate.Lectura
{
    /// <summary>
    /// Cada respuesta que el rival puede aprender contra un hábito del jugador (T-199, D-tony-192). Solo se agregan valores al final:
    /// el número se guarda en la memoria del rival.
    /// </summary>
    public enum TipoAdaptacion
    {
        /// <summary>Responde a PUÑOS (muchos golpes): sube los brazos al ver venir el golpe.</summary>
        Guardia = 0,
        /// <summary>Responde a CUBRIRSE (guardia arriba casi siempre): alarga el aviso y amaga.</summary>
        Fintas = 1,
        /// <summary>Responde a CASTIGAR (pegarle justo después de su golpe): da un paso atrás.</summary>
        Retirada = 2
    }
}
