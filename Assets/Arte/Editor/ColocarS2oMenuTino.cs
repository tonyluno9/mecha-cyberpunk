using MechaCyberpunk.UI;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-887 (playtest 7-nov, sesión 2): el menú de mejora de armas de Tino con el estilo del HUD H10. El acomodo y los marcos van en
    /// PantallaMejoraArmas; aquí se le ponen a su config las letras del HUD (Rajdhani, UI/Fuentes). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oMenuTino.Colocar
    /// </summary>
    public static class ColocarS2oMenuTino
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";
        public const string CarpetaFuentes = "Assets/_Proyecto/UI/Fuentes/";
        public const float DistanciaCamara = 3.5f;

        [MenuItem("Mecha Cyberpunk/S2o/8 Menú de Tino con el estilo de H10")]
        public static void Colocar()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            config.PonerFuentes(AssetDatabase.LoadAssetAtPath<Font>(CarpetaFuentes + "Rajdhani-Regular.ttf"),
                AssetDatabase.LoadAssetAtPath<Font>(CarpetaFuentes + "Rajdhani-Medium.ttf"));
            // El holograma ahora es más bajo (recortado arriba y abajo): la cámara del arma se aleja para que el arma quepa entera.
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("distancia").floatValue = DistanciaCamara;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] Menú de Tino con la letra de H10: " + (config.FuenteTexto != null ? config.FuenteTexto.name : "FALTA"));
        }
    }
}
