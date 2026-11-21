using System;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>Quién es un NPC para el sonido de su dolor (T-1533).</summary>
    public enum GeneroNpc
    {
        Hombre,
        Mujer,
        Robot
    }

    /// <summary>
    /// T-1533: qué voz de dolor suena cuando un NPC (peatón o gente de banqueta) recibe daño o lo atropellan: NPC_H_DOLOR (hombre), NPC_M_DOLOR (mujer) o las
    /// chispas del robot. El género sale del nombre del visual (Visual_man, Visual_woman, Visual_NP12_Abuela_a...). Los visuales NP08 turista, NP09 corredor y
    /// NP14 oficinista no dicen su género: salen hombre o mujer repartidos por una semilla estable (el número de instancia). Lógica pura.
    /// </summary>
    public static class ReglaSonidoNpc
    {
        public const string IdDolorHombre = "NPC_H_DOLOR";
        public const string IdDolorMujer = "NPC_M_DOLOR";
        public const string IdDolorRobot = "ROB_CHISPAS_DANIO";

        private static readonly string[] Robots = { "robot" };
        private static readonly string[] Mujeres = { "woman", "mujer", "abuela", "mesera", "doctora", "femen" };
        private static readonly string[] Hombres = { "man", "hombre", "pescador", "elotero", "estibador", "repartidor", "skull", "pandillero", "maton", "guardia", "policia", "mecanico", "inventor", "hacker", "doctor" };

        /// <summary>El género que dice el nombre; si no lo dice (turista, corredor, oficinista, un nombre raro), se reparte por <paramref name="semilla"/>.</summary>
        public static GeneroNpc Genero(string nombreVisual, int semilla)
        {
            string n = nombreVisual ?? string.Empty;
            if (Contiene(n, Robots)) return GeneroNpc.Robot;
            if (Contiene(n, Mujeres)) return GeneroNpc.Mujer;
            if (Contiene(n, Hombres)) return GeneroNpc.Hombre;
            return ((semilla % 2) + 2) % 2 == 0 ? GeneroNpc.Mujer : GeneroNpc.Hombre;
        }

        /// <summary>El ID del sonido de dolor de ese NPC.</summary>
        public static string IdDolor(string nombreVisual, int semilla)
        {
            switch (Genero(nombreVisual, semilla))
            {
                case GeneroNpc.Mujer: return IdDolorMujer;
                case GeneroNpc.Robot: return IdDolorRobot;
                default: return IdDolorHombre;
            }
        }

        private static bool Contiene(string texto, string[] partes)
        {
            foreach (string parte in partes)
            {
                if (texto.IndexOf(parte, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
