using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1536 (contrato de Core, sonidos 20-nov): el clima del mundo. Lo escribe Mundo (LluviaSigueJugador) y lo leen el sonido (Progresion: lluvia,
    /// tormenta y viento) y los vehículos (lluvia oída dentro del carro), que no se ven entre sí. Hoy la lluvia está apagada en el juego (S2, 5-nov).
    /// </summary>
    public static class ClimaMundo
    {
        /// <summary>Qué tanta lluvia cae ahora sobre el jugador (0 a 1; 0 = no llueve).</summary>
        public static float Lluvia { get; set; }

        public static bool Llueve => Lluvia > 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Lluvia = 0f;
    }
}
