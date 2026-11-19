using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1406: el callejón de K13 en el Cerro. Sobre el prefab T1355_Cerro (sin correr el generador) y bajo la raíz propia "T1406_Cerro" (Aplicar la rehace).
    /// El callejón es la escalera CE01 del z = EscaleraZ (la única ladera de 25 a 40 grados del Cerro: sube 18.75 m en 36 m al oeste, 27.5 grados) con:
    ///  - un pasillo escalonado al sur de la escalera (peldaños de 0.19 m, con collider igual a su malla) y su barandal de tubo (solo visual);
    ///  - una hilera de casas (CC07, CC06 y casas del kit con toldo y cortina) al sur del pasillo, cada una a la altura del pasillo frente a su puerta, con cimiento de concreto hasta el terreno;
    ///  - postes con transformador y lámpara de sodio al norte, cables, un carro viejo al pie, macetas, basura y 5 neones chicos;
    ///  - faroles de sodio extra en las calles de abajo (A13) donde no hay luz en 16 m.
    /// Las casas viejas de la franja del lado sur se APAGAN (SetActive false, reversible: se anotan en T1406_Cerro/Apagados y Aplicar las vuelve a encender antes de rehacer).
    /// Orden: T1385, T1394, T1401, (T1405), T1406.Aplicar; luego T1406.Validar; Fotos en una corrida de Unity aparte.
    /// </summary>
    public static class ZonaCerroT1406
    {
        public const string RutaPrefab = ZonaCerroT1385.RutaPrefab;
        public const string Raiz = "T1406_Cerro";
        public const string RutaInforme = "Logs/t1406-aplicar.txt";
        public const string RutaValidar = "Logs/t1406-validar.txt";
        public const int Semilla = 14060;
        public const long TopeBytesPrefab = 60L * 1024 * 1024;
        public const int TopeLucesNuevas = 40;

        /// <summary>z de la escalera CE01 que sirve de callejón (0: sube 18.75 m en 36 m, 27.5 grados; 60 da 23.6 y 120 da 19.5).</summary>
        public const float EscaleraZ = 0f;
        public const float XArriba = -354f;
        public const float XAbajo = -318f;
        public const float ZPasilloSur = -4.15f;
        public const float ZPasilloNorte = -1.95f;
        public const float ZFachada = -4.25f;
        public const float AlzaPeldano = 0.19f;
        public const float DesnivelMaximo = 0.3f;

        // Variantes (ver T1385): CC07_a trae cortina metálica, tinaco y tendedero; CC06_b es de bloque con escalera exterior.
        public const string Variante06 = "_b";
        public const string Variante07 = "_a";
        public const string RutaCarro = "Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab";
        public static readonly float[] CentrosCasas = { -324f, -331.9f, -339.8f, -347.7f };
        public static readonly string[] TipoCasas = { "CC07", "KIT", "CC06", "KIT" };

        // K13: al pie del callejón (boca de la escalera en la banqueta de la calle base), mirando cuesta arriba al oeste-noroeste.
        public static readonly Vector3 K13Desde = new Vector3(-312.4f, 1.95f, -2.8f);
        public static readonly Vector3 K13Hacia = new Vector3(-338f, 11f, 0.5f);
        // Extra: desde arriba, mirando cuesta abajo (la ciudad queda al frente).
        public static readonly Vector3 K13BajadaDesde = new Vector3(-356.5f, 20.7f, -3.2f);
        public static readonly Vector3 K13BajadaHacia = new Vector3(-300f, 4f, -8f);
        // A13: 47 m sobre la parte alta de la escalera, al oeste, mirando cuesta abajo hacia el este y la ciudad.
        public static readonly Vector3 A13Desde = new Vector3(-388f, 66f, -28f);
        public static readonly Vector3 A13Hacia = new Vector3(-322f, 5f, 4f);
        public const float CampoFoto = 66f;
        private const float DistanciaCable = 4.5f;

        private const int TopeFaroles = 12;
        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[256];
        private static readonly Collider[] bufCol = new Collider[256];
        private static readonly Dictionary<string, Vector3> medidas = new Dictionary<string, Vector3>();
        private static readonly string[] Protegidos = { "Terreno", "Calles", "Escaleras", "Postes", "Cables", "Carros", "Huecos" };

        private sealed class Ctx
        {
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Terreno, Calles, Escaleras, Nuevo, Casas, Pasillo;
            public KitS2c.Obra Obra;
            public Material Concreto, Tubo;
            public float YArriba, YAbajo, Pendiente;
            public readonly List<Vector3> Puntas = new List<Vector3>();
            public readonly List<Vector4> Techos = new List<Vector4>();
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1406/Cerro callejon: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1406: callejón del Cerro (" + RutaPrefab + ")\n");
            medidas.Clear();
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                int devueltas = Restaurar(zona);
                Log("Apagadas por una corrida anterior y vueltas a encender: " + devueltas + ".");
                Ctx c = Abrir(zona);
                int lucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                Log("Luces antes: " + lucesAntes + ". Tamaño del prefab antes: " + Megas(RutaPrefab) + " MB.");
                c.Obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                c.Concreto = AssetDatabase.LoadAssetAtPath<Material>(ZonaCerroT1385.RutaM09) ?? KitS2c.MatBanqueta();
                c.Tubo = KitS2c.Mat("T1406_TuboOxido", () => KitS2c.Lit(new Color(0.45f, 0.22f, 0.13f), 0.3f, KitS2c.TexMuro()));
                Physics.SyncTransforms();
                Paso("medir la escalera", () => Medir(c));
                Paso("apagar casas viejas", () => Apagar(c));
                Paso("pasillo y barandal", () => Pasillo(c));
                Paso("casas", () => Casas(c));
                Paso("postes y cables", () => PostesYCables(c));
                Paso("carro", () => Carro(c));
                Paso("menudo", () => Menudo(c));
                Paso("faroles de las calles de abajo", () => Faroles(c));
                Estatico(c);
                KitS2c.Terminar(c.Obra);
                int luces = zona.GetComponentsInChildren<Light>(true).Length;
                int nuevas = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                Log("Luces después: " + luces + " (nuevas " + nuevas + ", tope " + TopeLucesNuevas + ").");
                if (nuevas > TopeLucesNuevas)
                {
                    Log("AVISO: se pasó del tope de luces nuevas.");
                }
                Physics.SyncTransforms();
                bool ok;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(zona, RutaPrefab, out ok);
                Log(ok && guardado != null ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log("Tamaño del prefab después: " + Megas(RutaPrefab) + " MB (tope 60).");
            }
            catch (Exception e)
            {
                Log("ERROR: " + e);
                Debug.LogError("[ZonaCerroT1406] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
            Escribir(RutaInforme, informe);
        }

        private static void Paso(string nombre, Action accion)
        {
            try
            {
                accion();
            }
            catch (Exception e)
            {
                Log("ERROR en el paso '" + nombre + "': " + e);
                Debug.LogError("[ZonaCerroT1406] " + nombre + ": " + e);
            }
        }

        private static Ctx Abrir(GameObject zona)
        {
            Physics.SyncTransforms();
            return new Ctx
            {
                Zona = zona,
                Fisica = zona.scene.GetPhysicsScene(),
                Terreno = zona.transform.Find("Terreno"),
                Calles = zona.transform.Find("Calles"),
                Escaleras = zona.transform.Find("Escaleras"),
            };
        }

        // ================================================================ Restaurar / apagar (reversible)

        /// <summary>Vuelve a encender lo que una corrida anterior apagó. Cada marca se llama "i/j/k|nombre" (índices de hermano desde la raíz del prefab).</summary>
        private static int Restaurar(GameObject zona)
        {
            Transform raiz = zona.transform.Find(Raiz);
            Transform marcas = raiz == null ? null : raiz.Find("Apagados");
            if (marcas == null)
            {
                return 0;
            }
            int n = 0;
            foreach (Transform m in marcas.Cast<Transform>().ToList())
            {
                string[] partes = m.name.Split('|');
                if (partes.Length < 2)
                {
                    continue;
                }
                Transform t = zona.transform;
                bool valido = true;
                foreach (string s in partes[0].Split('/'))
                {
                    int i;
                    if (!int.TryParse(s, out i) || i < 0 || i >= t.childCount)
                    {
                        valido = false;
                        break;
                    }
                    t = t.GetChild(i);
                }
                if (valido && t.name == partes[1])
                {
                    t.gameObject.SetActive(true);
                    n++;
                }
                else
                {
                    Log("AVISO: no se pudo volver a encender " + m.name + " (la jerarquía cambió).");
                }
            }
            Physics.SyncTransforms();
            return n;
        }

        private static bool EsProtegido(Ctx c, Transform t)
        {
            for (Transform p = t; p != null && p != c.Zona.transform; p = p.parent)
            {
                if (Protegidos.Contains(p.name) || p.name.Contains("Poste") || p.name.Contains("Cable") || p.name.Contains("Ladera") || p.name.StartsWith("CarroViejo"))
                {
                    return true;
                }
            }
            return false;
        }

        private static Transform Unidad(Ctx c, Transform t)
        {
            Transform u = t;
            while (u.parent != null && u.parent != c.Zona.transform)
            {
                Transform p = u.parent;
                if (EsProtegido(c, p))
                {
                    break;
                }
                Bounds bp = Cubierta(p);
                if (bp.size.x > 14f || bp.size.z > 14f || bp.size.y > 30f)
                {
                    break;
                }
                u = p;
            }
            return u;
        }

        private static string Indices(Ctx c, Transform t)
        {
            List<string> l = new List<string>();
            for (Transform p = t; p != null && p != c.Zona.transform; p = p.parent)
            {
                l.Insert(0, p.GetSiblingIndex().ToString(CultureInfo.InvariantCulture));
            }
            return string.Join("/", l);
        }

        /// <summary>Apaga las casas (y cimientos, azoteas y enlaces) que caen en la franja sur del callejón: x de -352 a -320, z de -15 a -4.</summary>
        private static void Apagar(Ctx c)
        {
            const float xMin = -352f, xMax = -320f, zMin = -15f, zMax = -4f;
            HashSet<Transform> unidades = new HashSet<Transform>();
            foreach (Renderer r in c.Zona.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r.transform.IsChildOf(c.Nuevo) || EsProtegido(c, r.transform))
                {
                    continue;
                }
                Vector3 p = r.bounds.center;
                if (p.x < xMin || p.x > xMax || p.z < zMin || p.z > zMax)
                {
                    continue;
                }
                unidades.Add(Unidad(c, r.transform));
            }
            Transform marcas = Hijo(c.Nuevo, "Apagados");
            List<Transform> lista = unidades.Where(u => u != null).OrderBy(u => Indices(c, u), StringComparer.Ordinal).ToList();
            List<string> nombres = lista.Select(u => Indices(c, u) + "|" + u.name).ToList();
            for (int i = 0; i < lista.Count; i++)
            {
                new GameObject(nombres[i]).transform.SetParent(marcas, false);
                lista[i].gameObject.SetActive(false);
                Log("Apagada: " + nombres[i]);
            }
            Physics.SyncTransforms();
            Log("Casas y cosas viejas apagadas en la franja sur: " + lista.Count + " (reversible).");
        }

        // ================================================================ Medir la escalera

        private static bool Piso(Ctx c, float x, float z, bool soloPiso, bool soloEscalera, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, 120f, z), Vector3.down, bufRayos, 400f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = bufRayos[i].collider.transform;
                if (c.Nuevo != null && t.IsChildOf(c.Nuevo))
                {
                    continue;
                }
                if (soloPiso && !EsPiso(c, t))
                {
                    continue;
                }
                if (soloEscalera && !(c.Escaleras != null && t.IsChildOf(c.Escaleras)))
                {
                    continue;
                }
                if (bufRayos[i].point.y > y)
                {
                    y = bufRayos[i].point.y;
                    ok = true;
                }
            }
            return ok;
        }

        private static bool EsPiso(Ctx c, Transform t)
        {
            return (c.Terreno != null && t.IsChildOf(c.Terreno)) || (c.Calles != null && t.IsChildOf(c.Calles));
        }

        private static void Medir(Ctx c)
        {
            foreach (float z in ZonaCerroT1355ZEscaleras())
            {
                float grados = PendienteEn(c, z);
                Log("Escalera CE01 en z = " + z.ToString("0", CultureInfo.InvariantCulture) + ": pendiente medida " + grados.ToString("0.0", CultureInfo.InvariantCulture) + " grados.");
            }
            float g = PendienteEn(c, EscaleraZ);
            c.Pendiente = g;
            if (g < 25f || g > 40f)
            {
                Log("AVISO: la escalera elegida (z = " + EscaleraZ + ") mide " + g.ToString("0.0", CultureInfo.InvariantCulture) + " grados, fuera de 25 a 40.");
            }
            float zMed = (ZPasilloSur + ZPasilloNorte) * 0.5f;
            float yA, yB;
            bool okA = Piso(c, XArriba - 1.2f, zMed, true, false, out yA);
            bool okB = Piso(c, XAbajo + 1.5f, zMed, true, false, out yB);
            // Línea de la escalera medida (para comprobar que las puntas del pasillo caen cerca).
            float lineaA = float.NaN, lineaB = float.NaN;
            float s0, s1;
            if (Piso(c, XArriba + 1f, EscaleraZ, false, true, out s0) && Piso(c, XAbajo - 1f, EscaleraZ, false, true, out s1))
            {
                float pend = (s0 - s1) / (XAbajo - 1f - (XArriba + 1f));
                lineaA = s0 + pend * 1f;
                lineaB = s1 - pend * 1f;
            }
            if (!okA || (!float.IsNaN(lineaA) && Mathf.Abs(yA - lineaA) > 1f))
            {
                Log("AVISO: el piso de arriba (" + (okA ? yA.ToString("0.00", CultureInfo.InvariantCulture) : "sin piso") + ") no cuadra con la escalera (" + lineaA.ToString("0.00", CultureInfo.InvariantCulture) + "); se usa la línea de la escalera.");
                yA = float.IsNaN(lineaA) ? AlturaPierna(EscaleraZ) : lineaA;
            }
            if (!okB || (!float.IsNaN(lineaB) && Mathf.Abs(yB - lineaB) > 1f))
            {
                Log("AVISO: el piso de abajo (" + (okB ? yB.ToString("0.00", CultureInfo.InvariantCulture) : "sin piso") + ") no cuadra con la escalera (" + lineaB.ToString("0.00", CultureInfo.InvariantCulture) + "); se usa la línea de la escalera.");
                yB = float.IsNaN(lineaB) ? 0.2f : lineaB;
            }
            c.YArriba = yA;
            c.YAbajo = yB;
            Log("Pasillo: arriba y = " + yA.ToString("0.00", CultureInfo.InvariantCulture) + " en x = " + XArriba + ", abajo y = " + yB.ToString("0.00", CultureInfo.InvariantCulture) + " en x = " + XAbajo + ".");
        }

        private static float[] ZonaCerroT1355ZEscaleras()
        {
            return new[] { 0f, 60f, 120f };
        }

        private static float AlturaPierna(float z)
        {
            return CerroT1355Trazado.AlturaPierna2(z) + 0.2f;
        }

        /// <summary>Pendiente (grados) de la escalera de concreto en z: línea entre el promedio de las primeras y las últimas muestras de su collider.</summary>
        private static float PendienteEn(Ctx c, float z)
        {
            List<Vector2> pts = new List<Vector2>();
            for (float x = XArriba + 2f; x <= XAbajo - 2f + 0.01f; x += 0.5f)
            {
                float y;
                if (Piso(c, x, z, false, true, out y))
                {
                    pts.Add(new Vector2(x, y));
                }
            }
            if (pts.Count < 12)
            {
                return 0f;
            }
            int k = Mathf.Max(4, pts.Count / 5);
            float xa = pts.Take(k).Average(p => p.x), ya = pts.Take(k).Average(p => p.y);
            float xb = pts.Skip(pts.Count - k).Average(p => p.x), yb = pts.Skip(pts.Count - k).Average(p => p.y);
            return Mathf.Atan2(ya - yb, xb - xa) * Mathf.Rad2Deg;
        }

        // ================================================================ Pasillo escalonado

        private sealed class Malla
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<int> T = new List<int>();

            /// <summary>Cuadrilátero a-b-c-d (en orden alrededor); la cara visible mira hacia <paramref name="normal"/>. UV en metros.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                normal.Normalize();
                bool invertir = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f;
                int i0 = V.Count;
                Vector3[] p = { a, b, c, d };
                Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                foreach (Vector3 q in p)
                {
                    V.Add(q);
                    N.Add(normal);
                    Uv.Add(abs.y >= abs.x && abs.y >= abs.z ? new Vector2(q.x / 2f, q.z / 2f) : (abs.x >= abs.z ? new Vector2(q.z / 2f, q.y / 2f) : new Vector2(q.x / 2f, q.y / 2f)));
                }
                if (invertir)
                {
                    T.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
                }
                else
                {
                    T.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
                }
            }

            /// <summary>Tubo de 6 lados de a a b.</summary>
            public void Tubo(Vector3 a, Vector3 b, float radio)
            {
                Vector3 eje = (b - a).normalized;
                Vector3 u = Vector3.Cross(eje, Vector3.up);
                if (u.sqrMagnitude < 0.01f)
                {
                    u = Vector3.right;
                }
                u.Normalize();
                Vector3 w = Vector3.Cross(eje, u).normalized;
                const int lados = 6;
                for (int k = 0; k < lados; k++)
                {
                    float a0 = k * Mathf.PI * 2f / lados, a1 = (k + 1) * Mathf.PI * 2f / lados;
                    Vector3 n0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0);
                    Vector3 n1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                    Quad(a + n0 * radio, a + n1 * radio, b + n1 * radio, b + n0 * radio, n0 + n1);
                }
            }

            public Mesh Crear(string nombre)
            {
                Mesh m = new Mesh { name = nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.SetVertices(V);
                m.SetNormals(N);
                m.SetUVs(0, Uv);
                m.SetTriangles(T, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        private static int Peldanos(Ctx c)
        {
            return Mathf.Max(4, Mathf.CeilToInt((c.YArriba - c.YAbajo) / AlzaPeldano));
        }

        /// <summary>Altura del peldaño bajo x en el pasillo (tope del peldaño). Fuera del tramo, la de la punta.</summary>
        private static float AlturaPasillo(Ctx c, float x)
        {
            int n = Peldanos(c);
            float h = (XAbajo - XArriba) / n;
            int i = Mathf.Clamp(Mathf.FloorToInt((x - XArriba) / h), 0, n - 1);
            return c.YArriba - i * (c.YArriba - c.YAbajo) / n;
        }

        private static void Pasillo(Ctx c)
        {
            c.Pasillo = Hijo(c.Nuevo, "Pasillo");
            int n = Peldanos(c);
            float h = (XAbajo - XArriba) / n;
            float alza = (c.YArriba - c.YAbajo) / n;
            Malla m = new Malla();
            int sinSuelo = 0;
            for (int i = 0; i < n; i++)
            {
                float x0 = XArriba + i * h, x1 = x0 + h, y = c.YArriba - i * alza;
                m.Quad(new Vector3(x0, y, ZPasilloSur), new Vector3(x0, y, ZPasilloNorte), new Vector3(x1, y, ZPasilloNorte), new Vector3(x1, y, ZPasilloSur), Vector3.up);
                m.Quad(new Vector3(x1, y, ZPasilloSur), new Vector3(x1, y, ZPasilloNorte), new Vector3(x1, y - alza, ZPasilloNorte), new Vector3(x1, y - alza, ZPasilloSur), Vector3.right);
                // Faldones: hasta 0.35 m bajo el terreno más bajo que hay debajo (mínimo 0.4 m, 2.5 m si no hay terreno).
                float suelo = float.MaxValue;
                foreach (float z in new[] { ZPasilloSur, ZPasilloNorte })
                {
                    foreach (float x in new[] { x0, x1 })
                    {
                        float g;
                        if (Piso(c, x, z, true, false, out g))
                        {
                            suelo = Mathf.Min(suelo, g);
                        }
                    }
                }
                float fondo = suelo == float.MaxValue ? 2.5f : Mathf.Clamp(y - suelo + 0.35f, 0.4f, 14f);
                if (suelo == float.MaxValue)
                {
                    sinSuelo++;
                }
                m.Quad(new Vector3(x0, y, ZPasilloSur), new Vector3(x1, y, ZPasilloSur), new Vector3(x1, y - fondo, ZPasilloSur), new Vector3(x0, y - fondo, ZPasilloSur), Vector3.back);
                m.Quad(new Vector3(x0, y, ZPasilloNorte), new Vector3(x1, y, ZPasilloNorte), new Vector3(x1, y - fondo, ZPasilloNorte), new Vector3(x0, y - fondo, ZPasilloNorte), Vector3.forward);
                if (i == 0)
                {
                    m.Quad(new Vector3(x0, y, ZPasilloSur), new Vector3(x0, y, ZPasilloNorte), new Vector3(x0, y - fondo, ZPasilloNorte), new Vector3(x0, y - fondo, ZPasilloSur), Vector3.left);
                }
            }
            Mesh malla = m.Crear("T1406_Pasillo");
            c.Obra.Mallas.Add(malla);
            GameObject go = new GameObject("Escalones");
            go.transform.SetParent(c.Pasillo, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterial = c.Concreto;
            go.AddComponent<MeshCollider>().sharedMesh = malla;
            Log("Pasillo: " + n + " peldaños de " + alza.ToString("0.000", CultureInfo.InvariantCulture) + " m de alza y " + h.ToString("0.000", CultureInfo.InvariantCulture)
                + " m de huella (" + (Mathf.Atan2(alza, h) * Mathf.Rad2Deg).ToString("0.0", CultureInfo.InvariantCulture) + " grados), " + sinSuelo + " sin terreno debajo.");

            // Barandal de tubo en el borde norte del pasillo (entre el pasillo y la escalera): postes cada 4 peldaños, dos pasamanos.
            Malla r = new Malla();
            float zr = ZPasilloNorte - 0.07f;
            List<Vector3> pies = new List<Vector3>();
            for (int i = 1; i < n; i += 4)
            {
                float x = XArriba + (i + 0.5f) * h;
                pies.Add(new Vector3(x, c.YArriba - i * alza, zr));
            }
            foreach (Vector3 p in pies)
            {
                r.Tubo(p, p + Vector3.up * 1.0f, 0.03f);
            }
            for (int k = 0; k + 1 < pies.Count; k++)
            {
                foreach (float alto in new[] { 1.0f, 0.5f })
                {
                    r.Tubo(pies[k] + Vector3.up * alto, pies[k + 1] + Vector3.up * alto, 0.025f);
                }
            }
            Mesh mr = r.Crear("T1406_Barandal");
            c.Obra.Mallas.Add(mr);
            GameObject gr = new GameObject("BarandalTubo");
            gr.transform.SetParent(c.Pasillo, false);
            gr.AddComponent<MeshFilter>().sharedMesh = mr;
            gr.AddComponent<MeshRenderer>().sharedMaterial = c.Tubo;
            Physics.SyncTransforms();
        }

        // ================================================================ Casas

        private static void Casas(Ctx c)
        {
            c.Casas = Hijo(c.Nuevo, "Casas");
            Transform neones = Hijo(c.Nuevo, "Neones");
            GameObject cc07 = Cargar("CC07_CasaCerro" + Variante07);
            GameObject cc06 = Cargar("CC06_CasaCerro" + Variante06);
            int puestas = 0, neonesN = 0;
            for (int k = 0; k < CentrosCasas.Length; k++)
            {
                float xk = CentrosCasas[k];
                float piso = AlturaPasillo(c, xk);
                string tipo = TipoCasas[k];
                GameObject prefab = tipo == "CC07" ? cc07 : (tipo == "CC06" ? cc06 : null);
                if (tipo != "KIT" && prefab == null)
                {
                    tipo = "KIT";
                }
                // Terreno bajo la huella: no más alto que el piso de la casa (entonces la casa quedaría enterrada).
                float mayor = float.MinValue, menor = float.MaxValue;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        float g;
                        if (Piso(c, xk - 3.3f + i * 3.3f, ZFachada - 0.5f - j * 4f, true, false, out g))
                        {
                            mayor = Mathf.Max(mayor, g);
                            menor = Mathf.Min(menor, g);
                        }
                    }
                }
                if (mayor == float.MinValue)
                {
                    Log("Casa " + k + ": sin terreno debajo; no se pone.");
                    continue;
                }
                if (mayor > piso + 1.5f)
                {
                    Log("Casa " + k + " (x = " + xk + "): el terreno (" + mayor.ToString("0.0", CultureInfo.InvariantCulture) + ") queda " + (mayor - piso).ToString("0.0", CultureInfo.InvariantCulture)
                        + " m sobre el piso (" + piso.ToString("0.0", CultureInfo.InvariantCulture) + "); no se pone para no enterrarla.");
                    continue;
                }
                piso = Mathf.Max(piso, mayor - 0.05f);
                Transform casa;
                float ancho = 6.6f, fondo = 8f;
                if (tipo == "KIT")
                {
                    KitS2c.Estilo e = KitS2c.EstiloAlAzar(c.Obra);
                    e.Pisos = 3;
                    e.Toldo = true;
                    e.Neon = null;
                    e.Foco = true;
                    if (!LibreCaja(c, new Vector3(xk, piso + 2.5f, ZFachada - fondo * 0.5f), new Vector3(ancho * 0.5f - 0.2f, 1.5f, fondo * 0.5f - 0.2f)))
                    {
                        Log("Casa " + k + ": hay algo en el lote; no se pone.");
                        continue;
                    }
                    float azotea = KitS2c.Casa(c.Obra, c.Casas, new Vector3(xk, piso, ZFachada), Vector3.forward, ancho, fondo, e);
                    casa = c.Casas.GetChild(c.Casas.childCount - 1);
                    casa.name = "CasaKit_" + k;
                    Colisionar(casa.gameObject);
                    Vector3 atras = new Vector3(xk, piso + azotea, ZFachada - fondo * 0.55f);
                    if (KitS2c.Prefab("TK01_BaseTinaco") != null && KitS2c.Prefab("C09_Tinaco") != null)
                    {
                        KitS2c.Medida mt = KitS2c.Medir("TK01_BaseTinaco");
                        KitS2c.Pieza(casa, "TK01_BaseTinaco", atras, Quaternion.identity, new Vector3(1.5f / mt.Caja.size.x, 0.7f / mt.Caja.size.y, 1f));
                        KitS2c.Pieza(casa, "C09_Tinaco", atras + Vector3.up * 0.7f, Quaternion.identity, Vector3.one * 1.1f);
                    }
                }
                else
                {
                    Vector3 med = Medida(prefab);
                    if (med.x < 0.1f || med.y < 0.1f || med.z < 0.1f)
                    {
                        Log("Casa " + k + ": " + prefab.name + " sin medidas; no se pone.");
                        continue;
                    }
                    float esc = Mathf.Clamp(ancho / med.x, 0.5f, 1.6f);
                    if (med.z * esc > 10.5f)
                    {
                        esc = 10.5f / med.z;
                    }
                    if (med.y * esc > 12f)
                    {
                        esc = 12f / med.y;
                    }
                    ancho = med.x * esc;
                    fondo = med.z * esc;
                    if (!LibreCaja(c, new Vector3(xk, piso + 2.5f, ZFachada - fondo * 0.5f), new Vector3(ancho * 0.5f - 0.2f, 1.5f, fondo * 0.5f - 0.2f)))
                    {
                        Log("Casa " + k + ": hay algo en el lote; no se pone.");
                        continue;
                    }
                    GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, c.Casas);
                    g.name = prefab.name + "_" + k;
                    g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    g.transform.localScale = Vector3.one * esc;
                    Bounds b0 = Cubierta(g.transform);
                    g.transform.position += new Vector3(xk - b0.center.x, piso - b0.min.y, ZFachada - fondo * 0.5f - b0.center.z);
                    Colisionar(g);
                    casa = g.transform;
                }
                Physics.SyncTransforms();
                Bounds b = Cubierta(casa);
                // Cimiento de concreto del terreno más bajo (menos 0.5 m) hasta el piso: sin colgarse y sin hueco debajo.
                float alto = Mathf.Clamp(b.min.y - (menor - 0.5f), 0.6f, 16f);
                KitS2c.Losa(c.Obra, c.Casas, "Cimiento_" + k, new Vector3(b.center.x, b.min.y - alto * 0.5f + 0.01f, b.center.z), Quaternion.identity,
                    new Vector3(b.size.x, alto, b.size.z), c.Concreto, 3f, false);
                c.Techos.Add(new Vector4(b.center.x, b.max.y, b.max.z - 0.6f, k));
                puestas++;
                // Neón chico en la fachada, con su luz (LuzNocturna, sin sombras).
                Color color = k % 2 == 0 ? KitS2c.Magenta : KitS2c.Turquesa;
                KitS2c.Neon(c.Obra, neones, new Vector3(xk + 1.9f, piso + 2.6f, b.max.z), Vector3.forward, Vector3.right, color, true);
                neonesN++;
                if (k == 1)
                {
                    KitS2c.Neon(c.Obra, neones, new Vector3(xk - 2.0f, piso + 4.2f, b.max.z), Vector3.forward, Vector3.right, KitS2c.Turquesa, false);
                    neonesN++;
                }
            }
            Log("Casas puestas: " + puestas + " de " + CentrosCasas.Length + ". Neones: " + neonesN + ".");
        }

        private static bool LibreCaja(Ctx c, Vector3 centro, Vector3 mitad)
        {
            int n = c.Fisica.OverlapBox(centro, mitad, bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!EsPiso(c, bufCol[i].transform) && !bufCol[i].transform.IsChildOf(c.Nuevo))
                {
                    return false;
                }
            }
            return true;
        }

        // ================================================================ Postes y cables

        private static void PostesYCables(Ctx c)
        {
            Transform postes = Hijo(c.Nuevo, "Postes");
            float[] xs = { -327.5f, -340f, -351f };
            int k = 0;
            foreach (float x in xs)
            {
                float y;
                if (!Piso(c, x, 3.0f, true, false, out y))
                {
                    Log("Poste en x = " + x + ": sin piso; no se pone.");
                    continue;
                }
                if (!LibreCaja(c, new Vector3(x, y + 2f, 3.0f), new Vector3(0.4f, 2f, 0.4f)))
                {
                    Log("Poste en x = " + x + ": hay algo en el lugar; no se pone.");
                    continue;
                }
                Vector3 punta = KitS2c.Poste(c.Obra, postes, new Vector3(x, y, 3.0f), Vector3.back, y, k % 2 == 0);
                c.Puntas.Add(punta);
                k++;
            }
            Physics.SyncTransforms();
            Log("Postes con lámpara de sodio: " + c.Puntas.Count + ".");
            // Cables: entre postes y de cada poste a la azotea de una casa del otro lado. Ninguno pasa a menos de 4.5 m de las cámaras.
            int cables = 0, descartados = 0;
            List<(Vector3, Vector3, float)> lista = new List<(Vector3, Vector3, float)>();
            for (int i = 0; i + 1 < c.Puntas.Count; i++)
            {
                lista.Add((c.Puntas[i] + Vector3.up * 0.2f, c.Puntas[i + 1] + Vector3.up * 0.2f, 0.7f));
                lista.Add((c.Puntas[i] - Vector3.up * 0.35f, c.Puntas[i + 1] - Vector3.up * 0.35f, 0.9f));
            }
            for (int i = 0; i < c.Puntas.Count && i < c.Techos.Count; i++)
            {
                Vector4 t = c.Techos[(i * 2) % c.Techos.Count];
                lista.Add((c.Puntas[i], new Vector3(t.x, t.y + 0.25f, t.z), 0.8f));
            }
            foreach ((Vector3 a, Vector3 b, float flecha) in lista)
            {
                if (CableLejos(a, b, flecha))
                {
                    KitS2c.Cable(c.Obra, a, b, flecha);
                    cables++;
                }
                else
                {
                    descartados++;
                }
            }
            Log("Cables: " + cables + " (" + descartados + " descartados por pasar a menos de " + DistanciaCable + " m de una cámara).");
        }

        private static bool CableLejos(Vector3 a, Vector3 b, float flecha)
        {
            Vector3[] p = CalculoCatenaria.Puntos(a, b, flecha, 16);
            foreach (Vector3 cam in new[] { K13Desde, K13BajadaDesde, A13Desde })
            {
                foreach (Vector3 q in p)
                {
                    if (Vector3.Distance(q, cam) < DistanciaCable)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // ================================================================ Carro, menudo

        private static void Carro(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Carros");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            if (prefab == null)
            {
                Log("FALTA " + RutaCarro + "; no hay carro.");
                return;
            }
            // Al pie del callejón, en la boca (al norte de la escalera, orientado al oeste, hacia donde sube).
            Vector3 pos = new Vector3(-315f, 0f, 3.3f);
            float y;
            if (!Piso(c, pos.x, pos.z, true, false, out y))
            {
                Log("Carro: sin piso; no se pone.");
                return;
            }
            pos.y = y + 0.03f;
            Quaternion giro = Quaternion.LookRotation(Vector3.left);
            int n = c.Fisica.OverlapBox(pos + Vector3.up * 0.9f, new Vector3(1.0f, 0.6f, 2.2f), bufCol, giro, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!EsPiso(c, bufCol[i].transform))
                {
                    Log("Carro: algo estorba en " + bufCol[i].transform.name + "; no se pone.");
                    return;
                }
            }
            GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            car.name = "CarroViejo_T1406";
            car.transform.SetPositionAndRotation(pos, giro);
            Physics.SyncTransforms();
            Bounds b = Cubierta(car.transform);
            car.transform.position += Vector3.up * (y - b.min.y);
            Log("Carro viejo en " + car.transform.position.ToString("0.0") + ".");
        }

        private static void Menudo(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Menudo");
            string[] codigos = { "C20d_Macetas", "BS01a_Basura", "BT01_BoteBasura" };
            float[] dx = { -2.6f, 0.9f, 2.7f };
            int n = 0;
            for (int k = 0; k < CentrosCasas.Length; k++)
            {
                for (int j = 0; j < codigos.Length; j++)
                {
                    if (KitS2c.Prefab(codigos[j]) == null)
                    {
                        continue;
                    }
                    float x = CentrosCasas[k] + dx[j];
                    float z = ZPasilloSur + 0.4f;
                    float alto = float.MinValue;
                    foreach (float ox in new[] { -0.2f, 0f, 0.2f })
                    {
                        float yy;
                        if (PisoPasillo(c, x + ox, z, out yy))
                        {
                            alto = Mathf.Max(alto, yy);
                        }
                    }
                    if (alto == float.MinValue)
                    {
                        continue;
                    }
                    KitS2c.Pieza(padre, codigos[j], new Vector3(x, alto, z), Quaternion.Euler(0f, (k * 3 + j) * 70f, 0f), Vector3.one, false);
                    n++;
                }
            }
            Log("Macetas, basura y botes sobre el pasillo: " + n + ".");
        }

        private static bool PisoPasillo(Ctx c, float x, float z, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, 120f, z), Vector3.down, bufRayos, 400f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (c.Pasillo != null && bufRayos[i].collider.transform.IsChildOf(c.Pasillo) && bufRayos[i].point.y > y)
                {
                    y = bufRayos[i].point.y;
                    ok = true;
                }
            }
            return ok;
        }

        // ================================================================ Faroles de las calles de abajo (A13)

        private static void Faroles(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Faroles");
            List<Vector3> luces = c.Zona.GetComponentsInChildren<Light>(false).Where(l => l != null && !l.transform.IsChildOf(c.Nuevo)).Select(l => l.transform.position).ToList();
            List<(Vector3 pos, Vector3 haciaCalle)> candidatos = new List<(Vector3, Vector3)>();
            for (float z = -96f; z <= 128f; z += 24f)
            {
                candidatos.Add((new Vector3(-299.75f, 0f, z), Vector3.left));
            }
            for (float x = -292f; x >= -262f; x -= 15f)
            {
                candidatos.Add((new Vector3(x, 0f, 8.4f), Vector3.back));
            }
            int puestos = 0, tope = Mathf.Min(TopeFaroles, TopeLucesNuevas - c.Nuevo.GetComponentsInChildren<Light>(true).Length);
            foreach ((Vector3 pos, Vector3 haciaCalle) in candidatos)
            {
                if (puestos >= tope)
                {
                    break;
                }
                float y;
                if (!Piso(c, pos.x, pos.z, true, false, out y))
                {
                    continue;
                }
                Vector3 p = new Vector3(pos.x, y, pos.z);
                if (luces.Any(l => new Vector2(l.x - p.x, l.z - p.z).magnitude < 16f))
                {
                    continue;
                }
                if (!LibreCaja(c, p + Vector3.up * 2f, new Vector3(0.4f, 2f, 0.4f)))
                {
                    continue;
                }
                KitS2c.Poste(c.Obra, padre, p, haciaCalle, y, false);
                luces.Add(p);
                puestos++;
            }
            Physics.SyncTransforms();
            Log("Faroles de sodio nuevos en las calles de abajo: " + puestos + " (tope " + TopeFaroles + ", solo donde no había luz a 16 m).");
        }

        // ================================================================ Estático

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t == c.Nuevo || t.GetComponent<Light>() != null || t.GetComponent<Rigidbody>() != null || t.GetComponentInParent<Rigidbody>() != null)
                {
                    continue;
                }
                bool enCarro = false;
                for (Transform p = t; p != null && p != c.Nuevo; p = p.parent)
                {
                    if (p.name.StartsWith("CarroViejo_"))
                    {
                        enCarro = true;
                    }
                }
                if (enCarro)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
                n++;
            }
            Log("Objetos marcados Static: " + n + ".");
        }

        // ================================================================ Validar

        /// <summary>
        /// Sube y baja el callejón (pasillo y escalera) con rayos cada 0.5 m: sin huecos ni escalones de más de 0.3 m sobre la pendiente; las dos puntas conectan con piso;
        /// ningún collider nuevo sin malla visible; casas y cosas asentadas (sin flotar ni enterrarse más de 0.3 m) y sin encimarse; cables a más de 4 m de las cámaras;
        /// tope de luces y tamaño del prefab. Escribe Logs/t1406-validar.txt.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1406/Cerro callejon: validar")]
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1406: validación de " + RutaPrefab + "\n");
            int fallos = 0, avisos = 0;
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Ctx c = Abrir(zona);
                c.Nuevo = zona.transform.Find(Raiz);
                if (c.Nuevo == null)
                {
                    sb.AppendLine("FALLO: no existe " + Raiz + "; corre Aplicar.");
                    fallos++;
                }
                else
                {
                    c.Pasillo = c.Nuevo.Find("Pasillo");
                    c.Casas = c.Nuevo.Find("Casas");
                    Physics.SyncTransforms();
                    ValidarPerfil(c, sb, ref fallos, ref avisos);
                    ValidarColliders(c, sb, ref fallos);
                    ValidarAsentado(c, sb, ref fallos, ref avisos);
                    ValidarCables(c, sb, ref fallos, ref avisos);
                    int luces = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                    sb.AppendLine("Luces nuevas: " + luces + " (tope " + TopeLucesNuevas + ").");
                    if (luces > TopeLucesNuevas)
                    {
                        sb.AppendLine("FALLO: demasiadas luces nuevas.");
                        fallos++;
                    }
                    foreach (Light l in c.Nuevo.GetComponentsInChildren<Light>(true))
                    {
                        if (l.shadows != LightShadows.None)
                        {
                            sb.AppendLine("FALLO: la luz " + l.transform.name + " tiene sombras.");
                            fallos++;
                        }
                    }
                    long bytes = File.Exists(RutaPrefab) ? new FileInfo(RutaPrefab).Length : 0;
                    sb.AppendLine("Prefab: " + (bytes / 1048576f).ToString("0.0", CultureInfo.InvariantCulture) + " MB (tope 60).");
                    if (bytes > TopeBytesPrefab)
                    {
                        sb.AppendLine("FALLO: el prefab pasa de 60 MB.");
                        fallos++;
                    }
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("ERROR: " + e);
                fallos++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
            sb.AppendLine(fallos == 0 ? "RESULTADO: sin fallos (" + avisos + " avisos)." : "RESULTADO: " + fallos + " FALLOS (" + avisos + " avisos).");
            Escribir(RutaValidar, sb);
            Debug.Log("[ZonaCerroT1406] Validar: " + fallos + " fallos, " + avisos + " avisos (" + RutaValidar + ").");
        }

        private static bool Alto(Ctx c, float x, float z, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, 120f, z), Vector3.down, bufRayos, 400f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (bufRayos[i].point.y > y)
                {
                    y = bufRayos[i].point.y;
                    ok = true;
                }
            }
            return ok;
        }

        private static void ValidarPerfil(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            foreach ((string nombre, float z) in new[] { ("pasillo", (ZPasilloSur + ZPasilloNorte) * 0.5f), ("escalera", EscaleraZ) })
            {
                List<float> ys = new List<float>();
                bool hueco = false;
                for (float x = XArriba - 2f; x <= XAbajo + 2f + 0.01f; x += 0.5f)
                {
                    float y;
                    if (!Alto(c, x, z, out y))
                    {
                        sb.AppendLine("FALLO: sin piso en el " + nombre + " (x = " + x.ToString("0.0", CultureInfo.InvariantCulture) + ", z = " + z.ToString("0.0", CultureInfo.InvariantCulture) + ").");
                        fallos++;
                        hueco = true;
                        break;
                    }
                    ys.Add(y);
                }
                if (hueco)
                {
                    continue;
                }
                // Subiendo (de abajo a arriba) y bajando (de arriba a abajo) dan los mismos saltos en valor absoluto.
                float pend = (ys[0] - ys[ys.Count - 1]) / ((ys.Count - 1) * 0.5f);
                float esperado = Mathf.Abs(pend) * 0.5f;
                float peor = 0f;
                int malos = 0;
                for (int i = 1; i < ys.Count; i++)
                {
                    float exceso = Mathf.Abs(ys[i] - ys[i - 1]) - esperado;
                    peor = Mathf.Max(peor, exceso);
                    if (exceso > DesnivelMaximo)
                    {
                        malos++;
                        sb.AppendLine("FALLO: escalón de " + Mathf.Abs(ys[i] - ys[i - 1]).ToString("0.00", CultureInfo.InvariantCulture) + " m (pendiente espera " + esperado.ToString("0.00", CultureInfo.InvariantCulture)
                            + ") en el " + nombre + " cerca de x = " + (XArriba - 2f + i * 0.5f).ToString("0.0", CultureInfo.InvariantCulture) + ".");
                        fallos++;
                    }
                }
                sb.AppendLine("Perfil del " + nombre + ": " + ys.Count + " muestras, pendiente " + (Mathf.Atan(Mathf.Abs(pend)) * Mathf.Rad2Deg).ToString("0.0", CultureInfo.InvariantCulture)
                    + " grados, mayor exceso de escalón " + peor.ToString("0.00", CultureInfo.InvariantCulture) + " m, " + malos + " malos.");
                float grados = Mathf.Atan(Mathf.Abs(pend)) * Mathf.Rad2Deg;
                if (grados > 40f)
                {
                    sb.AppendLine("FALLO: la pendiente del " + nombre + " pasa de 40 grados.");
                    fallos++;
                }
            }
            // Conexión de las puntas del pasillo con piso caminable existente.
            float zm = (ZPasilloSur + ZPasilloNorte) * 0.5f;
            foreach ((string nombre, float x, float xPunta) in new[] { ("arriba", XArriba - 1.2f, XArriba + 0.2f), ("abajo", XAbajo + 1.5f, XAbajo - 0.2f) })
            {
                float yFuera, yPunta;
                if (!Alto(c, x, zm, out yFuera) || !Alto(c, xPunta, zm, out yPunta))
                {
                    sb.AppendLine("FALLO: sin piso en la punta de " + nombre + " del pasillo.");
                    fallos++;
                    continue;
                }
                float dif = Mathf.Abs(yFuera - yPunta);
                sb.AppendLine("Punta " + nombre + ": piso de fuera " + yFuera.ToString("0.00", CultureInfo.InvariantCulture) + ", peldaño " + yPunta.ToString("0.00", CultureInfo.InvariantCulture) + ".");
                if (dif > DesnivelMaximo + AlzaPeldano)
                {
                    sb.AppendLine("FALLO: la punta de " + nombre + " del pasillo no empata con el piso (" + dif.ToString("0.00", CultureInfo.InvariantCulture) + " m).");
                    fallos++;
                }
            }
            // Cada extremo de la escalera de concreto también conecta.
            foreach ((string nombre, float x) in new[] { ("arriba", XArriba - 1.2f), ("abajo", XAbajo + 1.5f) })
            {
                float y;
                if (!Alto(c, x, EscaleraZ, out y))
                {
                    sb.AppendLine("FALLO: la escalera no tiene piso " + nombre + ".");
                    fallos++;
                }
            }
            if (avisos < 0)
            {
                avisos = 0;
            }
        }

        private static void ValidarColliders(Ctx c, StringBuilder sb, ref int fallos)
        {
            int total = 0, sinMalla = 0;
            foreach (Collider col in c.Nuevo.GetComponentsInChildren<Collider>(true))
            {
                total++;
                if (col.isTrigger)
                {
                    continue;
                }
                // La malla visible: en el mismo objeto o en su unidad (hijo directo de una carpeta de la raíz nueva).
                Transform unidad = col.transform;
                while (unidad.parent != null && unidad.parent != c.Nuevo && unidad.parent.parent != c.Nuevo)
                {
                    unidad = unidad.parent;
                }
                Renderer[] rs = unidad.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer) && r.enabled).ToArray();
                if (rs.Length == 0)
                {
                    sinMalla++;
                    sb.AppendLine("FALLO: collider sin malla visible: " + Ruta(col.transform));
                    fallos++;
                    continue;
                }
                Bounds u = rs[0].bounds;
                foreach (Renderer r in rs.Skip(1))
                {
                    u.Encapsulate(r.bounds);
                }
                u.Expand(0.3f);
                MeshCollider mc = col as MeshCollider;
                MeshFilter mf = col.GetComponent<MeshFilter>();
                if (mc != null && mf != null && mf.sharedMesh != mc.sharedMesh)
                {
                    sb.AppendLine("FALLO: MeshCollider distinto de la malla visible: " + Ruta(col.transform));
                    fallos++;
                    sinMalla++;
                }
                else if (!u.Intersects(col.bounds) || col.bounds.size.x > u.size.x + 0.5f || col.bounds.size.y > u.size.y + 0.5f || col.bounds.size.z > u.size.z + 0.5f)
                {
                    sb.AppendLine("FALLO: el collider sobresale de lo visible: " + Ruta(col.transform));
                    fallos++;
                    sinMalla++;
                }
            }
            sb.AppendLine("Colliders nuevos: " + total + ", sin malla visible o fuera de ella: " + sinMalla + ".");
        }

        private static void ValidarAsentado(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            List<Bounds> casas = new List<Bounds>();
            if (c.Casas != null)
            {
                foreach (Transform t in c.Casas.Cast<Transform>().Where(t => !t.name.StartsWith("Cimiento_")))
                {
                    Bounds b = Cubierta(t);
                    casas.Add(b);
                    // Terreno más alto bajo la huella (no debe taparla) y cimiento apoyado en la base.
                    float mayor = float.MinValue;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float g;
                            if (Piso(c, b.min.x + 0.3f + (b.size.x - 0.6f) * i * 0.5f, b.min.z + 0.3f + (b.size.z - 0.6f) * j * 0.5f, true, false, out g))
                            {
                                mayor = Mathf.Max(mayor, g);
                            }
                        }
                    }
                    string num = t.name.Substring(t.name.LastIndexOf('_') + 1);
                    Transform cim = c.Casas.Find("Cimiento_" + num);
                    if (mayor > b.min.y + DesnivelMaximo)
                    {
                        sb.AppendLine("FALLO: " + t.name + " enterrada " + (mayor - b.min.y).ToString("0.00", CultureInfo.InvariantCulture) + " m en el terreno.");
                        fallos++;
                    }
                    if (cim == null)
                    {
                        sb.AppendLine("FALLO: " + t.name + " no tiene cimiento.");
                        fallos++;
                    }
                    else
                    {
                        Bounds bc = Cubierta(cim);
                        float suelo = float.MaxValue;
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                float g;
                                if (Piso(c, b.min.x + 0.3f + (b.size.x - 0.6f) * i * 0.5f, b.min.z + 0.3f + (b.size.z - 0.6f) * j * 0.5f, true, false, out g))
                                {
                                    suelo = Mathf.Min(suelo, g);
                                }
                            }
                        }
                        if (Mathf.Abs(bc.max.y - b.min.y) > 0.1f || (suelo != float.MaxValue && bc.min.y > suelo + DesnivelMaximo))
                        {
                            sb.AppendLine("FALLO: el cimiento de " + t.name + " no llega al terreno o no toca la casa (techo " + bc.max.y.ToString("0.00", CultureInfo.InvariantCulture)
                                + ", casa " + b.min.y.ToString("0.00", CultureInfo.InvariantCulture) + ", fondo " + bc.min.y.ToString("0.00", CultureInfo.InvariantCulture) + ", terreno " + suelo.ToString("0.00", CultureInfo.InvariantCulture) + ").");
                            fallos++;
                        }
                    }
                    if (b.max.z > ZFachada + 0.9f)
                    {
                        sb.AppendLine("FALLO: " + t.name + " invade el pasillo (z máx " + b.max.z.ToString("0.00", CultureInfo.InvariantCulture) + ").");
                        fallos++;
                    }
                    sb.AppendLine(t.name + ": base " + b.min.y.ToString("0.00", CultureInfo.InvariantCulture) + ", alto " + b.size.y.ToString("0.0", CultureInfo.InvariantCulture) + ", x " + b.center.x.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                }
                for (int i = 0; i < casas.Count; i++)
                {
                    for (int j = i + 1; j < casas.Count; j++)
                    {
                        Bounds a = casas[i], b = casas[j];
                        float ox = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                        float oz = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                        if (ox > 0.05f && oz > 0.05f)
                        {
                            sb.AppendLine("FALLO: dos casas nuevas se encima (" + i + " y " + j + ", " + ox.ToString("0.00", CultureInfo.InvariantCulture) + " x " + oz.ToString("0.00", CultureInfo.InvariantCulture) + " m).");
                            fallos++;
                        }
                    }
                }
            }
            // Cosas sueltas (carro, postes, menudo, faroles): su base a menos de 0.3 m del piso debajo.
            foreach (string carpeta in new[] { "Carros", "Postes", "Menudo", "Faroles" })
            {
                Transform f = c.Nuevo.Find(carpeta);
                if (f == null)
                {
                    continue;
                }
                foreach (Transform t in f.Cast<Transform>())
                {
                    Bounds b = Cubierta(t);
                    Vector3 p = carpeta == "Postes" || carpeta == "Faroles" ? t.position : new Vector3(b.center.x, b.min.y, b.center.z);
                    float base0 = carpeta == "Postes" || carpeta == "Faroles" ? t.position.y : b.min.y;
                    int n = c.Fisica.Raycast(new Vector3(p.x, base0 + 3f, p.z), Vector3.down, bufRayos, 12f, ~0, QueryTriggerInteraction.Ignore);
                    float mejor = float.MinValue;
                    for (int i = 0; i < n; i++)
                    {
                        if (!bufRayos[i].collider.transform.IsChildOf(t) && bufRayos[i].point.y > mejor && bufRayos[i].point.y <= base0 + 0.6f)
                        {
                            mejor = bufRayos[i].point.y;
                        }
                    }
                    if (mejor == float.MinValue)
                    {
                        sb.AppendLine("FALLO: " + carpeta + "/" + t.name + " flota: no hay piso a menos de 12 m debajo.");
                        fallos++;
                    }
                    else if (Mathf.Abs(base0 - mejor) > DesnivelMaximo)
                    {
                        sb.AppendLine("FALLO: " + carpeta + "/" + t.name + " a " + (base0 - mejor).ToString("0.00", CultureInfo.InvariantCulture) + " m del piso (flota o enterrado).");
                        fallos++;
                    }
                }
                sb.AppendLine(carpeta + ": " + f.childCount + " objetos revisados.");
            }
            if (avisos < 0)
            {
                avisos = 0;
            }
        }

        private static void ValidarCables(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            // Nuevos: FALLO si pasan a menos de 4 m de una cámara. Los de rondas anteriores: AVISO (no son de esta tarea).
            foreach (MeshFilter mf in c.Zona.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || !(mf.name.Contains("Cable") || mf.sharedMesh.name.Contains("Cable")) || !mf.sharedMesh.isReadable)
                {
                    continue;
                }
                bool nuevo = mf.transform.IsChildOf(c.Nuevo);
                foreach (Vector3 cam in new[] { K13Desde, K13BajadaDesde, A13Desde })
                {
                    float min = float.MaxValue;
                    foreach (Vector3 v in mf.sharedMesh.vertices)
                    {
                        min = Mathf.Min(min, Vector3.Distance(mf.transform.TransformPoint(v), cam));
                    }
                    if (min < 4f)
                    {
                        sb.AppendLine((nuevo ? "FALLO" : "AVISO") + ": cable " + Ruta(mf.transform) + " a " + min.ToString("0.0", CultureInfo.InvariantCulture) + " m de la cámara " + cam.ToString("0.0") + ".");
                        if (nuevo)
                        {
                            fallos++;
                        }
                        else
                        {
                            avisos++;
                        }
                    }
                }
            }
            sb.AppendLine("Cables revisados contra K13, K13 bajada y A13 (mínimo 4 m).");
        }

        // ================================================================ Fotos

        /// <summary>
        /// K13 (al pie del callejón, mirando cuesta arriba) y A13 (aérea, cuesta abajo hacia la ciudad), referencia a la izquierda y juego a la derecha, más una de K13 desde arriba.
        /// No guarda nada; corre en una corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1406/Cerro callejon: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1406");
            Physics.SyncTransforms();
            Vector3 corr = CamaraLibre(K13Desde, K13Hacia);
            Debug.Log("[ZonaCerroT1406] Cámara K13 corrida " + corr + " desde " + K13Desde + ".");
            ColocarS2cLook.Foto(K13Desde + corr, K13Hacia + corr, "docs/capturas/t-1406/cerro-k13-juego.png", CampoFoto);
            ColocarS2cLook.Foto(A13Desde, A13Hacia, "docs/capturas/t-1406/cerro-a13-juego.png", CampoFoto);
            ColocarS2cLook.Foto(K13BajadaDesde, K13BajadaHacia, "docs/capturas/t-1406/cerro-k13-bajada.png", CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/t-1406/cerro-k13-juego.png", "docs/capturas/t-1406/cerro-k13.png");
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", "docs/capturas/t-1406/cerro-a13-juego.png", "docs/capturas/t-1406/cerro-a13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        private static Vector3 CamaraLibre(Vector3 desde, Vector3 hacia)
        {
            Vector3 dir = (hacia - desde).normalized;
            Vector3[] corrimientos =
            {
                Vector3.zero, new Vector3(0.8f, 0f, 0f), new Vector3(0f, 0f, -0.6f), new Vector3(1.6f, 0f, -0.6f), new Vector3(0f, 0f, 0.8f), new Vector3(2.4f, 0f, 0f),
            };
            foreach (Vector3 o in corrimientos)
            {
                Vector3 p = desde + o;
                bool tapada = Physics.CheckSphere(p, 0.8f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(p, dir, 5f, ~0, QueryTriggerInteraction.Ignore);
                bool suelo = Physics.Raycast(p, Vector3.down, 3.5f, ~0, QueryTriggerInteraction.Ignore);
                if (!tapada && suelo)
                {
                    return o;
                }
            }
            Debug.LogWarning("[ZonaCerroT1406] Ningún corrimiento dejó libre la cámara K13; se usa la posición original.");
            return Vector3.zero;
        }

        // ================================================================ Ayudas

        private static void Log(string texto)
        {
            if (informe != null)
            {
                informe.AppendLine(texto);
            }
            Debug.Log("[ZonaCerroT1406] " + texto);
        }

        private static void Escribir(string ruta, StringBuilder sb)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllText(completa, sb.ToString());
        }

        private static string Megas(string ruta)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            return File.Exists(completa) ? (new FileInfo(completa).Length / 1048576f).ToString("0.0", CultureInfo.InvariantCulture) : "?";
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static string Ruta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        private static GameObject Cargar(string nombre)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ZonaCerroT1385.CarpetaCC + nombre + ".prefab");
            if (p == null)
            {
                Log("FALTA " + ZonaCerroT1385.CarpetaCC + nombre + ".prefab; se usa una casa del kit en su lugar.");
            }
            return p;
        }

        /// <summary>Tamaño del prefab sin girar ni escalar (por sus renderers).</summary>
        private static Vector3 Medida(GameObject prefab)
        {
            if (medidas.TryGetValue(prefab.name, out Vector3 ya))
            {
                return ya;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Vector3 tam = Cubierta(g.transform).size;
            Object.DestroyImmediate(g);
            medidas[prefab.name] = tam;
            return tam;
        }

        private static Bounds Cubierta(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return new Bounds(t.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
            {
                b.Encapsulate(rs[i].bounds);
            }
            return b;
        }

        /// <summary>Si el modelo no trae collider, cada malla visible recibe uno igual a ella (sin muros invisibles).</summary>
        private static void Colisionar(GameObject g)
        {
            if (g.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                return;
            }
            foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Renderer>() != null && mf.GetComponent<Collider>() == null)
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
        }
    }
}
