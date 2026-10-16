using System.IO;
using MechaCyberpunk.UI.Historia;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-125: ConfigPantallasHistoria_Base.asset y UI/Prefabs/PantallasHistoria.prefab (idempotente).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallasHistoria.Crear
    /// </summary>
    public static class CreadorPantallasHistoria
    {
        private const string RutaUI = "Assets/_Proyecto/UI";
        private const string RutaConfig = RutaUI + "/Datos/ConfigPantallasHistoria_Base.asset";
        private const string RutaPrefab = RutaUI + "/Prefabs/PantallasHistoria.prefab";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenContinuara = 32000;
        private const float AltoTitulo = 200f;
        private const float AltoLinea = 60f;
        private const float SeparacionLineas = 20f;
        private const float DesplazamientoTitulo = 150f;
        private const float DesplazamientoTeclas = -300f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);

        [MenuItem("Mecha Cyberpunk/Crear T-125")]
        public static void Crear()
        {
            if (!Directory.Exists(RutaUI + "/Prefabs"))
            {
                Directory.CreateDirectory(RutaUI + "/Prefabs");
                AssetDatabase.Refresh();
            }
            ConfigPantallasHistoria config = AssetDatabase.LoadAssetAtPath<ConfigPantallasHistoria>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallasHistoria>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                CrearPrefab(config);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-125 creada.");
        }

        private static void CrearPrefab(ConfigPantallasHistoria config)
        {
            GameObject raiz = new GameObject("PantallasHistoria");
            try
            {
                // Fundido: canvas Overlay con el orden más alto y una imagen negra transparente.
                GameObject fundido = Hijo("Fundido", raiz.transform);
                Canvas canvasFundido = fundido.AddComponent<Canvas>();
                canvasFundido.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasFundido.overrideSorting = true;
                canvasFundido.sortingOrder = PantallaFundido.OrdenDibujo;
                fundido.AddComponent<GraphicRaycaster>();
                Image negro = CrearImagen("Negro", fundido.transform, new Color(0f, 0f, 0f, 0f));
                Estirar(negro.rectTransform);
                PantallaFundido pantallaFundido = fundido.AddComponent<PantallaFundido>();
                SerializedObject soFundido = new SerializedObject(pantallaFundido);
                soFundido.FindProperty("config").objectReferenceValue = config;
                soFundido.FindProperty("canvas").objectReferenceValue = canvasFundido;
                soFundido.FindProperty("negro").objectReferenceValue = negro;
                soFundido.ApplyModifiedPropertiesWithoutUndo();

                // Continuará: canvas Overlay por debajo del fundido; el contenido empieza oculto.
                GameObject continuara = Hijo("Continuara", raiz.transform);
                Canvas canvasContinuara = continuara.AddComponent<Canvas>();
                canvasContinuara.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasContinuara.overrideSorting = true;
                canvasContinuara.sortingOrder = OrdenContinuara;
                CanvasScaler escala = continuara.AddComponent<CanvasScaler>();
                escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
                escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                escala.matchWidthOrHeight = 0.5f;
                continuara.AddComponent<GraphicRaycaster>();

                GameObject contenido = Hijo("Contenido", continuara.transform);
                Estirar(contenido.GetComponent<RectTransform>());
                Image fondo = CrearImagen("Fondo", contenido.transform, Color.black);
                Estirar(fondo.rectTransform);
                fondo.raycastTarget = true;
                Text titulo = CrearTexto("Titulo", contenido.transform, config.ColorTitulo, config.TamanoTitulo, DesplazamientoTitulo, AltoTitulo);
                float y = 0f;
                Text alias = CrearTexto("Alias", contenido.transform, config.ColorTexto, config.TamanoTexto, y, AltoLinea);
                y -= AltoLinea + SeparacionLineas;
                Text dinero = CrearTexto("Dinero", contenido.transform, config.ColorTexto, config.TamanoTexto, y, AltoLinea);
                y -= AltoLinea + SeparacionLineas;
                Text resultado = CrearTexto("Resultado", contenido.transform, config.ColorTexto, config.TamanoTexto, y, AltoLinea);
                Text teclas = CrearTexto("Teclas", contenido.transform, config.ColorTitulo, config.TamanoTexto, DesplazamientoTeclas, AltoLinea);
                teclas.gameObject.SetActive(false);

                PantallaContinuara pantalla = continuara.AddComponent<PantallaContinuara>();
                SerializedObject so = new SerializedObject(pantalla);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("textoTitulo").objectReferenceValue = titulo;
                so.FindProperty("textoAlias").objectReferenceValue = alias;
                so.FindProperty("textoDinero").objectReferenceValue = dinero;
                so.FindProperty("textoResultado").objectReferenceValue = resultado;
                so.FindProperty("textoTeclas").objectReferenceValue = teclas;
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

        private static Text CrearTexto(string nombre, Transform padre, Color color, int tamano, float y, float alto)
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
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.color = color;
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
