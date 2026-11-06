namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// El arresto (T-665), sin escena: te arrestan si una patrulla que te ve está a menos de <c>distancia</c> m y vas casi quieto (menos de
    /// <c>velocidadMaxima</c> m/s) durante <c>segundos</c> seguidos. Si fallas una condición el contador baja al doble de rápido.
    /// </summary>
    public sealed class ReglaArresto
    {
        private const float FactorDeBajada = 2f;
        private readonly float distancia;
        private readonly float segundos;
        private readonly float velocidadMaxima;

        public ReglaArresto(float metros, float segundosSeguidos, float metrosPorSegundoMaximos)
        {
            distancia = metros;
            segundos = segundosSeguidos;
            velocidadMaxima = metrosPorSegundoMaximos;
        }

        /// <summary>Segundos acumulados de arresto.</summary>
        public float Acumulado { get; private set; }

        public void Reiniciar() => Acumulado = 0f;

        /// <summary>Devuelve true en el cuadro en que te arrestan.</summary>
        public bool Avanzar(bool patrullaTeVe, float distanciaPatrulla, float velocidadJugador, float dt)
        {
            bool cumple = patrullaTeVe && distanciaPatrulla <= distancia && velocidadJugador <= velocidadMaxima;
            if (cumple)
            {
                Acumulado += dt;
                if (Acumulado >= segundos)
                {
                    Acumulado = 0f;
                    return true;
                }
            }
            else
            {
                Acumulado = Acumulado - dt * FactorDeBajada;
                if (Acumulado < 0f)
                {
                    Acumulado = 0f;
                }
            }
            return false;
        }
    }
}
