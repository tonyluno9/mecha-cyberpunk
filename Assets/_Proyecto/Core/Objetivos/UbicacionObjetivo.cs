using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Ayudante único para saber dónde está un <see cref="Objetivo"/> en el mundo. Lo usan el HUD y los marcadores.
    /// Lógica pura: no busca nada por su cuenta; quien llama pasa <c>Registro&lt;ILugar&gt;.Todos</c>.
    /// </summary>
    public static class UbicacionObjetivo
    {
        /// <summary>
        /// Calcula la posición (m, mundo) del objetivo. Si <see cref="Objetivo.TienePosicion"/>, esa posición; si no,
        /// la <see cref="ILugar.PosicionEntrada"/> del primer lugar cuyo <c>Id</c> es <see cref="Objetivo.Lugar"/>.
        /// Devuelve false (y <paramref name="posicion"/> en cero) si no hay objetivo, si el lugar no está en la lista
        /// o si la lista es null o vacía.
        /// </summary>
        public static bool Resolver(Objetivo objetivo, IReadOnlyList<ILugar> lugares, out Vector3 posicion)
        {
            posicion = Vector3.zero;
            if (objetivo.TienePosicion)
            {
                posicion = objetivo.Posicion;
                return true;
            }
            if (objetivo.Lugar == IdLugar.Ninguno || lugares == null)
            {
                return false;
            }
            for (int i = 0; i < lugares.Count; i++)
            {
                ILugar lugar = lugares[i];
                if (lugar != null && lugar.Id == objetivo.Lugar)
                {
                    posicion = lugar.PosicionEntrada;
                    return true;
                }
            }
            return false;
        }
    }
}
