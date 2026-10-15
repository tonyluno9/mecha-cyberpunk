using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>
    /// Doble de prueba de un punto de aparición de Alan (solo para escenas y pruebas de Tony). Su pose es la de su transform.
    /// </summary>
    public class PuntoAparicionDePrueba : MonoBehaviour, IPuntoAparicion
    {
        [Tooltip("Identificador del punto, por ejemplo Prueba_Inicio.")]
        [SerializeField] private string id = "Prueba_Inicio";
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

        /// <summary>Define el punto justo después de instanciarlo.</summary>
        public void Configurar(string nuevoId, ModoJugador nuevoModo)
        {
            id = nuevoId;
            modo = nuevoModo;
        }
    }
}
