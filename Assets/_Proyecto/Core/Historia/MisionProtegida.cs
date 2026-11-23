using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1620/T-1625 (T8 y T10): misión de historia protegida. Mientras está activa, la policía, los drones de calor y los refuerzos
    /// de NOVA por estrellas no entran; el calor se congela en 0 y lo que ya estaba en el mapa se retira sin desaparecer a la vista.
    /// La activa la historia (Progresion) y la lee el director de respuesta (Trafico/IA), las escenas del barrio y el bullying.
    /// <see cref="NovaPermitida"/>: en el paso 15 NOVA sí ataca aunque la policía normal no entre.
    /// </summary>
    public static class MisionProtegida
    {
        private static bool activaPorMision;
        private static bool novaPorMision;

        /// <summary>True con una misión protegida activa o con el candado de policía de la historia (<see cref="CandadosMundo.Policia"/>, T-78).</summary>
        public static bool Activa => activaPorMision || CandadosMundo.Policia;

        /// <summary>NOVA sí responde: solo si la misión lo permite y el candado de NOVA (<see cref="CandadosMundo.Nova"/>) no está puesto.</summary>
        public static bool NovaPermitida => novaPorMision && !CandadosMundo.Nova;

        public static string Motivo { get; private set; } = string.Empty;

        /// <summary>Se avisa en cada cambio de <see cref="Activa"/>.</summary>
        public static event Action<bool> Cambio;

        public static void Activar(string motivo, bool novaPermitida = false)
        {
            bool antes = activaPorMision;
            activaPorMision = true;
            novaPorMision = novaPermitida;
            Motivo = motivo ?? string.Empty;
            if (!antes) Cambio?.Invoke(true);
        }

        public static void Desactivar()
        {
            if (!activaPorMision) return;
            activaPorMision = false;
            novaPorMision = false;
            Motivo = string.Empty;
            Cambio?.Invoke(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            activaPorMision = false;
            novaPorMision = false;
            Motivo = string.Empty;
            Cambio = null;
        }
    }

    /// <summary>
    /// T-1625 (T10): una cinemática (pendiente o de video) está en pantalla. Mientras está activa no hay control del jugador,
    /// la IA y el tráfico cercano se pausan, no se disparan escenas del barrio ni bullying.
    /// </summary>
    public static class CinematicaEnCurso
    {
        public static bool Activa { get; private set; }
        public static event Action<bool> Cambio;

        public static void Fijar(bool activa)
        {
            if (Activa == activa) return;
            Activa = activa;
            Cambio?.Invoke(activa);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Activa = false;
            Cambio = null;
        }
    }
}
