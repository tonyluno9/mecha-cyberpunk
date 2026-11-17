using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1204: importa Escalera_Subir.fbx y Escalera_Bajar.fbx (de imageassets/animaciones) como clips Humanoid y los asigna a
    /// ConfigAnimacionEscalera. Si los archivos no existen, solo deja la config lista sin clips.
    /// </summary>
    public static class ImportarAnimacionesEscalera
    {
        public const string FuenteSubir = "imageassets/animaciones/Escalera_Subir.fbx";
        public const string FuenteBajar = "imageassets/animaciones/Escalera_Bajar.fbx";
        public const string CarpetaDestino = "Assets/Arte/Personajes/Animaciones/Escalera/";
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionEscalera.asset";

        [MenuItem("Mecha Cyberpunk/T-1204/Importar animaciones de escalera")]
        public static void Importar()
        {
            ConfigAnimacionEscalera config = AsegurarConfig();
            bool haySubir = File.Exists(FuenteSubir);
            bool hayBajar = File.Exists(FuenteBajar);
            if (!haySubir && !hayBajar)
            {
                Debug.Log("[Personaje] T-1204: faltan Escalera_Subir.fbx y Escalera_Bajar.fbx en imageassets/animaciones; la escalera sigue sin animación");
                AssetDatabase.SaveAssets();
                return;
            }
            SerializedObject so = new SerializedObject(config);
            if (haySubir)
            {
                so.FindProperty("subir").objectReferenceValue = ImportarUno(FuenteSubir);
            }
            if (hayBajar)
            {
                so.FindProperty("bajar").objectReferenceValue = ImportarUno(FuenteBajar);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1204: animaciones de escalera importadas (subir " + (config.Subir != null) + ", bajar " + (config.Bajar != null) + ")");
        }

        internal static bool FaltaClipConFuente()
        {
            ConfigAnimacionEscalera config = AssetDatabase.LoadAssetAtPath<ConfigAnimacionEscalera>(RutaConfig);
            bool subirPendiente = File.Exists(FuenteSubir) && (config == null || config.Subir == null);
            bool bajarPendiente = File.Exists(FuenteBajar) && (config == null || config.Bajar == null);
            return subirPendiente || bajarPendiente;
        }

        private static ConfigAnimacionEscalera AsegurarConfig()
        {
            ConfigAnimacionEscalera config = AssetDatabase.LoadAssetAtPath<ConfigAnimacionEscalera>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigAnimacionEscalera>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            return config;
        }

        private static AnimationClip ImportarUno(string fuente)
        {
            Directory.CreateDirectory(CarpetaDestino);
            string destino = CarpetaDestino + Path.GetFileName(fuente);
            File.Copy(fuente, destino, true);
            AssetDatabase.ImportAsset(destino, ImportAssetOptions.ForceUpdate);
            ModelImporter importador = AssetImporter.GetAtPath(destino) as ModelImporter;
            if (importador == null)
            {
                Debug.LogWarning("[Personaje] T-1204: no se pudo abrir el importador de " + destino);
                return null;
            }
            importador.animationType = ModelImporterAnimationType.Human;
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            ModelImporterClipAnimation[] clips = importador.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importador.defaultClipAnimations;
            }
            foreach (ModelImporterClipAnimation c in clips)
            {
                c.loopTime = true;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
            }
            importador.clipAnimations = clips;
            importador.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(destino).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }
    }

    /// <summary>T-1204: al abrir Unity, si hay fbx de escalera sin clip asignado, los importa solos. Sin archivos no hace nada.</summary>
    [InitializeOnLoad]
    internal static class ImportarAnimacionesEscaleraAuto
    {
        static ImportarAnimacionesEscaleraAuto()
        {
            if (!File.Exists(ImportarAnimacionesEscalera.FuenteSubir) && !File.Exists(ImportarAnimacionesEscalera.FuenteBajar))
            {
                return;
            }
            EditorApplication.delayCall += () =>
            {
                if (ImportarAnimacionesEscalera.FaltaClipConFuente())
                {
                    ImportarAnimacionesEscalera.Importar();
                }
            };
        }
    }
}
