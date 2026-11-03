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
    /// T-540 (plan de la noche 3-nov, paso 2): importa los modelos nuevos de imageassets/modelos3d con el flujo de T-250 (prefab con hijo Visual,
    /// URP Lit con su color y normal, pivote en la base, BoxCollider, Mesh LOD si pasan de 15 mil caras): kit de torres T20-T23 (de los dos zips de T23 se
    /// usa "T23_RemateTorre": misma malla que "(1)", textura más oscura que va con los pisos), poste rico N09, letreros 3D L10-L14 y la moto de reparto V07. Hace una hoja de vista previa en
    /// Logs/t540-modelos.png. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarT540.CrearTodo
    /// </summary>
    public static class ImportarT540
    {
        /// <summary>Medidas creíbles (m). Las piezas de torre se escalan por su ancho (la torre mide 18 m de lado); el armado ajusta el alto del piso.</summary>
        public static readonly ArteT250.Modelo[] Nuevos =
        {
            new ArteT250.Modelo("T20", ArteT250.Eje.Largo, 18f),
            new ArteT250.Modelo("T21", ArteT250.Eje.Largo, 18f),
            new ArteT250.Modelo("T22", ArteT250.Eje.Largo, 18f),
            new ArteT250.Modelo("T23", ArteT250.Eje.Largo, 18f),
            new ArteT250.Modelo("N09", ArteT250.Eje.Alto, 7.5f),
            new ArteT250.Modelo("L10", ArteT250.Eje.Largo, 6f),
            new ArteT250.Modelo("L11", ArteT250.Eje.Alto, 3.2f),
            new ArteT250.Modelo("L12", ArteT250.Eje.Largo, 4.2f),
            new ArteT250.Modelo("L13", ArteT250.Eje.Largo, 3.2f),
            new ArteT250.Modelo("L14", ArteT250.Eje.Largo, 3f),
            new ArteT250.Modelo("V07", ArteT250.Eje.Largo, 2.1f),
        };

        [MenuItem("Mecha Cyberpunk/T-540/Importar modelos nuevos")]
        public static void CrearTodo()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (ArteT250.Modelo modelo in Nuevos)
            {
                foreach (string t in new[] { "_basecolor.jpg", "_normal.jpg" })
                {
                    TextureImporter ti = AssetImporter.GetAtPath(modelo.Carpeta + "/" + modelo.Codigo + t) as TextureImporter;
                    if (ti != null && t == "_normal.jpg" && ti.textureType != TextureImporterType.NormalMap)
                    {
                        ti.textureType = TextureImporterType.NormalMap;
                        ti.SaveAndReimport();
                    }
                }
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
                    if (!importador.isReadable)
                    {
                        importador.isReadable = true; // el armado de torres y las pantallas leen la malla
                        cambio = true;
                    }
                    if (cambio)
                    {
                        importador.SaveAndReimport();
                    }
                }
                informe.AppendLine(ArteT250.CrearPrefab(modelo));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t540-importar.txt", informe.ToString());
            Hoja();
            Debug.Log("[Arte] T-540 importados:\n" + informe);
        }

        /// <summary>T-544: vistas ortográficas de frente (-Z), de lado (+X), de atrás (+Z) y del otro lado (-X) de las pantallas, para ubicar el área de la pantalla.</summary>
        public static void VistasPantallas()
        {
            string[] rutas = { "Assets/Arte/Exteriores/Prefabs/E03_PantallaGigante.prefab", "Assets/Arte/Calle/Prefabs/CY02_PantallaVertical.prefab",
                "Assets/Arte/Exteriores/Prefabs/N08.prefab", "Assets/Arte/Exteriores/Prefabs/T22.prefab" };
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sol = new GameObject("Sol");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            sol.transform.rotation = Quaternion.Euler(30f, 20f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.7f);
            const int Px = 400;
            Texture2D hoja = new Texture2D(Px * 4, Px * rutas.Length, TextureFormat.RGB24, false);
            Vector3[] dirs = { Vector3.back, Vector3.right, Vector3.forward, Vector3.left };
            for (int i = 0; i < rutas.Length; i++)
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rutas[i]));
                Bounds b = ArteT250.Limites(g);
                for (int d = 0; d < 4; d++)
                {
                    GameObject camObj = new GameObject("Cam");
                    Camera cam = camObj.AddComponent<Camera>();
                    camObj.AddComponent<UniversalAdditionalCameraData>();
                    cam.orthographic = true;
                    cam.orthographicSize = Mathf.Max(b.size.x, b.size.y, b.size.z) * 0.55f;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
                    cam.transform.position = b.center + dirs[d] * 50f;
                    cam.transform.LookAt(b.center);
                    RenderTexture rt = new RenderTexture(Px, Px, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    hoja.ReadPixels(new Rect(0, 0, Px, Px), d * Px, (rutas.Length - 1 - i) * Px);
                    RenderTexture.active = null;
                    rt.Release();
                    Object.DestroyImmediate(camObj);
                }
                File.AppendAllText("Logs/t544-limites.txt", rutas[i] + " centro " + b.center + " tam " + b.size + System.Environment.NewLine);
                Object.DestroyImmediate(g);
            }
            hoja.Apply();
            File.WriteAllBytes("Logs/t544-vistas.png", hoja.EncodeToPNG());
        }

        /// <summary>Vista previa: cada modelo en una fila con luz de día, a Logs/t540-modelos.png.</summary>
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
            const int Px = 512;
            Texture2D hoja = new Texture2D(Px * 4, Px * 3, TextureFormat.RGB24, false);
            for (int i = 0; i < Nuevos.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Nuevos[i].RutaPrefab);
                if (prefab == null)
                {
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds b = ArteT250.Limites(g);
                GameObject camObj = new GameObject("Cam");
                Camera cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<UniversalAdditionalCameraData>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.22f, 0.26f);
                ArteT250.Encuadrar(cam, b, new Vector3(-0.6f, 0.35f, -1f));
                RenderTexture rt = new RenderTexture(Px, Px, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                hoja.ReadPixels(new Rect(0, 0, Px, Px), (i % 4) * Px, (2 - i / 4) * Px);
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(camObj);
                Object.DestroyImmediate(g);
            }
            hoja.Apply();
            File.WriteAllBytes("Logs/t540-modelos.png", hoja.EncodeToPNG());
        }
    }
}
