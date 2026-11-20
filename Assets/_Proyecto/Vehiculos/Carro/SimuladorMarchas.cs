using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-1488: cambios de marcha simulados solo para el sonido del motor. Divide la rapidez máxima en <see cref="Marchas"/> tramos iguales:
    /// las rpm (0 a 1) suben dentro de cada marcha y caen al cambiar, con una pequeña caída extra un instante (el golpe de embrague). Con histéresis
    /// para no cambiar de ida y vuelta en el borde. Lógica pura con estado.
    /// </summary>
    public sealed class SimuladorMarchas
    {
        public const float RpmMinima = 0.25f;
        public const float FraccionSubir = 0.95f;
        public const float FraccionBajar = 0.6f;
        public const float SegundosCambio = 0.18f;
        public const float CaidaCambio = 0.18f;

        private float restanteCambio;

        public SimuladorMarchas(int marchas)
        {
            Marchas = Math.Max(1, marchas);
            Marcha = 1;
        }

        public int Marchas { get; }

        /// <summary>Marcha actual (1 a <see cref="Marchas"/>).</summary>
        public int Marcha { get; private set; }

        /// <summary>Rpm normalizadas (0 a 1) tras el último <see cref="Actualizar"/>.</summary>
        public float Rpm { get; private set; }

        /// <summary>True durante el golpe de embrague tras un cambio.</summary>
        public bool Cambiando => restanteCambio > 0f;

        /// <summary>Un paso: elige la marcha según la rapidez y devuelve las rpm (0 a 1).</summary>
        public float Actualizar(float rapidez, float rapidezMaxima, float dt)
        {
            float vmax = Math.Max(0.01f, rapidezMaxima);
            float v = Math.Clamp(Math.Abs(rapidez), 0f, vmax);
            float tramo = vmax / Marchas;
            int anterior = Marcha;
            while (Marcha < Marchas && v >= tramo * Marcha * FraccionSubir)
            {
                Marcha++;
            }
            while (Marcha > 1 && v < tramo * (Marcha - 1) * FraccionBajar)
            {
                Marcha--;
            }
            if (Marcha != anterior)
            {
                restanteCambio = SegundosCambio;
            }
            else if (restanteCambio > 0f)
            {
                restanteCambio = Math.Max(0f, restanteCambio - Math.Max(0f, dt));
            }
            float rpm = RpmMinima + (1f - RpmMinima) * Math.Clamp(v / (tramo * Marcha), 0f, 1f);
            if (restanteCambio > 0f)
            {
                rpm -= CaidaCambio * restanteCambio / SegundosCambio;
            }
            Rpm = Math.Clamp(rpm, 0f, 1f);
            return Rpm;
        }
    }
}
