using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1498: pone en IA/Resources/ConfigRespuestaNova las naves que bajan soldados (NV01, NV03_a, NV03_b; 2 soldados con 4 estrellas, 4 con 5) y la cuerda.
    /// Idempotente. Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.AplicarNavesNovaT1498.Aplicar
    /// </summary>
    public static class AplicarNavesNovaT1498
    {
        public const string RutaConfig = "Assets/_Proyecto/IA/Resources/ConfigRespuestaNova.asset";
        private const string CarpetaPrefabs = "Assets/Arte/Vehiculos/Drones/Prefabs/";
        private const string RutaCuerda = "Assets/_Proyecto/IA/Prefabs/Mat_AroEmisivoNova.mat";

        [MenuItem("Mecha Cyberpunk/T1498/Aplicar naves de Nova")]
        public static void Aplicar()
        {
            ConfigRespuestaNova config = AssetDatabase.LoadAssetAtPath<ConfigRespuestaNova>(RutaConfig);
            GameObject nv01 = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "Dron_NV01.prefab");
            GameObject nv03a = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "Dron_NV03_a.prefab");
            GameObject nv03b = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "Dron_NV03_b.prefab");
            if (config == null || nv01 == null || nv03a == null || nv03b == null)
            {
                Debug.LogError("[T-1498] falta ConfigRespuestaNova o un prefab de nave (Dron_NV01, Dron_NV03_a, Dron_NV03_b)");
                return;
            }
            config.ConfigurarNaves(new[] { nv03a, nv01, nv03b }, 2, 4);
            Material cuerda = AssetDatabase.LoadAssetAtPath<Material>(RutaCuerda);
            if (cuerda != null)
            {
                config.PonerMaterialCuerda(cuerda);
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1498] ConfigRespuestaNova: 3 naves, cuerda " + (cuerda != null ? cuerda.name : "sencilla"));
        }
    }
}
