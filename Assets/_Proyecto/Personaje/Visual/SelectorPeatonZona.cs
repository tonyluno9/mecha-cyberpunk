using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1409: lógica pura para elegir el visual de un peatón según la zona. Sin Unity: recibe nombres, pesos y la tirada (0 a 1) ya sacada.
    /// </summary>
    public static class SelectorPeatonZona
    {
        /// <summary>Resultado de "usa la persona de siempre" (ninguna variante).</summary>
        public const int Persona = -1;

        /// <summary>Posición de la zona con ese nombre (sin distinguir mayúsculas), o -1 si no hay zona o no está.</summary>
        public static int BuscarZona(IReadOnlyList<string> nombres, string zona)
        {
            if (nombres == null || string.IsNullOrEmpty(zona))
            {
                return -1;
            }
            for (int i = 0; i < nombres.Count; i++)
            {
                if (string.Equals(nombres[i], zona, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Elige entre la persona de siempre (peso <paramref name="pesoPersona"/>) y las variantes (<paramref name="pesos"/>).
        /// <paramref name="tirada"/> va de 0 a 1 (se recorta). Devuelve el índice de la variante, o <see cref="Persona"/>.
        /// Los pesos negativos o cero no salen nunca; si todo pesa cero, sale la persona.
        /// </summary>
        public static int Elegir(IReadOnlyList<float> pesos, float pesoPersona, float tirada)
        {
            float persona = Math.Max(0f, pesoPersona);
            float total = persona;
            for (int i = 0; pesos != null && i < pesos.Count; i++)
            {
                total += Math.Max(0f, pesos[i]);
            }
            if (total <= 0f)
            {
                return Persona;
            }
            float x = Math.Min(Math.Max(tirada, 0f), 1f) * total;
            if (x < persona)
            {
                return Persona;
            }
            x -= persona;
            int ultimo = Persona;
            for (int i = 0; pesos != null && i < pesos.Count; i++)
            {
                float p = Math.Max(0f, pesos[i]);
                if (p <= 0f)
                {
                    continue;
                }
                ultimo = i;
                if (x < p)
                {
                    return i;
                }
                x -= p;
            }
            return ultimo; // tirada = 1 exacto: la última variante con peso
        }
    }
}
