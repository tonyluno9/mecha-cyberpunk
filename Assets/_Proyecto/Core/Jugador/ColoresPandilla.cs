using System.Text;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// La paleta y el nombre de tu pandilla (T-415, D-tony-252). Son 4 colores que no se parecen al magenta de SKULLS, al verde ácido
    /// de SNAKES ni al rojo de los enemigos del minimapa. Está en Core porque la leen UI e IA, que no se ven entre sí.
    /// </summary>
    public static class ColoresPandilla
    {
        public const int Cantidad = 4;
        public const int Predeterminado = 0;
        public const string NombrePredeterminado = "LOS DE SUBURBIOS";
        public const int LargoMinimo = 3;
        public const int LargoMaximo = 16;

        private static readonly Color[] Colores =
        {
            new Color(1f, 0.502f, 0.102f),      // NARANJA  #FF801A
            new Color(0.102f, 0.902f, 0.902f),  // TURQUESA #1AE6E6
            new Color(1f, 0.851f, 0.102f),      // AMARILLO #FFD91A
            new Color(0.2f, 0.502f, 1f),        // AZUL     #3380FF
        };

        private static readonly string[] Nombres = { "NARANJA", "TURQUESA", "AMARILLO", "AZUL" };

        /// <summary>Un índice fuera de 0 a 3 da <see cref="Predeterminado"/>.</summary>
        public static int Normalizar(int indice) => indice >= 0 && indice < Cantidad ? indice : Predeterminado;

        public static Color Color(int indice) => Colores[Normalizar(indice)];

        public static string Nombre(int indice) => Nombres[Normalizar(indice)];

        /// <summary>
        /// Mayúsculas, solo letras (con Ñ y acentos) y espacios, espacios dobles a uno, máximo 16 caracteres. Si queda de menos de 3
        /// (o es null), el nombre predeterminado.
        /// </summary>
        public static string Limpiar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return NombrePredeterminado;
            }
            StringBuilder resultado = new StringBuilder(texto.Length);
            foreach (char c in texto.ToUpperInvariant())
            {
                if (char.IsLetter(c))
                {
                    resultado.Append(c);
                }
                else if (char.IsWhiteSpace(c) && resultado.Length > 0 && resultado[resultado.Length - 1] != ' ')
                {
                    resultado.Append(' ');
                }
            }
            string limpio = resultado.ToString().Trim();
            if (limpio.Length > LargoMaximo)
            {
                limpio = limpio.Substring(0, LargoMaximo).TrimEnd();
            }
            return limpio.Length < LargoMinimo ? NombrePredeterminado : limpio;
        }
    }
}
