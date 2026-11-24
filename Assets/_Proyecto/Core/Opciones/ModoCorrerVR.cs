namespace MechaCyberpunk.Core
{
    /// <summary>V9 (VR): cómo corre el clic del stick izquierdo. Se elige en OPCIONES y se guarda en <see cref="OpcionesVR"/>.</summary>
    public enum ModoCorrerVR
    {
        /// <summary>Corre mientras se mantiene el clic.</summary>
        Mantener,
        /// <summary>Un clic enciende correr y otro lo apaga (también se apaga al soltar el stick de mover).</summary>
        Alternar
    }
}
