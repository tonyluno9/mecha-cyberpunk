namespace MechaCyberpunk.Core
{
    /// <summary>
    /// VR26: cuánto se cierra la vista (viñeta) al moverse y al girar en VR. Se elige en OPCIONES y se guarda en <see cref="OpcionesVR"/>.
    /// VR38: al girar con el stick se cierra un poco más que al caminar (<see cref="OpcionesVR.AperturaVinetaGiro"/>).
    /// </summary>
    public enum NivelVinetaVR
    {
        /// <summary>Sin viñeta.</summary>
        No = 0,
        /// <summary>Muy leve, solo las orillas (por defecto).</summary>
        Baja = 1,
        /// <summary>Un poco más cerrada.</summary>
        Media = 2
    }
}
