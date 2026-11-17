using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-1315: la tecla que consume el objeto equipado (la bebida, las papitas, la venda...). Es una acción del Input System compartida: el HUD la
    /// pulsa (<see cref="HudConsumibles"/>) y el HUD y el tutorial muestran su tecla real con <see cref="TeclasHud.Texto"/>. Se puede recambiar con
    /// <c>ApplyBindingOverride</c> como cualquier otra acción.
    /// </summary>
    public static class AccionConsumirEquipado
    {
        public const string Nombre = "ConsumirEquipado";
        private const string TeclaInicial = "<Keyboard>/b";

        private static InputAction accion;

        /// <summary>La acción (se crea al pedirla).</summary>
        public static InputAction Accion => accion ?? (accion = new InputAction(Nombre, InputActionType.Button, TeclaInicial));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            accion?.Dispose();
            accion = null;
        }
    }
}
