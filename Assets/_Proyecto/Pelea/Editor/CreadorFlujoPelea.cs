using System.IO;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Pelea.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MechaCyberpunk.Pelea.Editor
{
    /// <summary>
    /// Crea lo de T-022: config, materiales, ArenaDePrueba.prefab, FlujoPelea.prefab y su colocación en Pruebas_Tony (idempotente).
    /// Batchmode: -executeMethod MechaCyberpunk.Pelea.Editor.CreadorFlujoPelea.Crear
    /// </summary>
    public static class CreadorFlujoPelea
    {
        private const string RutaPelea = "Assets/_Proyecto/Pelea";
        private const string RutaConfig = RutaPelea + "/Datos/ConfigFlujoPelea_Base.asset";
        private const string RutaPrefabFlujo = RutaPelea + "/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabArena = RutaPelea + "/Pruebas/ArenaDePrueba.prefab";
        private const string RutaMaterialPiso = RutaPelea + "/Materiales/Arena_Piso.mat";
        private const string RutaMaterialMuro = RutaPelea + "/Materiales/Arena_Muro.mat";
        private const string RutaMaterialPantalla = RutaPelea + "/Materiales/Arena_Pantalla.mat";
        private const string RutaPrefabRival = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";

        private const string TeclaRevancha = "<Keyboard>/r";
        private const string TeclaIniciar = "<Keyboard>/p";

        // Escala D-014: jaula de 24 x 24 m, peleadores a 12 m uno del otro.
        private const float LadoJaula = 24f;
        private const float AlturaMuro = 6f;
        private const float GrosorMuro = 0.5f;
        private const float GrosorPiso = 0.5f;
        private const float MitadSeparacion = 6f;
        private const float ZPantalla = 11.95f;
        private const float AlturaPantalla = 4f;
        private static readonly Vector2 TamanoPantalla = new Vector2(8f, 4.5f);
        private const float GiroRival = 180f;
        // Los peleadores nacen 5 cm sobre el piso, como en el resto de Pruebas_Tony.
        private const float AlturaInicio = 0.05f;

        private const float AnchoLienzo = 1600f;
        private const float AltoLienzo = 900f;
        private const float FraccionRevancha = 0.35f;
        private const int TamanoLetra = 280;
        private const int TamanoLetraRevancha = 80;

        // Lejos del saco, del atacante y del rival de T-021 en Pruebas_Tony.
        private static readonly Vector3 PosicionArenaEscena = new Vector3(80f, 0f, 0f);

        private static readonly Color ColorPiso = new Color(0.18f, 0.18f, 0.2f);
        private static readonly Color ColorMuro = new Color(0.1f, 0.1f, 0.13f);
        private static readonly Color ColorPantalla = new Color(0.02f, 0.02f, 0.04f);

        [MenuItem("Mecha Cyberpunk/Crear T-022")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaPelea + "/Datos");
            AsegurarCarpeta(RutaPelea + "/Materiales");
            AsegurarCarpeta(RutaPelea + "/Prefabs");
            AsegurarCarpeta(RutaPelea + "/Pruebas");
            AssetDatabase.Refresh();

            ConfigFlujoPelea config = CrearAsset<ConfigFlujoPelea>(RutaConfig);
            // La pantalla está a unos 24 m del jugador: la letra va grande para leerse desde la cabina.
            SerializedObject soConfig = new SerializedObject(config);
            soConfig.FindProperty("tamanoLetra").intValue = TamanoLetra;
            soConfig.FindProperty("tamanoLetraRevancha").intValue = TamanoLetraRevancha;
            soConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            Material piso = CrearMaterial(RutaMaterialPiso, ColorPiso, "Universal Render Pipeline/Lit");
            Material muro = CrearMaterial(RutaMaterialMuro, ColorMuro, "Universal Render Pipeline/Lit");
            Material pantalla = CrearMaterial(RutaMaterialPantalla, ColorPantalla, "Universal Render Pipeline/Unlit");
            AssetDatabase.SaveAssets();

            ArmarArena(piso, muro, pantalla);
            ArmarFlujo(config);
            ArmarEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[Pelea] T-022 creada.");
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
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

        private static Material CrearMaterial(string ruta, Color color, string shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Caja(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material,
            bool conCollider)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            if (!conCollider)
            {
                Object.DestroyImmediate(caja.GetComponent<Collider>());
            }
            caja.transform.SetParent(padre, false);
            caja.transform.localPosition = posicion;
            caja.transform.localScale = escala;
            caja.GetComponent<MeshRenderer>().sharedMaterial = material;
            return caja;
        }

        private static Transform Punto(string nombre, Transform padre, Vector3 posicion, float giroY)
        {
            Transform punto = new GameObject(nombre).transform;
            punto.SetParent(padre, false);
            punto.localPosition = posicion;
            punto.localRotation = Quaternion.Euler(0f, giroY, 0f);
            return punto;
        }

        private static void ArmarArena(Material piso, Material muro, Material materialPantalla)
        {
            GameObject raiz = new GameObject("ArenaDePrueba");
            try
            {
                Transform t = raiz.transform;
                float mitad = LadoJaula / 2f;
                // El piso queda con su cara de arriba en y = 0.
                Caja("Piso", t, new Vector3(0f, -GrosorPiso / 2f, 0f), new Vector3(LadoJaula, GrosorPiso, LadoJaula), piso, true);
                float yMuro = AlturaMuro / 2f;
                float desplazamiento = mitad + GrosorMuro / 2f;
                Vector3 escalaNorteSur = new Vector3(LadoJaula + GrosorMuro * 2f, AlturaMuro, GrosorMuro);
                Vector3 escalaEsteOeste = new Vector3(GrosorMuro, AlturaMuro, LadoJaula);
                Caja("MuroNorte", t, new Vector3(0f, yMuro, desplazamiento), escalaNorteSur, muro, true);
                Caja("MuroSur", t, new Vector3(0f, yMuro, -desplazamiento), escalaNorteSur, muro, true);
                Caja("MuroEste", t, new Vector3(desplazamiento, yMuro, 0f), escalaEsteOeste, muro, true);
                Caja("MuroOeste", t, new Vector3(-desplazamiento, yMuro, 0f), escalaEsteOeste, muro, true);

                Transform inicioJugador = Punto("InicioJugador", t, new Vector3(0f, AlturaInicio, -MitadSeparacion), 0f);
                Transform inicioRival = Punto("InicioRival", t, new Vector3(0f, AlturaInicio, MitadSeparacion), GiroRival);

                // La pantalla está en el muro de adelante; su +Z apunta desde la jaula hacia ella.
                Transform pantalla = Punto("PantallaArena", t, new Vector3(0f, AlturaPantalla, ZPantalla), 0f);
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "CaraPantalla";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(pantalla, false);
                quad.transform.localScale = new Vector3(TamanoPantalla.x, TamanoPantalla.y, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = materialPantalla;

                ArenaDePrueba arena = raiz.AddComponent<ArenaDePrueba>();
                SerializedObject so = new SerializedObject(arena);
                so.FindProperty("inicioJugador").objectReferenceValue = inicioJugador;
                so.FindProperty("inicioRival").objectReferenceValue = inicioRival;
                so.FindProperty("pantallaArena").objectReferenceValue = pantalla;
                so.FindProperty("tamanoPantalla").vector2Value = TamanoPantalla;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabArena);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void ConfigurarAccion(SerializedProperty accion, string nombre, string tecla)
        {
            accion.FindPropertyRelative("m_Name").stringValue = nombre;
            accion.FindPropertyRelative("m_Type").intValue = (int)InputActionType.Button;
            SerializedProperty bindings = accion.FindPropertyRelative("m_SingletonActionBindings");
            bindings.arraySize = 1;
            SerializedProperty binding = bindings.GetArrayElementAtIndex(0);
            binding.FindPropertyRelative("m_Path").stringValue = tecla;
            binding.FindPropertyRelative("m_Id").stringValue = System.Guid.NewGuid().ToString();
        }

        private static Text CrearTexto(string nombre, RectTransform padre, Vector2 anclaMin, Vector2 anclaMax)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.enabled = false;
            return texto;
        }

        private static void ArmarFlujo(ConfigFlujoPelea config)
        {
            GameObject prefabRival = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabRival);
            if (prefabRival == null)
            {
                Debug.LogError("[Pelea] Falta " + RutaPrefabRival + " (T-021).");
                return;
            }

            GameObject raiz = new GameObject("FlujoPelea");
            try
            {
                GameObject objetoPantalla = new GameObject("PantallaPelea", typeof(RectTransform));
                objetoPantalla.transform.SetParent(raiz.transform, false);
                RectTransform rectLienzo = (RectTransform)objetoPantalla.transform;
                rectLienzo.sizeDelta = new Vector2(AnchoLienzo, AltoLienzo);
                Canvas canvas = objetoPantalla.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                Text texto = CrearTexto("Texto", rectLienzo, new Vector2(0f, FraccionRevancha), Vector2.one);
                Text revancha = CrearTexto("Revancha", rectLienzo, Vector2.zero, new Vector2(1f, FraccionRevancha));

                PantallaPelea pantalla = objetoPantalla.AddComponent<PantallaPelea>();
                SerializedObject soPantalla = new SerializedObject(pantalla);
                soPantalla.FindProperty("config").objectReferenceValue = config;
                soPantalla.FindProperty("lienzo").objectReferenceValue = canvas;
                soPantalla.FindProperty("texto").objectReferenceValue = texto;
                soPantalla.FindProperty("textoRevancha").objectReferenceValue = revancha;
                soPantalla.ApplyModifiedPropertiesWithoutUndo();

                DirectorPelea director = raiz.AddComponent<DirectorPelea>();
                SerializedObject soDirector = new SerializedObject(director);
                soDirector.FindProperty("config").objectReferenceValue = config;
                soDirector.FindProperty("pantalla").objectReferenceValue = pantalla;
                soDirector.FindProperty("prefabRival").objectReferenceValue = prefabRival;
                soDirector.FindProperty("empezarAlArrancar").boolValue = true;
                ConfigurarAccion(soDirector.FindProperty("revancha"), "Revancha", TeclaRevancha);
                soDirector.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabFlujo);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void ArmarEscena()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            // Abrir la escena descarga los assets sin uso: se cargan de nuevo para no asignar referencias muertas.
            GameObject prefabArena = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArena);
            GameObject prefabFlujo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabFlujo);

            GameObject rival = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (raiz.name == "RivalBasico")
                {
                    rival = raiz;
                }
                else if (raiz.name == "ArenaDePrueba" || raiz.name == "FlujoPelea" || raiz.name == "IniciadorPeleaDePrueba")
                {
                    Object.DestroyImmediate(raiz);
                }
            }
            if (rival == null)
            {
                Debug.LogError("[Pelea] Falta el RivalBasico de T-021 en Pruebas_Tony.");
                return;
            }

            GameObject arena = (GameObject)PrefabUtility.InstantiatePrefab(prefabArena, escena);
            arena.transform.position = PosicionArenaEscena;

            GameObject flujo = (GameObject)PrefabUtility.InstantiatePrefab(prefabFlujo, escena);
            flujo.transform.position = PosicionArenaEscena;
            DirectorPelea director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("empezarAlArrancar").boolValue = false;
            soDirector.FindProperty("rivalEnEscena").objectReferenceValue = rival;
            soDirector.ApplyModifiedPropertiesWithoutUndo();

            GameObject iniciador = new GameObject("IniciadorPeleaDePrueba");
            SceneManager.MoveGameObjectToScene(iniciador, escena);
            IniciadorPeleaDePrueba componente = iniciador.AddComponent<IniciadorPeleaDePrueba>();
            SerializedObject soIniciador = new SerializedObject(componente);
            soIniciador.FindProperty("director").objectReferenceValue = director;
            ConfigurarAccion(soIniciador.FindProperty("iniciar"), "Iniciar pelea", TeclaIniciar);
            soIniciador.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
    }
}
