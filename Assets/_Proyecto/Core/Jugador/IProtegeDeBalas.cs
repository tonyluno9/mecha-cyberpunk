using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Quien puede estar detrás de una cubierta (T-421). Las armas lo consultan antes de aplicar el daño de una bala.</summary>
    public interface IProtegeDeBalas
    {
        /// <summary>True si una bala disparada desde <paramref name="origen"/> pega en la cubierta; <paramref name="puntoCubierta"/> es donde pega.</summary>
        bool ProtegeDe(Vector3 origen, out Vector3 puntoCubierta);
    }
}
