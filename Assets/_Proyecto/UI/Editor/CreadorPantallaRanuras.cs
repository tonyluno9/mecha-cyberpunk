using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Kit;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-412: crea UI/Prefabs/PantallaRanuras.prefab con el kit (la pantalla de ranuras y el aviso "GUARDADO").
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaRanuras.Crear
    /// </summary>
    public static class CreadorPantallaRanuras
    {
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallaRanuras.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-412 Pantalla de ranuras")]
        public static void Crear()
        {
            KitUI.FijarEstilo(CreadorKitUI.AsegurarEstilo());
            GameObject raiz = new GameObject("PantallaRanuras");
            try
            {
                raiz.AddComponent<PantallaRanuras>().Armar();
                raiz.AddComponent<AvisoGuardado>().Armar();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-412: " + RutaPrefab + " creada.");
        }
    }
}
