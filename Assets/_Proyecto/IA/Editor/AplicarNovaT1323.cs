using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1323: pone en IA/Resources/ConfigRespuestaNova el material de chispas del destello de llegada (el de las chispas del escudo de Nova, que ya existe).
    /// Se puede correr varias veces. Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.AplicarNovaT1323.Aplicar
    /// </summary>
    public static class AplicarNovaT1323
    {
        public const string RutaConfig = "Assets/_Proyecto/IA/Resources/ConfigRespuestaNova.asset";
        public const string RutaMaterial = "Assets/_Proyecto/IA/Prefabs/Mat_ChispaEscudoNova.mat";

        [MenuItem("Mecha Cyberpunk/T1323/Aplicar destello de Nova")]
        public static void Aplicar()
        {
            ConfigRespuestaNova config = AssetDatabase.LoadAssetAtPath<ConfigRespuestaNova>(RutaConfig);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (config == null || material == null)
            {
                Debug.LogError("[T-1323] falta " + (config == null ? RutaConfig : RutaMaterial));
                return;
            }
            config.PonerMaterialDestello(material);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1323] ConfigRespuestaNova: material del destello " + material.name);
        }
    }
}
