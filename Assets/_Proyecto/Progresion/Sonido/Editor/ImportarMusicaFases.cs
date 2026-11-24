using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido.Editor
{
    /// <summary>
    /// T-musica-24nov: copia (si faltan) los temas de "3d sonidos/20_Musica" a Assets/Arte/Audio/20_Musica, los importa como música (estéreo,
    /// Streaming, Vorbis, sin precargar; el loop lo pone la fuente de <see cref="MusicaFases"/>) y los pone en el CatalogoSonidos con su ID
    /// (MUS_PIANO_INTRO, MUS_CAMPO_TIRO, MUS_FASE_SKULLS/POLICIA/NOVA). Solo agrega o reemplaza esas entradas. Repetible.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Sonido.Editor.ImportarMusicaFases.Aplicar
    /// </summary>
    public static class ImportarMusicaFases
    {
        public const string Carpeta = "Assets/Arte/Audio/20_Musica";
        public const string CarpetaOrigen = "3d sonidos/20_Musica";
        private const float CalidadVorbis = 0.7f;

        [MenuItem("Mecha Cyberpunk/Sonido/Importar música de fases")]
        public static void Aplicar()
        {
            CopiarFaltantes();
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(ImportarMusicaMenu.RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[Sonido] No está " + ImportarMusicaMenu.RutaCatalogo);
                return;
            }
            List<CatalogoSonidos.Entrada> entradas = catalogo.Entradas.Where(e => e != null && !ReglaMusicaFases.Pistas.Contains(e.id)).ToList();
            foreach (string id in ReglaMusicaFases.Pistas)
            {
                AudioClip clip = Importar(id);
                if (clip == null)
                {
                    Debug.LogError("[Sonido] Falta " + id + " en " + Carpeta);
                    continue;
                }
                entradas.Add(new CatalogoSonidos.Entrada { id = id, finales = new[] { clip } });
                Debug.Log("[Sonido] " + id + ": " + clip.length.ToString("0.0") + " s, " + clip.channels + " canales, " + clip.loadType);
            }
            catalogo.Fijar(entradas.OrderBy(e => e.id, System.StringComparer.Ordinal).ToList());
            catalogo.SincronizarInterruptores();
            foreach (string id in ReglaMusicaFases.Pistas)
            {
                catalogo.FijarEncendido(id, true);
            }
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
        }

        private static void CopiarFaltantes()
        {
            string raiz = Path.GetDirectoryName(Application.dataPath);
            string origen = Path.Combine(raiz, CarpetaOrigen);
            string destino = Path.Combine(raiz, Carpeta);
            if (!Directory.Exists(origen))
            {
                return;
            }
            Directory.CreateDirectory(destino);
            bool copio = false;
            foreach (string archivo in Directory.GetFiles(origen))
            {
                string copia = Path.Combine(destino, Path.GetFileName(archivo));
                if (!File.Exists(copia))
                {
                    File.Copy(archivo, copia);
                    copio = true;
                }
            }
            AssetDatabase.Refresh();
            if (copio)
            {
                Debug.Log("[Sonido] Copiada la música de " + CarpetaOrigen + " a " + Carpeta);
            }
        }

        private static AudioClip Importar(string id)
        {
            string ruta = AssetDatabase.FindAssets(id + " t:AudioClip", new[] { Carpeta })
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(r => Path.GetFileNameWithoutExtension(r) == id);
            if (ruta == null)
            {
                return null;
            }
            AudioImporter imp = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (imp != null)
            {
                imp.forceToMono = false;
                imp.loadInBackground = true;
                imp.ambisonic = false;
                AudioImporterSampleSettings s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = CalidadVorbis;
                s.preloadAudioData = false;
                s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }
    }
}
