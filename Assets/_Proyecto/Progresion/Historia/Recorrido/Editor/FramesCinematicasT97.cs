using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace MechaCyberpunk.Progresion.Recorrido.Editor
{
    /// <summary>
    /// T97: las cinemáticas C02 a C05 pasan de videos a imágenes fijas (frames). Cada una toma, en orden de nombre, los PNG de
    /// Assets/Cinematicas/&lt;ID&gt;/Frames/ (04_C02_F01_…, 05_C03_F04_…, …) como tomas fijas; sus .mp4 se quedan en el repo pero ya no se
    /// referencian. C01 (el campo y el despertar) no se toca: sigue en video.
    /// <list type="bullet">
    /// <item>Importa cada PNG como textura Default sin mipmaps, máx. 2048, compresión de alta calidad, Clamp y Bilinear.</item>
    /// <item>Narración: la NAR_* cuya clave empieza el nombre sin número (como <see cref="VideosCinematicas23nov.BuscarNarracion"/>), con 1 s de
    /// pausa. Una misma voz va solo en la primera toma que la pide (NAR_C02 dura lo que antes duraba el video de F01–F03: va en F01 y F02–F03
    /// quedan sin voz, 3 s cada una).</item>
    /// <item>Si una cinemática no tiene PNG en Frames/ (aún no importados) se deja como está.</item>
    /// </list>
    /// Se puede correr varias veces. Correr DESPUÉS de VideosCinematicas23nov si se vuelve a correr aquel (aquel vuelve a poner los videos).
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Recorrido.Editor.FramesCinematicasT97.Aplicar
    /// </summary>
    public static class FramesCinematicasT97
    {
        public const string SubcarpetaFrames = "Frames";
        public const int TamanoMaximo = 2048;

        private static readonly string[] Ids = { "C02", "C03", "C04", "C05" };

        [MenuItem("Mecha Cyberpunk/Historia/Poner imágenes fijas de las cinemáticas (T97)")]
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
                List<Texture2D> fotos = Fotos(id);
                if (fotos.Count == 0)
                {
                    informe.Add(id + ": sin PNG en Frames/ (se deja como estaba)");
                    continue;
                }
                datos.ConfigurarTomas(new VideoClip[fotos.Count], fotos.ToArray(), 0f, string.Empty);
                var narraciones = new AudioClip[fotos.Count];
                var pausas = new float[fotos.Count];
                var usadas = new HashSet<AudioClip>();
                int conVoz = 0;
                for (int i = 0; i < fotos.Count; i++)
                {
                    AudioClip voz = VideosCinematicas23nov.BuscarNarracion(fotos[i].name, voces);
                    if (voz != null && usadas.Add(voz))
                    {
                        narraciones[i] = voz;
                        conVoz++;
                    }
                    pausas[i] = ReglaNarracion.PausaPorDefecto;
                }
                datos.ConfigurarNarracion(narraciones, pausas);
                EditorUtility.SetDirty(datos);
                informe.Add(id + ": " + fotos.Count + " imágenes fijas, " + conVoz + " con narración, " + datos.DuracionEfectiva.ToString("0.0") + " s");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Cinemáticas T97] " + string.Join(" | ", informe));
        }

        /// <summary>Los PNG de Assets/Cinematicas/&lt;id&gt;/Frames/ en orden de nombre, ya con su importador ajustado.</summary>
        private static List<Texture2D> Fotos(string id)
        {
            var lista = new List<Texture2D>();
            string carpeta = VideosCinematicas23nov.CarpetaVideos + "/" + id + "/" + SubcarpetaFrames;
            if (!AssetDatabase.IsValidFolder(carpeta))
            {
                return lista;
            }
            var rutas = AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(r => r.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => Path.GetFileName(r), System.StringComparer.Ordinal);
            foreach (string ruta in rutas)
            {
                Importar(ruta);
                Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
                if (textura != null)
                {
                    lista.Add(textura);
                }
            }
            return lista;
        }

        // Textura a pantalla completa: sin mipmaps (se ve siempre a su tamaño), tamaño real sin escalar a potencia de 2, calidad alta.
        private static void Importar(string ruta)
        {
            var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null)
            {
                return;
            }
            bool igual = imp.textureType == TextureImporterType.Default && imp.textureShape == TextureImporterShape.Texture2D
                && !imp.mipmapEnabled && imp.maxTextureSize == TamanoMaximo && imp.textureCompression == TextureImporterCompression.CompressedHQ
                && imp.npotScale == TextureImporterNPOTScale.None && imp.wrapMode == TextureWrapMode.Clamp && imp.filterMode == FilterMode.Bilinear
                && imp.sRGBTexture && !imp.isReadable && imp.alphaSource == TextureImporterAlphaSource.None;
            if (igual)
            {
                return;
            }
            imp.textureType = TextureImporterType.Default;
            imp.textureShape = TextureImporterShape.Texture2D;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = TamanoMaximo;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.sRGBTexture = true;
            imp.isReadable = false;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.SaveAndReimport();
        }

        // Las NAR_*.wav de la carpeta de narración (no las de subcarpetas); clave = nombre sin "NAR_". Su importación ya la hizo VideosCinematicas23nov.
        private static Dictionary<string, AudioClip> CargarNarraciones()
        {
            var voces = new Dictionary<string, AudioClip>();
            string carpeta = VideosCinematicas23nov.CarpetaNarracion;
            if (!AssetDatabase.IsValidFolder(carpeta))
            {
                Debug.LogWarning("[Cinemáticas T97] No está " + carpeta + ": las imágenes quedan sin narración (3 s cada una).");
                return voces;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                if (Path.GetDirectoryName(ruta).Replace('\\', '/') != carpeta
                    || !nombre.StartsWith(VideosCinematicas23nov.PrefijoNarracion, System.StringComparison.Ordinal))
                {
                    continue;
                }
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                if (clip != null)
                {
                    voces[nombre.Substring(VideosCinematicas23nov.PrefijoNarracion.Length)] = clip;
                }
            }
            return voces;
        }

        private static DatosCinematica Buscar(string id)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DatosCinematica", new[] { VideosCinematicas23nov.CarpetaDatos }))
            {
                var d = AssetDatabase.LoadAssetAtPath<DatosCinematica>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.Id == id)
                {
                    return d;
                }
            }
            return null;
        }
    }
}
