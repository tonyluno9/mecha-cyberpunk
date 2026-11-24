namespace MechaCyberpunk.Core
{
    /// <summary>V10 (VR): cómo gira el stick derecho. Se elige en OPCIONES y se guarda en <see cref="OpcionesVR"/>.</summary>
    public enum ModoGiroVR
    {
        /// <summary>Pasos de 45° (soltar el stick para el siguiente).</summary>
        PorPasos,
        /// <summary>Giro continuo, proporcional al stick (hasta OpcionesVR.VelocidadGiro, 120°/s por defecto). Es el de por defecto (VR18).</summary>
        Suave
    }
}
