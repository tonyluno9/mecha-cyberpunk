using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// Crea los assets de T-051 (entrada, datos, materiales, prefab del jugador a pie y escena de pruebas).
    /// Es la única forma de cambiar Pruebas_APie.unity: las tareas siguientes agregan su parte aquí y lo regeneran.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorJugadorAPie.Crear
    /// </summary>
    public static class CreadorJugadorAPie
    {
        private const string RutaPersonaje = "Assets/_Proyecto/Personaje";
        private const string RutaEntrada = RutaPersonaje + "/Entrada/ControlesAPie.inputactions";
        private const string RutaDatos = RutaPersonaje + "/Datos/DatosPersonaje_Base.asset";
        private const string RutaConfig = RutaPersonaje + "/Datos/ConfigControlesAPie_Base.asset";
        private const string RutaConfigInteraccion = RutaPersonaje + "/Datos/ConfigInteraccionAPie_Base.asset";
        private const string RutaPrefab = RutaPersonaje + "/Prefabs/JugadorAPie.prefab";
        private const string RutaPrefabLocal = RutaPersonaje + "/Pruebas/LocalDePrueba.prefab";
        private const string RutaMaterialPiso = RutaPersonaje + "/Materiales/Greybox_Piso.mat";
        private const string RutaMaterialCubo = RutaPersonaje + "/Materiales/Greybox_Cubo.mat";
        private const string RutaMaterialObstaculo = RutaPersonaje + "/Materiales/Greybox_Obstaculo.mat";
        private const string RutaMaterialCuerpo = RutaPersonaje + "/Materiales/Greybox_Cuerpo.mat";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";

        private const float AlturaCuerpo = 1.8f;
        private const float RadioCuerpo = 0.35f;
        private const float AlturaCamara = 1.65f;
        private const float DistanciaManos = 0.5f;
        private const float LadoPiso = 120f;
        private const float PosicionZLocales = -50f;

        [MenuItem("Mecha Cyberpunk/Crear T-051")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaPersonaje + "/Entrada");
            AsegurarCarpeta(RutaPersonaje + "/Datos");
            AsegurarCarpeta(RutaPersonaje + "/Prefabs");
            AsegurarCarpeta(RutaPersonaje + "/Materiales");
            AsegurarCarpeta(RutaPersonaje + "/Pruebas");
            AsegurarCarpeta("Assets/_Proyecto/Escenas/Tony");

            CrearEntrada();
            CrearAsset<DatosPersonaje>(RutaDatos);
            CrearAsset<ConfigControlesAPie>(RutaConfig);
            CrearAsset<ConfigInteraccionAPie>(RutaConfigInteraccion);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Material piso = CrearMaterial(RutaMaterialPiso, new Color(0.25f, 0.27f, 0.3f));
            Material cubo = CrearMaterial(RutaMaterialCubo, new Color(0.55f, 0.35f, 0.65f));
            Material obstaculo = CrearMaterial(RutaMaterialObstaculo, new Color(0.85f, 0.55f, 0.2f));
            Material cuerpo = CrearMaterial(RutaMaterialCuerpo, new Color(0.3f, 0.5f, 0.7f));
            CrearPrefab(cuerpo);
            CrearPrefabLocal(cubo, obstaculo);
            CrearEscena(piso, cubo, obstaculo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-051 creada.");
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
            asset.name = "ControlesAPie";
            InputActionMap mapa = asset.AddActionMap("APie");

            InputAction mover = mapa.AddAction("Mover", InputActionType.Value, expectedControlLayout: "Vector2");
            mover.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            mapa.AddAction("Mirar", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            mapa.AddAction("Correr", InputActionType.Button, "<Keyboard>/leftShift");
            mapa.AddAction("Saltar", InputActionType.Button, "<Keyboard>/space");
            mapa.AddAction("Interactuar", InputActionType.Button, "<Keyboard>/e");
            mapa.AddAction("Atacar", InputActionType.Button, "<Mouse>/leftButton");
            mapa.AddAction("ArmaPunos", InputActionType.Button, "<Keyboard>/1");
            mapa.AddAction("ArmaPistola", InputActionType.Button, "<Keyboard>/2");
            mapa.AddAction("Recargar", InputActionType.Button, "<Keyboard>/r");
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

        private static void CrearPrefab(Material materialCuerpo)
        {
            GameObject raiz = new GameObject("JugadorAPie");
            CharacterController cc = raiz.AddComponent<CharacterController>();
            cc.height = AlturaCuerpo;
            cc.radius = RadioCuerpo;
            cc.center = new Vector3(0f, AlturaCuerpo / 2f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0f;

            GameObject camaraObj = new GameObject("Camara");
            camaraObj.transform.SetParent(raiz.transform, false);
            camaraObj.transform.localPosition = new Vector3(0f, AlturaCamara, 0f);
            Camera camara = camaraObj.AddComponent<Camera>();
            camara.nearClipPlane = 0.05f;
            camaraObj.AddComponent<AudioListener>();
            camaraObj.tag = "MainCamera";

            GameObject manos = new GameObject("Manos");
            manos.transform.SetParent(camaraObj.transform, false);
            manos.transform.localPosition = new Vector3(0f, 0f, DistanciaManos);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Cuerpo";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = new Vector3(0f, AlturaCuerpo / 2f, 0f);
            visual.transform.localScale = new Vector3(RadioCuerpo * 2f, AlturaCuerpo / 2f, RadioCuerpo * 2f);
            MeshRenderer render = visual.GetComponent<MeshRenderer>();
            render.sharedMaterial = materialCuerpo;
            render.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            SerializedObject soJugador = new SerializedObject(raiz.AddComponent<JugadorAPie>());
            soJugador.FindProperty("camara").objectReferenceValue = camara;
            soJugador.ApplyModifiedPropertiesWithoutUndo();

            ControladorAPie controlador = raiz.AddComponent<ControladorAPie>();
            SerializedObject so = new SerializedObject(controlador);
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatos);
            so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaConfig);
            so.FindProperty("camara").objectReferenceValue = camaraObj.transform;
            so.FindProperty("mover").objectReferenceValue = Referencia("Mover");
            so.FindProperty("mirar").objectReferenceValue = Referencia("Mirar");
            so.FindProperty("correr").objectReferenceValue = Referencia("Correr");
            so.FindProperty("saltar").objectReferenceValue = Referencia("Saltar");
            so.FindProperty("atacar").objectReferenceValue = Referencia("Atacar");
            so.FindProperty("liberarCursor").objectReferenceValue = Referencia("LiberarCursor");
            so.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soInteraccion = new SerializedObject(raiz.AddComponent<InteraccionAPie>());
            soInteraccion.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigInteraccionAPie>(RutaConfigInteraccion);
            soInteraccion.FindProperty("interactuar").objectReferenceValue = Referencia("Interactuar");
            soInteraccion.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
        }

        private static GameObject Caja(string nombre, Vector3 centro, Vector3 tamano, Material material)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            caja.GetComponent<MeshRenderer>().sharedMaterial = material;
            return caja;
        }

        // Rampa que sube hacia +Z desde (x, 0, zInicio); el borde bajo de la cara de arriba queda a ras del piso.
        private static void Rampa(string nombre, float x, float zInicio, float grados, float largo, float ancho, Material material)
        {
            const float grosor = 0.5f;
            float radianes = grados * Mathf.Deg2Rad;
            Vector3 direccion = new Vector3(0f, Mathf.Sin(radianes), Mathf.Cos(radianes));
            Vector3 normal = new Vector3(0f, Mathf.Cos(radianes), -Mathf.Sin(radianes));
            Vector3 centro = new Vector3(x, 0f, zInicio) + direccion * (largo / 2f) - normal * (grosor / 2f);
            GameObject rampa = Caja(nombre, centro, new Vector3(ancho, grosor, largo), material);
            rampa.transform.rotation = Quaternion.Euler(-grados, 0f, 0f);
        }

        // Local greybox de 8 x 6 x 3.5 m. El origen es el centro del hueco de la puerta, al piso; el interior va hacia +Z
        // y la calle hacia -Z. La hoja de la puerta va del lado de la calle y se desliza hacia +X.
        private static void CrearPrefabLocal(Material muro, Material puerta)
        {
            const float ancho = 8f;
            const float fondo = 6f;
            const float alto = 3.5f;
            const float grosor = 0.2f;
            const float anchoHueco = 1.2f;
            const float altoHueco = 2.4f;
            const float distanciaEntrada = 1.5f;
            const float grosorHoja = 0.1f;
            const float margen = 0.15f;

            GameObject raiz = new GameObject("LocalDePrueba");
            Transform t = raiz.transform;

            float anchoLado = (ancho - anchoHueco) / 2f;
            Pieza("Muro frente izq", t, new Vector3(-(anchoHueco + anchoLado) / 2f, alto / 2f, 0f), new Vector3(anchoLado, alto, grosor), muro);
            Pieza("Muro frente der", t, new Vector3((anchoHueco + anchoLado) / 2f, alto / 2f, 0f), new Vector3(anchoLado, alto, grosor), muro);
            Pieza("Dintel", t, new Vector3(0f, (altoHueco + alto) / 2f, 0f), new Vector3(anchoHueco, alto - altoHueco, grosor), muro);
            Pieza("Muro fondo", t, new Vector3(0f, alto / 2f, fondo), new Vector3(ancho, alto, grosor), muro);
            Pieza("Muro izquierdo", t, new Vector3(-ancho / 2f, alto / 2f, fondo / 2f), new Vector3(grosor, alto, fondo), muro);
            Pieza("Muro derecho", t, new Vector3(ancho / 2f, alto / 2f, fondo / 2f), new Vector3(grosor, alto, fondo), muro);
            Pieza("Techo", t, new Vector3(0f, alto + grosor / 2f, fondo / 2f), new Vector3(ancho, grosor, fondo), muro);

            GameObject puertaObj = new GameObject("Puerta");
            puertaObj.transform.SetParent(t, false);
            puertaObj.transform.localPosition = new Vector3(0f, altoHueco / 2f, 0f);
            GameObject hoja = Pieza("Hoja", puertaObj.transform, new Vector3(0f, 0f, -(grosor + grosorHoja) / 2f),
                new Vector3(anchoHueco, altoHueco, grosorHoja), puerta);

            GameObject entrada = new GameObject("Entrada");
            entrada.transform.SetParent(t, false);
            entrada.transform.localPosition = new Vector3(0f, 0f, -distanciaEntrada);

            // Zona: cubre el interior, desde un margen dentro de la cara interior del muro del frente.
            float zInicio = grosor / 2f + margen;
            float zFin = fondo - grosor / 2f - margen;
            GameObject zona = new GameObject("Zona");
            zona.transform.SetParent(t, false);
            zona.transform.localPosition = new Vector3(0f, alto / 2f, (zInicio + zFin) / 2f);
            BoxCollider caja = zona.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(ancho - grosor - margen * 2f, alto, zFin - zInicio);

            LugarDePrueba lugar = raiz.AddComponent<LugarDePrueba>();
            SerializedObject soLugar = new SerializedObject(lugar);
            soLugar.FindProperty("entrada").objectReferenceValue = entrada.transform;
            soLugar.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soPuerta = new SerializedObject(puertaObj.AddComponent<PuertaDePrueba>());
            soPuerta.FindProperty("lugar").objectReferenceValue = lugar;
            soPuerta.FindProperty("hoja").objectReferenceValue = hoja.transform;
            soPuerta.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soZona = new SerializedObject(zona.AddComponent<ZonaLugarDePrueba>());
            soZona.FindProperty("lugar").objectReferenceValue = lugar;
            soZona.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabLocal);
            Object.DestroyImmediate(raiz);
        }

        private static GameObject Pieza(string nombre, Transform padre, Vector3 posicionLocal, Vector3 tamano, Material material)
        {
            GameObject pieza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pieza.name = nombre;
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = posicionLocal;
            pieza.transform.localScale = tamano;
            pieza.GetComponent<MeshRenderer>().sharedMaterial = material;
            return pieza;
        }

        private static void ColocarLocal(string nombre, float x, IdLugar id, HorarioLugar horario, bool cerradoEnDemo, bool abiertoPorHorario)
        {
            GameObject local = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabLocal));
            local.name = nombre;
            local.transform.position = new Vector3(x, 0f, PosicionZLocales);
            SerializedObject so = new SerializedObject(local.GetComponent<LugarDePrueba>());
            so.FindProperty("idLugar").enumValueIndex = (int)id;
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("horario.abre").floatValue = horario.Abre;
            so.FindProperty("horario.cierra").floatValue = horario.Cierra;
            so.FindProperty("horario.siempreAbierto").boolValue = horario.SiempreAbierto;
            so.FindProperty("cerradoEnDemo").boolValue = cerradoEnDemo;
            so.FindProperty("abiertoPorHorario").boolValue = abiertoPorHorario;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CrearEscena(Material piso, Material cubo, Material obstaculo)
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject luz = new GameObject("Luz direccional");
            Light componenteLuz = luz.AddComponent<Light>();
            componenteLuz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Caja("Piso", new Vector3(0f, -0.5f, 0f), new Vector3(LadoPiso, 1f, LadoPiso), piso);

            // Obstáculos delante del jugador (que mira a +Z): muro, escalón y rampas.
            Caja("Muro 1.2 m", new Vector3(7f, 0.6f, 8f), new Vector3(6f, 1.2f, 0.5f), obstaculo);
            Caja("Escalon 0.3 m", new Vector3(-4f, 0.15f, 9f), new Vector3(6f, 0.3f, 4f), obstaculo);
            Rampa("Rampa 30 grados", 0f, 10f, 30f, 14f, 4f, obstaculo);
            Rampa("Rampa 50 grados", -10f, 10f, 50f, 8f, 4f, obstaculo);

            // "Edificios": lado en m en el campo y; x y z son la posición en el piso.
            Vector3[] cubos =
            {
                new Vector3(-20f, 6f, 30f), new Vector3(15f, 10f, 35f), new Vector3(0f, 8f, 45f), new Vector3(30f, 5f, 15f),
                new Vector3(-30f, 4f, -10f), new Vector3(20f, 3f, -25f), new Vector3(-12f, 7f, -30f), new Vector3(40f, 9f, 40f)
            };
            for (int i = 0; i < cubos.Length; i++)
            {
                Caja("Edificio " + (i + 1), new Vector3(cubos[i].x, cubos[i].y / 2f, cubos[i].z), Vector3.one * cubos[i].y, cubo);
            }

            // Locales de prueba detrás del jugador: Bar abierto, Clandestino cerrado por horario, Casino cerrado en la demo.
            ColocarLocal("Bar", -20f, IdLugar.Bar, HorarioLugar.Siempre, false, true);
            ColocarLocal("Clandestino", 0f, IdLugar.Clandestino, new HorarioLugar(20f, 6f), false, false);
            ColocarLocal("Casino", 20f, IdLugar.Casino, HorarioLugar.Siempre, true, true);

            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            jugador.transform.position = new Vector3(0f, 0.05f, 0f);

            EditorSceneManager.SaveScene(escena, RutaEscena);
        }
    }
}
