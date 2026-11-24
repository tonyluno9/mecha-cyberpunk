namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR5: lo que pide el stick derecho en vertical (<see cref="ReglaStickArmaVR"/>). Desde VR19 solo sale Nada o Recentrar: ArmaSiguiente y
    /// ArmaAnterior quedan por compatibilidad y ya no se piden.
    /// </summary>
    public enum GestoStickVR
    {
        Nada,
        ArmaSiguiente,
        ArmaAnterior,
        Recentrar
    }
}
