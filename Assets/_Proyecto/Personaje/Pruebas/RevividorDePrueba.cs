using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>Herramienta de prueba: con una tecla (K) restaura la vida del jugador a pie. Solo para escenas de prueba.</summary>
    public class RevividorDePrueba : MonoBehaviour
    {
        [SerializeField] private VidaAPie jugador;
        [Tooltip("Acción que revive al jugador (tecla K).")]
        [SerializeField] private InputAction revivir;

        private void OnEnable()
        {
            revivir.performed += AlPulsar;
            revivir.Enable();
        }

        private void OnDisable()
        {
            revivir.performed -= AlPulsar;
            revivir.Disable();
        }

        /// <summary>Para el creador de la escena y las pruebas: configura sin pasar por el inspector.</summary>
        public void Configurar(VidaAPie nuevoJugador, InputAction accion)
        {
            jugador = nuevoJugador;
            revivir = accion;
        }

        private void AlPulsar(InputAction.CallbackContext contexto)
        {
            if (jugador == null)
            {
                Debug.LogError("[Personaje] RevividorDePrueba sin jugador", this);
                return;
            }
            jugador.Restaurar();
        }
    }
}
