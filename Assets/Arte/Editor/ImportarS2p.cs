using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 9-nov: mete las fachadas con hueco y las puertas sueltas de docs/tareas-piezas-08nov.md (B3), desde los zips de
    /// imageassets/modelos3d/piezas/. Cada zip pasa por el flujo de T-250 (hijo Visual, URP Lit, pivote en la base, medida real, BoxCollider)
    /// con su propio código, sin tocar los prefabs viejos (C01_FachadaLocal_cy, etc.), que siguen en uso en otros lugares. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2p.ImportarYRevisar
    /// </summary>
    public static class ImportarS2p
    {
        /// <summary>Un zip y el modelo que sale de él.</summary>
        public readonly struct Entrada
        {
            public readonly string Zip;
            public readonly ArteT250.Modelo Modelo;

            public Entrada(string zip, string codigo, ArteT250.Eje eje, float medida)
            {
                Zip = zip;
                Modelo = new ArteT250.Modelo(codigo, eje, medida);
            }
        }

        public const string CarpetaPiezas = "imageassets/modelos3d/piezas/";

        public static readonly Entrada[] Fachadas =
        {
            new Entrada(CarpetaPiezas + "C01FACHADA.zip", "C01_Fachada", ArteT250.Eje.Alto, 4f),
            new Entrada(CarpetaPiezas + "C02FACHADA.zip", "C02_Fachada", ArteT250.Eje.Alto, 3.5f),
            new Entrada(CarpetaPiezas + "C03.zip", "C03_Fachada", ArteT250.Eje.Alto, 3.2f),
            new Entrada(CarpetaPiezas + "P11FACHADA.zip", "P11_Fachada", ArteT250.Eje.Alto, 3.1f),
            new Entrada(CarpetaPiezas + "P12HOSPITAL.zip", "P12_Fachada", ArteT250.Eje.Alto, 3.4f),
            new Entrada(CarpetaPiezas + "PU01.zip", "PU01_Puerta", ArteT250.Eje.Alto, 2.1f),
            new Entrada(CarpetaPiezas + "PU03.zip", "PU03_Reja", ArteT250.Eje.Alto, 2.4f),
            new Entrada(CarpetaPiezas + "PU04.zip", "PU04_Metalica", ArteT250.Eje.Alto, 2.1f),
            new Entrada(CarpetaPiezas + "GARAGEPUERTA.zip", "PU02_Garaje", ArteT250.Eje.Alto, 2.6f),
        };

        public static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/S2p/0 Importar fachadas y puertas (B3)")]
        public static void Importar()
        {
            Importar(Fachadas);
        }

        public static void Importar(Entrada[] entradas)
        {
            Informe.Clear();
            foreach (Entrada e in entradas)
            {
                Desempacar(e);
            }
            AssetDatabase.Refresh();
            foreach (Entrada e in entradas)
            {
                ModelImporter imp = AssetImporter.GetAtPath(e.Modelo.RutaFbx) as ModelImporter;
                if (imp != null && imp.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.SaveAndReimport();
                }
                Informe.AppendLine(ArteT250.CrearPrefab(e.Modelo));
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/s2p-importados.txt", Informe.ToString());
            Debug.Log("[Arte] S2p importados:\n" + Informe);
        }

        private static void Desempacar(Entrada e)
        {
            if (!File.Exists(e.Zip))
            {
                Informe.AppendLine("falta " + e.Zip);
                return;
            }
            ArteT250.Modelo m = e.Modelo;
            Directory.CreateDirectory(m.Carpeta);
            using (ZipArchive z = ZipFile.OpenRead(e.Zip))
            {
                foreach (ZipArchiveEntry a in z.Entries)
                {
                    string n = a.Name.ToLowerInvariant();
                    string destino = null;
                    if (n.EndsWith(".fbx"))
                    {
                        destino = m.RutaFbx;
                    }
                    else if (n.Contains("normal") && (n.EndsWith(".jpeg") || n.EndsWith(".jpg")))
                    {
                        destino = m.Carpeta + "/" + m.Codigo + "_normal.jpg";
                    }
                    else if (n.Contains("basecolor") && (n.EndsWith(".jpeg") || n.EndsWith(".jpg") || n.EndsWith(".png")))
                    {
                        destino = m.Carpeta + "/" + m.Codigo + "_basecolor.jpg";
                    }
                    if (destino == null || (File.Exists(destino) && new FileInfo(destino).Length == a.Length))
                    {
                        continue;
                    }
                    a.ExtractToFile(destino, true);
                }
            }
        }

        /// <summary>Batchmode: importa, mide cada pieza y saca una foto de frente (desde -Z) en Logs/s2p-pre-&lt;código&gt;.png.</summary>
        public static void ImportarYRevisar()
        {
            Importar();
            Revisar(Fachadas);
        }

        public static void Revisar(Entrada[] entradas)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sol = new GameObject("Sol");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.6f;
            sol.transform.rotation = Quaternion.Euler(35f, 20f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            ColocarS2.Informe.Clear();
            KitS2c.GuardarProfundidad = true;
            foreach (Entrada e in entradas)
            {
                KitS2c.Medir(e.Modelo.Codigo);
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(KitS2c.Prefab(e.Modelo.Codigo));
                Bounds b = ArteT250.Limites(g);
                float d = Mathf.Max(b.size.x, b.size.y) * 1.3f + 1f;
                ColocarS2.Foto(b.center + Vector3.back * (d + b.extents.z), b.center, "Logs/s2p-pre-" + e.Modelo.Codigo + ".png", 50f);
                Object.DestroyImmediate(g);
            }
            KitS2c.GuardarProfundidad = false;
            File.AppendAllText("Logs/s2p-importados.txt", ColocarS2.Informe.ToString());
            Debug.Log("[Arte] S2p medidas:\n" + ColocarS2.Informe);
        }
    }
}
