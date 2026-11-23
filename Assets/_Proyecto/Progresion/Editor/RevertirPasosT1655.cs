using System.Collections.Generic;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-1655 (T32): regresa los pasos al sonido de antes de T-1654 (commit feec588). Deja en el catálogo, como finales de PAS_CONCRETO, PAS_METAL, PAS_MADERA,
    /// PAS_MOJADO, PAS_TIERRA y PAS_GRAVA, los clips originales PAS_*_NN (por su GUID, tal como estaban). Los PAS_*_21..28 nuevos se quedan en la carpeta pero ya no se usan:
    /// tenían una cola de ruido sostenida (concreto, metal y madera) que se oía como siseo al caminar. Se puede correr varias veces.
    /// </summary>
    public static class RevertirPasosT1655
    {
        // GUIDs de los .wav originales, tomados de CatalogoSonidos.asset antes de feec588.
        private static readonly Dictionary<string, string[]> Originales = new Dictionary<string, string[]>
        {
            { "PAS_CONCRETO", new[] { "a5c2ef1d444e309499ed8707d94cb1a0" } },
            { "PAS_GRAVA", new[] { "1d3589a92ecb5374ea5e43dbaf78e114", "9040c435d409cf147afcd5207310236a", "d48beacebfd241a419ed0e5a1f274eda", "e2fc8a36b872d0949a3c78b6f1cdab5f", "0abf6ba6a4a6f27409073f3777995eff", "667daac9e1ecacd428c90f9122d22172", "ca3c5b5e9fc2de9438f52e233739c913", "5ac7acd04410ecd4f952f8a9444b5fe9" } },
            { "PAS_MADERA", new[] { "555049df0247c6146b644b290bad8919", "bf1f15cfa302fbc4ab162a25cde74622" } },
            { "PAS_METAL", new[] { "fd1ede9b7ceaeeb4e93b828044d97c8f" } },
            { "PAS_MOJADO", new[] { "85a264cd74ead7d4a9d75948bc463074" } },
            { "PAS_TIERRA", new[] { "2fe5b88b64ed4d342a10386ee2d13b1f", "ed95c3faeecadee479d5c67ed04e11f2", "4eb1edcfe4c38044d9beef2e3a41e9f3", "35c1c953538655446aaadf3d82c1f787", "f5ea4f385467ece44bb2af44989d8942", "3dc1045ee0481fb44a64db70aa498dd4", "ae10ac8676df2fb438ea96b6eb374132", "6ddb92f262f7feb4a9ee1caba7e7efbb" } },
        };

        /// <summary>
        /// T-1690 (T49): <see cref="CreadorCatalogoSonidos.Crear"/> (lo corre cada build) vuelve a leer TODOS los PAS_*_NN de 05_Pasos, incluidos los
        /// PAS_*_21..28 de T28 con la cola de ruido (el siseo). Esto deja en cada ID de pasos solo sus clips originales, igual que <see cref="Aplicar"/>.
        /// </summary>
        public static void SoloOriginales(IDictionary<string, CatalogoSonidos.Entrada> entradas)
        {
            foreach (KeyValuePair<string, string[]> par in Originales)
            {
                List<AudioClip> clips = new List<AudioClip>();
                foreach (string guid in par.Value)
                {
                    string ruta = AssetDatabase.GUIDToAssetPath(guid);
                    AudioClip clip = string.IsNullOrEmpty(ruta) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                    if (clip != null) clips.Add(clip);
                }
                if (clips.Count == 0) continue;
                if (!entradas.TryGetValue(par.Key, out CatalogoSonidos.Entrada e) || e == null)
                {
                    e = new CatalogoSonidos.Entrada { id = par.Key };
                    entradas[par.Key] = e;
                }
                e.finales = clips.ToArray();
            }
        }

        [MenuItem("Mecha Cyberpunk/T1655/Regresar pasos a antes de T28")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(CreadorCatalogoSonidos.RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[T-1655] falta " + CreadorCatalogoSonidos.RutaCatalogo);
                return;
            }
            List<CatalogoSonidos.Entrada> lista = new List<CatalogoSonidos.Entrada>(catalogo.Entradas);
            foreach (KeyValuePair<string, string[]> par in Originales)
            {
                List<AudioClip> clips = new List<AudioClip>();
                foreach (string guid in par.Value)
                {
                    string ruta = AssetDatabase.GUIDToAssetPath(guid);
                    AudioClip clip = string.IsNullOrEmpty(ruta) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                    if (clip != null)
                    {
                        clips.Add(clip);
                    }
                    else
                    {
                        Debug.LogWarning("[T-1655] no se encontró el clip original " + guid + " de " + par.Key);
                    }
                }
                if (clips.Count == 0)
                {
                    continue;
                }
                CatalogoSonidos.Entrada entrada = lista.Find(e => e != null && e.id == par.Key);
                if (entrada == null)
                {
                    entrada = new CatalogoSonidos.Entrada { id = par.Key };
                    lista.Add(entrada);
                }
                entrada.finales = clips.ToArray();
                Debug.Log("[T-1655] " + par.Key + ": " + clips.Count + " clips originales");
            }
            catalogo.Fijar(lista);
            catalogo.SincronizarInterruptores();
            foreach (string id in Originales.Keys)
            {
                catalogo.FijarEncendido(id, true);
            }
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1655] pasos regresados a los originales.");
        }
    }
}
