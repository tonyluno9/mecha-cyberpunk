using UnityEditor;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Importa los modelos de T-250 (Assets/Arte/&lt;Zona&gt;/): 1 unidad = 1 m, ejes horneados, sin cámaras, luces ni animaciones,
    /// y sin materiales del archivo (el prefab les pone su URP Lit). Las texturas quedan a 2048 como máximo.
    /// Corre después del PostprocesadorArte de Alan, que también mira Assets/Arte/.
    /// </summary>
    public class PostprocesadorAssetsT250 : AssetPostprocessor
    {
        private const int OrdenDespuesDeAlan = 100;
        private const int TamanoMaximoTextura = 2048;

        public override int GetPostprocessOrder()
        {
            return OrdenDespuesDeAlan;
        }

        private void OnPreprocessModel()
        {
            if (!ArteT250.EsRutaDeZona(assetPath) || !assetPath.EndsWith(".fbx"))
            {
                return;
            }

            ModelImporter modelo = (ModelImporter)assetImporter;
            modelo.useFileScale = true;
            modelo.globalScale = 1f;
            modelo.bakeAxisConversion = true;
            modelo.importAnimation = false;
            modelo.animationType = ModelImporterAnimationType.None;
            modelo.importCameras = false;
            modelo.importLights = false;
            modelo.importBlendShapes = false;
            modelo.materialImportMode = ModelImporterMaterialImportMode.None;
            modelo.isReadable = false;
        }

        private void OnPreprocessTexture()
        {
            if (!ArteT250.EsRutaDeZona(assetPath))
            {
                return;
            }

            TextureImporter textura = (TextureImporter)assetImporter;
            textura.maxTextureSize = TamanoMaximoTextura;
            textura.mipmapEnabled = true;
            if (assetPath.Contains("_normal."))
            {
                textura.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                textura.textureType = TextureImporterType.Default;
                textura.sRGBTexture = true;
            }
        }
    }
}
