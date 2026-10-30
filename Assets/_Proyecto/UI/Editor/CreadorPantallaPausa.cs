using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Kit;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-410: crea UI/Prefabs/PantallaPausa.prefab con el kit (la pausa).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaPausa.Crear
    /// </summary>
    public static class CreadorPantallaPausa
    {
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallaPausa.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-410 Pantalla de pausa")]
        public static void Crear()
        {
            KitUI.FijarEstilo(CreadorKitUI.AsegurarEstilo());
            GameObject raiz = new GameObject("PantallaPausa");
            try
            {
                raiz.AddComponent<PantallaPausa>().Armar();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-410: " + RutaPrefab + " creada.");
        }
    }
}
