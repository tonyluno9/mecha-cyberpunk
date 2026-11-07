using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-709: marca los datos del policía a pie (DatosPandilleroAPie_Policia): siempre en alerta, empieza cubierto, dispararle sube la búsqueda.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.AjustarPoliciaT709.Aplicar
    /// </summary>
    public static class AjustarPoliciaT709
    {
        public const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Policia.asset";

        [MenuItem("Mecha Cyberpunk/T-709/Marcar datos del policía")]
        public static void Aplicar()
        {
            ScriptableObject datos = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[IA] T-709: falta " + RutaDatos);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("esPolicia").boolValue = true;
            so.FindProperty("calorAlGolpear").floatValue = 8f;
            so.FindProperty("calorAlMatar").floatValue = 30f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-709: datos del policía marcados.");
        }
    }
}
