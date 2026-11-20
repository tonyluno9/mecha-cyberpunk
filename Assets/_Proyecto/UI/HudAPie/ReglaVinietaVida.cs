using System;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>T-1489: cálculos puros de la viñeta roja con vida baja.</summary>
    public static class ReglaVinietaVida
    {
        /// <summary>Fracción de vida por debajo de la cual aparece la viñeta.</summary>
        public const float FraccionUmbral = 0.4f;
        /// <summary>Opacidad base justo bajo el umbral y con la vida casi en 0.</summary>
        public const float AlfaMinima = 0.2f;
        public const float AlfaMaxima = 0.65f;
        /// <summary>Pulsos por segundo justo bajo el umbral y con la vida casi en 0.</summary>
        public const float FrecuenciaMinima = 0.8f;
        public const float FrecuenciaMaxima = 2f;
        /// <summary>Cuánto baja la opacidad en el valle del pulso (0 = no pulsa, 1 = se apaga del todo).</summary>
        public const float ProfundidadPulso = 0.35f;

        private const float DosPi = 6.2831853f;

        /// <summary>0 con la vida en el umbral o más; 1 con la vida en 0. Crece linealmente.</summary>
        public static float Gravedad(float vida, float vidaMaxima)
        {
            if (vidaMaxima <= 0f || vida <= 0f)
            {
                return vida <= 0f && vidaMaxima > 0f ? 1f : 0f;
            }
            float fraccion = vida / vidaMaxima;
            if (fraccion >= FraccionUmbral)
            {
                return 0f;
            }
            return Math.Clamp(1f - fraccion / FraccionUmbral, 0f, 1f);
        }

        /// <summary>Opacidad objetivo (sin pulso): 0 sobre el umbral, de AlfaMinima a AlfaMaxima bajo él.</summary>
        public static float AlfaBase(float vida, float vidaMaxima)
        {
            float g = Gravedad(vida, vidaMaxima);
            return g <= 0f ? 0f : AlfaMinima + g * (AlfaMaxima - AlfaMinima);
        }

        public static float Frecuencia(float gravedad) => FrecuenciaMinima + Math.Clamp(gravedad, 0f, 1f) * (FrecuenciaMaxima - FrecuenciaMinima);

        /// <summary>Multiplicador del pulso (entre 1 - ProfundidadPulso y 1) en el segundo <paramref name="segundos"/>; suave (senoidal).</summary>
        public static float Pulso(float segundos, float gravedad)
        {
            float onda = 0.5f + 0.5f * (float)Math.Sin(segundos * Frecuencia(gravedad) * DosPi);
            return 1f - ProfundidadPulso * (1f - onda);
        }

        /// <summary>Opacidad final: la base de la vida por el pulso. 0 sin viñeta.</summary>
        public static float Alfa(float vida, float vidaMaxima, float segundos)
        {
            float b = AlfaBase(vida, vidaMaxima);
            return b <= 0f ? 0f : b * Pulso(segundos, Gravedad(vida, vidaMaxima));
        }
    }
}
