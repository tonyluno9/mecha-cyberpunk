namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>Estados de <see cref="ReglaGuardiaRival"/> (T-201).</summary>
    public enum EstadoGuardiaRival
    {
        /// <summary>No se cubre.</summary>
        Abajo = 0,
        /// <summary>Vio venir el golpe y decidió cubrirse; falta que pase su tiempo de reacción.</summary>
        EsperandoReaccion = 1,
        /// <summary>Pide la guardia arriba.</summary>
        Sosteniendo = 2
    }
}
