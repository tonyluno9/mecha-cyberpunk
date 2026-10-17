using MechaCyberpunk.Progresion.Guardado;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>Herramienta de pruebas: F5 guarda la partida y F9 la carga con el ServicioGuardado de la escena. Solo va en escenas de pruebas.</summary>
    public class GuardadoDePrueba : MonoBehaviour
    {
        [SerializeField] private InputAction guardar = new InputAction("GuardarPartida", InputActionType.Button, "<Keyboard>/f5");
        [SerializeField] private InputAction cargar = new InputAction("CargarPartida", InputActionType.Button, "<Keyboard>/f9");

        private void OnEnable()
        {
            guardar.performed += AlGuardar;
            cargar.performed += AlCargar;
            guardar.Enable();
            cargar.Enable();
        }

        private void OnDisable()
        {
            guardar.performed -= AlGuardar;
            cargar.performed -= AlCargar;
            guardar.Disable();
            cargar.Disable();
        }

        private void AlGuardar(InputAction.CallbackContext contexto)
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio == null)
            {
                Debug.LogWarning("[Progresion] No hay ServicioGuardado en la escena.");
                return;
            }
            Debug.Log("[Progresion] F5 guardar: " + (servicio.Guardar() ? "guardada" : "falló"));
        }

        private void AlCargar(InputAction.CallbackContext contexto)
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio == null)
            {
                Debug.LogWarning("[Progresion] No hay ServicioGuardado en la escena.");
                return;
            }
            Debug.Log("[Progresion] F9 cargar: " + (servicio.Cargar() ? "cargada" : "falló"));
        }
    }
}
