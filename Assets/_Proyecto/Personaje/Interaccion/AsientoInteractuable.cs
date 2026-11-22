using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>
    /// T-1628: un asiento (banca o silla) donde el jugador a pie se sienta con E (PJ01_Sentado) y se levanta con E o al moverse (<see cref="SentadoAPie"/>).
    /// También puede tener un NPC sentado (<see cref="SentarNpc"/>, con NPC_Sentado_Banca): el asiento queda ocupado y E no se ofrece.
    /// El origen del objeto va al piso; el asiento queda a <see cref="AlturaAsiento"/> sobre él y el frente es -forward (como PZ01_Banca) o +forward.
    /// </summary>
    public class AsientoInteractuable : MonoBehaviour, IInteractuable
    {
        [Tooltip("Altura de la superficie del asiento sobre el origen del objeto (m). PZ01_Banca: 0.41.")]
        [Min(0.05f)] [SerializeField] private float alturaAsiento = NpcSentadoBanca.AlturaAsientoBanca;
        [Tooltip("Altura del asiento del clip de sentado sobre el origen del personaje (m). Bájalo si el jugador queda hundido, súbelo si flota.")]
        [Min(0f)] [SerializeField] private float alturaAsientoClip = NpcSentadoBanca.AlturaAsientoClip;
        [Tooltip("Corrimiento a lo largo del asiento desde el origen (m, + a la derecha).")]
        [SerializeField] private float desplazamientoLateral;
        [Tooltip("True: el frente es -forward del objeto (PZ01_Banca). False: es +forward.")]
        [SerializeField] private bool frenteMenosZ = true;
        [Tooltip("Cuánto se adelanta el jugador al levantarse (m, hacia el frente).")]
        [Min(0.1f)] [SerializeField] private float distanciaSalida = 0.8f;
        [Tooltip("Visual de un NPC que ya empieza sentado aquí (opcional).")]
        [SerializeField] private GameObject npcInicial;
        [Tooltip("Segundos sin poder volver a sentarse tras levantarse (s).")]
        [Min(0f)] [SerializeField] private float segundosEnfriamiento = 0.6f;

        private bool registrado;
        private float libreDesde;

        /// <summary>Quien está sentado aquí: el jugador (<see cref="SentadoAPie"/>) o un NPC; null si está libre.</summary>
        public GameObject Ocupante { get; private set; }

        public float AlturaAsiento => alturaAsiento;
        public bool EstaOcupado => Ocupante != null;

        public Vector3 Posicion => transform.position + transform.right * desplazamientoLateral;
        public string Texto => "Sentarse";
        public bool PuedeUsar => Ocupante == null && Time.time >= libreDesde && Registro<IJugador>.Actual != null;

        /// <summary>Hacia dónde mira quien se sienta (horizontal).</summary>
        public Vector3 Frente
        {
            get
            {
                Vector3 f = Vector3.ProjectOnPlane(frenteMenosZ ? -transform.forward : transform.forward, Vector3.up);
                return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
            }
        }

        /// <summary>Dónde queda el origen del personaje sentado (asiento menos la altura del asiento del clip).</summary>
        public Vector3 PosicionSentado => Posicion + Vector3.up * (alturaAsiento - alturaAsientoClip);

        /// <summary>Dónde se para al levantarse: delante del asiento, en el piso.</summary>
        public Vector3 PosicionSalida => Posicion + Frente * distanciaSalida;

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
        }

        private void Start()
        {
            if (npcInicial != null)
            {
                SentarNpc(npcInicial);
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            SentadoAPie.Sentar(this);
        }

        /// <summary>Sienta a un NPC (la raíz de su visual) en este asiento con NPC_Sentado_Banca. False si está ocupado.</summary>
        public bool SentarNpc(GameObject visualNpc)
        {
            if (visualNpc == null || (Ocupante != null && Ocupante != visualNpc))
            {
                return false;
            }
            NpcSentadoBanca sentado = visualNpc.GetComponent<NpcSentadoBanca>();
            if (sentado == null)
            {
                sentado = visualNpc.AddComponent<NpcSentadoBanca>();
            }
            // NpcSentadoBanca alinea con el forward del objeto; si este asiento mira a +forward se gira después.
            sentado.Sentar(transform, desplazamientoLateral);
            if (!frenteMenosZ)
            {
                visualNpc.transform.rotation = Quaternion.LookRotation(Frente, Vector3.up);
            }
            Ocupante = visualNpc;
            Debug.Log("[Personaje] NPC sentado en " + name, this);
            return true;
        }

        /// <summary>Lo llama quien se sienta.</summary>
        public void Ocupar(GameObject quien)
        {
            Ocupante = quien;
        }

        /// <summary>Lo llama quien se levanta.</summary>
        public void Liberar()
        {
            Ocupante = null;
            libreDesde = Time.time + segundosEnfriamiento;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Posicion + Vector3.up * alturaAsiento, 0.12f);
            Gizmos.DrawLine(Posicion + Vector3.up * alturaAsiento, Posicion + Vector3.up * alturaAsiento + Frente * 0.4f);
        }
    }
}
