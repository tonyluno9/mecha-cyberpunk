using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Herramienta de pruebas (T-192): con una tecla instala los 4 implantes de nivel 1 sin cobrar (3), con otra los de nivel 2 (4)
    /// y con otra los quita (5). Usa <c>ServicioImplantes.Cargar</c>, así que no cobra ni pide liga. Solo va en escenas de Tony.
    /// Teclas libres en las escenas de prueba: 3, 4 y 5 (1 y 2 son las armas a pie, 7 y 8 el dinero).
    /// </summary>
    public class ImplantesDePrueba : MonoBehaviour
    {
        private const string Nivel1 = "{\"ids\":[\"implante_ojos_1\",\"implante_brazos_1\",\"implante_piernas_1\",\"implante_sistemanervioso_1\"]}";
        private const string Nivel2 = "{\"ids\":[\"implante_ojos_2\",\"implante_brazos_2\",\"implante_piernas_2\",\"implante_sistemanervioso_2\"]}";

        [SerializeField] private InputAction instalarNivel1 = new InputAction("InstalarImplantes1", InputActionType.Button, "<Keyboard>/3");
        [SerializeField] private InputAction instalarNivel2 = new InputAction("InstalarImplantes2", InputActionType.Button, "<Keyboard>/4");
        [SerializeField] private InputAction quitar = new InputAction("QuitarImplantes", InputActionType.Button, "<Keyboard>/5");

        private void OnEnable()
        {
            instalarNivel1.performed += AlInstalar1;
            instalarNivel2.performed += AlInstalar2;
            quitar.performed += AlQuitar;
            instalarNivel1.Enable();
            instalarNivel2.Enable();
            quitar.Enable();
        }

        private void OnDisable()
        {
            instalarNivel1.performed -= AlInstalar1;
            instalarNivel2.performed -= AlInstalar2;
            quitar.performed -= AlQuitar;
            instalarNivel1.Disable();
            instalarNivel2.Disable();
            quitar.Disable();
        }

        private void AlInstalar1(InputAction.CallbackContext contexto) => Registro<ServicioImplantes>.Actual?.Cargar(Nivel1);

        private void AlInstalar2(InputAction.CallbackContext contexto) => Registro<ServicioImplantes>.Actual?.Cargar(Nivel2);

        private void AlQuitar(InputAction.CallbackContext contexto) => Registro<ServicioImplantes>.Actual?.QuitarTodos();
    }
}
