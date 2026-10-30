using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Si el juego está en pausa (T-410, D-tony-242). Solo la pantalla de pausa lo cambia; lo leen el carro (no lee entrada ni gira la
    /// cámara), el director de pandillas (F6) y el mapa (M no abre). El tiempo, la IA y los disparos se detienen con <c>Time.timeScale</c>.
    /// </summary>
    public static class Pausa
    {
        /// <summary>True mientras el menú de pausa está abierto.</summary>
        public static bool Activa { get; private set; }

        /// <summary>Se dispara con el valor nuevo cada vez que cambia.</summary>
        public static event Action<bool> Cambio;

        public static void Fijar(bool activa)
        {
            if (Activa == activa)
            {
                return;
            }
            Activa = activa;
            Cambio?.Invoke(activa);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Activa = false;
            Cambio = null;
        }
    }
}
