namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR27, lógica pura de la recarga a mano: A suelta el cargador (solo si se puede recargar: no lleno, sin recargar y con balas de reserva),
    /// la mano izquierda en la cadera izquierda + grip toma uno nuevo y acercarlo a menos de <see cref="DistanciaMeter"/> del arma lo mete
    /// (y entonces se recarga de verdad). Soltar el grip con el cargador en la mano lo tira. Para no dejar al jugador atorado, pulsar A otra vez con el
    /// cargador fuera recarga directo (<see cref="AccionRecargaVR.RecargarDirecto"/>), y un cambio de arma lo vuelve a poner (<see cref="Reiniciar"/>).
    /// </summary>
    public sealed class ReglaRecargaManualVR
    {
        /// <summary>A menos de esto (m) de la empuñadura del arma, el cargador en la mano entra.</summary>
        public const float DistanciaMeter = 0.12f;

        public EstadoCargadorVR Estado { get; private set; } = EstadoCargadorVR.EnArma;

        /// <summary>True si con estas balas se puede recargar (lo mismo que pide <c>CargadorPistola.EmpezarRecarga</c>). Reserva -1 = infinita.</summary>
        public static bool PuedeRecargar(int balas, int tamano, int reserva, bool recargando) => !recargando && balas < tamano && reserva != 0;

        /// <summary>
        /// Un cuadro. <paramref name="puedeRecargar"/>: ver <see cref="PuedeRecargar"/>; <paramref name="pulsoA"/>: A se pulsó;
        /// <paramref name="pulsoGripIzq"/>/<paramref name="sostieneGripIzq"/>: el grip izquierdo; <paramref name="enCadera"/>: la mano izquierda está
        /// en la cadera izquierda; <paramref name="distanciaArma"/>: de la mano izquierda a la empuñadura del arma (m).
        /// </summary>
        public AccionRecargaVR Avanzar(bool puedeRecargar, bool pulsoA, bool pulsoGripIzq, bool sostieneGripIzq, bool enCadera, float distanciaArma)
        {
            switch (Estado)
            {
                case EstadoCargadorVR.EnArma:
                    if (pulsoA && puedeRecargar)
                    {
                        Estado = EstadoCargadorVR.Fuera;
                        return AccionRecargaVR.SoltarCargador;
                    }
                    return AccionRecargaVR.Nada;
                case EstadoCargadorVR.Fuera:
                    if (pulsoA)
                    {
                        Estado = EstadoCargadorVR.EnArma;
                        return AccionRecargaVR.RecargarDirecto;
                    }
                    if (pulsoGripIzq && enCadera)
                    {
                        Estado = EstadoCargadorVR.EnMano;
                        return AccionRecargaVR.TomarCargador;
                    }
                    return AccionRecargaVR.Nada;
                default:
                    if (pulsoA)
                    {
                        Estado = EstadoCargadorVR.EnArma;
                        return AccionRecargaVR.RecargarDirecto;
                    }
                    if (distanciaArma <= DistanciaMeter)
                    {
                        Estado = EstadoCargadorVR.EnArma;
                        return AccionRecargaVR.MeterCargador;
                    }
                    if (!sostieneGripIzq)
                    {
                        Estado = EstadoCargadorVR.Fuera;
                        return AccionRecargaVR.TirarCargador;
                    }
                    return AccionRecargaVR.Nada;
            }
        }

        /// <summary>Vuelve a poner el cargador (cambio de arma, salir de VR).</summary>
        public void Reiniciar()
        {
            Estado = EstadoCargadorVR.EnArma;
        }
    }
}
