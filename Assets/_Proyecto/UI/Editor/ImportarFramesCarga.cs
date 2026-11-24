using System.Text;
using MechaCyberpunk.UI.Inicio;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T100: importa los frames de las cinemáticas de la pantalla de carga (<see cref="FramesCarga.CarpetaAssets"/>) como Sprite,
    /// sin mipmaps, tamaño máximo 2048 (entran enteros a 1920×1080), filtro bilineal, borde fijo y compresión de alta calidad
    /// (BC7 en PC: sin bandas visibles y 4 veces más ligera que sin comprimir; con 22 frames sin comprimir serían ~137 MB).
    /// Repetible: solo reimporta los que cambian.
    ///   -executeMethod MechaCyberpunk.UI.Editor.ImportarFramesCarga.Aplicar
    /// </summary>
    public static class ImportarFramesCarga
    {
        public const int TamanoMaximo = 2048;

        [MenuItem("Mecha Cyberpunk/UI/Carga: importar frames de cinemáticas (T100)")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            StringBuilder sb = new StringBuilder("[UI] Frames de carga (T100):\n");
            int cuantos = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FramesCarga.CarpetaAssets }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (imp == null)
                {
                    continue;
                }
                if (Configurar(imp))
                {
                    imp.SaveAndReimport();
                }
                cuantos++;
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
                sb.AppendLine("  " + ruta + ": " + (t != null ? t.width + "x" + t.height + " " + t.format : "sin textura"));
            }
            sb.AppendLine("  total: " + cuantos);
            Debug.Log(sb.ToString());
            if (cuantos != FramesCarga.Cantidad)
            {
                Debug.LogError("[UI] Frames de carga: hay " + cuantos + " PNG en " + FramesCarga.CarpetaAssets + " y FramesCarga.Nombres lista " + FramesCarga.Cantidad + ".");
            }
        }

        /// <summary>Deja el importador como debe estar; true si cambió algo.</summary>
        private static bool Configurar(TextureImporter imp)
        {
            bool cambio = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; cambio = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; cambio = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; cambio = true; }
            if (imp.maxTextureSize != TamanoMaximo) { imp.maxTextureSize = TamanoMaximo; cambio = true; }
            if (imp.textureCompression != TextureImporterCompression.CompressedHQ) { imp.textureCompression = TextureImporterCompression.CompressedHQ; cambio = true; }
            if (imp.crunchedCompression) { imp.crunchedCompression = false; cambio = true; }
            if (imp.filterMode != FilterMode.Bilinear) { imp.filterMode = FilterMode.Bilinear; cambio = true; }
            if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; cambio = true; }
            if (!imp.sRGBTexture) { imp.sRGBTexture = true; cambio = true; }
            if (imp.alphaSource != TextureImporterAlphaSource.None) { imp.alphaSource = TextureImporterAlphaSource.None; cambio = true; }
            if (imp.isReadable) { imp.isReadable = false; cambio = true; }

            TextureImporterSettings ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            if (ajustes.spriteMeshType != SpriteMeshType.FullRect)
            {
                ajustes.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(ajustes);
                cambio = true;
            }

            TextureImporterPlatformSettings pc = imp.GetPlatformTextureSettings("Standalone");
            if (!pc.overridden || pc.maxTextureSize != TamanoMaximo || pc.format != TextureImporterFormat.BC7
                || pc.textureCompression != TextureImporterCompression.CompressedHQ || pc.crunchedCompression)
            {
                pc.overridden = true;
                pc.maxTextureSize = TamanoMaximo;
                pc.format = TextureImporterFormat.BC7;
                pc.textureCompression = TextureImporterCompression.CompressedHQ;
                pc.crunchedCompression = false;
                imp.SetPlatformTextureSettings(pc);
                cambio = true;
            }
            return cambio;
        }
    }
}
