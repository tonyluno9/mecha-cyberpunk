using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-425: la calidad Medio usaba el mismo renderer que Ultra (con oclusión ambiental). Crea Medio_Renderer = copia de PC_Renderer sin SSAO,
    /// lo asigna a Medio_RPAsset y apaga las sombras suaves de Medio. Se puede correr otra vez sin romper nada.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CrearRendererMedio.Crear
    /// </summary>
    public static class CrearRendererMedio
    {
        public const string RutaOrigen = "Assets/Settings/PC_Renderer.asset";
        public const string RutaMedio = "Assets/Settings/Medio_Renderer.asset";
        public const string RutaAssetMedio = "Assets/Settings/Medio_RPAsset.asset";

        [MenuItem("Mecha Cyberpunk/T-425/Crear renderer de calidad Medio")]
        public static void Crear()
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RutaMedio) == null)
            {
                AssetDatabase.CopyAsset(RutaOrigen, RutaMedio);
            }
            ScriptableRendererData renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RutaMedio);
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature != null && feature.name.Contains("ScreenSpaceAmbientOcclusion"))
                {
                    feature.SetActive(false);
                    EditorUtility.SetDirty(feature);
                }
            }
            EditorUtility.SetDirty(renderer);

            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RutaAssetMedio);
            SerializedObject so = new SerializedObject(asset);
            SerializedProperty lista = so.FindProperty("m_RendererDataList");
            lista.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_SoftShadowsSupported").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] CrearRendererMedio: Medio usa " + RutaMedio + " sin SSAO.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
