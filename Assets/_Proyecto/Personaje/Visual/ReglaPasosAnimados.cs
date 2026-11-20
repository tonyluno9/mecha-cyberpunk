using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1505: reglas puras de los pasos por evento de animación. Los clips de locomoción de Blender (PJ01_Caminar, Trote, Sprint y los de lado y atrás)
    /// llevan un evento <see cref="FuncionPaso"/> en cada pisada (intParameter: pie 0 izquierdo, 1 derecho). Con dos clips mezclados (árbol de mezcla o
    /// transición) los dos disparan eventos: solo cuenta el del clip con más peso.
    /// </summary>
    public static class ReglaPasosAnimados
    {
        public const string FuncionPaso = "AlPasoPJ01";
        public const int PieIzquierdo = 0;
        public const int PieDerecho = 1;
        /// <summary>Peso mínimo (0 a 1) para que un clip cuente como activo.</summary>
        public const float PesoMinimo = 0.01f;

        private static readonly Dictionary<AnimationClip, bool> memoria = new Dictionary<AnimationClip, bool>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => memoria.Clear();

        /// <summary>True si alguno de los eventos es el de paso.</summary>
        public static bool TienePasos(AnimationEvent[] eventos)
        {
            if (eventos == null)
            {
                return false;
            }
            foreach (AnimationEvent e in eventos)
            {
                if (e != null && e.functionName == FuncionPaso)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Igual para un clip (con memoria: los eventos de un clip no cambian jugando).</summary>
        public static bool TienePasos(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }
            if (!memoria.TryGetValue(clip, out bool tiene))
            {
                tiene = TienePasos(clip.events);
                memoria[clip] = tiene;
            }
            return tiene;
        }

        /// <summary>Índice del clip con más peso (el primero si empatan); -1 si ninguno llega a <see cref="PesoMinimo"/>.</summary>
        public static int IndiceDominante(IList<float> pesos)
        {
            int mejor = -1;
            float peso = PesoMinimo;
            for (int i = 0; i < pesos.Count; i++)
            {
                if (pesos[i] > peso)
                {
                    peso = pesos[i];
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>True si el evento del clip en <paramref name="indiceEvento"/> es el del clip dominante: solo ese suena.</summary>
        public static bool DebeSonar(int indiceEvento, IList<float> pesos)
        {
            return indiceEvento >= 0 && indiceEvento == IndiceDominante(pesos);
        }

        /// <summary>
        /// Peso real de un clip en una transición: el de su estado por el avance de la mezcla. <paramref name="siguiente"/> true para los clips del estado que entra
        /// (peso por <paramref name="avance"/>), false para los del que sale (por 1 - avance).
        /// </summary>
        public static float PesoEnTransicion(float pesoEnSuEstado, bool siguiente, float avance)
        {
            float a = Mathf.Clamp01(avance);
            return pesoEnSuEstado * (siguiente ? a : 1f - a);
        }
    }
}
