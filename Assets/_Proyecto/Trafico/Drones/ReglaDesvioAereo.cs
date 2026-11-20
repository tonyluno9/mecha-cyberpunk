using System;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1494: cuánto se desvía hacia arriba un dron para no atravesar un obstáculo. Si el paso siguiente está bloqueado, sube (y no avanza en su ruta);
    /// si está libre, va bajando poco a poco hasta volver a su altura. Lógica pura: el tiempo y el resultado de la prueba de colisión entran como parámetros.
    /// </summary>
    public sealed class ReglaDesvioAereo
    {
        private readonly float velocidadSubida;
        private readonly float velocidadBajada;
        private readonly float desvioMaximo;

        public ReglaDesvioAereo(float velocidadSubida, float velocidadBajada, float desvioMaximo)
        {
            if (velocidadSubida <= 0f || velocidadBajada <= 0f || desvioMaximo < 0f)
            {
                throw new ArgumentException("Las velocidades deben ser mayores que 0 y el desvío máximo no negativo.");
            }
            this.velocidadSubida = velocidadSubida;
            this.velocidadBajada = velocidadBajada;
            this.desvioMaximo = desvioMaximo;
        }

        /// <summary>Metros que está desviado hacia arriba respecto de su altura de ruta.</summary>
        public float Desvio { get; private set; }

        /// <summary>El desvío que tendría tras bajar un paso de <paramref name="dt"/> s (sin aplicarlo).</summary>
        public float DesvioBajando(float dt) => Math.Max(0f, Desvio - velocidadBajada * dt);

        /// <summary>El desvío que tendría tras subir un paso de <paramref name="dt"/> s (sin aplicarlo).</summary>
        public float DesvioSubiendo(float dt) => Math.Min(desvioMaximo, Desvio + velocidadSubida * dt);

        /// <summary>Metros que sube por segundo.</summary>
        public float VelocidadSubida => velocidadSubida;

        public void Fijar(float desvio) => Desvio = Math.Max(0f, Math.Min(desvioMaximo, desvio));
    }
}
