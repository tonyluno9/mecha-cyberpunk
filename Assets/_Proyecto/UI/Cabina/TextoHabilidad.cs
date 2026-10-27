using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Mecha;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>Textos compartidos de las habilidades en las pantallas (T-177): nombre con nivel en romano y tecla de cada ranura.</summary>
    public static class TextoHabilidad
    {
        private static readonly string[] Romanos = { "I", "II", "III", "IV" };

        /// <summary>Nivel en romano (1 a 4). Fuera de rango lo escribe en número.</summary>
        public static string Nivel(int nivel)
        {
            return nivel >= 1 && nivel <= Romanos.Length ? Romanos[nivel - 1] : nivel.ToString();
        }

        /// <summary>"MISIL II".</summary>
        public static string Etiqueta(DatosHabilidad habilidad, int nivel)
        {
            return habilidad.Nombre + " " + Nivel(nivel);
        }

        /// <summary>"1" para el brazo izquierdo, "2" para el torso, vacío para las demás ranuras.</summary>
        public static string Tecla(RanuraPieza ranura)
        {
            switch (ranura)
            {
                case RanuraPieza.BrazoIzquierdo: return "1";
                case RanuraPieza.Torso: return "2";
                default: return string.Empty;
            }
        }
    }
}
