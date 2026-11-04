using System.Text.RegularExpressions;
using UnityEditor;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Importa los FBX de Mixamo de Assets/Arte/Personajes/Animaciones/ como Humanoid, sin materiales,
    /// con el clip llamado como el archivo, bucle según el nombre y en el sitio. T-561: sin "bake into pose" en XZ ni rotación Y: los clips de Mixamo
    /// traen el avance en la raíz, y con bake se veía al personaje deslizarse 1.35 m por ciclo; sin bake la raíz se ignora (Apply Root Motion apagado) y queda en el sitio.
    /// Todos quedan In Place, también caídas y muertes (decisión: el controlador mueve al personaje).
    /// </summary>
    public class ImportadorMixamo : AssetPostprocessor
    {
        const string Carpeta = "Assets/Arte/Personajes/Animaciones/";

        // Con bucle: idle, caminar/correr (cualquier variante), apuntar, guardia, agachado, aire y caída.
        static readonly Regex NombreConBucle = new Regex(
            @"^(Idle|Caminar\w*|Correr|Apuntar\w*|Guardia|AgachadoCaminar|SaltoAire|Caida)$");

        public static bool TieneBucle(string nombreArchivo)
        {
            int i = nombreArchivo.IndexOf('_');
            string raiz = i >= 0 ? nombreArchivo.Substring(i + 1) : nombreArchivo;
            return NombreConBucle.IsMatch(raiz);
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Carpeta) || !assetPath.EndsWith(".fbx")) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;

            AjustarClips(importer, assetPath);
        }

        // En la primera importación los clips por defecto aún no existen: se completan en el segundo paso.
        static void OnPostprocessAllAssets(string[] importados, string[] borrados, string[] movidos, string[] movidosDesde)
        {
            foreach (var ruta in importados)
            {
                if (!ruta.StartsWith(Carpeta) || !ruta.EndsWith(".fbx")) continue;
                var importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer != null && importer.clipAnimations.Length == 0 && AjustarClips(importer, ruta))
                    importer.SaveAndReimport();
            }
        }

        static bool AjustarClips(ModelImporter importer, string ruta)
        {
            var clips = importer.defaultClipAnimations;
            if (clips.Length == 0) return false;
            string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
            var clip = clips[0];
            clip.name = nombre;
            clip.loopTime = TieneBucle(nombre);
            clip.lockRootPositionXZ = false;
            clip.lockRootRotation = false;
            // La altura sí va en la pose: sin eso las muertes y los levantarse no bajan el cuerpo al piso.
            clip.lockRootHeightY = true;
            importer.clipAnimations = new[] { clip };
            return true;
        }

        [MenuItem("Herramientas/Reimportar animaciones Mixamo")]
        public static void ReimportarTodo()
        {
            AssetDatabase.ImportAsset(Carpeta.TrimEnd('/'),
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Carpeta.TrimEnd('/') }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer != null && AjustarClips(importer, ruta)) importer.SaveAndReimport();
            }
        }
    }
}
