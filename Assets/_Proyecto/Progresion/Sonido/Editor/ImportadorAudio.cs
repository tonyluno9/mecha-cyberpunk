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
            // Las carpetas numeradas (01_UI .. 18_Radio_Musica) son de ImportarSonidos19nov/20nov, con sus reglas por duración,
            // mono/estéreo y precarga. Antes esta regla general corría al final y las dejaba todas en CompressedInMemory y mono.
            if (partes.Length == 2 && partes[0].Length > 2 && char.IsDigit(partes[0][0]) && char.IsDigit(partes[0][1]) && partes[0][2] == '_') return null;
            // La música (Assets/Arte/Audio/Musica: MUS_MENU_PRINCIPAL) va estéreo y en Streaming: la configura ImportarMusicaMenu.
            if (partes.Length == 2 && partes[0] == "Musica") return null;
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
