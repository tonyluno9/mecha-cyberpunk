namespace MechaCyberpunk.Combate.Peleador
{
    /// <summary>Regla pura del control efectivo del mecha: permiso de pelea Y permiso de piloto Y no estar en KO.</summary>
    public static class ReglaControlMecha
    {
        public static bool Calcular(bool permisoPelea, bool permisoPiloto, bool enKO)
        {
            return permisoPelea && permisoPiloto && !enKO;
        }
    }
}
