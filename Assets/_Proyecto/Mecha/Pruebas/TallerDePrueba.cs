using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Pruebas
{
    /// <summary>
    /// Doble de prueba del taller de Alan (solo para escenas y pruebas de Tony). El estacionamiento es la pose de su transform
    /// (+Z hacia la salida); el punto de interacción está a un lado.
    /// </summary>
    public class TallerDePrueba : MonoBehaviour, ITallerMecha
    {
        [Tooltip("Dónde está el banco de trabajo, relativo al estacionamiento (m).")]
        [SerializeField] private Vector3 desplazamientoInteraccion = new Vector3(3f, 0f, 0f);

        public Pose Estacionamiento => new Pose(transform.position, transform.rotation);
        public Vector3 PuntoInteraccion => transform.TransformPoint(desplazamientoInteraccion);

        private void OnEnable()
        {
            Registro<ITallerMecha>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ITallerMecha>.Quitar(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 2f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
        }
    }
}
