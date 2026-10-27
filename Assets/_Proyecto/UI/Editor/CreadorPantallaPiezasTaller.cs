using MechaCyberpunk.UI.Taller;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-169: ConfigPantallaPiezas_Base.asset, UI/Prefabs/PantallaPiezasTaller.prefab y lo anida en MechaJugador.prefab
    /// (MechaEstacionado.prefab lo hereda), así toda escena con el mecha tiene la pantalla sin colocar nada.
    /// Idempotente: lo que ya existe no se toca.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaPiezasTaller.Crear
    /// </summary>
    public static class CreadorPantallaPiezasTaller
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaPiezas_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallaPiezasTaller.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string NombreEnMecha = "PantallaPiezasTaller";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenDibujo = 30001;
        private const float AltoTitulo = 100f;
        private const float AltoSubtitulo = 50f;
        private const float AltoDinero = 50f;
        private const float AltoFila = 50f;
        private const float AltoLista = 560f;
        private const float AltoEstadisticas = 60f;
        private const float AltoDiferencia = 56f;
        private const float AltoAviso = 50f;
        private const float AltoAyuda = 40f;
        private const float YTitulo = 470f;
        private const float YSubtitulo = 400f;
        private const float YDinero = 345f;
        private const float YLista = 300f;
        private const float YEstadisticas = -340f;
        private const float YDiferencia = -400f;
        private const float YAviso = -455f;
        private const float YAyuda = -510f;
        private const float SeparacionFilas = 4f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);

        [MenuItem("Mecha Cyberpunk/Crear T-169 pantalla de piezas del taller")]
        public static void Crear()
        {
            ConfigPantallaPiezas config = AssetDatabase.LoadAssetAtPath<ConfigPantallaPiezas>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaPiezas>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                CrearPrefab(config);
            }
            AnidarEnMecha();
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-169 pantalla de piezas del taller creada.");
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

        private static void CrearPrefab(ConfigPantallaPiezas config)
        {
            GameObject raiz = new GameObject("PantallaPiezasTaller", typeof(RectTransform));
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
                Text subtitulo = CrearTexto("Subtitulo", contenido.transform, YSubtitulo, AltoSubtitulo);
                Text dinero = CrearTexto("Dinero", contenido.transform, YDinero, AltoDinero);

                GameObject lista = Hijo("Filas", contenido.transform);
                RectTransform rectLista = (RectTransform)lista.transform;
                rectLista.anchorMin = new Vector2(0f, 0.5f);
                rectLista.anchorMax = new Vector2(1f, 0.5f);
                rectLista.pivot = new Vector2(0.5f, 1f);
                rectLista.anchoredPosition = new Vector2(0f, YLista);
                rectLista.sizeDelta = new Vector2(0f, AltoLista);
                VerticalLayoutGroup grupo = lista.AddComponent<VerticalLayoutGroup>();
                grupo.childAlignment = TextAnchor.UpperCenter;
                grupo.spacing = SeparacionFilas;
                grupo.childControlWidth = true;
                grupo.childControlHeight = true;
                grupo.childForceExpandWidth = true;
                grupo.childForceExpandHeight = false;

                Text plantilla = CrearTexto("PlantillaFila", lista.transform, 0f, AltoFila);
                LayoutElement elemento = plantilla.gameObject.AddComponent<LayoutElement>();
                elemento.preferredHeight = AltoFila;
                plantilla.gameObject.SetActive(false);

                Text estadisticas = CrearTexto("Estadisticas", contenido.transform, YEstadisticas, AltoEstadisticas);
                Text diferencia = CrearTexto("Diferencia", contenido.transform, YDiferencia, AltoDiferencia);
                Text aviso = CrearTexto("Aviso", contenido.transform, YAviso, AltoAviso);
                Text ayuda = CrearTexto("Ayuda", contenido.transform, YAyuda, AltoAyuda);

                PantallaPiezasTaller pantalla = raiz.AddComponent<PantallaPiezasTaller>();
                SerializedObject so = new SerializedObject(pantalla);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("fondo").objectReferenceValue = fondo;
                so.FindProperty("textoTitulo").objectReferenceValue = titulo;
                so.FindProperty("textoSubtitulo").objectReferenceValue = subtitulo;
                so.FindProperty("textoDinero").objectReferenceValue = dinero;
                so.FindProperty("listaFilas").objectReferenceValue = rectLista;
                so.FindProperty("plantillaFila").objectReferenceValue = plantilla;
                so.FindProperty("textoEstadisticas").objectReferenceValue = estadisticas;
                so.FindProperty("textoDiferencia").objectReferenceValue = diferencia;
                so.FindProperty("textoAviso").objectReferenceValue = aviso;
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
