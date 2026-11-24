using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// Botones de los controles Meta Quest Touch (OpenXR) y del simulador de XR, por uso ({LeftHand}/{RightHand}).
    /// Reparto VR27 (23-nov, final):
    /// <list type="bullet">
    /// <item>Caminar / correr: stick izquierdo / clic del stick izquierdo.</item>
    /// <item>Girar: stick derecho (SUAVE por defecto, VR18; POR PASOS en Opciones).</item>
    /// <item>Saltar: clic del stick derecho.</item>
    /// <item>Disparar / clic en menús: gatillo derecho.</item>
    /// <item>Sacar / guardar arma: gesto con la mano derecha en la cadera (corta) o sobre el hombro (larga) + grip derecho (VR/ArmasGesto).</item>
    /// <item>Recargar: A suelta el cargador; la mano izquierda en la cadera izquierda + grip toma uno nuevo y se mete en el arma (VR/Recarga).</item>
    /// <item>Interactuar (la E del teclado): B (derecho).</item>
    /// <item>Rueda de armas: mantener X (izquierdo); VR46: el selector se mueve solo con el grip izquierdo apretado (mano izquierda) y al soltar el grip
    /// se elige (arma, granada o consumible). El stick izquierdo ya no la mueve (no camina con la rueda abierta).</item>
    /// <item>VR51: gatillo izquierdo = apuntar: mira holográfica (subfusil, fusil, escopeta), visor con zoom en el stick derecho vertical (francotirador),
    /// arco de caída (lanzagranadas), cargar (plasma; el derecho suelta el rayo). Pistola y revólver sin mira.</item>
    /// <item>VR52: sin recarga automática (salvo la granada): vacío, el gatillo hace clic y sale "A: RECARGAR".</item>
    /// <item>Pausa / opciones: Y (izquierdo) o Menú.</item>
    /// <item>Prótesis: solo gestos con los dos grips (VR/Guia): manos de la cadera a la rodilla = piernas, brazos en cruz = brazos, manos a los lados de la cabeza = visión. Sin rueda de prótesis en VR.</item>
    /// <item>Agarrar / lanzar (implante): grip con la mano libre cerca del enemigo.</item>
    /// <item>Recentrar: mantener el stick derecho abajo 1 s.</item>
    /// </list>
    /// El stick derecho ya no cambia de arma. Las acciones se encienden solas mientras <see cref="Core.ModoVR.Activo"/>.
    /// </summary>
    public static class EntradaVR
    {
        public const string Izq = "<XRController>{LeftHand}";
        public const string Der = "<XRController>{RightHand}";

        /// <summary>VR27: Saltar = clic del stick derecho.</summary>
        public const string RutaSaltar = Der + "/{Primary2DAxisClick}";
        /// <summary>VR19: Interactuar = B (secundario derecho).</summary>
        public const string RutaInteractuar = Der + "/{SecondaryButton}";
        /// <summary>VR27: soltar el cargador = A (primario derecho). Ya no es un binding de la acción Recargar (lo lee <c>RecargaManualVR</c>).</summary>
        public const string RutaSoltarCargador = Der + "/{PrimaryButton}";
        /// <summary>VR27: ya no es binding de nada (antes X = Recargar); queda por compatibilidad = <see cref="RutaSoltarCargador"/>.</summary>
        public const string RutaRecargar = RutaSoltarCargador;
        /// <summary>VR27: Rueda de armas = mantener X (primario izquierdo).</summary>
        public const string RutaRueda = Izq + "/{PrimaryButton}";
        /// <summary>VR27: Pausa = Y (secundario izquierdo), además del botón Menú.</summary>
        public const string RutaPausaY = Izq + "/{SecondaryButton}";

        public static InputAction StickIzquierdo { get; private set; }
        public static InputAction StickDerecho { get; private set; }
        public static InputAction ClicStickIzquierdo { get; private set; }
        /// <summary>VR27: clic del stick derecho (saltar).</summary>
        public static InputAction ClicStickDerecho { get; private set; }
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
        /// <summary>VR27: pausa = botón Menú del control izquierdo o Y (MenusVR la lee para abrir y cerrar la pausa como Esc).</summary>
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
                Menu = Boton("VR_Menu", Izq + "/{MenuButton}", RutaPausaY);
                ClicStickDerecho = Boton("VR_ClicStickDer", Der + "/{Primary2DAxisClick}");
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
            GripDerecho, GripIzquierdo, BotonA, BotonB, BotonX, BotonY, Menu, ClicStickDerecho,
        };

        /// <summary>True si la acción se pulsó este cuadro (false si no existe o VR está apagado).</summary>
        public static bool Pulso(InputAction a) => a != null && a.enabled && a.WasPressedThisFrame();

        /// <summary>True si la acción se soltó este cuadro.</summary>
        public static bool Solto(InputAction a) => a != null && a.enabled && a.WasReleasedThisFrame();

        /// <summary>True mientras se mantiene.</summary>
        public static bool Sostenido(InputAction a) => a != null && a.enabled && a.IsPressed();

        public static Vector2 Eje(InputAction a) => a != null && a.enabled ? a.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Tira las acciones y las vuelve a crear encendidas (las pruebas lo usan tras reiniciar el Input System).</summary>
        public static void Recrear()
        {
            if (creadas)
            {
                foreach (InputAction a in Todas())
                {
                    a.Disable();
                    a.Dispose();
                }
            }
            creadas = false;
            Encender();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Apagar();
        }
    }
}
