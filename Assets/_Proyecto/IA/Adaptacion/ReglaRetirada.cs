using System;
using System.Collections.Generic;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>Lo que pide una retirada: cuándo empieza, a qué distancia queda, cuánto dura como máximo y a qué velocidad (T-202).</summary>
    public readonly struct PedidoRetirada
    {
        public PedidoRetirada(float demora, float distancia, float segundosMaximos, float factorVelocidad)
        {
            Demora = demora;
            Distancia = distancia;
            SegundosMaximos = segundosMaximos;
            FactorVelocidad = factorVelocidad;
        }

        /// <summary>Segundos desde el impacto del rival hasta empezar a caminar hacia atrás (s).</summary>
        public float Demora { get; }
        /// <summary>Distancia al objetivo con la que debe quedar (m).</summary>
        public float Distancia { get; }
        /// <summary>Lo más que dura caminando hacia atrás (s).</summary>
        public float SegundosMaximos { get; }
        /// <summary>Multiplica su velocidad máxima (0 a 1).</summary>
        public float FactorVelocidad { get; }
    }

    /// <summary>
    /// Regla pura de la RETIRADA del rival (T-202, D-tony-196): tras cada impacto suyo, pegue o no, si el azar sale menor que la probabilidad
    /// de su nivel pide dar un paso atrás hasta el alcance del golpe del jugador más un margen, en a lo más su recarga. Nivel 0: nunca.
    /// </summary>
    public class ReglaRetirada
    {
        private readonly IReadOnlyList<float> probabilidad;
        private readonly IReadOnlyList<float> factorVelocidad;
        private readonly float demora;
        private readonly float margen;
        private readonly Func<float> azar;

        public ReglaRetirada(IReadOnlyList<float> probabilidad, IReadOnlyList<float> factorVelocidad, float demora, float margen,
            Func<float> azar)
        {
            this.probabilidad = probabilidad;
            this.factorVelocidad = factorVelocidad;
            this.demora = demora;
            this.margen = margen;
            this.azar = azar;
        }

        /// <summary>Se llama tras cada impacto del rival. Devuelve el pedido o false si no se retira.</summary>
        /// <param name="nivel">Nivel de RETIRADA (0 = no la tiene).</param>
        /// <param name="alcanceJugador">Alcance del golpe del jugador (m).</param>
        /// <param name="recargaRival">Recarga del golpe del rival (s): la retirada acaba con ella como tarde.</param>
        public bool Decidir(int nivel, float alcanceJugador, float recargaRival, out PedidoRetirada pedido)
        {
            pedido = default;
            if (nivel <= 0 || probabilidad == null || probabilidad.Count == 0)
            {
                return false;
            }
            int indice = Math.Min(nivel, probabilidad.Count) - 1;
            if (azar() >= probabilidad[indice])
            {
                return false;
            }
            float factor = factorVelocidad != null && factorVelocidad.Count > 0
                ? Math.Min(1f, Math.Max(0f, factorVelocidad[Math.Min(indice, factorVelocidad.Count - 1)]))
                : 1f;
            // Camina hasta que acaba la recarga: no gana tiempo para golpear antes de que la recarga termine, ni sale de ella.
            float segundos = Math.Max(0f, recargaRival);
            pedido = new PedidoRetirada(demora, alcanceJugador + margen, segundos, factor);
            return true;
        }
    }
}
