using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1491: el daño de una bala según la distancia (lógica pura). Hasta <c>inicioCaida</c> (fracción del alcance) hace el daño completo; de ahí
    /// al alcance baja en línea recta hasta <c>danioMinimo</c> (fracción del daño). Más allá del alcance no hace daño. Con inicioCaida = 1 no hay caída
    /// (francotirador, pistola, revólver).
    /// </summary>
    public static class ReglaDanioDistancia
    {
        /// <summary>Multiplicador del daño (0 a 1) a <paramref name="distancia"/> m para un arma de <paramref name="alcance"/> m.</summary>
        public static float Multiplicador(float distancia, float alcance, float inicioCaida, float danioMinimo)
        {
            if (alcance <= 0f || distancia > alcance + 0.01f)
            {
                return 0f;
            }
            float inicio = Mathf.Clamp01(inicioCaida) * alcance;
            if (distancia <= inicio || alcance - inicio < 0.0001f)
            {
                return 1f;
            }
            return Mathf.Lerp(1f, Mathf.Clamp01(danioMinimo), Mathf.Clamp01((distancia - inicio) / (alcance - inicio)));
        }
    }

    /// <summary>T-1491: reglas de los trazos de bala (lógica pura): color por arma, cuántos disparos de una ráfaga llevan trazo y su recorrido.</summary>
    public static class ReglaTrazoBala
    {
        /// <summary>Segundos que tarda el trazo en viajar del cañón al impacto.</summary>
        public const float SegundosViaje = 0.05f;
        /// <summary>Segundos que tarda en desvanecerse al llegar.</summary>
        public const float SegundosDesvanecer = 0.04f;
        /// <summary>Largo máximo del trazo (m).</summary>
        public const float LargoMaximo = 7f;
        /// <summary>Ancho del trazo (m).</summary>
        public const float Ancho = 0.018f;

        /// <summary>De cada cuántos disparos uno lleva trazo: 1 en las de un tiro, 3 en el subfusil, 2 en el fusil.</summary>
        public static int CadaCuantos(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Subfusil: return 3;
                case ArmaAPie.Fusil: return 2;
                default: return 1;
            }
        }

        /// <summary>El color (HDR, brilla con el bloom) del trazo de cada arma.</summary>
        public static Color ColorDe(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Escopeta: return new Color(3.0f, 1.4f, 0.4f, 1f);
                case ArmaAPie.Fusil: return new Color(2.6f, 2.4f, 0.7f, 1f);
                case ArmaAPie.Sniper: return new Color(1.2f, 2.8f, 3.4f, 1f);
                case ArmaAPie.Subfusil: return new Color(3.0f, 0.8f, 1.6f, 1f);
                case ArmaAPie.Revolver: return new Color(3.0f, 2.0f, 0.6f, 1f);
                default: return new Color(3.0f, 2.6f, 1.2f, 1f);
            }
        }

        /// <summary>
        /// Los extremos del trazo (distancias desde el cañón) en el instante <paramref name="t"/> para un recorrido de <paramref name="total"/> m: la cabeza
        /// avanza a velocidad constante y llega en <see cref="SegundosViaje"/>; la cola la sigue a <see cref="LargoMaximo"/> m como máximo.
        /// </summary>
        public static void Extremos(float total, float t, out float cola, out float cabeza)
        {
            float p = Mathf.Clamp01(t / SegundosViaje);
            cabeza = total * p;
            cola = Mathf.Max(0f, cabeza - Mathf.Min(LargoMaximo, total * 0.6f));
            if (t > SegundosViaje)
            {
                cola = Mathf.Lerp(cola, cabeza, Mathf.Clamp01((t - SegundosViaje) / SegundosDesvanecer));
            }
        }

        /// <summary>Opacidad (0 a 1) del trazo en el instante <paramref name="t"/>.</summary>
        public static float Opacidad(float t)
        {
            return t <= SegundosViaje ? 1f : 1f - Mathf.Clamp01((t - SegundosViaje) / SegundosDesvanecer);
        }
    }
}
