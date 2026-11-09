using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>T-933: alguien en la calle que la policía puede sorprender atacando (rateros, pandilleros). Se registra en <c>Registro&lt;ICriminalCalle&gt;</c>.</summary>
    public interface ICriminalCalle
    {
        Vector3 Posicion { get; }

        /// <summary>True mientras está atacando a alguien.</summary>
        bool EstaAtacando { get; }
    }
}
