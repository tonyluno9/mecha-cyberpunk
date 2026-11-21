using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1530: ajustes de importación de los sonidos nuevos del 20-nov
    /// (Assets/Arte/Audio/11_Nova .. 18_Radio_Musica). Igual que <see cref="ImportarSonidos19nov"/> pero para las carpetas nuevas.
    /// Reglas: cortos (menos de 2 s) -> DecompressOnLoad (ADPCM) con precarga; medianos -> CompressedInMemory (Vorbis 0.7);
    /// largos (más de 8 s) -> Streaming con carga en segundo plano. Mono y 3D en los sonidos del mundo; estéreo en
    /// 17_Misiones_HUD, 18_Radio_Musica y la rueda de cámara lenta (EXO_RUEDA_*), que son 2D.
    /// El 3D o 2D real lo decide la regla de mezcla en tiempo de juego, no el importador.
    /// </summary>
    public class ImportarSonidos20nov : AssetPostprocessor
    {
        public const string RaizAudio = "Assets/Arte/Audio/";

        public static readonly string[] Carpetas =
        {
            "11_Nova", "12_NPCs", "13_Robots", "14_Ambiente", "15_Exo_Implantes", "16_Casino",
            "17_Misiones_HUD", "18_Radio_Musica"
        };

        // Carpetas 2D: se dejan en estéreo.
        static readonly string[] CarpetasEstereo = { "17_Misiones_HUD", "18_Radio_Musica" };

        // Nombres 2D dentro de carpetas del mundo.
        static readonly string[] PrefijosEstereo = { "EXO_RUEDA_" };

        // Pisadas y golpes sueltos: siempre cortos aunque midan un poco más de 2 s.
        static readonly string[] PrefijosCortos = { "NOV_PASOS_", "NPC_", "NOV_ESCUDO_", "IMP_SINAPSIS_" };

        // Los que el AudioSource reproduce en bucle (los de 01..10 están también en ImportarSonidos19nov).
        static readonly string[] NombresLoop =
        {
            "NOV_JETPACK_LOOP", "RAD_EST1_CUMBIA_PERCUSION", "OBJ_REFRIGERADOR", "OBJ_TRANSFORMADOR", "OBJ_AIRE_ACONDICIONADO",
            "OBJ_CAMARA_SEGURIDAD", "AMB_AZOTEA_CIUDAD", "AMB_LOMAS", "AMB_NOVA", "AMB_SUBURBIOS_NOCHE",
            "CLI_LLUVIA_DENTRO_CARRO", "CLI_LLUVIA_FUERTE", "CLI_LLUVIA_SOBRE_LAMINA", "CLI_TORMENTA_TRUENOS", "CLI_VIENTO",
            "INT_ARMERIA_VENTILADOR", "INT_BODEGA_GOTERAS", "INT_TALLER_CANEPA", "INT_TIENDA_TINO_RADIO", "INT_TIENDA_TINO_VENTILADOR"
        };

        const float LimiteCorto = 2f;
        const float LimiteLargo = 8f;

        /// <summary>True si el nombre (con o sin ruta, con o sin .wav) es un sonido que se reproduce en bucle.</summary>
        public static bool EsLoop(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return false;
            string n = Path.GetFileNameWithoutExtension(nombre);
            return Array.IndexOf(NombresLoop, n) >= 0;
        }

        /// <summary>Carpeta de las ocho nuevas (por ejemplo "14_Ambiente") a la que pertenece la ruta, o null.</summary>
        public static string CarpetaNueva(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(RaizAudio, StringComparison.Ordinal)) return null;
            string resto = assetPath.Substring(RaizAudio.Length);
            int i = resto.IndexOf('/');
            if (i <= 0) return null;
            string carpeta = resto.Substring(0, i);
            return Array.IndexOf(Carpetas, carpeta) >= 0 ? carpeta : null;
        }

        /// <summary>True si el sonido se deja en estéreo (2D): carpetas de HUD y radio, y la rueda de cámara lenta.</summary>
        public static bool EsEstereo(string carpeta, string nombre)
        {
            if (Array.IndexOf(CarpetasEstereo, carpeta) >= 0) return true;
            foreach (string p in PrefijosEstereo)
                if (nombre.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        void OnPreprocessAudio()
        {
            string carpeta = CarpetaNueva(assetPath);
            if (carpeta == null) return;
            var importer = assetImporter as AudioImporter;
            if (importer == null) return;

            string nombre = Path.GetFileNameWithoutExtension(assetPath);
            float duracion = ImportarSonidos19nov.DuracionWav(assetPath);
            bool loop = EsLoop(nombre);
            bool corto = EsCorto(nombre, duracion);
            bool largo = duracion > LimiteLargo;

            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            if (largo)
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
            }
            else if (corto)
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            else
            {
                // Medianos y bucles cortos: en memoria para que el bucle no tenga hueco.
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
            }
            s.preloadAudioData = !largo && (corto || loop);
            importer.defaultSampleSettings = s;
            importer.forceToMono = !EsEstereo(carpeta, nombre);
            importer.loadInBackground = largo;
            importer.ambisonic = false;
        }

        static bool EsCorto(string nombre, float duracion)
        {
            if (duracion > 0f && duracion < LimiteCorto) return true;
            foreach (string p in PrefijosCortos)
                if (nombre.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Reimporta las ocho carpetas para que se apliquen los ajustes. Correr una vez con los WAV ya en el proyecto.</summary>
        public static void Aplicar()
        {
            foreach (string c in Carpetas)
            {
                string ruta = RaizAudio + c;
                if (AssetDatabase.IsValidFolder(ruta))
                    AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1530] Sonidos del 20-nov reimportados con las reglas de ImportarSonidos20nov.");
        }
    }
}
