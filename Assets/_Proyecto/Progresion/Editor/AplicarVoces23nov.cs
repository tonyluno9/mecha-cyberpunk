using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-voces-personajes-23nov: importa TODAS las voces de Assets/Arte/Audio/19_Voces (cada subcarpeta, también Policia/_megafono; nunca _sin_efecto) en mono:
    /// las frases largas (Jefe_SKULLS, Jefe_Nova, Doc) Vorbis comprimidas en memoria, las demás ADPCM descomprimidas al cargar. Después deja cada ID
    /// (nombre sin el _NN) en el CatalogoSonidos con todas sus tomas como finales, conserva las demás entradas, sincroniza los interruptores y prende los IDs.
    /// Se puede correr varias veces.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.Progresion.Editor.AplicarVoces23nov.Aplicar
    /// </summary>
    public static class AplicarVoces23nov
    {
        public const string Carpeta = Voces23nov.CarpetaRaiz;
        private const string SinEfecto = "_sin_efecto";
        private const float CalidadVorbis = 0.7f;
        private static readonly Regex Variante = new Regex(@"^(.+?)(_\d\d)?$");

        [MenuItem("Mecha Cyberpunk/Sonido/Aplicar voces 23-nov")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                Debug.LogError("[Voces23nov] falta la carpeta " + Carpeta);
                return;
            }
            int importados = ImportarClips();
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(CreadorCatalogoSonidos.RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[Voces23nov] falta " + CreadorCatalogoSonidos.RutaCatalogo);
                return;
            }
            SortedDictionary<string, CatalogoSonidos.Entrada> nuevas = new SortedDictionary<string, CatalogoSonidos.Entrada>();
            LeerClips(nuevas);
            List<CatalogoSonidos.Entrada> lista = new List<CatalogoSonidos.Entrada>();
            foreach (CatalogoSonidos.Entrada e in catalogo.Entradas)
            {
                if (e != null && !nuevas.ContainsKey(e.id))
                {
                    lista.Add(e);
                }
            }
            lista.AddRange(nuevas.Values);
            catalogo.Fijar(lista);
            catalogo.SincronizarInterruptores();
            foreach (string id in nuevas.Keys)
            {
                catalogo.FijarEncendido(id, true);
            }
            foreach (string id in Voces23nov.Ids)
            {
                if (!nuevas.ContainsKey(id))
                {
                    Debug.LogWarning("[Voces23nov] el ID " + id + " no tiene archivos en " + Carpeta);
                }
            }
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            int clips = 0;
            foreach (CatalogoSonidos.Entrada e in nuevas.Values)
            {
                clips += e.finales.Length;
            }
            Debug.Log("[Voces23nov] " + importados + " WAV importados; " + nuevas.Count + " IDs y " + clips + " clips de 19_Voces en el catalogo.");
        }

        /// <summary>La carpeta del WAV relativa a 19_Voces ("Doc", "Policia/_megafono"...), o null si no está dentro.</summary>
        public static string CarpetaRelativa(string ruta)
        {
            string dir = Path.GetDirectoryName(ruta).Replace('\\', '/');
            if (!dir.StartsWith(Carpeta + "/")) return null;
            return dir.Substring(Carpeta.Length + 1);
        }

        private static bool Ignorar(string ruta) => ruta.Contains("/" + SinEfecto + "/") || ruta.Contains("/" + SinEfecto) || !ruta.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase);

        private static int ImportarClips()
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string relativa = CarpetaRelativa(ruta);
                if (relativa == null || Ignorar(ruta))
                {
                    continue;
                }
                AudioImporter importador = AssetImporter.GetAtPath(ruta) as AudioImporter;
                if (importador == null)
                {
                    continue;
                }
                bool larga = Voces23nov.EsCarpetaDeFrasesLargas(relativa);
                AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
                ajustes.loadType = larga ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                ajustes.compressionFormat = larga ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                ajustes.quality = CalidadVorbis;
                ajustes.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                ajustes.preloadAudioData = true;
                importador.defaultSampleSettings = ajustes;
                importador.forceToMono = true;
                importador.loadInBackground = false;
                importador.ambisonic = false;
                importador.SaveAndReimport();
                n++;
            }
            return n;
        }

        private static void LeerClips(SortedDictionary<string, CatalogoSonidos.Entrada> entradas)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (CarpetaRelativa(ruta) == null || Ignorar(ruta))
                {
                    continue;
                }
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                if (clip == null)
                {
                    continue;
                }
                string id = Variante.Match(Path.GetFileNameWithoutExtension(ruta)).Groups[1].Value;
                if (!entradas.TryGetValue(id, out CatalogoSonidos.Entrada e))
                {
                    e = new CatalogoSonidos.Entrada { id = id };
                    entradas[id] = e;
                }
                List<AudioClip> lista = new List<AudioClip>(e.finales) { clip };
                lista.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                e.finales = lista.ToArray();
            }
        }
    }
}
