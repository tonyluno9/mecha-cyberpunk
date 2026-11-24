namespace MechaCyberpunk.VR
{
    /// <summary>VR27: dónde está el cargador del arma en la mano (<see cref="ReglaRecargaManualVR"/>).</summary>
    public enum EstadoCargadorVR
    {
        /// <summary>Puesto en el arma (normal).</summary>
        EnArma,
        /// <summary>Se soltó con A: el arma no dispara hasta meter otro.</summary>
        Fuera,
        /// <summary>La mano izquierda sostiene uno nuevo.</summary>
        EnMano
    }
}
