using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-545 (plan de la noche 3-nov, paso 3): la pistola W01 (imageassets/modelos3d/armas/W01_Pistola, LEEME.md).
    /// - Materiales URP Lit con las texturas que trae el FBX (armazón, corredera, cargador) y prefab W01_Pistola (21 cm, cañón a +Z, empuñadura
    ///   en el origen) con AnimacionPistola: corredera y cargador con las curvas de los .blend (Disparo y Recarga; ver ImportarT545).
    /// - Reemplaza la pistola de cubos en la mano del jugador (JugadorAPie.prefab), la pone en la mano de los SKULLS (PandilleroArmado_Skulls y
    ///   el jefe) con la misma pose de mano que la del jugador, y DirectorPandillas tira esta pistola en el piso (la que gira y brilla).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT545.Colocar
    /// </summary>
    public static class ColocarT545
    {
        public const string Carpeta = "Assets/Arte/Armas/W01_Pistola/";
        public const string RutaPrefab = Carpeta + "W01_Pistola.prefab";
        public const string NombreModelo = "W01";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private static readonly string[] PrefabsSkulls =
        {
            "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab",
            "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_JefeSkulls.prefab",
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-545/Pistola W01")]
        public static void Colocar()
        {
            Informe.Clear();
            Materiales(out Material armazon, out Material corredera, out Material bateria);
            GameObject prefab = Prefab(armazon, corredera, bateria);
            Pose enMano = Jugador(prefab);
            foreach (string ruta in PrefabsSkulls)
            {
                Skulls(ruta, prefab, enMano);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            int escena = 0;
            foreach (Transform mano in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == "ManoDer").ToList())
            {
                Transform pandillero = mano.GetComponentInParent<Transform>();
                Component p = null;
                for (Transform x = mano; x != null && p == null; x = x.parent)
                {
                    p = x.GetComponent("PandilleroAPie");
                }
                if (p == null || mano.Find(NombreModelo) != null)
                {
                    continue;
                }
                Object datos = new SerializedObject(p).FindProperty("datos").objectReferenceValue;
                if (datos == null || !datos.name.Contains("Calaveras") || p.GetComponent("PistolaPandillero") == null)
                {
                    continue;
                }
                Poner(mano, prefab, enMano);
                escena++;
            }
            MonoBehaviour director = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m != null && m.GetType().Name == "DirectorPandillas");
            if (director != null)
            {
                SerializedObject so = new SerializedObject(director);
                so.FindProperty("modeloPistola").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                Informe.AppendLine("DirectorPandillas tira la W01 en el piso");
            }
            UnityEngine.SceneManagement.Scene s = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
            Informe.AppendLine("SKULLS armados en la escena con la W01 (además de sus prefabs): " + escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t545-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-545 listo:\n" + Informe);
        }

        /// <summary>Capturas: la W01 sola, en la mano de un SKULL y en el piso (docs/capturas/prueba2-pistola-w01.png).</summary>
        public static void Capturas()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sol = new GameObject("Sol");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.5f;
            sol.transform.rotation = Quaternion.Euler(35f, 140f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = Vector3.one * 3f;
            GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            w.transform.position = new Vector3(3f, 0.6f, 0f);
            GameObject enPiso = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            enPiso.transform.SetPositionAndRotation(new Vector3(-3f, 0.03f, 0f), Quaternion.Euler(0f, 35f, 90f));
            GameObject skull = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsSkulls[0]));
            skull.transform.position = Vector3.zero;
            Transform mano = skull.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == NombreModelo);
            Texture2D hoja = new Texture2D(1200, 400, TextureFormat.RGB24, false);
            (Vector3 desde, Vector3 hacia)[] vistas =
            {
                (w.transform.position + new Vector3(-0.35f, 0.12f, 0.15f), w.transform.position + Vector3.up * 0.05f),
                (mano != null ? mano.position + new Vector3(-0.6f, 0.25f, 0.9f) : new Vector3(0f, 1.3f, 1.5f), mano != null ? mano.position : new Vector3(0f, 1f, 0f)),
                (enPiso.transform.position + new Vector3(0.4f, 0.35f, -0.3f), enPiso.transform.position),
            };
            for (int i = 0; i < vistas.Length; i++)
            {
                GameObject c = new GameObject("Cam");
                Camera cam = c.AddComponent<Camera>();
                c.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                cam.fieldOfView = 40f;
                cam.nearClipPlane = 0.02f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.25f, 0.27f, 0.32f);
                cam.transform.position = vistas[i].desde;
                cam.transform.LookAt(vistas[i].hacia);
                RenderTexture rt = new RenderTexture(400, 400, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                hoja.ReadPixels(new Rect(0, 0, 400, 400), i * 400, 0);
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(c);
            }
            hoja.Apply();
            File.WriteAllBytes("docs/capturas/prueba2-pistola-w01.png", hoja.EncodeToPNG());
        }

        private static void Materiales(out Material armazon, out Material corredera, out Material bateria)
        {
            string fbx = Carpeta + "W01_Pistola.fbx";
            ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
            string texturas = Carpeta + "Texturas";
            if (mi != null && !File.Exists(texturas + "/corredera_basecolor.JPEG") && !File.Exists(texturas + "/corredera_basecolor.jpeg"))
            {
                mi.ExtractTextures(texturas);
                AssetDatabase.Refresh();
            }
            if (mi != null && (mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.importAnimation))
            {
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importAnimation = false;
                mi.SaveAndReimport();
            }
            armazon = Material("W01_Armazon", "pistolaarmazon", texturas, 0.55f);
            corredera = Material("W01_Corredera", "corredera", texturas, 0.6f);
            bateria = Material("W01_Cargador", "tripo_image_1e646a2c", texturas, 0.4f);
        }

        private static Material Material(string nombre, string prefijo, string carpeta, float metal)
        {
            string ruta = Carpeta + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            Texture2D Buscar(string sufijo) => Directory.GetFiles(carpeta).Where(f => !f.EndsWith(".meta"))
                .Select(f => f.Replace('\\', '/')).Where(f => Path.GetFileName(f).StartsWith(prefijo) && Path.GetFileNameWithoutExtension(f).ToLower().Contains(sufijo))
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault(t => t != null);
            Texture2D color = Buscar("basecolor") ?? Buscar("0_0") ?? Directory.GetFiles(carpeta).Where(f => !f.EndsWith(".meta") && Path.GetFileName(f).StartsWith(prefijo))
                .Select(f => AssetDatabase.LoadAssetAtPath<Texture2D>(f.Replace('\\', '/'))).FirstOrDefault(t => t != null);
            Texture2D normal = Buscar("normal");
            if (normal != null)
            {
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
                if (ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                }
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetTexture("_BaseMap", color);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", 0.55f);
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssetIfDirty(m);
            Informe.AppendLine("Material " + nombre + ": color " + (color != null ? color.name : "FALTA") + ", normal " + (normal != null ? normal.name : "no"));
            return m;
        }

        private static GameObject Prefab(Material armazon, Material corredera, Material bateria)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "W01_Pistola.fbx");
            GameObject raiz = new GameObject("W01_Pistola");
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
            PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            modelo.name = "Modelo";
            // El modelo apunta a +X: el cañón va a +Z como la pistola del juego.
            modelo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Transform tArmazon = modelo.transform.Find("Armazon");
            Transform tCorredera = modelo.transform.Find("Corredera");
            Transform tBateria = modelo.transform.Find("Bateria");
            tArmazon.GetComponent<Renderer>().sharedMaterial = armazon;
            tCorredera.GetComponent<Renderer>().sharedMaterial = corredera;
            tBateria.GetComponent<Renderer>().sharedMaterial = bateria;
            // La empuñadura (donde está el cargador) en el origen: ahí la agarra la mano.
            Bounds b = tBateria.GetComponent<Renderer>().bounds;
            modelo.transform.localPosition -= new Vector3(b.center.x, b.max.y, b.center.z);
            Bounds todo = ArteT250.Limites(raiz);
            Type tipo = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Personaje.Combate.AnimacionPistola")).FirstOrDefault(t => t != null);
            if (tipo != null)
            {
                Component anim = raiz.AddComponent(tipo);
                SerializedObject so = new SerializedObject(anim);
                so.FindProperty("corredera").objectReferenceValue = tCorredera;
                so.FindProperty("bateria").objectReferenceValue = tBateria;
                so.FindProperty("escalaModelo").floatValue = 1f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("Prefab W01_Pistola: " + todo.size.ToString("F3") + " m (largo " + todo.size.z.ToString("F2") + ")");
            return guardado;
        }

        // Jugador: la pistola de cubos (Cuerpo pistola y Canon) se esconde y la W01 va en su lugar. Devuelve la pose de la pistola en la mano.
        private static Pose Jugador(GameObject prefab)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaJugador);
            Pose pose = new Pose(Vector3.zero, Quaternion.identity);
            try
            {
                Transform pistola = contenido.GetComponentsInChildren<Transform>(true).First(t => t.name == "Pistola" && t.Find("Canon") != null);
                foreach (string pieza in new[] { "Cuerpo pistola", "Canon" })
                {
                    Renderer r = pistola.Find(pieza)?.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.enabled = false;
                    }
                }
                Transform viejo = pistola.Find(NombreModelo);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pistola);
                w.name = NombreModelo;
                w.transform.localPosition = Vector3.zero;
                w.transform.localRotation = Quaternion.identity;
                w.transform.localScale = Vector3.one;
                pose = new Pose(pistola.localPosition, pistola.localRotation);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJugador);
                Informe.AppendLine("Jugador: W01 en la mano (pose de la pistola " + pose.position.ToString("F3") + ")");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            return pose;
        }

        private static void Skulls(string ruta, GameObject prefab, Pose enMano)
        {
            if (!File.Exists(ruta))
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform mano = contenido.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ManoDer");
                if (mano == null)
                {
                    Informe.AppendLine(Path.GetFileName(ruta) + ": sin ManoDer");
                    return;
                }
                Poner(mano, prefab, enMano);
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                Informe.AppendLine(Path.GetFileName(ruta) + ": W01 en la mano");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void Poner(Transform mano, GameObject prefab, Pose enMano)
        {
            Transform viejo = mano.Find(NombreModelo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mano);
            w.name = NombreModelo;
            w.transform.localPosition = enMano.position;
            w.transform.localRotation = enMano.rotation;
            w.transform.localScale = new Vector3(1f / Mathf.Max(0.001f, mano.lossyScale.x), 1f / Mathf.Max(0.001f, mano.lossyScale.y), 1f / Mathf.Max(0.001f, mano.lossyScale.z));
        }
    }
}
