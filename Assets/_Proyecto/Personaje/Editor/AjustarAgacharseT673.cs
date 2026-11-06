using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>T-673: el texto de ayuda a pie dice CTRL agacharse (y ESPACIO rueda). Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarAgacharseT673.Ajustar</summary>
    public static class AjustarAgacharseT673
    {
        private const string RutaAyudas = "Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset";

        [MenuItem("Mecha Cyberpunk/Ajustar T-673 ayuda de agacharse")]
        public static void Ajustar()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(RutaAyudas);
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("textoAPie").stringValue = "WASD caminar · SHIFT correr · ESPACIO saltar · E usar · Q cubrirse · CTRL agacharse (+ESPACIO rueda)";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
