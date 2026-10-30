using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// La forma del barrio que dibujan el mapa grande y el minimapa (T-409; antes eran constantes de <see cref="PantallaMapa"/>): 6 manzanas
    /// de 80 m de lado con paso de 94 m (las calles de 14 m quedan entre ellas y alrededor). El mapa usa X (este) y Z (norte) del mundo.
    /// </summary>
    public static class GeometriaBarrio
    {
        public const float LadoManzana = 80f;
        public const float Paso = 94f;
        public const int Columnas = 3;
        public const int Filas = 2;

        // De oeste a este y de norte (arriba) a sur.
        private static readonly Vector2[] centros =
        {
            new Vector2(-Paso, Paso * 0.5f), new Vector2(0f, Paso * 0.5f), new Vector2(Paso, Paso * 0.5f),
            new Vector2(-Paso, -Paso * 0.5f), new Vector2(0f, -Paso * 0.5f), new Vector2(Paso, -Paso * 0.5f),
        };

        /// <summary>Centros de las manzanas (m: x este, y norte).</summary>
        public static IReadOnlyList<Vector2> Centros => centros;

        public static int Cantidad => centros.Length;

        /// <summary>Ancho de las calles (m).</summary>
        public static float Calle => Paso - LadoManzana;

        /// <summary>Tamaño (m) del barrio con su calle de alrededor.</summary>
        public static Vector2 TamanoConCalles => new Vector2(Columnas * Paso + Calle, Filas * Paso + Calle);

        /// <summary>De mundo a mapa (m): X y Z.</summary>
        public static Vector2 AMapa(Vector3 mundo) => new Vector2(mundo.x, mundo.z);

        /// <summary>De mapa (m) a mundo, a la altura 0.</summary>
        public static Vector3 AMundo(Vector2 mapa) => new Vector3(mapa.x, 0f, mapa.y);

        /// <summary>El id de la zona de la manzana <paramref name="indice"/>, o "" si no hay territorio o cae fuera de las zonas.</summary>
        public static string Zona(int indice, ITerritorio territorio) => territorio != null ? territorio.ZonaEn(AMundo(centros[indice])) : string.Empty;
    }
}
