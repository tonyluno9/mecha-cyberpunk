using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-1654 (T28): pasos más suaves y parejos. Los PAS_*_S##.wav de Assets/Arte/Audio/05_Pasos salen de los PAS_*_NN originales (recortados a una pisada, filtrados
    /// con un paso bajo, normalizados al mismo nivel y, en concreto, metal, madera y mojado, remuestreados para tener variantes). Aquí se importan (mono, en memoria) y
    /// pasan a ser los finales de PAS_CONCRETO, PAS_METAL, PAS_MADERA, PAS_MOJADO, PAS_TIERRA y PAS_GRAVA. Se puede correr varias veces.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.Progresion.Editor.AplicarPasosT1654.Aplicar
    /// </summary>
    public static class AplicarPasosT1654
    {
        public const string Carpeta = "Assets/Arte/Audio/05_Pasos";
        public static readonly string[] Ids = { "PAS_CONCRETO", "PAS_METAL", "PAS_MADERA", "PAS_MOJADO", "PAS_TIERRA", "PAS_GRAVA" };

        [MenuItem("Mecha Cyberpunk/T1654/Aplicar pasos suaves")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(CreadorCatalogoSonidos.RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[T-1654] falta " + CreadorCatalogoSonidos.RutaCatalogo);
                return;
            }
            List<CatalogoSonidos.Entrada> lista = new List<CatalogoSonidos.Entrada>(catalogo.Entradas);
            foreach (string id in Ids)
            {
                List<AudioClip> clips = new List<AudioClip>();
                foreach (string ruta in Directory.GetFiles(Carpeta, id + "_2?.wav"))
                {
                    string assetRuta = ruta.Replace('\\', '/');
                    Importar(assetRuta);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetRuta);
                    if (clip != null)
                    {
                        clips.Add(clip);
                    }
                }
                if (clips.Count == 0)
                {
                    Debug.LogWarning("[T-1654] sin clips S## para " + id);
                    continue;
                }
                clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                CatalogoSonidos.Entrada entrada = lista.Find(e => e != null && e.id == id);
                if (entrada == null)
                {
                    entrada = new CatalogoSonidos.Entrada { id = id };
                    lista.Add(entrada);
                }
                entrada.finales = clips.ToArray();
                Debug.Log("[T-1654] " + id + ": " + clips.Count + " clips");
            }
            catalogo.Fijar(lista);
            catalogo.SincronizarInterruptores();
            foreach (string id in Ids)
            {
                catalogo.FijarEncendido(id, true);
            }
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1654] pasos suaves aplicados al catalogo.");
        }

        private static void Importar(string ruta)
        {
            AssetDatabase.ImportAsset(ruta);
            AudioImporter importador = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (importador == null)
            {
                return;
            }
            AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            ajustes.compressionFormat = AudioCompressionFormat.ADPCM;
            ajustes.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            ajustes.preloadAudioData = true;
            importador.defaultSampleSettings = ajustes;
            importador.forceToMono = true;
            importador.loadInBackground = false;
            importador.ambisonic = false;
            importador.SaveAndReimport();
        }
    }
}
