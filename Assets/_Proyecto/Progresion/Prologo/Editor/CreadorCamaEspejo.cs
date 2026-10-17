using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.UI.Historia;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Crea lo de T-130: CamaJugador.prefab (una cama greybox con el punto de uso a su lado) y EspejoJugador.prefab (un marco con
    /// superficie brillante, la vista previa de apariencia y la pantalla del espejo). La raíz de cada prefab es el punto de uso.
    /// Idempotente. Necesita PantallasHistoria.prefab, VistaPreviaApariencia.prefab y DatosEscenas_Demo.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCamaEspejo.Crear
    /// </summary>
    public static class CreadorCamaEspejo
    {
        public const string RutaPrefabCama = "Assets/_Proyecto/Progresion/Prologo/Prefabs/CamaJugador.prefab";
        public const string RutaPrefabEspejo = "Assets/_Proyecto/Progresion/Prologo/Prefabs/EspejoJugador.prefab";

        private const string RutaMateriales = "Assets/_Proyecto/Progresion/Prologo/Materiales";
        private const string RutaPantallasHistoria = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaVista = "Assets/_Proyecto/Personaje/Prefabs/VistaPreviaApariencia.prefab";
        private const string RutaDatosEscenas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenCanvas = 20000;
        private static readonly Vector2 TamanoImagen = new Vector2(480f, 720f);
        private static readonly Vector2 PosicionImagen = new Vector2(-380f, 0f);
        private static readonly Color ColorFondo = new Color(0.03f, 0.03f, 0.06f, 0.92f);
        private static readonly Color ColorAlias = new Color(0f, 1f, 0.9f, 1f);
        private static readonly Color ColorSalir = new Color(0.9f, 0.95f, 1f, 1f);
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);

        [MenuItem("Mecha Cyberpunk/Crear T-130 cama y espejo")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Proyecto/Progresion/Prologo/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Prologo", "Prefabs");
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCama) == null)
            {
                CrearCama();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEspejo) == null)
            {
                CrearEspejo();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-130 cama y espejo creados.");
        }

        private static void CrearCama()
        {
            Material colchon = CrearMaterial("Prologo_Cama", new Color(0.35f, 0.2f, 0.55f), 0.2f, 0f);
            Material madera = CrearMaterial("Prologo_CamaBase", new Color(0.3f, 0.18f, 0.1f), 0.2f, 0f);

            GameObject raiz = new GameObject("CamaJugador");
            try
            {
                Caja("Base", raiz.transform, new Vector3(-1f, 0.2f, -0.5f), new Vector3(1f, 0.4f, 2f), madera, true);
                Caja("Colchón", raiz.transform, new Vector3(-1f, 0.5f, -0.5f), new Vector3(0.9f, 0.2f, 1.9f), colchon, false);
                Caja("Almohada", raiz.transform, new Vector3(-1f, 0.67f, 0.3f), new Vector3(0.6f, 0.15f, 0.35f), colchon, false);

                CamaJugador cama = raiz.AddComponent<CamaJugador>();
                SerializedObject so = new SerializedObject(cama);
                so.FindProperty("datosEscenas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenas);
                so.FindProperty("prefabPantallas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallasHistoria);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabCama);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void CrearEspejo()
        {
            Material marco = CrearMaterial("Prologo_EspejoMarco", new Color(0.15f, 0.15f, 0.2f), 0.3f, 0f);
            Material superficie = CrearMaterial("Prologo_EspejoSuperficie", new Color(0.7f, 0.85f, 0.95f), 0.95f, 0.9f);

            GameObject raiz = new GameObject("EspejoJugador");
            try
            {
                // El marco está 1.5 m delante del punto de uso, con la superficie hacia él (-Z).
                Caja("Marco", raiz.transform, new Vector3(0f, 1.2f, 1.5f), new Vector3(1.2f, 2f, 0.1f), marco, true);
                Caja("Superficie", raiz.transform, new Vector3(0f, 1.2f, 1.44f), new Vector3(1f, 1.8f, 0.02f), superficie, false);

                GameObject vistaObjeto = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVista));
                vistaObjeto.transform.SetParent(raiz.transform, false);
                VistaPreviaApariencia vista = vistaObjeto.GetComponent<VistaPreviaApariencia>();

                PantallaEspejo pantalla = CrearPantalla(raiz.transform);

                EspejoJugador espejo = raiz.AddComponent<EspejoJugador>();
                SerializedObject so = new SerializedObject(espejo);
                so.FindProperty("pantalla").objectReferenceValue = pantalla;
                so.FindProperty("vista").objectReferenceValue = vista;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabEspejo);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static PantallaEspejo CrearPantalla(Transform padre)
        {
            GameObject raiz = Hijo("PantallaEspejo", padre);
            Canvas canvas = raiz.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OrdenCanvas;
            CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escala.matchWidthOrHeight = 0.5f;
            raiz.AddComponent<GraphicRaycaster>();

            GameObject contenido = Hijo("Contenido", raiz.transform);
            Estirar((RectTransform)contenido.transform);
            Image fondo = contenido.AddComponent<Image>();
            fondo.color = ColorFondo;

            GameObject imagenObjeto = Hijo("Reflejo", contenido.transform);
            RectTransform rectImagen = (RectTransform)imagenObjeto.transform;
            rectImagen.anchorMin = rectImagen.anchorMax = rectImagen.pivot = new Vector2(0.5f, 0.5f);
            rectImagen.anchoredPosition = PosicionImagen;
            rectImagen.sizeDelta = TamanoImagen;
            RawImage imagen = imagenObjeto.AddComponent<RawImage>();
            imagen.raycastTarget = false;

            Text alias = CrearTexto("Alias", contenido.transform, ColorAlias, 110, new Vector2(300f, 60f), new Vector2(1000f, 200f));
            Text salir = CrearTexto("Salir", contenido.transform, ColorSalir, 40, new Vector2(0f, -470f), new Vector2(900f, 80f));

            PantallaEspejo pantalla = raiz.AddComponent<PantallaEspejo>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("contenido").objectReferenceValue = contenido;
            so.FindProperty("imagen").objectReferenceValue = imagen;
            so.FindProperty("textoAlias").objectReferenceValue = alias;
            so.FindProperty("textoSalir").objectReferenceValue = salir;
            so.ApplyModifiedPropertiesWithoutUndo();
            contenido.SetActive(false);
            return pantalla;
        }

        private static Text CrearTexto(string nombre, Transform padre, Color color, int tamano, Vector2 posicion, Vector2 tamanoRect)
        {
            GameObject objeto = Hijo(nombre, padre);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamanoRect;
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.color = color;
            Shadow sombra = objeto.AddComponent<Shadow>();
            sombra.effectColor = ColorSombra;
            sombra.effectDistance = new Vector2(2f, -2f);
            return texto;
        }

        private static GameObject Hijo(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            return objeto;
        }

        private static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Caja(string nombre, Transform padre, Vector3 posicion, Vector3 tamano, Material material, bool conCollider)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.SetParent(padre, false);
            caja.transform.localPosition = posicion;
            caja.transform.localScale = tamano;
            caja.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!conCollider)
            {
                Object.DestroyImmediate(caja.GetComponent<Collider>());
            }
        }

        private static Material CrearMaterial(string nombre, Color color, float suavidad, float metal)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            material.SetFloat("_Metallic", metal);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }
    }
}
