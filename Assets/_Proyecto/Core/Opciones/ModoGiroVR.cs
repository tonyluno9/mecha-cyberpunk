namespace MechaCyberpunk.Core
{
    /// <summary>V10 (VR): cómo gira el stick derecho. Se elige en OPCIONES y se guarda en <see cref="OpcionesVR"/>.</summary>
    public enum ModoGiroVR
    {
        /// <summary>Pasos de 45° (soltar el stick para el siguiente).</summary>
        PorPasos,
        /// <summary>Giro continuo, proporcional al stick (hasta ~90°/s).</summary>
        Suave
    }
}
