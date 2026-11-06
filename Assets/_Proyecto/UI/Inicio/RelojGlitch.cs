using System;
using UnityEngine;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// Decide cuándo el logo tiene una ráfaga de glitch: espera un tiempo al azar entre <c>segundosMin</c> y <c>segundosMax</c>, dura
    /// unos instantes con una intensidad que tiembla entre 0.35 y 1 y vuelve a esperar. Lógica pura: el azar y el tiempo entran de fuera.
    /// </summary>
    public class RelojGlitch
    {
        private const float IntensidadMinima = 0.35f;
        private const float VariacionDuracion = 0.5f;

        private readonly Func<float> azar;
        private readonly float segundosMin;
        private readonly float segundosMax;
        private readonly float duracion;
        private float faltaParaRafaga;
        private float restaRafaga;

        public RelojGlitch(Func<float> azar, float segundosMin, float segundosMax, float duracion)
        {
            this.azar = azar ?? throw new ArgumentNullException(nameof(azar));
            this.segundosMin = Mathf.Max(0f, segundosMin);
            this.segundosMax = Mathf.Max(this.segundosMin, segundosMax);
            this.duracion = Mathf.Max(0.01f, duracion);
            faltaParaRafaga = Espera();
        }

        public bool EnRafaga => restaRafaga > 0f;

        /// <summary>0 fuera de la ráfaga; entre 0.35 y 1 dentro.</summary>
        public float Intensidad { get; private set; }

        public void Actualizar(float dt)
        {
            if (restaRafaga > 0f)
            {
                restaRafaga -= dt;
                if (restaRafaga <= 0f)
                {
                    restaRafaga = 0f;
                    Intensidad = 0f;
                    faltaParaRafaga = Espera();
                }
                else
                {
                    Intensidad = NuevaIntensidad();
                }
                return;
            }
            faltaParaRafaga -= dt;
            if (faltaParaRafaga <= 0f)
            {
                restaRafaga = duracion * (VariacionDuracion + azar());
                Intensidad = NuevaIntensidad();
            }
        }

        private float NuevaIntensidad() => IntensidadMinima + (1f - IntensidadMinima) * azar();

        private float Espera() => Mathf.Lerp(segundosMin, segundosMax, azar());
    }
}
