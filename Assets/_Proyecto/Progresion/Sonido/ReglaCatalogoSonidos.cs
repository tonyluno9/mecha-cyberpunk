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
            switch (prefijo)
            {
                case "PAN": return SeccionSonido.Voces;
                case "AMB": return SeccionSonido.Ambiente;
                case "MUS": return SeccionSonido.Musica;
                case "UI_": return SeccionSonido.Interfaz;
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

        /// <summary>Las secciones que arrancan apagadas (T-407, plan de la noche): Ambiente y Música.</summary>
        public static float VolumenInicial(SeccionSonido seccion)
        {
            return seccion == SeccionSonido.Ambiente || seccion == SeccionSonido.Musica ? 0f : 1f;
        }
    }
}
