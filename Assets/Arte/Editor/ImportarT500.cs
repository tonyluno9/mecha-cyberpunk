using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-500 (paso 0 del plan de ciudad): N01-N08 con el flujo de T-250 (prefab con hijo Visual, URP Lit, pivote en la base)
    /// y el informe de los tres personajes de Tripo (man, woman, woman2) con las animaciones que traen.
    /// </summary>
    public static class ImportarT500
    {
        public const string RutaCaptura = "docs/capturas/T-500-1.png";

        public static readonly string[] Personajes =
        {
            "Assets/Arte/Personajes/man/man.fbx",
            "Assets/Arte/Personajes/woman/woman.fbx",
            "Assets/Arte/Personajes/woman2/woman2.fbx",
        };

        /// <summary>Medidas creíbles (m).</summary>
        public static readonly ArteT250.Modelo[] Modelos =
        {
            new ArteT250.Modelo("N01", ArteT250.Eje.Alto, 0.5f),
            new ArteT250.Modelo("N02", ArteT250.Eje.Largo, 24f),
            new ArteT250.Modelo("N03a", ArteT250.Eje.Alto, 2.6f),
            new ArteT250.Modelo("N03b", ArteT250.Eje.Alto, 2.6f),
            new ArteT250.Modelo("N04", ArteT250.Eje.Alto, 3f),
            new ArteT250.Modelo("N05", ArteT250.Eje.Alto, 1.2f),
            new ArteT250.Modelo("N06", ArteT250.Eje.Alto, 0.9f),
            new ArteT250.Modelo("N07", ArteT250.Eje.Alto, 5f),
            new ArteT250.Modelo("N08", ArteT250.Eje.Alto, 4f),
        };

        public static string[] RutasPrefab => Modelos.Select(m => m.RutaPrefab).ToArray();

        [MenuItem("Mecha Cyberpunk/T-500/Crear prefabs, informe y captura")]
        public static void CrearTodo()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (string ruta in Personajes)
            {
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            }
            foreach (ArteT250.Modelo modelo in Modelos)
            {
                // Más de 15 mil caras (N03a, N03b): Mesh LOD de Unity, como en T-425.
                int triangulos = AssetDatabase.LoadAllAssetsAtPath(modelo.RutaFbx).OfType<Mesh>().Sum(m => m.triangles.Length / 3);
                ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(modelo.RutaFbx);
                if (importador != null && triangulos > 15000 && !importador.generateMeshLods)
                {
                    importador.generateMeshLods = true;
                    importador.SaveAndReimport();
                }
                informe.AppendLine(ArteT250.CrearPrefab(modelo));
            }
            foreach (string ruta in Personajes)
            {
                informe.AppendLine(InformePersonaje(ruta));
            }
            File.WriteAllText("Logs/t500-informe.txt", informe.ToString());
            AssetDatabase.SaveAssets();
            Capturar();
            Debug.Log("[Arte] T-500:\n" + informe);
        }

        private static string InformePersonaje(string ruta)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (fbx == null)
            {
                return ruta + "\tFALTA";
            }
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToArray();
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(ruta);
            int triangulos = fbx.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s => s.sharedMesh != null).Sum(s => s.sharedMesh.triangles.Length / 3);
            int huesos = fbx.GetComponentsInChildren<Transform>(true).Length;
            return string.Format("{0}\ttriangulos {1}\ttransforms {2}\ttipo {3}\tclips {4}: {5}", ruta, triangulos, huesos, importador.animationType,
                clips.Length, string.Join(", ", clips.Select(c => c.name + " (" + c.length.ToString("F2") + "s)")));
        }

        private static void Capturar()
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject grupo = new GameObject("Fila_N");
            float x = 0f;
            foreach (ArteT250.Modelo modelo in Modelos)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaPrefab);
                if (prefab == null)
                {
                    continue;
                }
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                instancia.transform.SetParent(grupo.transform, false);
                Bounds b = ArteT250.Limites(instancia);
                instancia.transform.localPosition = new Vector3(x + b.size.x * 0.5f, 0f, 0f);
                x += b.size.x + 1.5f;
            }
            GameObject persona = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            persona.name = "Persona_1.8m";
            persona.transform.SetParent(grupo.transform, false);
            persona.transform.localPosition = new Vector3(x + 0.5f, 0.9f, 0f);
            persona.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.position = new Vector3(x * 0.5f, 0f, 0f);
            suelo.transform.localScale = new Vector3(x * 0.1f + 2f, 1f, 6f);
            ArteT250.PonerLuz();
            GameObject camaraObjeto = new GameObject("Camara");
            Camera camara = camaraObjeto.AddComponent<Camera>();
            Bounds todo = ArteT250.Limites(grupo);
            ArteT250.Encuadrar(camara, todo, new Vector3(0.05f, 0.15f, -1f));
            camara.transform.position = new Vector3(todo.center.x, todo.center.y + 1f, todo.min.z - todo.size.x * 0.85f);
            camara.transform.LookAt(todo.center);
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            RenderTexture destino = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            imagen.Apply();
            File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
            camara.targetTexture = null;
            RenderTexture.active = null;
        }
    }
}
