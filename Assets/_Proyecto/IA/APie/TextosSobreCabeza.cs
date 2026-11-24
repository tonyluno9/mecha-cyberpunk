using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// VR54: los gritos de texto sobre la cabeza de enemigos y civiles (TextMesh "Grito") quedan desconectados: la frase se sigue guardando
    /// (GritoActual, para pruebas y lógica) y las voces suenan igual, pero el texto no se dibuja. Para volver a verlos, pon <see cref="Mostrar"/> en true.
    /// Los subtítulos de pantalla (PanelSubtitulos, con su opción en Ajustes) no cambian.
    /// </summary>
    public static class TextosSobreCabeza
    {
        /// <summary>True para dibujar el texto sobre la cabeza (apagado por VR54).</summary>
        public static bool Mostrar { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Mostrar = false;
    }
}
