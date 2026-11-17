using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1310: integra los clips de Blender de la ronda 2 (imageassets/blender/pj01_clips). Copia los FBX a Assets/Arte/Personajes/PJ01/Clips si hay una versión más nueva
    /// (conserva el .meta de los que ya existían), los importa como los demás clips de PJ01 (Humanoid con la descripción de PJ01.fbx, mismo Scale Factor, bucle por nombre;
    /// ver <see cref="ImportadorClipsPJ01"/>), borra la copia limpia vieja de PJ01_Moto_Bajar, crea Resources/ConfigAnimacionParkour con los clips del parkour y del robo de carro,
    /// y reconstruye el controlador base (cobertura con rifle, caminar, idle y moto nuevos). Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ImportarClipsRonda2T1310.Aplicar
    /// </summary>
    public static class ImportarClipsRonda2T1310
    {
        public const string CarpetaFuente = "imageassets/blender/pj01_clips/";
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionParkour.asset";
        public const string RutaLimpioMotoBajar = "Assets/Arte/Personajes/PJ01/Clips/Limpios/PJ01_Moto_Bajar.anim";

        /// <summary>Clips de la ronda 2 que reemplazan a otros ya integrados (mismo nombre, mismo .meta).</summary>
        public static readonly string[] ClipsReemplazados =
        {
            "PJ01_Caminar_SinImplante", "PJ01_Caminar_ConImplante", "PJ01_Loco_Idle", "PJ01_Loco_Idle_ConImplante",
            "PJ01_Escalera_Entrar", "PJ01_Escalera_Subir", "PJ01_Escalera_Bajar", "PJ01_Escalera_SalirArriba", "PJ01_Escalera_SalirAbajo",
            "PJ01_Moto_Bajar",
        };

        /// <summary>Clips nuevos de la ronda 2 y el campo de ConfigAnimacionParkour al que van (null: los usa el controlador base).</summary>
        public static readonly (string archivo, string campo)[] ClipsNuevos =
        {
            ("PJ01_Parkour_SaltarBarda", "saltarBarda"),
            ("PJ01_Parkour_TreparBorde", "treparBorde"),
            ("PJ01_Parkour_RodarCaida", "rodarCaida"),
            ("PJ01_Carro_SacarConductor", "carroSacarConductor"),
            ("PJ01_Conductor_SerSacado", "conductorSerSacado"),
            ("PJ01_CoberturaRifle_Entrar", null),
            ("PJ01_CoberturaRifle_Idle", null),
            ("PJ01_CoberturaRifle_Disparo", null),
        };

        [MenuItem("Mecha Cyberpunk/T-1310/Importar clips de la ronda 2")]
        public static void Aplicar()
        {
            Debug.Log("[T-1310] Inicio: importando clips de la ronda 2");
            AssetDatabase.Refresh();

            foreach (string nombre in ClipsReemplazados)
            {
                ImportarClip(nombre);
            }
            foreach ((string archivo, string _) in ClipsNuevos)
            {
                ImportarClip(archivo);
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(RutaLimpioMotoBajar) != null)
            {
                AssetDatabase.DeleteAsset(RutaLimpioMotoBajar);
                Debug.Log("[T-1310] Borrada la copia limpia vieja de PJ01_Moto_Bajar (el clip nuevo ya viene sin el cuadro malo)");
            }

            ConectarConfig();
            AssetDatabase.SaveAssets();

            // Cobertura con rifle, caminar, idle y moto: el controlador base y los overrides se rearman con los clips nuevos.
            ConstructorControladorBase.Construir();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[T-1310] Listo: clips importados, config conectada y controlador base reconstruido");
        }

        private static string RutaDestino(string nombre) => ImportadorClipsPJ01.Carpeta + nombre + ".fbx";

        private static void ImportarClip(string nombre)
        {
            string fuente = CarpetaFuente + nombre + ".fbx";
            string destino = RutaDestino(nombre);
            if (File.Exists(fuente))
            {
                if (!File.Exists(destino) || !MismoContenido(fuente, destino))
                {
                    File.Copy(fuente, destino, true);
                    Debug.Log("[T-1310] Copiado " + nombre + ".fbx");
                }
            }
            else if (!File.Exists(destino))
            {
                Debug.LogError("[T-1310] Falta el clip " + nombre + " (ni en " + CarpetaFuente + " ni en Assets)");
                return;
            }
            AssetDatabase.ImportAsset(destino, ImportAssetOptions.ForceUpdate);
            ModelImporter importador = AssetImporter.GetAtPath(destino) as ModelImporter;
            if (importador == null)
            {
                Debug.LogError("[T-1310] No se pudo abrir el importador de " + destino);
                return;
            }
            // Igual que ImportadorClipsPJ01.Aplicar y ImportarAnimacionesEscalera: Humanoid con la descripción de PJ01, escala de PJ01, bucle por nombre.
            ImportadorClipsPJ01.ConfigurarModelo(importador);
            importador.SaveAndReimport();
            ImportadorClipsPJ01.CopiarDescripcion(importador);
            ImportadorClipsPJ01.ConfigurarClip(importador, destino);
            importador.SaveAndReimport();
            Debug.Log("[T-1310] Importado " + nombre + " (bucle " + ImportadorClipsPJ01.TieneBucle(ImportadorClipsPJ01.NombreBase(nombre)) + ")");
        }

        private static bool MismoContenido(string a, string b)
        {
            FileInfo fa = new FileInfo(a);
            FileInfo fb = new FileInfo(b);
            if (fa.Length != fb.Length)
            {
                return false;
            }
            return File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b));
        }

        private static AnimationClip PrimerClip(string ruta)
        {
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        private static void ConectarConfig()
        {
            ConfigAnimacionParkour config = AssetDatabase.LoadAssetAtPath<ConfigAnimacionParkour>(RutaConfig);
            if (config == null)
            {
                string carpeta = Path.GetDirectoryName(RutaConfig).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                    AssetDatabase.Refresh();
                }
                config = ScriptableObject.CreateInstance<ConfigAnimacionParkour>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                Debug.Log("[T-1310] Creada " + RutaConfig);
            }
            SerializedObject so = new SerializedObject(config);
            foreach ((string archivo, string campo) in ClipsNuevos)
            {
                if (campo == null)
                {
                    continue;
                }
                AnimationClip clip = PrimerClip(RutaDestino(archivo));
                SerializedProperty propiedad = so.FindProperty(campo);
                if (propiedad == null || clip == null)
                {
                    Debug.LogError("[T-1310] No se pudo conectar " + archivo + " al campo " + campo);
                    continue;
                }
                propiedad.objectReferenceValue = clip;
                Debug.Log("[T-1310] " + campo + " = " + clip.name + " (" + clip.length.ToString("0.00") + " s)");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
