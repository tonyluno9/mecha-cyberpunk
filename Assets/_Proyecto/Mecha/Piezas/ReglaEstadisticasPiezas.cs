using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Regla pura: suma las piezas puestas sobre los valores de serie del mecha (T-166).</summary>
    public static class ReglaEstadisticasPiezas
    {
        /// <summary>Recarga mínima que dejan las piezas (s).</summary>
        public const float RecargaMinima = 0.3f;

        /// <summary>Compone con una lista: cada pieza va en su propia ranura. Null o repetida en una ranura: se ignora con warning.</summary>
        public static ValoresMecha Componer(ValoresMecha deSerie, IReadOnlyList<DatosPieza> puestas)
        {
            var porRanura = new Dictionary<RanuraPieza, DatosPieza>();
            if (puestas != null)
            {
                foreach (DatosPieza pieza in puestas)
                {
                    if (pieza == null) { Debug.LogWarning("[Piezas] Pieza null: se ignora."); continue; }
                    if (porRanura.ContainsKey(pieza.Ranura))
                    {
                        Debug.LogWarning($"[Piezas] Ranura {pieza.Ranura} repetida ({pieza.Id}): se ignora.");
                        continue;
                    }
                    porRanura[pieza.Ranura] = pieza;
                }
            }
            return Componer(deSerie, porRanura);
        }

        /// <summary>Compone con la pieza de cada ranura. Una pieza en la ranura equivocada o null se ignora con warning.</summary>
        public static ValoresMecha Componer(ValoresMecha deSerie, IReadOnlyDictionary<RanuraPieza, DatosPieza> porRanura)
        {
            float vida = 0f, danio = 0f, velocidad = 0f, recarga = 0f;
            if (porRanura != null)
            {
                foreach (KeyValuePair<RanuraPieza, DatosPieza> par in porRanura)
                {
                    DatosPieza pieza = par.Value;
                    if (pieza == null) { Debug.LogWarning($"[Piezas] Ranura {par.Key} sin pieza: se ignora."); continue; }
                    if (pieza.Ranura != par.Key)
                    {
                        Debug.LogWarning($"[Piezas] {pieza.Id} es de {pieza.Ranura} y no cabe en {par.Key}: se ignora.");
                        continue;
                    }
                    vida += pieza.BonoVida;
                    danio += pieza.BonoDanio;
                    velocidad += pieza.BonoVelocidad;
                    recarga += pieza.BonoRecarga;
                }
            }

            ValoresMecha v = deSerie;
            v.vidaMaxima = deSerie.vidaMaxima + vida;
            v.danioGolpe = deSerie.danioGolpe + danio;
            v.velocidadMaxima = deSerie.velocidadMaxima + velocidad;
            float razon = deSerie.velocidadMaxima > 0f ? v.velocidadMaxima / deSerie.velocidadMaxima : 1f;
            v.aceleracion = deSerie.aceleracion * razon;
            v.desaceleracion = deSerie.desaceleracion * razon;
            v.velocidadGiroMaxima = deSerie.velocidadGiroMaxima * razon;
            v.recargaGolpe = Mathf.Max(Mathf.Min(deSerie.recargaGolpe, RecargaMinima), deSerie.recargaGolpe + recarga);
            return v;
        }
    }
}
