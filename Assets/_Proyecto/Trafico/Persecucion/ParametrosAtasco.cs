namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T-1006: los números del detector de atasco (vienen de <c>ConfigPersecucion</c>); las pruebas los arman a mano.</summary>
    public struct ParametrosAtasco
    {
        /// <summary>Cada cuántos segundos se mide cuánto avanzó queriendo avanzar (s).</summary>
        public float VentanaSegundos;
        /// <summary>Si en una ventana avanzó menos que esto (m) cuenta como atorada.</summary>
        public float DistanciaMinima;
        /// <summary>Reversas seguidas en el mismo lugar para recolocarla.</summary>
        public int ReversasParaRecolocar;
        /// <summary>Segundos en el mismo lugar (aunque dé reversa) para recolocarla (s).</summary>
        public float SegundosParaRecolocar;
        /// <summary>Si se aleja tanto del lugar donde se atoró (m) se da por liberada.</summary>
        public float RadioLiberada;
    }
}
