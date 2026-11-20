using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// T-1516: qué peatones se quedan platicando y cómo se acomodan (lógica pura). Uno de cada <c>unoDeCada</c> peatones platica, en corros de dos y,
    /// si sobra uno, de tres; los corros ocupan lugares seguidos desde <see cref="IndiceInicial"/> (bajo, para que ya estén en el día, cuando hay menos gente).
    /// </summary>
    public static class ReglaPlatica
    {
        /// <summary>Primer peatón que platica (los de antes caminan siempre).</summary>
        public const int IndiceInicial = 3;
        public const float RadioCorro = 0.55f;
        private const float GradosVuelta = 360f;

        /// <summary>Cuántos peatones platican de <paramref name="total"/> (0 si <paramref name="unoDeCada"/> es 0; al menos 2 si hay lugar).</summary>
        public static int CuantosPlatican(int total, int unoDeCada)
        {
            if (unoDeCada <= 0 || total < IndiceInicial + 2)
            {
                return 0;
            }
            int cuantos = Mathf.Max(2, total / unoDeCada);
            return Mathf.Min(cuantos, total - IndiceInicial);
        }

        /// <summary>Los corros como (primer índice, tamaño): parejas y, si sobra uno, un trío al final.</summary>
        public static List<(int Primero, int Tamano)> Corros(int total, int unoDeCada)
        {
            var corros = new List<(int Primero, int Tamano)>();
            int quedan = CuantosPlatican(total, unoDeCada);
            int indice = IndiceInicial;
            while (quedan >= 2)
            {
                int tamano = quedan == 3 ? 3 : 2;
                corros.Add((indice, tamano));
                indice += tamano;
                quedan -= tamano;
            }
            return corros;
        }

        /// <summary>Dónde se para el miembro <paramref name="miembro"/> de un corro de <paramref name="tamano"/> (m, plano XZ, relativo al centro del corro).</summary>
        public static Vector3 Lugar(int miembro, int tamano)
        {
            float angulo = (GradosVuelta / Mathf.Max(2, tamano) * miembro + 90f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * RadioCorro;
        }
    }
}
