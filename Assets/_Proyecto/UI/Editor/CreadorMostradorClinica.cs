using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.UI.Clinica;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-193: ConfigPantallaClinica_Base.asset y UI/Prefabs/MostradorClinica.prefab (mesa y letrero greybox, la pantalla de la
    /// clínica y la vista previa del cuerpo). Idempotente: si ya existen no los toca.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorMostradorClinica.Crear
    /// </summary>
    public static class CreadorMostradorClinica
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/MostradorClinica.prefab";

        private const string RutaVista = "Assets/_Proyecto/Personaje/Prefabs/VistaPreviaApariencia.prefab";
        private const string RutaMaterialMesa = "Assets/_Proyecto/UI/Materiales/Clinica_Mesa.mat";
        private const string RutaMaterialLetrero = "Assets/_Proyecto/UI/Materiales/Clinica_Letrero.mat";
        private const string TextoLetrero = "I+D+I";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenDibujo = 30000;
        private const float AnchoFilas = 1300f;
        private const float XFilas = -920f;
        private const float YPrimeraFila = 250f;
        private const float SeparacionFilas = 120f;
        private const float AltoEspacio = 46f;
        private const float AltoOpcion = 60f;
        private const float YTitulo = 420f;
        private const float YDinero = 335f;
        private const float YDescripcion = -300f;
        private const float YAviso = -385f;
        private const float YAyuda = -470f;
        private static readonly Vector2 TamanoVista = new Vector2(480f, 720f);
        private static readonly Vector2 PosicionVista = new Vector2(640f, 0f);
        private static readonly Vector3 DesplazamientoVista = new Vector3(0f, -100f, 0f);
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);
        private static readonly Color ColorMesa = new Color(0.05f, 0.2f, 0.22f);
        private static readonly Color ColorLetrero = new Color(0f, 0.9f, 0.8f);
        private static readonly Vector3 TamanoMesa = new Vector3(2.4f, 1.1f, 0.9f);
        private static readonly Vector3 TamanoLetrero = new Vector3(2.6f, 0.6f, 0.08f);
        private const float AlturaLetrero = 2.3f;
        private const float TamanoLetra = 0.035f;

        [MenuItem("Mecha Cyberpunk/Crear T-193 mostrador de la clínica")]
        public static void Crear()
        {
            ConfigPantallaClinica config = AssetDatabase.LoadAssetAtPath<ConfigPantallaClinica>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaClinica>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                CrearPrefab(config);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-193 mostrador de la clínica listo.");
        }

        private static void CrearPrefab(ConfigPantallaClinica config)
        {
            Material mesa = CrearMaterial(RutaMaterialMesa, ColorMesa);
            Material letrero = CrearMaterial(RutaMaterialLetrero, ColorLetrero);
            GameObject raiz = new GameObject("MostradorClinica");
            try
            {
                GameObject cajaMesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cajaMesa.name = "Mesa";
                cajaMesa.transform.SetParent(raiz.transform, false);
                cajaMesa.transform.localPosition = new Vector3(0f, TamanoMesa.y / 2f, 0f);
                cajaMesa.transform.localScale = TamanoMesa;
                cajaMesa.GetComponent<MeshRenderer>().sharedMaterial = mesa;

                GameObject cajaLetrero = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cajaLetrero.name = "Letrero";
                Object.DestroyImmediate(cajaLetrero.GetComponent<Collider>());
                cajaLetrero.transform.SetParent(raiz.transform, false);
                cajaLetrero.transform.localPosition = new Vector3(0f, AlturaLetrero, 0f);
                cajaLetrero.transform.localScale = TamanoLetrero;
                cajaLetrero.GetComponent<MeshRenderer>().sharedMaterial = letrero;
                // El texto cuelga de un padre sin escala para no deformarse; mira al sur (-Z), donde se para el jugador.
                GameObject textoObjeto = new GameObject("Texto");
                textoObjeto.transform.SetParent(raiz.transform, false);
                textoObjeto.transform.localPosition = new Vector3(0f, AlturaLetrero, -(TamanoLetrero.z / 2f + 0.01f));
                TextMesh mesh = textoObjeto.AddComponent<TextMesh>();
                mesh.text = TextoLetrero;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.fontSize = 100;
                mesh.characterSize = TamanoLetra;
                mesh.color = Color.black;
                Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                mesh.font = fuente;
                textoObjeto.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;

                GameObject vistaObjeto = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVista));
                vistaObjeto.transform.SetParent(raiz.transform, false);
                vistaObjeto.transform.localPosition = DesplazamientoVista;
                VistaPreviaApariencia vista = vistaObjeto.GetComponent<VistaPreviaApariencia>();

                PantallaClinica pantalla = CrearPantalla(raiz.transform, config, vista);

                MostradorClinica mostrador = raiz.AddComponent<MostradorClinica>();
                SerializedObject so = new SerializedObject(mostrador);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("pantalla").objectReferenceValue = pantalla;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static PantallaClinica CrearPantalla(Transform padre, ConfigPantallaClinica config, VistaPreviaApariencia vista)
        {
            GameObject raiz = Hijo("PantallaClinica", padre);
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
            Estirar((RectTransform)contenido.transform);
            GameObject fondoObjeto = Hijo("Fondo", contenido.transform);
            Estirar((RectTransform)fondoObjeto.transform);
            Image fondo = fondoObjeto.AddComponent<Image>();
            fondo.color = config.ColorFondo;
            fondo.raycastTarget = false;

            Text titulo = CrearTexto("Titulo", contenido.transform, TextAnchor.MiddleLeft, XFilas, YTitulo, AnchoFilas, 110f, true);
            Text dinero = CrearTexto("Dinero", contenido.transform, TextAnchor.MiddleLeft, XFilas, YDinero, AnchoFilas, 70f, true);
            Text[] espacios = new Text[4];
            Text[] opciones = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y = YPrimeraFila - i * SeparacionFilas;
                espacios[i] = CrearTexto("Espacio" + i, contenido.transform, TextAnchor.MiddleLeft, XFilas, y + AltoOpcion / 2f, AnchoFilas, AltoEspacio, true);
                opciones[i] = CrearTexto("Opcion" + i, contenido.transform, TextAnchor.MiddleLeft, XFilas, y - AltoEspacio / 2f, AnchoFilas, AltoOpcion, true);
            }
            Text descripcion = CrearTexto("Descripcion", contenido.transform, TextAnchor.MiddleLeft, XFilas, YDescripcion, AnchoFilas, 80f, true);
            Text aviso = CrearTexto("Aviso", contenido.transform, TextAnchor.MiddleLeft, XFilas, YAviso, AnchoFilas, 80f, true);
            Text ayuda = CrearTexto("Ayuda", contenido.transform, TextAnchor.MiddleLeft, XFilas, YAyuda, AnchoFilas, 60f, true);

            GameObject imagenObjeto = Hijo("Vista", contenido.transform);
            RectTransform rectImagen = (RectTransform)imagenObjeto.transform;
            rectImagen.anchorMin = rectImagen.anchorMax = rectImagen.pivot = new Vector2(0.5f, 0.5f);
            rectImagen.anchoredPosition = PosicionVista;
            rectImagen.sizeDelta = TamanoVista;
            RawImage imagen = imagenObjeto.AddComponent<RawImage>();
            imagen.raycastTarget = false;

            PantallaClinica pantalla = raiz.AddComponent<PantallaClinica>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("contenido").objectReferenceValue = contenido;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("textoTitulo").objectReferenceValue = titulo;
            so.FindProperty("textoDinero").objectReferenceValue = dinero;
            AsignarLista(so.FindProperty("textosEspacio"), espacios);
            AsignarLista(so.FindProperty("textosOpcion"), opciones);
            so.FindProperty("textoDescripcion").objectReferenceValue = descripcion;
            so.FindProperty("textoAviso").objectReferenceValue = aviso;
            so.FindProperty("textoAyuda").objectReferenceValue = ayuda;
            so.FindProperty("imagenVista").objectReferenceValue = imagen;
            so.FindProperty("vista").objectReferenceValue = vista;
            so.ApplyModifiedPropertiesWithoutUndo();
            contenido.SetActive(false);
            return pantalla;
        }

        private static void AsignarLista(SerializedProperty lista, Text[] textos)
        {
            lista.arraySize = textos.Length;
            for (int i = 0; i < textos.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = textos[i];
            }
        }

        private static Material CrearMaterial(string ruta, Color color)
        {
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        private static GameObject Hijo(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            return objeto;
        }

        // Texto anclado a la izquierda del centro: x e y son desde el centro de la pantalla.
        private static Text CrearTexto(string nombre, Transform padre, TextAnchor alineacion, float x, float y, float ancho, float alto, bool sombra)
        {
            GameObject objeto = Hijo(nombre, padre);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(ancho, alto);
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alineacion;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            if (sombra)
            {
                Shadow efecto = objeto.AddComponent<Shadow>();
                efecto.effectColor = ColorSombra;
                efecto.effectDistance = DesplazamientoSombra;
            }
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
