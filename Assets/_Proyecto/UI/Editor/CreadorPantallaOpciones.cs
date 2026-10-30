using MechaCyberpunk.UI.Opciones;
using MechaCyberpunk.UI.Kit;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-411: crea UI/Prefabs/PantallaOpciones.prefab con el kit (las opciones).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaOpciones.Crear
    /// </summary>
    public static class CreadorPantallaOpciones
    {
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallaOpciones.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-411 Pantalla de opciones")]
        public static void Crear()
        {
            KitUI.FijarEstilo(CreadorKitUI.AsegurarEstilo());
            GameObject raiz = new GameObject("PantallaOpciones");
            try
            {
                raiz.AddComponent<PantallaOpciones>().Armar();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-411: " + RutaPrefab + " creada.");
        }
    }
}
