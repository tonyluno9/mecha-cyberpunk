namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1487: temporizador puro del hitstop. Se avanza con tiempo real (sin escala), así no depende de Time.timeScale ni de una pausa.</summary>
    public sealed class TemporizadorHitstop
    {
        public float Restante { get; private set; }

        public bool Activo => Restante > 0f;

        /// <summary>Empieza o alarga el congelado: si ya hay uno, queda el mayor de los dos tiempos.</summary>
        public void Iniciar(float segundos)
        {
            if (segundos > Restante)
            {
                Restante = segundos;
            }
        }

        /// <summary>Avanza el tiempo real. Devuelve true en el paso en que el congelado termina.</summary>
        public bool Avanzar(float dtReal)
        {
            if (Restante <= 0f)
            {
                return false;
            }
            Restante -= dtReal;
            if (Restante <= 0f)
            {
                Restante = 0f;
                return true;
            }
            return false;
        }

        public void Cancelar() => Restante = 0f;
    }
}
