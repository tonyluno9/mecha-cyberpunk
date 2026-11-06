namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>Cómo va la llamada de un civil que vio un delito (T-663).</summary>
    public enum EstadoLlamada
    {
        Libre,
        Llamando,
        Llamo,
        Cancelada
    }

    /// <summary>
    /// La regla del civil que llama a la policía (T-663), sin Unity: empieza a llamar, tarda <c>segundos</c> y entonces llamó;
    /// si lo cancelan antes (murió o huyó) no llama nunca.
    /// </summary>
    public sealed class ReglaLlamadaCivil
    {
        private readonly float segundosTotales;
        private float restante;

        public ReglaLlamadaCivil(float segundos)
        {
            segundosTotales = segundos < 0f ? 0f : segundos;
        }

        public EstadoLlamada Estado { get; private set; } = EstadoLlamada.Libre;

        /// <summary>Segundos que faltan para que llame (0 si no está llamando).</summary>
        public float Restante => Estado == EstadoLlamada.Llamando ? restante : 0f;

        /// <summary>Empieza a llamar. Solo desde Libre: uno que ya llamó o fue cancelado no vuelve a empezar.</summary>
        public bool Iniciar()
        {
            if (Estado != EstadoLlamada.Libre)
            {
                return false;
            }
            Estado = EstadoLlamada.Llamando;
            restante = segundosTotales;
            return true;
        }

        /// <summary>Avanza <paramref name="dt"/> s. Devuelve true solo en el cuadro en que termina la llamada.</summary>
        public bool Avanzar(float dt)
        {
            if (Estado != EstadoLlamada.Llamando || dt <= 0f)
            {
                return false;
            }
            restante -= dt;
            if (restante > 0f)
            {
                return false;
            }
            restante = 0f;
            Estado = EstadoLlamada.Llamo;
            return true;
        }

        /// <summary>Cancela la llamada en curso (murió o huyó).</summary>
        public void Cancelar()
        {
            if (Estado == EstadoLlamada.Llamando)
            {
                Estado = EstadoLlamada.Cancelada;
            }
        }
    }
}
