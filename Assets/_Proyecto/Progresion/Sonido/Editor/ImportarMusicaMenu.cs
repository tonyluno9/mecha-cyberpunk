using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido.Editor
{
    /// <summary>
    /// Tema del menú principal: importa Assets/Arte/Audio/Musica/MUS_MENU_PRINCIPAL.wav como música (estéreo, Streaming, Vorbis, sin
    /// precargar) y lo pone en el CatalogoSonidos con el ID MUS_MENU_PRINCIPAL (sección Música por el prefijo MUS_), encendido.
    /// Solo agrega o reemplaza esa entrada (no rearma el catálogo). Repetible.
    /// </summary>
    public static class ImportarMusicaMenu
    {
        public const string RutaClip = "Assets/Arte/Audio/Musica/" + MusicaMenu.Id + ".wav";
        public const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        private const float CalidadVorbis = 0.8f;

        [MenuItem("Mecha Cyberpunk/Sonido/Importar música del menú")]
        public static void Aplicar()
        {
            AudioImporter imp = AssetImporter.GetAtPath(RutaClip) as AudioImporter;
            if (imp == null)
            {
                Debug.LogError("[Sonido] No está " + RutaClip);
                return;
            }
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

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaClip);
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            List<CatalogoSonidos.Entrada> entradas = catalogo.Entradas.Where(e => e != null && e.id != MusicaMenu.Id).ToList();
            entradas.Add(new CatalogoSonidos.Entrada { id = MusicaMenu.Id, finales = new[] { clip } });
            catalogo.Fijar(entradas.OrderBy(e => e.id, System.StringComparer.Ordinal).ToList());
            catalogo.SincronizarInterruptores();
            catalogo.FijarEncendido(MusicaMenu.Id, true);
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sonido] " + MusicaMenu.Id + ": " + (clip != null ? clip.length.ToString("0.00") + " s, " + clip.channels + " canales, " + clip.loadType : "sin clip")
                + "; en el catálogo: " + catalogo.Entradas.Any(e => e != null && e.id == MusicaMenu.Id) + ", sección " + ReglaCatalogoSonidos.SeccionDe(MusicaMenu.Id));
        }
    }
}
