using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Combate.Lectura
{
    /// <summary>
    /// Lo que la cabina puede leer de cómo se adapta un rival (T-200, D-tony-168, D-tony-199). Quien la implementa se agrega a
    /// <c>Registro&lt;ILecturaAdaptacion&gt;</c> en <c>Awake</c> y se quita en <c>OnDestroy</c> (como <see cref="ILecturaRival"/>).
    /// </summary>
    public interface ILecturaAdaptacion
    {
        /// <summary>Nombre en pantallas.</summary>
        string Nombre { get; }

        /// <summary>Transform raíz del rival.</summary>
        Transform Raiz { get; }

        /// <summary>Nivel actual (0 = no la tiene) de una adaptación.</summary>
        int Nivel(TipoAdaptacion tipo);

        /// <summary>Cuántas adaptaciones tienen nivel 1 o más.</summary>
        int Activas { get; }

        /// <summary>El rival usó una adaptación (la guardia, una finta o una retirada).</summary>
        event Action<TipoAdaptacion> Usada;

        /// <summary>Una vez al cerrar cada pelea que observó, con los cambios de nivel (la lista puede venir vacía).</summary>
        event Action<IReadOnlyList<CambioAdaptacion>> Aprendio;

        /// <summary>Los niveles cambiaron (al preparar una pelea o al cargar la memoria).</summary>
        event Action NivelesCambiados;
    }
}
