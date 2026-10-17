using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Creacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-126: ConfigPantallaCreacion_Base.asset y Escenas/Tony/CreacionPersonaje.unity (idempotente: rehace la escena).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorEscenaCreacion.Crear
    /// </summary>
    public static class CreadorEscenaCreacion
    {
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaCreacion_Base.asset";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity";
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaVista = "Assets/_Proyecto/Personaje/Prefabs/VistaPreviaApariencia.prefab";
        private const string RutaPantallas = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaDatosEscenas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";
        private const string RutaCatalogo = "Assets/_Proyecto/Personaje/Datos/CatalogoApariencia_Base.asset";

        private const int OrdenCanvas = 100;
        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int Filas = 6;
        private const float YPrimeraFila = 250f;
        private const float SeparacionFilas = 100f;
        private const float AltoFila = 80f;
        private const float XEtiqueta = -640f;
        private const float AnchoEtiqueta = 380f;
        private const float XValor = -140f;
        private const float AnchoValor = 640f;
        private static readonly Vector2 TamanoVista = new Vector2(480f, 720f);
        private static readonly Vector2 PosicionVista = new Vector2(620f, 0f);
        private static readonly Color ColorFondoCamara = new Color(0.03f, 0.03f, 0.07f, 1f);
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);

        [MenuItem("Mecha Cyberpunk/Crear T-126")]
        public static void Crear()
        {
            ConfigPantallaCreacion config = AssetDatabase.LoadAssetAtPath<ConfigPantallaCreacion>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaCreacion>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene descarga los assets sin uso: se vuelve a cargar la config ya con la escena nueva.
            config = AssetDatabase.LoadAssetAtPath<ConfigPantallaCreacion>(RutaConfig);

            GameObject camaraObjeto = new GameObject("Camara", typeof(Camera), typeof(AudioListener));
            camaraObjeto.tag = "MainCamera";
            Camera camara = camaraObjeto.GetComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = ColorFondoCamara;
            camara.cullingMask = 0;

            InstanciarPrefab(RutaServicios);
            GameObject vistaObjeto = InstanciarPrefab(RutaVista);
            InstanciarPrefab(RutaPantallas);
            CrearPantalla(config, vistaObjeto.GetComponent<VistaPreviaApariencia>());

            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-126 creada.");
        }

        private static GameObject InstanciarPrefab(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void CrearPantalla(ConfigPantallaCreacion config, VistaPreviaApariencia vista)
        {
            GameObject raiz = Hijo("PantallaCreacion", null);
            Canvas canvas = raiz.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OrdenCanvas;
            CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escala.matchWidthOrHeight = 0.5f;
            raiz.AddComponent<GraphicRaycaster>();
            PantallaCreacion pantalla = raiz.AddComponent<PantallaCreacion>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("datosEscenas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenas);
            so.FindProperty("catalogo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CatalogoApariencia>(RutaCatalogo);
            so.FindProperty("vista").objectReferenceValue = vista;
            so.FindProperty("canvas").objectReferenceValue = canvas;

            Image fondo = CrearImagen("Fondo", raiz.transform, ColorFondoCamara);
            Estirar(fondo.rectTransform);

            // Título
            GameObject panelTitulo = Hijo("PanelTitulo", raiz.transform);
            Estirar((RectTransform)panelTitulo.transform);
            so.FindProperty("panelTitulo").objectReferenceValue = panelTitulo;
            so.FindProperty("textoJuego").objectReferenceValue = CrearTexto("Juego", panelTitulo.transform, 0f, 300f, 1600f, 140f, TextAnchor.MiddleCenter);
            so.FindProperty("textoContinuar").objectReferenceValue = CrearTexto("Continuar", panelTitulo.transform, 0f, 40f, 900f, AltoFila, TextAnchor.MiddleLeft);
            so.FindProperty("textoNuevaPartida").objectReferenceValue = CrearTexto("NuevaPartida", panelTitulo.transform, 0f, -60f, 900f, AltoFila, TextAnchor.MiddleLeft);
            so.FindProperty("textoConfirmacion").objectReferenceValue = CrearTexto("Confirmacion", panelTitulo.transform, 0f, -220f, 1700f, AltoFila, TextAnchor.MiddleCenter);
            so.FindProperty("textoAyudaTitulo").objectReferenceValue = CrearTexto("Ayuda", panelTitulo.transform, 0f, -450f, 1700f, AltoFila, TextAnchor.MiddleCenter);

            // Creación
            GameObject panelCreacion = Hijo("PanelCreacion", raiz.transform);
            Estirar((RectTransform)panelCreacion.transform);
            so.FindProperty("panelCreacion").objectReferenceValue = panelCreacion;
            so.FindProperty("textoCrea").objectReferenceValue = CrearTexto("Crea", panelCreacion.transform, -300f, 400f, 1100f, 100f, TextAnchor.MiddleLeft);
            SerializedProperty etiquetas = so.FindProperty("etiquetas");
            SerializedProperty valores = so.FindProperty("valores");
            etiquetas.arraySize = Filas;
            valores.arraySize = Filas;
            CampoTextoTeclado campoNombre = null;
            CampoTextoTeclado campoAlias = null;
            for (int i = 0; i < Filas; i++)
            {
                float y = YPrimeraFila - i * SeparacionFilas;
                Text etiqueta = CrearTexto("Etiqueta" + i, panelCreacion.transform, XEtiqueta, y, AnchoEtiqueta, AltoFila, TextAnchor.MiddleLeft);
                etiquetas.GetArrayElementAtIndex(i).objectReferenceValue = etiqueta;
                if (i == Filas - 1) continue;
                Text valor = CrearTexto("Valor" + i, panelCreacion.transform, XValor, y, AnchoValor, AltoFila, TextAnchor.MiddleLeft);
                valores.GetArrayElementAtIndex(i).objectReferenceValue = valor;
                if (i <= 1)
                {
                    CampoTextoTeclado campo = valor.gameObject.AddComponent<CampoTextoTeclado>();
                    SerializedObject soCampo = new SerializedObject(campo);
                    soCampo.FindProperty("texto").objectReferenceValue = valor;
                    soCampo.ApplyModifiedPropertiesWithoutUndo();
                    if (i == 0) campoNombre = campo; else campoAlias = campo;
                }
            }
            so.FindProperty("campoNombre").objectReferenceValue = campoNombre;
            so.FindProperty("campoAlias").objectReferenceValue = campoAlias;
            so.FindProperty("textoAviso").objectReferenceValue = CrearTexto("Aviso", panelCreacion.transform, -300f, -400f, 1100f, AltoFila, TextAnchor.MiddleLeft);
            so.FindProperty("textoAyuda").objectReferenceValue = CrearTexto("Ayuda", panelCreacion.transform, 0f, -480f, 1800f, AltoFila, TextAnchor.MiddleCenter);

            // Vista previa del cuerpo (a la derecha del panel).
            GameObject objetoVista = Hijo("VistaCuerpo", raiz.transform);
            RectTransform rectVista = (RectTransform)objetoVista.transform;
            rectVista.anchorMin = rectVista.anchorMax = new Vector2(0.5f, 0.5f);
            rectVista.sizeDelta = TamanoVista;
            rectVista.anchoredPosition = PosicionVista;
            RawImage imagen = objetoVista.AddComponent<RawImage>();
            imagen.raycastTarget = false;
            so.FindProperty("imagenVista").objectReferenceValue = imagen;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Hijo(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            if (padre != null) objeto.transform.SetParent(padre, false);
            return objeto;
        }

        private static Image CrearImagen(string nombre, Transform padre, Color color)
        {
            Image imagen = Hijo(nombre, padre).AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        private static Text CrearTexto(string nombre, Transform padre, float x, float y, float ancho, float alto, TextAnchor alineacion)
        {
            GameObject objeto = Hijo(nombre, padre);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(ancho, alto);
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alineacion;
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
