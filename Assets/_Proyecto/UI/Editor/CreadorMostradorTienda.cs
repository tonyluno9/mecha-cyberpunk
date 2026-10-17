using MechaCyberpunk.UI.Tiendas;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-146: ConfigPantallaTienda_Base.asset, UI/Prefabs/PantallaTienda.prefab y UI/Prefabs/MostradorMercadoNegro.prefab
    /// (mesa y letrero greybox con MostradorTienda y DatosTienda_MercadoNegro). Idempotente: si ya existen no los toca.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorMostradorTienda.Crear
    /// </summary>
    public static class CreadorMostradorTienda
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaTienda_Base.asset";
        public const string RutaPantalla = "Assets/_Proyecto/UI/Prefabs/PantallaTienda.prefab";
        public const string RutaMostrador = "Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab";

        private const string RutaDatosTienda = "Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset";
        private const string RutaCatalogo = "Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset";
        private const string CarpetaMateriales = "Assets/_Proyecto/UI/Materiales";
        private const string RutaMaterialMesa = CarpetaMateriales + "/MercadoNegro_Mesa.mat";
        private const string RutaMaterialLetrero = CarpetaMateriales + "/MercadoNegro_Letrero.mat";
        private const string TextoLetrero = "MERCADO NEGRO";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenDibujo = 30000;
        private const float AltoLinea = 90f;
        private const float AltoTitulo = 130f;
        private const float AltoFila = 110f;
        private const float YTitulo = 400f;
        private const float YDinero = 290f;
        private const float YPestanas = 190f;
        private const float XPestanas = 260f;
        private const float YLista = 20f;
        private const float AltoLista = 330f;
        private const float YAviso = -230f;
        private const float YAyuda = -450f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);
        private static readonly Color ColorMesa = new Color(0.16f, 0.07f, 0.2f);
        private static readonly Color ColorLetrero = new Color(1f, 0.1f, 0.7f);
        private static readonly Vector3 TamanoMesa = new Vector3(2.4f, 1.1f, 0.9f);
        private static readonly Vector3 TamanoLetrero = new Vector3(2.6f, 0.6f, 0.08f);
        private const float AlturaLetrero = 2.3f;
        private const float TamanoLetra = 0.035f;

        [MenuItem("Mecha Cyberpunk/Crear T-146 mostrador de tienda")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Proyecto/UI/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/UI", "Prefabs");
            }
            ConfigPantallaTienda config = AssetDatabase.LoadAssetAtPath<ConfigPantallaTienda>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaTienda>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantalla) == null)
            {
                CrearPantalla(config);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostrador) == null)
            {
                CrearMostrador();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-146 mostrador de tienda creado.");
        }

        private static void CrearPantalla(ConfigPantallaTienda config)
        {
            GameObject raiz = new GameObject("PantallaTienda", typeof(RectTransform));
            try
            {
                Canvas canvas = raiz.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = OrdenDibujo;
                CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
                escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
                escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                escala.matchWidthOrHeight = 0.5f;

                GameObject contenido = Hijo("Contenido", raiz.transform);
                Estirar(contenido.GetComponent<RectTransform>());
                Image fondo = CrearImagen("Fondo", contenido.transform, config.ColorFondo);
                Estirar(fondo.rectTransform);

                Text titulo = CrearTexto("Titulo", contenido.transform, 0f, YTitulo, AltoTitulo, 1f);
                Text dinero = CrearTexto("Dinero", contenido.transform, 0f, YDinero, AltoLinea, 1f);
                Text pestanaComprar = CrearTexto("PestanaComprar", contenido.transform, -XPestanas, YPestanas, AltoLinea, 0.5f);
                Text pestanaVender = CrearTexto("PestanaVender", contenido.transform, XPestanas, YPestanas, AltoLinea, 0.5f);

                GameObject lista = Hijo("ListaFilas", contenido.transform);
                RectTransform rectLista = (RectTransform)lista.transform;
                rectLista.anchorMin = new Vector2(0f, 0.5f);
                rectLista.anchorMax = new Vector2(1f, 0.5f);
                rectLista.pivot = new Vector2(0.5f, 0.5f);
                rectLista.anchoredPosition = new Vector2(0f, YLista);
                rectLista.sizeDelta = new Vector2(0f, AltoLista);
                VerticalLayoutGroup grupo = lista.AddComponent<VerticalLayoutGroup>();
                grupo.childAlignment = TextAnchor.UpperCenter;
                grupo.spacing = 10f;
                grupo.childControlWidth = true;
                grupo.childControlHeight = true;
                grupo.childForceExpandWidth = true;
                grupo.childForceExpandHeight = false;

                Text plantilla = CrearTexto("PlantillaFila", lista.transform, 0f, 0f, AltoFila, 1f);
                LayoutElement elemento = plantilla.gameObject.AddComponent<LayoutElement>();
                elemento.minHeight = AltoFila;
                elemento.preferredHeight = AltoFila;
                plantilla.gameObject.SetActive(false);

                Text aviso = CrearTexto("Aviso", contenido.transform, 0f, YAviso, AltoLinea, 1f);
                Text ayuda = CrearTexto("Ayuda", contenido.transform, 0f, YAyuda, AltoLinea, 1f);

                PantallaTienda pantalla = raiz.AddComponent<PantallaTienda>();
                SerializedObject so = new SerializedObject(pantalla);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("fondo").objectReferenceValue = fondo;
                so.FindProperty("textoTitulo").objectReferenceValue = titulo;
                so.FindProperty("textoDinero").objectReferenceValue = dinero;
                so.FindProperty("textoPestanaComprar").objectReferenceValue = pestanaComprar;
                so.FindProperty("textoPestanaVender").objectReferenceValue = pestanaVender;
                so.FindProperty("listaFilas").objectReferenceValue = rectLista;
                so.FindProperty("plantillaFila").objectReferenceValue = plantilla;
                so.FindProperty("textoAviso").objectReferenceValue = aviso;
                so.FindProperty("textoAyuda").objectReferenceValue = ayuda;
                so.ApplyModifiedPropertiesWithoutUndo();
                contenido.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPantalla);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void CrearMostrador()
        {
            Object datosTienda = AssetDatabase.LoadAssetAtPath<Object>(RutaDatosTienda);
            Object catalogo = AssetDatabase.LoadAssetAtPath<Object>(RutaCatalogo);
            GameObject prefabPantalla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantalla);
            if (datosTienda == null || catalogo == null || prefabPantalla == null)
            {
                Debug.LogError("[UI] T-146: faltan DatosTienda_MercadoNegro, CatalogoObjetos_Base o PantallaTienda.prefab.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/UI", "Materiales");
            }
            Material mesaMat = CrearMaterial(RutaMaterialMesa, "Universal Render Pipeline/Lit", ColorMesa);
            Material letreroMat = CrearMaterial(RutaMaterialLetrero, "Universal Render Pipeline/Unlit", ColorLetrero);

            GameObject raiz = new GameObject("MostradorMercadoNegro");
            try
            {
                Caja("Mesa", raiz.transform, new Vector3(0f, TamanoMesa.y / 2f, 0f), TamanoMesa, mesaMat);
                Caja("Letrero", raiz.transform, new Vector3(0f, AlturaLetrero, 0.5f), TamanoLetrero, letreroMat);
                GameObject texto = new GameObject("Texto letrero");
                texto.transform.SetParent(raiz.transform, false);
                texto.transform.localPosition = new Vector3(0f, AlturaLetrero, 0.5f - TamanoLetrero.z / 2f - 0.01f);
                TextMesh mesh = texto.AddComponent<TextMesh>();
                mesh.text = TextoLetrero;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.fontSize = 100;
                mesh.characterSize = TamanoLetra;
                mesh.color = Color.white;
                Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                mesh.font = fuente;
                texto.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;

                GameObject pantalla = (GameObject)PrefabUtility.InstantiatePrefab(prefabPantalla, raiz.transform);

                MostradorTienda mostrador = raiz.AddComponent<MostradorTienda>();
                SerializedObject so = new SerializedObject(mostrador);
                so.FindProperty("datos").objectReferenceValue = datosTienda;
                so.FindProperty("catalogo").objectReferenceValue = catalogo;
                so.FindProperty("pantalla").objectReferenceValue = pantalla.GetComponent<PantallaTienda>();
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaMostrador);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static Material CrearMaterial(string ruta, string shader, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, ruta);
            }
            return material;
        }

        private static void Caja(string nombre, Transform padre, Vector3 posicionLocal, Vector3 tamano, Material material)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.SetParent(padre, false);
            caja.transform.localPosition = posicionLocal;
            caja.transform.localScale = tamano;
            caja.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static GameObject Hijo(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            return objeto;
        }

        private static Image CrearImagen(string nombre, Transform padre, Color color)
        {
            GameObject objeto = Hijo(nombre, padre);
            Image imagen = objeto.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        // Texto centrado: ancho = fraccion del lienzo (1 = todo el ancho; 0.5 = mitad), centrado en x.
        private static Text CrearTexto(string nombre, Transform padre, float x, float y, float alto, float fraccionAncho)
        {
            GameObject objeto = Hijo(nombre, padre);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = new Vector2(0.5f - fraccionAncho / 2f, 0.5f);
            rect.anchorMax = new Vector2(0.5f + fraccionAncho / 2f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(0f, alto);
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            Shadow sombra = objeto.AddComponent<Shadow>();
            sombra.effectColor = ColorSombra;
            sombra.effectDistance = DesplazamientoSombra;
            return texto;
        }

        private static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
