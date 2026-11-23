using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1665 (T46): reglas puras del escudo holográfico de los drones de patrulla de Nova. Los drones son inmunes: ningún tipo de daño les quita vida
    /// (el contrato de daño no distingue armas especiales como plasma o EMP: todas llegan como <see cref="TipoDanio.Disparo"/>), y cada impacto
    /// muestra un destello que se expande y se desvanece. Sin MonoBehaviour para poder probarla en EditMode.
    /// </summary>
    public static class ReglaEscudoDron
    {
        /// <summary>Segundos que dura el destello del impacto.</summary>
        public const float SegundosDestello = 0.3f;
        /// <summary>Segundos mínimos entre dos destellos de un mismo dron (una ráfaga o los perdigones de la escopeta no lo saturan).</summary>
        public const float SegundosEntreDestellos = 0.06f;
        /// <summary>Segundos mínimos entre dos sonidos del escudo de un mismo dron.</summary>
        public const float SegundosEntreSonidos = 0.12f;
        /// <summary>Tamaño (m) del hexágono al aparecer.</summary>
        public const float EscalaInicial = 0.35f;
        /// <summary>Tamaño (m) del hexágono al desvanecerse.</summary>
        public const float EscalaFinal = 1.4f;

        /// <summary>True si el escudo absorbe este daño sin pasarlo a la vida. Hoy: todos los tipos (el dron es inmune).</summary>
        public static bool Absorbe(TipoDanio tipo) => true;

        /// <summary>True si ya pasó el intervalo desde el último evento (ultimo negativo = nunca ha pasado).</summary>
        public static bool PuedeMostrar(float ahora, float ultimo, float intervalo) => ultimo < 0f || ahora - ultimo >= intervalo;

        /// <summary>Avance del destello de 0 (recién pegó) a 1 (terminó).</summary>
        public static float Progreso(float edad) => Mathf.Clamp01(edad / SegundosDestello);

        /// <summary>Tamaño del hexágono: crece rápido al inicio y frena (salida suave).</summary>
        public static float Escala(float progreso)
        {
            float p = Mathf.Clamp01(progreso);
            return Mathf.Lerp(EscalaInicial, EscalaFinal, 1f - (1f - p) * (1f - p));
        }

        /// <summary>Opacidad del destello: 1 al pegar, 0 al terminar.</summary>
        public static float Alfa(float progreso) => Mathf.Pow(1f - Mathf.Clamp01(progreso), 1.5f);
    }
}
