using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Noche del 19-nov: corre en una sola sesión de Unity los pasos de las tareas T-1382..T-1389, cada uno aislado con try/catch.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.Correr19nov.Correr -pasos Tipo.Metodo,Tipo.Metodo...
    /// Los tipos se buscan por nombre corto en todos los ensamblados (no hace falta referenciar sus asmdef).
    /// Escribe Logs/correr19nov.txt.
    /// </summary>
    public static class Correr19nov
    {
        public static void Correr()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-pasos");
            string lista = i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
            var log = new StringBuilder();
            foreach (var paso in lista.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t0 = DateTime.Now;
                try
                {
                    int punto = paso.LastIndexOf('.');
                    string tipo = paso.Substring(0, punto), metodo = paso.Substring(punto + 1);
                    var t = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                        .FirstOrDefault(x => x.Name == tipo || x.FullName == tipo);
                    if (t == null) throw new Exception("no existe el tipo " + tipo);
                    var m = t.GetMethod(metodo, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (m == null) throw new Exception("no existe el método " + paso);
                    m.Invoke(null, null);
                    log.AppendLine($"OK\t{paso}\t{(DateTime.Now - t0).TotalSeconds:0}s");
                }
                catch (Exception e)
                {
                    var real = e is System.Reflection.TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                    log.AppendLine($"FALLO\t{paso}\t{(DateTime.Now - t0).TotalSeconds:0}s\t{real.GetType().Name}: {real.Message}\n{real.StackTrace}");
                    Debug.LogException(real);
                }
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/correr19nov.txt", log.ToString());
            }
        }

        /// <summary>Causa del piso de escarcha (T-1383): la normal del asfalto S2c estaba importada como textura Default y en sRGB.</summary>
        public static void ArreglarNormalesAsfalto()
        {
            foreach (var ruta in new[] { "Assets/Arte/Exteriores/Texturas/T535_Asfalto_Normal.png" })
            {
                var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (imp == null) { Debug.LogWarning("Correr19nov: no existe " + ruta); continue; }
                if (imp.textureType != TextureImporterType.NormalMap)
                {
                    imp.textureType = TextureImporterType.NormalMap;
                    imp.sRGBTexture = false;
                    imp.SaveAndReimport();
                    Debug.Log("Correr19nov: " + ruta + " ahora es NormalMap");
                }
            }
        }

        /// <summary>El skyline del Malecón (T-1393) copió el material antes de que T-1392 le pusiera el shader sin niebla: a 495 m la niebla lo pintaba como un panel liso.</summary>
        public static void ArreglarSkylineMalecon()
        {
            var sh = Shader.Find("MechaCyberpunk/Fondos/SkylineSinNiebla");
            foreach (var ruta in new[] { "Assets/Arte/Mundo/S2c/T1393_Skyline.mat" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null || sh == null) { Debug.LogWarning("Correr19nov: falta " + ruta + " o el shader"); continue; }
                var tex = m.GetTexture("_BaseMap");
                m.shader = sh;
                m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1.1f, 1.1f, 1.1f, 1f));
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>T-1400.QuitarColumna apagó el mástil de la grúa del barrio y las ventanas de una fachada de T530: son de los barrios, se reencienden.</summary>
        public static void ReencenderBarrio()
        {
            var escena = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity");
            foreach (var ruta in new[] { "Barrio/PulidoFinales/GruaYAndamio/MastilGrua", "T530_Afueras/Salida_4/Ventanas" })
            {
                var partes = ruta.Split('/');
                var raiz = System.Array.Find(escena.GetRootGameObjects(), g => g.name == partes[0]);
                var t = raiz == null ? null : raiz.transform.Find(string.Join("/", partes, 1, partes.Length - 1));
                var r = t == null ? null : t.GetComponent<Renderer>();
                if (r != null) { r.enabled = true; Debug.Log("Correr19nov: reencendido " + ruta); } else { Debug.LogWarning("Correr19nov: no está " + ruta); }
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(escena);
        }

        /// <summary>T-1403 apagó también azoteas (AzoteaT1318), rieles de escalera y un arroyo: son legítimos (finos o planos). Se reencienden; quedan apagados solo los halos.</summary>
        public static void ReencenderT1403()
        {
            var lineas = System.IO.File.ReadAllLines("Logs/t1403-apagados.txt");
            var porPrefab = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string[]>>();
            var quedan = new StringBuilder();
            foreach (var l in lineas)
            {
                var c = l.Split('|');
                if (c.Length < 3) continue;
                if (c[2] == "Halo") { quedan.AppendLine(l); continue; }
                if (!porPrefab.ContainsKey(c[0])) porPrefab[c[0]] = new System.Collections.Generic.List<string[]>();
                porPrefab[c[0]].Add(c);
            }
            int n = 0;
            foreach (var kv in porPrefab)
            {
                var raiz = PrefabUtility.LoadPrefabContents(kv.Key);
                foreach (var c in kv.Value)
                {
                    Transform t = raiz.transform;
                    foreach (var i in c[1].Split('/')) { int k = int.Parse(i); t = k < t.childCount ? t.GetChild(k) : null; if (t == null) break; }
                    var r = t == null ? null : t.GetComponent<Renderer>();
                    if (r != null && t.name == c[2]) { r.enabled = true; n++; }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, kv.Key);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            System.IO.File.WriteAllText("Logs/t1403-apagados.txt", quedan.ToString());
            Debug.Log("Correr19nov: reencendidos " + n + " renderers de T-1403");
        }

        /// <summary>Apaga, dentro de T1388_Uniones, lo que tenga collider en las bocas U1..U9 entre 1 y 3 m (lo que mide OrillasT1358Pruebas).</summary>
        public static void DespejarBocasUniones()
        {
            var log = new StringBuilder();
            DespejarBocas("Assets/Arte/Mundo/Zonas/T1388_Uniones.prefab", null, log);
            DespejarBocas("Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab", new[] { "T1385_", "T1394_", "T1401_" }, log);
            System.IO.File.WriteAllText("Logs/despejar-bocas.txt", log.ToString());
        }

        private static void DespejarBocas(string ruta, string[] soloBajo, StringBuilder log)
        {
            var raiz = PrefabUtility.LoadPrefabContents(ruta);
            foreach (var col in raiz.GetComponentsInChildren<Collider>())
            {
                var b = col.bounds;
                if (b.max.y < 1f || b.min.y > 3f) continue;
                if (soloBajo != null)
                {
                    bool nuevo = false;
                    for (var p = col.transform; p != null; p = p.parent) foreach (var pre in soloBajo) if (p.name.StartsWith(pre)) nuevo = true;
                    if (!nuevo) continue;
                }
                foreach (var u in ColocarT1358Orillas.Uniones)
                {
                    var r = u.rect;
                    if (b.max.x > r.x && b.min.x < r.z && b.max.z > r.y && b.min.z < r.w)
                    {
                        Transform t = col.transform;
                        if (soloBajo == null) { while (t.parent != null && t.parent != raiz.transform && t.parent.parent != raiz.transform) t = t.parent; }
                        else
                        {
                            var cadena = new System.Collections.Generic.List<Transform>();
                            for (var q = col.transform; q != null && q != raiz.transform; q = q.parent) cadena.Insert(0, q);
                            int iNuevo = cadena.FindIndex(q => System.Array.Exists(soloBajo, pre => q.name.StartsWith(pre)));
                            t = cadena[System.Math.Min(cadena.Count - 1, iNuevo + 2)];
                        }
                        log.AppendLine(u.nombre + "	" + t.name + "	" + b.center);
                        t.gameObject.SetActive(false);
                        break;
                    }
                }
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }
}
