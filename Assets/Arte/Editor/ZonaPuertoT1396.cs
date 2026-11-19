using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1396 (Puerto, ronda 2): corre DESPUES de ZonaPuertoT1387.Aplicar y edita T1357_Puerto.prefab (no rehace la zona con ColocarT1357Puerto).
    /// Idempotente: lo nuevo vive bajo "T1396_Puerto" y en hijos "T1396_Grafiti_*" de cada bloque; lo que reemplaza se renombra "T1396_Reemplazado_*" y se apaga.
    /// También deshace los contenedores con grafiti de T-1387 (T1387_Grafiti_*) y las balizas de grúa de T-1387 (T1387_Puerto/LucesGruas) para rehacerlos.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaPuertoT1396.Aplicar, luego .Validar, y en otra corrida .Fotos
    /// </summary>
    public static class ZonaPuertoT1396
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        public const string Raiz = "T1396_Puerto";
        public const string RutaInforme = "Logs/t1396-aplicar.txt";
        public const string RutaValidar = "Logs/t1396-validar.txt";
        public const string RutaCielo = "Logs/t1396-cielo.txt";

        /// <summary>Variante de PU08: la _a (naranja oxidado con grafiti turquesa y morado), como en T-1387 (combina con K15).</summary>
        public const string Variante = "_a";
        /// <summary>Si la lectura de la textura no decide, el grafiti se supone en local -z (cara larga) cuando el eje largo es x, o en local -x cuando es z.</summary>
        public const bool GrafitiEnMenosLocal = true;
        /// <summary>Tope de luces nuevas (Light) de esta tarea.</summary>
        public const int TopeLuces = 30;

        private const string PrefijoViejo = "T1396_Reemplazado_";
        private const string PrefijoViejo1387 = "T1387_Reemplazado_";
        /// <summary>T-1412: los contenedores reemplazados conservan su nombre "PT01_Contenedor*" (con este sufijo) para que las pruebas de T-1366 cuenten sus filas y pilas completas; quedan apagados.</summary>
        public const string SufijoViejo = "_T1396Reemplazado";
        private const float AnchoCalle = ColocarT1357Puerto.AnchoCalle;
        private const float YPatio = 0f;
        private const float MargenCarro = 0.5f;
        private const float AltoLibreCalle = 4.5f;
        private const float XBarcoCentro = 195f;
        private const float IntensidadNeon = 3f, AlcanceNeon = 5f;
        private const float IntensidadLampara = 60f, AlcanceLampara = 11f, AnguloLampara = 100f;
        private const string RutaLamina = "Assets/Arte/Materiales/Calle18nov/M_T08_LaminaOxidada.mat";
        private static readonly string[] RutasBasura =
        {
            "Assets/Arte/Calle/BS01a_Basura/BS01a_Basura.fbx", "Assets/Arte/Calle/BS01b_Basura/BS01b_Basura.fbx",
            "Assets/Arte/Calle/BS01c_Basura/BS01c_Basura.fbx", "Assets/Arte/Calle/BS01d_Basura/BS01d_Basura.fbx",
        };

        private static readonly StringBuilder informe = new StringBuilder();
        private static readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
        private static int luces;

        // ---------------------------------------------------------------- Medidas y ayudas

        private static Rect Huella(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);

        private static Rect Expandir(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        private static float DistanciaACalles(Vector2 p)
        {
            float m = float.MaxValue;
            foreach (Vector3[] calle in ColocarT1357Puerto.Calles())
            {
                m = Mathf.Min(m, ColocarT1357Puerto.DistanciaACalle(new Vector3(p.x, 0f, p.y), calle));
            }
            return m;
        }

        private static bool InvadeCalle(Rect r, float margen)
        {
            float limite = AnchoCalle * 0.5f + margen;
            for (float x = r.xMin; x <= r.xMax + 0.001f; x += 0.5f)
            {
                for (float z = r.yMin; z <= r.yMax + 0.001f; z += 0.5f)
                {
                    if (DistanciaACalles(new Vector2(Mathf.Min(x, r.xMax), Mathf.Min(z, r.yMax))) < limite)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static GameObject BuscarPrefab(string codigo)
        {
            return AssetDatabase.FindAssets(codigo + " t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(r => Path.GetFileNameWithoutExtension(r) == codigo)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
        }

        private static void Nota(string texto)
        {
            informe.AppendLine(texto);
        }

        private static Bounds Limites(Transform t) => ZonaPuertoT1387.Limites(t);

        /// <summary>Triángulos de un objeto en mundo, para rayos exactos sin física (las paredes grandes tienen pocos vértices).</summary>
        private sealed class Malla
        {
            private readonly List<Vector3> tri = new List<Vector3>();
            public readonly Bounds Caja;

            public Malla(Transform raiz)
            {
                foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(false))
                {
                    Mesh m = mf.sharedMesh;
                    if (m == null || mf.GetComponent<MeshRenderer>() == null)
                    {
                        continue;
                    }
                    Vector3[] v = m.vertices;
                    Matrix4x4 mat = mf.transform.localToWorldMatrix;
                    for (int s = 0; s < m.subMeshCount; s++)
                    {
                        int[] idx = m.GetTriangles(s);
                        for (int i = 0; i + 2 < idx.Length; i += 3)
                        {
                            tri.Add(mat.MultiplyPoint3x4(v[idx[i]]));
                            tri.Add(mat.MultiplyPoint3x4(v[idx[i + 1]]));
                            tri.Add(mat.MultiplyPoint3x4(v[idx[i + 2]]));
                        }
                    }
                }
                Caja = Limites(raiz);
            }

            public int Triangulos => tri.Count / 3;

            public IEnumerable<Vector3> Vertices => tri;

            public bool Rayo(Vector3 o, Vector3 d, out float t)
            {
                float mejor = float.MaxValue;
                for (int i = 0; i < tri.Count; i += 3)
                {
                    Vector3 a = tri[i], e1 = tri[i + 1] - a, e2 = tri[i + 2] - a;
                    Vector3 p = Vector3.Cross(d, e2);
                    float det = Vector3.Dot(e1, p);
                    if (Mathf.Abs(det) < 1e-9f)
                    {
                        continue;
                    }
                    float inv = 1f / det;
                    Vector3 s = o - a;
                    float u = Vector3.Dot(s, p) * inv;
                    if (u < 0f || u > 1f)
                    {
                        continue;
                    }
                    Vector3 q = Vector3.Cross(s, e1);
                    float w = Vector3.Dot(d, q) * inv;
                    if (w < 0f || u + w > 1f)
                    {
                        continue;
                    }
                    float tt = Vector3.Dot(e2, q) * inv;
                    if (tt > 1e-4f && tt < mejor)
                    {
                        mejor = tt;
                    }
                }
                t = mejor;
                return mejor < float.MaxValue;
            }

            /// <summary>Punto de la pared que mira hacia <paramref name="normal"/>, a la altura y lateral de <paramref name="punto"/>.</summary>
            public bool Pared(Vector3 punto, Vector3 normal, out Vector3 golpe)
            {
                Vector3 origen = punto + normal * 25f;
                bool ok = Rayo(origen, -normal, out float t);
                golpe = origen - normal * t;
                return ok;
            }

            /// <summary>Altura de la superficie más alta en (x, z); false si no hay nada.</summary>
            public bool Techo(float x, float z, out float y)
            {
                Vector3 origen = new Vector3(x, Caja.max.y + 3f, z);
                bool ok = Rayo(origen, Vector3.down, out float t);
                y = origen.y - t;
                return ok;
            }
        }

        // ---------------------------------------------------------------- Aplicar

        public static void Aplicar()
        {
            informe.Clear();
            luces = 0;
            Nota("T-1396 Puerto (ronda 2)");
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                int lucesAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                int objetosAntes = raiz.GetComponentsInChildren<Transform>(true).Length;
                Restaurar(raiz.transform);
                Transform nuevo = new GameObject(Raiz).transform;
                nuevo.SetParent(raiz.transform, false);
                KitS2c.Obra o = new KitS2c.Obra { Raiz = nuevo, Azar = new System.Random(13960), Nombre = Raiz };

                Grafiti(raiz.transform, nuevo);
                Bodegas(raiz.transform);
                DetallesBodegas(raiz.transform, nuevo, o);
                Gruas(raiz.transform, nuevo, o);
                Barco(raiz.transform);
                PostesYCables(raiz.transform, nuevo, o);
                PisoDetalle(raiz.transform, nuevo, o);

                KitS2c.Terminar(o);
                foreach (Transform t in nuevo.GetComponentsInChildren<Transform>(true))
                {
                    if (t.GetComponent<Light>() == null)
                    {
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                    }
                }
                int lucesDespues = raiz.GetComponentsInChildren<Light>(true).Length;
                Nota("Luces: " + lucesAntes + " antes, " + lucesDespues + " despues (" + (lucesDespues - lucesAntes) + " nuevas, tope " + TopeLuces + ").");
                Nota("Objetos: " + objetosAntes + " antes, " + raiz.GetComponentsInChildren<Transform>(true).Length + " despues (nuevos bajo " + Raiz + ": " + nuevo.GetComponentsInChildren<Transform>(true).Length + ").");
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[ZonaPuertoT1396] " + informe);
        }

        private static void Restaurar(Transform raiz)
        {
            Transform viejo = raiz.Find(Raiz);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            // T-1387: sus contenedores con grafiti y sus balizas se rehacen aquí (orientación, cobertura y balizas sobre la estructura).
            Transform balizas = raiz.Find("T1387_Puerto/LucesGruas");
            if (balizas != null)
            {
                Nota("Quitadas las balizas de T-1387 (T1387_Puerto/LucesGruas, " + balizas.childCount + " halos en esquinas de la caja, algunos en el aire).");
                Object.DestroyImmediate(balizas.gameObject);
            }
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("T1387_Grafiti_") || x.name.StartsWith("T1396_Grafiti_")).ToList())
            {
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith(PrefijoViejo) || x.name.StartsWith(PrefijoViejo1387 + "PT01_Contenedor")
                || x.name.EndsWith(SufijoViejo) || x.name.EndsWith(ZonaPuertoT1387.SufijoViejo)).ToList())
            {
                if (t.name.EndsWith(SufijoViejo))
                {
                    t.name = t.name.Substring(0, t.name.Length - SufijoViejo.Length);
                }
                else if (t.name.EndsWith(ZonaPuertoT1387.SufijoViejo))
                {
                    t.name = t.name.Substring(0, t.name.Length - ZonaPuertoT1387.SufijoViejo.Length);
                }
                else
                {
                    t.name = t.name.StartsWith(PrefijoViejo) ? t.name.Substring(PrefijoViejo.Length) : t.name.Substring(PrefijoViejo1387.Length);
                }
                t.gameObject.SetActive(true);
            }
        }

        private static void Reemplazar(GameObject go)
        {
            go.name = go.name.StartsWith("PT01_Contenedor") ? go.name + SufijoViejo : PrefijoViejo + go.name;
            go.SetActive(false);
        }

        private static List<Rect> Obstaculos(Transform raiz)
        {
            List<Rect> r = new List<Rect>();
            foreach (string grupo in new[] { "Contenedores", "Bodegas", "Entrada", "Postes", "Patio", "Muelle" })
            {
                foreach (Transform g in raiz)
                {
                    if (g.name != grupo)
                    {
                        continue;
                    }
                    foreach (Transform h in g)
                    {
                        if (!h.gameObject.activeInHierarchy || h.GetComponent<Light>() != null)
                        {
                            continue;
                        }
                        Bounds b = Limites(h);
                        if (b.size.sqrMagnitude > 0.01f && b.size.y > 0.3f)
                        {
                            r.Add(Huella(b));
                        }
                    }
                }
            }
            return r;
        }

        // ---------------------------------------------------------------- 1. Contenedores con grafiti (PU08), grafiti hacia el pasillo

        private static Texture2D Textura(Material m)
        {
            if (m == null)
            {
                return null;
            }
            Texture t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            if (t == null && m.HasProperty("_MainTex"))
            {
                t = m.GetTexture("_MainTex");
            }
            if (t == null)
            {
                return null;
            }
            string ruta = AssetDatabase.GetAssetPath(t);
            if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta))
            {
                return null;
            }
            if (!texturas.TryGetValue(ruta, out Texture2D x))
            {
                x = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!x.LoadImage(File.ReadAllBytes(ruta)))
                {
                    x = null;
                }
                texturas[ruta] = x;
            }
            return x;
        }

        /// <summary>
        /// Lee la textura de PU08 sobre las dos caras largas y decide cuál tiene el grafiti (la de más color saturado).
        /// <paramref name="cara"/> es la normal de esa cara con el modelo sin girar (eje x o z del mundo).
        /// </summary>
        private static bool CaraGrafiti(GameObject prefab, Transform padre, out Vector3 cara, out bool largoEnX, out string nota)
        {
            largoEnX = true;
            cara = Vector3.back;
            nota = "";
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            try
            {
                g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                g.transform.localScale = Vector3.one;
                Bounds b = Limites(g.transform);
                largoEnX = b.size.x >= b.size.z;
                Vector3 corto = largoEnX ? Vector3.forward : Vector3.right;
                double[] suma = new double[2];
                double[] area = new double[2];
                foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh m = mf.sharedMesh;
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (m == null || mr == null)
                    {
                        continue;
                    }
                    Vector3[] v = m.vertices;
                    Vector2[] uv = m.uv;
                    if (uv.Length != v.Length)
                    {
                        continue;
                    }
                    Matrix4x4 mat = mf.transform.localToWorldMatrix;
                    Material[] ms = mr.sharedMaterials;
                    for (int s = 0; s < m.subMeshCount; s++)
                    {
                        Texture2D tex = Textura(s < ms.Length ? ms[s] : null);
                        if (tex == null)
                        {
                            continue;
                        }
                        int[] idx = m.GetTriangles(s);
                        for (int i = 0; i + 2 < idx.Length; i += 3)
                        {
                            Vector3 a = mat.MultiplyPoint3x4(v[idx[i]]), c = mat.MultiplyPoint3x4(v[idx[i + 1]]), d = mat.MultiplyPoint3x4(v[idx[i + 2]]);
                            Vector3 n = Vector3.Cross(c - a, d - a);
                            float doble = n.magnitude;
                            if (doble < 1e-6f || Mathf.Abs(Vector3.Dot(n / doble, corto)) < 0.8f)
                            {
                                continue;
                            }
                            Vector3 centroide = (a + c + d) / 3f;
                            int lado = Vector3.Dot(centroide - b.center, corto) > 0f ? 0 : 1;
                            Vector2 uvc = (uv[idx[i]] + uv[idx[i + 1]] + uv[idx[i + 2]]) / 3f;
                            Color px = tex.GetPixelBilinear(Mathf.Repeat(uvc.x, 1f), Mathf.Repeat(uvc.y, 1f));
                            float sat = Mathf.Max(px.r, px.g, px.b) - Mathf.Min(px.r, px.g, px.b);
                            suma[lado] += sat * doble;
                            area[lado] += doble;
                        }
                    }
                }
                double p0 = area[0] > 0 ? suma[0] / area[0] : 0, p1 = area[1] > 0 ? suma[1] / area[1] : 0;
                if (area[0] <= 0 || area[1] <= 0 || Math.Abs(p0 - p1) < 0.02)
                {
                    cara = GrafitiEnMenosLocal ? -corto : corto;
                    nota = "lectura de textura no concluyente (saturación " + p0.ToString("F3") + " / " + p1.ToString("F3") + "): se supone la cara " + cara;
                    return false;
                }
                cara = p0 > p1 ? corto : -corto;
                nota = "saturación media en +" + (largoEnX ? "z" : "x") + " " + p0.ToString("F3") + ", en -" + (largoEnX ? "z" : "x") + " " + p1.ToString("F3") + ": el grafiti está en " + cara;
                return true;
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        private static float YawPara(Vector3 cara, bool largoEnX, Vector3 deseada)
        {
            for (float yaw = 0f; yaw < 360f; yaw += 90f)
            {
                Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                Vector3 largo = q * (largoEnX ? Vector3.right : Vector3.forward);
                if (Mathf.Abs(largo.x) > 0.9f && Vector3.Dot(q * cara, deseada) > 0.9f)
                {
                    return yaw;
                }
            }
            return 0f;
        }

        private static void Grafiti(Transform raiz, Transform nuevo)
        {
            GameObject prefab = BuscarPrefab("PU08_ContenedorGraffiti" + Variante);
            Transform padre = raiz.Find("Contenedores");
            if (prefab == null || padre == null)
            {
                Nota("FALTA PU08_ContenedorGraffiti" + Variante + " o el grupo Contenedores: sin contenedores con grafiti.");
                return;
            }
            bool leida = CaraGrafiti(prefab, nuevo, out Vector3 cara, out bool largoEnX, out string detalle);
            Nota("PU08 " + Variante + ": eje largo en " + (largoEnX ? "x" : "z") + "; " + detalle + (leida ? "" : " (AVISO: revisar en la foto y cambiar GrafitiEnMenosLocal si sale al reves)"));
            System.Random azar = new System.Random(13961);
            int cambiados = 0, forzados = 0, uniformes = 0;
            foreach (Transform bloque in padre.Cast<Transform>().Where(t => t.name.StartsWith("Bloque_")).ToList())
            {
                List<Transform> conts = bloque.Cast<Transform>().Where(t => t.name.StartsWith("PT01_Contenedor") && t.gameObject.activeSelf).ToList();
                if (conts.Count == 0)
                {
                    continue;
                }
                List<(Transform t, Bounds b)> cajas = conts.Select(t => (t, Limites(t))).ToList();
                float zMin = cajas.Min(c => c.b.center.z), zMax = cajas.Max(c => c.b.center.z);
                Bounds bb = cajas[0].b;
                foreach (var par in cajas)
                {
                    bb.Encapsulate(par.b);
                }
                // Los bloques de la primera columna (pegados al pasillo de K15) y los de la primera fila (frente a la cámara de K15) llevan grafiti en toda su cara norte (la que ve el pasillo).
                bool cercano = bb.center.x < 200f || bb.center.z > -100f;
                foreach ((Transform t, Bounds b) in cajas)
                {
                    bool sur = Mathf.Abs(b.center.z - zMin) < 0.6f, norte = Mathf.Abs(b.center.z - zMax) < 0.6f;
                    bool forzar = norte && cercano;
                    if (!(sur || norte) || !(forzar || azar.NextDouble() < 0.55))
                    {
                        continue;
                    }
                    GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bloque);
                    g.name = "T1396_Grafiti_" + prefab.name;
                    g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    g.transform.localScale = Vector3.one;
                    float yaw = YawPara(cara, largoEnX, norte ? Vector3.forward : Vector3.back);
                    g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    Bounds b0 = Limites(g.transform);
                    float rx = b.size.x / b0.size.x, ry = b.size.y / b0.size.y, rz = b.size.z / b0.size.z;
                    bool giro90 = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > 0.5f;
                    Vector3 e = giro90 ? new Vector3(rz, ry, rx) : new Vector3(rx, ry, rz);
                    if (Mathf.Min(rx, ry, rz) < 0.75f || Mathf.Max(rx, ry, rz) > 1.35f)
                    {
                        e = Vector3.one * Mathf.Min(rx, ry, rz); // nunca sale de la caja del contenedor que reemplaza
                        uniformes++;
                    }
                    g.transform.localScale = e;
                    Bounds n = Limites(g.transform);
                    g.transform.position += new Vector3(b.center.x - n.center.x, b.min.y - n.min.y, b.center.z - n.center.z);
                    foreach (Collider c in g.GetComponentsInChildren<Collider>(true))
                    {
                        c.enabled = false; // la pila conserva su BoxCollider exacto en el Bloque
                    }
                    Reemplazar(t.gameObject);
                    cambiados++;
                    forzados += forzar ? 1 : 0;
                }
                Renderer[] rend = bloque.GetComponentsInChildren<Renderer>(false).Where(r => r.gameObject.activeInHierarchy).ToArray();
                LODGroup lg = bloque.GetComponent<LODGroup>();
                if (lg == null && rend.Length > 0)
                {
                    lg = bloque.gameObject.AddComponent<LODGroup>();
                }
                if (lg != null && rend.Length > 0)
                {
                    lg.SetLODs(new[] { new LOD(0.012f, rend) });
                    lg.RecalculateBounds();
                }
            }
            Nota("Grafiti: " + cambiados + " contenedores cambiados por PU08" + Variante + " (" + forzados + " forzados en la cara norte de la primera columna y la primera fila, que son los que ve K15; el resto 55% de las filas sur y norte); "
                + uniformes + " con escala uniforme (cabe dentro de la caja original). El grafiti de cada pieza mira al norte (+z) en la fila norte y al sur (-z) en la fila sur.");
        }

        // ---------------------------------------------------------------- 6. Bodegas que rozan el arroyo

        private static List<Transform> ListaBodegas(Transform raiz)
        {
            Transform padre = raiz.Find("Bodegas");
            return padre == null ? new List<Transform>()
                : padre.Cast<Transform>().Where(t => (t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")) && t.gameObject.activeInHierarchy).ToList();
        }

        private static void Bodegas(Transform raiz)
        {
            Transform padre = raiz.Find("Bodegas");
            if (padre == null)
            {
                return;
            }
            foreach (Transform b in ListaBodegas(raiz))
            {
                Bounds caja = Limites(b);
                Rect h = Huella(caja);
                if (!InvadeCalle(h, MargenCarro))
                {
                    continue;
                }
                bool norte = caja.center.z > -100f;
                Vector3 dir = norte ? Vector3.forward : Vector3.left;
                // Cuánto cabe moverla hacia atrás (norte: hasta la reja; oeste: hasta el límite del patio) y cuánto hay que comprimirla en profundidad si no basta.
                float cabe = norte ? Mathf.Max(0f, (ColocarT1357Puerto.ZNorte - 0.8f) - caja.max.z) : Mathf.Max(0f, caja.min.x - (ColocarT1357Puerto.X0 + 1f));
                float s = 0f;
                bool encontrada = false;
                for (; s <= 14f; s += 0.25f)
                {
                    float atras = Mathf.Min(s, cabe);
                    Rect r = norte ? Rect.MinMaxRect(h.xMin, h.yMin + s, h.xMax, h.yMax + atras) : Rect.MinMaxRect(h.xMin - atras, h.yMin, h.xMax - s, h.yMax);
                    if (!InvadeCalle(r, MargenCarro))
                    {
                        encontrada = true;
                        break;
                    }
                }
                if (!encontrada)
                {
                    Nota("AVISO: bodega " + b.name + " en " + b.position.ToString("F1") + " roza el arroyo y no hay forma de apartarla ni de comprimirla: se deja.");
                    continue;
                }
                float comprimir = Mathf.Max(0f, s - cabe);
                float profundidad = norte ? caja.size.z : caja.size.x;
                Rect zona = Expandir(h, 1f);
                zona = norte ? Rect.MinMaxRect(zona.xMin, zona.yMin - 6f, zona.xMax, zona.yMax) : Rect.MinMaxRect(zona.xMin, zona.yMin, zona.xMax + 6f, zona.yMax);
                List<Transform> grupo = padre.Cast<Transform>().Where(t => t != b && t.gameObject.activeInHierarchy && !(t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")) && !t.name.StartsWith("Extra_")
                    && zona.Contains(new Vector2(t.position.x, t.position.z))).ToList();
                float atrasReal = Mathf.Min(s, cabe);
                if (comprimir > 0.01f)
                {
                    Vector3 local = Quaternion.Inverse(b.rotation) * dir;
                    bool ejeX = Mathf.Abs(local.x) > Mathf.Abs(local.z);
                    float alineado = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z));
                    if (alineado < 0.98f || profundidad - comprimir < profundidad * 0.7f)
                    {
                        Nota("AVISO: bodega " + b.name + " en " + b.position.ToString("F1") + " necesita comprimirse " + comprimir.ToString("F2") + " m pero no esta alineada a los ejes o pasaria del 30%: se deja.");
                        continue;
                    }
                    Vector3 sc = b.localScale;
                    float factor = (profundidad - comprimir) / profundidad;
                    if (ejeX)
                    {
                        sc.x *= factor;
                    }
                    else
                    {
                        sc.z *= factor;
                    }
                    b.localScale = sc;
                    Bounds c2 = Limites(b);
                    float objetivo = norte ? caja.max.z + atrasReal : caja.min.x - atrasReal;
                    float actual = norte ? c2.max.z : c2.min.x;
                    b.position += dir * (norte ? objetivo - actual : actual - objetivo);
                }
                else
                {
                    b.position += dir * s;
                }
                foreach (Transform t in grupo)
                {
                    t.position += dir * s;
                }
                Nota("Bodega " + b.name + " (" + b.position.ToString("F1") + "): fachada apartada " + s.ToString("F2") + " m del arroyo (" + atrasReal.ToString("F2") + " m moviendola y " + comprimir.ToString("F2")
                    + " m comprimiendo su profundidad de " + profundidad.ToString("F1") + " m), con " + grupo.Count + " detalles.");
            }
        }

        // ---------------------------------------------------------------- 2. Neón, lámparas de sodio, alero de lámina y tinacos en cada bodega

        private static void DetallesBodegas(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            Transform padre = new GameObject("Bodegas_K15").transform;
            padre.SetParent(nuevo, false);
            Material lamina = AssetDatabase.LoadAssetAtPath<Material>(RutaLamina);
            if (lamina == null)
            {
                Nota("FALTA " + RutaLamina + ": los aleros usan un gris liso.");
                lamina = KitS2c.Mat("T1396_LaminaGris", () => KitS2c.Lit(new Color(0.35f, 0.3f, 0.28f), 0.4f));
            }
            GameObject tinaco = BuscarPrefab("C09_Tinaco");
            GameObject foco = KitS2c.Prefab("N01");
            int i = 0, neones = 0, lamparas = 0, viseras = 0, tinacos = 0;
            foreach (Transform b in ListaBodegas(raiz).OrderBy(t => t.position.x).ThenBy(t => t.position.z))
            {
                Malla malla = new Malla(b);
                Bounds caja = malla.Caja;
                bool oeste = caja.center.x < 160f && caja.center.z < -90f;
                Vector3 normal = oeste ? Vector3.right : Vector3.back;
                Vector3 lado = Vector3.Cross(Vector3.up, normal);
                float centroLat = Vector3.Dot(caja.center, lado);
                float frente = oeste ? caja.max.x : -caja.min.z;
                float profundidad = oeste ? caja.size.x : caja.size.z;
                Vector3 PuntoEn(float lat, float y) => caja.center + lado * (lat - centroLat) + normal * (frente - Vector3.Dot(caja.center, normal)) + Vector3.up * (y - caja.center.y);
                bool izquierda = i % 2 == 0;
                Color neon = izquierda ? KitS2c.Magenta : KitS2c.Turquesa;
                string nombre = b.name + "_" + i;

                // Alero de lámina inclinado sobre la cortina.
                const float yAlero = 5.6f, anchoAlero = 8f, fondoAlero = 2.4f, grosor = 0.12f, inclina = 14f;
                if (malla.Pared(PuntoEn(centroLat, yAlero), normal, out Vector3 pw))
                {
                    Quaternion q = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(inclina, 0f, 0f);
                    Vector3 centro = pw - normal * 0.03f + q * new Vector3(0f, -grosor * 0.5f, fondoAlero * 0.5f);
                    GameObject v = KitS2c.Losa(o, padre, "Alero_" + nombre, centro, q, new Vector3(anchoAlero, grosor, fondoAlero), lamina, 2f, false);
                    v.name = "Alero_" + nombre;
                    viseras++;
                }
                else
                {
                    Nota("Bodega " + nombre + ": sin pared a " + yAlero + " m en su centro; sin alero.");
                }

                // Neón de bandera junto a la puerta, a media altura, pegado a la pared.
                float latNeon = centroLat + (izquierda ? 5.8f : -5.8f);
                if (malla.Pared(PuntoEn(latNeon, 3.9f), normal, out Vector3 pn))
                {
                    GameObject go = KitS2c.Pieza(padre, neon == KitS2c.Magenta ? KitS2c.NeonMagenta : KitS2c.NeonTurquesa, Vector3.zero,
                        Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, izquierda ? 32f : -32f, 0f), Vector3.one * 1.5f, false);
                    go.name = "Neon_" + nombre;
                    Bounds bn = Limites(go.transform);
                    float ext = Mathf.Abs(normal.x) > 0.5f ? bn.extents.x : bn.extents.z;
                    go.transform.position += pn + normal * 0.04f - (bn.center - normal * ext);
                    PonerLuz(go.transform, Limites(go.transform).center + normal * 0.5f, LightType.Point, neon, IntensidadNeon, AlcanceNeon, 0f, null, "LuzNeon");
                    neones++;
                }

                // Dos lámparas de sodio sobre la cortina (sobre el alero), con su Spot chico.
                foreach (float dl in new[] { -3.2f, 3.2f })
                {
                    if (foco != null && malla.Pared(PuntoEn(centroLat + dl, 6.5f), normal, out Vector3 pl))
                    {
                        GameObject f = KitS2c.Pieza(padre, "N01", Vector3.zero, Quaternion.LookRotation(-normal), Vector3.one * 1.6f, false);
                        f.name = "LamparaSodio_" + nombre;
                        Bounds bf = Limites(f.transform);
                        float ext = Mathf.Abs(normal.x) > 0.5f ? bf.extents.x : bf.extents.z;
                        f.transform.position += pl + normal * 0.03f - (bf.center - normal * ext);
                        PonerLuz(f.transform, Limites(f.transform).center + normal * 0.6f, LightType.Spot, KitS2c.Sodio, IntensidadLampara, AlcanceLampara, AnguloLampara, normal * 0.7f + Vector3.down, "LuzSodio");
                        lamparas++;
                    }
                }

                // Tinaco sobre el techo (en un punto plano del techo, medido con la malla).
                if (tinaco != null)
                {
                    bool puesto = false;
                    foreach (float frac in new[] { 0.55f, 0.7f })
                    {
                        foreach (float lat in new[] { -9f, 9f, -12f, 12f, -6f, 6f })
                        {
                            Vector3 p = PuntoEn(centroLat + lat, 0f) - normal * profundidad * frac;
                            float[] ys = new float[5];
                            bool plano = true;
                            Vector3[] pruebas = { p, p + lado * 1.1f, p - lado * 1.1f, p + normal * 1.1f, p - normal * 1.1f };
                            for (int k = 0; k < 5 && plano; k++)
                            {
                                plano = malla.Techo(pruebas[k].x, pruebas[k].z, out ys[k]);
                            }
                            if (!plano || ys.Max() - ys.Min() > 0.5f)
                            {
                                continue;
                            }
                            GameObject t = KitS2c.Pieza(padre, "C09_Tinaco", Vector3.zero, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), Vector3.one, false);
                            t.name = "Tinaco_" + nombre;
                            Bounds bt = Limites(t.transform);
                            t.transform.localScale = Vector3.one * (2.6f / Mathf.Max(0.01f, bt.size.y));
                            bt = Limites(t.transform);
                            t.transform.position += new Vector3(p.x - bt.center.x, ys.Min() - 0.02f - bt.min.y, p.z - bt.center.z);
                            tinacos++;
                            puesto = true;
                            break;
                        }
                        if (puesto)
                        {
                            break;
                        }
                    }
                    if (!puesto)
                    {
                        Nota("Bodega " + nombre + ": no hay un punto plano del techo para el tinaco.");
                    }
                }
                i++;
            }
            Nota("Bodegas: " + i + " con detalles K15: " + viseras + " aleros de lámina (M_T08, 14 grados, a 4.9 m o más sobre la calle), " + neones + " neones (magenta y turquesa alternados) con Point Light, "
                + lamparas + " lámparas de sodio N01 con Spot chico sin sombras, " + tinacos + " tinacos C09 sobre el techo.");
        }

        private static void PonerLuz(Transform padre, Vector3 pos, LightType tipo, Color color, float intensidad, float alcance, float angulo, Vector3? mira, string nombre)
        {
            if (luces >= TopeLuces)
            {
                Nota("AVISO: tope de " + TopeLuces + " luces nuevas alcanzado: " + nombre + " sin luz.");
                return;
            }
            Light l = KitS2c.Luz(padre, pos, tipo, color, intensidad, alcance, angulo, mira);
            l.name = nombre;
            luces++;
        }

        // ---------------------------------------------------------------- 3. Grúas: balizas rojas sobre la estructura

        private static void Gruas(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            Transform muelle = raiz.Find("Muelle");
            if (muelle == null)
            {
                return;
            }
            Material caja = KitS2c.Mat("T1396_BalizaRoja", () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", new Color(0.3f, 0.02f, 0.02f));
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(14f, 0.4f, 0.4f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
            Shader shader = Shader.Find("MechaCyberpunk/Mundo/HaloLuz");
            Material halo = shader == null ? null : KitS2c.Mat("T1396_HaloRojo", () =>
            {
                Material m = new Material(shader);
                m.SetColor("_Color", new Color(1f, 0.12f, 0.1f));
                m.SetFloat("_Intensidad", 0.9f);
                m.SetFloat("_Borde", 3.4f);
                m.SetFloat("_Caida", 2f);
                m.SetTexture("_Ruido", KitS2c.TexMuro()); // T-1412: el polvo de la niebla (con textura no cuenta como material liso en SinGreyboxT506)
                return m;
            });
            Transform padre = new GameObject("BalizasGruas").transform;
            padre.SetParent(nuevo, false);
            int gruas = 0, balizas = 0;
            foreach (Transform g in muelle.Cast<Transform>().Where(t => t.name.StartsWith("PU01_Grua") && t.gameObject.activeInHierarchy))
            {
                gruas++;
                Malla m = new Malla(g);
                List<Vector3> vs = m.Vertices.ToList();
                if (vs.Count == 0)
                {
                    continue;
                }
                Bounds b = m.Caja;
                float yBajo = b.min.y + b.size.y * 0.55f;
                List<Vector3> puntos = new List<Vector3>();
                void Agregar(Vector3 p)
                {
                    if (puntos.All(q => Vector3.Distance(p, q) > 3f))
                    {
                        puntos.Add(p);
                    }
                }
                // Las puntas de la estructura (arriba): el vértice más alto de cada cuadrante y los extremos de la pluma y del contrapeso.
                List<Vector3> altos = vs.Where(v => v.y >= yBajo).ToList();
                if (altos.Count == 0)
                {
                    altos = vs;
                }
                Agregar(altos.OrderByDescending(v => v.y).First());
                Agregar(altos.OrderBy(v => v.x).First());
                Agregar(altos.OrderByDescending(v => v.x).First());
                Agregar(altos.OrderBy(v => v.z).First());
                Agregar(altos.OrderByDescending(v => v.z).First());
                foreach (Vector3 vv in altos.OrderByDescending(v => v.y).Take(400))
                {
                    if (puntos.Count >= 7)
                    {
                        break;
                    }
                    Agregar(vv);
                }
                foreach (Vector3 punto in puntos)
                {
                    Vector3 pos = punto + Vector3.up * 0.22f;
                    GameObject baliza = KitS2c.Losa(o, padre, "BalizaRoja", pos, Quaternion.identity, new Vector3(0.45f, 0.45f, 0.45f), caja, 1f, false);
                    if (halo != null)
                    {
                        KitS2c.Halo(o, baliza.transform, pos, pos.y - 2.2f, new Color(1f, 0.12f, 0.1f));
                        Transform h = baliza.transform.GetChild(baliza.transform.childCount - 1);
                        MeshRenderer mr = h.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            mr.sharedMaterial = halo;
                        }
                    }
                    balizas++;
                }
            }
            Nota("Grúas: " + balizas + " balizas rojas (cubo emisivo 0.45 m + halo corto) sobre " + gruas + " grúas, cada una sobre un vértice real de la malla de la grúa (puntas de pluma, contrapeso y cima), no en las esquinas de su caja.");
        }

        // ---------------------------------------------------------------- 3b. Barco carguero a la vista de K15

        private static void Barco(Transform raiz)
        {
            Transform muelle = raiz.Find("Muelle");
            Transform barco = muelle == null ? null : muelle.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("PU02_Barco"));
            if (barco == null)
            {
                Nota("Barco: no hay PU02_Barco en el Muelle.");
                return;
            }
            Bounds b = Limites(barco);
            float dx = XBarcoCentro - b.center.x;
            dx = Mathf.Max(dx, ColocarT1357Puerto.X0 + 8f - b.min.x);
            dx = Mathf.Min(dx, ColocarT1357Puerto.X1 - 8f - b.max.x);
            barco.position += new Vector3(dx, 0f, 0f);
            Bounds d = Limites(barco);
            Nota("Barco: PU02_Barco movido " + dx.ToString("F1") + " m en x (centro en x " + d.center.x.ToString("F0") + "; x " + d.min.x.ToString("F0") + ".." + d.max.x.ToString("F0") + ", z " + d.min.z.ToString("F0") + ".." + d.max.z.ToString("F0")
                + ") para que quede de frente al fondo del pasillo de x = 160 que mira K15 (a z = -255 la linea de vista pasa por x = 173) y no tras las pilas de contenedores de la izquierda. Misma z y y: sigue a 1..3 m del muelle.");
        }

        // ---------------------------------------------------------------- 5. Postes de madera y cables cruzando el pasillo

        private static void PostesYCables(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            Transform padre = new GameObject("PostesCables").transform;
            padre.SetParent(nuevo, false);
            List<Malla> mallasOeste = ListaBodegas(raiz).Where(t => Limites(t).center.x < 160f && Limites(t).center.z < -90f).Select(t => new Malla(t)).ToList();
            List<Vector3> existentes = new List<Vector3>();
            Transform postes = raiz.Find("Postes");
            if (postes != null)
            {
                existentes = postes.Cast<Transform>().Where(t => t.name.StartsWith("Poste_")).Select(t => t.position).ToList();
            }
            // Los huecos entre filas de la primera columna de bloques: el poste va pegado a la orilla del pasillo, junto a un lado del hueco.
            List<ColocarT1357Puerto.Bloque> col = ColocarT1357Puerto.Bloques().Where(b => Mathf.Abs(b.Centro.x - ColocarT1357Puerto.XBloques) < 0.1f).OrderByDescending(b => b.Centro.y).ToList();
            List<float> huecos = new List<float>();
            for (int k = 0; k + 1 < col.Count; k++)
            {
                if (Mathf.Abs(col[k].Centro.y - col[k + 1].Centro.y - (ColocarT1357Puerto.FondoBloque + ColocarT1357Puerto.PasilloZ)) < 0.2f)
                {
                    huecos.Add((col[k].Centro.y + col[k + 1].Centro.y) * 0.5f);
                }
            }
            List<Vector3> puntas = new List<Vector3>();
            int nuevos = 0;
            foreach (float zh in huecos.Take(4))
            {
                Vector3 pie = new Vector3(ColocarT1357Puerto.XBloques - ColocarT1357Puerto.LargoBloque * 0.5f + 1.4f, YPatio, zh + 1.1f);
                Vector3? cercano = existentes.Where(p => new Vector2(p.x - pie.x, p.z - pie.z).magnitude < 3.5f).Select(p => (Vector3?)p).FirstOrDefault();
                if (cercano.HasValue)
                {
                    puntas.Add(new Vector3(cercano.Value.x, YPatio + 6.75f, cercano.Value.z));
                    continue;
                }
                if (luces >= TopeLuces)
                {
                    break;
                }
                puntas.Add(KitS2c.Poste(o, padre, pie, Vector3.left, YPatio, false));
                luces++;
                nuevos++;
            }
            int cables = 0;
            for (int k = 0; k < puntas.Count; k++)
            {
                if (k + 1 < puntas.Count)
                {
                    foreach (float dy in new[] { 0f, -0.55f, 0.4f })
                    {
                        KitS2c.Cable(o, puntas[k] + Vector3.up * dy, puntas[k + 1] + Vector3.up * dy, 0.9f);
                        cables++;
                    }
                }
                // Cruzan el pasillo hasta la pared de la bodega de enfrente (a la altura del techo de la fachada).
                Malla enfrente = mallasOeste.FirstOrDefault(m => puntas[k].z > m.Caja.min.z + 1f && puntas[k].z < m.Caja.max.z - 1f);
                if (enfrente != null && enfrente.Pared(new Vector3(0f, 6.2f, puntas[k].z), Vector3.right, out Vector3 pared))
                {
                    foreach (float dz in new[] { -0.5f, 0f, 0.6f })
                    {
                        Vector3 fin = pared + Vector3.right * 0.08f + Vector3.forward * dz;
                        KitS2c.Cable(o, puntas[k] + Vector3.forward * dz, fin, 1.0f);
                        cables++;
                    }
                }
            }
            Nota("Postes y cables: " + puntas.Count + " postes de madera con lámpara (" + nuevos + " nuevos en los huecos de la primera columna; los demás ya existían a menos de 3.5 m), " + cables + " cables entre ellos y hacia las paredes de las bodegas de enfrente; el punto más bajo de un cable sobre la calle queda a más de " + AltoLibreCalle + " m (se mide en Validar).");
        }

        // ---------------------------------------------------------------- 5b. Tarimas, montacargas, tambos, cajas y basura en el piso

        private static void PisoDetalle(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            Transform padre = new GameObject("PisoDetalle").transform;
            padre.SetParent(nuevo, false);
            (string codigo, float alto, int cuantos)[] tipos =
            {
                ("PU06_Pallets", 1.2f, 18), ("C16_Tambo", 1.0f, 14), ("C17_Huacales", 1.0f, 10), ("PU05_Montacargas", 2.6f, 3),
            };
            List<Rect> obstaculos = Obstaculos(raiz);
            List<Rect> bloques = new List<Rect>();
            Transform cont = raiz.Find("Contenedores");
            if (cont != null)
            {
                foreach (Transform bl in cont.Cast<Transform>().Where(t => t.name.StartsWith("Bloque_") && t.gameObject.activeInHierarchy))
                {
                    bloques.Add(Huella(Limites(bl)));
                }
            }
            bloques = bloques.OrderBy(r => new Vector2(r.center.x - 160f, r.center.y + 100f).magnitude).ToList();
            int total = 0;
            List<string> resumen = new List<string>();
            foreach ((string codigo, float alto, int cuantos) in tipos)
            {
                GameObject prefab = BuscarPrefab(codigo);
                if (prefab == null)
                {
                    Nota("FALTA " + codigo + ": sin ese detalle de piso.");
                    continue;
                }
                int puestos = 0;
                for (int intento = 0; intento < 400 && puestos < cuantos && bloques.Count > 0; intento++)
                {
                    // Pegado a la cara de un bloque (así el hueco de 3.5 m sigue pasando), con preferencia por los más cercanos al pasillo de K15.
                    double u = o.Azar.NextDouble();
                    Rect bl = bloques[Mathf.Min(bloques.Count - 1, (int)(u * u * bloques.Count))];
                    float margen = 1.0f;
                    Rect e = Expandir(bl, margen);
                    int lado = o.Azar.Next(4);
                    float t = o.Rango(0f, 1f);
                    Vector2 p = lado == 0 ? new Vector2(Mathf.Lerp(e.xMin, e.xMax, t), e.yMin) : lado == 1 ? new Vector2(Mathf.Lerp(e.xMin, e.xMax, t), e.yMax)
                        : lado == 2 ? new Vector2(e.xMin, Mathf.Lerp(e.yMin, e.yMax, t)) : new Vector2(e.xMax, Mathf.Lerp(e.yMin, e.yMax, t));
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                    go.name = codigo + "_" + puestos;
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f));
                    go.transform.localScale = Vector3.one;
                    Bounds b = Limites(go.transform);
                    go.transform.localScale = Vector3.one * (alto / Mathf.Max(0.01f, b.size.y));
                    b = Limites(go.transform);
                    go.transform.position += new Vector3(p.x - b.center.x, YPatio - b.min.y, p.y - b.center.z);
                    Rect h = Huella(Limites(go.transform));
                    bool ok = !InvadeCalle(h, 0.3f) && !obstaculos.Any(r => r.Overlaps(Expandir(h, -0.05f)));
                    if (!ok)
                    {
                        Object.DestroyImmediate(go);
                        continue;
                    }
                    foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                    {
                        c.enabled = false; // detalle visual: no estorba a pie ni a los carros
                    }
                    obstaculos.Add(h);
                    puestos++;
                }
                total += puestos;
                resumen.Add(codigo + " " + puestos + "/" + cuantos);
            }
            // Basura BS01 más densa (sin collider), fuera de carriles y de lo que ya hay.
            List<GameObject> modelos = RutasBasura.Select(r => AssetDatabase.LoadAssetAtPath<GameObject>(r)).Where(g => g != null).ToList();
            int basura = 0;
            for (int intento = 0; intento < 600 && basura < 30 && modelos.Count > 0; intento++)
            {
                Vector2 p = new Vector2(o.Rango(ColocarT1357Puerto.X0 + 8f, 312f), o.Rango(-235f, -52f));
                Rect h = Rect.MinMaxRect(p.x - 0.8f, p.y - 0.8f, p.x + 0.8f, p.y + 0.8f);
                if (obstaculos.Any(r => r.Overlaps(h)) || DistanciaACalles(p) < AnchoCalle * 0.5f + 0.8f)
                {
                    continue;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(o.Uno(modelos), padre);
                go.name = "Basura_" + basura;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f));
                go.transform.localScale = Vector3.one;
                Bounds b = Limites(go.transform);
                go.transform.localScale = Vector3.one * (o.Rango(0.5f, 0.9f) / Mathf.Max(0.01f, b.size.y));
                b = Limites(go.transform);
                go.transform.position += new Vector3(p.x - b.center.x, YPatio - b.min.y, p.y - b.center.z);
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
                basura++;
            }
            Nota("Detalle de piso: " + string.Join(", ", resumen) + ", basura BS01 " + basura + "/30 (" + (total + basura) + " piezas, sin collider, pegadas a las caras de los bloques, fuera del arroyo + 0.3 m y sin tocar nada).");
        }

        // ---------------------------------------------------------------- Validar

        public static void Validar()
        {
            StringBuilder v = new StringBuilder("T-1396 validar Puerto\n");
            int problemas = 0;
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform nuevo = raiz.transform.Find(Raiz);
                if (nuevo == null)
                {
                    v.AppendLine("FALLA: no hay " + Raiz + " (corre Aplicar).");
                    problemas++;
                }
                else
                {
                    int nl = nuevo.GetComponentsInChildren<Light>(true).Length;
                    v.AppendLine("Luces nuevas: " + nl + " (tope " + TopeLuces + ")");
                    if (nl > TopeLuces)
                    {
                        v.AppendLine("FALLA: pasa del tope de luces nuevas");
                        problemas++;
                    }
                    foreach (Light l in nuevo.GetComponentsInChildren<Light>(true))
                    {
                        if (l.shadows != LightShadows.None || l.GetComponent<LuzNocturna>() == null)
                        {
                            v.AppendLine("FALLA: luz " + l.name + " con sombras o sin LuzNocturna");
                            problemas++;
                        }
                    }
                    // Detalle de piso: sobre el piso, fuera del arroyo, sin atravesar nada.
                    List<Rect> obst = Obstaculos(raiz.transform).Where(r => true).ToList();
                    Transform piso = nuevo.Find("PisoDetalle");
                    int pisoN = 0;
                    if (piso != null)
                    {
                        foreach (Transform t in piso)
                        {
                            pisoN++;
                            Bounds b = Limites(t);
                            if (Mathf.Abs(b.min.y - YPatio) > 0.05f)
                            {
                                v.AppendLine("FALLA asiento: " + t.name + " base en y " + b.min.y.ToString("F2"));
                                problemas++;
                            }
                            if (!t.name.StartsWith("Basura_") && InvadeCalle(Huella(b), 0.2f))
                            {
                                v.AppendLine("FALLA: " + t.name + " en " + t.position.ToString("F1") + " invade el arroyo");
                                problemas++;
                            }
                            if (!t.name.StartsWith("Basura_") && obst.Any(r => r.Overlaps(Expandir(Huella(b), -0.12f))))
                            {
                                v.AppendLine("FALLA encimado: " + t.name + " en " + t.position.ToString("F1") + " atraviesa un bloque o una bodega");
                                problemas++;
                            }
                        }
                    }
                    v.AppendLine("Detalle de piso: " + pisoN + " piezas");
                    // Balizas: cada una sobre un vértice de grúa (a menos de 0.8 m).
                    Transform bal = nuevo.Find("BalizasGruas");
                    Transform muelle = raiz.transform.Find("Muelle");
                    int balN = 0;
                    if (bal != null && muelle != null)
                    {
                        List<Vector3> vg = new List<Vector3>();
                        foreach (Transform g in muelle.Cast<Transform>().Where(t => t.name.StartsWith("PU01_Grua") && t.gameObject.activeInHierarchy))
                        {
                            vg.AddRange(new Malla(g).Vertices);
                        }
                        foreach (Transform t in bal)
                        {
                            balN++;
                            float d = vg.Count == 0 ? 999f : vg.Min(p => Vector3.Distance(p, t.position));
                            if (d > 0.8f)
                            {
                                v.AppendLine("FALLA flotando: baliza en " + t.position.ToString("F1") + " a " + d.ToString("F1") + " m de la estructura de la grúa");
                                problemas++;
                            }
                        }
                    }
                    v.AppendLine("Balizas: " + balN);
                    // Cables: los que pasan sobre una calle quedan a más de 4.5 m.
                    Transform cables = nuevo.Find("Cables");
                    MeshFilter mfc = cables == null ? null : cables.GetComponent<MeshFilter>();
                    if (mfc != null && mfc.sharedMesh != null)
                    {
                        float minimo = float.MaxValue;
                        foreach (Vector3 p in mfc.sharedMesh.vertices)
                        {
                            Vector3 w = cables.TransformPoint(p);
                            if (DistanciaACalles(new Vector2(w.x, w.z)) < AnchoCalle * 0.5f)
                            {
                                minimo = Mathf.Min(minimo, w.y);
                            }
                        }
                        v.AppendLine("Cable más bajo sobre una calle: " + (minimo == float.MaxValue ? "ninguno" : minimo.ToString("F2") + " m"));
                        if (minimo < AltoLibreCalle)
                        {
                            v.AppendLine("FALLA: un cable cruza a menos de " + AltoLibreCalle + " m");
                            problemas++;
                        }
                    }
                    // Aleros: su borde bajo sobre la calle a 4.5 m o más.
                    Transform det = nuevo.Find("Bodegas_K15");
                    if (det != null)
                    {
                        foreach (Transform t in det.Cast<Transform>().Where(x => x.name.StartsWith("Alero_")))
                        {
                            float bajo = Limites(t).min.y;
                            if (bajo < AltoLibreCalle)
                            {
                                v.AppendLine("FALLA: " + t.name + " baja a " + bajo.ToString("F2") + " m sobre la calle");
                                problemas++;
                            }
                        }
                        foreach (Transform t in det.Cast<Transform>().Where(x => x.name.StartsWith("Tinaco_")))
                        {
                            Bounds bt = Limites(t);
                            Transform bod = ListaBodegas(raiz.transform).FirstOrDefault(b => Huella(Limites(b)).Contains(new Vector2(bt.center.x, bt.center.z)));
                            if (bod == null)
                            {
                                v.AppendLine("FALLA: " + t.name + " fuera de toda bodega");
                                problemas++;
                            }
                            else if (new Malla(bod).Techo(bt.center.x, bt.center.z, out float y) && Mathf.Abs(y - bt.min.y) > 0.3f)
                            {
                                v.AppendLine("FALLA: " + t.name + " base en y " + bt.min.y.ToString("F2") + " y el techo en " + y.ToString("F2"));
                                problemas++;
                            }
                        }
                    }
                }
                // Grafiti: sin encimar y sobre el piso o sobre otro contenedor.
                Transform cont = raiz.transform.Find("Contenedores");
                int graf = 0;
                if (cont != null)
                {
                    foreach (Transform bloque in cont.Cast<Transform>().Where(t => t.name.StartsWith("Bloque_")))
                    {
                        List<Transform> activos = bloque.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("PT01_Contenedor") || t.name.StartsWith("T1396_Grafiti_"))).ToList();
                        foreach (Transform g in activos.Where(t => t.name.StartsWith("T1396_Grafiti_")))
                        {
                            graf++;
                            Bounds a = Limites(g);
                            Bounds interior = a;
                            interior.Expand(-0.3f);
                            foreach (Transform otro in activos.Where(t => t != g))
                            {
                                if (interior.Intersects(Limites(otro)))
                                {
                                    v.AppendLine("FALLA encimado: " + bloque.name + "/" + g.name + " con " + otro.name);
                                    problemas++;
                                }
                            }
                            bool apoyado = Mathf.Abs(a.min.y - YPatio) < 0.05f || activos.Any(o2 => o2 != g && Mathf.Abs(Limites(o2).max.y - a.min.y) < 0.15f && Huella(Limites(o2)).Overlaps(Huella(a)));
                            if (!apoyado)
                            {
                                v.AppendLine("FALLA flotando: " + bloque.name + "/" + g.name + " base en y " + a.min.y.ToString("F2"));
                                problemas++;
                            }
                        }
                    }
                }
                v.AppendLine("Contenedores con grafiti: " + graf);
                if (graf == 0)
                {
                    v.AppendLine("FALLA: no hay contenedores con grafiti");
                    problemas++;
                }
                // Bodegas fuera del arroyo (con el margen de carro).
                foreach (Transform b in ListaBodegas(raiz.transform))
                {
                    if (InvadeCalle(Huella(Limites(b)), 0f))
                    {
                        v.AppendLine("FALLA: bodega " + b.name + " en " + b.position.ToString("F1") + " sobre el arroyo");
                        problemas++;
                    }
                    else if (InvadeCalle(Huella(Limites(b)), MargenCarro))
                    {
                        v.AppendLine("AVISO: bodega " + b.name + " en " + b.position.ToString("F1") + " a menos de " + MargenCarro + " m del arroyo");
                    }
                }
                Transform barco = raiz.transform.Find("Muelle") == null ? null : raiz.transform.Find("Muelle").Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("PU02_Barco"));
                if (barco != null)
                {
                    Bounds bb = Limites(barco);
                    v.AppendLine("Barco: x " + bb.min.x.ToString("F0") + ".." + bb.max.x.ToString("F0") + ", z " + bb.min.z.ToString("F0") + ".." + bb.max.z.ToString("F0"));
                    if (bb.min.x < ColocarT1357Puerto.X0 || bb.max.x > ColocarT1357Puerto.X1)
                    {
                        v.AppendLine("FALLA: el barco se sale del muelle");
                        problemas++;
                    }
                }
                v.AppendLine("Luces totales: " + raiz.GetComponentsInChildren<Light>(true).Length + "; objetos: " + raiz.GetComponentsInChildren<Transform>(true).Length);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            v.AppendLine(problemas == 0 ? "OK: sin hallazgos." : "HALLAZGOS: " + problemas);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaValidar, v.ToString());
            Debug.Log("[ZonaPuertoT1396] " + v);
        }

        // ---------------------------------------------------------------- 4 y 7. Fotos (no guardan nada; en una corrida aparte de Aplicar)

        public static readonly Vector3 CamaraK15 = new Vector3(158.6f, 2.3f, -84f);
        public static readonly Vector3 MiraK15 = new Vector3(172f, 24f, -255f);
        public static readonly Vector3 CamaraA15 = new Vector3(326f, 66f, -60f);
        public static readonly Vector3 MiraA15 = new Vector3(240f, 0f, -205f);

        /// <summary>
        /// El domo de nubes (CieloNubes) sigue a la cámara en el juego (LateUpdate), pero en el editor ColocarS2cLook.Foto no lo mueve: se quedaba donde estuviera en la escena
        /// y su borde inferior (un arco) cruzaba el cielo de la foto del Puerto. Aquí se pone en la cámara de la foto, como en el juego.
        /// </summary>
        private static void PonerDomoEn(Vector3 desde)
        {
            GameObject marca = new GameObject("MarcaDomoT1396");
            Camera cam = marca.AddComponent<Camera>();
            cam.enabled = false;
            marca.transform.position = desde;
            foreach (CieloNubes domo in Object.FindObjectsByType<CieloNubes>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                domo.Colocar(cam, ColocarS2cLook.HoraFoto);
            }
            Object.DestroyImmediate(marca);
        }

        /// <summary>Lista lo muy grande que hay en la escena (el arco del cielo debe ser el domo, el skyline o un plano de mar) y a cuánto está de la cámara de K15.</summary>
        private static void DiagnosticoCielo(Vector3 desde)
        {
            StringBuilder s = new StringBuilder("T-1396 diagnóstico del arco del cielo (renderers de más de 250 m)\ncámara K15 en " + desde.ToString("F1") + ", far clip 600 m\n");
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Bounds b = r.bounds;
                if (b.size.magnitude < 250f)
                {
                    continue;
                }
                string ruta = r.name;
                for (Transform t = r.transform.parent; t != null; t = t.parent)
                {
                    ruta = t.name + "/" + ruta;
                }
                bool dentro = b.Contains(desde);
                s.AppendLine(ruta + " centro " + b.center.ToString("F0") + " tamaño " + b.size.ToString("F0") + " distancia " + Vector3.Distance(b.ClosestPoint(desde), desde).ToString("F0") + (dentro ? " (la cámara está dentro de su caja)" : "")
                    + " material " + (r.sharedMaterial == null ? "-" : r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaCielo, s.ToString());
        }

        public static void Fotos()
        {
            Directory.CreateDirectory("docs/capturas/t-1396");
            ColocarS2.Abrir();
            DiagnosticoCielo(CamaraK15);
            PonerDomoEn(CamaraK15);
            ColocarS2cLook.Foto(CamaraK15, MiraK15, "docs/capturas/t-1396/puerto-juego-k15.png", 55f);
            ColocarS2.LadoALado("imageassets/referencias/look/K15_Puerto.png", "docs/capturas/t-1396/puerto-juego-k15.png", "docs/capturas/t-1396/puerto-k15.png");
            PonerDomoEn(CamaraA15);
            ColocarS2cLook.Foto(CamaraA15, MiraA15, "docs/capturas/t-1396/puerto-juego-a15.png", 62f);
            ColocarS2.LadoALado("imageassets/referencias/look/A15_Puerto_Aerea.png", "docs/capturas/t-1396/puerto-juego-a15.png", "docs/capturas/t-1396/puerto-a15.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }
    }
}
