using System.IO;
using MechaCyberpunk.UI.Peleas;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-160: ConfigPantallaCartelera_Base.asset y UI/Prefabs/PantallaCartelera.prefab (idempotente: si ya existen no los toca).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaCartelera.Crear
    /// </summary>
    public static class CreadorPantallaCartelera
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaCartelera_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallaCartelera.prefab";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenDibujo = 30000;
        private const float AltoEncabezado = 110f;
        private const float AltoFila = 80f;
        private const float AltoAyuda = 70f;
        private const float YEncabezado = 380f;
        private const float YFilas = 60f;
        private const float AltoLista = 500f;
        private const float YAyuda = -450f;
        private const float SeparacionFilas = 24f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);

        [MenuItem("Mecha Cyberpunk/Crear T-160 pantalla de la cartelera")]
        public static void Crear()
        {
            if (!Directory.Exists("Assets/_Proyecto/UI/Prefabs"))
            {
                Directory.CreateDirectory("Assets/_Proyecto/UI/Prefabs");
                AssetDatabase.Refresh();
            }
            ConfigPantallaCartelera config = AssetDatabase.LoadAssetAtPath<ConfigPantallaCartelera>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaCartelera>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                CrearPrefab(config);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-160 pantalla de la cartelera creada.");
        }

        private static void CrearPrefab(ConfigPantallaCartelera config)
        {
            GameObject raiz = new GameObject("PantallaCartelera", typeof(RectTransform));
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

                Text encabezado = CrearTexto("Encabezado", contenido.transform, YEncabezado, AltoEncabezado);

                GameObject lista = Hijo("ListaFilas", contenido.transform);
                RectTransform rectLista = (RectTransform)lista.transform;
                rectLista.anchorMin = new Vector2(0f, 0.5f);
                rectLista.anchorMax = new Vector2(1f, 0.5f);
                rectLista.pivot = new Vector2(0.5f, 0.5f);
                rectLista.anchoredPosition = new Vector2(0f, YFilas);
                rectLista.sizeDelta = new Vector2(0f, AltoLista);
                VerticalLayoutGroup grupo = lista.AddComponent<VerticalLayoutGroup>();
                grupo.childAlignment = TextAnchor.UpperCenter;
                grupo.spacing = SeparacionFilas;
                grupo.childControlWidth = true;
                grupo.childControlHeight = true;
                grupo.childForceExpandWidth = true;
                grupo.childForceExpandHeight = false;

                Text plantilla = CrearTexto("PlantillaFila", lista.transform, 0f, AltoFila);
                plantilla.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                plantilla.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                plantilla.gameObject.SetActive(false);

                Text ayuda = CrearTexto("Ayuda", contenido.transform, YAyuda, AltoAyuda);

                PantallaCartelera pantalla = raiz.AddComponent<PantallaCartelera>();
                SerializedObject so = new SerializedObject(pantalla);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("fondo").objectReferenceValue = fondo;
                so.FindProperty("textoEncabezado").objectReferenceValue = encabezado;
                so.FindProperty("listaFilas").objectReferenceValue = rectLista;
                so.FindProperty("plantillaFila").objectReferenceValue = plantilla;
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
