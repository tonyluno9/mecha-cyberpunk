using System.Collections.Generic;
using System.Linq;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1512: qué edificios ED (nuevos-19nov, importados en Assets/Arte/Zonas/ED/) van en cada barrio. Cuentas puras, sin escena.
    /// Las claves de barrio son las de ReemplazarEdificiosT1502 (NeonAlto, Suburbios, Tianguis, Mercado) y "" para el resto.
    /// </summary>
    public static class CalculoEdificiosT1512
    {
        public const string CarpetaEd = "Assets/Arte/Zonas/ED/";
        /// <summary>Estiramiento máximo de la primera pasada (la segunda usa el de T-1502).</summary>
        public const float DistorsionPrimeraPasada = 1.6f;

        /// <summary>Nombres de todos los edificios ED importados (código_variante).</summary>
        public static readonly string[] TodosLosEd =
        {
            "ED01_CasaSuburbios_a", "ED02_TienditaEsquina_a", "ED03_EdificioDeptos_b", "ED04_LocalesMercado_b", "ED05_EdificioMercado_a",
            "ED06_EdificioNeon_a", "ED07_HotelNeon_a", "ED08_OficinaNova_a", "ED08_OficinaNova_b", "ED09_PlantaBajaNova_b",
            "ED10_Bodega_a", "ED11_EdificioAbandonado_a", "ED12_Estacionamiento_a"
        };

        private static readonly Dictionary<string, string[]> PorBarrio = new Dictionary<string, string[]>
        {
            { "NeonAlto", new[] { "ED06_EdificioNeon_a", "ED07_HotelNeon_a", "ED08_OficinaNova_a", "ED08_OficinaNova_b", "ED09_PlantaBajaNova_b" } },
            { "Suburbios", new[] { "ED01_CasaSuburbios_a", "ED02_TienditaEsquina_a", "ED03_EdificioDeptos_b", "ED11_EdificioAbandonado_a" } },
            { "Tianguis", new[] { "ED04_LocalesMercado_b", "ED05_EdificioMercado_a", "ED02_TienditaEsquina_a", "ED03_EdificioDeptos_b", "ED10_Bodega_a" } },
            { "Mercado", new[] { "ED04_LocalesMercado_b", "ED05_EdificioMercado_a", "ED02_TienditaEsquina_a", "ED03_EdificioDeptos_b", "ED10_Bodega_a" } }
        };

        /// <summary>Ruta del prefab de un ED por su nombre (la receta de T-1382 lo deja junto a su carpeta).</summary>
        public static string RutaPrefab(string nombre)
        {
            return CarpetaEd + nombre + ".prefab";
        }

        /// <summary>Nombres de los ED de un barrio; para la clave vacía u otra desconocida, todos.</summary>
        public static string[] NombresPara(string barrio)
        {
            string[] nombres;
            return barrio != null && PorBarrio.TryGetValue(barrio, out nombres) ? nombres : TodosLosEd;
        }

        public static string[] RutasPara(string barrio)
        {
            return NombresPara(barrio).Select(RutaPrefab).ToArray();
        }

        public static IEnumerable<string> BarriosConPaleta()
        {
            return PorBarrio.Keys;
        }
    }
}
