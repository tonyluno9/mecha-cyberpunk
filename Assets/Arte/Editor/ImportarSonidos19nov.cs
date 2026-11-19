using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1470: ajustes de importación de los sonidos nuevos del 19-nov (Assets/Arte/Audio/01_UI .. 10_Policia).
    /// Un AssetPostprocessor (OnPreprocessAudio) que solo actúa en esas diez carpetas, y <see cref="Aplicar"/> que las reimporta.
    /// Reglas: cortos -> DecompressOnLoad (ADPCM) con precarga; medianos -> CompressedInMemory (Vorbis 0.7);
    /// largos (más de 8 s) -> Streaming con carga en segundo plano. Mono forzado para los sonidos 3D del mundo
    /// (armas, impactos, explosiones, pasos, vehículos, policía); estéreo para UI, HUD y tienda.
    /// El bucle de un AudioClip lo pone el AudioSource: <see cref="EsLoop"/> solo dice qué nombres son bucles.
    /// </summary>
    public class ImportarSonidos19nov : AssetPostprocessor
    {
        public const string RaizAudio = "Assets/Arte/Audio/";

        public static readonly string[] Carpetas =
        {
            "01_UI", "02_Armas", "03_Impactos", "04_Explosiones", "05_Pasos", "06_Jugador",
            "07_Interaccion", "08_Consumibles", "09_Vehiculos", "10_Policia"
        };

        // Carpetas con sonidos 3D del mundo: se fuerzan a mono.
        static readonly string[] CarpetasMono = { "02_Armas", "03_Impactos", "04_Explosiones", "05_Pasos", "09_Vehiculos", "10_Policia" };

        static readonly string[] PrefijosCortos = { "UI_", "HUD_", "TIE_", "IMP_", "MEL_", "PAS_" };

        static readonly string[] NombresLoop =
        {
            "FUE_LOOP_CHICO", "POL_SIRENA_LOOP", "VEH_ALARMA", "VEH_LLANTAS_GRAVA", "VEH_ELECTRICO_MOVER"
        };

        const float LimiteCorto = 2f;
        const float LimiteLargo = 8f;

        /// <summary>True si el nombre (con o sin ruta, con o sin .wav) es un sonido que se reproduce en bucle.</summary>
        public static bool EsLoop(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return false;
            string n = Path.GetFileNameWithoutExtension(nombre);
            if (n.IndexOf("_RALENTI", StringComparison.Ordinal) >= 0) return true;
            return Array.IndexOf(NombresLoop, n) >= 0;
        }

        /// <summary>Carpeta de las diez nuevas (por ejemplo "09_Vehiculos") a la que pertenece la ruta, o null.</summary>
        public static string CarpetaNueva(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(RaizAudio, StringComparison.Ordinal)) return null;
            string resto = assetPath.Substring(RaizAudio.Length);
            int i = resto.IndexOf('/');
            if (i <= 0) return null;
            string carpeta = resto.Substring(0, i);
            return Array.IndexOf(Carpetas, carpeta) >= 0 ? carpeta : null;
        }

        void OnPreprocessAudio()
        {
            string carpeta = CarpetaNueva(assetPath);
            if (carpeta == null) return;
            var importer = assetImporter as AudioImporter;
            if (importer == null) return;

            string nombre = Path.GetFileNameWithoutExtension(assetPath);
            float duracion = DuracionWav(assetPath);
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
                // Medianos y bucles cortos (ralentí de 5 s): en memoria para que el bucle no tenga hueco.
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
            }
            importer.defaultSampleSettings = s;
            importer.forceToMono = Array.IndexOf(CarpetasMono, carpeta) >= 0;
            importer.loadInBackground = largo;
            importer.preloadAudioData = !largo && (corto || loop);
            importer.ambisonic = false;
        }

        static bool EsCorto(string nombre, float duracion)
        {
            if (duracion > 0f && duracion < LimiteCorto) return true;
            foreach (string p in PrefijosCortos)
                if (nombre.StartsWith(p, StringComparison.Ordinal)) return true;
            return nombre.StartsWith("ARM_", StringComparison.Ordinal) && nombre.Contains("_DISPARO");
        }

        /// <summary>Duración en segundos leyendo la cabecera del WAV (0 si no se puede leer).</summary>
        public static float DuracionWav(string assetPath)
        {
            try
            {
                string full = Path.GetFullPath(assetPath);
                using (var fs = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    if (fs.Length < 44 || new string(br.ReadChars(4)) != "RIFF") return 0f;
                    br.ReadInt32();
                    if (new string(br.ReadChars(4)) != "WAVE") return 0f;
                    int byteRate = 0;
                    while (fs.Position + 8 <= fs.Length)
                    {
                        string id = new string(br.ReadChars(4));
                        int n = br.ReadInt32();
                        if (id == "fmt ")
                        {
                            long ini = fs.Position;
                            br.ReadInt16();
                            br.ReadInt16();
                            br.ReadInt32();
                            byteRate = br.ReadInt32();
                            fs.Position = ini + n + (n & 1);
                        }
                        else if (id == "data")
                        {
                            long bytes = Math.Min((long)(uint)n, fs.Length - fs.Position);
                            return byteRate > 0 ? bytes / (float)byteRate : 0f;
                        }
                        else fs.Position += n + (n & 1);
                    }
                }
            }
            catch (Exception)
            {
                // sin cabecera legible: se tratará como mediano
            }
            return 0f;
        }

        /// <summary>Reimporta las diez carpetas para que se apliquen los ajustes. Correr una vez con los WAV ya en el proyecto.</summary>
        public static void Aplicar()
        {
            foreach (string c in Carpetas)
            {
                string ruta = RaizAudio + c;
                if (AssetDatabase.IsValidFolder(ruta))
                    AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1470] Sonidos del 19-nov reimportados con las reglas de ImportarSonidos19nov.");
        }
    }
}
