namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>Lo que prende o apaga un nivel de calidad del realismo visual (T14).</summary>
    public readonly struct AjustesRealismo
    {
        public readonly bool OclusionAmbiental;
        public readonly bool Halos;
        public readonly bool Particulas;
        public readonly bool SombrasLucesAdicionales;

        public AjustesRealismo(bool oclusionAmbiental, bool halos, bool particulas, bool sombrasLucesAdicionales)
        {
            OclusionAmbiental = oclusionAmbiental;
            Halos = halos;
            Particulas = particulas;
            SombrasLucesAdicionales = sombrasLucesAdicionales;
        }
    }
}
