using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// Arma el CatalogoSonidos (T-407): lee Assets/Arte/Audio/&lt;SEC&gt;/&lt;ID&gt;.wav y &lt;ID&gt;_NN.wav (los finales) y Assets/Arte/Audio/Provisional/&lt;SEC&gt;/ (los provisionales).
    /// Los dos builds lo corren antes de construir, así los WAV que entregue el equipo entran solos.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.CreadorCatalogoSonidos.Crear
    /// </summary>
    public static class CreadorCatalogoSonidos
    {
        public const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        public const string CarpetaAudio = "Assets/Arte/Audio";
        private static readonly string[] Secciones = { "JUG", "PUN", "ARM", "VEH", "PAN", "CAL", "ZON", "ECO", "UI", "MUN", "AMB", "MUS" };
        private static readonly Regex Variante = new Regex(@"^(.+?)(_\d\d)?$");

        [MenuItem("Mecha Cyberpunk/Sonido/Crear catalogo")]
        public static void Crear()
        {
            GeneradorSonidosProvisionales.Crear();
            SortedDictionary<string, CatalogoSonidos.Entrada> entradas = new SortedDictionary<string, CatalogoSonidos.Entrada>();
            foreach (string seccion in Secciones)
            {
                Leer(CarpetaAudio + "/" + seccion, entradas, false);
                Leer(CarpetaAudio + "/Provisional/" + seccion, entradas, true);
            }
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            if (catalogo == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaCatalogo));
                catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
                AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
            }
            catalogo.Fijar(new List<CatalogoSonidos.Entrada>(entradas.Values));
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sonido] CreadorCatalogoSonidos: " + entradas.Count + " IDs en " + RutaCatalogo + ".");
        }

        private static void Leer(string carpeta, SortedDictionary<string, CatalogoSonidos.Entrada> entradas, bool provisional)
        {
            if (!AssetDatabase.IsValidFolder(carpeta)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(ruta).Replace('\\', '/') != carpeta) continue;
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                string id = Variante.Match(Path.GetFileNameWithoutExtension(ruta)).Groups[1].Value;
                if (!entradas.TryGetValue(id, out CatalogoSonidos.Entrada e))
                {
                    e = new CatalogoSonidos.Entrada { id = id };
                    entradas[id] = e;
                }
                List<AudioClip> lista = new List<AudioClip>(provisional ? e.provisionales : e.finales) { clip };
                if (provisional) e.provisionales = lista.ToArray(); else e.finales = lista.ToArray();
            }
        }
    }
}
