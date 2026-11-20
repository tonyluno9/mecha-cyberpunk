namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1521: lógica pura de la guarda que levanta a un civil vivo que quedó acostado en la calle (playtest del 20-nov). Un civil vivo no debería tener la cabeza a ras
    /// de piso (salvo agachado cubriéndose, que juega un clip en bucle y la cabeza queda más arriba) ni estar tumbado más que lo que tarda en sacarlo de un carro.
    /// El tiempo entra por parámetro (<c>dt</c>); no lee nada del mundo.
    /// </summary>
    public sealed class ReglaPosturaCivil
    {
        public enum Accion
        {
            Nada,
            /// <summary>Soltar el clip, quitar el KO y devolverle el control al controlador.</summary>
            Levantar
        }

        private readonly float alturaMinimaCabeza;
        private readonly float segundosBajo;
        private readonly float segundosTumbado;
        private float acumuladoBajo;
        private float acumuladoTumbado;

        /// <param name="alturaMinimaCabeza">Cabeza sobre la planta (m) por debajo de la cual se considera acostado.</param>
        /// <param name="segundosBajo">Cuánto tiempo seguido con la cabeza baja y sin estar tumbado a propósito antes de corregirlo (s).</param>
        /// <param name="segundosTumbado">Cuánto tiempo seguido tumbado sin que nadie lo haya matado antes de levantarlo (s).</param>
        public ReglaPosturaCivil(float alturaMinimaCabeza, float segundosBajo, float segundosTumbado)
        {
            this.alturaMinimaCabeza = alturaMinimaCabeza;
            this.segundosBajo = segundosBajo;
            this.segundosTumbado = segundosTumbado;
        }

        public float SegundosBajo => acumuladoBajo;
        public float SegundosTumbado => acumuladoTumbado;

        public void Reiniciar()
        {
            acumuladoBajo = 0f;
            acumuladoTumbado = 0f;
        }

        /// <summary>
        /// <paramref name="muerto"/>: ya se avisó su muerte (se queda tirado). <paramref name="caido"/>: el visual tiene el KO puesto. <paramref name="clipEnBucle"/>: juega
        /// platicar o agachado. <paramref name="raizParada"/>: la raíz del peatón está derecha (si no, la giró la caída de respaldo de PeatonMundo y es otro caso).
        /// </summary>
        public Accion Avanzar(float dt, float alturaCabeza, bool muerto, bool caido, bool clipEnBucle, bool raizParada)
        {
            if (muerto || clipEnBucle || !raizParada)
            {
                Reiniciar();
                return Accion.Nada;
            }
            if (caido)
            {
                acumuladoBajo = 0f;
                acumuladoTumbado += dt;
                if (acumuladoTumbado >= segundosTumbado)
                {
                    Reiniciar();
                    return Accion.Levantar;
                }
                return Accion.Nada;
            }
            acumuladoTumbado = 0f;
            if (alturaCabeza >= alturaMinimaCabeza)
            {
                acumuladoBajo = 0f;
                return Accion.Nada;
            }
            acumuladoBajo += dt;
            if (acumuladoBajo >= segundosBajo)
            {
                Reiniciar();
                return Accion.Levantar;
            }
            return Accion.Nada;
        }
    }
}
