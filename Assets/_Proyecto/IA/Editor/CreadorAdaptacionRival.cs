using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea los datos de adaptación de T-199: DatosAdaptacionRival_Base (nivelMaximo 1) y _Avanzado (nivelMaximo 2) con los valores de
    /// D-tony-193 a D-tony-196. Idempotente: un asset que ya existe no se toca, así los valores que Tony cambie se quedan.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorAdaptacionRival.Crear
    /// </summary>
    public static class CreadorAdaptacionRival
    {
        public const string RutaBase = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Base.asset";
        public const string RutaAvanzado = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Avanzado.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-199 adaptación")]
        public static void Crear()
        {
            CrearAsset(RutaBase, 1);
            CrearAsset(RutaAvanzado, 2);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-199 datos de adaptación creados.");
        }

        private static void CrearAsset(string ruta, int nivelMaximo)
        {
            if (AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(ruta) != null)
            {
                return;
            }
            DatosAdaptacionRival datos = ScriptableObject.CreateInstance<DatosAdaptacionRival>();
            AssetDatabase.CreateAsset(datos, ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nivelMaximo").intValue = nivelMaximo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}
