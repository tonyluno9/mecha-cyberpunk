using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Arma la lista de peleas del mostrador (T-159, D-tony-99), en este orden: el siguiente rival de la liga actual (si no eres
    /// campeón del clandestino), las revanchas de los rivales ya vencidos de la liga actual, la casera y, si hay una liga más alta,
    /// el aviso de la que sigue. Lógica pura: no lee assets.
    /// </summary>
    public static class ReglaCartelera
    {
        /// <param name="premioGanar">Premio de ganar de cada (liga, índice), ambos desde 1.</param>
        /// <param name="siguiente">Siguiente rival por vencer; 0 si ya eres campeón de la última liga.</param>
        public static List<OpcionCartelera> Armar(int cantidadLigas, int rivalesPorLiga, int ligaActual, int siguiente,
            float fraccionRevancha, int premioCasera, Func<int, int, int> premioGanar)
        {
            var lista = new List<OpcionCartelera>();
            bool campeon = siguiente == 0;
            if (!campeon)
            {
                lista.Add(new OpcionCartelera(TipoOpcionCartelera.Siguiente, ligaActual, siguiente, siguiente == rivalesPorLiga,
                    premioGanar(ligaActual, siguiente)));
            }
            int vencidos = campeon ? rivalesPorLiga : siguiente - 1;
            for (int indice = 1; indice <= vencidos; indice++)
            {
                int premio = Mathf.RoundToInt(premioGanar(ligaActual, indice) * Mathf.Max(0f, fraccionRevancha));
                lista.Add(new OpcionCartelera(TipoOpcionCartelera.Revancha, ligaActual, indice, indice == rivalesPorLiga, premio));
            }
            lista.Add(new OpcionCartelera(TipoOpcionCartelera.Casera, 0, 0, false, premioCasera));
            if (ligaActual < cantidadLigas)
            {
                lista.Add(new OpcionCartelera(TipoOpcionCartelera.Bloqueada, ligaActual + 1, rivalesPorLiga, true, 0));
            }
            return lista;
        }
    }
}
