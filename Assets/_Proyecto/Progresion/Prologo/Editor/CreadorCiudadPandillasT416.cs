using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-416 del creador de la ciudad de pandillas: rehace las 5 armas (datos, ofertas de El Mercado, catálogo) y el prefab del jugador
    /// con el fusil, el sniper y su zoom, y pone la mira del sniper (MiraSniper) bajo GTA_T416. Idempotente.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT416 = "GTA_T416";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-416 Fusil y sniper")]
        public static void T416()
        {
            T416Datos();
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T416Interno();
            Guardar(escena);
        }

        // Los datos y el prefab viven en Personaje.Editor, que este asmdef no referencia: se llama por reflexión.
        private static void T416Datos()
        {
            Type creador = Type.GetType("MechaCyberpunk.Personaje.Editor.CreadorArmas, MechaCyberpunk.Personaje.Editor");
            if (creador == null)
            {
                Debug.LogError("[T-416] No se encontró CreadorArmas.");
                return;
            }
            creador.GetMethod("Crear").Invoke(null, null);
        }

        private static void T416Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT416);
            if (anterior != null)
            {
                UnityEngine.Object.DestroyImmediate(anterior);
            }
            new GameObject(RaizT416).AddComponent<MechaCyberpunk.UI.Hud.MiraSniper>();
        }
    }
}
