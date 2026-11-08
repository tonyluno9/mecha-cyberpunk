using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

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
        // Clips nuevos con otro nombre (NPC, gestos, vehículos): con bucle si el archivo termina en "_Bucle" (docs/animaciones-faltan.md).
        const string SufijoBucle = "_Bucle";
        static readonly Regex NombreConBucle = new Regex(
            @"^(Idle|Caminar\w*|Correr|Apuntar\w*|Guardia|Agachado(?:Idle|Caminar|Atras|Lateral_\w+)|SaltoAire|Caida)$");

        public static bool TieneBucle(string nombreArchivo)
        {
            int i = nombreArchivo.IndexOf('_');
            string raiz = i >= 0 ? nombreArchivo.Substring(i + 1) : nombreArchivo;
            return NombreConBucle.IsMatch(raiz) || nombreArchivo.EndsWith(SufijoBucle);
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

        /// <summary>
        /// T-731: un FBX reemplazado conserva el esqueleto guardado en su .meta; si el esqueleto cambió el avatar falla ("Transform 'Hips' not found").
        /// Se apaga y se vuelve a crear el avatar desde el modelo para que lea el esqueleto nuevo. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportadorMixamo.RehacerAvatares
        /// </summary>
        public static void RehacerAvatares()
        {
            string[] rutas =
            {
                Carpeta + "Pistola/Pistola_Disparo.fbx", Carpeta + "Pistola/Pistola_Correr.fbx",
                Carpeta + "Pistola/Pistola_ApuntarCaminar_Derecha.fbx", Carpeta + "Pistola/Pistola_ApuntarCaminar_Izquierda.fbx", Carpeta + "Locomocion/Loco_Caminar.fbx",
            };
            foreach (var ruta in rutas)
            {
                var importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer == null) continue;
                importer.humanDescription = new UnityEngine.HumanDescription();
                importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// T-733: Loco_AgachadoIdle y Loco_AgachadoCaminar de Tony salen de Blender con otra escala en el esqueleto: su propio avatar da humanScale 24 y el
        /// cuerpo sale 30 cm arriba (los pies en el aire). Toman la descripción humanoide (esqueleto y mapeo) de Loco_Pararse, que viene del mismo
        /// esqueleto bien escalado, y siguen creando su avatar desde su propio modelo. Se guarda en sus .meta; se puede repetir.
        /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportadorMixamo.UsarAvatarDeLocoPararse
        /// </summary>
        public static void UsarAvatarDeLocoPararse()
        {
            var origen = (ModelImporter)AssetImporter.GetAtPath(Carpeta + "Locomocion/Loco_Pararse.fbx");
            HumanDescription descripcion = origen.humanDescription;
            foreach (string nombre in new[] { "Loco_AgachadoIdle", "Loco_AgachadoCaminar" })
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(Carpeta + "Locomocion/" + nombre + ".fbx");
                importer.humanDescription = descripcion;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }
    }
}
