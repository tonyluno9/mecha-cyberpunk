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

        private const string RutaConfigSeguimiento = "Assets/_Proyecto/Progresion/Datos/ConfigSeguimientoObjetivo_Base.asset";
        private const string CarpetaPrefabs = "Assets/_Proyecto/Progresion/Prefabs";
        private const string RutaPrefabServicios = CarpetaPrefabs + "/ServiciosProgresion.prefab";

        /// <summary>Crea la config de seguimiento del objetivo y el prefab ServiciosProgresion (T-055).</summary>
        [MenuItem("Mecha Cyberpunk/Crear T-055")]
        public static void CrearT055()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigSeguimientoObjetivo>(RutaConfigSeguimiento);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigSeguimientoObjetivo>();
                AssetDatabase.CreateAsset(config, RutaConfigSeguimiento);
            }
            var so = new SerializedObject(config);
            so.FindProperty("radioLlegada").floatValue = 4f;
            so.FindProperty("margenAlejarse").floatValue = 25f;
            so.FindProperty("margenVolver").floatValue = 10f;
            so.FindProperty("tiempoMinimoAlejandose").floatValue = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            if (!AssetDatabase.IsValidFolder(CarpetaPrefabs))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion", "Prefabs");
            }
            var raiz = new GameObject("ServiciosProgresion");
            var servicio = raiz.AddComponent<ServicioObjetivos>();
            var soServicio = new SerializedObject(servicio);
            soServicio.FindProperty("config").objectReferenceValue = config;
            soServicio.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabServicios);
            Object.DestroyImmediate(raiz);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Agrega ServicioCalor (con DatosCalor_Base) a ServiciosProgresion.prefab si todavía no lo tiene (T-067). Idempotente.</summary>
        [MenuItem("Mecha Cyberpunk/Crear T-067 servicio de calor")]
        public static void CrearT067()
        {
            var raiz = PrefabUtility.LoadPrefabContents(RutaPrefabServicios);
            try
            {
                var servicio = raiz.GetComponent<ServicioCalor>();
                if (servicio == null)
                {
                    servicio = raiz.AddComponent<ServicioCalor>();
                }
                var so = new SerializedObject(servicio);
                so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosCalor>(Ruta);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabServicios);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
