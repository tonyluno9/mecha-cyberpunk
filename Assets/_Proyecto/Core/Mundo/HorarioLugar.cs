using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Horario de un lugar: de qué hora a qué hora está abierto. Es un dato serializable que Alan pone en sus
    /// assets (Mundo/Datos/); la regla de "¿está abierto?" vive aquí y no se implementa en otro lado.
    /// Horas de 0 a 24 con decimales (21.5 es 21:30). Si <c>abre</c> es mayor que <c>cierra</c>, cruza la medianoche.
    /// Ojo: un horario de 0 a 24 se normaliza a 0 = 0 y queda Cerrado (nunca abre). Para abierto todo el día se usa
    /// <see cref="Siempre"/> / <c>siempreAbierto</c>.
    /// </summary>
    [Serializable]
    public struct HorarioLugar
    {
        private const float HorasPorDia = 24f;
        private const float MinutosPorHora = 60f;

        [SerializeField, Range(0f, 24f), Tooltip("Hora a la que abre (h, 0 a 24).")]
        private float abre;

        [SerializeField, Range(0f, 24f), Tooltip("Hora a la que cierra (h, 0 a 24). Si es menor que abre, cruza la medianoche.")]
        private float cierra;

        [SerializeField, Tooltip("Si está marcado, abre las 24 horas y abre/cierra se ignoran.")]
        private bool siempreAbierto;

        public HorarioLugar(float abre, float cierra, bool siempreAbierto = false)
        {
            this.abre = abre;
            this.cierra = cierra;
            this.siempreAbierto = siempreAbierto;
        }

        /// <summary>Horario de 24 horas.</summary>
        public static HorarioLugar Siempre => new HorarioLugar(0f, 0f, true);

        /// <summary>Hora de apertura (h).</summary>
        public float Abre => abre;
        /// <summary>Hora de cierre (h).</summary>
        public float Cierra => cierra;
        /// <summary>True si abre las 24 horas.</summary>
        public bool SiempreAbierto => siempreAbierto;

        /// <summary>
        /// ¿Está abierto a esa hora? La hora se normaliza a [0, 24) (24 es 0, 25 es 1, -1 es 23).
        /// Siempre abierto da true; <c>abre == cierra</c> da false (nunca abre; también 0 a 24, porque 24 se normaliza
        /// a 0: para todo el día usa <see cref="Siempre"/>); si <c>abre &lt; cierra</c> está abierto
        /// en [abre, cierra); si no, cruza la medianoche y está abierto si <c>hora &gt;= abre</c> o <c>hora &lt; cierra</c>.
        /// </summary>
        public bool EstaAbierto(float hora)
        {
            if (siempreAbierto) return true;
            float h = Normalizar(hora);
            float a = Normalizar(abre);
            float c = Normalizar(cierra);
            if (a == c) return false;
            if (a < c) return h >= a && h < c;
            return h >= a || h < c;
        }

        /// <summary>
        /// Horas que faltan para que abra, dando la vuelta a la medianoche. 0 si ya está abierto;
        /// <see cref="float.PositiveInfinity"/> si nunca abre.
        /// </summary>
        public float HorasHastaAbrir(float hora)
        {
            if (EstaAbierto(hora)) return 0f;
            if (Normalizar(abre) == Normalizar(cierra)) return float.PositiveInfinity;
            float falta = Normalizar(abre) - Normalizar(hora);
            if (falta < 0f) falta += HorasPorDia;
            return falta;
        }

        /// <summary>Texto para el letrero: "20:00 a 06:00", "24 horas" o "Cerrado" (si nunca abre).</summary>
        public string Texto()
        {
            if (siempreAbierto) return "24 horas";
            if (Normalizar(abre) == Normalizar(cierra)) return "Cerrado";
            return TextoHora(abre) + " a " + TextoHora(cierra);
        }

        /// <summary>
        /// Hora como "HH:MM". 21.5 da "21:30", 24 da "00:00". Trunca los minutos: 7.999 da "07:59", nunca "08:60".
        /// </summary>
        public static string TextoHora(float hora)
        {
            int totalMinutos = Mathf.FloorToInt(Normalizar(hora) * MinutosPorHora);
            int horas = totalMinutos / (int)MinutosPorHora;
            int minutos = totalMinutos % (int)MinutosPorHora;
            return horas.ToString("00") + ":" + minutos.ToString("00");
        }

        private static float Normalizar(float hora)
        {
            float h = hora % HorasPorDia;
            if (h < 0f) h += HorasPorDia;
            if (h >= HorasPorDia) h = 0f;
            return h;
        }
    }
}
