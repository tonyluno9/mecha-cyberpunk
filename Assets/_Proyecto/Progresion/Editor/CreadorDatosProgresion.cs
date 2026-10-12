using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>Crea los datos de la progresión (calor).</summary>
    public static class CreadorDatosProgresion
    {
        private const string Ruta = "Assets/_Proyecto/Progresion/Datos/DatosCalor_Base.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-034")]
        public static void Crear()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosCalor>(Ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosCalor>();
                AssetDatabase.CreateAsset(datos, Ruta);
            }
            var so = new SerializedObject(datos);
            so.FindProperty("maximo").floatValue = 100f;
            var umbrales = so.FindProperty("umbralesNivel");
            umbrales.arraySize = 3;
            umbrales.GetArrayElementAtIndex(0).floatValue = 25f;
            umbrales.GetArrayElementAtIndex(1).floatValue = 50f;
            umbrales.GetArrayElementAtIndex(2).floatValue = 75f;
            so.FindProperty("retrasoEnfriamiento").floatValue = 10f;
            so.FindProperty("enfriamientoPorSegundo").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
