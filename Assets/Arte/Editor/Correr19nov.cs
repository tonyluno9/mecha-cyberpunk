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
            DespejarBocas("Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab", new[] { "T1386_", "T1395_", "T1402_", "T1404_" }, log);
            DespejarBocas("Assets/Arte/Mundo/Zonas/T1354_Malecon.prefab", new[] { "T1384_", "T1393_", "T1400_", "T1404_" }, log);
            DespejarBocas("Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab", new[] { "T1387_", "T1396_", "T1402_" }, log);
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

        /// <summary>Qué renderers atraviesa el rayo de la cámara K11 por la aguja oscura (viewport x 0.19, y 0.35..0.75).</summary>
        public static void RayoK11()
        {
            var escena = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity");
            var go = new GameObject("CamRayo"); var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f; cam.aspect = 1376f / 768f; cam.farClipPlane = 1100f;
            go.transform.position = new Vector3(40f, 1.95f, -235.6f); go.transform.LookAt(new Vector3(-59f, 7f, -221.7f));
            var log = new StringBuilder();
            foreach (float vy in new[] { 0.4f, 0.55f, 0.7f })
            {
                var rayo = cam.ViewportPointToRay(new Vector3(0.19f, vy, 0f));
                var lista = new System.Collections.Generic.List<(float, string)>();
                foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!r.enabled) continue;
                    if (r.bounds.IntersectRay(rayo, out float d)) lista.Add((d, r.transform.GetHierarchyPath() + " | " + (r.sharedMaterial ? r.sharedMaterial.name + "[" + r.sharedMaterial.shader.name + "]" : "-") + " | " + r.bounds.size));
                }
                lista.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                log.AppendLine("vy " + vy);
                foreach (var e in lista.GetRange(0, System.Math.Min(12, lista.Count))) log.AppendLine($"  {e.Item1:0} m  {e.Item2}");
            }
            UnityEngine.Object.DestroyImmediate(go);
            System.IO.File.WriteAllText("Logs/rayo-k11.txt", log.ToString());
        }


        /// <summary>El panel pintado T513 Tramo_3 (fondo-suburbios, 560 m de alto a 700 m) se asoma sobre el mar en K11 como una aguja oscura: se apaga, como T-1358 apagó los del sur y del este.</summary>
        public static void ApagarFondoTramo3()
        {
            var escena = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity");
            var raiz = System.Array.Find(escena.GetRootGameObjects(), g => g.name == "T513_Fondo");
            var t = raiz == null ? null : raiz.transform.Find("Tramo_3_fondo-suburbios");
            if (t != null) { t.gameObject.SetActive(false); Debug.Log("Correr19nov: apagado T513_Fondo/Tramo_3_fondo-suburbios"); }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(escena);
        }


        /// <summary>E16_b (T-1408) tiene los mismos 52 huesos que E16_a, pero el mapeo automático de Unity no halla la cabeza: se copia el humanDescription de E16_a.</summary>
        public static void CopiarAvatarE16()
        {
            var a = AssetImporter.GetAtPath("Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_a.fbx") as ModelImporter;
            var b = AssetImporter.GetAtPath("Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_b.fbx") as ModelImporter;
            if (a == null || b == null) { Debug.LogWarning("Correr19nov: falta E16 a o b"); return; }
            b.animationType = ModelImporterAnimationType.Human;
            b.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var hd = b.humanDescription;
            hd.human = a.humanDescription.human;
            b.humanDescription = hd;
            Debug.Log("Correr19nov: E16_b esqueleto " + (hd.skeleton == null ? 0 : hd.skeleton.Length) + " huesos");
            b.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(b.assetPath);
            foreach (var o in avatar) if (o is Avatar av) Debug.Log("Correr19nov: E16_b avatar valido=" + av.isValid + " humano=" + av.isHuman);
        }


        /// <summary>Los FBX de drones (T-1397) traen texturas embebidas que no se extrajeron: se extraen y los materiales pasan a externos con su textura.</summary>
        public static void TexturasDrones()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Arte/Vehiculos/Drones" }))
            {
                var ruta = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (imp == null) continue;
                var dir = System.IO.Path.GetDirectoryName(ruta).Replace(System.IO.Path.DirectorySeparatorChar, '/');
                var tex = dir + "/Texturas";
                if (!AssetDatabase.IsValidFolder(tex)) AssetDatabase.CreateFolder(dir, "Texturas");
                bool ok = imp.ExtractTextures(tex);
                AssetDatabase.Refresh();
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                imp.materialLocation = ModelImporterMaterialLocation.External;
                imp.materialName = ModelImporterMaterialName.BasedOnMaterialName;
                imp.materialSearch = ModelImporterMaterialSearch.Local;
                imp.SaveAndReimport();
                int con = 0, total = 0;
                foreach (var g2 in AssetDatabase.FindAssets("t:Material", new[] { dir }))
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g2));
                    total++;
                    if (m.shader.name != "Universal Render Pipeline/Lit")
                    {
                        var t = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                        m.shader = Shader.Find("Universal Render Pipeline/Lit");
                        if (t != null) m.SetTexture("_BaseMap", t);
                    }
                    if (m.GetTexture("_BaseMap") != null) con++;
                    EditorUtility.SetDirty(m);
                }
                Debug.Log("Correr19nov: drones " + ruta + " texturas extraidas=" + ok + " materiales " + con + "/" + total + " con textura");
            }
            AssetDatabase.SaveAssets();
        }


        /// <summary>Material URP Lit con la textura de color (y normal si hay) para cada dron de T-1397, puesto en todos los renderers de su prefab Dron_*.</summary>
        public static void MaterialesDrones()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var pref in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Arte/Vehiculos/Drones/Prefabs" }))
            {
                var rutaPref = AssetDatabase.GUIDToAssetPath(pref);
                var codigo = System.IO.Path.GetFileNameWithoutExtension(rutaPref).Replace("Dron_", "");
                var dirTex = "Assets/Arte/Vehiculos/Drones/" + codigo + "/Texturas";
                Texture2D color = null, normal = null;
                foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { dirTex }))
                {
                    var rt = AssetDatabase.GUIDToAssetPath(g);
                    var n = System.IO.Path.GetFileNameWithoutExtension(rt);
                    if (n.EndsWith("_0_3")) { var ti = AssetImporter.GetAtPath(rt) as TextureImporter; if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); } normal = AssetDatabase.LoadAssetAtPath<Texture2D>(rt); }
                    else if (n.EndsWith("_0_0") || n.StartsWith("atlas") || color == null && !n.Contains("roughness") && !n.Contains("metallic")) color = AssetDatabase.LoadAssetAtPath<Texture2D>(rt);
                }
                if (color == null) { Debug.LogWarning("Correr19nov: sin textura de color para " + codigo); continue; }
                var rutaMat = "Assets/Arte/Vehiculos/Drones/" + codigo + "/M_" + codigo + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
                if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, rutaMat); }
                mat.shader = lit;
                mat.SetTexture("_BaseMap", color);
                mat.SetColor("_BaseColor", Color.white);
                if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
                mat.SetFloat("_Smoothness", 0.45f);
                EditorUtility.SetDirty(mat);
                var raiz = PrefabUtility.LoadPrefabContents(rutaPref);
                int n2 = 0;
                foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer || r.name.StartsWith("LuzNavegacion") || r.name.Contains("Halo")) continue;
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats; n2++;
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPref);
                PrefabUtility.UnloadPrefabContents(raiz);
                Debug.Log("Correr19nov: dron " + codigo + " con " + color.name + " en " + n2 + " renderers");
            }
            AssetDatabase.SaveAssets();
        }


        /// <summary>Diagnóstico de E16_b: arma el avatar humano con AvatarBuilder y el mapeo de E16_a, y escribe por qué falla.</summary>
        public static void DiagnosticoE16()
        {
            var log = new StringBuilder();
            var a = AssetImporter.GetAtPath("Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_a.fbx") as ModelImporter;
            var modeloB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_b.fbx");
            var go = (GameObject)UnityEngine.Object.Instantiate(modeloB);
            var hd = a.humanDescription;
            var nombres = new System.Collections.Generic.HashSet<string>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) nombres.Add(t.name);
            foreach (var h in hd.human) if (!nombres.Contains(h.boneName)) log.AppendLine("FALTA hueso " + h.humanName + " -> " + h.boneName);
            var esq = new System.Collections.Generic.List<SkeletonBone>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) esq.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            hd.skeleton = esq.ToArray();
            var av = AvatarBuilder.BuildHumanAvatar(go, hd);
            log.AppendLine("BuildHumanAvatar: valido=" + av.isValid + " humano=" + av.isHuman);
            if (av.isValid && av.isHuman)
            {
                var imp = AssetImporter.GetAtPath("Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_b.fbx") as ModelImporter;
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                var hdSinEsq = hd; hdSinEsq.skeleton = new SkeletonBone[0];
                imp.humanDescription = hdSinEsq;
                imp.SaveAndReimport();
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(imp.assetPath)) if (o is Avatar x) log.AppendLine("Importado sin esqueleto fijo: valido=" + x.isValid + " humano=" + x.isHuman);
            }
            UnityEngine.Object.DestroyImmediate(go);
            System.IO.File.WriteAllText("Logs/e16-diagnostico.txt", log.ToString());
        }

        private static string GetHierarchyPath(this Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    }
}
