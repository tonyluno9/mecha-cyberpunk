using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>Herramienta de pruebas: suma calor con una tecla (9) y lo borra con otra (0). Solo va en escenas de pruebas.</summary>
    public class CalorDePrueba : MonoBehaviour
    {
        [Tooltip("Cuánto suma la tecla de sumar (puntos).")]
        [Min(0f)] [SerializeField] private float cantidad = 25f;
        [SerializeField] private InputAction sumar = new InputAction("SumarCalor", InputActionType.Button, "<Keyboard>/9");
        [SerializeField] private InputAction borrar = new InputAction("BorrarCalor", InputActionType.Button, "<Keyboard>/0");

        private void OnEnable()
        {
            sumar.performed += AlSumar;
            borrar.performed += AlBorrar;
            sumar.Enable();
            borrar.Enable();
        }

        private void OnDisable()
        {
            sumar.performed -= AlSumar;
            borrar.performed -= AlBorrar;
            sumar.Disable();
            borrar.Disable();
        }

        private void AlSumar(InputAction.CallbackContext contexto) => Registro<ICalor>.Actual?.Sumar(cantidad, MotivoCalor.Otro);

        private void AlBorrar(InputAction.CallbackContext contexto) => Registro<ICalor>.Actual?.Borrar();
    }
}
