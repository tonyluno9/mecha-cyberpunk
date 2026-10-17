using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.UI.Historia;
using UnityEditor;
using UnityEngine;
using MechaCyberpunk.Progresion;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Crea lo de T-132: DatosPrologo_Base.asset y DirectorPrologo.prefab (el director con el flujo de la pelea tutorial, una EntradaArena y
    /// las pantallas de historia de hijos). Idempotente. La escena Pruebas_Prologo lo instancia desde CreadorEscenaPrologo.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorDirectorPrologo.Crear
    /// </summary>
    public static class CreadorDirectorPrologo
    {
        public const string RutaDatos = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosPrologo_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/Progresion/Prologo/Prefabs/DirectorPrologo.prefab";

        private const string RutaConfigEntrada = "Assets/_Proyecto/Progresion/Prologo/Datos/ConfigEntradaArena_Base.asset";
        private const string RutaFlujoTutorial = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";
        private const string RutaPantallasHistoria = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaDatosEscenasDemo = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-132 director del prólogo")]
        public static void Crear()
        {
            CrearDatos();
            CrearPrefab();
            ActualizarT133();
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] Director del prólogo (T-132, T-133) creado.");
        }

        // T-133: el asset de datos guarda los campos nuevos, y el prefab del director suma los subtítulos del aviso, la pantalla de continuará
        // (ya viene en PantallasHistoria) y las rutas de las escenas. Idempotente.
        private static void ActualizarT133()
        {
            DatosPrologo datos = AssetDatabase.LoadAssetAtPath<DatosPrologo>(RutaDatos);
            EditorUtility.SetDirty(datos);

            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                DirectorPrologo prologo = contenido.GetComponent<DirectorPrologo>();
                SerializedObject so = new SerializedObject(prologo);
                if (so.FindProperty("subtitulos").objectReferenceValue == null)
                {
                    so.FindProperty("subtitulos").objectReferenceValue = CreadorEscenaFlashback.CrearSubtitulos(contenido.transform);
                }
                so.FindProperty("continuara").objectReferenceValue = contenido.GetComponentInChildren<PantallaContinuara>(true);
                so.FindProperty("datosEscenas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenasDemo);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void CrearDatos()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosPrologo>(RutaDatos) != null)
            {
                return;
            }
            // Los valores son los de la clase (D-tony-69): se ajustan en el asset.
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DatosPrologo>(), RutaDatos);
        }

        private static void CrearPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
            {
                return;
            }
            GameObject raiz = new GameObject("DirectorPrologo");
            try
            {
                GameObject flujo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujoTutorial));
                flujo.transform.SetParent(raiz.transform, false);
                DirectorPelea director = flujo.GetComponent<DirectorPelea>();
                SerializedObject soFlujo = new SerializedObject(director);
                soFlujo.FindProperty("empezarAlArrancar").boolValue = false; // la pelea la empieza la entrada a la arena
                soFlujo.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefabPantallas = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallasHistoria);
                PrefabUtility.InstantiatePrefab(prefabPantallas, raiz.transform);

                GameObject objetoEntrada = new GameObject("EntradaArena");
                objetoEntrada.transform.SetParent(raiz.transform, false);
                EntradaArena entrada = objetoEntrada.AddComponent<EntradaArena>();
                SerializedObject soEntrada = new SerializedObject(entrada);
                soEntrada.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigEntradaArena>(RutaConfigEntrada);
                soEntrada.FindProperty("prefabPantallas").objectReferenceValue = prefabPantallas;
                soEntrada.ApplyModifiedPropertiesWithoutUndo();

                DirectorPrologo prologo = raiz.AddComponent<DirectorPrologo>();
                SerializedObject so = new SerializedObject(prologo);
                so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPrologo>(RutaDatos);
                so.FindProperty("entrada").objectReferenceValue = entrada;
                so.FindProperty("flujoTutorial").objectReferenceValue = director;
                so.FindProperty("etapaSinPartida").enumValueIndex = (int)MechaCyberpunk.Core.EtapaPrologo.Libre;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
