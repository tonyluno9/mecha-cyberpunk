using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Pantalla de carga minimalista (pedido de Tony "carga"): importa las ilustraciones L01-L06 de Resources/Carga sin escalar a
    /// potencia de 2 (con NPOT "ToNearest" Unity las reescalaba y su ancho/alto ya no daba la proporción real: la foto se deformaba),
    /// sin mipmaps y sin compresión, con filtro bilineal y borde fijo. Repetible.
    /// </summary>
    public static class ImportarFotosCarga
    {
        public const string Carpeta = "Assets/_Proyecto/UI/Resources/Carga";
        public const int TamanoMaximo = 2048;

        [MenuItem("Mecha Cyberpunk/UI/Carga: importar fotos sin deformar")]
        public static void Aplicar()
        {
            StringBuilder sb = new StringBuilder("[UI] Fotos de carga:\n");
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (imp == null)
                {
                    continue;
                }
                imp.textureType = TextureImporterType.Default;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = TamanoMaximo;
                imp.filterMode = FilterMode.Bilinear;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.sRGBTexture = true;
                imp.alphaSource = TextureImporterAlphaSource.None;
                TextureImporterPlatformSettings pc = imp.GetPlatformTextureSettings("Standalone");
                pc.overridden = true;
                pc.maxTextureSize = TamanoMaximo;
                pc.textureCompression = TextureImporterCompression.Uncompressed;
                pc.format = TextureImporterFormat.RGB24;
                imp.SetPlatformTextureSettings(pc);
                imp.SaveAndReimport();
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
                sb.AppendLine("  " + ruta + ": " + (t != null ? t.width + "x" + t.height + " " + t.format : "sin textura"));
            }
            Debug.Log(sb.ToString());
        }
    }
}
