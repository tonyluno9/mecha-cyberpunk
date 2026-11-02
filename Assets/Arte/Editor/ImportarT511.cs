using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-511 (paso 2 del plan de personajes y carros): V02, V03, V04, V05, V06 y V08 con el flujo de T-250 (prefab con hijo Visual, URP Lit,
    /// pivote en la base, BoxCollider; Mesh LOD si pasan de 15 mil caras).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarT511.CrearTodo
    /// </summary>
    public static class ImportarT511
    {
        /// <summary>Largo real de cada carro (m). El moto de policía (V08) mide 2.3 m.</summary>
        public static readonly ArteT250.Modelo[] Nuevos =
        {
            new ArteT250.Modelo("V02", ArteT250.Eje.Largo, 4.5f),
            new ArteT250.Modelo("V03", ArteT250.Eje.Largo, 5f),
            new ArteT250.Modelo("V04", ArteT250.Eje.Largo, 4.7f),
            new ArteT250.Modelo("V05", ArteT250.Eje.Largo, 4.6f),
            new ArteT250.Modelo("V06", ArteT250.Eje.Largo, 4.6f),
            new ArteT250.Modelo("V08", ArteT250.Eje.Largo, 2.3f),
        };

        /// <summary>Giro (°, eje Y) que lleva el frente de cada modelo a +Z y su eje largo a Z: los FBX vienen girados (V02 27°, V03 41°) y con el frente a -X o +X.</summary>
        public static float GiroDe(string codigo)
        {
            switch (codigo)
            {
                case "V02": return 117.4f;
                case "V03": return 131.5f;
                case "V04": return 90f;
                case "V05":
                case "V06": return 180f;
                case "V08": return 90f;
                default: return 0f;
            }
        }

        public static string[] RutasPrefab => Nuevos.Select(m => m.RutaPrefab).ToArray();

        [MenuItem("Mecha Cyberpunk/T-511/Crear prefabs de carros")]
        public static void CrearTodo()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (ArteT250.Modelo modelo in Nuevos)
            {
                int triangulos = AssetDatabase.LoadAllAssetsAtPath(modelo.RutaFbx).OfType<Mesh>().Sum(m => m.triangles.Length / 3);
                ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(modelo.RutaFbx);
                if (importador != null && triangulos > 15000 && !importador.generateMeshLods)
                {
                    importador.generateMeshLods = true;
                    importador.SaveAndReimport();
                }
                informe.AppendLine(ArteT250.CrearPrefab(modelo));
                Orientar(modelo);
            }
            File.WriteAllText("Logs/t511-informe.txt", informe.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T-511:\n" + informe);
        }

        // Medida exacta del modelo: se recorren los vértices (los bounds de un renderer girado salen inflados).
        public static Bounds LimitesExactos(GameObject objeto)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 m = f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 w = m.MultiplyPoint3x4(v);
                    if (primero)
                    {
                        b = new Bounds(w, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(w);
                    }
                }
            }
            return b;
        }

        /// <summary>Gira el Visual (frente a +Z), lo escala a su largo real, lo centra con los pies en y=0 y ajusta el BoxCollider del prefab.</summary>
        private static void Orientar(ArteT250.Modelo modelo)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(modelo.RutaPrefab);
            Transform visual = raiz.transform.Find("Visual");
            visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.Euler(0f, GiroDe(modelo.Codigo), 0f);
            Bounds b = LimitesExactos(raiz);
            float escala = modelo.Medida / b.size.z;
            visual.localScale = Vector3.one * escala;
            b = LimitesExactos(raiz);
            visual.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = LimitesExactos(raiz);
            BoxCollider caja = raiz.GetComponent<BoxCollider>();
            caja.center = b.center;
            caja.size = b.size;
            PrefabUtility.SaveAsPrefabAsset(raiz, modelo.RutaPrefab);
            PrefabUtility.UnloadPrefabContents(raiz);
        }

    }
}
