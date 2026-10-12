using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Pruebas
{
    /// <summary>
    /// Doble de prueba de un punto de llamado de Alan (solo para escenas y pruebas de Tony). La llegada es la pose de su
    /// transform (+Z hacia donde mira el mecha al llegar). Dibuja el radio como gizmo.
    /// </summary>
    public class PuntoLlamadoDePrueba : MonoBehaviour, IPuntoLlamadoMecha
    {
        [Tooltip("Distancia horizontal desde la llegada dentro de la cual se puede llamar (m).")]
        [Min(0.1f)] [SerializeField] private float radio = 30f;
        [SerializeField] private bool habilitado = true;

        public Pose Llegada => new Pose(transform.position, transform.rotation);
        public float Radio => radio;
        public bool Habilitado => habilitado;

        private void OnEnable()
        {
            Registro<IPuntoLlamadoMecha>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IPuntoLlamadoMecha>.Quitar(this);
        }

        /// <summary>Define el punto justo después de instanciarlo.</summary>
        public void Configurar(float nuevoRadio, bool nuevoHabilitado)
        {
            radio = nuevoRadio;
            habilitado = nuevoHabilitado;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = habilitado ? Color.cyan : Color.gray;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1f, 0.001f, 1f));
            Gizmos.DrawWireSphere(Vector3.zero, radio);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
