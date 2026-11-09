using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-749: los clips de Blender ya aplicados al esqueleto de PJ01 (Assets/Arte/Personajes/PJ01/Clips/PJ01_*.fbx, 78 huesos, sin malla, 30 fps).
    /// Se importan Humanoid con la descripción de PJ01 (el mismo mapeo de huesos y los mismos twist: ver <see cref="CopiarDescripcion"/>), con el mismo Scale Factor que PJ01 y los
    /// implantes, el clip llamado como el archivo, bucle según el nombre y los eventos de la pistola (T-734). "Copy From Other Avatar" no sirve: Unity llama al nodo raíz del clip
    /// como el archivo (PJ01_Pistola_Apuntar) y el avatar de PJ01 espera "PJ01" ("Parent for 'BoneRoot' differs ..."). Corre después de PostprocesadorAssetsT250 (que trata la carpeta de PJ01
    /// como un personaje de Tripo y la deja con su propio avatar).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportadorClipsPJ01.Aplicar
    /// </summary>
    public class ImportadorClipsPJ01 : AssetPostprocessor
    {
        public const string Carpeta = "Assets/Arte/Personajes/PJ01/Clips/";
        public const string RutaModeloOrigen = "Assets/Arte/Personajes/PJ01/PJ01.fbx";
        public const string Prefijo = "PJ01_";
        /// <summary>El Scale Factor de PJ01.fbx y PJ01_Implantes.fbx (userData "escala=1.88").</summary>
        public const float EscalaClips = 1.88f;
        /// <summary>Giro (grados en Y) que pasa el cuerpo de los FBX (-Z) al frente de los clips Humanoid del juego (+Z).</summary>
        public const float GiroCuerpoClips = 180f;

        public override int GetPostprocessOrder() => 200;

        /// <summary>
        /// Copias de unos clips de Clips/ importadas Generic (sin retargeting): son "lo que se ve en Blender", los huesos tal cual vienen en el FBX. Las pruebas comparan contra ellas
        /// el clip Humanoid que se reproduce en el juego (giro y cabeceo de la pistola y manos juntas). Mismas escala y nombres de clip.
        /// </summary>
        public const string CarpetaReferencia = "Assets/Arte/Personajes/PJ01/ClipsReferencia/";
        /// <summary>Los clips de Clips/ que se copian a la carpeta de referencia.</summary>
        public static readonly string[] ClipsDeReferencia =
        {
            "PJ01_Pistola_Apuntar", "PJ01_Pistola_Disparo", "PJ01_Pistola_ApuntarLateral_Derecha", "PJ01_Pistola_ApuntarLateral_Izquierda", "PJ01_Pistola_ApuntarAtras",
            "PJ01_Pistola_Idle", "PJ01_Pistola_Caminar", "PJ01_Pistola_ApuntarCaminar_Derecha", "PJ01_Pistola_ApuntarCaminar_Izquierda", "PJ01_Pistola_Correr",
        };

        public static bool EsClipPJ01(string ruta) => ruta.StartsWith(Carpeta) && ruta.EndsWith(".fbx");
        public static bool EsReferencia(string ruta) => ruta.StartsWith(CarpetaReferencia) && ruta.EndsWith(".fbx");

        /// <summary>El nombre que usa el controlador base (sin el prefijo del modelo): PJ01_Pistola_Apuntar -> Pistola_Apuntar; PJ01_Caminar_SinImplante -> Loco_Caminar_SinImplante.</summary>
        public static string NombreBase(string nombreClip)
        {
            string n = nombreClip.StartsWith(Prefijo) ? nombreClip.Substring(Prefijo.Length) : nombreClip;
            return n.StartsWith("Caminar_") || n.StartsWith("Correr_") ? "Loco_" + n : n;
        }

        void OnPreprocessModel()
        {
            if (EsReferencia(assetPath))
            {
                var r = (ModelImporter)assetImporter;
                r.animationType = ModelImporterAnimationType.Generic;
                r.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                r.materialImportMode = ModelImporterMaterialImportMode.None;
                r.importAnimation = true;
                r.useFileScale = true;
                r.globalScale = EscalaClips;
                r.animationCompression = ModelImporterAnimationCompression.Off;
                return;
            }
            if (!EsClipPJ01(assetPath)) { return; }
            ConfigurarModelo((ModelImporter)assetImporter);
        }

        static void OnPostprocessAllAssets(string[] importados, string[] borrados, string[] movidos, string[] movidosDesde)
        {
            foreach (string ruta in importados)
            {
                if (!EsClipPJ01(ruta)) { continue; }
                var importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer != null && importer.clipAnimations.Length == 0 && ConfigurarClip(importer, ruta))
                {
                    importer.SaveAndReimport();
                }
            }
        }

        public static void ConfigurarModelo(ModelImporter importer)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;
            importer.useFileScale = true;
            importer.globalScale = EscalaClips;
            importer.userData = "escala=" + EscalaClips.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            ConfigurarClip(importer, importer.assetPath);
        }

        /// <summary>
        /// Los FBX de clips guardan en sus nodos la pose de un cuadro de la animación, no la pose T de PJ01: si Unity armara el avatar con ellos, el retargeting saldría torcido.
        /// El avatar de cada clip toma de PJ01.fbx la pose T (posición, giro y escala locales de cada hueso), el mapeo humanoide, los límites y los twist; del clip solo queda el
        /// nombre de la raíz (el del archivo; su giro de 90 grados ya trae el del nodo Armature de PJ01, por eso BoneRoot queda igual).
        /// Necesita que el clip ya se haya importado una vez (para tener su esqueleto).
        /// </summary>
        public static void CopiarDescripcion(ModelImporter clip)
        {
            var origen = AssetImporter.GetAtPath(RutaModeloOrigen) as ModelImporter;
            if (origen == null) { return; }
            HumanDescription pj = origen.humanDescription;
            HumanDescription d = clip.humanDescription;
            var porNombre = new System.Collections.Generic.Dictionary<string, SkeletonBone>();
            foreach (SkeletonBone b in pj.skeleton) { porNombre[b.name] = b; }
            SkeletonBone[] esqueleto = d.skeleton;
            for (int i = 0; i < esqueleto.Length; i++)
            {
                if (!porNombre.TryGetValue(esqueleto[i].name, out SkeletonBone bone)) { continue; }
                esqueleto[i].position = bone.position;
                esqueleto[i].rotation = bone.rotation;
                esqueleto[i].scale = bone.scale;
            }
            d.skeleton = esqueleto;
            d.human = pj.human;
            d.upperArmTwist = pj.upperArmTwist;
            d.lowerArmTwist = pj.lowerArmTwist;
            d.upperLegTwist = pj.upperLegTwist;
            d.lowerLegTwist = pj.lowerLegTwist;
            d.armStretch = pj.armStretch;
            d.legStretch = pj.legStretch;
            d.feetSpacing = pj.feetSpacing;
            d.hasTranslationDoF = pj.hasTranslationDoF;
            clip.humanDescription = d;
        }

        /// <summary>Un clip por archivo, con el nombre del archivo, el bucle por nombre, la raíz como los de Mixamo, los eventos de la pistola y el aditivo de Dano.</summary>
        public static bool ConfigurarClip(ModelImporter importer, string ruta)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0) { return false; }
            string nombre = Path.GetFileNameWithoutExtension(ruta);
            string baseNombre = NombreBase(nombre);
            ModelImporterClipAnimation clip = clips[0];
            clip.name = nombre;
            clip.loopTime = ImportadorMixamo.TieneBucle(baseNombre);
            // Humanoid normaliza la raíz a la orientación del cuerpo y le quita el giro que el clip trae respecto al objeto de Blender (hasta 25 grados en apuntar). Para que la
            // pistola quede donde está en Blender, el giro y la altura se hornean en la pose y se toman del original. El avance XZ no se hornea: los clips quedan en el sitio, igual
            // que todos los demás (el cuerpo lo mueve el controlador).
            clip.lockRootPositionXZ = false;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            // Los FBX llevan el cuerpo mirando a -Z (el nodo raíz que trae Blender); los demás clips, a +Z. Sin este giro el cuerpo quedaba de espaldas al frente.
            clip.rotationOffset = GiroCuerpoClips;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.events = ImportadorMixamo.EventosDe(baseNombre, clip.firstFrame, clip.lastFrame);
            if (baseNombre.StartsWith("Pistola_Dano_"))
            {
                clip.hasAdditiveReferencePose = true;
                clip.additiveReferencePoseFrame = clip.firstFrame;
            }
            importer.clipAnimations = new[] { clip };
            return true;
        }

        [MenuItem("Herramientas/T-749 Reimportar clips de PJ01")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Carpeta.TrimEnd('/') }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer == null) { continue; }
                ConfigurarModelo(importer);
                importer.SaveAndReimport();
                CopiarDescripcion(importer);
                ConfigurarClip(importer, ruta);
                importer.SaveAndReimport();
            }
            CopiarReferencias();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Copia a la carpeta de referencia los clips de <see cref="ClipsDeReferencia"/> (si faltan) y los importa Generic con clip del mismo nombre, sin compresión.</summary>
        public static void CopiarReferencias()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaReferencia.TrimEnd('/'))) { AssetDatabase.CreateFolder("Assets/Arte/Personajes/PJ01", "ClipsReferencia"); }
            foreach (string nombre in ClipsDeReferencia)
            {
                string destino = CarpetaReferencia + nombre + ".fbx";
                if (AssetDatabase.LoadAssetAtPath<Object>(destino) == null) { AssetDatabase.CopyAsset(Carpeta + nombre + ".fbx", destino); }
                var importer = AssetImporter.GetAtPath(destino) as ModelImporter;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(destino) as ModelImporter;
                ModelImporterClipAnimation clip = importer.defaultClipAnimations[0];
                clip.name = nombre;
                clip.loopTime = false;
                importer.clipAnimations = new[] { clip };
                importer.SaveAndReimport();
            }
        }
    }
}
