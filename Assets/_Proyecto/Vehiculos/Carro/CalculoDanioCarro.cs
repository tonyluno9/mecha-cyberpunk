using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// La vida de un carro (T-444), sin Unity: baja con el daño, pasa de Sano a Humo, a Fuego y a Destruido según la fracción de vida que
    /// queda, y calcula el daño de un choque según la rapidez relativa. No se repara solo.
    /// </summary>
    public sealed class CalculoDanioCarro
    {
        private readonly float fraccionHumo;
        private readonly float fraccionFuego;

        /// <param name="vidaMaxima">Vida del carro (puntos); mayor que 0.</param>
        /// <param name="fraccionHumo">Con esta fracción de vida o menos sale humo (de 0 a 1).</param>
        /// <param name="fraccionFuego">Con esta fracción de vida o menos hay fuego (de 0 a <paramref name="fraccionHumo"/>).</param>
        public CalculoDanioCarro(float vidaMaxima, float fraccionHumo, float fraccionFuego)
        {
            if (vidaMaxima <= 0f)
            {
                throw new ArgumentException("La vida máxima debe ser mayor que 0.", nameof(vidaMaxima));
            }
            Maxima = vidaMaxima;
            Actual = vidaMaxima;
            this.fraccionHumo = Math.Min(1f, Math.Max(0f, fraccionHumo));
            this.fraccionFuego = Math.Min(this.fraccionHumo, Math.Max(0f, fraccionFuego));
        }

        public float Maxima { get; }

        public float Actual { get; private set; }

        public bool Destruido => Actual <= 0f;

        public EstadoCarro Estado
        {
            get
            {
                if (Destruido)
                {
                    return EstadoCarro.Destruido;
                }
                float fraccion = Actual / Maxima;
                if (fraccion <= fraccionFuego)
                {
                    return EstadoCarro.Fuego;
                }
                return fraccion <= fraccionHumo ? EstadoCarro.Humo : EstadoCarro.Sano;
            }
        }

        /// <summary>Resta vida sin bajar de 0 y devuelve lo que de verdad restó. Un carro destruido ya no recibe nada.</summary>
        public float Aplicar(float cantidad)
        {
            if (cantidad <= 0f || Destruido)
            {
                return 0f;
            }
            float aplicado = Math.Min(Actual, cantidad);
            Actual -= aplicado;
            return aplicado;
        }

        /// <summary>Daño de un choque: nada por debajo del umbral y, por encima, <paramref name="danioPorMetroPorSegundo"/> por cada m/s de más.</summary>
        public static float DanioPorChoque(float rapidezRelativa, float umbral, float danioPorMetroPorSegundo)
        {
            return rapidezRelativa > umbral ? (rapidezRelativa - umbral) * Math.Max(0f, danioPorMetroPorSegundo) : 0f;
        }
    }
}
