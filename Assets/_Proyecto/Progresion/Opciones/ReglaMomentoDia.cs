using System;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Opciones
{
    /// <summary>T68: el momento del día que se ve en todo el juego. Siempre es de noche; solo se cambia a día desde OPCIONES (MOMENTO DEL DÍA).</summary>
    public enum MomentoDelDia
    {
        Noche = 0,
        Dia = 1,
    }

    /// <summary>
    /// T68: lógica pura de "siempre de noche". El momento elegido lo fija <see cref="ServicioOpciones"/>; <c>AplicadorMomentoDia</c> (Recorrido) lo
    /// aplica al reloj del mundo: si la hora cae fuera del momento elegido, salta a <see cref="HoraDe"/> sin pasar por el otro momento.
    /// El reloj sigue corriendo (los días siguen pasando a las 00:00); lo único que se salta es la parte de día (o la de noche en modo día).
    /// </summary>
    public static class ReglaMomentoDia
    {
        /// <summary>Hora a la que salta el reloj para el modo noche (la misma noche fija de la historia).</summary>
        public const float HoraNoche = 23f;

        /// <summary>Hora a la que salta el reloj para el modo día (mediodía).</summary>
        public const float HoraDia = 12f;

        /// <summary>Horas antes del amanecer en que ya no cuenta como noche (el cielo ya aclara).</summary>
        public const float MargenAmanecer = 3f;

        /// <summary>Horas después del anochecer en que todavía hay luz del atardecer.</summary>
        public const float MargenAnochecer = 1f;

        /// <summary>Horas después del amanecer / antes del anochecer en que todavía no es pleno día.</summary>
        public const float MargenDia = 2f;

        public const MomentoDelDia PorDefecto = MomentoDelDia.Noche;

        /// <summary>El momento vigente. Lo escribe <see cref="Fijar"/>.</summary>
        public static MomentoDelDia Actual { get; private set; } = PorDefecto;

        /// <summary>Se avisa con el momento nuevo y si lo cambió el jugador a mano (true) o solo se cargaron las opciones (false).</summary>
        public static event Action<MomentoDelDia, bool> Cambio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = PorDefecto;
            Cambio = null;
        }

        /// <summary>Cambia el momento. Avisa solo si de verdad cambió.</summary>
        public static void Fijar(MomentoDelDia momento, bool porJugador)
        {
            if (!Enum.IsDefined(typeof(MomentoDelDia), momento))
            {
                momento = PorDefecto;
            }
            if (Actual == momento)
            {
                return;
            }
            Actual = momento;
            Cambio?.Invoke(momento, porJugador);
        }

        /// <summary>La hora a la que salta el reloj para ese momento.</summary>
        public static float HoraDe(MomentoDelDia momento) => momento == MomentoDelDia.Dia ? HoraDia : HoraNoche;

        /// <summary>
        /// True si la hora está fuera del momento elegido y el reloj debe saltar a <see cref="HoraDe"/>. Noche: vale de (anochecer + margen) a
        /// (amanecer - margen). Día: vale de (amanecer + margen) a (anochecer - margen). Una hora rara (NaN) siempre vuelve.
        /// </summary>
        public static bool HayQueVolver(MomentoDelDia momento, float hora, float amanecer, float anochecer)
        {
            if (float.IsNaN(hora) || float.IsInfinity(hora))
            {
                return true;
            }
            float h = Mathf.Repeat(hora, 24f);
            if (momento == MomentoDelDia.Dia)
            {
                return !(h >= amanecer + MargenDia && h < anochecer - MargenDia);
            }
            return !(h >= anochecer + MargenAnochecer || h < amanecer - MargenAmanecer);
        }
    }
}
