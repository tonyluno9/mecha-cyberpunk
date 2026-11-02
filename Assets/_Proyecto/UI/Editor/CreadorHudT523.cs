using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-523: quita de HudAPie.prefab la línea fija de controles ("WASD caminar · SHIFT correr...", el grupo "Ayuda" con AyudaControles).
    /// Idempotente; si se vuelve a correr CreadorHudT521.Crear (que rehace el prefab con CreadorAyudas) hay que correr esto después.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorHudT523.Crear
    /// </summary>
    public static class CreadorHudT523
    {
        private const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const string NombreGrupoAyuda = "Ayuda";

        [MenuItem("Mecha Cyberpunk/Crear T-523 HUD limpio")]
        public static void Crear()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform ayuda = raiz.transform.Find(NombreGrupoAyuda);
                if (ayuda != null)
                {
                    Object.DestroyImmediate(ayuda.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                    Debug.Log("[UI] T-523: quitada la línea fija de controles de HudAPie.prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
