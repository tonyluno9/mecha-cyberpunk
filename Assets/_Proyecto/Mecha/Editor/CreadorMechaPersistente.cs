using MechaCyberpunk.Mecha.Estado;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-103: ConfigMechaDaniado_Base y MechaPersistente en MechaEstacionado.prefab (no en MechaJugador.prefab, D-tony-59).
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorMechaPersistente.Crear
    /// </summary>
    public static class CreadorMechaPersistente
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaVariante = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
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

            // El prefab base (arranque Directo) no lleva daño persistente (D-tony-59): si lo tiene de antes, se quita.
            GameObject baseMecha = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                MechaPersistente enBase = baseMecha.GetComponent<MechaPersistente>();
                if (enBase != null)
                {
                    Object.DestroyImmediate(enBase);
                    PrefabUtility.SaveAsPrefabAsset(baseMecha, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(baseMecha);
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaVariante);
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
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaVariante);
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
