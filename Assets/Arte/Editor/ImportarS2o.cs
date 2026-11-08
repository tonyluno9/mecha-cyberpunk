using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov: mete los modelos de Tony que llegaron en imageassets/modelos3d (zips de Tripo): L15 letrero de El Canepa,
    /// P10 puerta del cuarto, P11 entrada de la casa y P12 fachada del consultorio. Desempaca el .fbx como &lt;código&gt;.fbx y sus
    /// texturas como &lt;código&gt;_basecolor/_normal.jpg, y arma el prefab con el flujo de T-250 (hijo Visual, URP Lit, pivote en la base,
    /// medida real, BoxCollider). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2o.Importar
    /// </summary>
    public static class ImportarS2o
    {
        public static readonly ArteT250.Modelo[] Nuevos =
        {
            new ArteT250.Modelo("L15", ArteT250.Eje.Largo, 4.2f),  // letrero EL CANEPA / TALLER MECÁNICO
            new ArteT250.Modelo("P10", ArteT250.Eje.Alto, 2.1f),   // puerta del cuarto
            new ArteT250.Modelo("P11", ArteT250.Eje.Alto, 3.1f),   // entrada de la casa (hueco de 3 x 3.2 m)
            new ArteT250.Modelo("P12", ArteT250.Eje.Alto, 3.4f),   // fachada del consultorio del Doc
        };

        public static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/S2o/0 Importar L15, P10, P11 y P12")]
        public static void Importar()
        {
            Informe.Clear();
            foreach (ArteT250.Modelo m in Nuevos)
            {
                Desempacar("imageassets/modelos3d/" + m.Codigo + ".zip", m);
            }
            AssetDatabase.Refresh();
            foreach (ArteT250.Modelo m in Nuevos)
            {
                ModelImporter imp = AssetImporter.GetAtPath(m.RutaFbx) as ModelImporter;
                if (imp != null && imp.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.SaveAndReimport();
                }
                Informe.AppendLine(ArteT250.CrearPrefab(m));
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] S2o importados:\n" + Informe);
        }

        private static void Desempacar(string zip, ArteT250.Modelo m)
        {
            if (!File.Exists(zip))
            {
                Informe.AppendLine("falta " + zip);
                return;
            }
            Directory.CreateDirectory(m.Carpeta);
            using (ZipArchive z = ZipFile.OpenRead(zip))
            {
                foreach (ZipArchiveEntry e in z.Entries)
                {
                    string n = e.Name.ToLowerInvariant();
                    string destino = null;
                    if (n.EndsWith(".fbx"))
                    {
                        destino = m.RutaFbx;
                    }
                    else if (n.Contains("normal") && (n.EndsWith(".jpeg") || n.EndsWith(".jpg")))
                    {
                        destino = m.Carpeta + "/" + m.Codigo + "_normal.jpg";
                    }
                    else if ((n.Contains("basecolor") || n.StartsWith("tripo_image")) && (n.EndsWith(".jpeg") || n.EndsWith(".jpg") || n.EndsWith(".png")))
                    {
                        destino = m.Carpeta + "/" + m.Codigo + "_basecolor.jpg";
                    }
                    if (destino == null)
                    {
                        continue;
                    }
                    if (File.Exists(destino) && new FileInfo(destino).Length == e.Length)
                    {
                        continue;
                    }
                    e.ExtractToFile(destino, true);
                    Informe.AppendLine("copiado " + e.Name + " -> " + destino);
                }
            }
        }
    }
}
