namespace MechaCyberpunk.UI.Armas
{
    /// <summary>Una fila de la tabla antes/después del banco de armas (T-814).</summary>
    public readonly struct FilaEstadistica
    {
        public readonly EstadisticaArma Estadistica;
        public readonly float Antes;
        public readonly float Despues;
        /// <summary>1 mejora, -1 empeora, 0 igual (en recarga y dispersión, menos es mejor).</summary>
        public readonly int Cambio;
        /// <summary>True si es la estadística de la pieza elegida.</summary>
        public readonly bool Resaltada;

        public FilaEstadistica(EstadisticaArma estadistica, float antes, float despues, int cambio, bool resaltada)
        {
            Estadistica = estadistica;
            Antes = antes;
            Despues = despues;
            Cambio = cambio;
            Resaltada = resaltada;
        }
    }
}
