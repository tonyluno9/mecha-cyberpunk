namespace MechaCyberpunk.UI
{
    /// <summary>
    /// Cuándo se acaba una ayuda de controles (T-134): al pasar su tiempo máximo o cuando la persona ya hizo lo que dice.
    /// "Lo que dice" puede ser segundos de actividad, golpes, subidas de guardia o una orden externa (<see cref="Completar"/>).
    /// Un requisito en 0 no se pide. Lógica pura: el tiempo entra como parámetro.
    /// </summary>
    public class ProgresoAyuda
    {
        private readonly float segundosMaximos;
        private readonly float segundosActividad;
        private readonly int golpesNecesarios;
        private readonly int guardiasNecesarias;
        private float transcurrido;
        private float actividad;
        private int golpes;
        private int guardias;
        private bool completada;

        public ProgresoAyuda(float segundosMaximos, float segundosActividad = 0f, int golpesNecesarios = 0, int guardiasNecesarias = 0)
        {
            this.segundosMaximos = segundosMaximos;
            this.segundosActividad = segundosActividad;
            this.golpesNecesarios = golpesNecesarios;
            this.guardiasNecesarias = guardiasNecesarias;
        }

        public float Transcurrido => transcurrido;
        public float Actividad => actividad;
        public int Golpes => golpes;
        public int Guardias => guardias;

        /// <summary>True cuando se acabó el tiempo o la persona ya hizo lo que pide la ayuda.</summary>
        public bool Terminada => completada || transcurrido >= segundosMaximos || Cumplida;

        private bool Cumplida
        {
            get
            {
                bool hayRequisito = segundosActividad > 0f || golpesNecesarios > 0 || guardiasNecesarias > 0;
                return hayRequisito
                    && actividad >= segundosActividad
                    && golpes >= golpesNecesarios
                    && guardias >= guardiasNecesarias;
            }
        }

        /// <param name="dt">Segundos desde el frame anterior.</param>
        /// <param name="activo">Si la persona está haciendo lo que dice la ayuda en este frame.</param>
        public void Avanzar(float dt, bool activo)
        {
            transcurrido += dt;
            if (activo)
            {
                actividad += dt;
            }
        }

        public void RegistrarGolpe() => golpes++;

        public void RegistrarGuardia() => guardias++;

        /// <summary>Da la ayuda por cumplida (por ejemplo, la persona pulsó la tecla que decía).</summary>
        public void Completar() => completada = true;
    }
}
