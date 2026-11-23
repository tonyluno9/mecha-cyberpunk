using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T43: cuándo se ve la barra de vida de un enemigo y qué tan grande. Regla pura, sin escena.
    /// La barra pequeña sale sobre la cabeza solo si el enemigo recibió daño hace poco (en combate), sigue vivo, está
    /// cerca y no hay una pared de por medio. La barra grande del jefe sale arriba de la pantalla, solo la del jefe que
    /// recibió daño más recientemente.
    /// </summary>
    public static class ReglaBarraEnemigo
    {
        public const float SegundosVisibleSinDanio = 6f;
        public const float SegundosVisibleJefe = 10f;
        public const float DistanciaMaxima = 35f;
        public const float DistanciaEscalaBase = 8f;
        public const float EscalaMinima = 0.45f;
        public const float EscalaMaxima = 1.2f;

        /// <summary>Segundos que lleva sin recibir daño (muy grande si nunca lo recibió).</summary>
        public static float SegundosDesdeDanio(float ahora, float ultimoDanio) => ultimoDanio < 0f ? float.MaxValue : ahora - ultimoDanio;

        /// <summary>True si la barra pequeña debe verse.</summary>
        public static bool BarraPequenaVisible(bool estaKO, float segundosSinDanio, float distancia, bool tapadoPorPared)
        {
            if (estaKO || tapadoPorPared)
            {
                return false;
            }
            return segundosSinDanio <= SegundosVisibleSinDanio && distancia <= DistanciaMaxima;
        }

        /// <summary>True si el jefe es candidato a la barra grande (el que se muestra es el más reciente, ver <see cref="ElegirJefe"/>).</summary>
        public static bool JefeEnCombate(bool estaKO, float segundosSinDanio) => !estaKO && segundosSinDanio <= SegundosVisibleJefe;

        /// <summary>Índice del jefe con menos segundos sin daño entre los candidatos, o -1 si no hay.</summary>
        public static int ElegirJefe(float[] segundosSinDanio, bool[] candidato)
        {
            int mejor = -1;
            for (int i = 0; i < segundosSinDanio.Length; i++)
            {
                if (!candidato[i])
                {
                    continue;
                }
                if (mejor < 0 || segundosSinDanio[i] < segundosSinDanio[mejor])
                {
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>Escala de la barra según la distancia: 1 a la distancia base, más chica lejos, más grande cerca.</summary>
        public static float Escala(float distancia)
        {
            return Mathf.Clamp(DistanciaEscalaBase / Mathf.Max(distancia, 0.01f), EscalaMinima, EscalaMaxima);
        }

        /// <summary>Opacidad: se desvanece el último segundo de visibilidad.</summary>
        public static float Opacidad(float segundosSinDanio, float segundosVisible)
        {
            float resto = segundosVisible - segundosSinDanio;
            return Mathf.Clamp01(resto / 1f);
        }
    }
}
