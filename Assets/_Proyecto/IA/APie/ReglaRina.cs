using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1107: lógica pura de la riña entre SNAKES y SKULLS. Elige a quién le pega cada uno, cuándo suelta el control (el jugador se mete)
    /// y cuándo se rearma. Sin escena: la usan <see cref="ConductaRina"/> y <see cref="RinaPandillas"/>.
    /// </summary>
    public static class ReglaRina
    {
        /// <summary>Radio (m) en el que busca rival.</summary>
        public const float RadioBusqueda = 15f;
        /// <summary>Si el jugador a pie entra a esta distancia (m), la riña se suelta.</summary>
        public const float RadioJugador = 6f;
        /// <summary>Con el jugador más lejos que esto (m) y sin que nadie lo vea, la riña se rearma.</summary>
        public const float DistanciaRearme = 80f;
        /// <summary>Qué parte del golpe normal pega entre ellos.</summary>
        public const float FraccionDanio = 0.35f;
        /// <summary>La riña nunca baja de esta fracción de vida (no matan: no hay búsqueda ni conquista por una riña).</summary>
        public const float FraccionMinimaVida = 0.25f;

        private static readonly string[] GritosSkulls = { "¡Lárgate, víbora!", "¡Aquí mandamos nosotros!", "¡Cállate, culebra!" };
        private static readonly string[] GritosSnakes = { "¡Fuera de aquí, calaca!", "¡Esta calle es nuestra!", "¡Pa su tumba, calaca!" };

        /// <summary>Un posible rival.</summary>
        public struct Candidato
        {
            public Vector3 Posicion;
            public IdPandilla Pandilla;
            public bool EnPie;
            public float VidaFraccion;
        }

        /// <summary>True si <paramref name="c"/> se le puede pegar a alguien de <paramref name="propia"/>: de otra pandilla, en pie y con vida por encima del mínimo.</summary>
        public static bool RivalValido(Candidato c, IdPandilla propia)
        {
            return c.EnPie && c.Pandilla != propia && c.VidaFraccion > FraccionMinimaVida;
        }

        /// <summary>Índice del rival válido más cercano (en el plano) dentro de <see cref="RadioBusqueda"/>, o -1.</summary>
        public static int ElegirRival(Vector3 posicion, IdPandilla propia, IReadOnlyList<Candidato> candidatos)
        {
            int mejor = -1;
            float menor = RadioBusqueda * RadioBusqueda;
            for (int i = 0; i < candidatos.Count; i++)
            {
                if (!RivalValido(candidatos[i], propia))
                {
                    continue;
                }
                float d = DistanciaPlana2(posicion, candidatos[i].Posicion);
                if (d <= menor)
                {
                    menor = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>True si la riña debe soltarse: el jugador le pegó a alguno o está a pie a <see cref="RadioJugador"/> o menos.</summary>
        public static bool DebeSoltar(float distanciaJugador, bool jugadorAPie, bool jugadorGolpeo)
        {
            return jugadorGolpeo || (jugadorAPie && distanciaJugador <= RadioJugador);
        }

        /// <summary>Daño que hacen entre ellos, dado el golpe normal.</summary>
        public static float Danio(float danioNormal) => danioNormal * FraccionDanio;

        /// <summary>True si el bando ya cayó: nadie está en pie con vida por encima del mínimo.</summary>
        public static bool BandoCaido(IReadOnlyList<Candidato> bando)
        {
            for (int i = 0; i < bando.Count; i++)
            {
                if (bando[i].EnPie && bando[i].VidaFraccion > FraccionMinimaVida)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>True si la riña se rearma: el jugador está lejos y ninguno de la riña cae en su campo de visión.</summary>
        public static bool DebeRearmar(float distanciaJugador, bool algunoVisible)
        {
            return distanciaJugador > DistanciaRearme && !algunoVisible;
        }

        /// <summary>Grito de riña de <paramref name="pandilla"/>; <paramref name="azar01"/> (0 a 1) elige la frase.</summary>
        public static string Grito(IdPandilla pandilla, float azar01)
        {
            string[] frases = pandilla == IdPandilla.Viboras ? GritosSnakes : GritosSkulls;
            return frases[Mathf.Clamp((int)(azar01 * frases.Length), 0, frases.Length - 1)];
        }

        private static float DistanciaPlana2(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
