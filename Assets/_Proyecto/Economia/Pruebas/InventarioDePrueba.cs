using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Economia.Pruebas
{
    /// <summary>Herramienta de pruebas: la tecla 9 agrega un Brazo de chatarra y la 0 un kit de reparación. Solo va en escenas de pruebas.</summary>
    public class InventarioDePrueba : MonoBehaviour
    {
        [SerializeField] private DatosObjeto brazo;
        [SerializeField] private DatosObjeto kit;
        [SerializeField] private InputAction agregarBrazo = new InputAction("AgregarBrazo", InputActionType.Button, "<Keyboard>/9");
        [SerializeField] private InputAction agregarKit = new InputAction("AgregarKit", InputActionType.Button, "<Keyboard>/0");

        private void OnEnable()
        {
            agregarBrazo.performed += AlAgregarBrazo;
            agregarKit.performed += AlAgregarKit;
            agregarBrazo.Enable();
            agregarKit.Enable();
        }

        private void OnDisable()
        {
            agregarBrazo.performed -= AlAgregarBrazo;
            agregarKit.performed -= AlAgregarKit;
            agregarBrazo.Disable();
            agregarKit.Disable();
        }

        private void AlAgregarBrazo(InputAction.CallbackContext contexto) => Registro<ServicioInventario>.Actual?.Agregar(brazo, 1);

        private void AlAgregarKit(InputAction.CallbackContext contexto) => Registro<ServicioInventario>.Actual?.Agregar(kit, 1);
    }
}
