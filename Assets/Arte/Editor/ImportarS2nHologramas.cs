using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-814: las imágenes holográficas de las armas (imageassets/ui/hologramas/H01-H04, fondo negro) a Assets/Arte/UI/Hologramas/ para la lista y
    /// el holograma del banco de Tino. Copia solo si el archivo existe y cambió; idempotente. Las deja como textura normal, sin mipmaps, a 1024 px.
    /// Si falta alguna, el menú usa el modelo 3D o un holograma vacío (no se rompe).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2nHologramas.Importar
    /// </summary>
    public static class ImportarS2nHologramas
    {
        public const string CarpetaOrigen = "imageassets/ui/hologramas/";
        public const string CarpetaDestino = "Assets/Arte/UI/Hologramas/";
        private const int TamanoMaximo = 1024;

        /// <summary>Los archivos que se buscan (sin carpeta), en el orden de las armas: pistola, escopeta, fusil, francotirador.</summary>
        public static readonly string[] Archivos = { "H01_Pistola.png", "H02_Escopeta.png", "H03_Fusil.png", "H04_Francotirador.png" };

        [MenuItem("Mecha Cyberpunk/S2n/Hologramas de armas (H01-H04)")]
        public static void Importar()
        {
            Directory.CreateDirectory(CarpetaDestino);
            int copiados = 0;
            foreach (string archivo in Archivos)
            {
                string origen = CarpetaOrigen + archivo;
                string destino = CarpetaDestino + archivo;
                if (!File.Exists(origen))
                {
                    continue;
                }
                if (!File.Exists(destino) || new FileInfo(origen).Length != new FileInfo(destino).Length)
                {
                    File.Copy(origen, destino, true);
                    copiados++;
                }
            }
            AssetDatabase.Refresh();
            foreach (string archivo in Archivos)
            {
                Configurar(CarpetaDestino + archivo);
            }
            Debug.Log("[Arte] Hologramas de armas: " + copiados + " copiados a " + CarpetaDestino);
        }

        /// <summary>La textura del holograma de esa posición (0 = pistola), o null si no se ha metido.</summary>
        public static Texture2D Cargar(int indice)
        {
            return indice >= 0 && indice < Archivos.Length ? AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaDestino + Archivos[indice]) : null;
        }

        private static void Configurar(string ruta)
        {
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                return;
            }
            bool cambio = importador.textureType != TextureImporterType.Default || importador.mipmapEnabled
                || importador.maxTextureSize != TamanoMaximo || importador.wrapMode != TextureWrapMode.Clamp || importador.alphaIsTransparency;
            if (!cambio)
            {
                return;
            }
            importador.textureType = TextureImporterType.Default;
            importador.mipmapEnabled = false;
            importador.maxTextureSize = TamanoMaximo;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.alphaIsTransparency = false;
            importador.SaveAndReimport();
        }
    }
}
