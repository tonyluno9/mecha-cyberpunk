using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1535: el clima tal como lo oye el jugador. Hoy el juego no tiene clima activo (LluviaSigueJugador.Activa arranca en false y vive en Mundo, que Progresion
    /// no puede leer), así que quien tenga el clima (la lluvia, una tormenta, el viento) lo fija aquí con <see cref="Fijar"/> y <see cref="SonidosClima"/>
    /// hace sonar el bucle que toca. Con todo en cero no suena nada.
    /// </summary>
    public static class ClimaSonoro
    {
        /// <summary>Qué tanta lluvia cae (0 a 1); 0 = no llueve.</summary>
        public static float Lluvia => Mathf.Max(lluviaFijada, MechaCyberpunk.Core.ClimaMundo.Lluvia); // T-1536: también la lluvia del mundo (Core)

        private static float lluviaFijada;

        /// <summary>True si la lluvia es tormenta con truenos (suena CLI_TORMENTA_TRUENOS en vez de la lluvia fuerte).</summary>
        public static bool Tormenta { get; private set; }

        /// <summary>Qué tanto viento hay (0 a 1).</summary>
        public static float Viento { get; private set; }

        public static bool Llueve => Lluvia > 0f;

        public static void Fijar(float lluvia, bool tormenta, float viento)
        {
            lluviaFijada = Mathf.Clamp01(lluvia);
            Tormenta = tormenta && Lluvia > 0f;
            Viento = Mathf.Clamp01(viento);
        }

        public static void Limpiar() => Fijar(0f, false, 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Limpiar();
    }
}
