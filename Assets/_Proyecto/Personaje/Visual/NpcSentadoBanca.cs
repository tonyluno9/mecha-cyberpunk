using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1516: un civil sentado en una banca (PZ01_Banca u otra): juega NPC_Sentado_Banca en bucle y queda alineado al asiento. Va en la raíz del visual
    /// (con su Animator). El clip pone el asiento a <see cref="AlturaAsientoClip"/> sobre el origen del personaje; la banca PZ01 tiene el asiento a
    /// <see cref="AlturaAsientoBanca"/> (medido sobre su malla) y su frente mira a -Z, así que el personaje baja la diferencia y mira a -forward de la banca.
    /// </summary>
    public class NpcSentadoBanca : MonoBehaviour
    {
        /// <summary>Altura del asiento del clip NPC_Sentado_Banca sobre el origen del personaje (m, bitácora de Blender).</summary>
        public const float AlturaAsientoClip = 0.46f;
        /// <summary>Altura del asiento de PZ01_Banca sobre el piso (m): la cara superior más grande de la malla está a 0.22 de 0.511 de alto, por la escala 1.834 del prefab.</summary>
        public const float AlturaAsientoBanca = 0.41f;

        [Tooltip("La banca donde se sienta (su origen está en el piso, al centro; el frente mira a -Z).")]
        [SerializeField] private Transform banca;
        [Tooltip("Cuánto se corre a lo largo de la banca desde su centro (m, + a la derecha de la banca).")]
        [SerializeField] private float desplazamientoLateral;

        private Animator animador;
        private ReproductorClipUnico reproductor;

        public Transform Banca => banca;
        public float DesplazamientoLateral => desplazamientoLateral;

        /// <summary>Para los creadores y las pruebas: se sienta en <paramref name="laBanca"/> y se alinea.</summary>
        public void Sentar(Transform laBanca, float lateral)
        {
            banca = laBanca;
            desplazamientoLateral = lateral;
            Alinear(transform, banca, lateral);
        }

        /// <summary>Posición y giro que deja a <paramref name="npc"/> sentado en <paramref name="laBanca"/>: sin flotar ni hundirse y de cara al frente de la banca.</summary>
        public static void Alinear(Transform npc, Transform laBanca, float lateral)
        {
            Vector3 frente = Vector3.ProjectOnPlane(-laBanca.forward, Vector3.up);
            if (frente.sqrMagnitude < 1e-6f)
            {
                frente = Vector3.forward;
            }
            npc.SetPositionAndRotation(
                laBanca.position + laBanca.right * lateral + Vector3.up * (AlturaAsientoBanca - AlturaAsientoClip),
                Quaternion.LookRotation(frente.normalized, Vector3.up));
        }

        private void Start()
        {
            if (banca != null)
            {
                Alinear(transform, banca, desplazamientoLateral);
            }
            animador = GetComponentInChildren<Animator>(true);
            reproductor = gameObject.GetComponent<ReproductorClipUnico>();
            if (reproductor == null)
            {
                reproductor = gameObject.AddComponent<ReproductorClipUnico>();
            }
        }

        private void Update()
        {
            // El clip se juega cada vez que no esté jugando (al activarse el objeto, por ejemplo).
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (reproductor != null && animador != null && config != null && config.SentadoBanca != null && reproductor.ClipActual != config.SentadoBanca)
            {
                reproductor.Reproducir(animador, config.SentadoBanca, 1f, true);
            }
        }
    }
}
