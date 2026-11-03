using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-550: importa los muebles de interior I01-I21 con el flujo de T-250 (prefab con hijo Visual, URP Lit con su color, pivote en la base, BoxCollider,
    /// Mesh LOD si pasan de 15 mil caras) y crea los materiales de pared X01-X05 (Repeat; el UV de los cuartos va en metros y el mosaico mide 2 m).
    /// Hoja de vista previa en Logs/t550-modelos.png. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarT550.CrearTodo
    /// </summary>
    public static class ImportarT550
    {
        /// <summary>Lado del mosaico de pared en metros: con UV en metros, el Tiling del material es 1 / este valor.</summary>
        public const float MetrosPorMosaico = 2f;
        public const string CarpetaMateriales = "Assets/Arte/Interiores/Materiales/";

        /// <summary>Medidas creíbles (m; persona 1.8). Alto = lo que mide de alto; Largo = su lado más largo en planta.</summary>
        public static readonly ArteT250.Modelo[] Nuevos =
        {
            new ArteT250.Modelo("I01", ArteT250.Eje.Alto, 1.9f),   // ropero
            new ArteT250.Modelo("I02", ArteT250.Eje.Largo, 1.3f),  // escritorio con CRT
            new ArteT250.Modelo("I03", ArteT250.Eje.Alto, 0.95f),  // silla
            new ArteT250.Modelo("I04", ArteT250.Eje.Alto, 1.2f),   // ventana con barrotes
            new ArteT250.Modelo("I05", ArteT250.Eje.Alto, 0.5f),   // mochila
            new ArteT250.Modelo("I06", ArteT250.Eje.Alto, 1.7f),   // brazo robótico (cuelga del techo)
            new ArteT250.Modelo("I07", ArteT250.Eje.Largo, 2f),    // sillón de cirugía
            new ArteT250.Modelo("I08", ArteT250.Eje.Alto, 1.8f),   // vitrina de implantes
            new ArteT250.Modelo("I09", ArteT250.Eje.Alto, 1f),     // lavabo
            new ArteT250.Modelo("I10", ArteT250.Eje.Largo, 0.5f),  // monitor
            new ArteT250.Modelo("I11", ArteT250.Eje.Largo, 2f),    // mesa de blackjack
            new ArteT250.Modelo("I12", ArteT250.Eje.Alto, 1f),     // silla de casino
            new ArteT250.Modelo("I13", ArteT250.Eje.Alto, 1.3f),   // candil de neón
            new ArteT250.Modelo("I14", ArteT250.Eje.Largo, 4f),    // barra con botellas
            new ArteT250.Modelo("I15", ArteT250.Eje.Alto, 0.8f),   // banco de barra
            new ArteT250.Modelo("I16", ArteT250.Eje.Alto, 3.2f),   // arco dorado
            new ArteT250.Modelo("I17", ArteT250.Eje.Largo, 2f),    // vitrina de armas
            new ArteT250.Modelo("I18", ArteT250.Eje.Largo, 2f),    // rifles en pared
            new ArteT250.Modelo("I19", ArteT250.Eje.Alto, 2.4f),   // reja de tijera
            new ArteT250.Modelo("I20", ArteT250.Eje.Alto, 2f),     // estante de drones
            new ArteT250.Modelo("I21", ArteT250.Eje.Largo, 0.9f),  // lámpara de brazo
        };

        /// <summary>Ruta del material de pared X0n (1 a 5).</summary>
        public static string RutaMaterialPared(int n)
        {
            return CarpetaMateriales + "X0" + n + "_Pared.mat";
        }

        [MenuItem("Mecha Cyberpunk/T-550/Importar interiores")]
        public static void CrearTodo()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (ArteT250.Modelo modelo in Nuevos)
            {
                ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(modelo.RutaFbx);
                if (importador != null)
                {
                    int triangulos = AssetDatabase.LoadAllAssetsAtPath(modelo.RutaFbx).OfType<Mesh>().Sum(m => m.triangles.Length / 3);
                    bool cambio = false;
                    if (importador.materialImportMode != ModelImporterMaterialImportMode.None)
                    {
                        importador.materialImportMode = ModelImporterMaterialImportMode.None;
                        cambio = true;
                    }
                    if (triangulos > 15000 && !importador.generateMeshLods)
                    {
                        importador.generateMeshLods = true;
                        cambio = true;
                    }
                    if (cambio)
                    {
                        importador.SaveAndReimport();
                    }
                }
                informe.AppendLine(ArteT250.CrearPrefab(modelo));
            }
            Directory.CreateDirectory(CarpetaMateriales);
            for (int n = 1; n <= 5; n++)
            {
                CrearMaterialPared(n);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t550-importar.txt", informe.ToString());
            Hoja();
            Debug.Log("[Arte] T-550 importados:\n" + informe);
        }

        private static void CrearMaterialPared(int n)
        {
            string ruta = RutaMaterialPared(n);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arte/Interiores/Texturas/X0" + n + ".png");
            m.SetTexture("_BaseMap", t);
            m.SetTexture("_MainTex", t);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.1f);
            m.SetFloat("_Metallic", 0f);
            Vector2 mosaico = Vector2.one / MetrosPorMosaico;
            m.SetTextureScale("_BaseMap", mosaico);
            m.SetTextureScale("_MainTex", mosaico);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Vista previa: cada mueble en una celda con luz de día y una cápsula de 1.8 m al lado, a Logs/t550-modelos.png.</summary>
        public static void Hoja()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sol = new GameObject("Sol");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.6f;
            sol.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            const int Px = 400;
            const int Cols = 6;
            Texture2D hoja = new Texture2D(Px * Cols, Px * 4, TextureFormat.RGB24, false);
            for (int i = 0; i < Nuevos.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Nuevos[i].RutaPrefab);
                if (prefab == null)
                {
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds b = ArteT250.Limites(g);
                GameObject persona = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                persona.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
                persona.transform.position = new Vector3(b.max.x + 0.5f, 0.9f, b.center.z);
                Object.DestroyImmediate(persona.GetComponent<Collider>());
                Bounds todo = ArteT250.Limites(g);
                todo.Encapsulate(new Bounds(persona.transform.position, new Vector3(0.45f, 1.8f, 0.45f)));
                GameObject camObj = new GameObject("Cam");
                Camera cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<UniversalAdditionalCameraData>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.22f, 0.26f);
                ArteT250.Encuadrar(cam, todo, new Vector3(-0.6f, 0.35f, -1f));
                RenderTexture rt = new RenderTexture(Px, Px, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                hoja.ReadPixels(new Rect(0, 0, Px, Px), (i % Cols) * Px, (3 - i / Cols) * Px);
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(camObj);
                Object.DestroyImmediate(persona);
                Object.DestroyImmediate(g);
            }
            hoja.Apply();
            File.WriteAllBytes("Logs/t550-modelos.png", hoja.EncodeToPNG());
        }
    }
}
