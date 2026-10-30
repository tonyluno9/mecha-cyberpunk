using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Economia.Pruebas
{
    /// <summary>Herramienta de pruebas: abona dinero con una tecla (7) y carga deuda con otra (8). Solo va en escenas de pruebas.</summary>
    public class DineroDePrueba : MonoBehaviour
    {
        [Tooltip("Cuánto abona o carga cada tecla (Ƀ).")]
        [Min(1)] [SerializeField] private int cantidad = 500;
        [SerializeField] private InputAction abonar = new InputAction("AbonarDinero", InputActionType.Button, "<Keyboard>/7");
        [SerializeField] private InputAction cargar = new InputAction("CargarDeuda", InputActionType.Button, "<Keyboard>/8");

        private void OnEnable()
        {
            abonar.performed += AlAbonar;
            cargar.performed += AlCargar;
            abonar.Enable();
            cargar.Enable();
        }

        private void OnDisable()
        {
            abonar.performed -= AlAbonar;
            cargar.performed -= AlCargar;
            abonar.Disable();
            cargar.Disable();
        }

        private void AlAbonar(InputAction.CallbackContext contexto) => Registro<ICartera>.Actual?.Abonar(cantidad, MotivoDinero.Otro);

        private void AlCargar(InputAction.CallbackContext contexto) => Registro<ICartera>.Actual?.CargarDeuda(cantidad, MotivoDinero.Otro);
    }
}
