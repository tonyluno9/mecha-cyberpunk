using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-742: las piezas de los carros (carrocerías V01a, V01b y V05 sin puertas ni ruedas, sus puertas de Blender y la llanta única RU01).
    /// Desempaca cada .zip de imageassets/modelos3d/piezas/ en Assets/Arte/Vehiculos/Piezas/&lt;código&gt;/ con los nombres de V02-V08
    /// (&lt;nombre&gt;.fbx, &lt;nombre&gt;_basecolor.jpg, &lt;nombre&gt;_normal.jpg) y copia las puertas .fbx. Solo escribe lo que cambió.
    /// Los archivos se buscan con <see cref="ImportarS1n.Buscar"/>. Ver docs/meter-assets-06nov.md, sección Sesión 1.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS1nPiezas.MeterTodo
    /// </summary>
    public static class ImportarS1nPiezas
    {
        public const string Carpeta = "Assets/Arte/Vehiculos/Piezas/";

        /// <summary>Un zip de Tripo: dónde está y cómo se llama su modelo en Assets.</summary>
        public readonly struct Zip
        {
            public readonly string Origen;
            public readonly string Carpeta;
            public readonly string Nombre;

            public Zip(string origen, string carpeta, string nombre)
            {
                Origen = origen;
                Carpeta = carpeta;
                Nombre = nombre;
            }

            public string Fbx => ImportarS1nPiezas.Carpeta + Carpeta + "/" + Nombre + ".fbx";
        }

        public static readonly Zip[] Zips =
        {
            new Zip("modelos3d/piezas/V01CARROZA.zip", "V01a", "V01a_Carroceria"),
            new Zip("modelos3d/piezas/V01BCARROZA.zip", "V01b", "V01b_Carroceria"),
            new Zip("modelos3d/piezas/V05CARROZA.zip", "V05", "V05_Carroceria"),
            new Zip("modelos3d/piezas/RU01COCHE.zip", "RU01", "RU01"),
        };

        /// <summary>Puertas de Blender: archivo de origen y nombre en Assets (carro, lado).</summary>
        public static readonly (string origen, string carpeta, string nombre)[] Puertas =
        {
            ("blender/puertas/V01A_Puerta_Izquierda.fbx", "V01a", "V01a_Puerta_Izq"),
            ("blender/puertas/V01A_Puerta_Derecha.fbx", "V01a", "V01a_Puerta_Der"),
            ("blender/puertas/V01B_Puerta_Izquierda.fbx", "V01b", "V01b_Puerta_Izq"),
            ("blender/puertas/V01B_Puerta_Derecha.fbx", "V01b", "V01b_Puerta_Der"),
            ("blender/puertas/V05_Puerta_Izquierda.fbx", "V05", "V05_Puerta_Izq"),
            ("blender/puertas/V05_Puerta_Derecha.fbx", "V05", "V05_Puerta_Der"),
        };

        public static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/S1n/Meter piezas de carros (T-742)")]
        public static void MeterTodo()
        {
            Informe.Clear();
            foreach (Zip z in Zips)
            {
                MeterZip(z);
            }
            foreach (var p in Puertas)
            {
                MeterPuerta(p.origen, Carpeta + p.carpeta + "/" + p.nombre + ".fbx");
            }
            AssetDatabase.Refresh();
            foreach (var izq in Puertas.Where(x => x.nombre.EndsWith("_Izq")))
            {
                ExtraerTexturasDePuerta(Carpeta + izq.carpeta + "/" + izq.nombre + ".fbx", Carpeta + izq.carpeta + "/" + izq.carpeta + "_Puerta");
            }
            AssetDatabase.Refresh();
            AjustarNormales();
            AjustarMallas();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/s1n-piezas.txt", Informe.ToString());
            Debug.Log("[Arte] ImportarS1nPiezas:\n" + Informe);
        }

        private static void MeterZip(Zip z)
        {
            string origen = ImportarS1n.Buscar(z.Origen);
            if (origen == null)
            {
                Informe.AppendLine("falta " + z.Origen + " (se mete cuando llegue)");
                return;
            }
            using (ZipArchive zip = ZipFile.OpenRead(origen))
            {
                ZipArchiveEntry fbx = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase));
                if (fbx == null)
                {
                    Informe.AppendLine("ERROR: " + z.Origen + " no trae .fbx");
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(z.Fbx));
                Extraer(fbx, z.Fbx);
                string baseNombre = Path.GetDirectoryName(z.Fbx).Replace("\\", "/") + "/" + z.Nombre;
                ZipArchiveEntry color = zip.Entries.FirstOrDefault(e => e.Name.ToLowerInvariant().Contains("basecolor"));
                ZipArchiveEntry normal = zip.Entries.FirstOrDefault(e => e.Name.ToLowerInvariant().Contains("normal"));
                if (color != null)
                {
                    Extraer(color, baseNombre + "_basecolor.jpg");
                }
                if (normal != null)
                {
                    Extraer(normal, baseNombre + "_normal.jpg");
                }
            }
        }

        private static void MeterPuerta(string relativo, string destino)
        {
            string origen = ImportarS1n.Buscar(relativo);
            if (origen == null)
            {
                Informe.AppendLine("falta " + relativo);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            if (File.Exists(destino) && new FileInfo(destino).Length == new FileInfo(origen).Length)
            {
                Informe.AppendLine("igual: " + destino);
                return;
            }
            File.Copy(origen, destino, true);
            Informe.AppendLine("copiado: " + origen + " -> " + destino);
        }

        private static void Extraer(ZipArchiveEntry entrada, string destino)
        {
            if (File.Exists(destino) && new FileInfo(destino).Length == entrada.Length)
            {
                Informe.AppendLine("igual: " + destino);
                return;
            }
            entrada.ExtractToFile(destino, true);
            Informe.AppendLine("copiado: " + entrada.FullName + " -> " + destino);
        }

        /// <summary>
        /// Las puertas de Blender traen su propia textura incrustada en el .fbx (son modelos de Tripo aparte de la carrocería). Se sacan una vez a
        /// &lt;prefijo&gt;_basecolor.jpg y &lt;prefijo&gt;_normal.jpg, junto al .fbx (la de la izquierda sirve para las dos puertas).
        /// </summary>
        private static void ExtraerTexturasDePuerta(string rutaFbx, string prefijo)
        {
            if (!File.Exists(rutaFbx) || (File.Exists(prefijo + "_basecolor.jpg") && File.Exists(prefijo + "_normal.jpg")))
            {
                return;
            }
            string carpetaTemporal = Path.GetDirectoryName(rutaFbx).Replace("\\", "/") + "/_texturas_temporales";
            AssetDatabase.CreateFolder(Path.GetDirectoryName(rutaFbx).Replace("\\", "/"), "_texturas_temporales");
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(rutaFbx);
            bool extraidas = importador.ExtractTextures(carpetaTemporal);
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { carpetaTemporal }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();
                string destino = nombre.Contains("basecolor") ? prefijo + "_basecolor.jpg" : nombre.Contains("normal") ? prefijo + "_normal.jpg" : null;
                if (destino != null)
                {
                    File.Copy(ruta, destino, true);
                    Informe.AppendLine("textura de puerta: " + ruta + " -> " + destino);
                }
            }
            AssetDatabase.DeleteAsset(carpetaTemporal);
            Informe.AppendLine("ExtractTextures " + rutaFbx + ": " + extraidas);
        }

        // T-511: los modelos de más de 15 mil caras llevan Mesh LOD.
        private static void AjustarMallas()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Carpeta.TrimEnd('/') }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                int triangulos = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Mesh>().Sum(m => m.triangles.Length / 3);
                ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(ruta);
                if (importador != null && triangulos > 15000 && !importador.generateMeshLods)
                {
                    importador.generateMeshLods = true;
                    importador.SaveAndReimport();
                    Informe.AppendLine("Mesh LOD: " + ruta + " (" + triangulos + " caras)");
                }
            }
        }

        private static void AjustarNormales()
        {
            foreach (string guid in AssetDatabase.FindAssets("_normal t:Texture2D", new[] { Carpeta.TrimEnd('/') }))
            {
                TextureImporter t = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if (t != null && t.textureType != TextureImporterType.NormalMap)
                {
                    t.textureType = TextureImporterType.NormalMap;
                    t.SaveAndReimport();
                }
            }
        }
    }
}
