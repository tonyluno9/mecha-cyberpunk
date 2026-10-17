using System.IO;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Pruebas;
using MechaCyberpunk.IA.Rival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea lo de T-021: datos, materiales, prefab RivalBasico y el rival con su alternador en Pruebas_Tony (idempotente).
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorRivalBasico.Crear
    /// </summary>
    public static class CreadorRivalBasico
    {
        private const string RutaIA = "Assets/_Proyecto/IA";
        private const string RutaDatosMecha = RutaIA + "/Datos/DatosMecha_RivalPrueba.asset";
        private const string RutaDatosRival = RutaIA + "/Datos/DatosRival_Prueba.asset";
        private const string RutaConfig = RutaIA + "/Datos/ConfigPresentacionRival_Base.asset";
        private const string RutaMaterialCuerpo = RutaIA + "/Materiales/Rival_Cuerpo.mat";
        private const string RutaMaterialBrazo = RutaIA + "/Materiales/Rival_Brazo.mat";
        private const string RutaPrefab = RutaIA + "/Prefabs/RivalBasico.prefab";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";

        private const float PreparacionGolpeRival = 0.6f;
        private const float AlturaRival = 4f;
        private const float RadioRival = 1f;
        // A 15 m del jugador (que está en el origen), por el lado +X: lejos del saco y del atacante, sin cubos en el camino.
        private static readonly Vector3 PosicionEscena = new Vector3(15f, 0.05f, 0f);
        private const float GiroEscena = 270f; // mira hacia -X, donde está el jugador
        private static readonly Color ColorCuerpo = new Color(0.6f, 0.12f, 0.15f);
        private static readonly Color ColorBrazo = new Color(0.35f, 0.08f, 0.1f);
        private const string TeclaAlternar = "<Keyboard>/c";

        [MenuItem("Mecha Cyberpunk/Crear T-021")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaIA + "/Datos");
            AsegurarCarpeta(RutaIA + "/Materiales");
            AsegurarCarpeta(RutaIA + "/Prefabs");
            AssetDatabase.Refresh();

            DatosMecha datosMecha = CrearDatosMecha();
            DatosRival datosRival = CrearDatosRival(datosMecha);
            ConfigPresentacionRival config = CrearAsset<ConfigPresentacionRival>(RutaConfig);
            Material cuerpo = CrearMaterial(RutaMaterialCuerpo, ColorCuerpo, false);
            Material brazo = CrearMaterial(RutaMaterialBrazo, ColorBrazo, true);
            AssetDatabase.SaveAssets();

            ArmarPrefab(datosMecha, datosRival, config, cuerpo, brazo);
            ArmarEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-021 creada.");
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
        }

        private static T CrearAsset<T>(string ruta) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, ruta);
            }
            return asset;
        }

        private static DatosMecha CrearDatosMecha()
        {
            DatosMecha datos = CrearAsset<DatosMecha>(RutaDatosMecha);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = "Rival de prueba";
            so.FindProperty("preparacionGolpe").floatValue = PreparacionGolpeRival;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static DatosRival CrearDatosRival(DatosMecha mecha)
        {
            DatosRival datos = CrearAsset<DatosRival>(RutaDatosRival);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("alias").stringValue = "Rival de prueba";
            so.FindProperty("mecha").objectReferenceValue = mecha;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static Material CrearMaterial(string ruta, Color color, bool conEmision)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color); // Unity sincroniza _Color con _BaseColor al importar; así el asset ya queda igual y no cambia solo
            if (conEmision)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Asignar(Object componente, string campo, Object valor)
        {
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Pieza(string nombre, PrimitiveType tipo, Transform padre, Vector3 posicion,
            Vector3 escala, Material material)
        {
            GameObject pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = nombre;
            Object.DestroyImmediate(pieza.GetComponent<Collider>());
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = posicion;
            pieza.transform.localScale = escala;
            pieza.GetComponent<MeshRenderer>().sharedMaterial = material;
            return pieza;
        }

        private static void ArmarPrefab(DatosMecha datosMecha, DatosRival datosRival, ConfigPresentacionRival config,
            Material materialCuerpo, Material materialBrazo)
        {
            GameObject raiz = new GameObject("RivalBasico");
            try
            {
                CharacterController cc = raiz.AddComponent<CharacterController>();
                cc.height = AlturaRival;
                cc.radius = RadioRival;
                cc.minMoveDistance = 0f;
                cc.center = new Vector3(0f, AlturaRival / 2f, 0f);

                // "Visual" es el pivote en los pies: se inclina entero al caer.
                Transform visual = new GameObject("Visual").transform;
                visual.SetParent(raiz.transform, false);
                GameObject cuerpo = Pieza("Cuerpo", PrimitiveType.Capsule, visual, new Vector3(0f, AlturaRival / 2f, 0f),
                    new Vector3(RadioRival * 2f, AlturaRival / 2f, RadioRival * 2f), materialCuerpo);

                // Hombro derecho a la altura del pecho; el brazo de cubos sale al frente (+Z).
                Transform hombro = new GameObject("Hombro").transform;
                hombro.SetParent(visual, false);
                hombro.localPosition = new Vector3(0.9f, 2.8f, 0.4f);
                GameObject antebrazo = Pieza("Brazo", PrimitiveType.Cube, hombro, new Vector3(0f, 0f, 0.8f),
                    new Vector3(0.45f, 0.45f, 1.6f), materialBrazo);
                GameObject puno = Pieza("Puno", PrimitiveType.Cube, hombro, new Vector3(0f, 0f, 1.8f),
                    new Vector3(0.8f, 0.8f, 0.8f), materialBrazo);

                VidaMecha vida = raiz.AddComponent<VidaMecha>();
                Asignar(vida, "datos", datosMecha);

                PeleadorMecha peleador = raiz.AddComponent<PeleadorMecha>();
                RivalBasico rival = raiz.AddComponent<RivalBasico>();
                PresentacionRival presentacion = raiz.AddComponent<PresentacionRival>();

                SerializedObject soPeleador = new SerializedObject(peleador);
                soPeleador.FindProperty("lado").enumValueIndex = (int)LadoPelea.Rival;
                SerializedProperty lista = soPeleador.FindProperty("controlados");
                lista.arraySize = 1;
                lista.GetArrayElementAtIndex(0).objectReferenceValue = rival;
                soPeleador.ApplyModifiedPropertiesWithoutUndo();

                Asignar(rival, "datos", datosRival);
                Asignar(rival, "config", config);
                Asignar(rival, "brazo", hombro);
                SerializedObject soRival = new SerializedObject(rival);
                SerializedProperty piezas = soRival.FindProperty("piezasBrazo");
                piezas.arraySize = 2;
                piezas.GetArrayElementAtIndex(0).objectReferenceValue = antebrazo.GetComponent<Renderer>();
                piezas.GetArrayElementAtIndex(1).objectReferenceValue = puno.GetComponent<Renderer>();
                soRival.FindProperty("controlAlArrancar").boolValue = false;
                soRival.ApplyModifiedPropertiesWithoutUndo();

                Asignar(presentacion, "config", config);
                Asignar(presentacion, "visual", visual);
                Asignar(presentacion, "cuerpo", cuerpo.GetComponent<Renderer>());

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void ArmarEscena()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            // Abrir la escena descarga los assets sin uso: se cargan de nuevo para no asignar referencias muertas.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (raiz.name == "RivalBasico" || raiz.name == "AlternadorControlRival")
                {
                    Object.DestroyImmediate(raiz);
                }
            }

            GameObject rival = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            rival.transform.SetPositionAndRotation(PosicionEscena, Quaternion.Euler(0f, GiroEscena, 0f));

            GameObject alternador = new GameObject("AlternadorControlRival");
            SceneManager.MoveGameObjectToScene(alternador, escena);
            AlternadorControlRival componente = alternador.AddComponent<AlternadorControlRival>();
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty("rival").objectReferenceValue = rival;
            SerializedProperty accion = so.FindProperty("alternar");
            accion.FindPropertyRelative("m_Name").stringValue = "Alternar control del rival";
            accion.FindPropertyRelative("m_Type").intValue = (int)UnityEngine.InputSystem.InputActionType.Button;
            SerializedProperty bindings = accion.FindPropertyRelative("m_SingletonActionBindings");
            bindings.arraySize = 1;
            SerializedProperty binding = bindings.GetArrayElementAtIndex(0);
            binding.FindPropertyRelative("m_Path").stringValue = TeclaAlternar;
            binding.FindPropertyRelative("m_Id").stringValue = System.Guid.NewGuid().ToString();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
    }
}
