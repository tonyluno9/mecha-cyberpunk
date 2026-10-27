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

        /// <summary>Agrega ServicioGuardado a ServiciosProgresion.prefab si no lo tiene (T-111). Idempotente.</summary>
        [MenuItem("Mecha Cyberpunk/Crear T-111 servicio de guardado")]
        public static void CrearT111()
        {
            var raiz = PrefabUtility.LoadPrefabContents(RutaPrefabServicios);
            try
            {
                if (raiz.GetComponent<MechaCyberpunk.Progresion.Guardado.ServicioGuardado>() == null)
                {
                    raiz.AddComponent<MechaCyberpunk.Progresion.Guardado.ServicioGuardado>();
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabServicios);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }

        private const string RutaDatosPerfil = "Assets/_Proyecto/Progresion/Datos/DatosPerfil_Base.asset";

        /// <summary>Crea DatosPerfil_Base y agrega ServicioPerfil a ServiciosProgresion.prefab si no lo tiene (T-070). Idempotente.</summary>
        [MenuItem("Mecha Cyberpunk/Crear T-070 servicio de perfil")]
        public static void CrearT070()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosPerfil>(RutaDatosPerfil);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosPerfil>();
                AssetDatabase.CreateAsset(datos, RutaDatosPerfil);
            }
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            datos = AssetDatabase.LoadAssetAtPath<DatosPerfil>(RutaDatosPerfil);

            var raiz = PrefabUtility.LoadPrefabContents(RutaPrefabServicios);
            try
            {
                var servicio = raiz.GetComponent<ServicioPerfil>();
                if (servicio == null)
                {
                    servicio = raiz.AddComponent<ServicioPerfil>();
                }
                var so = new SerializedObject(servicio);
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabServicios);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }

        private const string RutaDatosEscenasPruebas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Pruebas.asset";
        private const string RutaDatosEscenasDemo = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";
        private const string EscenaTitulo = "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity";
        private const string EscenaCiudad = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        private const string EscenaFlashback = "Assets/_Proyecto/Escenas/Tony/Pruebas_Flashback.unity";

        /// <summary>Crea DatosEscenas_Pruebas y _Demo y agrega ServicioHistoria a ServiciosProgresion.prefab (T-124). Idempotente.</summary>
        [MenuItem("Mecha Cyberpunk/Crear T-124 historia y escenas")]
        public static void CrearT124()
        {
            CrearDatosEscenas(RutaDatosEscenasPruebas);
            CrearDatosEscenas(RutaDatosEscenasDemo);
            AssetDatabase.SaveAssets();

            var raiz = PrefabUtility.LoadPrefabContents(RutaPrefabServicios);
            try
            {
                if (raiz.GetComponent<ServicioHistoria>() == null) raiz.AddComponent<ServicioHistoria>();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabServicios);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }

        private const string RutaDatosLigas = "Assets/_Proyecto/Progresion/Datos/DatosLigas_Base.asset";

        /// <summary>
        /// Crea DatosLigas_Base (no pisa valores de uno existente) y agrega ServicioLigas a ServiciosProgresion.prefab (T-154).
        /// Idempotente: corrido otra vez no cambia ningún archivo.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Crear T-154 servicio de ligas")]
        public static void CrearT154()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosLigas>(RutaDatosLigas);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosLigas>();
                AssetDatabase.CreateAsset(datos, RutaDatosLigas);
                AssetDatabase.SaveAssets();
                datos = AssetDatabase.LoadAssetAtPath<DatosLigas>(RutaDatosLigas);
            }

            var raiz = PrefabUtility.LoadPrefabContents(RutaPrefabServicios);
            try
            {
                var servicio = raiz.GetComponent<ServicioLigas>();
                bool cambio = false;
                if (servicio == null)
                {
                    servicio = raiz.AddComponent<ServicioLigas>();
                    cambio = true;
                }
                var so = new SerializedObject(servicio);
                var propiedad = so.FindProperty("datos");
                if (propiedad.objectReferenceValue != datos)
                {
                    propiedad.objectReferenceValue = datos;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    cambio = true;
                }
                if (cambio) PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabServicios);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }

        private static void CrearDatosEscenas(string ruta)
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosEscenas>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosEscenas>();
                AssetDatabase.CreateAsset(datos, ruta);
            }
            var so = new SerializedObject(datos);
            so.FindProperty("rutaTitulo").stringValue = EscenaTitulo;
            so.FindProperty("rutaCiudad").stringValue = EscenaCiudad;
            so.FindProperty("rutaFlashback").stringValue = EscenaFlashback;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}
