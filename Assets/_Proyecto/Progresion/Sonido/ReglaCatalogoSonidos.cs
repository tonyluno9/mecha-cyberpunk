using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>Reglas puras del catálogo de sonidos (T-407): sección por prefijo del ID, variante al azar sin repetir y final sobre provisional.</summary>
    public static class ReglaCatalogoSonidos
    {
        private const int LargoPrefijo = 3;

        /// <summary>Sección de volumen por prefijo del ID (PAN_, AMB_, MUS_, UI_...). Lo demás va a Efectos.</summary>
        public static SeccionSonido SeccionDe(string id)
        {
            string prefijo = id != null && id.Length >= LargoPrefijo ? id.Substring(0, LargoPrefijo) : string.Empty;
            // T-1531: las voces de NPC y de robot, el clima y los objetos que zumban (ambiente), la radio (música) y los avisos de misión (interfaz).
            if (id != null && id.StartsWith("ROB_VOZ", StringComparison.Ordinal)) return SeccionSonido.Voces;
            if (MapaSonidos20nov.EsInteriorAmbiente(id)) return SeccionSonido.Ambiente;
            switch (prefijo)
            {
                case "PAN": return SeccionSonido.Voces;
                case "NPC": return SeccionSonido.Voces; // T-1531
                case "SKU": return SeccionSonido.Voces; // T-1623
                case "AMB": return SeccionSonido.Ambiente;
                case "CLI": return SeccionSonido.Ambiente; // T-1531
                case "OBJ": return SeccionSonido.Ambiente; // T-1531
                case "MUS": return SeccionSonido.Musica;
                case "RAD": return SeccionSonido.Musica; // T-1531
                case "MIS": return SeccionSonido.Interfaz; // T-1531
                case "UI_": return SeccionSonido.Interfaz;
                case "HUD": return SeccionSonido.Interfaz; // T-1471
                case "TIE": return SeccionSonido.Interfaz; // T-1471
                default: return SeccionSonido.Efectos;
            }
        }

        /// <summary>
        /// Elige un índice entre <paramref name="cantidad"/> variantes, distinto de <paramref name="ultimo"/> si hay más de una.
        /// -1 si no hay ninguna. <paramref name="azar"/> devuelve un valor en [0, 1).
        /// </summary>
        public static int ElegirVariante(int cantidad, int ultimo, Func<float> azar)
        {
            if (cantidad <= 0) return -1;
            if (cantidad == 1) return 0;
            bool hayUltimo = ultimo >= 0 && ultimo < cantidad;
            int posibles = hayUltimo ? cantidad - 1 : cantidad;
            int indice = Math.Min((int)(azar() * posibles), posibles - 1);
            if (hayUltimo && indice >= ultimo) indice++;
            return indice;
        }

        /// <summary>Los clips que cuentan: si hay finales, solo ellos; si no, los provisionales.</summary>
        public static IReadOnlyList<T> Efectivos<T>(IReadOnlyList<T> finales, IReadOnlyList<T> provisionales)
        {
            if (finales != null && finales.Count > 0) return finales;
            return provisionales ?? (IReadOnlyList<T>)Array.Empty<T>();
        }

        /// <summary>Volumen con el que arranca cada sección antes de leer las opciones. Ya ninguna arranca apagada (música del menú).</summary>
        public static float VolumenInicial(SeccionSonido seccion)
        {
            return 1f;
        }
    }
}
