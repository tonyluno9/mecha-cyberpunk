namespace MechaCyberpunk.Progresion
{
    /// <summary>Qué pasó al pedir instalar un implante (T-191).</summary>
    public enum ResultadoImplante
    {
        Instalado,
        SinDinero,
        LigaBaja,
        /// <summary>Ya tienes ese mismo implante.</summary>
        YaInstalado,
        /// <summary>Tienes uno mejor en ese espacio, o el implante no existe.</summary>
        NoMejora
    }
}
