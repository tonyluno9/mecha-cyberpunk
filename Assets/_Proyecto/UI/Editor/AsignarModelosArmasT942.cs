using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-942: asigna el modelo 3D (W02, W03, W04) a la escopeta, el fusil y el francotirador de ConfigMenuArmas_Base.asset y deja las mallas
    /// legibles para que el banco las parta en piezas. Batchmode: -executeMethod MechaCyberpunk.UI.Editor.AsignarModelosArmasT942.Asignar
    /// </summary>
    public static class AsignarModelosArmasT942
    {
        private static readonly string[] Rutas =
        {
            "Assets/Arte/Armas/W02_Escopeta/W02_Escopeta",
            "Assets/Arte/Armas/W03_Fusil/W03_Fusil",
            "Assets/Arte/Armas/W04_Francotirador/W04_Francotirador"
        };

        [MenuItem("Mecha Cyberpunk/UI/Modelos 3D de todas las armas (T-942)")]
        public static void Asignar()
        {
            foreach (string ruta in Rutas)
            {
                ModelImporter importador = AssetImporter.GetAtPath(ruta + ".fbx") as ModelImporter;
                if (importador != null && !importador.isReadable)
                {
                    importador.isReadable = true;
                    importador.SaveAndReimport();
                }
            }
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(CreadorMenuArmas.RutaConfig);
            SerializedObject so = new SerializedObject(config);
            SerializedProperty armas = so.FindProperty("armas");
            for (int i = 0; i < Rutas.Length; i++)
            {
                GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Rutas[i] + ".prefab");
                armas.GetArrayElementAtIndex(i + 1).FindPropertyRelative("modelo").objectReferenceValue = modelo;
                Debug.Log("[UI] T-942: modelo " + (modelo != null ? modelo.name : "NO ENCONTRADO"));
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
