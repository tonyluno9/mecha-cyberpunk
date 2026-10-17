using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// Zona desde la que el jugador puede llamar a su mecha (T-088). La pose de llegada es la del transform: +Z hacia donde
    /// mira el mecha al aterrizar. Necesita cielo abierto: un círculo libre de 3 m de radio y 6 m de alto.
    /// </summary>
    public class PuntoLlamadoMundo : MonoBehaviour, IPuntoLlamadoMecha
    {
        [Tooltip("Distancia horizontal (m) desde la llegada dentro de la cual el jugador puede llamar.")]
        [Min(0.1f)]
        [SerializeField] private float radio = 14f;
        [SerializeField] private bool habilitado = true;

        public Pose Llegada => new Pose(transform.position, transform.rotation);
        public float Radio => radio;
        public bool Habilitado => habilitado;

        public void Habilitar(bool valor)
        {
            habilitado = valor;
        }

        private void OnEnable()
        {
            Registro<IPuntoLlamadoMecha>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IPuntoLlamadoMecha>.Quitar(this);
        }
    }
}
