using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-726: toda cinemática del juego avisa aquí. Con <see cref="Entrar"/> el HUD se oculta entero (<see cref="OcultadorHudCinematica"/>) y solo queda
    /// el letrero de saltar, que sale de la acción real "SaltarCinematica" del Input System (<see cref="TextoSaltar"/>); con <see cref="Salir"/> el HUD vuelve.
    /// Cuenta las entradas: si dos cinemáticas se solapan, el HUD vuelve cuando salen las dos.
    /// </summary>
    public static class HudCinematica
    {
        public const string SufijoSaltar = ": SALTAR";
        private const string TeclaRespaldo = "Enter";

        private static int cuenta;
        private static InputAction accionSaltar;
        private static OcultadorHudCinematica ocultador;

        /// <summary>True mientras hay una cinemática en curso.</summary>
        public static bool Activa => cuenta > 0;
        /// <summary>El ocultador vivo (null fuera de una cinemática): para saber cuántos lienzos tiene apagados.</summary>
        public static OcultadorHudCinematica Ocultador => ocultador;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            cuenta = 0;
            accionSaltar = null;
            ocultador = null;
        }

        /// <summary>Empieza una cinemática: oculta el HUD y deja lista la acción de saltar.</summary>
        public static void Entrar()
        {
            cuenta++;
            if (cuenta > 1)
            {
                return;
            }
            InputActionAsset asset = ControlesHud.Actual != null ? ControlesHud.Actual.AssetInterfaz : null;
            if (asset != null)
            {
                // Una copia: habilitarla aquí no toca el asset compartido.
                InputActionAsset copia = Object.Instantiate(asset);
                accionSaltar = copia.FindAction(ControlesHud.MapaInterfaz + "/" + ControlesHud.AccionSaltarCinematica, false);
                accionSaltar?.Enable();
            }
            ocultador = new GameObject("OcultadorHudCinematica").AddComponent<OcultadorHudCinematica>();
        }

        /// <summary>Acaba una cinemática: el HUD vuelve cuando ya no queda ninguna.</summary>
        public static void Salir()
        {
            if (cuenta == 0)
            {
                return;
            }
            cuenta--;
            if (cuenta > 0)
            {
                return;
            }
            Soltar();
        }

        /// <summary>El letrero de saltar con la tecla real de la acción: "ENTER: SALTAR".</summary>
        public static string TextoSaltar()
        {
            InputAction accion = accionSaltar != null ? accionSaltar : ControlesHud.Actual?.AccionInterfaz(ControlesHud.AccionSaltarCinematica);
            return TeclasHud.Texto(accion, TeclaRespaldo).ToUpperInvariant() + SufijoSaltar;
        }

        /// <summary>True el cuadro en que el jugador pulsó la tecla de saltar (solo durante una cinemática).</summary>
        public static bool SaltarPresionado
        {
            get
            {
                if (!Activa)
                {
                    return false;
                }
                if (accionSaltar != null)
                {
                    return accionSaltar.WasPressedThisFrame();
                }
                Keyboard teclado = Keyboard.current;
                return teclado != null && teclado.enterKey.wasPressedThisFrame;
            }
        }

        internal static void AlDestruirseElOcultador(OcultadorHudCinematica destruido)
        {
            if (!ReferenceEquals(destruido, ocultador))
            {
                return;
            }
            cuenta = 0;
            ocultador = null;
            accionSaltar?.Disable();
            accionSaltar = null;
        }

        private static void Soltar()
        {
            accionSaltar?.Disable();
            accionSaltar = null;
            if (ocultador != null)
            {
                OcultadorHudCinematica viejo = ocultador;
                ocultador = null;
                viejo.Restaurar();
                Object.Destroy(viejo.gameObject);
            }
        }
    }
}
