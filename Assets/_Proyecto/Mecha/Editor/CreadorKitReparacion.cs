using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Kits;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-148 en MechaJugador.prefab (y por herencia en MechaEstacionado.prefab): UsoKitReparacion con su config y la pantalla
    /// de kits a la izquierda de la de vida (espejo de la de calor). Idempotente: rehace la pantalla cada vez.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorKitReparacion.Crear
    /// </summary>
    public static class CreadorKitReparacion
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfigMecha = "Assets/_Proyecto/Mecha/Datos/ConfigKitReparacion_Base.asset";
        private const string RutaConfigPantalla = "Assets/_Proyecto/UI/Datos/ConfigPantallaKits_Base.asset";
        private const string RutaObjetoKit = "Assets/_Proyecto/Economia/Datos/DatosObjeto_KitReparacion.asset";
        private const string NombrePantalla = "PantallaKits";

        // Espejo de la pantalla de calor (CreadorCalorMecha): mismo alto, distancia e inclinación, a la izquierda de la de vida.
        private const float AlturaCamara = 3.5f;
        private const float XPantalla = -0.24f;
        private const float YPantalla = -0.245f;
        private const float ZPantalla = 0.6f;
        private const float InclinacionPantalla = 60f;
        private const float SeparacionDelTablero = -0.003f;

        // Canvas de 160 x 60 unidades a 0.001 m por unidad (0.16 x 0.06 m).
        private const float AnchoCanvas = 160f;
        private const float AltoCanvas = 60f;
        private const float EscalaCanvas = 0.001f;
        private const float MargenTexto = 6f;
        private const int TamanoMinimo = 10;
        private const int TamanoMaximo = 26;

        [MenuItem("Mecha Cyberpunk/Crear T-148 kits de reparación")]
        public static void Crear()
        {
            ConfigKitReparacion configMecha = CrearAsset<ConfigKitReparacion>(RutaConfigMecha);
            ConfigPantallaKits configPantalla = CrearAsset<ConfigPantallaKits>(RutaConfigPantalla);
            AssetDatabase.SaveAssets();
            DatosObjeto kit = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaObjetoKit);
            if (kit == null)
            {
                Debug.LogError("[Mecha] Falta " + RutaObjetoKit + " (corre CreadorInventario antes).");
                return;
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                UsoKitReparacion uso = raiz.GetComponent<UsoKitReparacion>();
                if (uso == null)
                {
                    uso = raiz.AddComponent<UsoKitReparacion>();
                }
                SerializedObject soUso = new SerializedObject(uso);
                soUso.FindProperty("config").objectReferenceValue = configMecha;
                soUso.FindProperty("objetoKit").objectReferenceValue = kit;
                soUso.ApplyModifiedPropertiesWithoutUndo();

                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                Transform vieja = cabina.Find(NombrePantalla);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                ArmarPantalla(cabina, uso, raiz.GetComponent<EncendidoMecha>(), configPantalla);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-148 kits de reparación creado.");
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

        private static void ArmarPantalla(Transform cabina, UsoKitReparacion uso, EncendidoMecha encendido, ConfigPantallaKits config)
        {
            GameObject objeto = new GameObject(NombrePantalla, typeof(RectTransform));
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(XPantalla, AlturaCamara + YPantalla, ZPantalla + SeparacionDelTablero);
            objeto.transform.localRotation = Quaternion.Euler(InclinacionPantalla, 0f, 0f);
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
            texto.alignment = TextAnchor.MiddleCenter;
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = TamanoMinimo;
            texto.resizeTextMaxSize = TamanoMaximo;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.text = string.Format(config.TextoKits, 0);
            texto.raycastTarget = false;

            PantallaKitsCabina pantalla = objeto.AddComponent<PantallaKitsCabina>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("uso").objectReferenceValue = uso;
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
