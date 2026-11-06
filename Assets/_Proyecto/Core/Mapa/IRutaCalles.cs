using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Quien sabe trazar una ruta por las calles del barrio (T-669). Lo implementa Trafico (que tiene el grafo de calles, el mismo de las patrullas)
    /// y lo registra en <c>Registro&lt;IRutaCalles&gt;</c> al arrancar; UI lo usa para la ruta tipo GPS del minimapa (T-632) sin referenciar a Trafico.
    /// Sin ninguno registrado, UI traza una recta.
    /// </summary>
    public interface IRutaCalles
    {
        /// <summary>
        /// Puntos de la ruta (plano XZ, y = 0) de <paramref name="origen"/> a <paramref name="destino"/>: empieza en el origen, termina en el destino
        /// y entre los dos va por las calles. Lista vacía si no hay camino.
        /// </summary>
        List<Vector3> Calcular(Vector3 origen, Vector3 destino);
    }
}
