using System.Linq;
using MechaCyberpunk.UI.Misiones;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-186: ConfigPantallaMisiones_Base.asset y los prefabs PantallaMisiones, PantallaTelefono, EncargosCalle y ContactoBar en
    /// UI/Prefabs. Idempotente: si ya existen no los toca. Antes hay que correr CreadorJugadorAPie.Crear (agrega la acción Telefono) y
    /// CreadorHudAPie.Crear; después, CreadorJugadorAPie.Crear otra vez para anidar los prefabs en JugadorAPie.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallasMisiones.Crear
    /// </summary>
    public static class CreadorPantallasMisiones
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaMisiones_Base.asset";
        public const string RutaPantalla = "Assets/_Proyecto/UI/Prefabs/PantallaMisiones.prefab";
        public const string RutaTelefono = "Assets/_Proyecto/UI/Prefabs/PantallaTelefono.prefab";
        public const string RutaEncargos = "Assets/_Proyecto/UI/Prefabs/EncargosCalle.prefab";
        public const string RutaContacto = "Assets/_Proyecto/UI/Prefabs/ContactoBar.prefab";

        private const string RutaEntrada = "Assets/_Proyecto/Personaje/Entrada/ControlesAPie.inputactions";
        private const string CarpetaMateriales = "Assets/_Proyecto/UI/Materiales";
        private const string RutaMaterialContacto = CarpetaMateriales + "/Contacto_Cuerpo.mat";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenTelefono = 30000;
        private const int OrdenLista = 30001;
        private const float AltoLinea = 80f;
        private const float AltoTitulo = 110f;
        private const float AltoFila = 74f;
        private const float AltoContenido = 320f;
        private const float YTitulo = 400f;
        private const float YPestanas = 300f;
        private const float XPestanas = 560f;
        private const float XSeparacionPestanas = 450f;
        private const float FraccionPestana = 0.22f;
        private const float XSeparacionPestanasCinco = 360f;
        private const float FraccionPestanaCinco = 0.18f;
        private const float YLista = 40f;
        private const float AltoLista = 440f;
        private const float YContenido = 60f;
        private const float YAviso = -330f;
        private const float YAyuda = -450f;
        private const float SeparacionFilas = 14f;
        private const float YBarraBarrio = 100f;
        private const float YBarraCorporaciones = -80f;
        private const float AlturaEtiquetaBarra = 56f;
        private const float AnchoMarcaCero = 4f;
        private const float AnchoContenido = 0.8f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(2f, -2f);
        private static readonly Color ColorCuerpo = new Color(0.1f, 0.05f, 0.14f);
        private static readonly Color ColorLetrero = new Color(1f, 0.55f, 0.1f);
        private static readonly Vector3 TamanoCuerpo = new Vector3(0.7f, 0.9f, 0.7f);
        private const float AlturaLetrero = 2.2f;
        private const float TamanoLetra = 0.03f;
        private const float GradosMediaVuelta = 180f;

        [MenuItem("Mecha Cyberpunk/Crear T-186 pantallas de misiones")]
        public static void Crear()
        {
            ConfigPantallaMisiones config = AssetDatabase.LoadAssetAtPath<ConfigPantallaMisiones>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPantallaMisiones>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantalla) == null)
            {
                CrearPantalla(config);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaTelefono) == null)
            {
                CrearTelefono(config);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaEncargos) == null)
            {
                CrearEncargos(config);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaContacto) == null)
            {
                CrearContacto(config);
            }
            AsegurarPestanaFavores();
            AsegurarPestanaMensajes();
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-186/T-187 pantallas de misiones creadas.");
        }

        // T-449: la quinta pestaña (MENSAJES) del teléfono; las cinco se reparten el ancho. Idempotente: solo escribe si falta.
        public static void AsegurarPestanaMensajes()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaTelefono) == null)
            {
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaTelefono);
            try
            {
                PantallaTelefono telefono = raiz.GetComponent<PantallaTelefono>();
                SerializedObject so = new SerializedObject(telefono);
                SerializedProperty propiedad = so.FindProperty("textoPestanaMensajes");
                if (propiedad.objectReferenceValue != null)
                {
                    return;
                }
                Transform contenido = raiz.transform.Find("Contenido");
                Text mensajes = CrearTexto("PestanaMensajes", contenido, 0f, YPestanas, AltoLinea, FraccionPestanaCinco);
                string[] nombres = { "PestanaContratos", "PestanaMision", "PestanaReputacion", "PestanaFavores", "PestanaMensajes" };
                for (int i = 0; i < nombres.Length; i++)
                {
                    RectTransform rect = (RectTransform)contenido.Find(nombres[i]);
                    float x = (i - (nombres.Length - 1) / 2f) * XSeparacionPestanasCinco;
                    rect.anchorMin = new Vector2(0.5f - FraccionPestanaCinco / 2f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f + FraccionPestanaCinco / 2f, 0.5f);
                    rect.anchoredPosition = new Vector2(x, YPestanas);
                }
                propiedad.objectReferenceValue = mensajes;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaTelefono);
                Debug.Log("[UI] T-449: pestaña MENSAJES agregada al teléfono.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // T-187: la cuarta pestaña (FAVORES) del teléfono; las cuatro se reparten el ancho. Idempotente: solo escribe si falta.
        private static void AsegurarPestanaFavores()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaTelefono);
            try
            {
                PantallaTelefono telefono = raiz.GetComponent<PantallaTelefono>();
                SerializedObject so = new SerializedObject(telefono);
                SerializedProperty propiedad = so.FindProperty("textoPestanaFavores");
                if (propiedad.objectReferenceValue != null)
                {
                    return;
                }
                Transform contenido = raiz.transform.Find("Contenido");
                Text favores = CrearTexto("PestanaFavores", contenido, 0f, YPestanas, AltoLinea, FraccionPestana);
                string[] nombres = { "PestanaContratos", "PestanaMision", "PestanaReputacion", "PestanaFavores" };
                for (int i = 0; i < nombres.Length; i++)
                {
                    RectTransform rect = (RectTransform)contenido.Find(nombres[i]);
                    float x = (i - (nombres.Length - 1) / 2f) * XSeparacionPestanas;
                    rect.anchorMin = new Vector2(0.5f - FraccionPestana / 2f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f + FraccionPestana / 2f, 0.5f);
                    rect.anchoredPosition = new Vector2(x, YPestanas);
                }
                propiedad.objectReferenceValue = favores;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaTelefono);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static GameObject CrearRaizCanvas(string nombre, int orden)
        {
            GameObject raiz = new GameObject(nombre, typeof(RectTransform));
            Canvas canvas = raiz.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = orden;
            CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escala.matchWidthOrHeight = 0.5f;
            return raiz;
        }

        private static void CrearPantalla(ConfigPantallaMisiones config)
        {
            GameObject raiz = CrearRaizCanvas("PantallaMisiones", OrdenLista);
            try
            {
                GameObject contenido = Hijo("Contenido", raiz.transform);
                Estirar(contenido.GetComponent<RectTransform>());
                Image fondo = CrearImagen("Fondo", contenido.transform, config.ColorFondo);
                Estirar(fondo.rectTransform);

                Text titulo = CrearTexto("Titulo", contenido.transform, 0f, YTitulo, AltoTitulo, 1f);

                GameObject lista = Hijo("ListaFilas", contenido.transform);
                RectTransform rectLista = (RectTransform)lista.transform;
                rectLista.anchorMin = new Vector2(0f, 0.5f);
                rectLista.anchorMax = new Vector2(1f, 0.5f);
                rectLista.pivot = new Vector2(0.5f, 0.5f);
                rectLista.anchoredPosition = new Vector2(0f, YLista);
                rectLista.sizeDelta = new Vector2(0f, AltoLista);
                VerticalLayoutGroup grupo = lista.AddComponent<VerticalLayoutGroup>();
                grupo.childAlignment = TextAnchor.UpperCenter;
                grupo.spacing = SeparacionFilas;
                grupo.childControlWidth = true;
                grupo.childControlHeight = true;
                grupo.childForceExpandWidth = true;
                grupo.childForceExpandHeight = false;

                Text plantilla = CrearTexto("PlantillaFila", lista.transform, 0f, 0f, AltoFila, 1f);
                LayoutElement elemento = plantilla.gameObject.AddComponent<LayoutElement>();
                elemento.minHeight = AltoFila;
                elemento.preferredHeight = AltoFila;
                plantilla.gameObject.SetActive(false);

                Text aviso = CrearTexto("Aviso", contenido.transform, 0f, YAviso, AltoLinea, 1f);
                Text ayuda = CrearTexto("Ayuda", contenido.transform, 0f, YAyuda, AltoLinea, 1f);

                PantallaMisiones pantalla = raiz.AddComponent<PantallaMisiones>();
                SerializedObject so = new SerializedObject(pantalla);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("fondo").objectReferenceValue = fondo;
                so.FindProperty("textoTitulo").objectReferenceValue = titulo;
                so.FindProperty("listaFilas").objectReferenceValue = rectLista;
                so.FindProperty("plantillaFila").objectReferenceValue = plantilla;
                so.FindProperty("textoAviso").objectReferenceValue = aviso;
                so.FindProperty("textoAyuda").objectReferenceValue = ayuda;
                so.ApplyModifiedPropertiesWithoutUndo();
                contenido.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPantalla);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void CrearTelefono(ConfigPantallaMisiones config)
        {
            InputActionReference accion = AssetDatabase.LoadAllAssetsAtPath(RutaEntrada)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.name == "Telefono");
            if (accion == null)
            {
                Debug.LogError("[UI] T-186: falta la acción Telefono en ControlesAPie; corre antes CreadorJugadorAPie.Crear.");
                return;
            }
            GameObject raiz = CrearRaizCanvas("PantallaTelefono", OrdenTelefono);
            try
            {
                GameObject contenido = Hijo("Contenido", raiz.transform);
                Estirar(contenido.GetComponent<RectTransform>());
                Image fondo = CrearImagen("Fondo", contenido.transform, config.ColorFondo);
                Estirar(fondo.rectTransform);

                Text titulo = CrearTexto("Titulo", contenido.transform, 0f, YTitulo, AltoTitulo, 1f);
                Text pestanaContratos = CrearTexto("PestanaContratos", contenido.transform, -XPestanas, YPestanas, AltoLinea, 0.3f);
                Text pestanaMision = CrearTexto("PestanaMision", contenido.transform, 0f, YPestanas, AltoLinea, 0.3f);
                Text pestanaReputacion = CrearTexto("PestanaReputacion", contenido.transform, XPestanas, YPestanas, AltoLinea, 0.3f);

                Text texto = CrearTexto("Contenido texto", contenido.transform, 0f, YContenido, AltoContenido, AnchoContenido);
                texto.horizontalOverflow = HorizontalWrapMode.Wrap;

                GameObject grupoReputacion = Hijo("Reputacion", contenido.transform);
                Estirar(grupoReputacion.GetComponent<RectTransform>());
                Text[] etiquetas = new Text[2];
                Image[] huecos = new Image[2];
                Image[] rellenos = new Image[2];
                float[] ys = { YBarraBarrio, YBarraCorporaciones };
                for (int i = 0; i < 2; i++)
                {
                    etiquetas[i] = CrearTexto("Etiqueta" + i, grupoReputacion.transform, 0f, ys[i] + AlturaEtiquetaBarra, AlturaEtiquetaBarra, 1f);
                    huecos[i] = CrearImagen("Hueco" + i, grupoReputacion.transform, config.ColorHuecoBarra);
                    RectTransform rectHueco = huecos[i].rectTransform;
                    rectHueco.anchorMin = rectHueco.anchorMax = rectHueco.pivot = new Vector2(0.5f, 0.5f);
                    rectHueco.anchoredPosition = new Vector2(0f, ys[i]);
                    rectHueco.sizeDelta = new Vector2(config.AnchoBarra, config.AltoBarra);
                    rellenos[i] = CrearImagen("Relleno" + i, huecos[i].transform, config.ColorBarraPositiva);
                    Estirar(rellenos[i].rectTransform);
                    Image cero = CrearImagen("Cero" + i, huecos[i].transform, Color.white);
                    RectTransform rectCero = cero.rectTransform;
                    rectCero.anchorMin = new Vector2(0.5f, 0f);
                    rectCero.anchorMax = new Vector2(0.5f, 1f);
                    rectCero.pivot = new Vector2(0.5f, 0.5f);
                    rectCero.anchoredPosition = Vector2.zero;
                    rectCero.sizeDelta = new Vector2(AnchoMarcaCero, 0f);
                }
                grupoReputacion.SetActive(false);

                Text ayuda = CrearTexto("Ayuda", contenido.transform, 0f, YAyuda, AltoLinea, 1f);

                PantallaTelefono telefono = raiz.AddComponent<PantallaTelefono>();
                SerializedObject so = new SerializedObject(telefono);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("contenido").objectReferenceValue = contenido;
                so.FindProperty("fondo").objectReferenceValue = fondo;
                so.FindProperty("textoTitulo").objectReferenceValue = titulo;
                so.FindProperty("textoPestanaContratos").objectReferenceValue = pestanaContratos;
                so.FindProperty("textoPestanaMision").objectReferenceValue = pestanaMision;
                so.FindProperty("textoPestanaReputacion").objectReferenceValue = pestanaReputacion;
                so.FindProperty("textoContenido").objectReferenceValue = texto;
                so.FindProperty("textoAyuda").objectReferenceValue = ayuda;
                so.FindProperty("grupoReputacion").objectReferenceValue = grupoReputacion;
                so.FindProperty("telefono").objectReferenceValue = accion;
                for (int i = 0; i < 2; i++)
                {
                    so.FindProperty("textosBarras").GetArrayElementAtIndex(i).objectReferenceValue = etiquetas[i];
                    so.FindProperty("huecosBarras").GetArrayElementAtIndex(i).objectReferenceValue = huecos[i];
                    so.FindProperty("rellenosBarras").GetArrayElementAtIndex(i).objectReferenceValue = rellenos[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                contenido.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaTelefono);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void CrearEncargos(ConfigPantallaMisiones config)
        {
            GameObject raiz = new GameObject("EncargosCalle");
            try
            {
                SerializedObject so = new SerializedObject(raiz.AddComponent<EncargosCalle>());
                so.FindProperty("config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaEncargos);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // El contacto del bar: una figura de cápsula con un letrero. Alan o un pase de arte lo reemplaza.
        private static void CrearContacto(ConfigPantallaMisiones config)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/UI", "Materiales");
            }
            Material cuerpoMat = CrearMaterial(RutaMaterialContacto, "Universal Render Pipeline/Lit", ColorCuerpo);
            GameObject raiz = new GameObject("ContactoBar");
            try
            {
                GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cuerpo.name = "Cuerpo";
                Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
                cuerpo.transform.SetParent(raiz.transform, false);
                cuerpo.transform.localPosition = new Vector3(0f, TamanoCuerpo.y, 0f);
                cuerpo.transform.localScale = new Vector3(TamanoCuerpo.x, TamanoCuerpo.y, TamanoCuerpo.z);
                cuerpo.GetComponent<MeshRenderer>().sharedMaterial = cuerpoMat;

                // El letrero mira hacia +Z (la entrada del bar): se lee desde ese lado.
                Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                {
                    GameObject texto = new GameObject("Texto letrero");
                    texto.transform.SetParent(raiz.transform, false);
                    texto.transform.localPosition = new Vector3(0f, AlturaLetrero, 0f);
                    texto.transform.localRotation = Quaternion.Euler(0f, GradosMediaVuelta, 0f);
                    TextMesh mesh = texto.AddComponent<TextMesh>();
                    mesh.text = "CONTACTO";
                    mesh.anchor = TextAnchor.MiddleCenter;
                    mesh.alignment = TextAlignment.Center;
                    mesh.fontSize = 100;
                    mesh.characterSize = TamanoLetra;
                    mesh.color = ColorLetrero;
                    mesh.font = fuente;
                    texto.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
                }

                SerializedObject so = new SerializedObject(raiz.AddComponent<ContactoBar>());
                so.FindProperty("config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaContacto);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static Material CrearMaterial(string ruta, string shader, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, ruta);
            }
            return material;
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

        // Texto centrado: ancho = fracción del lienzo (1 = todo el ancho), centrado en x.
        private static Text CrearTexto(string nombre, Transform padre, float x, float y, float alto, float fraccionAncho)
        {
            GameObject objeto = Hijo(nombre, padre);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = new Vector2(0.5f - fraccionAncho / 2f, 0.5f);
            rect.anchorMax = new Vector2(0.5f + fraccionAncho / 2f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
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
