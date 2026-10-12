using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Movimiento;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Crea los assets de T-010 (entrada, datos, prefab y escena de pruebas).
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorEscenaMecha.Crear
    /// </summary>
    public static class CreadorEscenaMecha
    {
        private const string RutaMecha = "Assets/_Proyecto/Mecha";
        private const string RutaEntrada = RutaMecha + "/Entrada/ControlesMecha.inputactions";
        private const string RutaDatos = RutaMecha + "/Datos/DatosMecha_Jugador.asset";
        private const string RutaConfig = RutaMecha + "/Datos/ConfigControlesMecha_Base.asset";
        private const string RutaPrefab = RutaMecha + "/Prefabs/MechaJugador.prefab";
        private const string RutaMaterialPiso = RutaMecha + "/Materiales/Greybox_Piso.mat";
        private const string RutaMaterialCubo = RutaMecha + "/Materiales/Greybox_Cubo.mat";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";

        private const float AlturaMecha = 4f;
        private const float RadioMecha = 1f;
        private const float AlturaCamara = 3.5f;
        private const float LadoPiso = 40f;

        [MenuItem("Mecha Cyberpunk/Crear T-010")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaMecha + "/Entrada");
            AsegurarCarpeta(RutaMecha + "/Prefabs");
            AsegurarCarpeta(RutaMecha + "/Materiales");
            AsegurarCarpeta(RutaMecha + "/Datos");

            CrearEntrada();
            CrearAsset<DatosMecha>(RutaDatos);
            CrearAsset<ConfigControlesMecha>(RutaConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Material piso = CrearMaterial(RutaMaterialPiso, new Color(0.25f, 0.27f, 0.3f));
            Material cubo = CrearMaterial(RutaMaterialCubo, new Color(0.55f, 0.35f, 0.65f));
            CrearPrefab();
            CrearEscena(piso, cubo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-010 creada.");
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
        }

        private static void CrearAsset<T>(string ruta) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(ruta) != null)
            {
                return;
            }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), ruta);
        }

        private static void CrearEntrada()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "ControlesMecha";
            InputActionMap mapa = asset.AddActionMap("Mecha");

            InputAction mover = mapa.AddAction("Mover", InputActionType.Value, expectedControlLayout: "Vector2");
            mover.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            mapa.AddAction("Mirar", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            mapa.AddAction("Golpear", InputActionType.Button, "<Mouse>/leftButton");
            mapa.AddAction("Bloquear", InputActionType.Button, "<Mouse>/rightButton");
            mapa.AddAction("LiberarCursor", InputActionType.Button, "<Keyboard>/escape");

            File.WriteAllText(RutaEntrada, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(RutaEntrada);
        }

        private static InputActionReference Referencia(string nombreAccion)
        {
            return AssetDatabase.LoadAllAssetsAtPath(RutaEntrada)
                .OfType<InputActionReference>()
                .First(r => r.action != null && r.action.name == nombreAccion);
        }

        private static Material CrearMaterial(string ruta, Color color)
        {
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        private static void CrearPrefab()
        {
            GameObject raiz = new GameObject("MechaJugador");
            CharacterController cc = raiz.AddComponent<CharacterController>();
            cc.height = AlturaMecha;
            cc.radius = RadioMecha;
            cc.minMoveDistance = 0f; // con fps altos el paso por frame es menor que el mínimo por defecto y no se movería
            cc.center = new Vector3(0f, AlturaMecha / 2f, 0f);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Cuerpo";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = new Vector3(0f, AlturaMecha / 2f, 0f);
            visual.transform.localScale = new Vector3(RadioMecha * 2f, AlturaMecha / 2f, RadioMecha * 2f);

            GameObject camaraObj = new GameObject("Camara");
            camaraObj.transform.SetParent(raiz.transform, false);
            camaraObj.transform.localPosition = new Vector3(0f, AlturaCamara, 0f);
            camaraObj.AddComponent<Camera>();
            camaraObj.AddComponent<AudioListener>();
            camaraObj.tag = "MainCamera";

            ControladorMecha controlador = raiz.AddComponent<ControladorMecha>();
            SerializedObject so = new SerializedObject(controlador);
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatos);
            so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigControlesMecha>(RutaConfig);
            so.FindProperty("camara").objectReferenceValue = camaraObj.transform;
            so.FindProperty("mover").objectReferenceValue = Referencia("Mover");
            so.FindProperty("mirar").objectReferenceValue = Referencia("Mirar");
            so.FindProperty("golpear").objectReferenceValue = Referencia("Golpear");
            so.FindProperty("liberarCursor").objectReferenceValue = Referencia("LiberarCursor");
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
        }

        private static void CrearEscena(Material piso, Material cubo)
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject luz = new GameObject("Luz direccional");
            Light componenteLuz = luz.AddComponent<Light>();
            componenteLuz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.name = "Piso";
            suelo.transform.position = new Vector3(0f, -0.5f, 0f);
            suelo.transform.localScale = new Vector3(LadoPiso, 1f, LadoPiso);
            suelo.GetComponent<MeshRenderer>().sharedMaterial = piso;

            // Cubos de referencia: x y z de la posición en el piso, y lado del cubo (m) en el campo y.
            Vector3[] cubos =
            {
                new Vector3(8f, 3f, 3f), new Vector3(-10f, 2f, 6f), new Vector3(0f, 4f, 15f), new Vector3(14f, 2f, -8f),
                new Vector3(-6f, 3f, -12f), new Vector3(-15f, 4f, -3f), new Vector3(5f, 2f, -16f), new Vector3(17f, 3f, 14f)
            };
            for (int i = 0; i < cubos.Length; i++)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "Cubo " + (i + 1);
                c.transform.position = new Vector3(cubos[i].x, cubos[i].y / 2f, cubos[i].z);
                c.transform.localScale = Vector3.one * cubos[i].y;
                c.GetComponent<MeshRenderer>().sharedMaterial = cubo;
            }

            GameObject mecha = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            mecha.transform.position = new Vector3(0f, 0.05f, 0f);

            EditorSceneManager.SaveScene(escena, RutaEscena);
        }
    }
}
