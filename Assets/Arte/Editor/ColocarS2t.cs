using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 6-nov tarde (docs/playtest-06nov-b.md): ayudas de fotos para revisar assets antes de meterlos a la ciudad.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2t.FotoCarrosNuevos
    /// </summary>
    public static class ColocarS2t
    {
        public const string CarpetaCapturas = "docs/capturas/";

        /// <summary>V09 y V10 (y los de referencia V02/V03) de frente a 3/4 y de lado, en docs/capturas/s2t-carros-nuevos.png.</summary>
        [MenuItem("Mecha Cyberpunk/S2t/Foto de los carros nuevos")]
        public static void FotoCarrosNuevos()
        {
            FotoPrefabs(new[]
            {
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V02.prefab",
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V09.prefab",
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V03.prefab",
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V10.prefab",
            }, CarpetaCapturas + "s2t-carros-nuevos.png");
        }

        /// <summary>Foto con el ciclo de día y noche puesto en esa hora (sin guardar la escena), como las de ColocarS2nCielo.</summary>
        public static void FotoHora(float hora, Vector3 desde, Vector3 hacia, string ruta, float campo = 60f)
        {
            Mundo.Reloj.CicloDiaNoche ciclo = Object.FindFirstObjectByType<Mundo.Reloj.CicloDiaNoche>(FindObjectsInactive.Include);
            if (ciclo != null)
            {
                ciclo.Aplicar(hora);
                ColocarS2nCielo.SeguirNoche();
            }
            // En el juego LuzNocturna prende de noche las farolas y los letreros; en el editor no corre: se prenden para la foto.
            bool noche = hora >= 19f || hora < 6f;
            foreach (Mundo.Reloj.LuzNocturna ln in Object.FindObjectsByType<Mundo.Reloj.LuzNocturna>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Light l = new SerializedObject(ln).FindProperty("luz").objectReferenceValue as Light;
                if (l != null)
                {
                    l.enabled = noche;
                }
            }
            ColocarS2.Foto(desde, hacia, ruta, campo);
        }

        /// <summary>Fotos de revisión de la sesión: (nombre, desde, hacia) de día (13 h) y de noche (22 h), en docs/capturas/s2t-&lt;nombre&gt;-dia|noche.png.</summary>
        public static void Fotos(params (string nombre, Vector3 desde, Vector3 hacia)[] tomas)
        {
            ColocarS2.Abrir();
            foreach (var t in tomas)
            {
                FotoHora(13f, t.desde, t.hacia, CarpetaCapturas + "s2t-" + t.nombre + "-dia.png");
                FotoHora(22f, t.desde, t.hacia, CarpetaCapturas + "s2t-" + t.nombre + "-noche.png");
            }
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        [MenuItem("Mecha Cyberpunk/S2t/Fotos de la casa")]
        public static void FotosCasa()
        {
            Fotos(("casa-frente", new Vector3(-44f, 1.7f, -77f), new Vector3(-57f, 2.5f, -77f)),
                ("casa-sur", new Vector3(-47f, 2.5f, -92f), new Vector3(-56f, 2f, -76f)),
                ("casa-norte", new Vector3(-47f, 2.5f, -62f), new Vector3(-56f, 2f, -78f)));
        }

        public static void FotosCuarto()
        {
            Fotos(("cuarto-piso", new Vector3(-62.3f, 1.7f, -75.2f), new Vector3(-63.4f, 0.2f, -72.6f)),
                ("cuarto-pasillo", new Vector3(-63.6f, 1.6f, -77.0f), new Vector3(-60.5f, 1.4f, -78.4f)),
                ("cuarto-cama", new Vector3(-61.8f, 1.6f, -75.0f), new Vector3(-64f, 1.0f, -72f)));
        }

        /// <summary>Texto en el mundo (Canvas World Space con uGUI Text), con su cara hacia 'haciaDonde' (de donde se lee).</summary>
        public static GameObject TextoMundo(Transform padre, string nombre, string texto, Vector3 centro, Vector2 tam, Vector3 haciaDonde, Color color)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(padre, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform rt = go.GetComponent<RectTransform>();
            const float PixelesPorMetro = 100f;
            rt.sizeDelta = tam * PixelesPorMetro;
            rt.localScale = Vector3.one / PixelesPorMetro;
            // Un texto de uGUI se lee mirando por su +Z: el +Z va contrario a donde está quien lo lee.
            rt.SetPositionAndRotation(centro, Quaternion.LookRotation(-haciaDonde, Vector3.up));
            GameObject t = new GameObject("Texto", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            t.transform.SetParent(go.transform, false);
            RectTransform trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            UnityEngine.UI.Text txt = t.GetComponent<UnityEngine.UI.Text>();
            txt.text = texto;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 10;
            txt.resizeTextMaxSize = 200;
            txt.color = color;
            return go;
        }

        public const string RutaLugarCasino = "Assets/_Proyecto/Mundo/Datos/DatosLugar_Casino.asset";

        /// <summary>T-863: el casino VivaCoin abre las 24 horas (Tony: "siempre abierto, se puede entrar"). Idempotente.</summary>
        [MenuItem("Mecha Cyberpunk/S2t/Casino siempre abierto")]
        public static void CasinoSiempreAbierto()
        {
            Object datos = AssetDatabase.LoadMainAssetAtPath(RutaLugarCasino);
            if (datos == null)
            {
                Debug.LogError("[Arte] Falta " + RutaLugarCasino);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            SerializedProperty siempre = so.FindProperty("horario.siempreAbierto");
            SerializedProperty demo = so.FindProperty("cerradoEnDemo");
            if (!siempre.boolValue || demo.boolValue)
            {
                siempre.boolValue = true;
                demo.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            Debug.Log("[Arte] Casino VivaCoin: 24 horas");
        }

        /// <summary>
        /// Pone cada prefab en fila mirando a +Z (su frente, si está bien orientado) con una flecha roja hacia +Z en el piso y lo retrata desde
        /// adelante a 3/4. Arriba: vista desde el frente-derecha; abajo: de lado. Abre otra vez Ciudad_Prologo al terminar.
        /// </summary>
        public static void FotoPrefabs(string[] rutas, string salida)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sol = new GameObject("Sol");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.4f;
            sol.transform.rotation = Quaternion.Euler(45f, 200f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = Vector3.one * 10f;
            const float Paso = 7f;
            for (int i = 0; i < rutas.Length; i++)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(rutas[i]);
                if (p == null)
                {
                    Debug.LogWarning("[Arte] Falta " + rutas[i]);
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(p);
                g.transform.position = new Vector3(i * Paso, 0f, 0f);
                GameObject flecha = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flecha.transform.position = new Vector3(i * Paso, 0.02f, 3.6f);
                flecha.transform.localScale = new Vector3(0.3f, 0.04f, 1.2f);
                Material rojo = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.red };
                flecha.GetComponent<Renderer>().sharedMaterial = rojo;
            }
            float centro = (rutas.Length - 1) * Paso * 0.5f;
            Texture2D arriba = Retratar(new Vector3(centro + 6f, 6f, 16f), new Vector3(centro, 0.8f, 0f));
            Texture2D abajo = Retratar(new Vector3(centro, 3f, -22f), new Vector3(centro, 1f, 0f));
            Texture2D hoja = new Texture2D(1280, 1440, TextureFormat.RGB24, false);
            hoja.SetPixels(0, 720, 1280, 720, arriba.GetPixels());
            hoja.SetPixels(0, 0, 1280, 720, abajo.GetPixels());
            hoja.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(salida));
            File.WriteAllBytes(salida, hoja.EncodeToPNG());
            Debug.Log("[Arte] Foto en " + salida);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }


        private static Texture2D Retratar(Vector3 desde, Vector3 hacia)
        {
            GameObject camObj = new GameObject("Cam");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<UniversalAdditionalCameraData>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.22f, 0.26f);
            cam.fieldOfView = 50f;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(camObj);
            return foto;
        }
    }
}
