using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-1012: le da a la pantalla de tienda las armas del banco de Tino (ConfigMenuArmas_Base) para que El Mercado dibuje el holograma 3D (mockup M32).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.MercadoM32T1012.Aplicar
    /// </summary>
    public static class MercadoM32T1012
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaTienda_Base.asset";
        public const string RutaArmas = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";

        [MenuItem("Mecha Cyberpunk/T-1012 El Mercado como M32")]
        public static void Aplicar()
        {
            Object config = AssetDatabase.LoadAssetAtPath<Object>(RutaConfig);
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("configArmas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(RutaArmas);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-1012: El Mercado con holograma");
        }
    }
}
