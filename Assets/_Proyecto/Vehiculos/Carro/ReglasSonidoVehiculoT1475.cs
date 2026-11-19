using System;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>T-1475: reglas puras de cuándo suenan el acelerón, el choque y el tope, y del tono del motor.</summary>
    public static class ReglasSonidoVehiculoT1475
    {
        public const string IdAceleron = "VEH_ACELERON";
        public const string IdChoque = "VEH_CHOQUE";
        public const string IdChoqueFuerte = "VEH_CHOQUE_FUERTE";
        public const string IdCristalRomper = "VEH_CRISTAL_ROMPER";
        public const string IdTope = "VEH_TOPE";
        public const string IdAlarma = "VEH_ALARMA";

        /// <summary>Tono del motor parado y a rapidez máxima (antes del factor del grupo).</summary>
        public const float TonoReposo = 0.8f;
        public const float TonoMaximo = 1.8f;

        /// <summary>Cuántas veces el umbral de choque fuerte hace falta para que además se rompa un cristal.</summary>
        public const float MultiplicadorCristal = 1.5f;

        public const float AceleronFraccionLento = 0.25f;
        public const float AceleronPedalAlto = 0.9f;
        public const float AceleronPedalBajo = 0.5f;
        public const float AceleronSegundosMinimos = 3f;

        public const float TopeSaltoVertical = 2.5f;
        public const float TopeRapidezMinima = 6f;
        public const float TopeSegundosMinimos = 1.5f;

        /// <summary>Tono objetivo del motor: creciente con la rapidez y acotado entre el de reposo y el máximo (por el factor del grupo).</summary>
        public static float TonoObjetivo(float rapidez, float rapidezMaxima, float factor)
        {
            float t = rapidezMaxima <= 0f ? 0f : Math.Min(1f, Math.Max(0f, rapidez / rapidezMaxima));
            return (TonoReposo + (TonoMaximo - TonoReposo) * t) * factor;
        }

        /// <summary>Acerca el tono actual al objetivo sin saltos (exponencial, independiente de dt).</summary>
        public static float Suavizar(float actual, float objetivo, float dt, float tasa)
        {
            float k = 1f - (float)Math.Exp(-Math.Max(0f, tasa) * Math.Max(0f, dt));
            return actual + (objetivo - actual) * k;
        }

        /// <summary>IDs del choque según su fuerza (m/s relativos): leve, fuerte, y con cristal si es muy fuerte. Vacío si no llega al umbral.</summary>
        public static string[] IdsChoque(float rapidez, float umbral, float umbralFuerte)
        {
            if (rapidez <= umbral)
            {
                return Array.Empty<string>();
            }
            if (rapidez >= umbralFuerte * MultiplicadorCristal)
            {
                return new[] { IdChoqueFuerte, IdCristalRomper };
            }
            return new[] { rapidez >= umbralFuerte ? IdChoqueFuerte : IdChoque };
        }

        /// <summary>El acelerón suena al pisar a fondo (el pedal estaba suelto) yendo lento, con un tiempo mínimo entre uno y otro.</summary>
        public static bool Aceleron(float pedalAnterior, float pedal, float rapidez, float rapidezMaxima, float segundosDesdeUltimo)
        {
            return pedal >= AceleronPedalAlto && pedalAnterior < AceleronPedalBajo
                && rapidez <= rapidezMaxima * AceleronFraccionLento && segundosDesdeUltimo >= AceleronSegundosMinimos;
        }

        /// <summary>El tope suena con un golpe vertical brusco (la velocidad vertical sube de golpe) yendo a cierta rapidez.</summary>
        public static bool Tope(float velocidadVerticalAnterior, float velocidadVertical, float rapidez, float segundosDesdeUltimo)
        {
            return velocidadVertical - velocidadVerticalAnterior >= TopeSaltoVertical && rapidez >= TopeRapidezMinima
                && segundosDesdeUltimo >= TopeSegundosMinimos;
        }
    }
}
