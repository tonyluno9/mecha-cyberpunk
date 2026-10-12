namespace MechaCyberpunk.Core
{
    /// <summary>Cómo terminó una pelea.</summary>
    public readonly struct ResultadoPelea
    {
        public LadoPelea Ganador { get; }
        /// <summary>Segundos desde que empezó la pelea hasta el KO.</summary>
        public float DuracionSegundos { get; }

        public ResultadoPelea(LadoPelea ganador, float duracionSegundos)
        {
            Ganador = ganador;
            DuracionSegundos = duracionSegundos;
        }
    }
}
