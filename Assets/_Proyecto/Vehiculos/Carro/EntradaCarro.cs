namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>Entrada del conductor en un instante: acelerar y frenar de 0 a 1, girar de -1 (izquierda) a 1 (derecha).</summary>
    public readonly struct EntradaCarro
    {
        public readonly float Acelerar;
        public readonly float Frenar;
        public readonly float Girar;
        public readonly float FrenoMano;

        public EntradaCarro(float acelerar, float frenar, float girar, float frenoMano)
        {
            Acelerar = acelerar;
            Frenar = frenar;
            Girar = girar;
            FrenoMano = frenoMano;
        }
    }
}
