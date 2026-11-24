using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// Botones de los controles Meta Quest Touch (OpenXR) y del simulador de XR, por uso ({LeftHand}/{RightHand}).
    /// Mapa: stick izq = caminar (clic = correr), stick der = giro de 45°, X = saltar, B = interactuar (E),
    /// gatillo der = disparar, A = recargar, grip = agarrar, Y = rueda (Tab), Menú izq = pausa.
    /// Las acciones se encienden solas mientras <see cref="Core.ModoVR.Activo"/>.
    /// </summary>
    public static class EntradaVR
    {
        public const string Izq = "<XRController>{LeftHand}";
        public const string Der = "<XRController>{RightHand}";

        public static InputAction StickIzquierdo { get; private set; }
        public static InputAction StickDerecho { get; private set; }
        public static InputAction ClicStickIzquierdo { get; private set; }
        public static InputAction GatilloDerecho { get; private set; }
        public static InputAction GatilloIzquierdo { get; private set; }
        /// <summary>Valor 0..1 del gatillo derecho.</summary>
        public static InputAction ValorGatilloDerecho { get; private set; }
        public static InputAction GripDerecho { get; private set; }
        public static InputAction GripIzquierdo { get; private set; }
        /// <summary>A (derecho, primario).</summary>
        public static InputAction BotonA { get; private set; }
        /// <summary>B (derecho, secundario).</summary>
        public static InputAction BotonB { get; private set; }
        /// <summary>X (izquierdo, primario).</summary>
        public static InputAction BotonX { get; private set; }
        /// <summary>Y (izquierdo, secundario).</summary>
        public static InputAction BotonY { get; private set; }
        /// <summary>Botón Menú del control izquierdo.</summary>
        public static InputAction Menu { get; private set; }

        private static bool creadas;

        private static InputAction Boton(string nombre, params string[] rutas)
        {
            var a = new InputAction(nombre, InputActionType.Button);
            foreach (string r in rutas)
            {
                a.AddBinding(r);
            }
            return a;
        }

        private static InputAction Valor(string nombre, string ruta)
        {
            var a = new InputAction(nombre, InputActionType.Value, ruta);
            return a;
        }

        /// <summary>Crea y enciende las acciones (no hace nada si ya están).</summary>
        public static void Encender()
        {
            if (!creadas)
            {
                creadas = true;
                StickIzquierdo = Valor("VR_StickIzq", Izq + "/{Primary2DAxis}");
                StickDerecho = Valor("VR_StickDer", Der + "/{Primary2DAxis}");
                ClicStickIzquierdo = Boton("VR_ClicStickIzq", Izq + "/{Primary2DAxisClick}");
                GatilloDerecho = Boton("VR_GatilloDer", Der + "/{TriggerButton}");
                GatilloIzquierdo = Boton("VR_GatilloIzq", Izq + "/{TriggerButton}");
                ValorGatilloDerecho = Valor("VR_ValorGatilloDer", Der + "/{Trigger}");
                GripDerecho = Boton("VR_GripDer", Der + "/{GripButton}");
                GripIzquierdo = Boton("VR_GripIzq", Izq + "/{GripButton}");
                BotonA = Boton("VR_A", Der + "/{PrimaryButton}");
                BotonB = Boton("VR_B", Der + "/{SecondaryButton}");
                BotonX = Boton("VR_X", Izq + "/{PrimaryButton}");
                BotonY = Boton("VR_Y", Izq + "/{SecondaryButton}");
                Menu = Boton("VR_Menu", Izq + "/{MenuButton}");
            }
            foreach (InputAction a in Todas())
            {
                a.Enable();
            }
        }

        public static void Apagar()
        {
            if (!creadas)
            {
                return;
            }
            foreach (InputAction a in Todas())
            {
                a.Disable();
            }
        }

        private static InputAction[] Todas() => new[]
        {
            StickIzquierdo, StickDerecho, ClicStickIzquierdo, GatilloDerecho, GatilloIzquierdo, ValorGatilloDerecho,
            GripDerecho, GripIzquierdo, BotonA, BotonB, BotonX, BotonY, Menu,
        };

        /// <summary>True si la acción se pulsó este cuadro (false si no existe o VR está apagado).</summary>
        public static bool Pulso(InputAction a) => a != null && a.enabled && a.WasPressedThisFrame();

        /// <summary>True si la acción se soltó este cuadro.</summary>
        public static bool Solto(InputAction a) => a != null && a.enabled && a.WasReleasedThisFrame();

        /// <summary>True mientras se mantiene.</summary>
        public static bool Sostenido(InputAction a) => a != null && a.enabled && a.IsPressed();

        public static Vector2 Eje(InputAction a) => a != null && a.enabled ? a.ReadValue<Vector2>() : Vector2.zero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Apagar();
        }
    }
}
