using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1484: pone en Override_Jugador el clip de trote en el estado Caminar (ConstructorControladorBase.ClipsSoloJugadorT1484) sin reconstruir
    /// el controlador base ni tocar los overrides de los NPC. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarLocomocionT1484.Aplicar
    /// </summary>
    public static class AjustarLocomocionT1484
    {
        [MenuItem("Mecha Cyberpunk/Personajes/T-1484 Aplicar locomoción del jugador")]
        public static void Aplicar()
        {
            ConstructorControladorBase.AplicarClipsDelJugador();
            Debug.Log("[Personaje] T-1484: Override_Jugador con el trote en Caminar.");
        }
    }
}
