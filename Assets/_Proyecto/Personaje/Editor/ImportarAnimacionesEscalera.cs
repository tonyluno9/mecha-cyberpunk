using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1204: importa las animaciones de escalera y las asigna a ConfigAnimacionEscalera. Acepta dos fuentes:
    /// - Las de Blender hechas sobre PJ01 (imageassets/blender/pj01_clips/PJ01_Escalera_Entrar, _Subir, _Bajar, _SalirArriba y _SalirAbajo): se copian a
    ///   PJ01/Clips y se importan igual que los demás clips de PJ01 (Humanoid con el avatar de PJ01, escala 1.8367; ImportadorClipsPJ01). Tienen prioridad.
    /// - Las de Mixamo (imageassets/animaciones/Escalera_Subir.fbx y Escalera_Bajar.fbx), solo para subir y bajar.
    /// Si no hay ninguna, solo deja la config lista sin clips.
    /// </summary>
    public static class ImportarAnimacionesEscalera
    {
        public const string FuenteSubir = "imageassets/animaciones/Escalera_Subir.fbx";
        public const string FuenteBajar = "imageassets/animaciones/Escalera_Bajar.fbx";
        public const string CarpetaFuentePJ01 = "imageassets/blender/pj01_clips/";
        public const string CarpetaDestino = "Assets/Arte/Personajes/Animaciones/Escalera/";
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionEscalera.asset";

        /// <summary>Clip de PJ01 por cada campo de la config.</summary>
        public static readonly (string campo, string archivo)[] ClipsPJ01 =
        {
            ("entrar", "PJ01_Escalera_Entrar"),
            ("subir", "PJ01_Escalera_Subir"),
            ("bajar", "PJ01_Escalera_Bajar"),
            ("salirArriba", "PJ01_Escalera_SalirArriba"),
            ("salirAbajo", "PJ01_Escalera_SalirAbajo"),
        };

        private static string FuentePJ01(string archivo) => CarpetaFuentePJ01 + archivo + ".fbx";

        private static string DestinoPJ01(string archivo) => ImportadorClipsPJ01.Carpeta + archivo + ".fbx";

        [MenuItem("Mecha Cyberpunk/T-1204/Importar animaciones de escalera")]
        public static void Importar()
        {
            ConfigAnimacionEscalera config = AsegurarConfig();
            SerializedObject so = new SerializedObject(config);
            int importados = 0;
            foreach ((string campo, string archivo) in ClipsPJ01)
            {
                if (File.Exists(FuentePJ01(archivo)))
                {
                    so.FindProperty(campo).objectReferenceValue = ImportarPJ01(archivo);
                    importados++;
                }
            }
            if (!File.Exists(FuentePJ01("PJ01_Escalera_Subir")) && File.Exists(FuenteSubir))
            {
                so.FindProperty("subir").objectReferenceValue = ImportarMixamo(FuenteSubir);
                importados++;
            }
            if (!File.Exists(FuentePJ01("PJ01_Escalera_Bajar")) && File.Exists(FuenteBajar))
            {
                so.FindProperty("bajar").objectReferenceValue = ImportarMixamo(FuenteBajar);
                importados++;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            if (importados == 0)
            {
                Debug.Log("[Personaje] T-1204: no hay animaciones de escalera (ni PJ01_Escalera_* en " + CarpetaFuentePJ01 + " ni Escalera_Subir/Bajar en imageassets/animaciones); la escalera sigue sin animación");
                return;
            }
            Debug.Log("[Personaje] T-1204: animaciones de escalera importadas: entrar " + (config.Entrar != null) + ", subir " + (config.Subir != null) + ", bajar "
                + (config.Bajar != null) + ", salir arriba " + (config.SalirArriba != null) + ", salir abajo " + (config.SalirAbajo != null));
        }

        internal static bool HayFuentes()
        {
            return File.Exists(FuenteSubir) || File.Exists(FuenteBajar) || ClipsPJ01.Any(c => File.Exists(FuentePJ01(c.archivo)));
        }

        internal static bool FaltaClipConFuente()
        {
            ConfigAnimacionEscalera config = AssetDatabase.LoadAssetAtPath<ConfigAnimacionEscalera>(RutaConfig);
            if (config == null)
            {
                return HayFuentes();
            }
            SerializedObject so = new SerializedObject(config);
            foreach ((string campo, string archivo) in ClipsPJ01)
            {
                if (File.Exists(FuentePJ01(archivo)) && so.FindProperty(campo).objectReferenceValue == null)
                {
                    return true;
                }
            }
            return (File.Exists(FuenteSubir) && config.Subir == null) || (File.Exists(FuenteBajar) && config.Bajar == null);
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

        private static AnimationClip PrimerClip(string ruta)
        {
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        // Igual que "Herramientas/T-749 Reimportar clips de PJ01", pero solo para este archivo.
        private static AnimationClip ImportarPJ01(string archivo)
        {
            string destino = DestinoPJ01(archivo);
            File.Copy(FuentePJ01(archivo), destino, true);
            AssetDatabase.ImportAsset(destino, ImportAssetOptions.ForceUpdate);
            ModelImporter importador = AssetImporter.GetAtPath(destino) as ModelImporter;
            if (importador == null)
            {
                Debug.LogWarning("[Personaje] T-1204: no se pudo abrir el importador de " + destino);
                return null;
            }
            ImportadorClipsPJ01.ConfigurarModelo(importador);
            importador.SaveAndReimport();
            ImportadorClipsPJ01.CopiarDescripcion(importador);
            ImportadorClipsPJ01.ConfigurarClip(importador, destino);
            importador.SaveAndReimport();
            return PrimerClip(destino);
        }

        private static AnimationClip ImportarMixamo(string fuente)
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
            return PrimerClip(destino);
        }
    }

    /// <summary>T-1204: al abrir Unity, si hay fbx de escalera sin clip asignado, los importa solos. Sin archivos no hace nada.</summary>
    [InitializeOnLoad]
    internal static class ImportarAnimacionesEscaleraAuto
    {
        static ImportarAnimacionesEscaleraAuto()
        {
            if (Application.isBatchMode || !ImportarAnimacionesEscalera.HayFuentes())
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
