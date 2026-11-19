using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1403: caza de los "fantasmas" de la foto K13 del Cerro (cintas negras enormes en diagonal y un triángulo blanco en el cielo).
    /// Diagnosticar lista TODOS los renderers activos dentro del frustum de K13 a menos de 60 m (y aparte los sospechosos hasta 400 m) en Logs/t1403-diagnostico.txt.
    /// Aplicar apaga (renderer.enabled = false, sin borrar) los sospechosos que viven en los prefabs T1355_Cerro o T1388_Uniones (menos calles, muros y techos);
    /// los de la escena o de un barrio solo se anotan. Se puede deshacer: la lista queda en Logs/t1403-apagados.txt y Aplicar vuelve a encender lo de la
    /// corrida anterior antes de evaluar. Fotos saca la foto K13 en docs/capturas/t-1403/cerro-k13.png (corrida aparte).
    /// Orden: Diagnosticar (solo mira), Aplicar, Fotos (otra corrida de Unity).
    /// </summary>
    public static class ZonaCerroT1403
    {
        public const string RutaDiagnostico = "Logs/t1403-diagnostico.txt";
        public const string RutaApagados = "Logs/t1403-apagados.txt";
        public const string RutaInforme = "Logs/t1403-aplicar.txt";
        public const string RutaCerro = ZonaCerroT1385.RutaPrefab;
        public const string RutaUniones = UnionesT1388.RutaPrefab;

        public static readonly Vector3 K13Desde = ZonaCerroT1394.K13Desde;
        public static readonly Vector3 K13Hacia = ZonaCerroT1394.K13Hacia;
        public const float CampoFoto = ZonaCerroT1394.CampoFoto;

        private const float Cerca = 60f;
        private const float Lejos = 400f;
        private const float AristaLarga = 4f;
        private const float DelgadezMin = 0.02f;
        private const float BoundsLargo = 15f;
        private const float BoundsFino = 0.3f;
        private const float EscalaMin = 0.01f;
        private const float EscalaMax = 50f;
        private const int TopeTriangulos = 400000;

        private static StringBuilder informe;

        private sealed class Hallazgo
        {
            public Renderer R;
            public string Ruta, Malla, Materiales, Razones, TrianguloMalo;
            public Bounds Caja;
            public int Triangulos, Malos;
            public float AristaMax, DelgadezPeor, Distancia;
            public bool Sospechoso;
        }

        // ================================================================ Diagnosticar

        [MenuItem("Mecha Cyberpunk/T1403/Cerro fantasmas: diagnosticar")]
        public static void Diagnosticar()
        {
            informe = new StringBuilder();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            List<Hallazgo> todos = Analizar(out Vector3 camara);
            EscribirDiagnostico(todos, camara);
            Debug.Log("[ZonaCerroT1403] Diagnóstico escrito en " + RutaDiagnostico + " (" + todos.Count + " renderers, " + todos.Count(h => h.Sospechoso) + " sospechosos).");
        }

        private static List<Hallazgo> Analizar(out Vector3 posCamara)
        {
            Vector3 corrimiento = CamaraLibre(K13Desde, K13Hacia);
            posCamara = K13Desde + corrimiento;
            GameObject go = new GameObject("CamaraT1403");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = Lejos;
            cam.fieldOfView = CampoFoto;
            cam.aspect = 1376f / 768f;
            cam.transform.position = posCamara;
            cam.transform.LookAt(K13Hacia + corrimiento);
            Plane[] planos = GeometryUtility.CalculateFrustumPlanes(cam);
            Object.DestroyImmediate(go);

            List<Hallazgo> lista = new List<Hallazgo>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (!GeometryUtility.TestPlanesAABB(planos, b))
                {
                    continue;
                }
                float dist = Mathf.Sqrt(b.SqrDistance(posCamara));
                if (dist > Lejos)
                {
                    continue;
                }
                lista.Add(Evaluar(r, b, dist));
            }
            lista.Sort((a, b) => a.Distancia.CompareTo(b.Distancia));
            return lista;
        }

        private static Hallazgo Evaluar(Renderer r, Bounds b, float dist)
        {
            Hallazgo h = new Hallazgo { R = r, Caja = b, Distancia = dist, Ruta = RutaCompleta(r.transform), DelgadezPeor = -1f };
            List<string> razones = new List<string>();
            Mesh m = null;
            if (r is SkinnedMeshRenderer sk)
            {
                m = sk.sharedMesh;
            }
            else
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                m = mf != null ? mf.sharedMesh : null;
            }
            h.Malla = m != null ? m.name + " (" + AssetDatabase.GetAssetPath(m) + ")" : "(sin malla: " + r.GetType().Name + ")";
            h.Materiales = string.Join(", ", r.sharedMaterials.Select(x => x != null ? x.name + "[" + (x.shader != null ? x.shader.name : "?") + "]" : "null"));
            if (m != null)
            {
                Triangulos(r, m, h);
                if (h.Malos > 0)
                {
                    razones.Add("triángulos largos y delgados (" + h.Malos + " con arista > " + AristaLarga + " m y delgadez < " + DelgadezMin + ")");
                }
            }
            Vector3 s = r.transform.lossyScale;
            foreach (float e in new[] { Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z) })
            {
                if (e > 0f && (e < EscalaMin || e > EscalaMax))
                {
                    razones.Add("escala local rara " + s.ToString("0.####"));
                    break;
                }
            }
            float[] d = { b.size.x, b.size.y, b.size.z };
            Array.Sort(d);
            bool nombreCable = h.Ruta.IndexOf("cable", StringComparison.OrdinalIgnoreCase) >= 0;
            if (d[2] > BoundsLargo && d[0] < BoundsFino && !(nombreCable && d[2] <= 40f && h.Malos == 0))
            {
                razones.Add("bounds de cinta " + d[2].ToString("0.0") + " x " + d[1].ToString("0.0") + " x " + d[0].ToString("0.00") + " m");
            }
            h.Razones = string.Join("; ", razones);
            h.Sospechoso = razones.Count > 0;
            return h;
        }

        private static void Triangulos(Renderer r, Mesh m, Hallazgo h)
        {
            Matrix4x4 mat = r.localToWorldMatrix;
            Vector3[] v = m.vertices;
            int total = 0;
            int malos = 0;
            float arMax = 0f;
            float peor = float.MaxValue;
            string ejemplo = null;
            for (int sm = 0; sm < m.subMeshCount; sm++)
            {
                int[] t = m.GetTriangles(sm);
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    total++;
                    if (total > TopeTriangulos)
                    {
                        break;
                    }
                    Vector3 a = mat.MultiplyPoint3x4(v[t[i]]);
                    Vector3 b = mat.MultiplyPoint3x4(v[t[i + 1]]);
                    Vector3 c = mat.MultiplyPoint3x4(v[t[i + 2]]);
                    float ab = (a - b).magnitude;
                    float bc = (b - c).magnitude;
                    float ca = (c - a).magnitude;
                    float mayor = Mathf.Max(ab, Mathf.Max(bc, ca));
                    if (float.IsNaN(mayor) || float.IsInfinity(mayor))
                    {
                        malos++;
                        continue;
                    }
                    arMax = Mathf.Max(arMax, mayor);
                    if (mayor > AristaLarga)
                    {
                        float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                        float delg = area / (mayor * mayor);
                        if (delg < peor)
                        {
                            peor = delg;
                        }
                        if (delg < DelgadezMin)
                        {
                            malos++;
                            if (malos <= 3)
                            {
                                ejemplo = (ejemplo ?? string.Empty) + "[" + a.ToString("0.0") + " " + b.ToString("0.0") + " " + c.ToString("0.0") + " arista " + mayor.ToString("0.0") + "] ";
                            }
                        }
                    }
                }
            }
            h.Triangulos = total;
            h.Malos = malos;
            h.AristaMax = arMax;
            h.DelgadezPeor = peor == float.MaxValue ? -1f : peor;
            h.TrianguloMalo = ejemplo;
        }

        private static void EscribirDiagnostico(List<Hallazgo> todos, Vector3 camara)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1403 diagnóstico de K13. Cámara en " + camara + ", mira a " + K13Hacia + ", campo " + CampoFoto + ".");
            sb.AppendLine("SOSPECHOSO: triángulos con arista > " + AristaLarga + " m y delgadez < " + DelgadezMin + ", o bounds > " + BoundsLargo + " m con una dimensión < " + BoundsFino + " m, o escala < " + EscalaMin + " / > " + EscalaMax + ".");
            sb.AppendLine("Renderers activos en el frustum: " + todos.Count + " (a menos de " + Cerca + " m: " + todos.Count(h => h.Distancia <= Cerca) + "). Sospechosos: " + todos.Count(h => h.Sospechoso) + ".");
            sb.AppendLine();
            sb.AppendLine("=== SOSPECHOSOS (hasta " + Lejos + " m, de cerca a lejos) ===");
            foreach (Hallazgo h in todos.Where(x => x.Sospechoso))
            {
                Linea(sb, h);
            }
            sb.AppendLine();
            sb.AppendLine("=== TODOS LOS RENDERERS A MENOS DE " + Cerca + " m ===");
            foreach (Hallazgo h in todos.Where(x => x.Distancia <= Cerca))
            {
                Linea(sb, h);
            }
            sb.AppendLine();
            sb.AppendLine("=== OBJETOS DE CIELO / NUBES EN LA ESCENA (el triángulo blanco puede ser un domo o capa de cielo creada al tomar la foto) ===");
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && (mb.GetType().Name.Contains("Cielo") || mb.GetType().Name.Contains("Nube")))
                {
                    sb.AppendLine(mb.GetType().Name + " en " + RutaCompleta(mb.transform) + " activo=" + mb.gameObject.activeInHierarchy);
                }
            }
            Escribir(RutaDiagnostico, sb);
        }

        private static void Linea(StringBuilder sb, Hallazgo h)
        {
            sb.AppendLine((h.Sospechoso ? "SOSPECHOSO " : "") + h.Distancia.ToString("0.0", CultureInfo.InvariantCulture) + " m | " + h.Ruta);
            sb.AppendLine("    malla: " + h.Malla + " | material: " + h.Materiales);
            sb.AppendLine("    bounds centro " + h.Caja.center.ToString("0.0") + " tamaño " + h.Caja.size.ToString("0.00") + " | triángulos " + h.Triangulos + " | arista mayor "
                + h.AristaMax.ToString("0.00", CultureInfo.InvariantCulture) + " m | delgadez peor " + (h.DelgadezPeor < 0f ? "-" : h.DelgadezPeor.ToString("0.0000", CultureInfo.InvariantCulture))
                + " | escala " + h.R.transform.lossyScale.ToString("0.###"));
            if (h.Sospechoso)
            {
                sb.AppendLine("    por qué: " + h.Razones + (string.IsNullOrEmpty(h.TrianguloMalo) ? string.Empty : " | triángulos: " + h.TrianguloMalo));
            }
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1403/Cerro fantasmas: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1403: apagar sospechosos de K13\n");
            int vueltos = Restaurar();
            informe.AppendLine("Vueltos a encender de la corrida anterior: " + vueltos + ".");
            AssetDatabase.Refresh();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            List<Hallazgo> todos = Analizar(out Vector3 camara);
            List<Hallazgo> sospechosos = todos.Where(h => h.Sospechoso).ToList();
            EscribirDiagnostico(todos, camara);
            informe.AppendLine("Sospechosos en el frustum: " + sospechosos.Count + ".");

            Dictionary<string, List<string>> porPrefab = new Dictionary<string, List<string>>();
            foreach (Hallazgo h in sospechosos)
            {
                string prefab = PrefabDe(h.R.transform, out string indices, out string rutaRel);
                if (prefab == null)
                {
                    informe.AppendLine("SOLO ANOTADO (escena o barrio, no es T1355/T1388): " + h.Distancia.ToString("0.0") + " m " + h.Ruta + " | " + h.Razones);
                    continue;
                }
                if (EsEstructura(rutaRel))
                {
                    informe.AppendLine("SOLO ANOTADO (calle, muro o techo): " + h.Ruta + " | " + h.Razones);
                    continue;
                }
                if (h.Malos > 0 && h.Triangulos > 2000 && h.Malos < h.Triangulos / 2)
                {
                    informe.AppendLine("SOLO ANOTADO (malla combinada de " + h.Triangulos + " triángulos con solo " + h.Malos + " malos; apagarla quitaría todo lo demás): " + h.Ruta + " | " + h.TrianguloMalo);
                    continue;
                }
                if (!porPrefab.TryGetValue(prefab, out List<string> l))
                {
                    l = new List<string>();
                    porPrefab[prefab] = l;
                }
                l.Add(indices + "|" + h.R.gameObject.name);
                informe.AppendLine("APAGADO en " + prefab + ": " + rutaRel + " (" + h.Distancia.ToString("0.0") + " m) | " + h.Razones);
            }

            StringBuilder lista = new StringBuilder();
            foreach (KeyValuePair<string, List<string>> kv in porPrefab)
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(kv.Key);
                try
                {
                    foreach (string entrada in kv.Value.Distinct())
                    {
                        string[] p = entrada.Split('|');
                        Transform t = PorIndices(raiz.transform, p[0]);
                        Renderer r = t != null && t.name == p[1] ? t.GetComponent<Renderer>() : null;
                        if (r == null)
                        {
                            informe.AppendLine("AVISO: no se encontró " + entrada + " en " + kv.Key + " (¿es un objeto agregado en la escena?).");
                            continue;
                        }
                        r.enabled = false;
                        lista.AppendLine(kv.Key + "|" + entrada);
                    }
                    PrefabUtility.SaveAsPrefabAsset(raiz, kv.Key);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            Escribir(RutaApagados, lista);
            Escribir(RutaInforme, informe);
            AssetDatabase.SaveAssets();
            Debug.Log("[ZonaCerroT1403] Aplicar listo. Lista en " + RutaApagados + ", informe en " + RutaInforme + ".");
        }

        private static int Restaurar()
        {
            string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, RutaApagados);
            if (!File.Exists(ruta))
            {
                return 0;
            }
            int n = 0;
            IEnumerable<IGrouping<string, string>> grupos = File.ReadAllLines(ruta).Where(x => x.Split('|').Length == 3).GroupBy(x => x.Split('|')[0]);
            foreach (IGrouping<string, string> g in grupos)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(g.Key) == null)
                {
                    continue;
                }
                GameObject raiz = PrefabUtility.LoadPrefabContents(g.Key);
                try
                {
                    foreach (string linea in g)
                    {
                        string[] p = linea.Split('|');
                        Transform t = PorIndices(raiz.transform, p[1]);
                        Renderer r = t != null && t.name == p[2] ? t.GetComponent<Renderer>() : null;
                        if (r != null)
                        {
                            r.enabled = true;
                            n++;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(raiz, g.Key);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            return n;
        }

        /// <summary>Sube por los padres hasta una instancia de T1355_Cerro o T1388_Uniones. Devuelve la ruta del prefab, los índices de hijo y la ruta por nombres bajo su raíz.</summary>
        private static string PrefabDe(Transform t, out string indices, out string rutaRel)
        {
            indices = null;
            rutaRel = null;
            GameObject actual = t.gameObject;
            while (actual != null)
            {
                GameObject raiz = PrefabUtility.GetNearestPrefabInstanceRoot(actual);
                if (raiz == null)
                {
                    return null;
                }
                Object fuente = PrefabUtility.GetCorrespondingObjectFromSource(raiz);
                string ruta = fuente != null ? AssetDatabase.GetAssetPath(fuente) : null;
                if (ruta == RutaCerro || ruta == RutaUniones)
                {
                    List<int> idx = new List<int>();
                    List<string> nombres = new List<string>();
                    for (Transform x = t; x != null && x != raiz.transform; x = x.parent)
                    {
                        idx.Insert(0, x.GetSiblingIndex());
                        nombres.Insert(0, x.name);
                    }
                    indices = string.Join("/", idx);
                    rutaRel = string.Join("/", nombres);
                    return ruta;
                }
                Transform padre = raiz.transform.parent;
                actual = padre != null ? padre.gameObject : null;
            }
            return null;
        }

        private static Transform PorIndices(Transform raiz, string indices)
        {
            Transform t = raiz;
            if (string.IsNullOrEmpty(indices))
            {
                return t;
            }
            foreach (string s in indices.Split('/'))
            {
                if (!int.TryParse(s, out int i) || i < 0 || i >= t.childCount)
                {
                    return null;
                }
                t = t.GetChild(i);
            }
            return t;
        }

        private static bool EsEstructura(string rutaRel)
        {
            string[] claves = { "calle", "muro", "wall", "techo", "roof", "banqueta", "terreno", "piso", "losa", "suelo", "asfalto" };
            string baja = rutaRel.ToLowerInvariant();
            return claves.Any(baja.Contains);
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T1403/Cerro fantasmas: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1403");
            Physics.SyncTransforms();
            Vector3 corrimiento = CamaraLibre(K13Desde, K13Hacia);
            Debug.Log("[ZonaCerroT1403] Cámara K13 corrida " + corrimiento + " desde " + K13Desde + ".");
            ColocarS2cLook.Foto(K13Desde + corrimiento, K13Hacia + corrimiento, "docs/capturas/t-1403/cerro-k13-juego.png", CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/t-1403/cerro-k13-juego.png", "docs/capturas/t-1403/cerro-k13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        // ================================================================ Ayudas

        private static Vector3 CamaraLibre(Vector3 desde, Vector3 hacia)
        {
            Vector3 dir = (hacia - desde).normalized;
            Vector3[] corrimientos =
            {
                Vector3.zero, new Vector3(1.5f, 0f, 0f), new Vector3(-1.5f, 0f, 0f), new Vector3(0f, 0f, -5f), new Vector3(1.5f, 0f, -5f),
                new Vector3(0f, 0f, 5f), new Vector3(1.5f, 0f, 5f), new Vector3(0f, 0f, -10f), new Vector3(1.5f, 0f, -10f), new Vector3(0f, 0f, 10f),
            };
            foreach (Vector3 o in corrimientos)
            {
                Vector3 p = desde + o;
                bool tapada = Physics.CheckSphere(p, 0.8f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(p, dir, 6f, ~0, QueryTriggerInteraction.Ignore);
                bool suelo = Physics.Raycast(p, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                if (!tapada && suelo)
                {
                    return o;
                }
            }
            return Vector3.zero;
        }

        private static string RutaCompleta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        private static void Escribir(string ruta, StringBuilder sb)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllText(completa, sb.ToString());
        }
    }
}
