using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1492 / T-1528: marca al dron que te dispara en el indicador direccional de daño del HUD (el mismo de las balas), no con un cuadro propio:
    /// mientras el dron avisa y poco después de disparar pide una marca hacia él, pero SOLO si el jugador trae puesto el visor (<see cref="VisorJugador.Puesto"/>).
    /// Sin visor no hay marcador de dron (el indicador normal al recibir un tiro sigue igual). Lo agrega <see cref="DronMundo"/> al activarse.
    /// </summary>
    public class MarcadorDronHud : MonoBehaviour
    {
        /// <summary>Cada cuántos segundos se refresca la marca mientras el dron amenaza (la marca dura 1 s).</summary>
        public const float CadaCuanto = 0.45f;

        private DronMundo dron;
        private float siguiente;

        /// <summary>True si el dron se está marcando en el indicador (amenaza y el visor está puesto).</summary>
        public bool Visible { get; private set; }

        /// <summary>Cuántas marcas se han pedido al indicador de daño.</summary>
        public int MarcasPedidas { get; private set; }

        private void Awake() => dron = GetComponent<DronMundo>();

        private void Update()
        {
            Visible = dron != null && dron.Amenazando && VisorJugador.Puesto;
            if (!Visible || Time.time < siguiente)
            {
                return;
            }
            siguiente = Time.time + CadaCuanto;
            MarcasPedidas++;
            EventosIndicadorDanio.MarcarFuente(transform.position);
        }
    }
}
