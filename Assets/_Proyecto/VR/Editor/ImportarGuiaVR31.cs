using MechaCyberpunk.VR.Guia;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.VR.Editor
{
    /// <summary>
    /// VR31/VR31b: importa las 16 imágenes de la guía de controles VR (Assets/Arte/UI/ControlesVR/Resources/GuiaVR, una por página de
    /// <see cref="TextosGuiaVR.Paginas"/>; también las usan las tarjetas del tutorial VR62) como Sprite (2D and UI), con compresión ligera
    /// (alta calidad: BC7 en PC, ASTC en Quest; 16 sin comprimir pesarían ~65 MB en Resources) y
    /// sin mip maps, para que se lean bien en el visor. La guía (<see cref="GuiaControlesVR"/>) las carga de Resources/GuiaVR.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ImportarGuiaVR31.Aplicar
    /// </summary>
    public static class ImportarGuiaVR31
    {
        public const string Carpeta = "Assets/Arte/UI/ControlesVR/Resources/" + TextosGuiaVR.CarpetaImagenes;
        private const int TamanoMaximo = 2048;

        [MenuItem("Mecha Cyberpunk/VR/Importar guía de controles (VR31b, 16 páginas)")]
        public static void Aplicar()
        {
            int listas = 0;
            int faltan = 0;
            foreach (var pagina in TextosGuiaVR.Paginas)
            {
                string ruta = Carpeta + "/" + pagina.imagen + ".png";
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
                var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (importador == null)
                {
                    Debug.LogError("[VR] VR31: no está la imagen " + ruta + ".");
                    faltan++;
                    continue;
                }
                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.mipmapEnabled = false;
                importador.alphaIsTransparency = true;
                importador.textureCompression = TextureImporterCompression.CompressedHQ;
                importador.maxTextureSize = TamanoMaximo;
                importador.filterMode = FilterMode.Bilinear;
                importador.wrapMode = TextureWrapMode.Clamp;
                importador.sRGBTexture = true;
                importador.SaveAndReimport();
                if (AssetDatabase.LoadAssetAtPath<Sprite>(ruta) != null)
                {
                    listas++;
                }
                else
                {
                    Debug.LogError("[VR] VR31: " + ruta + " no quedó como Sprite.");
                    faltan++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[VR] VR31: " + listas + " imágenes de la guía importadas como Sprite (compresión alta calidad, sin mip maps)" + (faltan > 0 ? "; " + faltan + " con error." : "."));
        }
    }
}
