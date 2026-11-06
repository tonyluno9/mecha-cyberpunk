using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>
    /// T-655: las puertas de un montable. El contrato solo da <see cref="IMontable.PuntoSubida"/> (la puerta del conductor);
    /// la puerta del otro lado es su espejo respecto del plano central del carro (el padre del asiento).
    /// </summary>
    public static class PuertasMontable
    {
        /// <summary>Espeja un punto del mundo respecto del plano x = 0 del marco dado.</summary>
        public static Vector3 Espejar(Vector3 punto, Transform marco)
        {
            Vector3 local = marco.InverseTransformPoint(punto);
            local.x = -local.x;
            return marco.TransformPoint(local);
        }

        /// <summary>La puerta del lado opuesto de un montable de carro. False si no es un carro o no tiene asiento con padre.</summary>
        public static bool TryEspejo(IMontable montable, out Vector3 puerta)
        {
            puerta = default;
            if (montable.Modo != ModoJugador.Carro || montable.Asiento == null || montable.Asiento.parent == null)
            {
                return false;
            }
            puerta = Espejar(montable.PuntoSubida, montable.Asiento.parent);
            return true;
        }
    }
}
