namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Los lugares reales de la ciudad (docs/diseno/ciudad.md). Es un enum cerrado (D-tony-4):
    /// agregar un lugar es una tarea de Core. <c>Ninguno</c> significa "no estoy en ningún lugar".
    /// </summary>
    public enum IdLugar
    {
        Ninguno,
        Calle,
        Casa,
        Taller,
        Bar,
        Clandestino,
        Casino,
        MercadoNegro,
        /// <summary>La clínica está dentro del mercado negro, pero cuenta como lugar propio.</summary>
        Clinica
    }
}
