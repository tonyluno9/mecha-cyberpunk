namespace MechaCyberpunk.VR
{
    /// <summary>VR27: lo que pide la recarga a mano en un cuadro (<see cref="ReglaRecargaManualVR"/>).</summary>
    public enum AccionRecargaVR
    {
        Nada,
        /// <summary>A soltó el cargador: cae del arma.</summary>
        SoltarCargador,
        /// <summary>La mano izquierda tomó uno nuevo de la cadera.</summary>
        TomarCargador,
        /// <summary>Se soltó el grip con el cargador en la mano: cae.</summary>
        TirarCargador,
        /// <summary>El cargador en la mano llegó al arma: entra y recarga.</summary>
        MeterCargador,
        /// <summary>A otra vez con el cargador fuera: recarga directo (para no quedar atorado).</summary>
        RecargarDirecto
    }
}
