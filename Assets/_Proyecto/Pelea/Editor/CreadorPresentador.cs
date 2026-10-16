using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Editor
{
    /// <summary>
    /// Crea lo de T-070: ConfigPresentador_Tutorial, FlujoPeleaTutorial.prefab (FlujoPelea con RivalTutorial y presentador) y la
    /// escena Pruebas_Tutorial. Idempotente. Batchmode: -executeMethod MechaCyberpunk.Pelea.Editor.CreadorPresentador.Crear
    /// </summary>
    public static class CreadorPresentador
    {
        private const string RutaConfig = "Assets/_Proyecto/Pelea/Datos/ConfigPresentador_Tutorial.asset";
        private const string RutaPrefabBase = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabTutorial = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";
        private const string RutaRivalTutorial = "Assets/_Proyecto/IA/Prefabs/RivalTutorial.prefab";
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Tutorial.unity";
        private static readonly Vector3 GiroLuz = new Vector3(50f, -30f, 0f);

        [MenuItem("Mecha Cyberpunk/Crear T-070 presentador")]
        public static void Crear()
        {
            ConfigPresentador config = AssetDatabase.LoadAssetAtPath<ConfigPresentador>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPresentador>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            config = AssetDatabase.LoadAssetAtPath<ConfigPresentador>(RutaConfig);

            ArmarPrefab(config);
            ArmarEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[Pelea] T-070 creada.");
        }

        private static void ArmarPrefab(ConfigPresentador config)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabTutorial) == null)
            {
                AssetDatabase.CopyAsset(RutaPrefabBase, RutaPrefabTutorial);
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabTutorial);
            try
            {
                raiz.name = "FlujoPeleaTutorial";
                DirectorPelea director = raiz.GetComponent<DirectorPelea>();
                SerializedObject so = new SerializedObject(director);
                so.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaRivalTutorial);
                so.FindProperty("presentador").objectReferenceValue = config;
                so.FindProperty("empezarAlArrancar").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabTutorial);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // Escena de prueba: arena, jugador, servicios de progresión (perfil) y el flujo del tutorial, que empieza al dar Play.
        private static void ArmarEscena()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject luz = new GameObject("Luz");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(GiroLuz);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaArena), escena);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), escena);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios), escena);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabTutorial), escena);
            EditorSceneManager.SaveScene(escena, RutaEscena);
        }
    }
}
