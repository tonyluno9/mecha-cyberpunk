using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Importa los modelos FBX de Assets/Arte/ siempre igual: 1 unidad = 1 m, ejes ya horneados (raíz sin rotación),
    /// sin animaciones, cámaras ni luces, normales del archivo y materiales remapeados por nombre a los de Assets/Arte/Materiales/.
    /// </summary>
    public class PostprocesadorArte : AssetPostprocessor
    {
        public const string CarpetaArte = "Assets/Arte/";
        public const string CarpetaMateriales = "Assets/Arte/Materiales";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(CarpetaArte) || !assetPath.EndsWith(".fbx"))
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
            modelo.importNormals = ModelImporterNormals.Import;
            modelo.importTangents = ModelImporterTangents.CalculateMikk;
            modelo.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            modelo.materialLocation = ModelImporterMaterialLocation.InPrefab;

            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                return;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { CarpetaMateriales }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (material != null)
                {
                    modelo.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), Path.GetFileNameWithoutExtension(ruta)), material);
                }
            }
        }
    }
}
