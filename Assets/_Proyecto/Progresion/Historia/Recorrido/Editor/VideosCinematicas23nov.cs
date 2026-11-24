using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace MechaCyberpunk.Progresion.Recorrido.Editor
{
    /// <summary>
    /// T-cinematicas-videos-23nov: pone los videos reales en los DatosCinematica. Cada cinemática del juego toma, en orden de nombre (01_, 02_…),
    /// los .mp4 de Assets/Cinematicas/&lt;ID&gt;/. C03 LA DECISIÓN lleva al final la parte pendiente de C07 (Ranura B) mientras no haya clips de C07;
    /// C04 EL SUERO queda pendiente mientras Assets/Cinematicas/C04/ no tenga clips. Se puede correr varias veces.
    /// T-cinematicas-narracion-24nov: también pone la narración de cada clip (Assets/Arte/Audio/19_Voces/Narracion/NAR_&lt;clave&gt;.wav, donde la
    /// clave es el nombre del video sin el número: 06_C04_F08_Se_escabulle → NAR_C04_F08) con 1 s de pausa, e importa esas voces mono.
    /// C05 (Tino, la primera mejora del arma) toma los videos 25–27 de Assets/Cinematicas/C05/.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Recorrido.Editor.VideosCinematicas23nov.Aplicar
    /// </summary>
    public static class VideosCinematicas23nov
    {
        public const string CarpetaVideos = "Assets/Cinematicas";
        public const string CarpetaDatos = "Assets/_Proyecto/Progresion/Historia/Cinematicas";
        public const float SegundosPendienteC07 = 6f;
        public const string TextoPendienteC07 = "Desde la grieta ve cómo los SKULLS golpean al vendedor. Duda… y regresa por ellos.";

        public const string CarpetaNarracion = "Assets/Arte/Audio/19_Voces/Narracion";
        public const string PrefijoNarracion = "NAR_";

        private static readonly string[] Ids = { "C01", "C02", "C03", "C04", "C05" };

        [MenuItem("Mecha Cyberpunk/Historia/Poner videos de las cinemáticas (23-nov)")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            Dictionary<string, AudioClip> voces = CargarNarraciones();
            var informe = new List<string>();
            foreach (string id in Ids)
            {
                DatosCinematica datos = Buscar(id);
                if (datos == null)
                {
                    informe.Add(id + ": no hay DatosCinematica");
                    continue;
                }
                // T97: las que ya tienen imágenes fijas en Frames/ las pone FramesCinematicasT97 (no se les vuelven a poner los videos).
                string frames = CarpetaVideos + "/" + id + "/" + FramesCinematicasT97.SubcarpetaFrames;
                if (AssetDatabase.IsValidFolder(frames) && AssetDatabase.FindAssets("t:Texture2D", new[] { frames }).Length > 0)
                {
                    informe.Add(id + ": imágenes fijas (T97), no se toca");
                    continue;
                }
                List<VideoClip> clips = Clips(id);
                float pendientes = 0f;
                string linea = string.Empty;
                if (id == "C03" && !clips.Any(c => c.name.Contains("_C07_")))
                {
                    pendientes = SegundosPendienteC07;
                    linea = TextoPendienteC07;
                }
                datos.ConfigurarVideos(clips.ToArray(), pendientes, linea);
                var narraciones = new AudioClip[clips.Count];
                var pausas = new float[clips.Count];
                int conVoz = 0;
                for (int i = 0; i < clips.Count; i++)
                {
                    narraciones[i] = BuscarNarracion(clips[i].name, voces);
                    pausas[i] = ReglaNarracion.PausaPorDefecto;
                    if (narraciones[i] != null) conVoz++;
                }
                datos.ConfigurarNarracion(narraciones, pausas);
                EditorUtility.SetDirty(datos);
                informe.Add(id + ": " + clips.Count + " clips, " + datos.DuracionVideos.ToString("0.0") + " s de video"
                    + (pendientes > 0f ? " + " + pendientes + " s pendiente" : "") + (clips.Count == 0 ? " (placeholder)" : "")
                    + ", " + conVoz + " con narración, " + datos.DuracionEfectiva.ToString("0.0") + " s con la regla");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Cinemáticas 23-nov] " + string.Join(" | ", informe));
        }

        /// <summary>La clave de narración de un video: su nombre sin el número del principio ("06_C04_F08_Se_escabulle" → "C04_F08_Se_escabulle").</summary>
        public static string SinNumero(string nombreVideo)
        {
            int guion = nombreVideo.IndexOf('_');
            return guion > 0 && char.IsDigit(nombreVideo[0]) ? nombreVideo.Substring(guion + 1) : nombreVideo;
        }

        /// <summary>La narración del video: la clave más larga de NAR_* con la que empieza su nombre sin número (seguida de "_" o al final).</summary>
        public static AudioClip BuscarNarracion(string nombreVideo, Dictionary<string, AudioClip> voces)
        {
            string resto = SinNumero(nombreVideo);
            AudioClip mejor = null;
            int largo = -1;
            foreach (KeyValuePair<string, AudioClip> par in voces)
            {
                string clave = par.Key;
                bool coincide = resto == clave || resto.StartsWith(clave + "_", System.StringComparison.Ordinal);
                if (coincide && clave.Length > largo)
                {
                    mejor = par.Value;
                    largo = clave.Length;
                }
            }
            return mejor;
        }

        // Las NAR_*.wav de la carpeta (no las de subcarpetas como _sin_efecto), importadas mono y sin 3D; clave = nombre sin "NAR_".
        private static Dictionary<string, AudioClip> CargarNarraciones()
        {
            var voces = new Dictionary<string, AudioClip>();
            if (!AssetDatabase.IsValidFolder(CarpetaNarracion))
            {
                Debug.LogWarning("[Cinemáticas] No está " + CarpetaNarracion + ": las cinemáticas quedan sin narración.");
                return voces;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { CarpetaNarracion }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                if (Path.GetDirectoryName(ruta).Replace('\\', '/') != CarpetaNarracion || !nombre.StartsWith(PrefijoNarracion, System.StringComparison.Ordinal))
                {
                    continue;
                }
                ImportarVoz(ruta);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                if (clip != null)
                {
                    voces[nombre.Substring(PrefijoNarracion.Length)] = clip;
                }
            }
            return voces;
        }

        // Voz en off: mono, se descomprime al cargar (frases cortas, sin retraso al empezar con su video).
        private static void ImportarVoz(string ruta)
        {
            var importador = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (importador == null)
            {
                return;
            }
            AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
            bool cambia = !importador.forceToMono || ajustes.loadType != AudioClipLoadType.DecompressOnLoad || importador.loadInBackground;
            if (!cambia)
            {
                return;
            }
            importador.forceToMono = true;
            importador.loadInBackground = false;
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            importador.defaultSampleSettings = ajustes;
            importador.SaveAndReimport();
        }

        private static DatosCinematica Buscar(string id)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DatosCinematica", new[] { CarpetaDatos }))
            {
                var d = AssetDatabase.LoadAssetAtPath<DatosCinematica>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.Id == id)
                {
                    return d;
                }
            }
            return null;
        }

        private static List<VideoClip> Clips(string id)
        {
            var lista = new List<VideoClip>();
            string carpeta = CarpetaVideos + "/" + id;
            if (!AssetDatabase.IsValidFolder(carpeta))
            {
                return lista;
            }
            var rutas = AssetDatabase.FindAssets("t:VideoClip", new[] { carpeta })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(r => Path.GetFileName(r), System.StringComparer.Ordinal);
            foreach (string ruta in rutas)
            {
                VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(ruta);
                if (clip != null)
                {
                    lista.Add(clip);
                }
            }
            return lista;
        }
    }
}
