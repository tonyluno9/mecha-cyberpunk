using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// Crea los datos del botín de los enemigos (T-686) en Personaje/Datos/Resources.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorBotin.Crear
    /// </summary>
    public static class CreadorBotin
    {
        private const string Ruta = "Assets/_Proyecto/Personaje/Datos/Resources/DatosBotin_Base.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-686 botín")]
        public static void Crear()
        {
            Directory.CreateDirectory("Assets/_Proyecto/Personaje/Datos/Resources");
            var datos = AssetDatabase.LoadAssetAtPath<DatosBotin>(Ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosBotin>();
                AssetDatabase.CreateAsset(datos, Ruta);
            }
            var so = new SerializedObject(datos);
            so.FindProperty("modeloPistola").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Armas/W01_Pistola/W01_Pistola.prefab");
            so.FindProperty("prefabEscopeta").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Armas/W02_Escopeta/W02_Escopeta_Piso.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
