using System;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>
    /// T-1534: la lluvia oída desde dentro del carro (CLI_LLUVIA_DENTRO_CARRO, bucle). Suena si llueve y el jugador va dentro de un carro (no moto: va al aire);
    /// el clima (T-1535) fija <see cref="FuenteLluvia"/> y mientras esté vacía no suena nada. Pura: sin Unity.
    /// </summary>
    public static class ReglaLluviaDentroCarro
    {
        public const string Id = "CLI_LLUVIA_DENTRO_CARRO";

        /// <summary>
        /// Quién dice si llueve ahora (lo fija el clima, p. ej. <c>() =&gt; LluviaSigueJugador.Activa</c>). Vacía: no llueve. Estado estático: se limpia en cada arranque.
        /// </summary>
        public static Func<bool> FuenteLluvia;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => FuenteLluvia = null;

        public static bool Llueve => FuenteLluvia != null && FuenteLluvia();

        /// <summary>Suena con el jugador manejando un carro cerrado (no una moto) y lloviendo.</summary>
        public static bool Suena(bool jugadorDentro, bool esMoto, bool llueve) => jugadorDentro && !esMoto && llueve;
    }
}
