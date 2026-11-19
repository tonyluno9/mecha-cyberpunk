using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión A, 19-nov: arreglos que necesitan ver la escena completa y no solo el prefab de la zona.
    /// QuitarEncimadosNuevos: con la escena Ciudad_Prologo, corre la revisión de T-995 y quita del prefab de su zona las piezas nuevas
    /// de T-1463 (fila del paseo y bancas) y de T-1464 que se enciman con otra pieza, aunque la otra esté fuera del prefab (p. ej. T1364_ZonasTrafico).
    /// Correr después de ZonaMaleconT1463.Aplicar y ZonaAutopistaT1464.Aplicar. Escribe Logs/sesionA-encimados.txt.
    /// </summary>
    public static class ArreglosSesionA19nov
    {
        private static readonly (string prefab, string raiz, string marca)[] Nuevos =
        {
            ("Assets/Arte/Mundo/Zonas/T1354_Malecon.prefab", "T1354_Malecon", "/T1463_Malecon/"),
            ("Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab", "T1356_Autopista", "/T1464_Autopista/"),
        };

        public static void QuitarEncimadosNuevos()
        {
            var log = new StringBuilder("Sesión A 19-nov: encimados de piezas nuevas\n");
            for (int vuelta = 0; vuelta < 3; vuelta++)
            {
                ColocarS2.Abrir();
                Physics.SyncTransforms();
                var porPrefab = new Dictionary<string, HashSet<string>>();
                foreach (RevisarCiudadT995.Encimado e in RevisarCiudadT995.Encimados())
                {
                    foreach (Transform t in new[] { e.A, e.B })
                    {
                        if (t == null)
                        {
                            continue;
                        }
                        string ruta = Ruta(t);
                        foreach (var n in Nuevos)
                        {
                            int i = ruta.IndexOf(n.marca);
                            if (!ruta.StartsWith(n.raiz + "/") || i < 0)
                            {
                                continue;
                            }
                            // Pieza = hijo directo del grupo bajo la raíz nueva (T1463_Malecon/<Grupo>/<Pieza>).
                            string[] partes = ruta.Substring(i + n.marca.Length).Split('/');
                            if (partes.Length < 2)
                            {
                                continue;
                            }
                            string relativa = ruta.Substring(n.raiz.Length + 1, i + n.marca.Length - n.raiz.Length - 1) + partes[0] + "/" + partes[1];
                            if (!porPrefab.TryGetValue(n.prefab, out var set))
                            {
                                porPrefab[n.prefab] = set = new HashSet<string>();
                            }
                            set.Add(relativa);
                            log.AppendLine("  vuelta " + vuelta + ": " + e);
                        }
                    }
                }
                if (porPrefab.Count == 0)
                {
                    log.AppendLine("Vuelta " + vuelta + ": sin encimados de piezas nuevas.");
                    break;
                }
                foreach (var par in porPrefab)
                {
                    GameObject raiz = PrefabUtility.LoadPrefabContents(par.Key);
                    int quitadas = 0;
                    foreach (string rel in par.Value)
                    {
                        Transform p = raiz.transform.Find(rel);
                        if (p != null)
                        {
                            Object.DestroyImmediate(p.gameObject);
                            quitadas++;
                            log.AppendLine("  quitada " + rel);
                        }
                        else
                        {
                            log.AppendLine("  no se halló en el prefab: " + rel);
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(raiz, par.Key);
                    PrefabUtility.UnloadPrefabContents(raiz);
                    log.AppendLine("Vuelta " + vuelta + ": " + par.Key + " -> " + quitadas + " piezas quitadas.");
                }
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/sesionA-encimados.txt", log.ToString());
            Debug.Log("[Arte] " + log);
        }

        private static string Ruta(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }
    }
}
