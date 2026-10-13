using System.IO;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea los assets de T-041: configs del reloj y del ciclo, acciones de prueba, LuzNocturna en las farolas y
    /// letreros del barrio, y el reloj y el ciclo en Ciudad y Pruebas_Alan. Se puede correr varias veces.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorReloj.Crear
    /// </summary>
    public static class CreadorReloj
    {
        private const string RutaDatos = "Assets/_Proyecto/Mundo/Datos";
        private const string RutaConfigReloj = RutaDatos + "/ConfigRelojMundo_Mundo.asset";
        private const string RutaConfigCiclo = RutaDatos + "/ConfigCicloDia_Mundo.asset";
        private const string RutaControles = "Assets/_Proyecto/Mundo/Pruebas/ControlesCamaraLibre.inputactions";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaEscenaCiudad = "Assets/_Proyecto/Escenas/Ciudad.unity";
        private const string RutaEscenaPruebas = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const string PrefijoLetrero = "Barrio_Letrero";

        [MenuItem("Mecha Cyberpunk/Crear T-041")]
        public static void Crear()
        {
            CrearConfig<ConfigRelojMundo>(RutaConfigReloj);
            CrearConfig<ConfigCicloDia>(RutaConfigCiclo, true);
            AgregarAccionesDePrueba();
            AssetDatabase.Refresh();
            PonerLuzNocturnaEnBarrio();
            PrepararEscena(RutaEscenaCiudad, false);
            PrepararEscena(RutaEscenaPruebas, true);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-041 creada.");
        }

        // Con restaurar, los valores del asset vuelven a los del código (el config del ciclo se afina allí).
        private static void CrearConfig<T>(string ruta, bool restaurar = false) where T : ScriptableObject
        {
            T existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), ruta);
            }
            else if (restaurar)
            {
                EditorUtility.CopySerialized(ScriptableObject.CreateInstance<T>(), existente);
                EditorUtility.SetDirty(existente);
            }
        }

        private static void AgregarAccionesDePrueba()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(RutaControles));
            InputActionMap mapa = asset.FindActionMap("CamaraLibre", true);
            bool cambio = false;
            cambio |= AgregarAccion(mapa, "SaltarANoche", "<Keyboard>/n");
            cambio |= AgregarAccion(mapa, "SaltarAMediodia", "<Keyboard>/m");
            cambio |= AgregarAccion(mapa, "AlternarVelocidad", "<Keyboard>/t");
            if (cambio)
            {
                File.WriteAllText(RutaControles, asset.ToJson());
                AssetDatabase.ImportAsset(RutaControles);
            }
            Object.DestroyImmediate(asset);
        }

        private static bool AgregarAccion(InputActionMap mapa, string nombre, string tecla)
        {
            if (mapa.FindAction(nombre) != null)
            {
                return false;
            }
            mapa.AddAction(nombre, InputActionType.Button, tecla);
            return true;
        }

        // ---------------------------------------------------------------- barrio

        private static void PonerLuzNocturnaEnBarrio()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            int farolas = 0;
            int letreros = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Farola_") && t.GetComponent<LuzNocturna>() == null)
                {
                    Light luz = t.GetComponentInChildren<Light>(true);
                    Transform lampara = t.Find("Lampara");
                    LuzNocturna componente = t.gameObject.AddComponent<LuzNocturna>();
                    Configurar(componente, luz, lampara != null ? new[] { lampara.GetComponent<Renderer>() } : new Renderer[0]);
                    farolas++;
                }
            }
            foreach (Renderer renderer in raiz.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material != null && material.name.StartsWith(PrefijoLetrero) && renderer.GetComponent<LuzNocturna>() == null)
                {
                    LuzNocturna componente = renderer.gameObject.AddComponent<LuzNocturna>();
                    Configurar(componente, null, new[] { renderer });
                    letreros++;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            Debug.Log("[Mundo] LuzNocturna agregada: " + farolas + " farolas, " + letreros + " letreros.");
        }

        private static void Configurar(LuzNocturna componente, Light luz, Renderer[] renderers)
        {
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty("luz").objectReferenceValue = luz;
            SerializedProperty lista = so.FindProperty("renderers");
            lista.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- escenas

        private static void PrepararEscena(string ruta, bool conControl)
        {
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            RelojMundo reloj = Object.FindFirstObjectByType<RelojMundo>();
            if (reloj == null)
            {
                GameObject objeto = new GameObject("RelojMundo");
                reloj = objeto.AddComponent<RelojMundo>();
                SerializedObject so = new SerializedObject(reloj);
                so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigRelojMundo>(RutaConfigReloj);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (Object.FindFirstObjectByType<CicloDiaNoche>() == null)
            {
                GameObject objeto = new GameObject("CicloDiaNoche");
                CicloDiaNoche ciclo = objeto.AddComponent<CicloDiaNoche>();
                SerializedObject so = new SerializedObject(ciclo);
                so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaConfigCiclo);
                so.FindProperty("luz").objectReferenceValue = GameObject.Find("LuzNoche").GetComponent<Light>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (conControl && Object.FindFirstObjectByType<ControlRelojDePrueba>() == null)
            {
                GameObject objeto = new GameObject("ControlRelojDePrueba");
                ControlRelojDePrueba control = objeto.AddComponent<ControlRelojDePrueba>();
                SerializedObject so = new SerializedObject(control);
                so.FindProperty("reloj").objectReferenceValue = reloj;
                so.FindProperty("controles").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaControles);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
    }
}
