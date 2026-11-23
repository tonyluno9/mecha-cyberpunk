using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Partida
{
    /// <summary>
    /// T-62: los avisos del tutorial que ya se mostraron en esta partida. Cada aviso sale una sola vez: quien lo muestra pregunta
    /// <see cref="YaVisto"/> y, al mostrarlo o al completar su paso, lo <see cref="Marcar"/>. El director de la historia guarda y carga
    /// la lista junto con el paso (<see cref="Serializar"/> / <see cref="Cargar"/>) y la limpia al empezar partida nueva (<see cref="Reiniciar"/>).
    /// </summary>
    public static class AvisosUnicos
    {
        public const string Protesis = "protesis";
        public const string Rueda = "rueda";
        public const string Maquina = "maquina";

        private static readonly HashSet<string> vistos = new HashSet<string>();

        /// <summary>Se dispara cuando se marca un aviso nuevo (el director guarda la partida).</summary>
        public static event Action Cambio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            vistos.Clear();
            Cambio = null;
        }

        public static bool YaVisto(string id) => !string.IsNullOrEmpty(id) && vistos.Contains(id);

        /// <summary>Marca el aviso como visto. Devuelve true si era la primera vez.</summary>
        public static bool Marcar(string id)
        {
            if (string.IsNullOrEmpty(id) || !vistos.Add(id))
            {
                return false;
            }
            Cambio?.Invoke();
            return true;
        }

        public static void Reiniciar() => vistos.Clear();

        /// <summary>Los ids vistos separados por coma (orden fijo), para el guardado.</summary>
        public static string Serializar()
        {
            List<string> lista = new List<string>(vistos);
            lista.Sort(StringComparer.Ordinal);
            return string.Join(",", lista);
        }

        /// <summary>Reemplaza lo visto por lo guardado (null o vacío = nada visto).</summary>
        public static void Cargar(string texto)
        {
            vistos.Clear();
            if (string.IsNullOrWhiteSpace(texto))
            {
                return;
            }
            foreach (string id in texto.Split(','))
            {
                string limpio = id.Trim();
                if (limpio.Length > 0)
                {
                    vistos.Add(limpio);
                }
            }
        }
    }
}
