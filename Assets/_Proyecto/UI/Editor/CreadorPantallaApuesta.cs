using System.IO;
using MechaCyberpunk.UI.Peleas;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-145: ConfigPantallaApuesta_Base.asset y UI/Prefabs/PantallaApuesta.prefab (idempotente: si ya existen no los toca).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaApuesta.Crear
    /// </summary>
    public static class CreadorPantallaApuesta
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaApuesta_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallaApuesta.prefab";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenDibujo = 30000;
        private const float AltoLinea = 90f;
        private const float AltoTitulo = 140f;
        private const float AltoFila = 100f;
        private const float SeparacionFila = 70f;
        private const float YTitulo = 380f;
        private const float YRival = 250f;
        private const float YPremio = 160f;
        private const float YMomio = 80f;
        private const float YFila = -40f;
        private const float YPieza = -150f;
        private const float YAviso = -240f;
        private const float YAyuda = -450f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);

        [MenuItem("Mecha Cyberpunk/Crear T-145 pantalla de apuesta")]
        public static void Crear()
        {
            if (!Directory.Exists("Assets/_Proyecto/UI/Prefabs"))
            {
                Directory.CreateDirectory("Assets/_Proyecto/UI/Prefabs");
                AssetDatabase.Refresh();
            }
            ConfigPantallaApuesta config = AssetDatabase.LoadAssetAtPath<ConfigPantallaApuesta>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaApuesta>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                CrearPrefab(config);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-145 pantalla de apuesta creada.");
        }

        private static void CrearPrefab(ConfigPantallaApuesta config)
        {
            GameObject raiz = new GameObject("PantallaApuesta", typeof(RectTransform));
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
                Text rival = CrearTexto("Rival", contenido.transform, YRival, AltoLinea);
                Text premio = CrearTexto("Premio", contenido.transform, YPremio, AltoLinea);
                Text momio = CrearTexto("Momio", contenido.transform, YMomio, AltoLinea);

                GameObject fila = Hijo("FilaMontos", contenido.transform);
                RectTransform rectFila = (RectTransform)fila.transform;
                rectFila.anchorMin = new Vector2(0f, 0.5f);
                rectFila.anchorMax = new Vector2(1f, 0.5f);
                rectFila.pivot = new Vector2(0.5f, 0.5f);
                rectFila.anchoredPosition = new Vector2(0f, YFila);
                rectFila.sizeDelta = new Vector2(0f, AltoFila);
                HorizontalLayoutGroup grupo = fila.AddComponent<HorizontalLayoutGroup>();
                grupo.childAlignment = TextAnchor.MiddleCenter;
                grupo.spacing = SeparacionFila;
                grupo.childControlWidth = true;
                grupo.childControlHeight = true;
                grupo.childForceExpandWidth = false;
                grupo.childForceExpandHeight = false;

                Text plantilla = CrearTexto("PlantillaMonto", fila.transform, 0f, AltoFila);
                RectTransform rectPlantilla = plantilla.rectTransform;
                rectPlantilla.anchorMin = new Vector2(0.5f, 0.5f);
                rectPlantilla.anchorMax = new Vector2(0.5f, 0.5f);
                plantilla.horizontalOverflow = HorizontalWrapMode.Overflow;
                plantilla.gameObject.SetActive(false);

                Text pieza = CrearTexto("Pieza", contenido.transform, YPieza, AltoLinea);
                Text aviso = CrearTexto("Aviso", contenido.transform, YAviso, AltoLinea);
                Text ayuda = CrearTexto("Ayuda", contenido.transform, YAyuda, AltoLinea);

                PantallaApuesta pantalla = raiz.AddComponent<PantallaApuesta>();
                SerializedObject so = new SerializedObject(pantalla);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("fondo").objectReferenceValue = fondo;
                so.FindProperty("textoTitulo").objectReferenceValue = titulo;
                so.FindProperty("textoRival").objectReferenceValue = rival;
                so.FindProperty("textoPremio").objectReferenceValue = premio;
                so.FindProperty("textoMomio").objectReferenceValue = momio;
                so.FindProperty("filaMontos").objectReferenceValue = rectFila;
                so.FindProperty("plantillaMonto").objectReferenceValue = plantilla;
                so.FindProperty("textoPieza").objectReferenceValue = pieza;
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
