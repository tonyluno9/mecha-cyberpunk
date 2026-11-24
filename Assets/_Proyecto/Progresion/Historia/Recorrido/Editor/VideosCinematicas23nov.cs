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
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Recorrido.Editor.VideosCinematicas23nov.Aplicar
    /// </summary>
    public static class VideosCinematicas23nov
    {
        public const string CarpetaVideos = "Assets/Cinematicas";
        public const string CarpetaDatos = "Assets/_Proyecto/Progresion/Historia/Cinematicas";
        public const float SegundosPendienteC07 = 6f;
        public const string TextoPendienteC07 = "Desde la grieta ve cómo los SKULLS golpean al vendedor. Duda… y regresa por ellos.";

        private static readonly string[] Ids = { "C01", "C02", "C03", "C04" };

        [MenuItem("Mecha Cyberpunk/Historia/Poner videos de las cinemáticas (23-nov)")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            var informe = new List<string>();
            foreach (string id in Ids)
            {
                DatosCinematica datos = Buscar(id);
                if (datos == null)
                {
                    informe.Add(id + ": no hay DatosCinematica");
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
                EditorUtility.SetDirty(datos);
                informe.Add(id + ": " + clips.Count + " clips, " + datos.DuracionVideos.ToString("0.0") + " s de video"
                    + (pendientes > 0f ? " + " + pendientes + " s pendiente" : "") + (clips.Count == 0 ? " (placeholder)" : ""));
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Cinemáticas 23-nov] " + string.Join(" | ", informe));
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
