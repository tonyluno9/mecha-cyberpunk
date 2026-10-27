using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-194: la pantalla de OJOS de la cabina en MechaJugador.prefab (por herencia llega a MechaEstacionado), a la izquierda y bajo la
    /// de piezas, fuera del 40 % central de la vista. Idempotente: si la pantalla ya existe no se rehace.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaOjosCabina.Crear
    /// </summary>
    public static class CreadorPantallaOjosCabina
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaOjos_Base.asset";
        private const string NombrePantalla = "PantallaOjos";

        private const float AlturaCamara = 3.5f;
        private const float XPantalla = -0.5f;
        private const float YPantalla = 0.08f;
        private const float ZPantalla = 0.8f;

        // Canvas de 240 x 150 unidades a 0.001 m por unidad (0.24 x 0.15 m).
        private const float AnchoCanvas = 240f;
        private const float AltoCanvas = 150f;
        private const float EscalaCanvas = 0.001f;
        private const float Margen = 8f;
        private const float AltoNombre = 30f;
        private const float AltoBarra = 18f;
        private const float AltoNumeros = 26f;
        private const float AltoAviso = 34f;
        private const float Separacion = 6f;
        private const int TamanoMinimo = 8;
        private const int TamanoMaximo = 22;

        [MenuItem("Mecha Cyberpunk/Crear T-194 pantalla de ojos de la cabina")]
        public static void Crear()
        {
            ConfigPantallaOjos config = AssetDatabase.LoadAssetAtPath<ConfigPantallaOjos>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaOjos>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                if (cabina.Find(NombrePantalla) == null)
                {
                    ArmarPantalla(cabina, raiz.GetComponent<EncendidoMecha>(), raiz.GetComponentInChildren<Camera>(true), config);
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-194 pantalla de ojos de la cabina creada.");
        }

        private static void ArmarPantalla(Transform cabina, EncendidoMecha encendido, Camera camara, ConfigPantallaOjos config)
        {
            GameObject objeto = new GameObject(NombrePantalla, typeof(RectTransform));
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(XPantalla, AlturaCamara + YPantalla, ZPantalla);
            objeto.transform.localScale = Vector3.one * EscalaCanvas;
            RectTransform rectCanvas = (RectTransform)objeto.transform;
            rectCanvas.sizeDelta = new Vector2(AnchoCanvas, AltoCanvas);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Image fondo = NuevaImagen("Fondo", rectCanvas);
            Estirar(fondo.rectTransform, 0f, 0f, 0f, 0f);

            float y = Margen;
            Text nombre = NuevoTexto("Nombre", rectCanvas, TextAnchor.MiddleLeft);
            Fila(nombre.rectTransform, y, AltoNombre);
            y += AltoNombre + Separacion;

            Image fondoBarra = NuevaImagen("FondoBarra", rectCanvas);
            Fila(fondoBarra.rectTransform, y, AltoBarra);
            Image relleno = NuevaImagen("Relleno", fondoBarra.rectTransform);
            relleno.rectTransform.anchorMin = Vector2.zero;
            relleno.rectTransform.anchorMax = Vector2.one;
            relleno.rectTransform.offsetMin = Vector2.zero;
            relleno.rectTransform.offsetMax = Vector2.zero;
            y += AltoBarra + Separacion;

            Text numeros = NuevoTexto("Numeros", rectCanvas, TextAnchor.MiddleCenter);
            Fila(numeros.rectTransform, y, AltoNumeros);
            y += AltoNumeros + Separacion;

            Text aviso = NuevoTexto("Aviso", rectCanvas, TextAnchor.MiddleCenter);
            Fila(aviso.rectTransform, y, AltoAviso);

            PantallaOjosCabina pantalla = objeto.AddComponent<PantallaOjosCabina>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("encendido").objectReferenceValue = encendido;
            so.FindProperty("camara").objectReferenceValue = camara;
            so.FindProperty("lienzo").objectReferenceValue = canvas;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("textoNombre").objectReferenceValue = nombre;
            so.FindProperty("fondoBarra").objectReferenceValue = fondoBarra;
            so.FindProperty("relleno").objectReferenceValue = relleno;
            so.FindProperty("textoNumeros").objectReferenceValue = numeros;
            so.FindProperty("textoAviso").objectReferenceValue = aviso;
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image NuevaImagen(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Image imagen = objeto.AddComponent<Image>();
            imagen.raycastTarget = false;
            return imagen;
        }

        private static Text NuevoTexto(string nombre, Transform padre, TextAnchor alineacion)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.alignment = alineacion;
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = TamanoMinimo;
            texto.resizeTextMaxSize = TamanoMaximo;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.raycastTarget = false;
            return texto;
        }

        // Una fila de alto fijo que empieza a "desdeArriba" unidades del borde superior.
        private static void Fila(RectTransform rect, float desdeArriba, float alto)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(Margen, -(desdeArriba + alto));
            rect.offsetMax = new Vector2(-Margen, -desdeArriba);
        }

        private static void Estirar(RectTransform rect, float izq, float abajo, float der, float arriba)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(izq, abajo);
            rect.offsetMax = new Vector2(-der, -arriba);
        }
    }
}
