using System;
using System.Globalization;
using UnityEngine;

namespace MechaCyberpunk.Herramientas.ModoVideo
{
    /// <summary>T-472: lógica pura del modo video (velocidad de la cámara libre y nombre de las capturas).</summary>
    public static class ReglasModoVideo
    {
        public const float VelocidadInicial = 4f;
        public const float VelocidadMinima = 0.5f;
        public const float VelocidadMaxima = 60f;
        public const float FactorRapido = 4f;
        public const int SuperTamano = 2;

        /// <summary>Cada muesca de la rueda (120 unidades) multiplica o divide la velocidad por 1.25; queda entre mínima y máxima.</summary>
        public static float AjustarVelocidad(float actual, float rueda)
        {
            float muescas = rueda / 120f;
            float nueva = actual * Mathf.Pow(1.25f, muescas);
            return Mathf.Clamp(nueva, VelocidadMinima, VelocidadMaxima);
        }

        public static float VelocidadEfectiva(float baseVelocidad, bool rapido)
        {
            return rapido ? baseVelocidad * FactorRapido : baseVelocidad;
        }

        public static string NombreCaptura(DateTime ahora)
        {
            return "captura_" + ahora.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".png";
        }
    }
}
