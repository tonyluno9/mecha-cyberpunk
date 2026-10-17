using MechaCyberpunk.Mecha.Taller;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-109: ConfigBancoTaller_Base y BancoTaller en MechaJugador.prefab (MechaEstacionado.prefab lo hereda).
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorBancoTaller.Crear
    /// </summary>
    public static class CreadorBancoTaller
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Mecha/Datos/ConfigBancoTaller_Base.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-109 banco del taller")]
        public static void Crear()
        {
            ConfigBancoTaller config = AssetDatabase.LoadAssetAtPath<ConfigBancoTaller>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigBancoTaller>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                BancoTaller banco = raiz.GetComponent<BancoTaller>();
                if (banco == null)
                {
                    banco = raiz.AddComponent<BancoTaller>();
                }
                SerializedObject so = new SerializedObject(banco);
                so.FindProperty("config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-109 banco del taller creado.");
        }
    }
}
