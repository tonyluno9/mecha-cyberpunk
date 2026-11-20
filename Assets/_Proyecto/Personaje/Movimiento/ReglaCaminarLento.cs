using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1505: caminar de verdad. El paso normal del jugador es un trote (4.5 m/s); con la tecla de caminar (Bloq Mayús, alterna) baja a 1.6 m/s y se ve PJ01_Caminar.
    /// Shift (correr) manda mientras se mantiene. Lógica pura.
    /// </summary>
    public static class ReglaCaminarLento
    {
        /// <summary>Tecla que alterna caminar de verdad (ruta del Input System). No choca con nada: Alt es de los consumibles (T-1492), C también, Ctrl agacha.</summary>
        public const string TeclaCaminar = "<Keyboard>/capsLock";

        /// <summary>Velocidad (m/s) de caminar de verdad (la del clip PJ01_Caminar).</summary>
        public const float Velocidad = 1.6f;

        /// <summary>El estado nuevo del alternador: cambia con cada pulsación.</summary>
        public static bool Alternar(bool actual, bool pulsado) => pulsado ? !actual : actual;

        /// <summary>Velocidad de caminar: la de caminar de verdad (sin pasar de la normal) si está activado, si no la normal.</summary>
        public static float VelocidadCaminar(bool lento, float velocidadNormal) => lento ? Mathf.Min(Velocidad, velocidadNormal) : velocidadNormal;
    }
}
