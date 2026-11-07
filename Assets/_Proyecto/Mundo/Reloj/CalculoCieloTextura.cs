using UnityEngine;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// Cuentas del cielo con textura y de la luz de día (T-635), sin escena: opacidad de estrellas, color de las nubes (blancas de día,
    /// teñidas por el brillo de la ciudad de noche), ambiente por gradiente (cielo, horizonte y suelo) y luz solar más cálida.
    /// <c>pesoNoche</c> va de 0 (día) a 1 (noche).
    /// </summary>
    public static class CalculoCieloTextura
    {
        private static readonly Color NubesDia = new Color(1f, 1f, 1f, 0.6f);
        private static readonly Color NubesNoche = new Color(0.42f, 0.4f, 0.56f, 0.04f); // T-880: cielo de noche azul marino liso como K01 (casi sin nubes)
        private static readonly Color CalidoSol = new Color(1f, 0.88f, 0.68f, 1f);
        private const float OpacidadMaximaEstrellas = 0.22f; // T-880: pocas estrellas, el cielo de K01 es liso
        private const float CalidezSol = 0.45f;
        private const float CieloSobreAmbiente = 0.55f;
        private const float BrilloCielo = 1.15f;
        private const float OscuridadSuelo = 0.55f;

        /// <summary>Opacidad de las estrellas: 0 de día y casi 1 de noche (crece rápido al anochecer).</summary>
        public static float OpacidadEstrellas(float pesoNoche)
        {
            float p = Mathf.Clamp01(pesoNoche);
            return OpacidadMaximaEstrellas * p * p;
        }

        /// <summary>Color y opacidad de las nubes: blancas de día y con el brillo rosado de la ciudad de noche.</summary>
        public static Color ColorNubes(float pesoNoche) => Color.Lerp(NubesDia, NubesNoche, Mathf.Clamp01(pesoNoche));

        /// <summary>
        /// Ambiente por gradiente: el cielo toma parte del color del cielo (más de día), el horizonte es el ambiente de siempre y el
        /// suelo es más oscuro.
        /// </summary>
        public static void Ambiente(Color ambiente, Color cielo, float pesoNoche, out Color arriba, out Color horizonte, out Color suelo)
        {
            float dia = 1f - Mathf.Clamp01(pesoNoche);
            arriba = Color.Lerp(ambiente, cielo, CieloSobreAmbiente * dia) * BrilloCielo;
            arriba.a = 1f;
            horizonte = ambiente;
            suelo = ambiente * OscuridadSuelo;
            suelo.a = 1f;
        }

        /// <summary>El sol de día se tiñe de cálido; de noche (la luna) queda como estaba.</summary>
        public static Color LuzCalida(Color luz, float pesoNoche)
        {
            return Color.Lerp(luz, CalidoSol, CalidezSol * (1f - Mathf.Clamp01(pesoNoche)));
        }
    }
}
