using System;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1540: tabla de impactos de los clips de Blender (BITACORA 19-nov y borradores del 21-nov): fotograma (1 a <c>Cuadros</c>) en que el cuerpo toca el piso o el golpe conecta, y el
    /// sonido que va ahí. Como la tabla de pisadas (<see cref="TablaPisadasPJ01"/>), <see cref="ReceptorImpactosPJ01"/> los dispara con el tiempo normalizado del estado del Animator
    /// sin tocar ningún clip ni su .meta. Pura: sin Unity. Solo hay impactos que el resto del juego no suena ya: el aterrizaje por caída y el golpe de puños los suenan
    /// ControladorAPie y GolpeAPie, y las balas y el daño ya traen su sonido, por eso no están aquí.
    /// </summary>
    public static class TablaImpactosPJ01
    {
        /// <summary>Sonido del cuerpo que cae al piso: el del aterrizaje del jugador (el catálogo no tiene un "cuerpo cae" propio; ver el reporte de T-1540).</summary>
        public const string SonidoCuerpoAlPiso = "JUG_ATERRIZAJE";
        /// <summary>Sonido del escudo del E16 al pegar: golpe a metal.</summary>
        public const string SonidoGolpeMetal = "MEL_GOLPE_METAL";
        /// <summary>Lo más que puede tener un clip (el receptor usa un detector por impacto).</summary>
        public const int MaximoPorClip = 2;

        public readonly struct Impacto
        {
            public readonly string Clip;
            public readonly int Cuadros;
            public readonly int Cuadro;
            public readonly string Sonido;

            public Impacto(string clip, int cuadros, int cuadro, string sonido)
            {
                Clip = clip;
                Cuadros = cuadros;
                Cuadro = cuadro;
                Sonido = sonido;
            }

            /// <summary>Fase (0 a 1 dentro del clip) del impacto; 0 = cuadro 1.</summary>
            public float Fase => Cuadros <= 1 ? 0f : Math.Max(0f, (Cuadro - 1f) / (Cuadros - 1f));
        }

        // Las muertes de PJ01 (46 cuadros) las comparten NPC_Muerte_* y Guardia_Muerte (= PJ01_Muerte_Frente): el nombre cambia, el clip es el mismo.
        public static readonly Impacto[] Todos =
        {
            new Impacto("PJ01_Muerte_Frente", 46, 10, SonidoCuerpoAlPiso), new Impacto("PJ01_Muerte_Frente", 46, 24, SonidoCuerpoAlPiso),
            new Impacto("NPC_Muerte_Frente", 46, 10, SonidoCuerpoAlPiso), new Impacto("NPC_Muerte_Frente", 46, 24, SonidoCuerpoAlPiso),
            new Impacto("Guardia_Muerte", 46, 10, SonidoCuerpoAlPiso), new Impacto("Guardia_Muerte", 46, 24, SonidoCuerpoAlPiso),
            new Impacto("PJ01_Muerte_Espaldas", 46, 18, SonidoCuerpoAlPiso), new Impacto("PJ01_Muerte_Espaldas", 46, 23, SonidoCuerpoAlPiso),
            new Impacto("NPC_Muerte_Espaldas", 46, 18, SonidoCuerpoAlPiso), new Impacto("NPC_Muerte_Espaldas", 46, 23, SonidoCuerpoAlPiso),
            new Impacto("PJ01_Muerte_Explosion", 56, 18, SonidoCuerpoAlPiso),
            new Impacto("PJ01_Muerte_Atropello", 54, 15, SonidoCuerpoAlPiso),
            new Impacto("PJ01_Muerte_CaidaAltura", 42, 8, SonidoCuerpoAlPiso),
            new Impacto("PJ01_Derribo_Explosion", 40, 14, SonidoCuerpoAlPiso),
            new Impacto("E18_Caida_Aterrizaje", 31, 5, SonidoCuerpoAlPiso),
            new Impacto("Guardia_Caida_Aterrizaje", 31, 5, SonidoCuerpoAlPiso),
            new Impacto("E16_Golpe_Escudo", 29, 10, SonidoGolpeMetal),
        };

        /// <summary>Los impactos del clip por su nombre exacto (vacío si no lleva). Escribe en <paramref name="salida"/> (hasta su largo) y devuelve cuántos.</summary>
        public static int Buscar(string nombreClip, Impacto[] salida)
        {
            if (string.IsNullOrEmpty(nombreClip) || salida == null)
            {
                return 0;
            }
            int n = 0;
            foreach (Impacto i in Todos)
            {
                if (i.Clip == nombreClip && n < salida.Length)
                {
                    salida[n++] = i;
                }
            }
            return n;
        }

        /// <summary>True si el clip lleva al menos un impacto.</summary>
        public static bool Tiene(string nombreClip) => Buscar(nombreClip, new Impacto[MaximoPorClip]) > 0;
    }
}
