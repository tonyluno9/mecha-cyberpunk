using MechaCyberpunk.UI.Taller;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-149: ConfigPantallaBancoTaller_Base.asset, UI/Prefabs/PantallaBancoTaller.prefab y lo anida en
    /// MechaJugador.prefab (MechaEstacionado.prefab lo hereda), así toda escena con el mecha tiene la pantalla sin colocar nada.
    /// Idempotente: lo que ya existe no se toca.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaBancoTaller.Crear
    /// </summary>
    public static class CreadorPantallaBancoTaller
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaBancoTaller_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallaBancoTaller.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string NombreEnMecha = "PantallaBancoTaller";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenDibujo = 30000;
        private const float AltoLinea = 90f;
        private const float AltoTitulo = 140f;
        private const float AltoOpcion = 100f;
        private const float AltoFilaOpciones = 360f;
        private const float YTitulo = 380f;
        private const float YVida = 260f;
        private const float YDinero = 180f;
        private const float YOpciones = -60f;
        private const float YAyuda = -450f;
        private const float SeparacionOpciones = 10f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);

        [MenuItem("Mecha Cyberpunk/Crear T-149 pantalla del banco del taller")]
        public static void Crear()
        {
            ConfigPantallaBancoTaller config = AssetDatabase.LoadAssetAtPath<ConfigPantallaBancoTaller>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaBancoTaller>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                CrearPrefab(config);
            }
            AnidarEnMecha();
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-149 pantalla del banco del taller creada.");
        }

        private static void AnidarEnMecha()
        {
            GameObject pantalla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaMecha);
            try
            {
                if (raiz.transform.Find(NombreEnMecha) != null)
                {
                    return;
                }
                GameObject hijo = (GameObject)PrefabUtility.InstantiatePrefab(pantalla, raiz.transform);
                hijo.name = NombreEnMecha;
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaMecha);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void CrearPrefab(ConfigPantallaBancoTaller config)
        {
            GameObject raiz = new GameObject("PantallaBancoTaller", typeof(RectTransform));
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

                Text titulo = CrearTexto("Titulo", contenido.transform, YTitulo, AltoTitulo);
                Text vida = CrearTexto("Vida", contenido.transform, YVida, AltoLinea);
                Text dinero = CrearTexto("Dinero", contenido.transform, YDinero, AltoLinea);

                GameObject fila = Hijo("Opciones", contenido.transform);
                RectTransform rectFila = (RectTransform)fila.transform;
                rectFila.anchorMin = new Vector2(0f, 0.5f);
                rectFila.anchorMax = new Vector2(1f, 0.5f);
                rectFila.pivot = new Vector2(0.5f, 0.5f);
                rectFila.anchoredPosition = new Vector2(0f, YOpciones);
                rectFila.sizeDelta = new Vector2(0f, AltoFilaOpciones);
                VerticalLayoutGroup grupo = fila.AddComponent<VerticalLayoutGroup>();
                grupo.childAlignment = TextAnchor.UpperCenter;
                grupo.spacing = SeparacionOpciones;
                grupo.childControlWidth = true;
                grupo.childControlHeight = true;
                grupo.childForceExpandWidth = true;
                grupo.childForceExpandHeight = false;

                Text plantilla = CrearTexto("PlantillaOpcion", fila.transform, 0f, AltoOpcion);
                LayoutElement elemento = plantilla.gameObject.AddComponent<LayoutElement>();
                elemento.preferredHeight = AltoOpcion;
                plantilla.gameObject.SetActive(false);

                Text ayuda = CrearTexto("Ayuda", contenido.transform, YAyuda, AltoLinea);

                PantallaBancoTaller pantalla = raiz.AddComponent<PantallaBancoTaller>();
                SerializedObject so = new SerializedObject(pantalla);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("fondo").objectReferenceValue = fondo;
                so.FindProperty("textoTitulo").objectReferenceValue = titulo;
                so.FindProperty("textoVida").objectReferenceValue = vida;
                so.FindProperty("textoDinero").objectReferenceValue = dinero;
                so.FindProperty("filaOpciones").objectReferenceValue = rectFila;
                so.FindProperty("plantillaOpcion").objectReferenceValue = plantilla;
                so.FindProperty("textoAyuda").objectReferenceValue = ayuda;
                so.ApplyModifiedPropertiesWithoutUndo();
                contenido.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
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

        private static Text CrearTexto(string nombre, Transform padre, float y, float alto)
        {
            GameObject objeto = Hijo(nombre, padre);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
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
