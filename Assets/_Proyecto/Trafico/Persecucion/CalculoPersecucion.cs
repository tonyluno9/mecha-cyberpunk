using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Las reglas de la persecución policial (T-340), sin Unity: cuántos drones y patrullas hay por nivel de calor y cómo se acercan.
    /// El nivel es el de <c>ICalor.Nivel</c> (0 sin calor, 1 a 3 después); uno fuera de rango se recorta.
    /// </summary>
    public sealed class CalculoPersecucion
    {
        private readonly int[] dronesPorNivel;
        private readonly int[] patrullasPorNivel;
        private readonly float velocidadDron;
        private readonly float velocidadPatrulla;
        private readonly float distanciaSeguimiento;
        private readonly float distanciaCaptura;

        /// <param name="drones">Drones en los niveles 1, 2, 3...: el índice 0 es el nivel 1.</param>
        /// <param name="patrullas">Patrullas en los mismos niveles.</param>
        /// <param name="metrosPorSegundoDron">Velocidad del dron (m/s).</param>
        /// <param name="metrosPorSegundoPatrulla">Velocidad de la patrulla (m/s).</param>
        /// <param name="metrosSeguimiento">A qué distancia del jugador se queda un perseguidor (m).</param>
        /// <param name="metrosCaptura">Distancia a la que cuenta como alcanzado (m); menor que la de seguimiento.</param>
        public CalculoPersecucion(IReadOnlyList<int> drones, IReadOnlyList<int> patrullas, float metrosPorSegundoDron, float metrosPorSegundoPatrulla,
            float metrosSeguimiento, float metrosCaptura)
        {
            if (drones == null || patrullas == null || drones.Count == 0 || drones.Count != patrullas.Count)
            {
                throw new ArgumentException("Una cantidad de drones y de patrullas por cada nivel, y al menos un nivel.");
            }
            if (metrosPorSegundoDron <= 0f || metrosPorSegundoPatrulla <= 0f || metrosCaptura < 0f || metrosSeguimiento < metrosCaptura)
            {
                throw new ArgumentException("Velocidades mayores que 0 y captura menor o igual que el seguimiento.");
            }
            dronesPorNivel = new int[drones.Count];
            patrullasPorNivel = new int[patrullas.Count];
            for (int i = 0; i < dronesPorNivel.Length; i++)
            {
                dronesPorNivel[i] = Math.Max(0, drones[i]);
                patrullasPorNivel[i] = Math.Max(0, patrullas[i]);
            }
            velocidadDron = metrosPorSegundoDron;
            velocidadPatrulla = metrosPorSegundoPatrulla;
            distanciaSeguimiento = metrosSeguimiento;
            distanciaCaptura = metrosCaptura;
        }

        /// <summary>El nivel más alto con cantidades definidas.</summary>
        public int NivelMaximo => dronesPorNivel.Length;

        public int Drones(int nivel) => nivel <= 0 ? 0 : dronesPorNivel[Math.Min(nivel, NivelMaximo) - 1];

        public int Patrullas(int nivel) => nivel <= 0 ? 0 : patrullasPorNivel[Math.Min(nivel, NivelMaximo) - 1];

        /// <summary>Metros que avanza hacia el jugador en <paramref name="dt"/> s sin pasar de la distancia de seguimiento; 0 si ya está ahí o más cerca.</summary>
        public float Paso(bool esDron, float distanciaAlJugador, float dt)
        {
            if (dt <= 0f || distanciaAlJugador <= distanciaSeguimiento)
            {
                return 0f;
            }
            float posible = (esDron ? velocidadDron : velocidadPatrulla) * dt;
            return Math.Min(posible, distanciaAlJugador - distanciaSeguimiento);
        }

        /// <summary>True si el perseguidor está lo bastante cerca como para alcanzar al jugador.</summary>
        public bool Alcanzado(float distanciaAlJugador) => distanciaAlJugador <= distanciaCaptura;
    }
}
