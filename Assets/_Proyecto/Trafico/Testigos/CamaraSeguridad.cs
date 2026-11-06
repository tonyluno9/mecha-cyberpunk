using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>
    /// Una cámara de seguridad en un poste (T-662). Ve al jugador si está dentro de su cono de visión y no hay pared en medio
    /// (<see cref="CalculoVistaCamara"/> y <see cref="LineaDeVista"/>); entonces cuenta como testigo del delito. Sin colliders.
    /// </summary>
    public class CamaraSeguridad : MonoBehaviour
    {
        private const float AlturaPecho = 1.2f;
        private static readonly int ColorEmision = Shader.PropertyToID("_EmissionColor");
        private static readonly List<CamaraSeguridad> ActivasLista = new List<CamaraSeguridad>();

        [Tooltip("Desde donde mira la cámara (la lente). Si falta se usa el propio objeto.")]
        [SerializeField] private Transform ojo;
        [Tooltip("El foquito que parpadea en rojo cuando ve al jugador.")]
        [SerializeField] private Renderer indicador;
        [Min(1f)] [Tooltip("Alcance de la cámara (m).")]
        [SerializeField] private float alcance = 45f;
        [Range(5f, 90f)] [Tooltip("Medio ángulo del cono de visión (°).")]
        [SerializeField] private float semiAngulo = 40f;
        [Min(0.1f)] [Tooltip("Cada cuánto parpadea el indicador cuando ve al jugador (s).")]
        [SerializeField] private float periodoAviso = 0.25f;
        [Tooltip("Color del foquito mientras vigila (HDR).")]
        [ColorUsage(false, true)] [SerializeField] private Color colorVigila = new Color(0.4f, 2.2f, 0.5f);
        [Tooltip("Color del foquito mientras te ve (HDR).")]
        [ColorUsage(false, true)] [SerializeField] private Color colorViendo = new Color(5f, 0.2f, 0.1f);

        private MaterialPropertyBlock bloque;

        public static IReadOnlyList<CamaraSeguridad> Activas => ActivasLista;

        /// <summary>True mientras ve al jugador (el indicador parpadea en rojo).</summary>
        public bool Viendo { get; private set; }

        public Vector3 PosicionOjo => ojo != null ? ojo.position : transform.position;

        public Vector3 DireccionOjo => ojo != null ? ojo.forward : transform.forward;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => ActivasLista.Clear();

        public void Configurar(float metros, float grados)
        {
            alcance = metros;
            semiAngulo = grados;
        }

        private void OnEnable() => ActivasLista.Add(this);

        private void OnDisable() => ActivasLista.Remove(this);

        /// <summary>True si la cámara ve a quien está parado en <paramref name="punto"/> (el suelo) ahora.</summary>
        public bool Ve(Vector3 punto)
        {
            Vector3 pecho = punto + Vector3.up * AlturaPecho;
            return CalculoVistaCamara.EnCono(PosicionOjo, DireccionOjo, semiAngulo, alcance, pecho) && LineaDeVista.Libre(PosicionOjo, punto);
        }

        /// <summary>Lo llama quien vigila las cámaras: enciende o apaga el parpadeo rojo.</summary>
        public void PonerViendo(bool ve) => Viendo = ve;

        private void Update()
        {
            if (indicador == null)
            {
                return;
            }
            if (bloque == null)
            {
                bloque = new MaterialPropertyBlock();
            }
            bool prendido = !Viendo || Mathf.FloorToInt(Time.time / periodoAviso) % 2 == 0;
            indicador.GetPropertyBlock(bloque);
            bloque.SetColor(ColorEmision, prendido ? (Viendo ? colorViendo : colorVigila) : Color.black);
            indicador.SetPropertyBlock(bloque);
        }
    }
}
