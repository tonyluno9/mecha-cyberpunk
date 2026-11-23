using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1682 (T38 + E3): banquetas con la textura T02_Banqueta y bordillo de concreto; charcos con la forma de T11_Charco (mismo shader y reflejo).
    /// Batchmode (Unity cerrado, una corrida por metodo):
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.BanquetasT1682.Diagnostico | Aplicar | Fotos
    /// </summary>
    public static class BanquetasT1682
    {
        public const string RutaEscena = CallesTexturasT1642.RutaEscena;
        public const string RutaLog = "Logs/t1682.txt";
        public const string CarpetaFotos = "docs/capturas/_tareas/T38";
        public const string Etiqueta = "[T-1682] ";

        // Una cara hacia arriba entre estas alturas (m, mundo) es piso de banqueta; abajo de AltoCalle es la calle.
        public const float BanquetaMin = 0.06f;
        public const float BanquetaMax = 0.45f;
        public const float AltoCalle = 0.05f;
        public const float AreaMinima = 2f; // m2 de cara de banqueta para contar un renderer

        public static readonly string[] PalabrasAsfalto = { "asfalt", "asphalt", "road", "calle_", "arroyo" };

        // ------------------------------------------------------------------ geometria

        public struct CaraBanqueta
        {
            public Renderer Renderer;
            public int Submalla;
            public Material Material;
            public float Area;      // m2 de caras hacia arriba a altura de banqueta
            public float AreaCalle; // m2 de caras hacia arriba a altura de calle (en la misma submalla)
            public float MetrosPorUv; // metros del mundo por 1 unidad de UV (sin tiling)
        }

        /// <summary>Submallas de la escena con caras de banqueta (horizontales, a 0.06-0.45 m), con su area y su escala de UV.</summary>
        public static List<CaraBanqueta> CarasBanqueta(Scene escena)
        {
            var lista = new List<CaraBanqueta>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    if (r.gameObject.layer == Ambiente.ReflejoCharcos.CapaCharcos) continue;
                    Bounds b = r.bounds;
                    if (b.min.y > BanquetaMax || b.max.y < BanquetaMin) continue;
                    if (EsInterior(r.transform)) continue;
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    Material m0 = r.sharedMaterial;
                    if (m0 != null && m0.HasProperty("_Surface") && m0.GetFloat("_Surface") > 0.5f) continue;
                    Medir(r, mf.sharedMesh, lista);
                }
            }
            return lista;
        }

        private static readonly List<Vector3> vs = new List<Vector3>();
        private static readonly List<Vector2> uvs = new List<Vector2>();
        private static readonly List<int> tris = new List<int>();

        private static void Medir(MeshRenderer r, Mesh malla, List<CaraBanqueta> lista)
        {
            try
            {
                malla.GetVertices(vs);
                malla.GetUVs(0, uvs);
            }
            catch (Exception)
            {
                return;
            }
            bool hayUv = uvs.Count == vs.Count;
            Matrix4x4 l2w = r.transform.localToWorldMatrix;
            var mundo = new Vector3[vs.Count];
            for (int i = 0; i < vs.Count; i++) mundo[i] = l2w.MultiplyPoint3x4(vs[i]);
            Material[] mats = r.sharedMaterials;
            for (int s = 0; s < malla.subMeshCount && s < mats.Length; s++)
            {
                if (malla.GetTopology(s) != MeshTopology.Triangles) continue;
                malla.GetTriangles(tris, s);
                float area = 0f, areaCalle = 0f, areaUv = 0f, areaMundoUv = 0f;
                for (int t = 0; t + 2 < tris.Count; t += 3)
                {
                    Vector3 a = mundo[tris[t]], bb = mundo[tris[t + 1]], c = mundo[tris[t + 2]];
                    Vector3 cruz = Vector3.Cross(bb - a, c - a);
                    float doble = cruz.magnitude;
                    if (doble < 1e-6f || cruz.y / doble < 0.95f) continue;
                    float ar = doble * 0.5f;
                    float y = (a.y + bb.y + c.y) / 3f;
                    if (y >= BanquetaMin && y <= BanquetaMax)
                    {
                        area += ar;
                        if (hayUv)
                        {
                            Vector2 ua = uvs[tris[t]], ub = uvs[tris[t + 1]], uc = uvs[tris[t + 2]];
                            float auv = Mathf.Abs((ub.x - ua.x) * (uc.y - ua.y) - (uc.x - ua.x) * (ub.y - ua.y)) * 0.5f;
                            if (auv > 1e-9f)
                            {
                                areaUv += auv;
                                areaMundoUv += ar;
                            }
                        }
                    }
                    else if (y < AltoCalle && y > -0.6f)
                    {
                        areaCalle += ar;
                    }
                }
                if (area < AreaMinima) continue;
                lista.Add(new CaraBanqueta
                {
                    Renderer = r,
                    Submalla = s,
                    Material = mats[s],
                    Area = area,
                    AreaCalle = areaCalle,
                    MetrosPorUv = areaUv > 1e-9f ? Mathf.Sqrt(areaMundoUv / areaUv) : 0f,
                });
            }
        }

        private static bool EsInterior(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                string n = p.name;
                if (n == "Interior" || n.StartsWith("Interior_", StringComparison.Ordinal) || n == "Revestimiento") return true;
            }
            return false;
        }

        public static bool EsAsfalto(Material m)
        {
            if (m == null) return false;
            string tex = m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "";
            return CallesTexturasT1642.Contiene(m.name + "|" + tex, PalabrasAsfalto);
        }

        // ------------------------------------------------------------------ diagnostico

        [MenuItem("Mecha Cyberpunk/T-1682/Diagnostico")]
        public static void Diagnostico()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, Informe(escena));
            Debug.Log(Etiqueta + "diagnostico en " + RutaLog);
        }

        public static string Informe(Scene escena)
        {
            var sb = new StringBuilder();
            sb.AppendLine("T-1682 diagnostico " + DateTime.Now);
            var caras = CarasBanqueta(escena);
            sb.AppendLine("=== CARAS DE BANQUETA (y " + BanquetaMin + "-" + BanquetaMax + "): " + caras.Count + " submallas, " + caras.Sum(c => c.Area).ToString("0") + " m2 ===");
            foreach (var g in caras.GroupBy(c => c.Material).OrderByDescending(g => g.Sum(c => c.Area)))
            {
                Material m = g.Key;
                string tex = m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "-";
                Vector2 til = m != null && m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap") : Vector2.one;
                sb.AppendLine(g.Count() + "x " + g.Sum(c => c.Area).ToString("0") + " m2 mat=" + (m != null ? m.name : "null") + " tex=" + tex + " tiling=" + til.ToString("0.00")
                    + " asfalto=" + EsAsfalto(m) + " ruta=" + (m != null ? AssetDatabase.GetAssetPath(m) : ""));
                foreach (var c in g.OrderByDescending(c => c.Area).Take(12))
                {
                    MeshFilter mf = c.Renderer.GetComponent<MeshFilter>();
                    sb.AppendLine("     " + CallesTexturasT1642.Ruta(c.Renderer.transform) + " sub=" + c.Submalla + "/" + c.Renderer.sharedMaterials.Length + " area=" + c.Area.ToString("0.0")
                        + " areaCalle=" + c.AreaCalle.ToString("0.0") + " m/uv=" + c.MetrosPorUv.ToString("0.00") + " centro=" + c.Renderer.bounds.center.ToString("0.0")
                        + " tam=" + c.Renderer.bounds.size.ToString("0.0") + " malla=" + (mf.sharedMesh != null ? mf.sharedMesh.name : "?")
                        + " prefab=" + (PrefabUtility.IsPartOfPrefabInstance(c.Renderer) ? AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(c.Renderer.gameObject)) : "-"));
                }
            }
            sb.AppendLine("=== CHARCOS: materiales y mascaras ===");
            foreach (var g in CallesTexturasT1642.CharcosQueSuenan(escena).GroupBy(r => r.sharedMaterial))
            {
                Material m = g.Key;
                Texture mas = m != null && m.HasProperty("_Mascara") ? m.GetTexture("_Mascara") : null;
                sb.AppendLine(g.Count() + "x mat=" + (m != null ? m.name : "null") + " mascara=" + (mas != null ? AssetDatabase.GetAssetPath(mas) : "-")
                    + " st=" + (m != null && m.HasProperty("_Mascara") ? m.GetTextureScale("_Mascara") + "/" + m.GetTextureOffset("_Mascara") : ""));
            }
            sb.AppendLine("=== BORDILLOS T1642: " + (GameObject.Find(CallesTexturasT1642.NombreRaizBordillos) != null ? GameObject.Find(CallesTexturasT1642.NombreRaizBordillos).transform.childCount : 0));
            GameObject nuevos = GameObject.Find(NombreRaizBordillos);
            sb.AppendLine("=== BORDILLOS T1682: " + (nuevos != null ? nuevos.transform.childCount : 0));
            sb.AppendLine("=== BANQUETAS POR NOMBRE: " + BanquetasPorNombre(escena).Count + ", sin T02: "
                + string.Join(", ", BanquetasPorNombre(escena).Where(r => !TieneT02(r.sharedMaterial)).Select(r => CallesTexturasT1642.Ruta(r.transform)).Take(30)));
            return sb.ToString();
        }

        // ------------------------------------------------------------------ reglas para las pruebas

        public const string RutaTexturaBanqueta = "Assets/Arte/Calle/Banqueta/T02_Banqueta.png";
        public const string CarpetaCharcos = "Assets/Arte/Calle/Charcos";
        public const string PrefijoMascaraT11 = "T11_Charco_Mascara_";
        public const string CarpetaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        public const string NombreRaizBordillos = "T1682_Bordillos";
        public const float MetrosPorRepeticion = 2.4f; // T02 trae 3x3 losas: losa de 0.8 m
        public const float MetrosPorRepeticionBordillo = 0.5f;

        /// <summary>Renderers activos de afuera cuyo nombre dice banqueta/acera/sidewalk.</summary>
        public static List<MeshRenderer> BanquetasPorNombre(Scene escena)
        {
            var lista = new List<MeshRenderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!r.enabled || EsInterior(r.transform)) continue;
                    if (CallesTexturasT1642.Contiene(r.name, new[] { "banqueta", "acera", "sidewalk" }) && !r.name.StartsWith("Puesto", StringComparison.Ordinal) && !r.name.StartsWith("Bordillo", StringComparison.Ordinal)) lista.Add(r);
                }
            }
            return lista;
        }

        public static bool TieneT02(Material m) => m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null && m.GetTexture("_BaseMap").name == "T02_Banqueta";

        /// <summary>Caras de banqueta con material de asfalto que no son una calle (arroyo) a nivel de banqueta.</summary>
        public static List<CaraBanqueta> BanquetasConAsfalto(Scene escena)
        {
            return CarasBanqueta(escena).Where(c => EsAsfalto(c.Material) && !EsCalle(c.Renderer)).ToList();
        }

        public static bool EsCalle(Renderer r) => CallesTexturasT1642.Contiene(r.name, new[] { "arroyo", "asfalto", "calle" });

        public static bool MascaraT11(Material m)
        {
            Texture t = m != null && m.HasProperty("_Mascara") ? m.GetTexture("_Mascara") : null;
            return t != null && t.name.StartsWith(PrefijoMascaraT11, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------ aplicar

        [MenuItem("Mecha Cyberpunk/T-1682/Aplicar")]
        public static void Aplicar()
        {
            var sb = new StringBuilder();
            sb.AppendLine("T-1682 Aplicar " + DateTime.Now);
            Importar(sb);
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();

            // 1) Banquetas: T02 con tiling a 0.8 m por losa segun la escala de UV de cada malla.
            Texture2D t02 = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaBanqueta);
            var porMalla = CarasBanqueta(escena).Where(c => c.Submalla == 0).ToDictionary(c => c.Renderer, c => c);
            int nb = 0;
            foreach (MeshRenderer r in BanquetasPorNombre(escena))
            {
                float mPorUv = porMalla.TryGetValue(r, out CaraBanqueta c) && c.MetrosPorUv > 0f ? c.MetrosPorUv : MedirMetrosPorUv(r);
                if (mPorUv <= 0f)
                {
                    sb.AppendLine("SIN UV " + CallesTexturasT1642.Ruta(r.transform));
                    continue;
                }
                Material m = MaterialBanqueta(t02, mPorUv);
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                nb++;
                sb.AppendLine("banqueta " + CallesTexturasT1642.Ruta(r.transform) + " m/uv=" + mPorUv.ToString("0.00") + " -> " + m.name);
            }
            sb.AppendLine("banquetas con T02=" + nb);

            // 2) Bordillos: los de la calle de la casa (T-1641) pasan a concreto; los que faltan en las banquetas S2c junto a un arroyo.
            Material t09 = AssetDatabase.LoadAssetAtPath<Material>(CallesTexturasT1642.RutaMatBordillo);
            foreach (string ruta in new[] { "T1641_CalleCasa/Bordillo_Sur", "T1641_CalleCasa/Bordillo_Norte" })
            {
                GameObject go = GameObject.Find(ruta);
                if (go == null) continue;
                Renderer r = go.GetComponent<Renderer>();
                r.sharedMaterial = MaterialBordillo(t09, r.bounds.size.x > r.bounds.size.z ? r.bounds.size.x : r.bounds.size.z);
                EditorUtility.SetDirty(r);
                sb.AppendLine("bordillo T1641 " + ruta + " -> " + r.sharedMaterial.name);
            }
            foreach (GameObject viejo in escena.GetRootGameObjects().Where(g => g.name == NombreRaizBordillos).ToArray())
            {
                Object.DestroyImmediate(viejo);
            }
            Physics.SyncTransforms();
            var raiz = new GameObject(NombreRaizBordillos);
            int nbo = 0;
            foreach (MeshRenderer r in BanquetasPorNombre(escena))
            {
                if (r.name == "Banqueta" && r.transform.parent != null && r.transform.parent.name.StartsWith("Manzana_", StringComparison.Ordinal)) continue; // ya los tiene T-1642
                Bounds b = r.bounds;
                var lados = new (string, Vector3, Vector3, Vector3)[]
                {
                    ("N", new Vector3(b.min.x, 0f, b.max.z), new Vector3(b.max.x, 0f, b.max.z), Vector3.forward),
                    ("S", new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.min.z), Vector3.back),
                    ("E", new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.max.z), Vector3.right),
                    ("O", new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z), Vector3.left),
                };
                foreach (var (lado, a, z, fuera) in lados)
                {
                    float largo = Vector3.Distance(a, z);
                    if (largo < 3f || !DaACalle(a, z, fuera, b.max.y)) continue;
                    var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubo.name = "Bordillo_" + r.transform.root.name + "_" + lado + "_" + nbo;
                    cubo.transform.SetParent(raiz.transform, false);
                    float abajo = b.min.y, arriba = b.max.y + 0.04f;
                    cubo.transform.position = (a + z) * 0.5f + Vector3.up * ((abajo + arriba) * 0.5f) - fuera * (0.25f * 0.5f - 0.02f);
                    cubo.transform.rotation = Quaternion.FromToRotation(Vector3.right, (z - a).normalized);
                    cubo.transform.localScale = new Vector3(largo, arriba - abajo, 0.25f);
                    Object.DestroyImmediate(cubo.GetComponent<Collider>());
                    cubo.GetComponent<Renderer>().sharedMaterial = MaterialBordillo(t09, largo);
                    nbo++;
                    sb.AppendLine("bordillo nuevo " + cubo.name + " largo=" + largo.ToString("0.0"));
                }
            }
            sb.AppendLine("bordillos nuevos=" + nbo);

            // 3) Charcos: la forma de T11 en la mascara de los 4 materiales de agua de T-1503 (mismo shader, mismo reflejo).
            for (int i = 0; i < 4; i++)
            {
                Material agua = AssetDatabase.LoadAssetAtPath<Material>(AguaT1503.CarpetaMateriales + "/" + AguaT1503.PrefijoMaterial + "S2c_Charco_" + i + ".mat");
                Texture2D mascara = AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaCharcos + "/" + PrefijoMascaraT11 + i + ".png");
                if (agua == null || mascara == null)
                {
                    sb.AppendLine("FALTA agua o mascara " + i);
                    continue;
                }
                agua.SetTexture("_Mascara", mascara);
                EditorUtility.SetDirty(agua);
                sb.AppendLine("charco " + agua.name + " -> " + mascara.name);
            }
            var charcos = CallesTexturasT1642.CharcosQueSuenan(escena);
            sb.AppendLine("charcos=" + charcos.Count + " con T11=" + charcos.Count(c => MascaraT11(c.sharedMaterial)));

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            sb.AppendLine("banquetas con asfalto=" + BanquetasConAsfalto(escena).Count + " banquetas sin T02=" + BanquetasPorNombre(escena).Count(r => !TieneT02(r.sharedMaterial)));
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1682-aplicar.txt", sb.ToString());
            Debug.Log(Etiqueta + sb);
        }

        /// <summary>True si afuera de ese lado (a 0.8 m) el piso es una calle mas baja que la banqueta (rayos en 3 puntos).</summary>
        private static bool DaACalle(Vector3 a, Vector3 z, Vector3 fuera, float alto)
        {
            int calle = 0;
            foreach (float t in new[] { 0.2f, 0.5f, 0.8f })
            {
                Vector3 p = Vector3.Lerp(a, z, t) + fuera * 0.8f;
                if (!Physics.Raycast(new Vector3(p.x, alto + 1.5f, p.z), Vector3.down, out RaycastHit h, 3f, ~(1 << Ambiente.ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore)) continue;
                if (h.point.y > alto - 0.03f) continue;
                Renderer r = h.collider.GetComponent<Renderer>();
                if (EsCalle(h.collider) || (r != null && EsAsfalto(r.sharedMaterial))) calle++;
            }
            return calle >= 2;
        }

        private static bool EsCalle(Collider c) => CallesTexturasT1642.Contiene(c.name, new[] { "arroyo", "asfalto", "calle" });

        private static float MedirMetrosPorUv(MeshRenderer r)
        {
            var l = new List<CaraBanqueta>();
            Medir(r, r.GetComponent<MeshFilter>().sharedMesh, l);
            return l.Count > 0 ? l[0].MetrosPorUv : 0f;
        }

        private static Material MaterialBanqueta(Texture2D t02, float metrosPorUv)
        {
            float tiling = metrosPorUv / MetrosPorRepeticion;
            string ruta = CarpetaMateriales + "/T1682_Banqueta_" + tiling.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', '_') + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetTexture("_BaseMap", t02);
            m.SetTexture("_MainTex", t02);
            m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            m.SetTextureScale("_MainTex", new Vector2(tiling, tiling));
            m.SetColor("_BaseColor", new Color(0.82f, 0.82f, 0.84f, 1f));
            m.SetFloat("_Smoothness", 0.18f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material MaterialBordillo(Material t09, float largo)
        {
            int metros = Mathf.Max(1, Mathf.RoundToInt(largo));
            string ruta = CarpetaMateriales + "/T1682_Bordillo_" + metros + "m.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(t09);
                AssetDatabase.CreateAsset(m, ruta);
            }
            var tiling = new Vector2(metros / MetrosPorRepeticionBordillo, 1f);
            m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_BumpMap")) m.SetTextureScale("_BumpMap", tiling);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Importacion: T02 color con repeticion y mipmaps; mascaras T11 lineales y sin repetir (como las de S2c).</summary>
        public static void Importar(StringBuilder sb)
        {
            AssetDatabase.Refresh();
            var ti = AssetImporter.GetAtPath(RutaTexturaBanqueta) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.anisoLevel = 8;
                ti.filterMode = FilterMode.Trilinear;
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
                sb.AppendLine("importada " + RutaTexturaBanqueta);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D " + PrefijoMascaraT11, new[] { CarpetaCharcos }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var mi = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (mi == null) continue;
                mi.textureType = TextureImporterType.Default;
                mi.sRGBTexture = false;
                mi.mipmapEnabled = true;
                mi.wrapMode = TextureWrapMode.Clamp;
                mi.alphaSource = TextureImporterAlphaSource.FromInput;
                mi.maxTextureSize = 512;
                mi.SaveAndReimport();
                sb.AppendLine("importada " + ruta);
            }
        }

        // ------------------------------------------------------------------ fotos

        /// <summary>Fotos en una corrida de Unity APARTE de Aplicar. T1682_SUF=antes|despues (por defecto despues).</summary>
        [MenuItem("Mecha Cyberpunk/T-1682/Fotos")]
        public static void Fotos()
        {
            string suf = Environment.GetEnvironmentVariable("T1682_SUF");
            if (string.IsNullOrEmpty(suf)) suf = "despues";
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Directory.CreateDirectory(CarpetaFotos);
            Foto("1_casa_" + suf, new Vector3(-55.5f, 1.7f, -84f), new Vector3(-55.5f, 0.2f, -68f), 70f);
            Foto("2_casa_bordillo_" + suf, new Vector3(-50f, 1.5f, -80f), new Vector3(-55f, 0.1f, -75f), 70f);
            Foto("3_suburbios_" + suf, new Vector3(-115.3f, 1.7f, -80f), new Vector3(-115.3f, 0.2f, -62f), 70f);
            Foto("4_nova_" + suf, new Vector3(70f, 1.7f, -37f), new Vector3(86f, 0.2f, -36f), 70f);
            Foto("5_mercado_" + suf, new Vector3(-38.5f, 1.7f, 12f), new Vector3(-38.5f, 0.2f, 28f), 70f);
            Foto("6_centro_" + suf, new Vector3(-38.5f, 1.7f, -12f), new Vector3(-38.5f, 0.2f, -28f), 70f);
            Foto("7_charco_" + suf, new Vector3(-51f, 1.6f, -84f), new Vector3(-51f, 0f, -80f), 65f);
        }

        private static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            const int Ancho = 1280;
            const int Alto = 720;
            var go = new GameObject("CamFotosT1682");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 800f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            var datos = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = (float)Ancho / Alto;
            Ambiente.ReflejoCharcos reflejo = Object.FindFirstObjectByType<Ambiente.ReflejoCharcos>();
            if (reflejo != null && reflejo.Actualizar(cam))
            {
                reflejo.Camara.Render();
            }
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            if (reflejo != null) reflejo.Soltar();
            Debug.Log(Etiqueta + "foto " + nombre);
        }
    }
}
