using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// Qué sonido de paso toca según el piso (T-596, T-1473): interior por el lugar (madera), y metal, mojado, madera, grava, tierra o concreto por los nombres
    /// del piso (material físico, material del renderer, collider y objeto). El catálogo elige la variante (PAS_TIERRA_01.._NN): aquí solo se pide el ID.
    /// </summary>
    public static class ReglaSuperficiePaso
    {
        public const string Concreto = "PAS_CONCRETO";
        public const string Madera = "PAS_MADERA";
        public const string Metal = "PAS_METAL";
        public const string Mojado = "PAS_MOJADO";
        public const string Tierra = "PAS_TIERRA";
        public const string Grava = "PAS_GRAVA";

        private static readonly string[] PalabrasMojado = { "mojad", "charco", "wet", "lluvia", "agua", "water", "puddle" };
        private static readonly string[] PalabrasMetal =
        {
            "metal", "rejilla", "acero", "lamina", "fierro", "hierro", "carro", "contenedor", "container", "pu08", "reja", "steel", "grate", "techobodega", "techo_bodega"
        };
        private static readonly string[] PalabrasMadera = { "madera", "muelle", "tablon", "tablones", "tabla", "wood", "plank", "parquet", "deck", "duela" };
        private static readonly string[] PalabrasGrava = { "grava", "gravel", "obra", "escombro", "rubble", "balasto" };
        private static readonly string[] PalabrasTierra = { "tierra", "baldio", "deshuesadero", "dirt", "soil", "lodo", "mud", "terreno", "lote" };

        /// <summary>
        /// El ID del paso. <paramref name="nombrePiso"/> son los nombres del piso juntos (material físico, material, collider, objeto; puede ser nulo);
        /// <paramref name="esMontable"/> si el piso es un carro; <paramref name="llueve"/>: si llueve y el piso es de calle (concreto) suena mojado.
        /// </summary>
        public static string IdPaso(IdLugar lugar, string nombrePiso, bool esMontable, bool llueve = false)
        {
            if (EsInterior(lugar)) return Madera;
            if (esMontable || Contiene(nombrePiso, PalabrasMetal)) return Metal;
            if (Contiene(nombrePiso, PalabrasMojado)) return Mojado;
            if (Contiene(nombrePiso, PalabrasMadera)) return Madera;
            if (Contiene(nombrePiso, PalabrasGrava)) return Grava;
            if (Contiene(nombrePiso, PalabrasTierra)) return Tierra;
            return llueve ? Mojado : Concreto;
        }

        public static bool EsInterior(IdLugar lugar)
        {
            return lugar == IdLugar.Casa || lugar == IdLugar.Taller || lugar == IdLugar.Bar || lugar == IdLugar.Clandestino
                || lugar == IdLugar.Casino || lugar == IdLugar.MercadoNegro || lugar == IdLugar.Clinica;
        }

        private static bool Contiene(string texto, string[] palabras)
        {
            if (string.IsNullOrEmpty(texto)) return false;
            foreach (string p in palabras)
            {
                if (texto.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
