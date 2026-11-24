using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-historia-libre-23nov / T-musica-24nov: en qué parte del juego va el jugador. La publica la historia (guiada, campo de tiro) y la escalada
    /// del modo libre (fases 1 a 6); la escuchan la música, el HUD y quien lo necesite. Nunca retrocede mientras el jugador sigue vivo, salvo con
    /// <see cref="Reiniciar"/> (REINTENTAR o partida nueva).
    /// </summary>
    public static class FaseJuego
    {
        /// <summary>Fase actual.</summary>
        public static Fase Actual { get; private set; } = Fase.Guiada;

        /// <summary>Fase de escalada 1 a 6 mientras <see cref="Actual"/> es una fase del modo libre; 0 si no.</summary>
        public static int NivelEscalada { get; private set; }

        /// <summary>True desde que empieza el modo libre (al salir de la tienda de Tino tras el campo de tiro).</summary>
        public static bool ModoLibre => Actual >= Fase.Skulls && Actual != Fase.Muerto;

        /// <summary>Se avisa con la fase nueva cada vez que cambia.</summary>
        public static event Action<Fase> Cambio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            Actual = Fase.Guiada;
            NivelEscalada = 0;
            Cambio = null;
        }

        /// <summary>Pasa a <paramref name="nueva"/> (con su nivel de escalada, 0 si no aplica). No hace nada si ya está ahí o si es una fase anterior.</summary>
        public static void Fijar(Fase nueva, int nivel = 0)
        {
            if (nueva == Actual && nivel == NivelEscalada)
            {
                return;
            }
            if (nueva < Actual && nueva != Fase.Guiada)
            {
                return; // nunca hacia atrás mientras se sigue vivo
            }
            Actual = nueva;
            NivelEscalada = nivel;
            Cambio?.Invoke(nueva);
        }

        /// <summary>Vuelve a <paramref name="fase"/> aunque sea anterior (REINTENTAR, partida nueva, cargar).</summary>
        public static void Reiniciar(Fase fase, int nivel = 0)
        {
            Actual = fase;
            NivelEscalada = nivel;
            Cambio?.Invoke(fase);
        }

        public enum Fase
        {
            /// <summary>Historia guiada: cinemáticas, calle, Doc, Tino.</summary>
            Guiada = 0,
            /// <summary>Dentro del campo de tiro holográfico.</summary>
            CampoTiro = 1,
            /// <summary>Modo libre: los SKULLS (arranque del modo libre).</summary>
            Skulls = 2,
            /// <summary>Modo libre: policía (escalada 1 a 3).</summary>
            Policia = 3,
            /// <summary>Modo libre: NOVA (escalada 4 a 6).</summary>
            Nova = 4,
            /// <summary>Pantalla HAS MUERTO.</summary>
            Muerto = 5,
        }
    }
}
