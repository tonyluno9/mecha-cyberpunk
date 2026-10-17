using MechaCyberpunk.Pelea.Flujo;
using UnityEditor;
using UnityEngine;

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

        [MenuItem("Mecha Cyberpunk/Crear T-132 director del prólogo")]
        public static void Crear()
        {
            CrearDatos();
            CrearPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-132 director del prólogo creado.");
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
