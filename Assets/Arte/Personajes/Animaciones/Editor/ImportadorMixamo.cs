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
            clip.events = EventosDe(nombre, clip.firstFrame, clip.lastFrame);
            // T-735: Pistola_Dano_* se usan en una capa aditiva; empiezan en Pistola_Apuntar, que es su pose de referencia (su primer cuadro).
            if (nombre.StartsWith("Pistola_Dano_"))
            {
                clip.hasAdditiveReferencePose = true;
                clip.additiveReferencePoseFrame = clip.firstFrame;
            }
            importer.clipAnimations = new[] { clip };
            return true;
        }

        // T-734: la pistola solo está en la mano mientras se apunta. Pistola_Sacar la activa a los 0.30 s (la mano está en la cadera, fotograma 10) y
        // Pistola_Guardar la desactiva a los 0.53 s (la mano llega a la cadera, fotograma 17), como pide agarre.txt de Tony. Los eventos llaman a
        // ReceptorEventosArma (Personaje.Visual); los nombres de las funciones están en las pruebas de EditMode para que no se separen.
        public const string FuncionSacarPistola = "AlSacarPistola";
        public const string FuncionGuardarPistola = "AlGuardarPistola";
        public const float SegundosEventoSacar = 0.30f;
        public const float SegundosEventoGuardar = 0.53f;
        const float CuadrosPorSegundo = 30f;

        /// <summary>Los eventos de un clip por su nombre (vacío si no lleva). El tiempo del importador es la fracción del clip (0 a 1).</summary>
        public static AnimationEvent[] EventosDe(string nombre, float primerCuadro, float ultimoCuadro)
        {
            float largo = Mathf.Max(0.01f, (ultimoCuadro - primerCuadro) / CuadrosPorSegundo);
            if (nombre == "Pistola_Sacar")
            {
                return new[] { new AnimationEvent { functionName = FuncionSacarPistola, time = SegundosEventoSacar / largo } };
            }
            if (nombre == "Pistola_Guardar")
            {
                return new[] { new AnimationEvent { functionName = FuncionGuardarPistola, time = SegundosEventoGuardar / largo } };
            }
            return new AnimationEvent[0];
        }

        /// <summary>T-734: escribe los eventos de Pistola_Sacar y Pistola_Guardar en sus .meta. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportadorMixamo.AplicarEventosPistola</summary>
        public static void AplicarEventosPistola()
        {
            foreach (string nombre in new[] { "Pistola_Sacar", "Pistola_Guardar" })
            {
                string ruta = Carpeta + "Pistola/" + nombre + ".fbx";
                var importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer != null && AjustarClips(importer, ruta)) importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>T-735: marca Pistola_Dano_Frente, Espalda y Fuerte como aditivos en su .meta. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportadorMixamo.AplicarAditivoDano</summary>
        public static void AplicarAditivoDano()
        {
            foreach (string nombre in new[] { "Pistola_Dano_Frente", "Pistola_Dano_Espalda", "Pistola_Dano_Fuerte" })
            {
                string ruta = Carpeta + "Dano/" + nombre + ".fbx";
                var importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer != null && AjustarClips(importer, ruta)) importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
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
