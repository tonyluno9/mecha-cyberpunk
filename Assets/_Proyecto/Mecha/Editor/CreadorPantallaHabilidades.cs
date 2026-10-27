using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-177 en MechaJugador.prefab (y por herencia en MechaEstacionado.prefab): la pantalla de habilidades arriba a la
    /// derecha del tablero (espejo de la de piezas) y la franja del cristal que se prende con el escudo. También deja guardados con los
    /// textos nuevos los configs de ayudas y de piezas del taller. Idempotente: una segunda corrida no cambia el prefab.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorPantallaHabilidades.Crear
    /// </summary>
    public static class CreadorPantallaHabilidades
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaHabilidades_Base.asset";
        private const string RutaConfigAyudas = "Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset";
        private const string RutaConfigPiezas = "Assets/_Proyecto/UI/Datos/ConfigPantallaPiezas_Base.asset";
        private const string NombrePantalla = "PantallaHabilidades";
        private const string NombreFranja = "FranjaCristal";

        // Arriba a la derecha, delante del marco del cristal (a 0.84 m): espejo de la pantalla de piezas de T-170.
        private const float AlturaCamara = 3.5f;
        private const float XPantalla = 0.5f;
        private const float YPantalla = 0.28f;
        private const float ZPantalla = 0.8f;

        // Canvas de 240 x 150 unidades a 0.001 m por unidad (0.24 x 0.15 m).
        private const float AnchoCanvas = 240f;
        private const float AltoCanvas = 150f;
        private const float EscalaCanvas = 0.001f;
        private const float GrosorMarco = 4f;
        private const float Margen = 8f;
        private const float AltoTextoFila = 28f;
        private const float AltoBarra = 22f;
        private const float SeparacionTextoBarra = 2f;
        private const float SeparacionFilas = 56f;
        private const float AltoPie = 24f;
        private const int TamanoMinimo = 8;
        private const int TamanoMaximoFila = 22;
        private const int TamanoMaximoEstado = 18;
        private const int TamanoMaximoPie = 20;

        // Franja del borde del cristal (el marco mide 1.4 x 0.7 m a 0.84 m): un canvas de 1310 x 635 con tres tiras de 8 unidades.
        private const float XFranja = 0f;
        private const float YFranja = 0.0175f;
        private const float ZFranja = 0.83f;
        private const float AnchoFranja = 1310f;
        private const float AltoFranja = 635f;
        private const float GrosorFranja = 8f;

        [MenuItem("Mecha Cyberpunk/Crear T-177 pantalla de habilidades")]
        public static void Crear()
        {
            ConfigPantallaHabilidades config = CrearAsset<ConfigPantallaHabilidades>(RutaConfig);
            // Los configs viejos ganan campos nuevos: se vuelven a guardar para que el .asset los lleve.
            GuardarConNuevosCampos(AssetDatabase.LoadAssetAtPath<ConfigAyudas>(RutaConfigAyudas));
            GuardarConNuevosCampos(AssetDatabase.LoadAssetAtPath<ConfigPantallaPiezas>(RutaConfigPiezas));
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                UsoHabilidades uso = raiz.GetComponent<UsoHabilidades>();
                LanzadorHabilidades lanzador = raiz.GetComponent<LanzadorHabilidades>();
                if (uso == null || lanzador == null)
                {
                    Debug.LogError("[Mecha] Falta UsoHabilidades o LanzadorHabilidades en MechaJugador (corre CreadorHabilidadesMecha antes).");
                    return;
                }
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                // Idempotente: si la pantalla ya existe no se rehace (rehacerla cambia los fileID y ensucia el prefab).
                if (cabina.Find(NombrePantalla) == null)
                {
                    Canvas lienzoFranja;
                    Image[] franjas = ArmarFranja(cabina, out lienzoFranja);
                    ArmarPantalla(cabina, uso, lanzador, raiz.GetComponent<EncendidoMecha>(), config, lienzoFranja, franjas);
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-177 pantalla de habilidades creada.");
        }

        private static void GuardarConNuevosCampos(Object asset)
        {
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
            }
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

        private static Image[] ArmarFranja(Transform cabina, out Canvas canvas)
        {
            GameObject objeto = new GameObject(NombreFranja, typeof(RectTransform));
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(XFranja, AlturaCamara + YFranja, ZFranja);
            objeto.transform.localScale = Vector3.one * EscalaCanvas;
            RectTransform rectCanvas = (RectTransform)objeto.transform;
            rectCanvas.sizeDelta = new Vector2(AnchoFranja, AltoFranja);
            canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.enabled = false;

            Image arriba = Tira("Arriba", rectCanvas, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, GrosorFranja));
            Image izquierda = Tira("Izquierda", rectCanvas, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(GrosorFranja, 0f));
            Image derecha = Tira("Derecha", rectCanvas, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(GrosorFranja, 0f));
            return new[] { arriba, izquierda, derecha };
        }

        private static Image Tira(string nombre, RectTransform padre, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 tamano)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.pivot = pivote;
            rect.sizeDelta = tamano;
            rect.anchoredPosition = Vector2.zero;
            Image imagen = objeto.AddComponent<Image>();
            imagen.raycastTarget = false;
            return imagen;
        }

        private static void ArmarPantalla(Transform cabina, UsoHabilidades uso, LanzadorHabilidades lanzador, EncendidoMecha encendido,
            ConfigPantallaHabilidades config, Canvas lienzoFranja, Image[] franjas)
        {
            GameObject objeto = new GameObject(NombrePantalla, typeof(RectTransform));
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(XPantalla, AlturaCamara + YPantalla, ZPantalla);
            objeto.transform.localScale = Vector3.one * EscalaCanvas;
            RectTransform rectCanvas = (RectTransform)objeto.transform;
            rectCanvas.sizeDelta = new Vector2(AnchoCanvas, AltoCanvas);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.enabled = false;

            Image marco = Capa("Marco", rectCanvas, 0f);
            Image fondo = Capa("Fondo", rectCanvas, GrosorMarco);

            Text[] textos = new Text[2];
            Image[] fondosBarra = new Image[2];
            Image[] rellenos = new Image[2];
            Text[] estados = new Text[2];
            for (int i = 0; i < 2; i++)
            {
                float arriba = Margen + i * SeparacionFilas;
                textos[i] = TextoFila("Fila" + (i + 1), rectCanvas, arriba, AltoTextoFila, TextAnchor.MiddleLeft, TamanoMaximoFila);
                float yBarra = arriba + AltoTextoFila + SeparacionTextoBarra;
                RectTransform rectBarra = Fila("Barra" + (i + 1), rectCanvas, yBarra, AltoBarra);
                fondosBarra[i] = rectBarra.gameObject.AddComponent<Image>();
                fondosBarra[i].raycastTarget = false;
                rellenos[i] = Capa("Relleno", rectBarra, 0f);
                estados[i] = TextoFila("Estado", rectBarra, 0f, AltoBarra, TextAnchor.MiddleCenter, TamanoMaximoEstado);
                RectTransform rectEstado = (RectTransform)estados[i].transform;
                rectEstado.anchorMin = Vector2.zero;
                rectEstado.anchorMax = Vector2.one;
                rectEstado.pivot = new Vector2(0.5f, 0.5f);
                rectEstado.offsetMin = Vector2.zero;
                rectEstado.offsetMax = Vector2.zero;
            }
            Text pie = TextoFila("Pie", rectCanvas, Margen + 2 * SeparacionFilas, AltoPie, TextAnchor.MiddleCenter, TamanoMaximoPie);

            PantallaHabilidadesCabina pantalla = objeto.AddComponent<PantallaHabilidadesCabina>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("uso").objectReferenceValue = uso;
            so.FindProperty("lanzador").objectReferenceValue = lanzador;
            so.FindProperty("encendido").objectReferenceValue = encendido;
            so.FindProperty("lienzo").objectReferenceValue = canvas;
            so.FindProperty("marco").objectReferenceValue = marco;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            SerializedProperty propiedadFilas = so.FindProperty("filas");
            propiedadFilas.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                SerializedProperty fila = propiedadFilas.GetArrayElementAtIndex(i);
                fila.FindPropertyRelative("texto").objectReferenceValue = textos[i];
                fila.FindPropertyRelative("fondoBarra").objectReferenceValue = fondosBarra[i];
                fila.FindPropertyRelative("relleno").objectReferenceValue = rellenos[i];
                fila.FindPropertyRelative("estado").objectReferenceValue = estados[i];
            }
            so.FindProperty("textoPie").objectReferenceValue = pie;
            so.FindProperty("lienzoFranja").objectReferenceValue = lienzoFranja;
            SerializedProperty propiedadFranjas = so.FindProperty("franjas");
            propiedadFranjas.arraySize = franjas.Length;
            for (int i = 0; i < franjas.Length; i++)
            {
                propiedadFranjas.GetArrayElementAtIndex(i).objectReferenceValue = franjas[i];
            }
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Rectángulo que ocupa todo el ancho del padre menos el margen, pegado arriba.
        private static RectTransform Fila(string nombre, RectTransform padre, float arriba, float alto)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-2f * Margen, alto);
            rect.anchoredPosition = new Vector2(0f, -arriba);
            return rect;
        }

        private static Text TextoFila(string nombre, RectTransform padre, float arriba, float alto, TextAnchor alineacion, int tamanoMaximo)
        {
            RectTransform rect = Fila(nombre, padre, arriba, alto);
            Text texto = rect.gameObject.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.alignment = alineacion;
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = TamanoMinimo;
            texto.resizeTextMaxSize = tamanoMaximo;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            texto.raycastTarget = false;
            return texto;
        }

        private static Image Capa(string nombre, RectTransform padre, float margen)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margen, margen);
            rect.offsetMax = new Vector2(-margen, -margen);
            Image imagen = objeto.AddComponent<Image>();
            imagen.raycastTarget = false;
            return imagen;
        }
    }
}
