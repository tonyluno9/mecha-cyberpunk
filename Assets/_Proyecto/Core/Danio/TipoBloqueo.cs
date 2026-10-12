namespace MechaCyberpunk.Core
{
    /// <summary>Cómo se defendió el que recibió el daño.</summary>
    public enum TipoBloqueo
    {
        /// <summary>Sin guardia: entra todo el daño.</summary>
        Ninguno,
        /// <summary>Guardia arriba: el daño se reduce.</summary>
        Normal,
        /// <summary>Guardia recién subida: no entra daño.</summary>
        Perfecto
    }
}
