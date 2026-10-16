using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Emboscada;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Editor
{
    /// <summary>
    /// Crea lo de T-071: DatosEmboscada_Base y Emboscada.prefab (DirectorEmboscada). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Pelea.Editor.CreadorEmboscada.Crear
    /// </summary>
    public static class CreadorEmboscada
    {
        private const string RutaDatos = "Assets/_Proyecto/Pelea/Datos/DatosEmboscada_Base.asset";
        private const string RutaPrefab = "Assets/_Proyecto/Pelea/Prefabs/Emboscada.prefab";
        private const string RutaRival = "Assets/_Proyecto/IA/Prefabs/RivalPandilla.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-071 emboscada")]
        public static void Crear()
        {
            DatosEmboscada datos = AssetDatabase.LoadAssetAtPath<DatosEmboscada>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosEmboscada>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaRival);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            datos = AssetDatabase.LoadAssetAtPath<DatosEmboscada>(RutaDatos);

            GameObject raiz = new GameObject("Emboscada");
            try
            {
                DirectorEmboscada director = raiz.AddComponent<DirectorEmboscada>();
                SerializedObject soDirector = new SerializedObject(director);
                soDirector.FindProperty("datos").objectReferenceValue = datos;
                soDirector.FindProperty("activarAlAcercarse").boolValue = true;
                soDirector.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Pelea] T-071 creada.");
        }
    }
}
