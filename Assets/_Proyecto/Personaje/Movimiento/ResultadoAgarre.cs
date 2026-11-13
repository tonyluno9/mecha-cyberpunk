namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Resultado de evaluar si el jugador puede agarrarse de un borde.</summary>
    public readonly struct ResultadoAgarre
    {
        public readonly bool Valido;
        public readonly float Segundos;

        public ResultadoAgarre(bool valido, float segundos)
        {
            Valido = valido;
            Segundos = segundos;
        }
    }
}
