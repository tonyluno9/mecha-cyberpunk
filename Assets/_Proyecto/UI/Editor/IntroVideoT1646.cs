using System.IO;
using System.Linq;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Intro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-1646: deja lista la intro del juego. <see cref="Asignar"/> busca el video del logo (Assets/Arte/Video, raíz del proyecto,
    /// imageassets/logo; nombre con "logo" y .mp4/.mov/.webm), lo mueve a Assets/Arte/Video/ si está fuera, lo importa y lo asigna a
    /// ConfigIntro. <see cref="Aplicar"/> (idempotente) crea ConfigIntro, agrega IntroInicio al MenuInicio de Inicio.unity y llama a Asignar.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.IntroVideoT1646.Aplicar
    /// </summary>
    public static class IntroVideoT1646
    {
        public const string CarpetaVideo = "Assets/Arte/Video";
        public const string RutaVideo = CarpetaVideo + "/videologo.mp4";
        public const string CarpetaConfig = "Assets/_Proyecto/UI/Intro/Datos";
        public const string RutaConfig = CarpetaConfig + "/ConfigIntro.asset";
        public const string RutaInforme = "Logs/t1646.txt";
        private const string Marca = "logo";
        private static readonly string[] Extensiones = { ".mp4", ".mov", ".webm" };

        [MenuItem("Mecha Cyberpunk/Inicio/Intro: aplicar (T-1646)")]
        public static void Aplicar()
        {
            ConfigIntro config = AsegurarConfig();
            AgregarASceneInicio(config);
            Asignar();
        }

        [MenuItem("Mecha Cyberpunk/Inicio/Intro: asignar video (T-1646)")]
        public static void Asignar()
        {
            string informe = "";
            string origen = BuscarVideo();
            if (origen == null)
            {
                informe = "T-1646: no se encontró el video del logo (nombre con \"" + Marca + "\" y .mp4/.mov/.webm en " + CarpetaVideo
                    + ", la raíz del proyecto o imageassets/logo). ConfigIntro queda sin video: la intro se salta.";
                Informar(informe);
                return;
            }

            string ruta = TraerAAssets(origen);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            ConfigurarImportador(ruta);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);

            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(ruta);
            if (clip == null)
            {
                Informar("T-1646: " + ruta + " no se importó como VideoClip.");
                return;
            }
            ConfigIntro config = AsegurarConfig();
            config.AsignarVideo(clip);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Informar("T-1646: video asignado: " + ruta + " (" + clip.width + "x" + clip.height + ", " + clip.length.ToString("0.0") + " s).");
        }

        // ------------------------------------------------------------------ búsqueda y movimiento

        private static string BuscarVideo()
        {
            string raizProyecto = Path.GetDirectoryName(Application.dataPath);
            string[] carpetas =
            {
                Path.Combine(raizProyecto, CarpetaVideo),
                raizProyecto,
                Path.Combine(raizProyecto, "imageassets", "logo"),
                Path.Combine(raizProyecto, "Assets", "_Proyecto", "UI", "Intro", "Video"),
            };
            foreach (string carpeta in carpetas)
            {
                if (!Directory.Exists(carpeta))
                {
                    continue;
                }
                string hallado = Directory.GetFiles(carpeta, "*", SearchOption.TopDirectoryOnly)
                    .Where(f => Extensiones.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Where(f => Path.GetFileName(f).ToLowerInvariant().Contains(Marca))
                    .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (hallado != null)
                {
                    return hallado;
                }
            }
            return null;
        }

        // Si el video está fuera de Assets/Arte/Video lo MUEVE ahí (sin dejar copia) y devuelve su ruta de asset.
        private static string TraerAAssets(string origen)
        {
            string raizProyecto = Path.GetDirectoryName(Application.dataPath);
            string destinoCarpeta = Path.Combine(raizProyecto, CarpetaVideo);
            string nombre = Path.GetFileName(origen);
            string rutaAsset = CarpetaVideo + "/" + nombre;
            if (Path.GetFullPath(Path.GetDirectoryName(origen)) == Path.GetFullPath(destinoCarpeta))
            {
                return rutaAsset;
            }
            Directory.CreateDirectory(destinoCarpeta);
            string destino = Path.Combine(destinoCarpeta, nombre);
            if (File.Exists(destino))
            {
                File.Delete(destino);
            }
            string metaOrigen = origen + ".meta";
            File.Move(origen, destino);
            if (File.Exists(metaOrigen))
            {
                File.Delete(metaOrigen);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return rutaAsset;
        }

        private static void ConfigurarImportador(string ruta)
        {
            VideoClipImporter importador = AssetImporter.GetAtPath(ruta) as VideoClipImporter;
            if (importador == null)
            {
                return;
            }
            importador.importAudio = true;
            importador.keepAlpha = false;
            // Transcodifica (H264) para que se reproduzca igual en cualquier Windows, sin depender de los códecs del sistema.
            VideoImporterTargetSettings ajustes = importador.defaultTargetSettings;
            ajustes.enableTranscoding = true;
            ajustes.codec = VideoCodec.H264;
            ajustes.resizeMode = VideoResizeMode.OriginalSize;
            ajustes.aspectRatio = VideoEncodeAspectRatio.NoScaling;
            ajustes.bitrateMode = VideoBitrateMode.High;
            ajustes.spatialQuality = VideoSpatialQuality.HighSpatialQuality;
            importador.defaultTargetSettings = ajustes;
            importador.SaveAndReimport();
        }

        // ------------------------------------------------------------------ config y escena

        private static ConfigIntro AsegurarConfig()
        {
            ConfigIntro config = AssetDatabase.LoadAssetAtPath<ConfigIntro>(RutaConfig);
            if (config != null)
            {
                return config;
            }
            Directory.CreateDirectory(CarpetaConfig);
            config = ScriptableObject.CreateInstance<ConfigIntro>();
            AssetDatabase.CreateAsset(config, RutaConfig);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void AgregarASceneInicio(ConfigIntro config)
        {
            EditorSceneManager.OpenScene(EscenaInicio.Ruta, OpenSceneMode.Single);
            // Abrir la escena descarga los assets sin referencias: el ConfigIntro recién creado se vuelve a cargar del disco.
            config = AssetDatabase.LoadAssetAtPath<ConfigIntro>(RutaConfig);
            MenuInicio menu = Object.FindFirstObjectByType<MenuInicio>();
            if (menu == null)
            {
                Informar("T-1646: Inicio.unity no tiene MenuInicio; no se agregó la intro.");
                return;
            }
            IntroInicio intro = menu.GetComponent<IntroInicio>();
            if (intro == null)
            {
                intro = menu.gameObject.AddComponent<IntroInicio>();
            }
            SerializedObject so = new SerializedObject(intro);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            EditorSceneManager.SaveScene(menu.gameObject.scene);
        }

        private static void Informar(string texto)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, texto + "\n");
            Debug.Log("[UI] " + texto);
        }
    }
}
