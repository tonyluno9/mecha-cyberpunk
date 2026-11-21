using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1553: dónde viven y cómo se importan los clips de los robots de Nova que pasaron de PJ01/Clips a la carpeta Clips de su personaje (E16_CaminarEscudo y los E18_*). Los cuatro
    /// que Blender exportó con el rig del propio personaje (E16_CaminarEscudo, E18_Caminar, E18_Correr, E18_Golpe_Der) se importan Humanoid con la escala del personaje y el
    /// Avatar de su modelo (Copy From Other Avatar); los demás siguen igual que en PJ01 (ImportadorClipsPJ01). Lo usan ClipsNpcNovaT1506, ConstructorControladorBase y el método de editor RobotsNovaT1553.
    /// </summary>
    public static class ClipsRobotsNovaT1553
    {
        public const string CarpetaE16 = "Assets/Arte/Personajes/E16_RobotPesadoNova/Clips/";
        public const string CarpetaE18 = "Assets/Arte/Personajes/E18_EliteNova/Clips/";
        public const string RutaModeloE15 = "Assets/Arte/Personajes/E15_CentinelaNova/E15_CentinelaNova_a.fbx";
        public const string RutaModeloE16 = "Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_a.fbx";
        public const string RutaModeloE18 = "Assets/Arte/Personajes/E18_EliteNova/E18_EliteNova_a.fbx";

        /// <summary>Clips que pasaron de PJ01/Clips a la carpeta de su personaje (git mv con su .meta: el GUID se conserva).</summary>
        public static readonly string[] ClipsDeRobot =
        {
            "E16_CaminarEscudo",
            "E18_Caida_Aire", "E18_Caida_Aterrizaje", "E18_Caida_Levantarse", "E18_Caminar", "E18_Correr", "E18_Disparar", "E18_Golpe_Der", "E18_Jetpack_Volar", "E18_Muerte",
        };

        /// <summary>
        /// Los clips que Blender exportó con el rig del propio personaje (huesos mixamorig:*, 2.5 MB con la malla) y no con la armadura de PJ01 (BoneRoot, 0.7 MB).
        /// Se importan con la escala del personaje y el Avatar de su modelo (<see cref="AvatarDelPersonaje"/>).
        /// </summary>
        public static readonly string[] ClipsConRigPropio = { "E16_CaminarEscudo", "E18_Caminar", "E18_Correr", "E18_Golpe_Der" };

        /// <summary>De los clips con rig propio, los que se repiten (caminar y correr; el golpe va una sola vez).</summary>
        public static readonly string[] ClipsEnBucle = { "E16_CaminarEscudo", "E18_Caminar", "E18_Correr" };

        /// <summary>Velocidades de los clips del E18 (m/s, de Blender): caminar y correr. Las usa el prefab visual para que los pies no patinen.</summary>
        public const float VelocidadCaminarE18 = 1.62f;
        public const float VelocidadCorrerE18 = 4.53f;

        public static bool EsClipDeRobot(string ruta) =>
            ruta != null && ruta.EndsWith(".fbx") && (ruta.StartsWith(CarpetaE16) || ruta.StartsWith(CarpetaE18));

        public static bool TieneRigPropio(string nombre) => System.Array.IndexOf(ClipsConRigPropio, nombre) >= 0;

        /// <summary>Carpeta (con "/" al final) del clip: la de su personaje si es de <see cref="ClipsDeRobot"/>, si no la de PJ01.</summary>
        public static string CarpetaDe(string nombre)
        {
            if (System.Array.IndexOf(ClipsDeRobot, nombre) < 0)
            {
                return ImportadorClipsPJ01.Carpeta;
            }
            return nombre.StartsWith("E16_") ? CarpetaE16 : CarpetaE18;
        }

        public static string RutaClip(string nombre) => CarpetaDe(nombre) + nombre + ".fbx";

        /// <summary>Todas las carpetas donde pueden estar los clips Blender de Nova (para buscarlos por nombre).</summary>
        public static string[] CarpetasDeClips() => new[] { ImportadorClipsPJ01.Carpeta.TrimEnd('/'), CarpetaE16.TrimEnd('/'), CarpetaE18.TrimEnd('/') };

        public static string RutaModeloDe(string nombreClip) => nombreClip.StartsWith("E16_") ? RutaModeloE16 : RutaModeloE18;

        // ------------------------------------------------------------------ Importación

        /// <summary>
        /// Ajustes del importador de un clip de <see cref="ClipsConRigPropio"/>: Humanoid (Copy From Other Avatar con el del personaje), escala y userData del personaje, sin materiales
        /// ni malla útil, un clip con el nombre del archivo y bucle según <see cref="ClipsEnBucle"/>. Lo llama el postprocesador en cada importación y <see cref="ImportarRigPropio"/>.
        /// </summary>
        public static void ConfigurarRigPropio(ModelImporter importador, string nombre)
        {
            var modelo = AssetImporter.GetAtPath(RutaModeloDe(nombre)) as ModelImporter;
            importador.animationType = ModelImporterAnimationType.Human;
            // "Humanoid con el Avatar del personaje" (Tony, 20-nov): Copy From Other Avatar con el del modelo, que ya está en pose T. Si todavía no existe (primera importación),
            // queda con avatar propio y RobotsNovaT1553.Aplicar vuelve a configurarlo cuando el del personaje ya está listo.
            Avatar fuente = AvatarDelPersonaje(nombre);
            if (fuente != null)
            {
                importador.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importador.sourceAvatar = fuente;
            }
            else
            {
                importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            importador.materialImportMode = ModelImporterMaterialImportMode.None;
            importador.importAnimation = true;
            importador.useFileScale = true;
            if (modelo != null)
            {
                importador.globalScale = modelo.globalScale;
                importador.userData = modelo.userData;
            }
            importador.importCameras = false;
            importador.importLights = false;
            importador.importBlendShapes = false;
            ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;
            if (clips.Length > 0)
            {
                ModelImporterClipAnimation clip = clips[0];
                clip.name = nombre;
                clip.loopTime = System.Array.IndexOf(ClipsEnBucle, nombre) >= 0;
                clip.loopPose = clip.loopTime;
                importador.clipAnimations = new[] { clip };
            }
        }

        /// <summary>Importa un clip con rig propio: reimporta, configura (avatar copiado del personaje) y vuelve a reimportar.</summary>
        public static void ImportarRigPropio(string nombre)
        {
            string ruta = RutaClip(nombre);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                Debug.LogError("[Personaje] T-1553: falta " + ruta);
                return;
            }
            ConfigurarRigPropio(importador, nombre);
            importador.SaveAndReimport();
        }

        /// <summary>El Avatar Humanoid del modelo del personaje (el sub-asset del .fbx) o null si todavía no existe o no es válido.</summary>
        public static Avatar AvatarDelPersonaje(string nombreClip)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(RutaModeloDe(nombreClip)).OfType<Avatar>().FirstOrDefault();
            return avatar != null && avatar.isValid && avatar.isHuman ? avatar : null;
        }
    }
}
