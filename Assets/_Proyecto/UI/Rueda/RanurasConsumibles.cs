using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.UI.Rueda
{
    /// <summary>
    /// T-1492: las dos ranuras de consumibles del HUD (la de E y la de C). Cada ranura guarda el id del objeto que la persona asignó; si no asignó nada, o
    /// lo asignado ya no lo lleva (o lo quitó de la rueda), se llena sola con el primer objeto que lleva y no está en la otra ranura. Las asigna
    /// <see cref="Rotar"/> (teclas Z y X) o <see cref="Asignar"/>. Dura la sesión, como <see cref="EquipoRueda"/>.
    /// Lógica pura: no lee el inventario, recibe la lista de ids que lleva (ya sin los quitados de la rueda).
    /// </summary>
    public static class RanurasConsumibles
    {
        public const int Cantidad = 2;

        private static readonly string[] asignados = new string[Cantidad];

        /// <summary>Se avisa al asignar o rotar.</summary>
        public static event System.Action Cambio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            Reiniciar();
            Cambio = null;
        }

        /// <summary>Quita las asignaciones (pruebas y partida nueva).</summary>
        public static void Reiniciar()
        {
            for (int i = 0; i < Cantidad; i++)
            {
                asignados[i] = null;
            }
        }

        /// <summary>El id asignado a mano a la ranura (puede ser null o de algo que ya no lleva).</summary>
        public static string Asignado(int ranura) => ranura >= 0 && ranura < Cantidad ? asignados[ranura] : null;

        /// <summary>
        /// Pone un objeto en una ranura. Si ya estaba en la otra, se intercambian. Un id vacío deja la ranura en automático.
        /// </summary>
        public static void Asignar(int ranura, string id)
        {
            if (ranura < 0 || ranura >= Cantidad)
            {
                return;
            }
            int otra = 1 - ranura;
            if (!string.IsNullOrEmpty(id) && asignados[otra] == id)
            {
                asignados[otra] = asignados[ranura];
            }
            asignados[ranura] = string.IsNullOrEmpty(id) ? null : id;
            Cambio?.Invoke();
        }

        /// <summary>
        /// Los ids que quedan en las dos ranuras con lo que lleva: lo asignado si lo lleva; si no, el primero que lleva y no esté ya puesto. Null si no hay.
        /// </summary>
        public static string[] Efectivos(IReadOnlyList<string> lleva)
        {
            string[] r = new string[Cantidad];
            // Primero lo asignado a mano que todavía lleva.
            for (int i = 0; i < Cantidad; i++)
            {
                if (Lleva(lleva, asignados[i]))
                {
                    r[i] = asignados[i];
                }
            }
            // Luego los huecos se llenan solos.
            for (int i = 0; i < Cantidad; i++)
            {
                if (r[i] != null)
                {
                    continue;
                }
                for (int k = 0; lleva != null && k < lleva.Count; k++)
                {
                    if (!Esta(r, lleva[k]))
                    {
                        r[i] = lleva[k];
                        break;
                    }
                }
            }
            return r;
        }

        /// <summary>Cambia el objeto de la ranura por el siguiente que lleva y no está en la otra. Devuelve el id nuevo, o el mismo si no hay otro.</summary>
        public static string Rotar(int ranura, IReadOnlyList<string> lleva)
        {
            if (ranura < 0 || ranura >= Cantidad || lleva == null || lleva.Count == 0)
            {
                return null;
            }
            string[] actuales = Efectivos(lleva);
            string actual = actuales[ranura];
            string otra = actuales[1 - ranura];
            int inicio = IndiceDe(lleva, actual);
            for (int paso = 1; paso <= lleva.Count; paso++)
            {
                string candidato = lleva[(inicio + paso + lleva.Count) % lleva.Count];
                if (candidato != otra)
                {
                    // Se fija también la otra ranura, para que no cambie sola al liberarse este objeto.
                    if (otra != null)
                    {
                        asignados[1 - ranura] = otra;
                    }
                    asignados[ranura] = candidato;
                    Cambio?.Invoke();
                    return candidato;
                }
            }
            return actual;
        }

        private static bool Lleva(IReadOnlyList<string> lleva, string id) => !string.IsNullOrEmpty(id) && IndiceDe(lleva, id) >= 0;

        private static bool Esta(string[] r, string id)
        {
            for (int i = 0; i < r.Length; i++)
            {
                if (r[i] == id)
                {
                    return true;
                }
            }
            return false;
        }

        private static int IndiceDe(IReadOnlyList<string> lista, string id)
        {
            for (int i = 0; lista != null && id != null && i < lista.Count; i++)
            {
                if (lista[i] == id)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
