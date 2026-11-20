using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1487: crea Datos/ConfigGolpeAPie_Base.asset y se lo asigna al GolpeAPie del prefab JugadorAPie. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CrearConfigGolpeT1487.Crear
    /// </summary>
    public static class CrearConfigGolpeT1487
    {
        private const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/ConfigGolpeAPie_Base.asset";
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        public static void Crear()
        {
            ConfigGolpeAPie config = AssetDatabase.LoadAssetAtPath<ConfigGolpeAPie>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigGolpeAPie>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Combate.GolpeAPie golpe = raiz.GetComponentInChildren<Combate.GolpeAPie>(true);
                if (golpe == null)
                {
                    Debug.LogError("[Personaje] JugadorAPie no tiene GolpeAPie");
                    return;
                }
                SerializedObject so = new SerializedObject(golpe);
                so.FindProperty("config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] ConfigGolpeAPie_Base creado y asignado a JugadorAPie");
        }
    }
}
