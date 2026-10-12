using System.IO;
using System.Linq;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Vida;
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
        private const string RutaPrefabMuneco = RutaPersonaje + "/Pruebas/MunecoAPie.prefab";
        private const string RutaMaterialPuno = RutaPersonaje + "/Materiales/Greybox_Puno.mat";
        private const string RutaMaterialMuneco = RutaPersonaje + "/Materiales/Greybox_Muneco.mat";
        private const string RutaMaterialPistola = RutaPersonaje + "/Materiales/Greybox_Pistola.mat";
        private const string RutaMaterialMarca = RutaPersonaje + "/Materiales/Greybox_Marca.mat";
        private const string RutaDatosPistola = RutaPersonaje + "/Datos/DatosPistola_Base.asset";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";

        private const float AlturaCuerpo = 1.8f;
        private const float RadioCuerpo = 0.35f;
        private const float AlturaCamara = 1.65f;
        private const float DistanciaManos = 0.5f;
        private const float LadoPiso = 200f;
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
            // Reescribe el asset con los campos nuevos (golpe, KO) aunque ya existiera.
            EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatos));
            CrearAsset<ConfigControlesAPie>(RutaConfig);
            CrearAsset<ConfigInteraccionAPie>(RutaConfigInteraccion);
            CrearAsset<DatosPistola>(RutaDatosPistola);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Material piso = CrearMaterial(RutaMaterialPiso, new Color(0.25f, 0.27f, 0.3f));
            Material cubo = CrearMaterial(RutaMaterialCubo, new Color(0.55f, 0.35f, 0.65f));
            Material obstaculo = CrearMaterial(RutaMaterialObstaculo, new Color(0.85f, 0.55f, 0.2f));
            Material cuerpo = CrearMaterial(RutaMaterialCuerpo, new Color(0.3f, 0.5f, 0.7f));
            Material puno = CrearMaterial(RutaMaterialPuno, new Color(0.9f, 0.75f, 0.6f));
            Material muneco = CrearMaterial(RutaMaterialMuneco, new Color(0.8f, 0.8f, 0.8f));
            Material pistola = CrearMaterial(RutaMaterialPistola, new Color(0.15f, 0.15f, 0.18f));
            Material marca = CrearMaterial(RutaMaterialMarca, new Color(0.05f, 0.05f, 0.05f));
            CrearPrefab(cuerpo, puno, pistola, marca);
            CrearPrefabMuneco(muneco);
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
            // Si ya existe se conserva: regenerarla cambia los ids de las acciones y ensucia el prefab sin motivo.
            if (File.Exists(RutaEntrada))
            {
                return;
            }
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

        private static void CrearPrefab(Material materialCuerpo, Material materialPuno, Material materialPistola, Material materialMarca)
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

            GameObject punoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            punoObj.name = "Puno";
            Object.DestroyImmediate(punoObj.GetComponent<Collider>());
            punoObj.transform.SetParent(manos.transform, false);
            punoObj.transform.localPosition = new Vector3(0.25f, -0.28f, 0f);
            punoObj.transform.localScale = new Vector3(0.14f, 0.14f, 0.2f);
            punoObj.GetComponent<MeshRenderer>().sharedMaterial = materialPuno;
            punoObj.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            DatosPersonaje datosPersonaje = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatos);
            SerializedObject soVida = new SerializedObject(raiz.AddComponent<VidaAPie>());
            soVida.FindProperty("datos").objectReferenceValue = datosPersonaje;
            soVida.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soGolpe = new SerializedObject(raiz.AddComponent<GolpeAPie>());
            soGolpe.FindProperty("datos").objectReferenceValue = datosPersonaje;
            soGolpe.FindProperty("atacar").objectReferenceValue = Referencia("Atacar");
            soGolpe.FindProperty("puno").objectReferenceValue = punoObj.transform;
            soGolpe.ApplyModifiedPropertiesWithoutUndo();

            // Pistola greybox: cuerpo y cañón de cubos en Manos, con una luz breve en la boca.
            GameObject pistolaObj = new GameObject("Pistola");
            pistolaObj.transform.SetParent(manos.transform, false);
            pistolaObj.transform.localPosition = new Vector3(0.2f, -0.22f, 0.05f);
            Pieza("Cuerpo pistola", pistolaObj.transform, Vector3.zero, new Vector3(0.07f, 0.13f, 0.22f), materialPistola);
            Pieza("Canon", pistolaObj.transform, new Vector3(0f, 0.05f, 0.18f), new Vector3(0.05f, 0.05f, 0.2f), materialPistola);
            foreach (Transform pieza in pistolaObj.transform)
            {
                Object.DestroyImmediate(pieza.GetComponent<Collider>());
                pieza.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            GameObject bocaObj = new GameObject("Boca");
            bocaObj.transform.SetParent(pistolaObj.transform, false);
            bocaObj.transform.localPosition = new Vector3(0f, 0.05f, 0.32f);
            Light luzBoca = bocaObj.AddComponent<Light>();
            luzBoca.type = LightType.Point;
            luzBoca.color = new Color(1f, 0.8f, 0.4f);
            luzBoca.intensity = 8f;
            luzBoca.range = 12f;
            luzBoca.shadows = LightShadows.None;
            luzBoca.enabled = false;

            SerializedObject soPistola = new SerializedObject(raiz.AddComponent<PistolaAPie>());
            soPistola.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPistola>(RutaDatosPistola);
            soPistola.FindProperty("atacar").objectReferenceValue = Referencia("Atacar");
            soPistola.FindProperty("recargar").objectReferenceValue = Referencia("Recargar");
            soPistola.FindProperty("visual").objectReferenceValue = pistolaObj.transform;
            soPistola.FindProperty("luzBoca").objectReferenceValue = luzBoca;
            soPistola.FindProperty("materialMarca").objectReferenceValue = materialMarca;
            soPistola.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soArmas = new SerializedObject(raiz.AddComponent<ArmasAPie>());
            soArmas.FindProperty("armaPunos").objectReferenceValue = Referencia("ArmaPunos");
            soArmas.FindProperty("armaPistola").objectReferenceValue = Referencia("ArmaPistola");
            soArmas.FindProperty("visualPunos").objectReferenceValue = punoObj;
            soArmas.FindProperty("visualPistola").objectReferenceValue = pistolaObj;
            soArmas.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
        }

        // Muñeco de 1.8 m: cápsula visual con su collider; Y local 0.9 para que los pies queden en el piso.
        private static void CrearPrefabMuneco(Material material)
        {
            GameObject raiz = new GameObject("MunecoAPie");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Cuerpo";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = new Vector3(0f, AlturaCuerpo / 2f, 0f);
            visual.transform.localScale = new Vector3(RadioCuerpo * 2f, AlturaCuerpo / 2f, RadioCuerpo * 2f);
            MeshRenderer render = visual.GetComponent<MeshRenderer>();
            render.sharedMaterial = material;

            CapsuleCollider capsula = raiz.AddComponent<CapsuleCollider>();
            capsula.height = AlturaCuerpo;
            capsula.radius = RadioCuerpo;
            capsula.center = new Vector3(0f, AlturaCuerpo / 2f, 0f);

            SerializedObject so = new SerializedObject(raiz.AddComponent<MunecoAPie>());
            so.FindProperty("visual").objectReferenceValue = render;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabMuneco);
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

            // Dos muñecos frente al jugador, el daniador (H) y el revividor (K).
            GameObject prefabMuneco = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabMuneco);
            GameObject muneco1 = (GameObject)PrefabUtility.InstantiatePrefab(prefabMuneco);
            muneco1.name = "Muneco 1";
            muneco1.transform.position = new Vector3(0f, 0f, 1.4f);
            GameObject muneco2 = (GameObject)PrefabUtility.InstantiatePrefab(prefabMuneco);
            muneco2.name = "Muneco 2";
            muneco2.transform.position = new Vector3(2.5f, 0f, 3f);

            // Muñecos de la pistola: uno a 20 m (en alcance) y otro a 80 m (fuera de alcance), en x = -5 para esquivar rampas y edificios.
            GameObject muneco20 = (GameObject)PrefabUtility.InstantiatePrefab(prefabMuneco);
            muneco20.name = "Muneco 20 m";
            muneco20.transform.position = new Vector3(-5f, 0f, Mathf.Sqrt(400f - 25f));
            GameObject muneco80 = (GameObject)PrefabUtility.InstantiatePrefab(prefabMuneco);
            muneco80.name = "Muneco 80 m";
            muneco80.transform.position = new Vector3(-5f, 0f, Mathf.Sqrt(6400f - 25f));

            GameObject herramientas = new GameObject("Herramientas de prueba");
            DaniadorDePrueba daniador = herramientas.AddComponent<DaniadorDePrueba>();
            daniador.Configurar(jugador, 10f, new InputAction("Danio", InputActionType.Button, "<Keyboard>/h"));
            RevividorDePrueba revividor = herramientas.AddComponent<RevividorDePrueba>();
            revividor.Configurar(jugador.GetComponent<VidaAPie>(), new InputAction("Revivir", InputActionType.Button, "<Keyboard>/k"));

            EditorSceneManager.SaveScene(escena, RutaEscena);
        }
    }
}
