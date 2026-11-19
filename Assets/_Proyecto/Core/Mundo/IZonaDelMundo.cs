using UnityEngine;

namespace MechaCyberpunk.Core.Mundo
{
    /// <summary>
    /// T-1409: una zona de la ciudad con nombre (Malecon, Puerto, Cerro, Autopista...) que sabe si un punto cae dentro.
    /// Las zonas se registran en <see cref="Registro{T}"/> (en OnEnable) y <see cref="ZonasMundo"/> responde "¿en qué zona está este punto?"
    /// sin que Personaje, Trafico y compañía se referencien entre sí.
    /// </summary>
    public interface IZonaDelMundo
    {
        /// <summary>Nombre de la zona (el mismo que usan ConfigCarrosZonas y ConfigPeatonesZona).</summary>
        string NombreZona { get; }

        /// <summary>Si el punto del mundo cae dentro de la zona.</summary>
        bool ContienePunto(Vector3 mundo);
    }
}
