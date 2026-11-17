using System;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.UI.Rueda
{
    /// <summary>
    /// T-1315: qué armas y objetos van en la rueda. Por omisión todo lo que el jugador trae va equipado (para no romper partidas ni pruebas); lo que
    /// la persona quita en el inventario (I) se guarda aquí como "quitado" y la rueda ya no lo muestra. Dura la sesión (no se guarda con la partida).
    /// Los ids son los de las casillas del inventario: <see cref="IdArma"/> para las armas y el Id del objeto para los consumibles.
    /// </summary>
    public static class EquipoRueda
    {
        private static readonly HashSet<string> quitados = new HashSet<string>();

        /// <summary>Se avisa al equipar o quitar algo.</summary>
        public static event Action Cambio;

        /// <summary>Lo máximo que cabe en la rueda: las casillas que ya tiene (armas y objetos).</summary>
        public static int Maximo => ReglaRuedaArmas.Casillas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            quitados.Clear();
            Cambio = null;
        }

        /// <summary>El id del arma en el inventario y en la rueda.</summary>
        public static string IdArma(ArmaAPie arma) => "arma_" + arma;

        /// <summary>True si el id va en la rueda (todo lo que no se quitó).</summary>
        public static bool Equipado(string id) => string.IsNullOrEmpty(id) || !quitados.Contains(id);

        /// <summary>Equipa o quita un id. Equipar no tiene tope aquí: el tope se revisa con <see cref="HayLugar"/> antes de equipar.</summary>
        public static void Poner(string id, bool equipado)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }
            bool cambio = equipado ? quitados.Remove(id) : quitados.Add(id);
            if (cambio)
            {
                Cambio?.Invoke();
            }
        }

        /// <summary>True si todavía cabe otra casilla en la rueda con <paramref name="equipadas"/> ya puestas.</summary>
        public static bool HayLugar(int equipadas) => equipadas < Maximo;

        /// <summary>Deja todo equipado otra vez (pruebas y partida nueva).</summary>
        public static void Reiniciar()
        {
            if (quitados.Count > 0)
            {
                quitados.Clear();
                Cambio?.Invoke();
            }
        }

        /// <summary>
        /// El objeto que se consume con la tecla: el primero equipado que sirve ahora; si ninguno sirve, el primero equipado (para mostrarlo). -1 si no hay.
        /// </summary>
        public static int ElegirConsumible(IReadOnlyList<string> ids, Func<int, bool> sirve)
        {
            int primero = -1;
            for (int i = 0; ids != null && i < ids.Count; i++)
            {
                if (!Equipado(ids[i]))
                {
                    continue;
                }
                if (primero < 0)
                {
                    primero = i;
                }
                if (sirve == null || sirve(i))
                {
                    return i;
                }
            }
            return primero;
        }
    }
}
