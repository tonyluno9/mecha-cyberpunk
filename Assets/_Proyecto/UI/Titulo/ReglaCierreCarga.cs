namespace MechaCyberpunk.UI.Titulo
{
    /// <summary>
    /// T96: cuándo se quita la <see cref="PantallaCargando"/> (regla pura, probada en EditMode). Antes solo se quitaba cuando se registraba
    /// un jugador en la escena nueva; el menú principal (Inicio.unity, o el título de la ciudad en el editor) no tiene jugador, así que
    /// al volver al menú (desde HAS MUERTO o desde la pausa) la carga se quedaba encima hasta el tope de <see cref="PantallaCargando.SegundosMaximos"/>.
    /// Ahora el destino está listo si llegó el jugador o si la escena que se cargó es el menú.
    /// </summary>
    public static class ReglaCierreCarga
    {
        private const float BarraLlena = 0.999f;

        /// <summary>
        /// True si ya llegó lo que la carga esperaba: el jugador de la escena nueva, la pantalla de inicio, o la ciudad abierta en su
        /// título cuando ese título es el menú (en el editor; en un build el título manda a Inicio y la carga sigue hasta allá).
        /// </summary>
        public static bool DestinoListo(bool jugadorNuevo, bool cargoInicio, bool cargoTitulo, bool tituloVuelveAlInicio)
        {
            return jugadorNuevo || cargoInicio || (cargoTitulo && !tituloVuelveAlInicio);
        }

        /// <summary>Con una carga asíncrona real: la escena ya se activó, la operación terminó y el destino está listo.</summary>
        public static bool PuedeTerminarAsincrona(bool activada, bool operacionHecha, bool destinoListo)
        {
            return activada && operacionHecha && destinoListo;
        }

        /// <summary>Sin operación (recarga de la ciudad): el destino está listo, pasó el tiempo mínimo y el avance interno llegó al final.</summary>
        public static bool PuedeTerminarSimulada(bool destinoListo, float segundos, float segundosMinimos, float progreso)
        {
            return destinoListo && segundos >= segundosMinimos && progreso >= BarraLlena;
        }
    }
}
