namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>Qué pasa con un delito que acaba de ocurrir (T-661).</summary>
    public enum ResultadoTestigos
    {
        /// <summary>Alguien lo vio ya: el calor sube ahora.</summary>
        Pasa,
        /// <summary>Un civil lo vio y está llamando: el calor sube si llega a llamar.</summary>
        Pendiente,
        /// <summary>Nadie lo vio: no hay calor.</summary>
        Nada
    }
}
