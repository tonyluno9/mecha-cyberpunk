using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1631 (T17): reglas puras de la retirada de un policía a pie cuando "NOVA INTERVIENE": hacia dónde camina (lejos del jugador, en el plano), cómo rodea un
    /// obstáculo y cuándo puede desaparecer (solo lejos y sin que nadie lo vea: nunca frente al jugador).
    /// </summary>
    public static class CalculoRetiradaPolicia
    {
        /// <summary>Grados que gira el rumbo cada vez que el camino está tapado (se alterna izquierda y derecha, cada vez más).</summary>
        public const float GradosDesvio = 50f;

        /// <summary>Dirección plana normalizada de <paramref name="jugador"/> hacia <paramref name="policia"/>; si están encimados, hacia donde mira <paramref name="respaldo"/>.</summary>
        public static Vector3 Rumbo(Vector3 policia, Vector3 jugador, Vector3 respaldo)
        {
            Vector3 d = policia - jugador;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f)
            {
                d = respaldo;
                d.y = 0f;
            }
            return d.sqrMagnitude < 0.0001f ? Vector3.forward : d.normalized;
        }

        /// <summary>Rumbo desviado para el intento número <paramref name="intento"/> (0 = sin desvío; 1 = +50, 2 = -50, 3 = +100, 4 = -100...).</summary>
        public static Vector3 RumboDesviado(Vector3 rumbo, int intento)
        {
            if (intento <= 0)
            {
                return rumbo;
            }
            float grados = ((intento + 1) / 2) * GradosDesvio * (intento % 2 == 1 ? 1f : -1f);
            return Quaternion.AngleAxis(grados, Vector3.up) * rumbo;
        }

        /// <summary>True si el policía puede desaparecer: a <paramref name="metrosMinimos"/> o más del jugador (en el plano) y sin que la cámara lo vea.</summary>
        public static bool PuedeDesaparecer(float metrosAlJugador, bool loVe, float metrosMinimos) => !loVe && metrosAlJugador >= metrosMinimos;
    }
}
