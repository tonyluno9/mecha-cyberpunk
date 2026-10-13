using MechaCyberpunk.Mecha.Calor;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-067 en MechaJugador.prefab (y por herencia en MechaEstacionado.prefab): CalorPorMecha con su config y la pantalla
    /// de calor al lado de la de vida. Idempotente: no regenera lo que ya existe.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorCalorMecha.Crear
    /// </summary>
    public static class CreadorCalorMecha
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfigMecha = "Assets/_Proyecto/Mecha/Datos/ConfigCalorMecha_Base.asset";
        private const string RutaConfigPantalla = "Assets/_Proyecto/UI/Datos/ConfigPantallaCalor_Base.asset";
        private const string NombrePantalla = "PantallaCalor";
        private const int NumeroMarcas = 3;

        // Misma altura, distancia e inclinación que la pantalla de vida (CreadorCabina), a su derecha en el tablero.
        private const float AlturaCamara = 3.5f;
        private const float XPantalla = 0.225f;
        private const float YPantalla = -0.285f;
        private const float ZPantalla = 0.6f;
        private const float InclinacionPantalla = 60f;
        private const float SeparacionDelTablero = -0.003f;

        // Canvas de 120 x 180 unidades a 0.001 m por unidad (0.12 x 0.18 m).
        private const float AnchoCanvas = 120f;
        private const float AltoCanvas = 180f;
        private const float EscalaCanvas = 0.001f;
        private const int TamanoTitulo = 30;

        [MenuItem("Mecha Cyberpunk/Crear T-067 calor del mecha")]
        public static void Crear()
        {
            ConfigCalorMecha configMecha = CrearAsset<ConfigCalorMecha>(RutaConfigMecha);
            ConfigPantallaCalor configPantalla = CrearAsset<ConfigPantallaCalor>(RutaConfigPantalla);
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                CalorPorMecha calor = raiz.GetComponent<CalorPorMecha>();
                if (calor == null)
                {
                    calor = raiz.AddComponent<CalorPorMecha>();
                }
                AsignarObjeto(calor, "config", configMecha);

                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                if (cabina.Find(NombrePantalla) == null)
                {
                    ArmarPantalla(cabina, raiz.GetComponent<EncendidoMecha>(), configPantalla);
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-067 calor del mecha creado.");
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

        private static void AsignarObjeto(Object componente, string campo, Object valor)
        {
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ArmarPantalla(Transform cabina, EncendidoMecha encendido, ConfigPantallaCalor config)
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

            Image fondo = CrearImagen("Fondo", rectCanvas, Vector2.zero, Vector2.one);

            GameObject objetoTexto = new GameObject("Titulo", typeof(RectTransform));
            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.SetParent(rectCanvas, false);
            Estirar(rectTexto, new Vector2(0f, 0.62f), new Vector2(1f, 0.96f));
            Text titulo = objetoTexto.AddComponent<Text>();
            titulo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titulo.fontSize = TamanoTitulo;
            titulo.alignment = TextAnchor.MiddleCenter;
            titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
            titulo.text = config.Titulo;
            titulo.raycastTarget = false;

            // Tres marcas en fila, separadas por huecos iguales.
            Image[] marcas = new Image[NumeroMarcas];
            const float margen = 0.08f;
            const float hueco = 0.06f;
            float ancho = (1f - 2f * margen - (NumeroMarcas - 1) * hueco) / NumeroMarcas;
            for (int i = 0; i < NumeroMarcas; i++)
            {
                float inicio = margen + i * (ancho + hueco);
                marcas[i] = CrearImagen("Marca" + (i + 1), rectCanvas, new Vector2(inicio, 0.34f), new Vector2(inicio + ancho, 0.54f));
            }

            Image huecoBarra = CrearImagen("Hueco", rectCanvas, new Vector2(margen, 0.12f), new Vector2(1f - margen, 0.2f));
            Image relleno = CrearImagen("Relleno", (RectTransform)huecoBarra.transform, Vector2.zero, Vector2.one);

            PantallaCalorCabina pantalla = objeto.AddComponent<PantallaCalorCabina>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("encendido").objectReferenceValue = encendido;
            so.FindProperty("lienzo").objectReferenceValue = canvas;
            so.FindProperty("titulo").objectReferenceValue = titulo;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("hueco").objectReferenceValue = huecoBarra;
            so.FindProperty("relleno").objectReferenceValue = relleno;
            so.FindProperty("config").objectReferenceValue = config;
            SerializedProperty lista = so.FindProperty("marcas");
            lista.arraySize = marcas.Length;
            for (int i = 0; i < marcas.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = marcas[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image CrearImagen(string nombre, RectTransform padre, Vector2 anclaMin, Vector2 anclaMax)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            Estirar(rect, anclaMin, anclaMax);
            Image imagen = objeto.AddComponent<Image>();
            imagen.raycastTarget = false;
            return imagen;
        }

        private static void Estirar(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax)
        {
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
