using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-170: la pantalla de piezas de la cabina en MechaJugador.prefab (por herencia llega a MechaEstacionado), arriba a la izquierda,
    /// fuera del 40 % central de la vista. Idempotente: si la pantalla ya existe no se rehace.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaPiezasCabina.Crear
    /// </summary>
    public static class CreadorPantallaPiezasCabina
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaPiezasCabina_Base.asset";
        private const string NombrePantalla = "PantallaPiezas";

        // Posición relativa a la cámara en reposo: arriba a la izquierda, delante del marco del cristal (que está a 0.84 m).
        private const float AlturaCamara = 3.5f;
        private const float XPantalla = -0.5f;
        private const float YPantalla = 0.28f;
        private const float ZPantalla = 0.8f;

        // Canvas de 240 x 150 unidades a 0.001 m por unidad (0.24 x 0.15 m).
        private const float AnchoCanvas = 240f;
        private const float AltoCanvas = 150f;
        private const float EscalaCanvas = 0.001f;
        private const float MargenTexto = 8f;
        private const int TamanoMinimo = 8;
        private const int TamanoMaximo = 22;

        [MenuItem("Mecha Cyberpunk/Crear T-170 pantalla de piezas de la cabina")]
        public static void Crear()
        {
            ConfigPantallaPiezasCabina config = AssetDatabase.LoadAssetAtPath<ConfigPantallaPiezasCabina>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaPiezasCabina>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                if (cabina.Find(NombrePantalla) == null)
                {
                    ArmarPantalla(cabina, raiz.GetComponent<EncendidoMecha>(), config);
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-170 pantalla de piezas de la cabina creada.");
        }

        private static void ArmarPantalla(Transform cabina, EncendidoMecha encendido, ConfigPantallaPiezasCabina config)
        {
            GameObject objeto = new GameObject(NombrePantalla, typeof(RectTransform));
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(XPantalla, AlturaCamara + YPantalla, ZPantalla);
            objeto.transform.localScale = Vector3.one * EscalaCanvas;
            RectTransform rectCanvas = (RectTransform)objeto.transform;
            rectCanvas.sizeDelta = new Vector2(AnchoCanvas, AltoCanvas);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject objetoFondo = new GameObject("Fondo", typeof(RectTransform));
            RectTransform rectFondo = (RectTransform)objetoFondo.transform;
            rectFondo.SetParent(rectCanvas, false);
            Estirar(rectFondo, 0f);
            Image fondo = objetoFondo.AddComponent<Image>();
            fondo.raycastTarget = false;

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.SetParent(rectCanvas, false);
            Estirar(rectTexto, MargenTexto);
            Text texto = objetoTexto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.alignment = TextAnchor.MiddleLeft;
            texto.supportRichText = true;
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = TamanoMinimo;
            texto.resizeTextMaxSize = TamanoMaximo;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.raycastTarget = false;

            PantallaPiezasCabina pantalla = objeto.AddComponent<PantallaPiezasCabina>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("encendido").objectReferenceValue = encendido;
            so.FindProperty("lienzo").objectReferenceValue = canvas;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Estirar(RectTransform rect, float margen)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margen, margen);
            rect.offsetMax = new Vector2(-margen, -margen);
        }
    }
}
