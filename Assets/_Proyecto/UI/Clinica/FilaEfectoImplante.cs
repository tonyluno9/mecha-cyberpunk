namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>Una fila de la tabla antes/después de la clínica (T-815): el efecto, sus dos valores y si mejora (1), empeora (-1) o queda igual (0).</summary>
    public readonly struct FilaEfectoImplante
    {
        public readonly EfectoImplante Efecto;
        public readonly float Antes;
        public readonly float Despues;
        public readonly int Cambio;

        public FilaEfectoImplante(EfectoImplante efecto, float antes, float despues, int cambio)
        {
            Efecto = efecto;
            Antes = antes;
            Despues = despues;
            Cambio = cambio;
        }
    }
}
