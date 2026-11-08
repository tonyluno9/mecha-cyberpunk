namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>T-893: cuál calle toca en el fondo del menú de inicio: cambia cada <c>segundos</c>, en orden y dando la vuelta. Lógica pura.</summary>
    public sealed class CalculoFondoCalles
    {
        private readonly int total;
        private readonly float segundos;
        private float transcurrido;

        public int Actual { get; private set; }

        public CalculoFondoCalles(int total, float segundos)
        {
            this.total = total;
            this.segundos = segundos;
        }

        /// <summary>Avanza el tiempo; devuelve true en el cuadro en que toca cambiar de calle.</summary>
        public bool Avanzar(float dt)
        {
            if (total < 2)
            {
                return false;
            }
            transcurrido += dt;
            if (transcurrido < segundos)
            {
                return false;
            }
            transcurrido -= segundos;
            Actual = (Actual + 1) % total;
            return true;
        }
    }
}
