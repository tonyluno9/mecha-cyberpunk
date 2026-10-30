using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Algo que sale en el minimapa y en el mapa grande (T-409): un enemigo, un jefe o un lugar fijo (El Mercado, I+D+I, el CIC, la casa).
    /// Se agrega a <c>Registro&lt;IMarcaMapa&gt;</c> en <c>OnEnable</c> y se quita en <c>OnDisable</c>. IA registra a los pandilleros vivos
    /// y UI a los lugares; UI los dibuja sin ver IA.
    /// </summary>
    public interface IMarcaMapa
    {
        TipoMarcaMapa Tipo { get; }

        /// <summary>Dónde está en el mundo (m; el mapa usa X y Z).</summary>
        Vector3 Posicion { get; }

        /// <summary>False mientras no debe dibujarse (por ejemplo, un pandillero en KO).</summary>
        bool Visible { get; }
    }
}
