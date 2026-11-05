using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.Aliados
{
    /// <summary>
    /// Reglas puras de un aliado (T-405): nivel por vencidos, vida y daño por nivel, y a quién ataca.
    /// </summary>
    public static class ReglaAliado
    {
        public const int NivelMaximo = 3;
        public const int VencidosNivel2 = 3;
        public const int VencidosNivel3 = 8;
        public const float AlcanceObjetivo = 20f;

        private static readonly float[] Vidas = { 80f, 110f, 140f };
        private static readonly float[] Danios = { 6f, 8f, 10f };

        /// <summary>Distancia a pie a la que, si te alejas, el aliado se te teletransporta cerca (m).</summary>
        public const float DistanciaAlcanzar = 40f;

        /// <summary>Qué hace el aliado según tu modo, la orden y tu distancia (T-630).</summary>
        public enum Accion
        {
            Esperar,
            Caminar,
            Correr,
            /// <summary>Va contigo dentro del carro o el mecha: se oculta y viaja a tu posición.</summary>
            IrAbordo,
            /// <summary>Quedó muy lejos: reaparece cerca de ti.</summary>
            Alcanzar
        }

        public static Accion Decidir(bool siguiendo, ModoJugador modo, float distancia)
        {
            if (!siguiendo)
            {
                return Accion.Esperar;
            }
            if (modo != ModoJugador.APie)
            {
                return Accion.IrAbordo;
            }
            if (distancia > DistanciaAlcanzar)
            {
                return Accion.Alcanzar;
            }
            if (distancia > DistanciaMax)
            {
                return distancia > DistanciaCorrer ? Accion.Correr : Accion.Caminar;
            }
            return Accion.Esperar;
        }

        public const float DistanciaMax = 5f;
        public const float DistanciaCorrer = 8f;

        public static int Nivel(int vencidos)
        {
            if (vencidos >= VencidosNivel3)
            {
                return 3;
            }
            return vencidos >= VencidosNivel2 ? 2 : 1;
        }

        public static float Vida(int nivel) => Vidas[Mathf.Clamp(nivel, 1, NivelMaximo) - 1];

        public static float Danio(int nivel) => Danios[Mathf.Clamp(nivel, 1, NivelMaximo) - 1];

        /// <summary>
        /// Índice del rival a atacar, o -1. Primero el que te dispara (si está en alcance); si no, el más cercano a ti a menos de
        /// <see cref="AlcanceObjetivo"/>. La distancia es plana (X, Z).
        /// </summary>
        public static int ElegirObjetivo(Vector3 jugador, IReadOnlyList<Vector3> rivales, int indiceQueTeDispara)
        {
            if (indiceQueTeDispara >= 0 && indiceQueTeDispara < rivales.Count && Distancia(jugador, rivales[indiceQueTeDispara]) <= AlcanceObjetivo)
            {
                return indiceQueTeDispara;
            }
            int mejor = -1;
            float menor = AlcanceObjetivo;
            for (int i = 0; i < rivales.Count; i++)
            {
                float d = Distancia(jugador, rivales[i]);
                if (d <= menor)
                {
                    menor = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
