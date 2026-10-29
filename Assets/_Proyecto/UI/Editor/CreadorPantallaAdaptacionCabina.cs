using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-203: la pantalla de adaptación en MechaJugador.prefab (por herencia llega a MechaEstacionado), a la derecha y bajo la de
    /// habilidades (espejo de OJOS), fuera del 40 % central de la vista. Idempotente: si la pantalla ya existe no se rehace.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaAdaptacionCabina.Crear
    /// </summary>
    public static class CreadorPantallaAdaptacionCabina
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaAdaptacion_Base.asset";
        private const string NombrePantalla = "PantallaAdaptacion";

        private const float AlturaCamara = 3.5f;
        private const float XPantalla = 0.5f;
        private const float YPantalla = 0.06f;
        private const float ZPantalla = 0.8f;

        private const float AnchoCanvas = 300f;
        private const float AltoCanvas = 232f;
        private const float EscalaCanvas = 0.0011f;
        private const float Margen = 8f;
        private const float AltoTitulo = 28f;
        private const float AltoFila = 24f;
        private const float AltoFin = 22f;
        private const float AltoPista = 26f;
        private const float Separacion = 4f;
        private const int Filas = 3;
        private const int TamanoMinimo = 8;
        private const int TamanoMaximo = 20;

        [MenuItem("Mecha Cyberpunk/Crear T-203 pantalla de adaptación de la cabina")]
        public static void Crear()
        {
            ConfigPantallaAdaptacion config = AssetDatabase.LoadAssetAtPath<ConfigPantallaAdaptacion>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaAdaptacion>();
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
            Debug.Log("[UI] T-203 pantalla de adaptación de la cabina creada.");
        }

        private static void ArmarPantalla(Transform cabina, EncendidoMecha encendido, Camera camara, ConfigPantallaAdaptacion config)
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
            fondo.rectTransform.anchorMin = Vector2.zero;
            fondo.rectTransform.anchorMax = Vector2.one;
            fondo.rectTransform.offsetMin = Vector2.zero;
            fondo.rectTransform.offsetMax = Vector2.zero;

            float y = Margen;
            Text titulo = NuevoTexto("Titulo", rectCanvas, TextAnchor.MiddleLeft);
            Fila(titulo.rectTransform, y, AltoTitulo);
            y += AltoTitulo + Separacion;

            Text[] filas = new Text[Filas];
            for (int i = 0; i < Filas; i++)
            {
                filas[i] = NuevoTexto("Fila" + i, rectCanvas, TextAnchor.MiddleLeft);
                Fila(filas[i].rectTransform, y, AltoFila);
                y += AltoFila + Separacion;
            }

            Text[] fin = new Text[Filas];
            for (int i = 0; i < Filas; i++)
            {
                fin[i] = NuevoTexto("Fin" + i, rectCanvas, TextAnchor.MiddleLeft);
                Fila(fin[i].rectTransform, y, AltoFin);
                y += AltoFin + Separacion;
            }

            Text pista = NuevoTexto("Pista", rectCanvas, TextAnchor.MiddleLeft);
            Fila(pista.rectTransform, y, AltoPista);

            PantallaAdaptacionCabina pantalla = objeto.AddComponent<PantallaAdaptacionCabina>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("encendido").objectReferenceValue = encendido;
            so.FindProperty("camara").objectReferenceValue = camara;
            so.FindProperty("lienzo").objectReferenceValue = canvas;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("textoTitulo").objectReferenceValue = titulo;
            so.FindProperty("textoPista").objectReferenceValue = pista;
            so.FindProperty("config").objectReferenceValue = config;
            AsignarLista(so.FindProperty("filas"), filas);
            AsignarLista(so.FindProperty("lineasFin"), fin);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AsignarLista(SerializedProperty propiedad, Text[] textos)
        {
            propiedad.arraySize = textos.Length;
            for (int i = 0; i < textos.Length; i++)
            {
                propiedad.GetArrayElementAtIndex(i).objectReferenceValue = textos[i];
            }
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
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.raycastTarget = false;
            return texto;
        }

        private static void Fila(RectTransform rect, float desdeArriba, float alto)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(Margen, -(desdeArriba + alto));
            rect.offsetMax = new Vector2(-Margen, -desdeArriba);
        }
    }
}
