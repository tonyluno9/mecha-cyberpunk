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

        public const string CarpetaCalle = "imageassets/modelos3d/calle/";

        /// <summary>T-891: los modelos de la calle que sustituyen lo hecho en código (medidas de docs/pedido-arte-08nov.md).</summary>
        public static readonly Entrada[] Calle =
        {
            new Entrada(CarpetaCalle + "TK01.zip", "TK01_BaseTinaco", ArteT250.Eje.Largo, 1.9f),
            new Entrada(CarpetaCalle + "NE01.zip", "NE01_NeonBandera", ArteT250.Eje.Alto, 0.8f),
            new Entrada(CarpetaCalle + "NE01B.zip", "NE01B_NeonBandera", ArteT250.Eje.Alto, 0.8f),
            new Entrada(CarpetaCalle + "NE02.zip", "NE02_NeonPared", ArteT250.Eje.Largo, 0.9f),
            new Entrada(CarpetaCalle + "AL01.zip", "AL01_Alcantarilla", ArteT250.Eje.Largo, 0.9f),
            new Entrada(CarpetaCalle + "MT01.zip", "MT01_MuroTrasero", ArteT250.Eje.Alto, 6f),
            new Entrada(CarpetaCalle + "TL01.zip", "TL01_Toldo", ArteT250.Eje.Largo, 2.5f),
            new Entrada(CarpetaCalle + "FA01.zip", "FA01_Farmacia", ArteT250.Eje.Largo, 3f),
            new Entrada(CarpetaCalle + "NV01.zip", "NV01_FachadaOficinas", ArteT250.Eje.Largo, 6f),
            new Entrada(CarpetaCalle + "NV02.zip", "NV02_PlantaBajaNova", ArteT250.Eje.Largo, 6f),
            new Entrada(CarpetaCalle + "BL01.zip", "BL01_Bolardo", ArteT250.Eje.Alto, 0.9f),
            new Entrada(CarpetaCalle + "BT01.zip", "BT01_BoteBasura", ArteT250.Eje.Alto, 0.95f),
        };


        /// <summary>T-891: si el código es de una pieza de imageassets/modelos3d/calle/.</summary>
        public static bool EsDeCalle(string codigo)
        {
            // Local y no estática: los modelos estáticos de esta clase la llaman mientras se inicializa.
            return new[] { "TK01", "NE01", "NE02", "AL01", "MT01", "TL01", "FA01", "NV01", "NV02", "BL01", "BT01", "ES01", "BS01", "PS01", "PC01", "O01" }.Any(codigo.StartsWith);
        }

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

        /// <summary>Batchmode de T-891: importa las piezas de calle y las fotografía de frente, de lado y desde arriba.</summary>
        public static void ImportarCalleYRevisar()
        {
            ImportarCalle();
            Revisar(Calle);
        }

        /// <summary>T-891: importa las piezas de calle, enciende lo que brilla y les activa Mesh LOD (son 12 a 19 mil triángulos cada una).</summary>
        [MenuItem("Mecha Cyberpunk/S2p/2 Importar piezas de calle (B5)")]
        public static void ImportarCalle()
        {
            Importar(Calle);
            foreach (Entrada e in Calle.Concat(Fachadas))
            {
                ModelImporter imp = AssetImporter.GetAtPath(e.Modelo.RutaFbx) as ModelImporter;
                if (imp != null && !imp.generateMeshLods)
                {
                    imp.generateMeshLods = true;
                    imp.SaveAndReimport();
                }
            }
            foreach (Entrada e in Calle.Where(e => e.Modelo.Codigo.StartsWith("NE") || e.Modelo.Codigo.StartsWith("FA")))
            {
                Emision(e.Modelo, UmbralNeon, 0f, IntensidadNeon);
            }
            Emision(Calle.First(e => e.Modelo.Codigo.StartsWith("NV02")).Modelo, UmbralMarcoNova, SaturacionMarcoNova, IntensidadNeon);
            foreach (string c in new[] { "NV01_FachadaOficinas", "NV02_PlantaBajaNova" })
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(Calle.First(e => e.Modelo.Codigo == c).Modelo.RutaMaterial);
                m.enableInstancing = true; // Nova repite estos módulos decenas de veces
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        private const float UmbralNeon = 0.62f;
        private const float UmbralMarcoNova = 0.45f;
        private const float SaturacionMarcoNova = 0.35f;
        private const float IntensidadNeon = 3f;

        /// <summary>
        /// Los tubos de neón no son material aparte: vienen pintados en la textura. Se copia a &lt;código&gt;_emision.png lo que pasa del umbral de
        /// brillo y de saturación, y el material lo usa como mapa de emisión. Así brilla el tubo y no la caja.
        /// </summary>
        private static void Emision(ArteT250.Modelo m, float umbral, float saturacionMinima, float intensidad)
        {
            string origen = m.Carpeta + "/" + m.Codigo + "_basecolor.jpg";
            string destino = m.Carpeta + "/" + m.Codigo + "_emision.png";
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(origen));
            Color32[] p = t.GetPixels32();
            for (int i = 0; i < p.Length; i++)
            {
                Color.RGBToHSV(p[i], out float _, out float s, out float v);
                if (v < umbral || s < saturacionMinima)
                {
                    p[i] = new Color32(0, 0, 0, 255);
                }
            }
            t.SetPixels32(p);
            t.Apply();
            File.WriteAllBytes(destino, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(destino);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(m.RutaMaterial);
            mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(destino));
            mat.SetColor("_EmissionColor", Color.white * intensidad);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);
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
