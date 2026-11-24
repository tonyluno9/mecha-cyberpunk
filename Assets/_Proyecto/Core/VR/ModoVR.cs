using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// VR (rama vr/quest3): true mientras el juego corre con visor (OpenXR activo) o con el simulador de XR.
    /// Sin visor queda en false y el juego sigue igual que siempre (tercera persona, ratón y teclado).
    /// Lo enciende MechaCyberpunk.VR.SistemaVR; cualquier área puede leerlo para apagar lo que marea (temblores, empujones, cinemáticas).
    /// </summary>
    public static class ModoVR
    {
        /// <summary>True con visor o simulador.</summary>
        public static bool Activo { get; private set; }

        /// <summary>True si es el simulador de XR (sin visor real).</summary>
        public static bool Simulado { get; private set; }

        /// <summary>Se dispara al cambiar <see cref="Activo"/>.</summary>
        public static event Action<bool> Cambio;

        public static void Fijar(bool activo, bool simulado = false)
        {
            bool cambio = activo != Activo;
            Activo = activo;
            Simulado = activo && simulado;
            if (cambio)
            {
                Cambio?.Invoke(activo);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Activo = false;
            Simulado = false;
            Cambio = null;
        }
    }
}
