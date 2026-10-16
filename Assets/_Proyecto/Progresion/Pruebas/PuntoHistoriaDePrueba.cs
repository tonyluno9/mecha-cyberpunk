using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Doble de prueba de un punto de historia de Alan (solo para escenas y pruebas de Tony). Su pose es la de su transform
    /// y cuenta las llamadas a <see cref="Activar"/>.
    /// </summary>
    public class PuntoHistoriaDePrueba : MonoBehaviour, IPuntoHistoria
    {
        [Tooltip("Identificador del punto, por ejemplo Prologo_Carro.")]
        [SerializeField] private string id = "Prologo_Punto";

        public string Id => id;
        public Pose Pose => new Pose(transform.position, transform.rotation);

        /// <summary>Cuántas veces se llamó a Activar.</summary>
        public int LlamadasActivar { get; private set; }

        /// <summary>El último valor con el que se llamó a Activar (false si nunca se llamó).</summary>
        public bool Activo { get; private set; }

        private void OnEnable()
        {
            Registro<IPuntoHistoria>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IPuntoHistoria>.Quitar(this);
        }

        public void Activar(bool activo)
        {
            LlamadasActivar++;
            Activo = activo;
        }
    }
}
