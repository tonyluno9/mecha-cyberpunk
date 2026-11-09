using UnityEngine;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>T-933: alguien de la policía que puede ver un delito (una patrulla con sus policías). Se registra en <c>Registro&lt;ITestigoPolicia&gt;</c>.</summary>
    public interface ITestigoPolicia
    {
        /// <summary>True si ve a quien está en <paramref name="punto"/> (cerca y sin pared en medio).</summary>
        bool VeElPunto(Vector3 punto);
    }
}
