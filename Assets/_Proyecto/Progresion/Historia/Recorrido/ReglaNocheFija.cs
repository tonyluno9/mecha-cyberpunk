namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1685 (T36): lógica pura de la noche fija. Desde que el jugador despierta (paso 2) hasta terminar la misión de "la chica" (paso 11)
    /// el reloj del mundo se queda a las <see cref="HoraNoche"/> (la misma noche con la que hoy empieza la partida nueva) y en pausa:
    /// no avanza solo, y si algo lo adelanta (dormir, la clínica) o una partida se carga de día, vuelve a esa hora.
    /// Desde el paso 12 (<see cref="PasoRecorrido.GuerraSkulls"/>) el reloj corre normal y ya puede amanecer.
    /// Punto único para cambiar cuándo se apaga: <see cref="UltimoPasoDeNoche"/> (o <see cref="Activa"/> = false para apagarla del todo).
    /// </summary>
    public static class ReglaNocheFija
    {
        /// <summary>Hora a la que se queda el reloj (23:00, la de <c>PonerNoche</c> desde el paso 2).</summary>
        public const float HoraNoche = 23f;

        /// <summary>Primer paso con la noche fija: el jugador despierta en la ciudad (el recuerdo del paso 1 no se toca).</summary>
        public const PasoRecorrido PrimerPasoDeNoche = PasoRecorrido.Despierta;

        /// <summary>Último paso con la noche fija: la misión de "la chica" (paso 11). Después se puede pasar a día.</summary>
        public const PasoRecorrido UltimoPasoDeNoche = PasoRecorrido.Chica;

        private const float Tolerancia = 0.01f;

        /// <summary>Interruptor general (true por defecto). En false, el reloj nunca se fija (como antes de T-1685).</summary>
        public static bool Activa { get; set; } = true;

        /// <summary>True si en este paso de la historia el reloj se queda de noche y en pausa.</summary>
        public static bool Aplica(PasoRecorrido paso) => Aplica(paso, Activa);

        /// <summary>Igual que <see cref="Aplica(PasoRecorrido)"/> con el interruptor como parámetro (pruebas).</summary>
        public static bool Aplica(PasoRecorrido paso, bool activa)
        {
            // T-78: UsarProtesis (21) va entre Suero y Armas aunque su número sea mayor que Libre.
            return activa && ((paso >= PrimerPasoDeNoche && paso <= UltimoPasoDeNoche) || paso == PasoRecorrido.UsarProtesis);
        }

        /// <summary>
        /// True si el reloj marca otra hora y hay que regresarlo a <see cref="HoraNoche"/>. Entre las 23:00 y las 24:00 no hace falta
        /// (sigue siendo la misma noche; regresar a las 23 sumaría un día).
        /// </summary>
        public static bool HayQueVolver(float hora)
        {
            if (float.IsNaN(hora) || float.IsInfinity(hora))
            {
                return true;
            }
            return !(hora >= HoraNoche - Tolerancia && hora < 24f);
        }
    }
}
