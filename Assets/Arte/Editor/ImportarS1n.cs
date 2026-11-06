using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-678: importador de la sesión 1 (noche 6-nov). Mete cada archivo solo cuando aparece y es idempotente (si el archivo es igual, no hace nada):
    /// - Armas W02 escopeta, W03 fusil, W04 francotirador (.zip de Tripo) a Assets/Arte/Armas/Wxx_Nombre/ con la convención de W01_Pistola
    ///   (modelo Wxx_Nombre.fbx y texturas en Texturas/). Después arma sus prefabs (T-679, ColocarS1nArmas).
    /// - Policía C01 (.zip) a Assets/Arte/Personajes/Policia/ (Policia.fbx y Policia.fbm/). Después arma el policía a pie (T-680, ColocarS1nPolicia).
    /// - F01_Destello (imageassets/vfx) a Assets/Arte/VFX/ y asignada a DatosSensacion.texturaDestello.
    /// - Hologramas H01-H04: los mete <see cref="ImportarS2nHologramas"/> (T-816); aquí solo se comprueba y, si faltan, se llama.
    /// Los archivos se buscan en imageassets/ del proyecto y, si no, en imageassets/ de la carpeta principal del repo (hermana mecha-cyberpunk).
    /// Ver docs/meter-assets-06nov.md, sección Sesión 1.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS1n.MeterTodo
    /// </summary>
    public static class ImportarS1n
    {
        public const string CarpetaImagenes = "imageassets/";
        /// <summary>imageassets/ de la carpeta principal (mecha-s1 no tiene la suya: no está en git).</summary>
        public const string CarpetaImagenesPrincipal = "../mecha-cyberpunk/imageassets/";
        public const string CarpetaArmas = "Assets/Arte/Armas/";
        public const string RutaDestello = "Assets/Arte/VFX/F01_Destello.png";
        public const string RutaDatosSensacion = "Assets/_Proyecto/Personaje/Datos/Resources/DatosSensacion.asset";
        public const string RutaPolicia = "Assets/Arte/Personajes/Policia/Policia.fbx";

        public readonly struct Arma
        {
            public readonly string Codigo;
            public readonly string Nombre;

            public Arma(string codigo, string nombre)
            {
                Codigo = codigo;
                Nombre = nombre;
            }

            public string Zip => "modelos3d/" + Codigo + ".zip";
            public string Carpeta => CarpetaArmas + Codigo + "_" + Nombre + "/";
            public string Fbx => Carpeta + Codigo + "_" + Nombre + ".fbx";
            public string Texturas => Carpeta + "Texturas/";
        }

        public static readonly Arma[] Armas =
        {
            new Arma("W02", "Escopeta"),
            new Arma("W03", "Fusil"),
            new Arma("W04", "Francotirador"),
        };

        public static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/S1n/Meter assets nuevos")]
        public static void MeterTodo()
        {
            Informe.Clear();
            foreach (Arma arma in Armas)
            {
                MeterZip(arma.Zip, arma.Fbx, arma.Texturas);
            }
            MeterZip("modelos3d/C01.zip", RutaPolicia, "Assets/Arte/Personajes/Policia/Policia.fbm/");
            MeterDestello();
            AssetDatabase.Refresh();
            AjustarDestello();
            ComprobarHologramas();
            ArmarDespues();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/s1n-importar.txt", Informe.ToString());
            Debug.Log("[Arte] ImportarS1n:\n" + Informe);
        }

        /// <summary>La ruta del archivo en imageassets/ (del proyecto o de la carpeta principal), o null si no ha llegado.</summary>
        public static string Buscar(string relativo)
        {
            foreach (string carpeta in new[] { CarpetaImagenes, CarpetaImagenesPrincipal })
            {
                if (File.Exists(carpeta + relativo))
                {
                    return carpeta + relativo;
                }
            }
            return null;
        }

        /// <summary>Desempaca un .zip de Tripo: el .fbx va a <paramref name="destinoFbx"/> y las imágenes a <paramref name="carpetaImagenes"/>. Solo escribe lo que cambió.</summary>
        private static void MeterZip(string relativo, string destinoFbx, string carpetaImagenes)
        {
            string origen = Buscar(relativo);
            if (origen == null)
            {
                Informe.AppendLine("falta " + relativo + " (se mete cuando llegue)");
                return;
            }
            using (ZipArchive zip = ZipFile.OpenRead(origen))
            {
                ZipArchiveEntry fbx = zip.Entries.FirstOrDefault(z => z.FullName.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase));
                if (fbx == null)
                {
                    Informe.AppendLine("ERROR: " + relativo + " no trae .fbx");
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destinoFbx));
                Extraer(fbx, destinoFbx);
                Directory.CreateDirectory(carpetaImagenes);
                foreach (ZipArchiveEntry imagen in zip.Entries.Where(EsImagen))
                {
                    Extraer(imagen, carpetaImagenes + imagen.Name);
                }
            }
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

        private static bool EsImagen(ZipArchiveEntry z)
        {
            string n = z.Name.ToLowerInvariant();
            return n.EndsWith(".jpg") || n.EndsWith(".jpeg") || n.EndsWith(".png");
        }

        private static void MeterDestello()
        {
            string origen = Buscar("vfx/F01_Destello.png");
            if (origen == null)
            {
                Informe.AppendLine("falta vfx/F01_Destello.png (el destello usa la chispa procedural)");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaDestello));
            if (File.Exists(RutaDestello) && new FileInfo(RutaDestello).Length == new FileInfo(origen).Length)
            {
                Informe.AppendLine("igual: " + RutaDestello);
                return;
            }
            File.Copy(origen, RutaDestello, true);
            Informe.AppendLine("copiado: " + origen + " -> " + RutaDestello);
        }

        /// <summary>F01 como textura de destello: sin mipmaps (se dibuja con aditivo, el fondo negro no suma) y asignada a DatosSensacion.</summary>
        private static void AjustarDestello()
        {
            if (!File.Exists(RutaDestello))
            {
                return;
            }
            TextureImporter importador = AssetImporter.GetAtPath(RutaDestello) as TextureImporter;
            if (importador != null && (importador.mipmapEnabled || importador.alphaIsTransparency || importador.wrapMode != TextureWrapMode.Clamp))
            {
                importador.textureType = TextureImporterType.Default;
                importador.mipmapEnabled = false;
                importador.alphaIsTransparency = false;
                importador.wrapMode = TextureWrapMode.Clamp;
                importador.SaveAndReimport();
            }
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(RutaDatosSensacion);
            if (datos == null)
            {
                Informe.AppendLine("ERROR: falta " + RutaDatosSensacion);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            SerializedProperty propiedad = so.FindProperty("texturaDestello");
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaDestello);
            if (propiedad.objectReferenceValue != textura)
            {
                propiedad.objectReferenceValue = textura;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
                Informe.AppendLine("DatosSensacion.texturaDestello = F01_Destello");
            }
            else
            {
                Informe.AppendLine("DatosSensacion.texturaDestello ya es F01_Destello");
            }
        }

        /// <summary>H01-H04 los mete la sesión 2 (T-816). Si alguno falta en Assets, se llama a su importador.</summary>
        private static void ComprobarHologramas()
        {
            bool faltan = ImportarS2nHologramas.Archivos.Any(a => !File.Exists(ImportarS2nHologramas.CarpetaDestino + a));
            if (faltan)
            {
                ImportarS2nHologramas.Importar();
                Informe.AppendLine("hologramas H01-H04: faltaban, se llamó a ImportarS2nHologramas");
                return;
            }
            Informe.AppendLine("hologramas H01-H04: ya están en " + ImportarS2nHologramas.CarpetaDestino + " (los metió la sesión 2, T-816)");
        }

        // Lo que se arma con lo importado (T-679 armas, T-680 policía a pie); cada uno se salta solo si no llegó su archivo.
        private static void ArmarDespues()
        {
            ColocarS1nArmas.Colocar();
            Informe.AppendLine(File.ReadAllText("Logs/s1n-armas.txt"));
        }
    }
}
