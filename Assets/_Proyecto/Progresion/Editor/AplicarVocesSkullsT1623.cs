using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-1623: importa las voces de los SKULLS (Assets/Arte/Audio/19_Voces/SKULLS/SKU_*_NN.wav: mono, DecompressOnLoad, precargadas) y deja sus 13 IDs en el CatalogoSonidos
    /// (todas las variantes como finales) con su interruptor encendido. Se puede correr varias veces.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.Progresion.Editor.AplicarVocesSkullsT1623.Aplicar
    /// </summary>
    public static class AplicarVocesSkullsT1623
    {
        public const string Carpeta = "Assets/Arte/Audio/19_Voces/SKULLS";
        private static readonly Regex Variante = new Regex(@"^(.+?)(_\d\d)?$");

        [MenuItem("Mecha Cyberpunk/T1623/Aplicar voces de los SKULLS")]
        public static void Aplicar()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                AssetDatabase.Refresh();
            }
            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                Debug.LogError("[T-1623] falta la carpeta " + Carpeta);
                return;
            }
            ImportarClips();
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(CreadorCatalogoSonidos.RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[T-1623] falta " + CreadorCatalogoSonidos.RutaCatalogo);
                return;
            }
            SortedDictionary<string, CatalogoSonidos.Entrada> nuevas = new SortedDictionary<string, CatalogoSonidos.Entrada>();
            LeerClips(nuevas);
            List<CatalogoSonidos.Entrada> lista = new List<CatalogoSonidos.Entrada>();
            foreach (CatalogoSonidos.Entrada e in catalogo.Entradas)
            {
                // T-voces-23nov: solo se rehacen los IDs de los SKULLS comunes; el matón y el jefe (SKU_MATON_*, SKU_JEFE_*) los deja AplicarVoces23nov.
                if (e != null && System.Array.IndexOf(ReglaMezclaSonidos.IdsSkulls, e.id) < 0)
                {
                    lista.Add(e);
                }
            }
            lista.AddRange(nuevas.Values);
            catalogo.Fijar(lista);
            catalogo.SincronizarInterruptores();
            foreach (string id in ReglaMezclaSonidos.IdsSkulls)
            {
                catalogo.FijarEncendido(id, true);
                if (!nuevas.ContainsKey(id))
                {
                    Debug.LogWarning("[T-1623] el ID " + id + " no tiene archivos en " + Carpeta);
                }
            }
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            int clips = 0;
            foreach (CatalogoSonidos.Entrada e in nuevas.Values)
            {
                clips += e.finales.Length;
            }
            Debug.Log("[T-1623] voces de los SKULLS: " + nuevas.Count + " IDs, " + clips + " clips en el catalogo.");
        }

        private static void ImportarClips()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importador = AssetImporter.GetAtPath(ruta) as AudioImporter;
                if (importador == null)
                {
                    continue;
                }
                AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
                ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
                ajustes.compressionFormat = AudioCompressionFormat.ADPCM;
                ajustes.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                ajustes.preloadAudioData = true;
                importador.defaultSampleSettings = ajustes;
                importador.forceToMono = true;
                importador.loadInBackground = false;
                importador.ambisonic = false;
                importador.SaveAndReimport();
            }
        }

        private static void LeerClips(SortedDictionary<string, CatalogoSonidos.Entrada> entradas)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                string id = Variante.Match(Path.GetFileNameWithoutExtension(ruta)).Groups[1].Value;
                if (clip == null || System.Array.IndexOf(ReglaMezclaSonidos.IdsSkulls, id) < 0)
                {
                    continue;
                }
                if (!entradas.TryGetValue(id, out CatalogoSonidos.Entrada e))
                {
                    e = new CatalogoSonidos.Entrada { id = id };
                    entradas[id] = e;
                }
                List<AudioClip> lista = new List<AudioClip>(e.finales) { clip };
                e.finales = lista.ToArray();
            }
        }
    }
}
