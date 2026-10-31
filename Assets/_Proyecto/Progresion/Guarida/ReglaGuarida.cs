using System;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Reglas puras de la guarida (T-448): precio del siguiente nivel y el valor de cada efecto según el nivel.</summary>
    public static class ReglaGuarida
    {
        /// <summary>Precio de pasar al siguiente nivel; 0 si ya está en el máximo.</summary>
        public static int PrecioSiguiente(int precioBase, int nivel, float[] factoresPrecio, int nivelMaximo)
        {
            if (precioBase <= 0 || nivel < 0 || nivel >= nivelMaximo || factoresPrecio == null || factoresPrecio.Length == 0)
            {
                return 0;
            }
            float factor = factoresPrecio[Math.Min(nivel, factoresPrecio.Length - 1)];
            return Math.Max(1, (int)Math.Round(precioBase * factor));
        }

        /// <summary>El valor de la tabla para ese nivel (1 = primer elemento); en el nivel 0 devuelve <paramref name="sinMejora"/>.</summary>
        public static float PorNivel(float[] tabla, int nivel, float sinMejora)
        {
            if (nivel <= 0 || tabla == null || tabla.Length == 0)
            {
                return sinMejora;
            }
            return tabla[Math.Min(nivel, tabla.Length) - 1];
        }

        /// <summary>Recorta un nivel al rango válido (de 0 al máximo).</summary>
        public static int RecortarNivel(int nivel, int nivelMaximo) => Math.Max(0, Math.Min(nivel, Math.Max(0, nivelMaximo)));
    }
}
