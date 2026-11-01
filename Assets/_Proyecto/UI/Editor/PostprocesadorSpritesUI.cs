using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-521: todo PNG de Assets/Arte/UI/Sprites se importa como Sprite (sin mipmaps, bilineal), y marco_panel y los botones llevan los
    /// bordes del 9-slice (izquierda, abajo, derecha, arriba, en px del PNG).
    /// </summary>
    public class PostprocesadorSpritesUI : AssetPostprocessor
    {
        public const string Carpeta = "Assets/Arte/UI/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Carpeta, System.StringComparison.Ordinal))
            {
                return;
            }
            TextureImporter importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.filterMode = FilterMode.Bilinear;
            importador.maxTextureSize = 2048;
            importador.spriteBorder = BordeDe(System.IO.Path.GetFileNameWithoutExtension(assetPath));
        }

        /// <summary>Borde 9-slice (izquierda, abajo, derecha, arriba) de la pieza; cero si no es de 9-slice.</summary>
        public static Vector4 BordeDe(string nombre)
        {
            if (nombre == "marco_panel")
            {
                return new Vector4(110f, 80f, 110f, 80f);
            }
            if (nombre.StartsWith("boton_", System.StringComparison.Ordinal))
            {
                return new Vector4(60f, 50f, 60f, 50f);
            }
            return Vector4.zero;
        }
    }
}
