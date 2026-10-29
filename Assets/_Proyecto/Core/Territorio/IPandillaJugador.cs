using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Tu pandilla: hasta 3 aliados que te siguen, pelean y suben de nivel (T-405, D-tony-221). La implementa IA (<c>PandillaJugador</c>)
    /// y se agrega a <c>Registro&lt;IPandillaJugador&gt;</c> en <c>OnEnable</c>. UI y la historia la leen sin ver IA.
    /// Los índices van de 0 a <see cref="Cantidad"/> - 1.
    /// </summary>
    public interface IPandillaJugador
    {
        /// <summary>Cuántos aliados tienes.</summary>
        int Cantidad { get; }

        /// <summary>Máximo de aliados (3).</summary>
        int Maximo { get; }

        /// <summary>Los transforms de los aliados que existen ahora en el mundo (puede haber menos que <see cref="Cantidad"/> si uno cayó).</summary>
        IReadOnlyList<Transform> Aliados { get; }

        /// <summary>Nivel del aliado (1 a 3).</summary>
        int Nivel(int indice);

        /// <summary>Nombre del aliado ("EL CHATO").</summary>
        string Nombre(int indice);

        /// <summary>Vida del aliado de 0 a 1 (0 si cayó y espera volver).</summary>
        float Vida(int indice);

        /// <summary>True si te siguen; false si esperan donde están.</summary>
        bool Siguiendo { get; }

        /// <summary>Cambió la lista, un nivel, la vida o la orden.</summary>
        event Action Cambio;

        /// <summary>Un aliado subió de nivel (con su índice).</summary>
        event Action<int> SubioDeNivel;
    }
}
