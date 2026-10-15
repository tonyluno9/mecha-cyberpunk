using MechaCyberpunk.Mecha.Estado;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-103: ConfigMechaDaniado_Base y MechaPersistente en MechaJugador.prefab (MechaEstacionado.prefab lo hereda).
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorMechaPersistente.Crear
    /// </summary>
    public static class CreadorMechaPersistente
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Mecha/Datos/ConfigMechaDaniado_Base.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-103 mecha persistente")]
        public static void Crear()
        {
            ConfigMechaDaniado config = AssetDatabase.LoadAssetAtPath<ConfigMechaDaniado>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigMechaDaniado>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                MechaPersistente persistente = raiz.GetComponent<MechaPersistente>();
                if (persistente == null)
                {
                    persistente = raiz.AddComponent<MechaPersistente>();
                }
                SerializedObject so = new SerializedObject(persistente);
                so.FindProperty("config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-103 mecha persistente creado.");
        }
    }
}
