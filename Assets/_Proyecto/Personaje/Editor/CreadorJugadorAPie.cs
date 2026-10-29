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
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        private const string RutaConfigCamara = RutaPersonaje + "/Datos/ConfigCamaraHombro_Base.asset";
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
        private const string RutaEntradaCarro = RutaPersonaje + "/Pruebas/ControlesCarroDePrueba.inputactions";
        private const string RutaConfigCarro = RutaPersonaje + "/Datos/ConfigCarroDePrueba_Base.asset";
        private const string RutaPrefabCarro = RutaPersonaje + "/Pruebas/CarroDePrueba.prefab";
        private const string RutaMaterialCarro = RutaPersonaje + "/Materiales/Greybox_Carro.mat";
        private const string RutaMaterialCalle = RutaPersonaje + "/Materiales/Greybox_Calle.mat";
        private const string RutaMaterialBanqueta = RutaPersonaje + "/Materiales/Greybox_Banqueta.mat";
        private const string RutaPrefabHud = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const string RutaPrefabPantallaMisiones = "Assets/_Proyecto/UI/Prefabs/PantallaMisiones.prefab";
        private const string RutaPrefabPantallaTelefono = "Assets/_Proyecto/UI/Prefabs/PantallaTelefono.prefab";
        private const string RutaPrefabEncargosCalle = "Assets/_Proyecto/UI/Prefabs/EncargosCalle.prefab";
        private const string RutaPrefabServiciosProgresion = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaPrefabServiciosEconomia = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";
        private const string RutaEscena ="Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";

        private const float AlturaCuerpo = 1.8f;
        private const float RadioCuerpo = 0.35f;
        private const float AlturaCamara = 1.65f;
        private const float DistanciaManos = 0.5f;
        private const float LadoPiso = 200f;
        private const float PosicionZLocales = 17f;
        private const float PosicionEsquinaZ = 14f;

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
            CrearAsset<ConfigCamaraHombro>(RutaConfigCamara);
            CrearAsset<ConfigInteraccionAPie>(RutaConfigInteraccion);
            CrearAsset<DatosPistola>(RutaDatosPistola);
            CrearAsset<ConfigCarroDePrueba>(RutaConfigCarro);
            CrearEntradaCarro();
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
            CrearPrefabCarro(CrearMaterial(RutaMaterialCarro, new Color(0.9f, 0.2f, 0.45f)));
            CrearEscena(piso, CrearMaterial(RutaMaterialCalle, new Color(0.12f, 0.12f, 0.14f)),
                CrearMaterial(RutaMaterialBanqueta, new Color(0.45f, 0.45f, 0.48f)), cubo, obstaculo);
            AssetDatabase.SaveAssets();
            CreadorImplantesAPie.Crear();
            Debug.Log("[Personaje] T-051 creada.");
        }

        /// <summary>
        /// T-400: asegura la acción Apuntar, la config de la cámara de hombro y el muñeco, y regenera solo JugadorAPie.prefab.
        /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorJugadorAPie.RegenerarPrefabTerceraPersona
        /// </summary>
        public static void RegenerarPrefabTerceraPersona()
        {
            CrearEntrada();
            CrearAsset<ConfigCamaraHombro>(RutaConfigCamara);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CreadorMunecoHumano.Crear();
            CreadorArmas.CrearDatos();
            Material puno = CrearMaterial(RutaMaterialPuno, new Color(0.9f, 0.75f, 0.6f));
            Material pistola = CrearMaterial(RutaMaterialPistola, new Color(0.15f, 0.15f, 0.18f));
            Material marca = CrearMaterial(RutaMaterialMarca, new Color(0.05f, 0.05f, 0.05f));
            Material cuerpo = CrearMaterial(RutaMaterialCuerpo, new Color(0.3f, 0.5f, 0.7f));
            CrearPrefab(cuerpo, puno, pistola, marca);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] JugadorAPie.prefab regenerado en tercera persona (T-400).");
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
            // Las acciones nuevas se agregan al archivo que hay, y las que ya existen conservan sus ids.
            if (File.Exists(RutaEntrada))
            {
                AgregarAccionesFaltantes();
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
            mapa.AddAction("Telefono", InputActionType.Button, "<Keyboard>/t");
            mapa.AddAction("Apuntar", InputActionType.Button, "<Mouse>/rightButton");
            mapa.AddAction("ArmaEscopeta", InputActionType.Button, "<Keyboard>/3");
            mapa.AddAction("ArmaSubfusil", InputActionType.Button, "<Keyboard>/4");
            mapa.AddAction("CambiarArma", InputActionType.Value, "<Mouse>/scroll/y", expectedControlLayout: "Axis");

            File.WriteAllText(RutaEntrada, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(RutaEntrada);
        }

        // T-186: agrega la acción Telefono (T) al mapa APie si falta, sin tocar las demás.
        private static void AgregarAccionesFaltantes()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(RutaEntrada));
            InputActionMap mapa = asset.FindActionMap("APie");
            bool cambio = false;
            if (mapa.FindAction("Telefono") == null)
            {
                mapa.AddAction("Telefono", InputActionType.Button, "<Keyboard>/t");
                cambio = true;
            }
            // T-400: clic derecho para apuntar.
            if (mapa.FindAction("Apuntar") == null)
            {
                mapa.AddAction("Apuntar", InputActionType.Button, "<Mouse>/rightButton");
                cambio = true;
            }
            // T-403: 3 escopeta, 4 subfusil y la rueda del ratón.
            if (mapa.FindAction("ArmaEscopeta") == null)
            {
                mapa.AddAction("ArmaEscopeta", InputActionType.Button, "<Keyboard>/3");
                cambio = true;
            }
            if (mapa.FindAction("ArmaSubfusil") == null)
            {
                mapa.AddAction("ArmaSubfusil", InputActionType.Button, "<Keyboard>/4");
                cambio = true;
            }
            if (mapa.FindAction("CambiarArma") == null)
            {
                mapa.AddAction("CambiarArma", InputActionType.Value, "<Mouse>/scroll/y", expectedControlLayout: "Axis");
                cambio = true;
            }
            if (!cambio)
            {
                Object.DestroyImmediate(asset);
                return;
            }
            File.WriteAllText(RutaEntrada, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(RutaEntrada);
        }

        // Manejo del carro de prueba con las flechas, para no chocar con WASD. Si ya existe se conserva.
        private static void CrearEntradaCarro()
        {
            if (File.Exists(RutaEntradaCarro))
            {
                return;
            }
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "ControlesCarroDePrueba";
            InputActionMap mapa = asset.AddActionMap("Carro");
            InputAction conducir = mapa.AddAction("Conducir", InputActionType.Value, expectedControlLayout: "Vector2");
            conducir.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            File.WriteAllText(RutaEntradaCarro, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(RutaEntradaCarro);
        }

        private static InputActionReference ReferenciaCarro()
        {
            return AssetDatabase.LoadAllAssetsAtPath(RutaEntradaCarro)
                .OfType<InputActionReference>()
                .First(r => r.action != null && r.action.name == "Conducir");
        }

        // Carro de prueba: caja de 2 x 1.5 x 4.2 m (ancho, alto, largo) con el frente en +Z; el asiento y la puerta van a la izquierda (-X).
        private static void CrearPrefabCarro(Material material)
        {
            const float ancho = 2f;
            const float alto = 1.5f;
            const float largo = 4.2f;
            const float distanciaPuerta = 1f;

            GameObject raiz = new GameObject("CarroDePrueba");
            raiz.AddComponent<Rigidbody>().isKinematic = true;
            BoxCollider caja = raiz.AddComponent<BoxCollider>();
            caja.center = new Vector3(0f, alto / 2f, 0f);
            caja.size = new Vector3(ancho, alto, largo);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Caja";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = new Vector3(0f, alto / 2f, 0f);
            visual.transform.localScale = new Vector3(ancho, alto, largo);
            visual.GetComponent<MeshRenderer>().sharedMaterial = material;

            GameObject asiento = new GameObject("Asiento");
            asiento.transform.SetParent(raiz.transform, false);
            asiento.transform.localPosition = new Vector3(-ancho / 4f, 0.1f, 0.4f);

            GameObject camaraObj = new GameObject("Camara");
            camaraObj.transform.SetParent(raiz.transform, false);
            camaraObj.transform.localPosition = new Vector3(0f, 3.2f, -7f);
            camaraObj.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Camera camara = camaraObj.AddComponent<Camera>();
            camara.nearClipPlane = 0.1f;
            camara.enabled = false;
            camaraObj.AddComponent<AudioListener>().enabled = false;

            GameObject puntoSubida = new GameObject("PuntoSubida");
            puntoSubida.transform.SetParent(raiz.transform, false);
            puntoSubida.transform.localPosition = new Vector3(-(ancho / 2f + distanciaPuerta), 0f, 0.4f);

            SerializedObject so = new SerializedObject(raiz.AddComponent<CarroDePrueba>());
            so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigCarroDePrueba>(RutaConfigCarro);
            so.FindProperty("asiento").objectReferenceValue = asiento.transform;
            so.FindProperty("camara").objectReferenceValue = camara;
            so.FindProperty("puntoSubida").objectReferenceValue = puntoSubida.transform;
            so.FindProperty("conducir").objectReferenceValue = ReferenciaCarro();
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabCarro);
            Object.DestroyImmediate(raiz);
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
            CreadorArmas.CrearDatos();
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
            // Pedido de Alan (D-alan-19): la cámara del jugador dibuja el posproceso de los Volumes de su escena.
            camara.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            GameObject manos = new GameObject("Manos");
            manos.transform.SetParent(camaraObj.transform, false);
            manos.transform.localPosition = new Vector3(0f, 0f, DistanciaManos);
            // T-400: tercera persona; las manos y el cuerpo de primera persona quedan apagados (sus scripts siguen).
            manos.SetActive(false);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Cuerpo";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = new Vector3(0f, AlturaCuerpo / 2f, 0f);
            visual.transform.localScale = new Vector3(RadioCuerpo * 2f, AlturaCuerpo / 2f, RadioCuerpo * 2f);
            MeshRenderer render = visual.GetComponent<MeshRenderer>();
            render.sharedMaterial = materialCuerpo;
            render.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            visual.SetActive(false);

            GameObject modeloObj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorMunecoHumano.RutaModeloJugador), raiz.transform);
            modeloObj.transform.localPosition = Vector3.zero;
            modeloObj.transform.localRotation = Quaternion.identity;
            Transform manoDer = modeloObj.transform.Find("Cadera/Torso/BrazoSupDer/BrazoInfDer/ManoDer");

            CamaraHombro camaraHombro = camaraObj.AddComponent<CamaraHombro>();

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
            so.FindProperty("apuntar").objectReferenceValue = Referencia("Apuntar");
            so.FindProperty("configCamara").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigCamaraHombro>(RutaConfigCamara);
            so.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soHombro = new SerializedObject(camaraHombro);
            soHombro.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigCamaraHombro>(RutaConfigCamara);
            soHombro.FindProperty("controlador").objectReferenceValue = controlador;
            soHombro.FindProperty("camara").objectReferenceValue = camara;
            soHombro.ApplyModifiedPropertiesWithoutUndo();
            raiz.AddComponent<AnimacionJugador>();

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
            soGolpe.FindProperty("puno").objectReferenceValue = null;
            soGolpe.ApplyModifiedPropertiesWithoutUndo();

            // Pistola greybox: cuerpo y cañón de cubos en Manos, con una luz breve en la boca.
            GameObject pistolaObj = new GameObject("Pistola");
            // T-400: la pistola va en la mano derecha del modelo; el antebrazo cuelga en -Y, así que el cañón apunta a -Y local.
            pistolaObj.transform.SetParent(manoDer, false);
            pistolaObj.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            pistolaObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
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

            // T-403: la escopeta (cañón largo, madera) y el subfusil (compacto, oscuro), también de cubos en la mano derecha.
            Material materialEscopeta = CrearMaterial(RutaPersonaje + "/Materiales/Greybox_Escopeta.mat", new Color(0.4f, 0.25f, 0.12f));
            Material materialSubfusil = CrearMaterial(RutaPersonaje + "/Materiales/Greybox_Subfusil.mat", new Color(0.1f, 0.35f, 0.4f));
            GameObject escopetaObj = CrearArmaDeFuego("Escopeta", manoDer, materialEscopeta, new Vector3(0.07f, 0.1f, 0.34f),
                new Vector3(0.06f, 0.06f, 0.55f), 0.4f, out Light luzEscopeta);
            GameObject subfusilObj = CrearArmaDeFuego("Subfusil", manoDer, materialSubfusil, new Vector3(0.08f, 0.16f, 0.3f),
                new Vector3(0.04f, 0.04f, 0.22f), 0.26f, out Light luzSubfusil);
            escopetaObj.SetActive(false);
            subfusilObj.SetActive(false);

            SerializedObject soPistola = new SerializedObject(raiz.AddComponent<PistolaAPie>());
            soPistola.FindProperty("visualEscopeta").objectReferenceValue = escopetaObj.transform;
            soPistola.FindProperty("luzBocaEscopeta").objectReferenceValue = luzEscopeta;
            soPistola.FindProperty("visualSubfusil").objectReferenceValue = subfusilObj.transform;
            soPistola.FindProperty("luzBocaSubfusil").objectReferenceValue = luzSubfusil;
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
            soArmas.FindProperty("visualPunos").objectReferenceValue = null;
            soArmas.FindProperty("visualPistola").objectReferenceValue = pistolaObj;
            soArmas.FindProperty("armaEscopeta").objectReferenceValue = Referencia("ArmaEscopeta");
            soArmas.FindProperty("armaSubfusil").objectReferenceValue = Referencia("ArmaSubfusil");
            soArmas.FindProperty("cambiarArma").objectReferenceValue = Referencia("CambiarArma");
            soArmas.FindProperty("visualEscopeta").objectReferenceValue = escopetaObj;
            soArmas.FindProperty("visualSubfusil").objectReferenceValue = subfusilObj;
            soArmas.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soArsenal = new SerializedObject(raiz.AddComponent<ArsenalJugador>());
            soArsenal.FindProperty("catalogo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CatalogoArmas>(CreadorArmas.RutaCatalogoArmas);
            soArsenal.FindProperty("pistolaAlInicio").boolValue = true;
            soArsenal.ApplyModifiedPropertiesWithoutUndo();

            // Un solo prefab para Alan (D-tony-23): el HUD y los servicios de objetivos van anidados, y la aparición en un punto.
            AnidarPrefab(RutaPrefabHud, raiz.transform);
            AnidarPrefab(RutaPrefabPantallaMisiones, raiz.transform);
            AnidarPrefab(RutaPrefabPantallaTelefono, raiz.transform);
            AnidarPrefab(RutaPrefabEncargosCalle, raiz.transform);
            AnidarPrefab(RutaPrefabServiciosProgresion, raiz.transform);
            AnidarPrefab(RutaPrefabServiciosEconomia, raiz.transform);
            raiz.AddComponent<AparicionJugador>();

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

        // Arma de fuego greybox: cuerpo y cañón de cubos, con una luz breve en la boca. Igual que la pistola de T-400 (cañón hacia -Y local de la mano).
        private static GameObject CrearArmaDeFuego(string nombre, Transform mano, Material material, Vector3 tamanoCuerpo,
            Vector3 tamanoCanon, float zBoca, out Light luz)
        {
            GameObject arma = new GameObject(nombre);
            arma.transform.SetParent(mano, false);
            arma.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            arma.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Pieza("Cuerpo " + nombre, arma.transform, Vector3.zero, tamanoCuerpo, material);
            Pieza("Canon", arma.transform, new Vector3(0f, 0.05f, zBoca - tamanoCanon.z / 2f + 0.1f), tamanoCanon, material);
            foreach (Transform pieza in arma.transform)
            {
                Object.DestroyImmediate(pieza.GetComponent<Collider>());
                pieza.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            GameObject boca = new GameObject("Boca");
            boca.transform.SetParent(arma.transform, false);
            boca.transform.localPosition = new Vector3(0f, 0.05f, zBoca + 0.1f);
            luz = boca.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.8f, 0.4f);
            luz.intensity = 8f;
            luz.range = 12f;
            luz.shadows = LightShadows.None;
            luz.enabled = false;
            return arma;
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

        // Barrio mínimo (T-059): calle 1 sobre el eje Z (x de -3 a 3, z de 0 a 17), calle 2 sobre el eje X (z de 11 a 17, x de 3 a 32).
        // La esquina de la L queda en (0, 14). El edificio de la esquina tapa la línea recta del inicio al Bar.
        private static void CrearEscena(Material piso, Material calle, Material banqueta, Material cubo, Material obstaculo)
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject luz = new GameObject("Luz direccional");
            Light componenteLuz = luz.AddComponent<Light>();
            componenteLuz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Caja("Piso", new Vector3(0f, -0.5f, 0f), new Vector3(LadoPiso, 1f, LadoPiso), piso);

            // Calles y banquetas (la banqueta mide 0.15 m, menos que el escalón que sube el jugador).
            Caja("Calle 1", new Vector3(0f, 0.02f, 8.5f), new Vector3(6f, 0.04f, 17f), calle);
            Caja("Calle 2", new Vector3(17.5f, 0.02f, 14f), new Vector3(29f, 0.04f, 6f), calle);
            Caja("Banqueta oeste", new Vector3(-3.75f, 0.075f, 8.5f), new Vector3(1.5f, 0.15f, 17f), banqueta);
            Caja("Banqueta este", new Vector3(3.75f, 0.075f, 5f), new Vector3(1.5f, 0.15f, 10f), banqueta);
            Caja("Banqueta sur", new Vector3(18.25f, 0.075f, 10.5f), new Vector3(27.5f, 0.15f, 1f), banqueta);

            // Edificios del lado sur de la calle 2, con un callejón de 4 m entre los dos (x de 14 a 18).
            Caja("Edificio esquina", new Vector3(9.25f, 6f, 5f), new Vector3(9.5f, 12f, 10f), cubo);
            Caja("Edificio sur", new Vector3(25f, 4f, 5f), new Vector3(14f, 8f, 10f), cubo);

            GameObject esquina = new GameObject("Esquina");
            esquina.transform.position = new Vector3(0f, 0f, PosicionEsquinaZ);

            // Locales de prueba en la calle 2, de espaldas a ella: Bar abierto, Clandestino cerrado por horario, Casino cerrado en la demo.
            ColocarLocal("Bar", 8f, IdLugar.Bar, HorarioLugar.Siempre, false, true);
            ColocarLocal("Clandestino", 17f, IdLugar.Clandestino, new HorarioLugar(20f, 6f), false, false);
            ColocarLocal("Casino", 26f, IdLugar.Casino, HorarioLugar.Siempre, true, true);

            // Carro de prueba en la calle 1, mirando a -Z: su puerta (a la izquierda del carro) queda del lado del centro de la calle.
            GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCarro));
            carro.name = "Carro de prueba";
            carro.transform.SetPositionAndRotation(new Vector3(-2f, 0f, 9f), Quaternion.Euler(0f, 180f, 0f));

            // Interactuable de prueba (T-108), lejos de las puertas y del carro.
            GameObject bancoPrueba = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bancoPrueba.name = "Interactuable de prueba";
            bancoPrueba.transform.position = new Vector3(4f, 0.5f, 12f);
            bancoPrueba.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            Object.DestroyImmediate(bancoPrueba.GetComponent<Collider>());
            bancoPrueba.GetComponent<Renderer>().sharedMaterial = obstaculo;
            bancoPrueba.AddComponent<InteractuableDePrueba>();

            // Punto de aparición y jugador completo (el HUD y los servicios de objetivos van dentro del prefab). El jugador empieza
            // lejos del punto para que la prueba compruebe que aparece donde debe.
            GameObject puntoObj = new GameObject("Punto Prueba_Inicio");
            puntoObj.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            puntoObj.AddComponent<PuntoAparicionDePrueba>();

            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            jugador.transform.SetPositionAndRotation(new Vector3(-30f, 0.05f, -20f), Quaternion.Euler(0f, 90f, 0f));
            SerializedObject soAparicion = new SerializedObject(jugador.GetComponent<AparicionJugador>());
            soAparicion.FindProperty("idAparicion").stringValue = "Prueba_Inicio";
            soAparicion.ApplyModifiedPropertiesWithoutUndo();

            // Dos muñecos en el callejón, el daniador (H) y el revividor (K) para probar.
            GameObject prefabMuneco = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabMuneco);
            GameObject muneco1 = (GameObject)PrefabUtility.InstantiatePrefab(prefabMuneco);
            muneco1.name = "Muneco 1";
            muneco1.transform.position = new Vector3(16f, 0f, 4f);
            GameObject muneco2 = (GameObject)PrefabUtility.InstantiatePrefab(prefabMuneco);
            muneco2.name = "Muneco 2";
            muneco2.transform.position = new Vector3(16f, 0f, 7f);

            // Secuencia de objetivos: la esquina, el Bar y el carro; pasa al siguiente al llegar.
            GameObject fijador = new GameObject("Fijador de objetivo");
            SerializedObject soFijador = new SerializedObject(fijador.AddComponent<FijadorObjetivoDePrueba>());
            soFijador.FindProperty("avanzarAlLlegar").boolValue = true;
            SerializedProperty listaObjetivos = soFijador.FindProperty("objetivos");
            listaObjetivos.arraySize = 3;
            SerializedProperty primero = listaObjetivos.GetArrayElementAtIndex(0);
            primero.FindPropertyRelative("texto").stringValue = "Ve a la esquina";
            primero.FindPropertyRelative("destino").objectReferenceValue = esquina.transform;
            SerializedProperty segundo = listaObjetivos.GetArrayElementAtIndex(1);
            segundo.FindPropertyRelative("texto").stringValue = "Entra al bar";
            segundo.FindPropertyRelative("lugar").enumValueIndex = (int)IdLugar.Bar;
            segundo.FindPropertyRelative("destino").objectReferenceValue = null;
            SerializedProperty tercero = listaObjetivos.GetArrayElementAtIndex(2);
            tercero.FindPropertyRelative("texto").stringValue = "Sube al carro";
            tercero.FindPropertyRelative("destino").objectReferenceValue = carro.transform.Find("PuntoSubida");
            soFijador.ApplyModifiedPropertiesWithoutUndo();

            GameObject herramientas = new GameObject("Herramientas de prueba");
            DaniadorDePrueba daniador = herramientas.AddComponent<DaniadorDePrueba>();
            daniador.Configurar(jugador, 10f, new InputAction("Danio", InputActionType.Button, "<Keyboard>/h"));
            RevividorDePrueba revividor = herramientas.AddComponent<RevividorDePrueba>();
            revividor.Configurar(jugador.GetComponent<VidaAPie>(), new InputAction("Revivir", InputActionType.Button, "<Keyboard>/k"));

            EditorSceneManager.SaveScene(escena, RutaEscena);
        }

        // Instancia un prefab existente como hijo de la raíz; si no existe todavía (otra tarea no lo ha creado), lo omite.
        private static void AnidarPrefab(string ruta, Transform padre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                Debug.LogWarning("[Personaje] No existe " + ruta + "; el jugador queda sin él.");
                return;
            }
            PrefabUtility.InstantiatePrefab(prefab, padre);
        }
    }
}
