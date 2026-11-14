using UnityEngine;

namespace MechaCyberpunk.Core.Trepar
{
    /// <summary>
    /// Marca un lugar trepable (escalera de servicio, tubo). Convención: el transform está al pie de la pared y
    /// su <c>forward</c> apunta HACIA la pared (hacia donde mira el jugador al trepar). El jugador a pie lo detecta con su trigger.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PuntoTrepar : MonoBehaviour
    {
        private const float AnchoTrigger = 1f;
        private const float FondoTrigger = 1.2f;
        private const float RadioGizmo = 0.15f;

        [Tooltip("Escalera o tubo: cambia la velocidad de subida.")]
        [SerializeField] private TipoTrepar tipo = TipoTrepar.Escalera;

        [Tooltip("Altura desde el transform hasta el borde de arriba (m).")]
        [Min(0.5f)] [SerializeField] private float alturaSubida = 3f;

        [Tooltip("Dónde quedan los pies del jugador arriba, en espacio local (m). Por defecto (0, alturaSubida, 0.8): sobre el techo.")]
        [SerializeField] private Vector3 salida = new Vector3(0f, 3f, 0.8f);

        [Tooltip("Distancia a la que se cuelga el jugador, delante de la pared (m).")]
        [Min(0f)] [SerializeField] private float separacionPared = 0.4f;

        public TipoTrepar Tipo => tipo;
        public float AlturaSubida => alturaSubida;
        /// <summary>Dónde empieza la subida: delante de la pared, a nivel del suelo.</summary>
        public Vector3 PieAbajo => transform.position - Frente * separacionPared;
        /// <summary>Los pies del jugador al llegar al borde de arriba, todavía delante de la pared.</summary>
        public Vector3 Cima => PieAbajo + Vector3.up * alturaSubida;
        /// <summary>Dónde quedan los pies del jugador arriba, ya sobre el techo.</summary>
        public Vector3 Salida => transform.TransformPoint(salida);
        /// <summary>Hacia la pared, en horizontal.</summary>
        public Vector3 Frente
        {
            get
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0f ? f.normalized : Vector3.forward;
            }
        }

        private void Reset()
        {
            salida = new Vector3(0f, alturaSubida, 0.8f);
            AjustarTrigger();
        }

        private void OnValidate()
        {
            AjustarTrigger();
        }

        /// <summary>Pone el collider como trigger y lo dimensiona. Se llama solo en el editor (Reset/OnValidate); créalo a mano si armas el punto por código.</summary>
        public void AjustarTrigger()
        {
            BoxCollider caja = GetComponent<BoxCollider>();
            if (caja == null)
            {
                return;
            }
            caja.isTrigger = true;
            caja.size = new Vector3(AnchoTrigger, alturaSubida, FondoTrigger);
            caja.center = new Vector3(0f, alturaSubida * 0.5f, -FondoTrigger * 0.5f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = tipo == TipoTrepar.Escalera ? Color.yellow : Color.cyan;
            Gizmos.DrawLine(PieAbajo, Cima);
            Gizmos.DrawLine(Cima, Salida);
            Gizmos.DrawWireSphere(Salida, RadioGizmo);
            Gizmos.DrawLine(transform.position, transform.position + Frente);
        }
    }
}
