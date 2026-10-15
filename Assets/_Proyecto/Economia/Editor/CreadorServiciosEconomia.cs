using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Economia.Editor
{
    /// <summary>
    /// Crea DatosEconomia_Base y ServiciosEconomia.prefab con el ServicioCartera (T-102). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Economia.Editor.CreadorServiciosEconomia.Crear
    /// </summary>
    public static class CreadorServiciosEconomia
    {
        private const string RutaDatos = "Assets/_Proyecto/Economia/Datos/DatosEconomia_Base.asset";
        private const string RutaPrefab = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-102 servicios de economía")]
        public static void Crear()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosEconomia>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            datos = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaDatos);

            if (!AssetDatabase.IsValidFolder("Assets/_Proyecto/Economia/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Economia", "Prefabs");
            }
            var raiz = new GameObject("ServiciosEconomia");
            var servicio = raiz.AddComponent<ServicioCartera>();
            var so = new SerializedObject(servicio);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Economia] T-102 servicios de economía creados.");
        }
    }
}
