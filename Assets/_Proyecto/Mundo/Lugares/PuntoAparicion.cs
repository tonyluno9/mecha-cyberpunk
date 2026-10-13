using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>Un lugar donde puede aparecer el jugador. Su pose es la del transform: adelante mira a la calle.</summary>
    public class PuntoAparicion : MonoBehaviour, IPuntoAparicion
    {
        [Tooltip("Identificador único en la escena, por ejemplo Casa_Puerta.")]
        [SerializeField] private string id;
        [SerializeField] private ModoJugador modo = ModoJugador.APie;

        public string Id => id;
        public Pose Pose => new Pose(transform.position, transform.rotation);
        public ModoJugador Modo => modo;

        private void OnEnable()
        {
            Registro<IPuntoAparicion>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IPuntoAparicion>.Quitar(this);
        }
    }
}
