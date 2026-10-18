using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>Herramienta de pruebas: con una tecla (L) registra la victoria del siguiente rival sin pelear. Solo va en escenas de pruebas.</summary>
    public class LigasDePrueba : MonoBehaviour
    {
        [SerializeField] private InputAction vencerSiguiente = new InputAction("VencerSiguienteRival", InputActionType.Button, "<Keyboard>/l");

        private void OnEnable()
        {
            vencerSiguiente.performed += AlVencer;
            vencerSiguiente.Enable();
        }

        private void OnDisable()
        {
            vencerSiguiente.performed -= AlVencer;
            vencerSiguiente.Disable();
        }

        private void AlVencer(InputAction.CallbackContext contexto)
        {
            ServicioLigas ligas = Registro<ServicioLigas>.Actual;
            if (ligas == null || ligas.Siguiente == 0) return;
            ligas.RegistrarVictoria(ligas.LigaActual, ligas.Siguiente);
        }
    }
}
