using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-1315 / T-1492: las teclas de los consumibles. Hay dos ranuras: la primera se usa con E y la segunda con C. Para cambiar qué objeto hay en una
    /// ranura se mantiene Alt y se pulsa su tecla (Alt + E, Alt + C): pasa al siguiente objeto que lleva. Son acciones del Input System compartidas: el HUD
    /// las pulsa (<see cref="HudConsumibles"/>) y el HUD y el tutorial muestran su tecla real con <see cref="TeclasHud.Texto"/>. Se pueden recambiar con
    /// <c>ApplyBindingOverride</c> como cualquier otra acción.
    /// Choques de teclas: E es también "interactuar", así que la ranura 1 solo se usa si no hay nada con qué interactuar enfrente. C no choca a pie
    /// (agacharse es Ctrl). Dentro de un carro E baja del carro y C cambia la cámara, por eso ahí las ranuras se usan con Z y X.
    /// </summary>
    public static class AccionConsumirEquipado
    {
        public const string Nombre = "ConsumirEquipado";
        public const string NombreRanura2 = "ConsumirEquipado2";
        public const string NombreCambiar = "CambiarConsumible";
        public const string NombreCarro1 = "ConsumirEnCarro1";
        public const string NombreCarro2 = "ConsumirEnCarro2";
        private const string TeclaRanura1 = "<Keyboard>/e";
        private const string TeclaRanura2 = "<Keyboard>/c";
        private const string TeclaCambiar = "<Keyboard>/leftAlt";
        private const string TeclaCarro1 = "<Keyboard>/z";
        private const string TeclaCarro2 = "<Keyboard>/x";

        private static InputAction accion;
        private static InputAction accion2;
        private static InputAction cambiar;
        private static InputAction carro1;
        private static InputAction carro2;

        /// <summary>La acción de la ranura 1 (E).</summary>
        public static InputAction Accion => accion ?? (accion = new InputAction(Nombre, InputActionType.Button, TeclaRanura1));

        /// <summary>La acción de la ranura 2 (C).</summary>
        public static InputAction Accion2 => accion2 ?? (accion2 = new InputAction(NombreRanura2, InputActionType.Button, TeclaRanura2));

        /// <summary>El modificador que, con la tecla de una ranura, cambia su objeto (Alt).</summary>
        public static InputAction Cambiar => cambiar ?? (cambiar = new InputAction(NombreCambiar, InputActionType.Button, TeclaCambiar));

        /// <summary>La ranura 1 dentro de un carro (Z).</summary>
        public static InputAction EnCarro1 => carro1 ?? (carro1 = new InputAction(NombreCarro1, InputActionType.Button, TeclaCarro1));

        /// <summary>La ranura 2 dentro de un carro (X).</summary>
        public static InputAction EnCarro2 => carro2 ?? (carro2 = new InputAction(NombreCarro2, InputActionType.Button, TeclaCarro2));

        /// <summary>La acción de uso a pie de la ranura (0 = E, 1 = C).</summary>
        public static InputAction De(int ranura) => ranura == 0 ? Accion : Accion2;

        /// <summary>La acción de uso en carro de la ranura (0 = Z, 1 = X).</summary>
        public static InputAction DeCarro(int ranura) => ranura == 0 ? EnCarro1 : EnCarro2;

        /// <summary>Activa todas las acciones.</summary>
        public static void Habilitar()
        {
            Accion.Enable();
            Accion2.Enable();
            Cambiar.Enable();
            EnCarro1.Enable();
            EnCarro2.Enable();
        }

        /// <summary>Apaga todas las acciones.</summary>
        public static void Deshabilitar()
        {
            Accion.Disable();
            Accion2.Disable();
            Cambiar.Disable();
            EnCarro1.Disable();
            EnCarro2.Disable();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            accion?.Dispose();
            accion2?.Dispose();
            cambiar?.Dispose();
            carro1?.Dispose();
            carro2?.Dispose();
            accion = accion2 = cambiar = carro1 = carro2 = null;
        }
    }
}
