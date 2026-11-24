using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-musica-24nov / T-cinematicas-narracion-24nov: true mientras suena la narración de una cinemática. La prende y apaga quien reproduce la
    /// narración (cinemáticas); la lee la música de fases (Progresion/Sonido/MusicaFases) para bajar ~-10 dB. Se limpia al entrar a Play.
    /// </summary>
    public static class DuckingNarracion
    {
        /// <summary>True: hay narración sonando ahora.</summary>
        public static bool Activo { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Activo = false;
    }
}
