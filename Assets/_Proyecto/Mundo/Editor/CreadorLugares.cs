using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea los assets de T-042: acción Interactuar, config de puertas, datos de los cinco locales y, en el prefab del
    /// barrio, vestíbulo, puerta con hoja, letrero con Canvas World Space y los componentes de cada local.
    /// Se puede correr varias veces. Orden: despues de CreadorBarrio y CreadorReloj; usa CreadorTodo.Crear. Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorLugares.Crear
    /// </summary>
    public static class CreadorLugares
    {
        private const string RutaDatos = "Assets/_Proyecto/Mundo/Datos";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaConfigPuertas = RutaDatos + "/ConfigPuertas_Mundo.asset";
        private const string RutaControles = "Assets/_Proyecto/Mundo/Pruebas/ControlesCamaraLibre.inputactions";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";

        // Medidas del greybox de CreadorBarrio (m).
        private const float AnchoPuerta = 3f;
        private const float AltoPuerta = 3.2f;
        private const float GrosorFachada = 1f;
        private const float GrosorHoja = 0.3f;
        private const float ProfundidadVestibulo = 3f;
        private const float FondoUmbral = 2.4f;
        private const float AlturaUmbral = 3f;
        private const float AltoLetrero = 1.4f;
        private const float AnchoMarginLetrero = 2f;
        private const float SalienteLetrero = 0.32f;

        // Canvas del letrero: 100 px por metro.
        private const float PixelesPorMetro = 100f;
        private const float MargenPanel = 10f;
        private const float DivisionTextos = 0.6f;
        private const int TamanoMinimo = 14;
        private const int TamanoNombre = 40;
        private const int TamanoEstado = 46;

        private static readonly string[] Lugares =
        {
            "Lugar_Casa", "Lugar_Bar", "Lugar_Clandestino", "Lugar_Casino", "Lugar_MercadoNegro"
        };

        [MenuItem("Mecha Cyberpunk/Crear T-042")]
        public static void Crear()
        {
            AgregarAccionInteractuar();
            AssetDatabase.Refresh();
            ConfigPuertas config = CrearConfigPuertas();
            DatosLugar casa = CrearDatos("Casa", IdLugar.Casa, "Casa", HorarioLugar.Siempre, false);
            DatosLugar bar = CrearDatos("Bar", IdLugar.Bar, "Bar El Foco", new HorarioLugar(19f, 4f), false);
            DatosLugar clandestino = CrearDatos("Clandestino", IdLugar.Clandestino, "El Sótano", new HorarioLugar(20f, 6f), false);
            DatosLugar casino = CrearDatos("Casino", IdLugar.Casino, "Casino Aurora", new HorarioLugar(20f, 6f), false);
            AbrirEnLaDemo(casino); // T-303: el casino ya tiene interior; el asset de T-042 quedó cerrado y aquí se abre
            DatosLugar mercado = CrearDatos("MercadoNegro", IdLugar.MercadoNegro, "Mercado Negro", new HorarioLugar(9f, 18f), false);
            AbrirEnLaDemo(mercado); // T-320: el mercado negro ya tiene interior y abre de día
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Material materialHoja = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + "/Barrio_PanelPuerta.mat");
            Armar(raiz.transform, "Lugar_Casa", casa, config, materialHoja, fuente);
            Armar(raiz.transform, "Lugar_Bar", bar, config, materialHoja, fuente);
            Armar(raiz.transform, "Lugar_Clandestino", clandestino, config, materialHoja, fuente);
            Armar(raiz.transform, "Lugar_Casino", casino, config, materialHoja, fuente);
            Armar(raiz.transform, "Lugar_MercadoNegro", mercado, config, materialHoja, fuente);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-042 creada: " + Lugares.Length + " locales con puerta y letrero.");
        }

        private static void AgregarAccionInteractuar()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(RutaControles));
            InputActionMap mapa = asset.FindActionMap("CamaraLibre", true);
            if (mapa.FindAction("Interactuar") == null)
            {
                mapa.AddAction("Interactuar", InputActionType.Button, "<Keyboard>/e");
                File.WriteAllText(RutaControles, asset.ToJson());
                AssetDatabase.ImportAsset(RutaControles);
            }
            Object.DestroyImmediate(asset);
        }

        private static ConfigPuertas CrearConfigPuertas()
        {
            ConfigPuertas config = AssetDatabase.LoadAssetAtPath<ConfigPuertas>(RutaConfigPuertas);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPuertas>();
                AssetDatabase.CreateAsset(config, RutaConfigPuertas);
            }
            return config;
        }

        // Solo configura el asset al crearlo: no pisa valores existentes.
        private static DatosLugar CrearDatos(string archivo, IdLugar id, string nombre, HorarioLugar horario, bool cerradoEnDemo)
        {
            string ruta = RutaDatos + "/DatosLugar_" + archivo + ".asset";
            DatosLugar datos = AssetDatabase.LoadAssetAtPath<DatosLugar>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosLugar>();
                AssetDatabase.CreateAsset(datos, ruta);
                datos.Configurar(id, nombre, horario, cerradoEnDemo);
                EditorUtility.SetDirty(datos);
            }
            return datos;
        }

        // T-303 y T-320: CrearDatos no pisa assets existentes, así que el casino y el mercado de T-042 (cerrados en la demo) se corrigen aquí.
        private static void AbrirEnLaDemo(DatosLugar datos)
        {
            if (!datos.CerradoEnDemo)
            {
                return;
            }
            datos.Configurar(datos.Id, datos.Nombre, datos.Horario, false);
            EditorUtility.SetDirty(datos);
        }

        // ---------------------------------------------------------------- locales

        private static void Armar(Transform barrio, string nombre, DatosLugar datos, ConfigPuertas config, Material materialHoja, Font fuente)
        {
            Transform lugar = BuscarPorNombre(barrio, nombre);
            Transform entrada = lugar.Find("Entrada");
            float zFrente = entrada.localPosition.z - 1.5f;

            Limpiar(lugar);
            Vaciar(lugar, zFrente);

            Transform puerta = CrearPuerta(lugar, zFrente, config, materialHoja, out LugarMundo lugarMundo, datos, entrada, out HojaPuerta hoja);
            PuertaLocal puertaLocal = puerta.gameObject.AddComponent<PuertaLocal>();
            SerializedObject so = new SerializedObject(puertaLocal);
            so.FindProperty("lugar").objectReferenceValue = lugarMundo;
            so.FindProperty("hoja").objectReferenceValue = hoja;
            so.ApplyModifiedPropertiesWithoutUndo();

            CrearLetrero(lugar, zFrente, lugarMundo, fuente);
        }

        private static Transform BuscarPorNombre(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            throw new System.InvalidOperationException("No existe " + nombre + " en el barrio; corre CreadorBarrio antes.");
        }

        // Quita lo que dejó una corrida anterior, salvo el cuerpo ya ahuecado.
        private static void Limpiar(Transform lugar)
        {
            string[] nombres = { "Puerta", "PanelPuerta", "LetreroCanvas" };
            foreach (string n in nombres)
            {
                Transform viejo = lugar.Find(n);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }
            LugarMundo previo = lugar.GetComponent<LugarMundo>();
            if (previo != null)
            {
                Object.DestroyImmediate(previo);
            }
        }

        // Cambia el cuerpo macizo por fondo, dos lados y techo, dejando un vestíbulo hueco detrás de la puerta.
        private static void Vaciar(Transform lugar, float zFrente)
        {
            Transform cuerpo = lugar.Find("Cuerpo");
            if (cuerpo == null)
            {
                return;
            }
            Vector3 escala = cuerpo.localScale;
            float ancho = escala.x;
            float altura = escala.y;
            float zFondo = cuerpo.localPosition.z - escala.z / 2f;
            float zInterior = zFrente - GrosorFachada;
            float zVestibulo = zInterior - ProfundidadVestibulo;
            Material material = cuerpo.GetComponent<MeshRenderer>().sharedMaterial;
            Object.DestroyImmediate(cuerpo.gameObject);

            float profundidadFondo = zVestibulo - zFondo;
            Pieza(lugar, "CuerpoFondo", new Vector3(0f, altura / 2f, zFondo + profundidadFondo / 2f), new Vector3(ancho, altura, profundidadFondo), material);
            float lado = (ancho - AnchoPuerta) / 2f;
            float centroVestibulo = zVestibulo + ProfundidadVestibulo / 2f;
            Pieza(lugar, "CuerpoLadoIzquierdo", new Vector3(-(AnchoPuerta / 2f + lado / 2f), altura / 2f, centroVestibulo), new Vector3(lado, altura, ProfundidadVestibulo), material);
            Pieza(lugar, "CuerpoLadoDerecho", new Vector3(AnchoPuerta / 2f + lado / 2f, altura / 2f, centroVestibulo), new Vector3(lado, altura, ProfundidadVestibulo), material);
            Pieza(lugar, "CuerpoTecho", new Vector3(0f, AltoPuerta + (altura - AltoPuerta) / 2f, centroVestibulo), new Vector3(AnchoPuerta, altura - AltoPuerta, ProfundidadVestibulo), material);
        }

        private static void Pieza(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Transform CrearPuerta(Transform lugar, float zFrente, ConfigPuertas config, Material materialHoja,
            out LugarMundo lugarMundo, DatosLugar datos, Transform entrada, out HojaPuerta hoja)
        {
            GameObject puerta = new GameObject("Puerta");
            puerta.transform.SetParent(lugar, false);
            puerta.transform.localPosition = new Vector3(0f, 0f, zFrente - GrosorFachada / 2f);

            GameObject hojaObjeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hojaObjeto.name = "Hoja";
            hojaObjeto.transform.SetParent(puerta.transform, false);
            hojaObjeto.transform.localPosition = new Vector3(0f, AltoPuerta / 2f, 0f);
            hojaObjeto.transform.localScale = new Vector3(AnchoPuerta, AltoPuerta, GrosorHoja);
            hojaObjeto.GetComponent<MeshRenderer>().sharedMaterial = materialHoja;

            GameObject umbralObjeto = new GameObject("Umbral");
            umbralObjeto.transform.SetParent(puerta.transform, false);
            umbralObjeto.transform.localPosition = new Vector3(0f, AlturaUmbral / 2f, 0f);

            hoja = hojaObjeto.AddComponent<HojaPuerta>();
            SerializedObject soHoja = new SerializedObject(hoja);
            soHoja.FindProperty("config").objectReferenceValue = config;
            soHoja.FindProperty("colliderHoja").objectReferenceValue = hojaObjeto.GetComponent<BoxCollider>();
            soHoja.FindProperty("umbral").objectReferenceValue = umbralObjeto.transform;
            soHoja.FindProperty("tamanoUmbral").vector3Value = new Vector3(AnchoPuerta, AlturaUmbral, FondoUmbral);
            soHoja.ApplyModifiedPropertiesWithoutUndo();

            lugarMundo = lugar.gameObject.AddComponent<LugarMundo>();
            SerializedObject soLugar = new SerializedObject(lugarMundo);
            soLugar.FindProperty("datos").objectReferenceValue = datos;
            soLugar.FindProperty("entrada").objectReferenceValue = entrada;
            soLugar.ApplyModifiedPropertiesWithoutUndo();
            return puerta.transform;
        }

        // ---------------------------------------------------------------- letrero

        private static void CrearLetrero(Transform lugar, float zFrente, LugarMundo lugarMundo, Font fuente)
        {
            float ancho = (AnchoPuerta + AnchoMarginLetrero) * PixelesPorMetro;
            float alto = AltoLetrero * PixelesPorMetro;
            GameObject canvasObjeto = new GameObject("LetreroCanvas", typeof(RectTransform));
            canvasObjeto.transform.SetParent(lugar, false);
            // Un Canvas se lee desde atrás de su +Z: se gira para que mire a la calle.
            canvasObjeto.transform.localPosition = new Vector3(0f, AltoPuerta + 1.2f, zFrente + SalienteLetrero);
            canvasObjeto.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasObjeto.transform.localScale = Vector3.one / PixelesPorMetro;
            Canvas canvas = canvasObjeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObjeto.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(ancho, alto);

            Image fondo = CrearHijo<Image>("Fondo", rect);
            fondo.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
            Estirar(fondo.rectTransform, 0f, 1f, MargenPanel);
            fondo.raycastTarget = false;

            Text nombre = CrearTexto("TextoNombre", rect, fuente, TextAnchor.MiddleLeft, TamanoNombre, FontStyle.Bold, Color.white);
            Estirar(nombre.rectTransform, 0f, DivisionTextos, MargenPanel * 2f);
            Text estado = CrearTexto("TextoEstado", rect, fuente, TextAnchor.MiddleCenter, TamanoEstado, FontStyle.Bold, Color.white);
            Estirar(estado.rectTransform, DivisionTextos, 1f, MargenPanel * 2f);

            LetreroLugar letrero = canvasObjeto.AddComponent<LetreroLugar>();
            SerializedObject so = new SerializedObject(letrero);
            so.FindProperty("lugar").objectReferenceValue = lugarMundo;
            so.FindProperty("textoNombre").objectReferenceValue = nombre;
            so.FindProperty("textoEstado").objectReferenceValue = estado;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CrearHijo<T>(string nombre, RectTransform padre) where T : Component
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return go.AddComponent<T>();
        }

        private static Text CrearTexto(string nombre, RectTransform padre, Font fuente, TextAnchor alineacion, int tamano, FontStyle estilo, Color color)
        {
            Text texto = CrearHijo<Text>(nombre, padre);
            texto.font = fuente;
            texto.alignment = alineacion;
            texto.fontSize = tamano;
            texto.fontStyle = estilo;
            texto.color = color;
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = TamanoMinimo;
            texto.resizeTextMaxSize = tamano;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            texto.raycastTarget = false;
            return texto;
        }

        private static void Estirar(RectTransform rect, float xMin, float xMax, float margen)
        {
            rect.anchorMin = new Vector2(xMin, 0f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.offsetMin = new Vector2(margen, margen);
            rect.offsetMax = new Vector2(-margen, -margen);
        }
    }
}
