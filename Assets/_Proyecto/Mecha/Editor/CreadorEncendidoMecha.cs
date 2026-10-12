using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma el encendido (T-065) en MechaJugador.prefab (y por herencia en MechaEstacionado.prefab): acciones Confirmar, Levantar y
    /// Cristal en ControlesMecha, persiana, pantalla de encendido y EncendidoMecha. Idempotente: no regenera la entrada.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorEncendidoMecha.Crear
    /// </summary>
    public static class CreadorEncendidoMecha
    {
        private const string RutaMecha = "Assets/_Proyecto/Mecha";
        private const string RutaPrefab = RutaMecha + "/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = RutaMecha + "/Datos/ConfigEncendido_Base.asset";
        private const string RutaMaterialPersiana = RutaMecha + "/Materiales/Cabina_Persiana.mat";
        private const string RutaConfigPantalla = "Assets/_Proyecto/UI/Datos/ConfigPantallaEncendido_Base.asset";

        private const string AccionConfirmar = "Confirmar";
        private const string AccionLevantar = "Levantar";
        private const string AccionCristal = "Cristal";

        // Medidas en metros relativas a la cámara en reposo (como CreadorCabina).
        private const float AlturaCamara = 3.5f;
        private const float DistanciaPersiana = 0.86f;
        private const float AnchoPersiana = 1.8f;
        private const float AltoPersiana = 1.1f;
        private const float AlturaPantalla = -0.12f;
        private const float DistanciaPantalla = 0.7f;
        private const float InclinacionPantalla = 10f;

        // Canvas de 800 x 200 unidades a 0.001 m por unidad.
        private const float AnchoCanvas = 800f;
        private const float AltoCanvas = 200f;
        private const float EscalaCanvas = 0.001f;
        private const int TamanoTexto = 44;

        [MenuItem("Mecha Cyberpunk/Crear T-065 encendido")]
        public static void Crear()
        {
            EntradaMechaEditor.AgregarAccion(AccionConfirmar, "<Keyboard>/f");
            EntradaMechaEditor.AgregarAccion(AccionLevantar, "<Keyboard>/space");
            EntradaMechaEditor.AgregarAccion(AccionCristal, "<Keyboard>/q");

            ConfigEncendido config = CrearAsset<ConfigEncendido>(RutaConfig);
            ConfigPantallaEncendido configPantalla = CrearAsset<ConfigPantallaEncendido>(RutaConfigPantalla);
            Material materialPersiana = CrearMaterialPersiana();
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                Transform persiana = ArmarPersiana(cabina, materialPersiana);
                EncendidoMecha encendido = ArmarEncendido(raiz, config, cabina, persiana);
                ArmarPantalla(cabina, encendido, configPantalla);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-065 encendido creado.");
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

        private static Material CrearMaterialPersiana()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialPersiana);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, RutaMaterialPersiana);
            }
            material.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.025f));
            material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform ArmarPersiana(Transform cabina, Material material)
        {
            Transform existente = cabina.Find("Persiana");
            if (existente != null)
            {
                return existente;
            }
            GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Quad);
            objeto.name = "Persiana";
            Object.DestroyImmediate(objeto.GetComponent<Collider>());
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(0f, AlturaCamara, DistanciaPersiana);
            objeto.transform.localScale = new Vector3(AnchoPersiana, AltoPersiana, 1f);
            MeshRenderer render = objeto.GetComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return objeto.transform;
        }

        private static EncendidoMecha ArmarEncendido(GameObject raiz, ConfigEncendido config, Transform cabina, Transform persiana)
        {
            EncendidoMecha encendido = raiz.GetComponent<EncendidoMecha>();
            if (encendido == null)
            {
                encendido = raiz.AddComponent<EncendidoMecha>();
            }
            SerializedObject so = new SerializedObject(encendido);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("confirmar").objectReferenceValue = EntradaMechaEditor.Referencia(AccionConfirmar);
            so.FindProperty("levantar").objectReferenceValue = EntradaMechaEditor.Referencia(AccionLevantar);
            so.FindProperty("cristal").objectReferenceValue = EntradaMechaEditor.Referencia(AccionCristal);
            so.FindProperty("cabina").objectReferenceValue = raiz.GetComponentInChildren<SacudidaCabina>(true);
            so.FindProperty("persiana").objectReferenceValue = persiana;
            so.FindProperty("pantallaVida").objectReferenceValue = cabina.Find("AnclaPantallaVida/PantallaVida").gameObject;
            SerializedProperty luces = so.FindProperty("luces");
            luces.arraySize = 2;
            luces.GetArrayElementAtIndex(0).objectReferenceValue = cabina.Find("NeonViga").GetComponent<Renderer>();
            luces.GetArrayElementAtIndex(1).objectReferenceValue = cabina.Find("NeonTablero").GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return encendido;
        }

        private static void ArmarPantalla(Transform cabina, EncendidoMecha encendido, ConfigPantallaEncendido config)
        {
            Transform vieja = cabina.Find("PantallaEncendido");
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }

            GameObject objeto = new GameObject("PantallaEncendido", typeof(RectTransform));
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(0f, AlturaCamara + AlturaPantalla, DistanciaPantalla);
            objeto.transform.localRotation = Quaternion.Euler(InclinacionPantalla, 0f, 0f);
            objeto.transform.localScale = Vector3.one * EscalaCanvas;
            RectTransform rectCanvas = (RectTransform)objeto.transform;
            rectCanvas.sizeDelta = new Vector2(AnchoCanvas, AltoCanvas);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Image fondo = CrearImagen("Fondo", rectCanvas, Vector2.zero, Vector2.one);

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.SetParent(rectCanvas, false);
            Estirar(rectTexto, new Vector2(0.04f, 0.4f), new Vector2(0.96f, 0.96f));
            Text texto = objetoTexto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = TamanoTexto;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.text = "PASO 1/3";
            texto.raycastTarget = false;

            Image hueco = CrearImagen("Hueco", rectCanvas, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.3f));
            Image relleno = CrearImagen("Relleno", (RectTransform)hueco.transform, Vector2.zero, Vector2.one);

            PantallaEncendido pantalla = objeto.AddComponent<PantallaEncendido>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("encendido").objectReferenceValue = encendido;
            so.FindProperty("lienzo").objectReferenceValue = canvas;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("hueco").objectReferenceValue = hueco;
            so.FindProperty("relleno").objectReferenceValue = relleno;
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image CrearImagen(string nombre, RectTransform padre, Vector2 anclaMin, Vector2 anclaMax)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            Estirar(rect, anclaMin, anclaMax);
            Image imagen = objeto.AddComponent<Image>();
            imagen.raycastTarget = false;
            return imagen;
        }

        private static void Estirar(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax)
        {
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
