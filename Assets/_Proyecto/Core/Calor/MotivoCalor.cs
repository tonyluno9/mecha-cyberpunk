namespace MechaCyberpunk.Core
{
    /// <summary>Por qué sube el calor policial (para estadísticas y reacciones).</summary>
    public enum MotivoCalor
    {
        MechaEnCalle,
        Pelea,
        Choque,
        Reten,
        Otro,
        /// <summary>El calor sube por una misión (robo, ilegal).</summary>
        Mision
    }
}
