namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>T-rendimiento-tope-24nov: un paso de la medición: la fase del modo libre y el tope de enemigos simultáneos (0 = el del asset).</summary>
    public readonly struct PasoMedicionRendimiento
    {
        public readonly int Fase;
        public readonly int TopeEnemigos;

        public PasoMedicionRendimiento(int fase, int topeEnemigos)
        {
            Fase = fase;
            TopeEnemigos = topeEnemigos;
        }
    }
}
