using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El círculo de búsqueda de la policía (T-665), como en GTA: mientras te buscan, su centro es donde te vieron por última vez.
    /// Para escapar hay que salir de su vista y de este círculo y esperar. La registra Trafico (<c>GestorPersecucion</c>) en
    /// <c>Registro&lt;IZonaBusqueda&gt;</c> y UI la dibuja en el minimapa y en el mapa grande sin ver Trafico.
    /// </summary>
    public interface IZonaBusqueda
    {
        /// <summary>True mientras te buscan (nivel de calor mayor que 0).</summary>
        bool Activa { get; }

        /// <summary>Donde te vieron por última vez (m, mundo; el mapa usa X y Z).</summary>
        Vector3 Centro { get; }

        /// <summary>Radio del círculo (m).</summary>
        float Radio { get; }

        /// <summary>True si ahora mismo alguien de la policía te ve (el círculo se dibuja fijo; si no, parpadea).</summary>
        bool Visto { get; }
    }
}
