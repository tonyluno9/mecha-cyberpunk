using System;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Movimiento;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-013 en MechaJugador.prefab y Pruebas_Tony.unity (idempotente).
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorVidaMecha.Crear
    /// Es herramienta de editor: referencia Combate y UI solo para ensamblar el prefab.
    /// </summary>
    public static class CreadorVidaMecha
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaDatosMecha = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string RutaConfigSacudida = "Assets/_Proyecto/Mecha/Datos/ConfigSacudidaCabina_Base.asset";
        private const string RutaConfigPantalla = "Assets/_Proyecto/UI/Datos/ConfigPantallaVida_Base.asset";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const string TeclaDanio = "<Keyboard>/h";
        private const string NombreAccionDanio = "Danar";
        private const float DanioPrueba = 10f;

        // Pantalla en el marco del tablero (0.3 x 0.18 m): 300 x 180 unidades de canvas a 0.001 m por unidad.
        private const float AnchoPantalla = 300f;
        private const float AltoPantalla = 180f;
        private const float EscalaCanvas = 0.001f;
        private const float SeparacionDelMarco = -0.003f;
        private const int TamanoTexto = 70;

        [MenuItem("Mecha Cyberpunk/Crear T-013")]
        public static void Crear()
        {
            ConfigSacudidaCabina sacudida = CrearAsset<ConfigSacudidaCabina>(RutaConfigSacudida);
            ConfigPantallaVida pantalla = CrearAsset<ConfigPantallaVida>(RutaConfigPantalla);
            AssetDatabase.SaveAssets();

            ArmarPrefab(sacudida, pantalla);
            ArmarEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-013 creada.");
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

        private static T Componente<T>(GameObject objeto) where T : Component
        {
            T existente = objeto.GetComponent<T>();
            return existente != null ? existente : objeto.AddComponent<T>();
        }

        private static void ArmarPrefab(ConfigSacudidaCabina sacudida, ConfigPantallaVida pantalla)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                DatosMecha datos = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosMecha);

                // Objeto Sacudida: contiene la cabina y la cámara, en la misma pose que tenían bajo la raíz.
                Transform intermedio = raiz.transform.Find("Sacudida");
                if (intermedio == null)
                {
                    intermedio = new GameObject("Sacudida").transform;
                    intermedio.SetParent(raiz.transform, false);
                    raiz.transform.Find("Cabina").SetParent(intermedio, false);
                    raiz.transform.Find("Camara").SetParent(intermedio, false);
                }
                SacudidaCabina componenteSacudida = Componente<SacudidaCabina>(intermedio.gameObject);
                AsignarObjeto(componenteSacudida, "config", sacudida);

                VidaMecha vida = Componente<VidaMecha>(raiz);
                AsignarObjeto(vida, "datos", datos);

                PeleadorMecha peleador = Componente<PeleadorMecha>(raiz);
                SerializedObject soPeleador = new SerializedObject(peleador);
                soPeleador.FindProperty("lado").enumValueIndex = (int)LadoPelea.Jugador;
                SerializedProperty lista = soPeleador.FindProperty("controlados");
                lista.arraySize = 1;
                lista.GetArrayElementAtIndex(0).objectReferenceValue = raiz.GetComponent<ControladorMecha>();
                soPeleador.ApplyModifiedPropertiesWithoutUndo();

                Transform ancla = intermedio.Find("Cabina/AnclaPantallaVida");
                ArmarPantalla(ancla, raiz, pantalla);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void ArmarPantalla(Transform ancla, GameObject raiz, ConfigPantallaVida config)
        {
            Transform vieja = ancla.Find("PantallaVida");
            if (vieja != null)
            {
                UnityEngine.Object.DestroyImmediate(vieja.gameObject);
            }

            GameObject objeto = new GameObject("PantallaVida", typeof(RectTransform));
            objeto.transform.SetParent(ancla, false);
            objeto.transform.localPosition = new Vector3(0f, 0f, SeparacionDelMarco);
            objeto.transform.localScale = Vector3.one * EscalaCanvas;
            RectTransform rectCanvas = (RectTransform)objeto.transform;
            rectCanvas.sizeDelta = new Vector2(AnchoPantalla, AltoPantalla);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Image fondo = CrearImagen("Fondo", rectCanvas, Vector2.zero, Vector2.one);

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.SetParent(rectCanvas, false);
            Estirar(rectTexto, new Vector2(0f, 0.5f), Vector2.one);
            Text texto = objetoTexto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = TamanoTexto;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.text = "VIDA 100";
            texto.raycastTarget = false;

            Image hueco = CrearImagen("Hueco", rectCanvas, new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.42f));
            Image relleno = CrearImagen("Relleno", (RectTransform)hueco.transform, Vector2.zero, Vector2.one);

            PantallaVidaCabina componente = objeto.AddComponent<PantallaVidaCabina>();
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty("fuenteVida").objectReferenceValue = raiz;
            so.FindProperty("relleno").objectReferenceValue = relleno;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("hueco").objectReferenceValue = hueco;
            so.FindProperty("config").objectReferenceValue = config;
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

        private static void AsignarObjeto(UnityEngine.Object componente, string campo, UnityEngine.Object valor)
        {
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ArmarEscena()
        {
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject jugador = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (raiz.GetComponent<ControladorMecha>() != null)
                {
                    jugador = raiz;
                }
                if (raiz.name == "DaniadorDePrueba")
                {
                    UnityEngine.Object.DestroyImmediate(raiz);
                }
            }
            if (jugador == null)
            {
                throw new InvalidOperationException("Pruebas_Tony no tiene el mecha del jugador.");
            }

            GameObject objeto = new GameObject("DaniadorDePrueba");
            DaniadorDePrueba daniador = objeto.AddComponent<DaniadorDePrueba>();
            SerializedObject so = new SerializedObject(daniador);
            so.FindProperty("objetivo").objectReferenceValue = jugador;
            so.FindProperty("danio").floatValue = DanioPrueba;

            // InputAction suelta: se llena su serialización (nombre, tipo y un binding a la tecla H).
            SerializedProperty accion = so.FindProperty("aplicar");
            accion.FindPropertyRelative("m_Name").stringValue = NombreAccionDanio;
            accion.FindPropertyRelative("m_Type").intValue = (int)UnityEngine.InputSystem.InputActionType.Button;
            accion.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();
            SerializedProperty bindings = accion.FindPropertyRelative("m_SingletonActionBindings");
            bindings.arraySize = 1;
            SerializedProperty binding = bindings.GetArrayElementAtIndex(0);
            binding.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();
            binding.FindPropertyRelative("m_Path").stringValue = TeclaDanio;
            binding.FindPropertyRelative("m_Action").stringValue = NombreAccionDanio;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
    }
}
