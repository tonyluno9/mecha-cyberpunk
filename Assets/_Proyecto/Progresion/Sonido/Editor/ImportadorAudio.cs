using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido.Editor
{
    /// <summary>
    /// Importa los WAV de Assets/Arte/Audio/&lt;SEC&gt;/ (T-470): efectos y voces en mono, 3D, Vorbis calidad 70; AMB en streaming
    /// (stereo, 2D); UI en mono 2D. Los provisionales (Provisional/) no se tocan.
    /// Para reimportar todo: Mecha Cyberpunk/Sonido/Reimportar audio.
    /// </summary>
    public class ImportadorAudio : AssetPostprocessor
    {
        public const string Carpeta = "Assets/Arte/Audio/";
        public const float Calidad = 0.7f;

        /// <summary>Sección (AMB, ARM...) de la ruta del WAV final, o null si no es un final.</summary>
        public static string SeccionDeRuta(string ruta)
        {
            if (!ruta.StartsWith(Carpeta) || !ruta.EndsWith(".wav")) return null;
            string[] partes = ruta.Substring(Carpeta.Length).Split('/');
            return partes.Length == 2 ? partes[0] : null;
        }

        private void OnPreprocessAudio()
        {
            string seccion = SeccionDeRuta(assetPath);
            if (seccion == null) return;
            Aplicar((AudioImporter)assetImporter, seccion);
        }

        public static void Aplicar(AudioImporter importador, string seccion)
        {
            bool ambiente = seccion == "AMB";
            AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
            ajustes.loadType = ambiente ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = Calidad;
            ajustes.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importador.defaultSampleSettings = ajustes;
            importador.forceToMono = !ambiente;
            importador.loadInBackground = ambiente;
        }

        [MenuItem("Mecha Cyberpunk/Sonido/Reimportar audio")]
        public static void ReimportarTodo()
        {
            AssetDatabase.ImportAsset("Assets/Arte/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }
    }
}
