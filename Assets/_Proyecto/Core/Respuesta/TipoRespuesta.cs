namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1620 (T8): clases de unidades de respuesta (policía y NOVA). El orden de <see cref="ReglaEscaladaRespuesta.NivelDe"/> es la jerarquía fija:
    /// 1 policía a pie y patrullas, 2 camionetas PA02, 3 drones, 4 NOVA humanoides E15, 5 robots medianos E18, 6 perros E17, 7 robots grandes E16, 8 jefe E14.
    /// </summary>
    public enum TipoRespuesta
    {
        PoliciaAPie = 0,
        Patrulla = 1,
        Camioneta = 2,
        Dron = 3,
        NovaHumanoide = 4,
        RobotMediano = 5,
        Perro = 6,
        RobotGrande = 7,
        Jefe = 8
    }
}
