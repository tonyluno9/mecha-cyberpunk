using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1497: la altura real (m, en pose de reposo, medida con los renderers como CreadorVisualTripo) de los agentes de Nova y las medidas que de ella salen
    /// (cápsula del CharacterController, factor del cuerpo). Una sola tabla: la leen el creador de visuales, el de enemigos, la IA y las pruebas.
    /// </summary>
    public static class EscalasNova
    {
        /// <summary>Altura del guardia normal (E15 original, 1.85 m): con ella se pensaron la boca del arma (1.4 m), el pecho y la cápsula de 1.8 m.</summary>
        public const float AlturaReferencia = 1.85f;
        public const float AlturaCentinelaE15 = 2.6f;
        public const float AlturaRobotPesadoE16 = 3.5f;
        public const float AlturaEliteE18 = 2.2f;
        /// <summary>Alto total del perro robot E17 (m).</summary>
        public const float AlturaPerroE17 = 1.2f;

        /// <summary>Fracción de la altura que mide la cápsula (1.8 de 1.85 m).</summary>
        public const float FraccionCapsula = 0.97f;
        public const float RadioBase = 0.3f;
        public const float RadioMaximo = 0.5f;
        public const float PasoBase = 0.3f;

        /// <summary>Altura objetivo de una variante de Nova; 0 si no es una de las que se reescalan.</summary>
        public static float Altura(string variante)
        {
            if (string.IsNullOrEmpty(variante)) { return 0f; }
            if (variante.StartsWith("E15")) { return AlturaCentinelaE15; }
            if (variante.StartsWith("E16")) { return AlturaRobotPesadoE16; }
            if (variante.StartsWith("E17")) { return AlturaPerroE17; }
            if (variante.StartsWith("E18")) { return AlturaEliteE18; }
            return 0f;
        }

        /// <summary>Cuánto más grande que el guardia de referencia (1.85 m) es un agente de esa altura.</summary>
        public static float Factor(float altura) => altura / AlturaReferencia;

        public static float AlturaCapsula(float altura) => altura * FraccionCapsula;
        public static float RadioCapsula(float altura) => Mathf.Min(RadioBase * Factor(altura), RadioMaximo);
        public static float PasoCapsula(float altura) => PasoBase * Factor(altura);
    }
}
