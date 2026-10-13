using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Arena.Jaula
{
    /// <summary>
    /// La jaula del clandestino como arena de pelea (D-027). Va en la raíz del prefab de la jaula y se registra en
    /// <c>Registro&lt;IArenaPelea&gt;</c> mientras está activa, para que el flujo de pelea la encuentre sin referenciar Arena.
    /// </summary>
    public class ArenaPelea : MonoBehaviour, IArenaPelea
    {
        [SerializeField, Tooltip("Nombre para mostrar de la arena.")]
        private string nombre = "Jaula del Clandestino";

        [SerializeField, Tooltip("Dónde empieza el jugador (sobre el piso, mirando al rival).")]
        private Transform inicioJugador;

        [SerializeField, Tooltip("Dónde empieza el rival (sobre el piso, mirando al jugador).")]
        private Transform inicioRival;

        [SerializeField, Tooltip("Centro de la cara visible de la pantalla; su +Z apunta de la jaula hacia la pantalla.")]
        private Transform pantallaArena;

        [SerializeField, Tooltip("Piso de la jaula; su centro es el Centro de la arena.")]
        private Transform piso;

        [SerializeField, Tooltip("Ancho (x) y alto (y) de la zona útil de la pantalla (m).")]
        private Vector2 tamanoPantalla = new Vector2(8f, 4.5f);

        private const float RadioGizmo = 0.6f;
        private const float LargoFlechaGizmo = 3f;

        public string Nombre => nombre;
        public Pose InicioJugador => new Pose(inicioJugador.position, inicioJugador.rotation);
        public Pose InicioRival => new Pose(inicioRival.position, inicioRival.rotation);
        public Vector3 Centro => piso.position;
        public Transform PantallaArena => pantallaArena;
        public Vector2 TamanoPantalla => tamanoPantalla;

        private void Awake()
        {
            if (inicioJugador == null || inicioRival == null || pantallaArena == null || piso == null)
            {
                Debug.LogError("[Arena] Faltan referencias en " + name + " (InicioJugador, InicioRival, PantallaArena o Piso).", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<IArenaPelea>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IArenaPelea>.Quitar(this);
        }

        private void OnDrawGizmos()
        {
            DibujarInicio(inicioJugador, Color.cyan);
            DibujarInicio(inicioRival, Color.red);
            if (pantallaArena != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(pantallaArena.position, RadioGizmo);
                Gizmos.DrawRay(pantallaArena.position, pantallaArena.forward * LargoFlechaGizmo);
            }
        }

        private static void DibujarInicio(Transform inicio, Color color)
        {
            if (inicio == null)
            {
                return;
            }
            Gizmos.color = color;
            Gizmos.DrawWireSphere(inicio.position + Vector3.up * RadioGizmo, RadioGizmo);
            Gizmos.DrawRay(inicio.position + Vector3.up * RadioGizmo, inicio.forward * LargoFlechaGizmo);
        }
    }
}
