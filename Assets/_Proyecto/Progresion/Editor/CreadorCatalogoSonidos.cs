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
        /// <summary>T-1471: carpetas de los sonidos nuevos (T-1470), dentro de Assets/Arte/Audio.</summary>
        public static readonly string[] CarpetasNuevas =
        {
            "01_UI", "02_Armas", "03_Impactos", "04_Explosiones", "05_Pasos", "06_Jugador", "07_Interaccion", "08_Consumibles", "09_Vehiculos", "10_Policia",
        };

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
            // T-1471: las 10 carpetas nuevas (19-nov). Si un ID nuevo ya existia (UI_MOVER, UI_ERROR, VEH_PUERTA_ABRIR...), sus archivos REEMPLAZAN a los finales viejos.
            SortedDictionary<string, CatalogoSonidos.Entrada> nuevas = new SortedDictionary<string, CatalogoSonidos.Entrada>();
            foreach (string carpeta in CarpetasNuevas)
            {
                Leer(CarpetaAudio + "/" + carpeta, nuevas, false);
            }
            MapaSonidos19nov.Reemplazar(entradas, nuevas.Values);
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            if (catalogo == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaCatalogo));
                catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
                AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
            }
            catalogo.Fijar(new List<CatalogoSonidos.Entrada>(entradas.Values));
            catalogo.SincronizarInterruptores();
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
