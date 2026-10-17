using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Economia.Editor
{
    /// <summary>
    /// Crea DatosPremioPelea_Casera (600 / 0) y DatosPremioPelea_Tutorial (1500 / 0) (T-106). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Economia.Editor.CreadorDatosPremioPelea.Crear
    /// </summary>
    public static class CreadorDatosPremioPelea
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos";
        private const int PremioCasera = 600;
        private const int PremioTutorial = 1500;

        [MenuItem("Mecha Cyberpunk/Crear T-106 premios de pelea")]
        public static void Crear()
        {
            Premio("Casera", PremioCasera, 0);
            Premio("Tutorial", PremioTutorial, 0);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Economia] T-106 premios de pelea creados.");
        }

        private static void Premio(string archivo, int ganar, int perder)
        {
            string ruta = $"{Carpeta}/DatosPremioPelea_{archivo}.asset";
            var datos = AssetDatabase.LoadAssetAtPath<DatosPremioPelea>(ruta);
            if (datos != null) return; // ya existe: no se pisan los valores ajustados en el inspector (T-150)
            datos = ScriptableObject.CreateInstance<DatosPremioPelea>();
            AssetDatabase.CreateAsset(datos, ruta);
            var so = new SerializedObject(datos);
            so.FindProperty("premioGanar").intValue = ganar;
            so.FindProperty("premioPerder").intValue = perder;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}
