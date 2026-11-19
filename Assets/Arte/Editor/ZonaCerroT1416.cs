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
    /// T-1416: K13 con la ciudad a un lado. La pierna 2 del zigzag (x = -360) sube al sur (5 %) y la ladera baja al este: mirando al sur desde su extremo norte
    /// la ciudad iluminada queda a la IZQUIERDA (espejo de la referencia, donde queda a la derecha) y las casas del oeste y la calle a la derecha.
    /// Sobre el prefab T1355_Cerro (sin correr el generador) y bajo la raíz propia "T1416_Cerro" (Aplicar la rehace):
    ///  - un paseo escalonado de concreto sobre el borde este de la calle (3 tramos, entre las escaleras CE01 de z = 120, 60 y 0), a la altura de la banqueta
    ///    y con peldaños de 0.15 m cada 3 m, con barandal de tubo (módulo CE02_BarandalTubo, o tubo propio si falta) del lado de la ciudad;
    ///  - las casas del lado este que estorban (donde va el paseo y las que tapan la vista entre 6 y 60 m) se APAGAN (SetActive false, reversible:
    ///    se anotan en T1416_Cerro/Apagados y Aplicar las vuelve a encender antes de rehacer);
    ///  - 2 postes con lámpara de sodio sobre el paseo, cables, 2 carros viejos, neones en las fachadas del oeste.
    /// Orden: T1385, T1394, T1401, (T1405), T1406, T1416.Aplicar; luego T1416.Validar; Fotos (busca la mejor cámara) en una corrida de Unity aparte.
    /// </summary>
    public static class ZonaCerroT1416
    {
        public const string RutaPrefab = ZonaCerroT1385.RutaPrefab;
        public const string Raiz = "T1416_Cerro";
        public const string RutaInforme = "Logs/t1416-aplicar.txt";
        public const string RutaValidar = "Logs/t1416-validar.txt";
        public const string RutaBusqueda = "Logs/t1416-busqueda.txt";
        public const int Semilla = 14160;
        public const long TopeBytesPrefab = 60L * 1024 * 1024;
        public const int TopeLucesNuevas = 40;

        // Paseo: franja de 2 m pegada al borde este de la banqueta (la banqueta termina en x = -354, donde empiezan las escaleras CE01).
        public const float XPaseo0 = -354f;
        public const float XPaseo1 = -352f;
        public const float LargoPeldano = 3f;
        public const float DesnivelMaximo = 0.3f;
        public const float AltoBanqueta = 0.2f;
        /// <summary>Tramos del paseo (z de arriba en el mapa = norte, z de abajo = sur), entre las escaleras de z = 120, 60 y 0 (hueco de 8 m).</summary>
        public static readonly float[][] Tramos = { new[] { 150f, 124f }, new[] { 116f, 64f }, new[] { 56f, 20f } };

        // K13b: sobre la pierna 2, en el carril oeste, 2 m antes del extremo norte del paseo, mirando al sur (cuesta arriba) un poco al este.
        // y = calle (10 + 0.05 * (175 - z)) + 1.7 m de ojos. La ciudad (al este y abajo) cae en la mitad izquierda del cuadro.
        public static readonly Vector3 K13bDesde = new Vector3(-361.5f, 12.85f, 152f);
        public static readonly Vector3 K13bHacia = new Vector3(-355f, 14.0f, 60f);
        /// <summary>true: la ciudad queda en la mitad izquierda (mirando al sur); casas y escalera en la derecha.</summary>
        public const bool CiudadALaIzquierda = true;
        public const float CampoFoto = 66f;
        public const float Aspecto = 1376f / 768f;
        public const float DistanciaCiudad = 80f;
        public const float VistaMinima = 6f;
        public const float VistaMaxima = 60f;
        private const float DistanciaCable = 4.5f;

        public const string RutaBarandal = "CE02_BarandalTubo";
        public const string RutaCarro = "Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab";

        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[256];
        private static readonly Collider[] bufCol = new Collider[256];
        private static readonly Dictionary<string, Vector3> medidas = new Dictionary<string, Vector3>();
        private static readonly string[] Protegidos = { "Terreno", "Calles", "Escaleras", "Postes", "Cables", "Carros", "Huecos", "T1406_Cerro" };

        private sealed class Plano
        {
            public float ZN, ZS, Top, Suelo;
            public bool Ok;
        }

        private sealed class Ctx
        {
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Terreno, Calles, Escaleras, Nuevo, Paseo;
            public KitS2c.Obra Obra;
            public Material Concreto, Tubo;
            public readonly List<Plano> Planos = new List<Plano>();
            public readonly List<Vector3> Puntas = new List<Vector3>();
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1416/Cerro K13 ciudad: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1416: K13 con la ciudad a un lado (" + RutaPrefab + ")\n");
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
                c.Tubo = KitS2c.Mat("T1416_TuboOxido", () => KitS2c.Lit(new Color(0.45f, 0.22f, 0.13f), 0.3f, KitS2c.TexMuro()));
                Physics.SyncTransforms();
                Paso("apagar casas", () => Apagar(c));
                Paso("paseo", () => Paseo(c));
                Paso("barandal", () => Barandal(c));
                Paso("postes y cables", () => PostesYCables(c));
                Paso("carros", () => Carros(c));
                Paso("neones", () => Neones(c));
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
                Debug.LogError("[ZonaCerroT1416] " + e);
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
                Debug.LogError("[ZonaCerroT1416] " + nombre + ": " + e);
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

        /// <summary>Renderers activos de la zona que pueden apagarse (ni nuevos, ni protegidos, ni chicos).</summary>
        private static List<Renderer> Candidatos(Ctx c)
        {
            List<Renderer> l = new List<Renderer>();
            foreach (Renderer r in c.Zona.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r.transform.IsChildOf(c.Nuevo) || EsProtegido(c, r.transform))
                {
                    continue;
                }
                l.Add(r);
            }
            return l;
        }

        /// <summary>Cajas (en planta) de los tramos del paseo, con 0.3 m de margen.</summary>
        private static Bounds CajaTramo(float[] t)
        {
            float zc = (t[0] + t[1]) * 0.5f, largo = Mathf.Abs(t[0] - t[1]) + 0.6f;
            return new Bounds(new Vector3((XPaseo0 + XPaseo1) * 0.5f, 10f, zc), new Vector3(XPaseo1 - XPaseo0 + 0.6f, 400f, largo));
        }

        private static bool EnHuella(Bounds b)
        {
            foreach (float[] t in Tramos)
            {
                Bounds h = CajaTramo(t);
                float ox = Mathf.Min(b.max.x, h.max.x) - Mathf.Max(b.min.x, h.min.x);
                float oz = Mathf.Min(b.max.z, h.max.z) - Mathf.Max(b.min.z, h.min.z);
                if (ox > 0.05f && oz > 0.05f)
                {
                    return true;
                }
            }
            return false;
        }

        // ================================================================ El cuadro de K13b

        private static bool Proyectar(Vector3 cam, Vector3 f, Vector3 q, out float xn, out float yn, out float prof)
        {
            Vector3 derecha = Vector3.Cross(Vector3.up, f).normalized;
            Vector3 arriba = Vector3.Cross(f, derecha).normalized;
            Vector3 d = q - cam;
            prof = Vector3.Dot(d, f);
            float tanV = Mathf.Tan(CampoFoto * 0.5f * Mathf.Deg2Rad);
            xn = 0f;
            yn = 0f;
            if (prof < 0.1f)
            {
                return false;
            }
            xn = Vector3.Dot(d, derecha) / (prof * tanV * Aspecto);
            yn = Vector3.Dot(d, arriba) / (prof * tanV);
            return Mathf.Abs(xn) <= 1f && Mathf.Abs(yn) <= 1f;
        }

        /// <summary>Puntos de una ciudad sintética (al este y abajo de la cámara) que caen en el lado de la ciudad del cuadro de K13b.</summary>
        private static List<Vector3> ObjetivosCiudad()
        {
            List<Vector3> l = new List<Vector3>();
            Vector3 f = (K13bHacia - K13bDesde).normalized;
            for (float x = -300f; x <= -160f; x += 20f)
            {
                for (float z = -100f; z <= 150f; z += 25f)
                {
                    foreach (float y in new[] { 4f, 9f })
                    {
                        Vector3 q = new Vector3(x, y, z);
                        float xn, yn, prof;
                        if ((q - K13bDesde).magnitude >= DistanciaCiudad && q.y < K13bDesde.y && Proyectar(K13bDesde, f, q, out xn, out yn, out prof) && (CiudadALaIzquierda ? xn < 0f : xn > 0f))
                        {
                            l.Add(q);
                        }
                    }
                }
            }
            return l;
        }

        /// <summary>Unidades (casas) cuyo renderer corta algún rayo de la cámara a la ciudad entre 6 y 60 m de la cámara.</summary>
        private static HashSet<Transform> Bloqueos(Ctx c)
        {
            HashSet<Transform> unidades = new HashSet<Transform>();
            List<Vector3> objetivos = ObjetivosCiudad();
            List<Renderer> cerca = Candidatos(c).Where(r => r.bounds.size.magnitude > 4f && Vector3.Distance(r.bounds.center, K13bDesde) < VistaMaxima + 20f).ToList();
            foreach (Vector3 q in objetivos)
            {
                Vector3 d = q - K13bDesde;
                float largo = d.magnitude;
                Ray rayo = new Ray(K13bDesde, d / largo);
                foreach (Renderer r in cerca)
                {
                    float dist;
                    if (r.bounds.IntersectRay(rayo, out dist) && dist >= VistaMinima && dist <= VistaMaxima && dist < largo - 1f)
                    {
                        unidades.Add(Unidad(c, r.transform));
                    }
                }
            }
            return unidades;
        }

        private static void Apagar(Ctx c)
        {
            HashSet<Transform> unidades = new HashSet<Transform>();
            int porHuella = 0;
            foreach (Renderer r in Candidatos(c))
            {
                if (EnHuella(r.bounds))
                {
                    if (unidades.Add(Unidad(c, r.transform)))
                    {
                        porHuella++;
                    }
                }
            }
            HashSet<Transform> vista = Bloqueos(c);
            int porVista = 0;
            foreach (Transform u in vista)
            {
                if (unidades.Add(u))
                {
                    porVista++;
                }
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
            Log("Casas y cosas apagadas (reversible): " + lista.Count + " (" + porHuella + " por la huella del paseo, " + porVista + " por tapar la vista a la ciudad entre "
                + VistaMinima + " y " + VistaMaxima + " m).");
        }

        // ================================================================ Piso

        private static bool EsPiso(Ctx c, Transform t)
        {
            return (c.Terreno != null && t.IsChildOf(c.Terreno)) || (c.Calles != null && t.IsChildOf(c.Calles));
        }

        private static bool Piso(Ctx c, float x, float z, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, 120f, z), Vector3.down, bufRayos, 400f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = bufRayos[i].collider.transform;
                if ((c.Nuevo != null && t.IsChildOf(c.Nuevo)) || !EsPiso(c, t))
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

        /// <summary>Altura de la banqueta (borde este de la calle) en z: la medida, o la del trazado si la medida no cuadra.</summary>
        private static float Banqueta(Ctx c, float z)
        {
            float analitica = CerroT1355Trazado.AlturaPierna2(z) + AltoBanqueta;
            float y;
            if (Piso(c, XPaseo0 - 0.3f, z, out y) && Mathf.Abs(y - analitica) <= 0.8f)
            {
                return y;
            }
            return analitica;
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

        // ================================================================ Mallas

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

        // ================================================================ Paseo escalonado

        private static void Paseo(Ctx c)
        {
            c.Paseo = Hijo(c.Nuevo, "Paseo");
            Malla m = new Malla();
            int hechos = 0, saltados = 0;
            foreach (float[] t in Tramos)
            {
                float largo = t[0] - t[1];
                int n = Mathf.Max(1, Mathf.RoundToInt(largo / LargoPeldano));
                float paso = largo / n;
                for (int i = 0; i < n; i++)
                {
                    Plano p = new Plano { ZN = t[0] - i * paso, ZS = t[0] - (i + 1) * paso };
                    // Tope: la banqueta en el borde norte del peldaño (el más bajo): el peldaño queda al ras o a lo más 0.15 m abajo.
                    p.Top = Banqueta(c, p.ZN);
                    float menor = float.MaxValue, mayor = float.MinValue;
                    int muestras = 0;
                    foreach (float x in new[] { XPaseo0 + 0.05f, (XPaseo0 + XPaseo1) * 0.5f, XPaseo1 - 0.05f })
                    {
                        foreach (float z in new[] { p.ZN - 0.05f, (p.ZN + p.ZS) * 0.5f, p.ZS + 0.05f })
                        {
                            float g;
                            if (Piso(c, x, z, out g))
                            {
                                menor = Mathf.Min(menor, g);
                                mayor = Mathf.Max(mayor, g);
                                muestras++;
                            }
                        }
                    }
                    string porque = null;
                    if (muestras == 0)
                    {
                        porque = "sin terreno debajo";
                    }
                    else if (mayor > p.Top + 0.1f)
                    {
                        porque = "el terreno (" + mayor.ToString("0.00", CultureInfo.InvariantCulture) + ") queda sobre el tope (" + p.Top.ToString("0.00", CultureInfo.InvariantCulture) + ")";
                    }
                    else if (!LibreCaja(c, new Vector3((XPaseo0 + XPaseo1) * 0.5f, p.Top + 1.4f, (p.ZN + p.ZS) * 0.5f), new Vector3((XPaseo1 - XPaseo0) * 0.5f - 0.1f, 1.2f, Mathf.Abs(p.ZN - p.ZS) * 0.5f - 0.1f)))
                    {
                        porque = "hay algo en el lugar (poste, casa)";
                    }
                    if (porque != null)
                    {
                        Log("Peldaño z " + p.ZS.ToString("0.0", CultureInfo.InvariantCulture) + " a " + p.ZN.ToString("0.0", CultureInfo.InvariantCulture) + ": no se pone, " + porque + ".");
                        saltados++;
                        c.Planos.Add(p);
                        continue;
                    }
                    p.Ok = true;
                    p.Suelo = menor;
                    float fondo = Mathf.Clamp(p.Top - menor + 0.35f, 0.4f, 14f);
                    float x0 = XPaseo0, x1 = XPaseo1, y = p.Top;
                    m.Quad(new Vector3(x0, y, p.ZS), new Vector3(x0, y, p.ZN), new Vector3(x1, y, p.ZN), new Vector3(x1, y, p.ZS), Vector3.up);
                    m.Quad(new Vector3(x0, y, p.ZN), new Vector3(x1, y, p.ZN), new Vector3(x1, y - fondo, p.ZN), new Vector3(x0, y - fondo, p.ZN), Vector3.forward);
                    m.Quad(new Vector3(x0, y, p.ZS), new Vector3(x1, y, p.ZS), new Vector3(x1, y - fondo, p.ZS), new Vector3(x0, y - fondo, p.ZS), Vector3.back);
                    m.Quad(new Vector3(x1, y, p.ZS), new Vector3(x1, y, p.ZN), new Vector3(x1, y - fondo, p.ZN), new Vector3(x1, y - fondo, p.ZS), Vector3.right);
                    m.Quad(new Vector3(x0, y, p.ZS), new Vector3(x0, y, p.ZN), new Vector3(x0, y - fondo, p.ZN), new Vector3(x0, y - fondo, p.ZS), Vector3.left);
                    c.Planos.Add(p);
                    hechos++;
                }
            }
            if (hechos == 0)
            {
                Log("AVISO: no se puso ningún peldaño del paseo.");
                return;
            }
            Mesh malla = m.Crear("T1416_Paseo");
            c.Obra.Mallas.Add(malla);
            GameObject go = new GameObject("Escalones");
            go.transform.SetParent(c.Paseo, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterial = c.Concreto;
            go.AddComponent<MeshCollider>().sharedMesh = malla;
            Physics.SyncTransforms();
            float peor = 0f;
            Plano[] oks = c.Planos.Where(p => p.Ok).ToArray();
            for (int i = 1; i < oks.Length; i++)
            {
                if (Mathf.Abs(oks[i].ZN - oks[i - 1].ZS) < 0.01f)
                {
                    peor = Mathf.Max(peor, oks[i].Top - oks[i - 1].Top);
                }
            }
            Log("Paseo: " + hechos + " peldaños de " + LargoPeldano + " m (" + saltados + " saltados), mayor alza entre peldaños " + peor.ToString("0.000", CultureInfo.InvariantCulture) + " m.");
        }

        // ================================================================ Barandal de tubo (lado de la ciudad)

        private static void Barandal(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Barandal");
            GameObject modulo = AssetDatabase.LoadAssetAtPath<GameObject>(CorrerZonasT1350.RutaModulo(RutaBarandal));
            Vector3 med = modulo == null ? Vector3.zero : Medida(modulo);
            bool largoEnX = med.x >= med.z;
            float largoModulo = largoEnX ? med.x : med.z;
            bool usable = modulo != null && largoModulo > 0.5f && med.y > 0.6f && med.y < 2.2f;
            if (!usable)
            {
                Log("Módulo " + RutaBarandal + " " + (modulo == null ? "no existe" : "con medidas raras " + med.ToString("0.00")) + ": se usa el tubo propio.");
            }
            else
            {
                Log("Módulo " + RutaBarandal + ": " + med.ToString("0.00") + " m, largo en " + (largoEnX ? "x" : "z") + ".");
            }
            Malla propio = new Malla();
            int modulos = 0, tubos = 0;
            float xr = XPaseo1 - 0.12f;
            foreach (Plano p in c.Planos.Where(q => q.Ok))
            {
                float largo = p.ZN - p.ZS;
                float zc = (p.ZN + p.ZS) * 0.5f;
                float esc = usable ? largo / largoModulo : 0f;
                if (usable && esc >= 0.5f && esc <= 2f)
                {
                    GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(modulo, padre);
                    g.name = modulo.name + "_" + modulos;
                    g.transform.SetPositionAndRotation(Vector3.zero, largoEnX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity);
                    g.transform.localScale = largoEnX ? new Vector3(esc, 1f, 1f) : new Vector3(1f, 1f, esc);
                    Bounds b0 = Cubierta(g.transform);
                    g.transform.position += new Vector3(xr - b0.center.x, p.Top - b0.min.y, zc - b0.center.z);
                    modulos++;
                }
                else
                {
                    // Tubo propio: postes cada 1.5 m y dos pasamanos.
                    int postes = Mathf.Max(2, Mathf.CeilToInt(largo / 1.5f) + 1);
                    List<Vector3> pies = new List<Vector3>();
                    for (int i = 0; i < postes; i++)
                    {
                        pies.Add(new Vector3(xr, p.Top, Mathf.Lerp(p.ZN - 0.05f, p.ZS + 0.05f, i / (float)(postes - 1))));
                    }
                    foreach (Vector3 pie in pies)
                    {
                        propio.Tubo(pie, pie + Vector3.up * 1.0f, 0.03f);
                    }
                    for (int i = 0; i + 1 < pies.Count; i++)
                    {
                        foreach (float alto in new[] { 1.0f, 0.5f })
                        {
                            propio.Tubo(pies[i] + Vector3.up * alto, pies[i + 1] + Vector3.up * alto, 0.025f);
                        }
                    }
                    tubos++;
                }
            }
            if (propio.T.Count > 0)
            {
                Mesh mr = propio.Crear("T1416_Barandal");
                c.Obra.Mallas.Add(mr);
                GameObject gr = new GameObject("BarandalTubo");
                gr.transform.SetParent(padre, false);
                gr.AddComponent<MeshFilter>().sharedMesh = mr;
                gr.AddComponent<MeshRenderer>().sharedMaterial = c.Tubo;
            }
            Physics.SyncTransforms();
            Log("Barandal: " + modulos + " módulos y " + tubos + " tramos de tubo propio (solo visual).");
        }

        // ================================================================ Postes y cables

        private static void PostesYCables(Ctx c)
        {
            Transform postes = Hijo(c.Nuevo, "Postes");
            Plano[] oks = c.Planos.Where(p => p.Ok).ToArray();
            if (oks.Length == 0)
            {
                return;
            }
            // Un poste en el peldaño más cercano a z = 137 y otro al más cercano a z = 90, en el centro del paseo.
            foreach (float zq in new[] { 137f, 90f })
            {
                Plano p = oks.OrderBy(q => Mathf.Abs((q.ZN + q.ZS) * 0.5f - zq)).First();
                Vector3 pie = new Vector3(XPaseo0 + 0.7f, p.Top, (p.ZN + p.ZS) * 0.5f);
                if (!LibreCaja(c, pie + Vector3.up * 2f, new Vector3(0.35f, 2f, 0.35f)))
                {
                    Log("Poste en z = " + pie.z.ToString("0.0", CultureInfo.InvariantCulture) + ": hay algo en el lugar; no se pone.");
                    continue;
                }
                Vector3 punta = KitS2c.Poste(c.Obra, postes, pie, Vector3.left, pie.y, c.Puntas.Count % 2 == 0);
                c.Puntas.Add(punta);
            }
            Physics.SyncTransforms();
            Log("Postes con lámpara de sodio sobre el paseo: " + c.Puntas.Count + ".");
            int cables = 0, descartados = 0;
            for (int i = 0; i + 1 < c.Puntas.Count; i++)
            {
                foreach ((Vector3 a, Vector3 b, float flecha) in new[]
                {
                    (c.Puntas[i] + Vector3.up * 0.2f, c.Puntas[i + 1] + Vector3.up * 0.2f, 0.9f),
                    (c.Puntas[i] - Vector3.up * 0.35f, c.Puntas[i + 1] - Vector3.up * 0.35f, 1.1f),
                })
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
            }
            Log("Cables: " + cables + " (" + descartados + " descartados por pasar a menos de " + DistanciaCable + " m de la cámara).");
        }

        private static bool CableLejos(Vector3 a, Vector3 b, float flecha)
        {
            foreach (Vector3 q in CalculoCatenaria.Puntos(a, b, flecha, 16))
            {
                if (Vector3.Distance(q, K13bDesde) < DistanciaCable)
                {
                    return false;
                }
            }
            return true;
        }

        // ================================================================ Carros y neones

        private static void Carros(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Carros");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            if (prefab == null)
            {
                Log("FALTA " + RutaCarro + "; no hay carros.");
                return;
            }
            int n = 0;
            foreach ((float x, float z, bool haciaSur) in new[] { (-357.6f, 100f, true), (-362.4f, 76f, false) })
            {
                Vector3 pos = new Vector3(x, 0f, z);
                float y;
                if (!Piso(c, pos.x, pos.z, out y))
                {
                    Log("Carro en z = " + z + ": sin piso; no se pone.");
                    continue;
                }
                pos.y = y + 0.03f;
                Quaternion giro = Quaternion.LookRotation(haciaSur ? Vector3.back : Vector3.forward);
                if (!LibreCaja(c, pos + Vector3.up * 0.9f, new Vector3(1.0f, 0.6f, 2.2f)))
                {
                    Log("Carro en z = " + z + ": algo estorba; no se pone.");
                    continue;
                }
                GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                car.name = "CarroViejo_T1416_" + n;
                car.transform.SetPositionAndRotation(pos, giro);
                Physics.SyncTransforms();
                Bounds b = Cubierta(car.transform);
                car.transform.position += Vector3.up * (y - b.min.y);
                Log("Carro viejo en " + car.transform.position.ToString("0.0") + ".");
                n++;
            }
        }

        /// <summary>Neones de bandera en las fachadas del oeste (la ciudad queda del otro lado): se buscan con un rayo hacia la pared, así quedan pegados.</summary>
        private static void Neones(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Neones");
            int n = 0;
            foreach (float z in new[] { 132f, 108f, 88f, 68f })
            {
                float y = CerroT1355Trazado.AlturaPierna2(z) + AltoBanqueta + 3.2f;
                Vector3 origen = new Vector3(-361f, y, z);
                int k = c.Fisica.Raycast(origen, Vector3.left, bufRayos, 12f, ~0, QueryTriggerInteraction.Ignore);
                bool puesto = false;
                float mejor = float.MaxValue;
                RaycastHit golpe = default(RaycastHit);
                for (int i = 0; i < k; i++)
                {
                    Transform t = bufRayos[i].collider.transform;
                    if (t.IsChildOf(c.Nuevo) || EsPiso(c, t) || EsProtegido(c, t) || bufRayos[i].normal.x < 0.7f)
                    {
                        continue;
                    }
                    if (bufRayos[i].distance < mejor)
                    {
                        mejor = bufRayos[i].distance;
                        golpe = bufRayos[i];
                        puesto = true;
                    }
                }
                if (!puesto)
                {
                    Log("Neón en z = " + z + ": no hay pared del oeste a la altura; no se pone.");
                    continue;
                }
                Color color = n % 2 == 0 ? KitS2c.Magenta : KitS2c.Turquesa;
                KitS2c.Neon(c.Obra, padre, golpe.point + Vector3.right * 0.04f, Vector3.right, Vector3.forward, color, true);
                n++;
            }
            Log("Neones en las fachadas del oeste: " + n + ".");
        }

        // ================================================================ Estático

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t == c.Nuevo || t.GetComponent<Light>() != null || t.GetComponentInParent<Rigidbody>() != null)
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
        /// Paseo: sin huecos ni alzas de más de 0.3 m entre peldaños ni contra la banqueta; collider igual a la malla; ningún collider nuevo sin malla visible;
        /// nada activo sobre la huella; la vista a la ciudad libre entre 6 y 60 m; carros y postes asentados; cables a más de 4 m de la cámara; luces y tamaño.
        /// Escribe Logs/t1416-validar.txt.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1416/Cerro K13 ciudad: validar")]
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1416: validación de " + RutaPrefab + "\n");
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
                    c.Paseo = c.Nuevo.Find("Paseo");
                    Physics.SyncTransforms();
                    ValidarPaseo(c, sb, ref fallos, ref avisos);
                    ValidarColliders(c, sb, ref fallos);
                    ValidarHuellaYVista(c, sb, ref fallos, ref avisos);
                    ValidarAsentado(c, sb, ref fallos);
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
            Debug.Log("[ZonaCerroT1416] Validar: " + fallos + " fallos, " + avisos + " avisos (" + RutaValidar + ").");
        }

        /// <summary>Tope del paseo en (x, z): rayo contra los colliders del paseo.</summary>
        private static bool TopePaseo(Ctx c, float x, float z, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, 120f, z), Vector3.down, bufRayos, 400f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (c.Paseo != null && bufRayos[i].collider.transform.IsChildOf(c.Paseo) && bufRayos[i].point.y > y)
                {
                    y = bufRayos[i].point.y;
                    ok = true;
                }
            }
            return ok;
        }

        private static void ValidarPaseo(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            if (c.Paseo == null || c.Paseo.Find("Escalones") == null)
            {
                sb.AppendLine("FALLO: no hay paseo.");
                fallos++;
                return;
            }
            float xm = (XPaseo0 + XPaseo1) * 0.5f;
            int total = 0, conPiso = 0;
            foreach (float[] t in Tramos)
            {
                float anterior = float.NaN;
                for (float z = t[0] - 0.1f; z >= t[1] + 0.1f; z -= 0.5f)
                {
                    total++;
                    float y;
                    if (!TopePaseo(c, xm, z, out y))
                    {
                        anterior = float.NaN;
                        continue;
                    }
                    conPiso++;
                    if (!float.IsNaN(anterior) && Mathf.Abs(y - anterior) > DesnivelMaximo)
                    {
                        sb.AppendLine("FALLO: alza de " + Mathf.Abs(y - anterior).ToString("0.00", CultureInfo.InvariantCulture) + " m en el paseo cerca de z = " + z.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                        fallos++;
                    }
                    float banqueta;
                    if (Piso(c, XPaseo0 - 0.3f, z, out banqueta) && Mathf.Abs(banqueta - y) > DesnivelMaximo)
                    {
                        sb.AppendLine("FALLO: el paseo difiere " + Mathf.Abs(banqueta - y).ToString("0.00", CultureInfo.InvariantCulture) + " m de la banqueta en z = " + z.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                        fallos++;
                    }
                    anterior = y;
                }
            }
            sb.AppendLine("Paseo: " + conPiso + " de " + total + " muestras con piso.");
            if (conPiso == 0)
            {
                sb.AppendLine("FALLO: el paseo no tiene piso en ninguna muestra.");
                fallos++;
            }
            else if (conPiso < total * 0.5f)
            {
                sb.AppendLine("AVISO: el paseo quedó a menos de la mitad (peldaños saltados; ver Logs/t1416-aplicar.txt).");
                avisos++;
            }
            Transform bar = c.Nuevo.Find("Barandal");
            sb.AppendLine("Barandal: " + (bar == null ? 0 : bar.childCount) + " piezas.");
            if (bar == null || bar.childCount == 0)
            {
                sb.AppendLine("FALLO: el paseo no tiene barandal del lado de la ciudad.");
                fallos++;
            }
        }

        private static void ValidarColliders(Ctx c, StringBuilder sb, ref int fallos)
        {
            int total = 0, malos = 0;
            foreach (Collider col in c.Nuevo.GetComponentsInChildren<Collider>(true))
            {
                total++;
                if (col.isTrigger)
                {
                    continue;
                }
                Transform unidad = col.transform;
                while (unidad.parent != null && unidad.parent != c.Nuevo && unidad.parent.parent != c.Nuevo)
                {
                    unidad = unidad.parent;
                }
                Renderer[] rs = unidad.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer) && r.enabled).ToArray();
                if (rs.Length == 0)
                {
                    malos++;
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
                    malos++;
                }
                else if (!u.Intersects(col.bounds) || col.bounds.size.x > u.size.x + 0.5f || col.bounds.size.y > u.size.y + 0.5f || col.bounds.size.z > u.size.z + 0.5f)
                {
                    sb.AppendLine("FALLO: el collider sobresale de lo visible: " + Ruta(col.transform));
                    fallos++;
                    malos++;
                }
            }
            sb.AppendLine("Colliders nuevos: " + total + ", sin malla visible o fuera de ella: " + malos + ".");
        }

        private static void ValidarHuellaYVista(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            // Nada activo (que no sea piso, escalera o poste viejo ya considerado) bajo la huella de los peldaños puestos.
            int sobre = 0;
            foreach (Renderer r in Candidatos(c))
            {
                if (EnHuella(r.bounds))
                {
                    sobre++;
                    sb.AppendLine("FALLO: " + Ruta(r.transform) + " sigue activo sobre la huella del paseo.");
                    fallos++;
                }
            }
            sb.AppendLine("Renderers activos sobre la huella del paseo: " + sobre + ".");
            HashSet<Transform> bloqueos = Bloqueos(c);
            foreach (Transform u in bloqueos)
            {
                sb.AppendLine("FALLO: " + Ruta(u) + " tapa la vista a la ciudad entre " + VistaMinima + " y " + VistaMaxima + " m.");
                fallos++;
            }
            int objetivos = ObjetivosCiudad().Count;
            sb.AppendLine("Vista a la ciudad: " + objetivos + " objetivos sintéticos en el lado " + (CiudadALaIzquierda ? "izquierdo" : "derecho") + ", " + bloqueos.Count + " bloqueos.");
            if (objetivos == 0)
            {
                sb.AppendLine("AVISO: ningún objetivo cae en el cuadro; revisa K13bDesde y K13bHacia.");
                avisos++;
            }
            Transform apagados = c.Nuevo.Find("Apagados");
            sb.AppendLine("Apagadas (reversibles): " + (apagados == null ? 0 : apagados.childCount) + ".");
        }

        private static void ValidarAsentado(Ctx c, StringBuilder sb, ref int fallos)
        {
            foreach (string carpeta in new[] { "Carros", "Postes" })
            {
                Transform f = c.Nuevo.Find(carpeta);
                if (f == null)
                {
                    continue;
                }
                foreach (Transform t in f.Cast<Transform>())
                {
                    Bounds b = Cubierta(t);
                    bool poste = carpeta == "Postes";
                    Vector3 p = poste ? t.position : new Vector3(b.center.x, b.min.y, b.center.z);
                    float base0 = poste ? t.position.y : b.min.y;
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
        }

        private static void ValidarCables(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            foreach (MeshFilter mf in c.Zona.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || !(mf.name.Contains("Cable") || mf.sharedMesh.name.Contains("Cable")) || !mf.sharedMesh.isReadable)
                {
                    continue;
                }
                bool nuevo = mf.transform.IsChildOf(c.Nuevo);
                float min = float.MaxValue;
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    min = Mathf.Min(min, Vector3.Distance(mf.transform.TransformPoint(v), K13bDesde));
                }
                if (min < 4f)
                {
                    sb.AppendLine((nuevo ? "FALLO" : "AVISO") + ": cable " + Ruta(mf.transform) + " a " + min.ToString("0.0", CultureInfo.InvariantCulture) + " m de la cámara K13b.");
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
            sb.AppendLine("Cables revisados contra K13b (mínimo 4 m).");
        }

        // ================================================================ Búsqueda de la cámara y fotos

        private sealed class Datos
        {
            public readonly List<Vector3> Ciudad = new List<Vector3>();
            public readonly List<Vector3> Casas = new List<Vector3>();
            public readonly List<Vector3> Escalera = new List<Vector3>();
        }

        private static bool EsEmisivo(Renderer r)
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null && m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor").maxColorComponent > 0.3f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Con la escena abierta: puntos de la ciudad (renderers con emisión y luces al este), casas y escalera del Cerro.</summary>
        private static Datos Reunir()
        {
            Datos d = new Datos();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Vector3 p = r.bounds.center;
                bool enCerro = p.x < -300f && p.x > -480f && p.z > -125f && p.z < 220f;
                if (enCerro)
                {
                    string ruta = Ruta(r.transform);
                    if (ruta.Contains("/Escaleras/") || ruta.Contains("CE01"))
                    {
                        d.Escalera.Add(p);
                    }
                    else if (ruta.Contains("/Casas/"))
                    {
                        d.Casas.Add(p);
                    }
                }
                else if (p.x > -330f && EsEmisivo(r))
                {
                    d.Ciudad.Add(p);
                }
            }
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional && l.transform.position.x > -330f)
                {
                    d.Ciudad.Add(l.transform.position);
                }
            }
            int paso = Mathf.Max(1, d.Ciudad.Count / 3000);
            if (paso > 1)
            {
                List<Vector3> menos = new List<Vector3>();
                for (int i = 0; i < d.Ciudad.Count; i += paso)
                {
                    menos.Add(d.Ciudad[i]);
                }
                d.Ciudad.Clear();
                d.Ciudad.AddRange(menos);
            }
            return d;
        }

        /// <summary>
        /// Puntaje de una cámara (lado = -1 ciudad a la izquierda, +1 a la derecha): % de la ciudad (con luz o emisión, a más de 80 m y abajo de la cámara) que se ve sin
        /// tapar en el lado de la ciudad, más casas y escalera (a 6 - 90 m) en el lado contrario, menos la ciudad que se cuela en el lado contrario.
        /// </summary>
        private static float Puntaje(Datos d, Vector3 cam, Vector3 hacia, int lado, out string detalle)
        {
            Vector3 f = (hacia - cam).normalized;
            if (Physics.CheckSphere(cam, 0.8f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(cam, f, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                detalle = "cámara tapada";
                return -1000f;
            }
            List<Vector3> vistos = new List<Vector3>();
            List<int> lados = new List<int>();
            foreach (Vector3 q in d.Ciudad)
            {
                float xn, yn, prof;
                if ((q - cam).magnitude >= DistanciaCiudad && q.y < cam.y && Proyectar(cam, f, q, out xn, out yn, out prof))
                {
                    vistos.Add(q);
                    lados.Add(xn < 0f ? -1 : 1);
                }
            }
            int stride = Mathf.Max(1, vistos.Count / 300);
            float propia = 0f, ajena = 0f;
            for (int i = 0; i < vistos.Count; i += stride)
            {
                Vector3 dir = vistos[i] - cam;
                float dist = dir.magnitude;
                if (Physics.Raycast(cam, dir / dist, dist - 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if (lados[i] == lado)
                {
                    propia += stride;
                }
                else
                {
                    ajena += stride;
                }
            }
            int casas = 0, escalera = 0;
            foreach (Vector3 q in d.Casas)
            {
                float xn, yn, prof;
                if (Proyectar(cam, f, q, out xn, out yn, out prof) && prof >= VistaMinima && prof <= 90f && (xn < 0f ? -1 : 1) == -lado)
                {
                    casas++;
                }
            }
            foreach (Vector3 q in d.Escalera)
            {
                float xn, yn, prof;
                if (Proyectar(cam, f, q, out xn, out yn, out prof) && prof >= VistaMinima && prof <= 90f)
                {
                    escalera++;
                }
            }
            float total = Mathf.Max(1f, d.Ciudad.Count);
            float puntaje = 100f * propia / total * 20f - 50f * ajena / total * 20f + 0.15f * Mathf.Min(casas, 150) + 0.25f * Mathf.Min(escalera, 60);
            detalle = "ciudad propia " + propia.ToString("0") + ", ajena " + ajena.ToString("0") + " de " + d.Ciudad.Count + "; casas del otro lado " + casas + "; escalera " + escalera;
            return puntaje;
        }

        /// <summary>
        /// Busca la cámara de K13b entre posiciones del carril oeste de la pierna 2 (x -361.5 y -358.5; z de 120 a 170), rumbos de 160 a 200 grados (180 = sur) y dos cabeceos,
        /// con los dos lados posibles para la ciudad. Escribe Logs/t1416-busqueda.txt y devuelve la mejor.
        /// </summary>
        private static void Buscar(Datos d, out Vector3 desde, out Vector3 hacia, out float puntaje, out int lado, StringBuilder log)
        {
            desde = K13bDesde;
            hacia = K13bHacia;
            lado = CiudadALaIzquierda ? -1 : 1;
            string det;
            puntaje = Puntaje(d, desde, hacia, lado, out det);
            log.AppendLine("Constantes (K13bDesde " + K13bDesde.ToString("0.00") + ", K13bHacia " + K13bHacia.ToString("0.00") + "): " + puntaje.ToString("0.0") + " (" + det + ").");
            float mejor = puntaje;
            foreach (float x in new[] { -361.5f, -358.5f })
            {
                for (float z = 120f; z <= 170f; z += 10f)
                {
                    float y = 10f + 0.05f * (175f - z) + 1.7f;
                    Vector3 cam = new Vector3(x, y, z);
                    foreach (float rumbo in new[] { 160f, 168f, 176f, 184f, 192f, 200f })
                    {
                        foreach (float cabeceo in new[] { 0f, -4f })
                        {
                            float r = rumbo * Mathf.Deg2Rad;
                            Vector3 dir = new Vector3(Mathf.Sin(r), Mathf.Tan(cabeceo * Mathf.Deg2Rad), Mathf.Cos(r));
                            Vector3 h = cam + dir.normalized * 92f;
                            foreach (int l in new[] { -1, 1 })
                            {
                                string det2;
                                float p = Puntaje(d, cam, h, l, out det2);
                                if (p > mejor)
                                {
                                    mejor = p;
                                    desde = cam;
                                    hacia = h;
                                    lado = l;
                                    puntaje = p;
                                    log.AppendLine("Mejor hasta ahora: desde " + cam.ToString("0.00") + " hacia " + h.ToString("0.00") + " lado " + l + " puntaje " + p.ToString("0.0") + " (" + det2 + ").");
                                }
                            }
                        }
                    }
                }
            }
            log.AppendLine("ELEGIDA: desde " + desde.ToString("0.00") + " hacia " + hacia.ToString("0.00") + " lado de la ciudad " + (lado < 0 ? "izquierdo" : "derecho") + " puntaje " + puntaje.ToString("0.0") + ".");
        }

        /// <summary>Solo busca y escribe Logs/t1416-busqueda.txt (necesita la escena de la ciudad).</summary>
        [MenuItem("Mecha Cyberpunk/T1416/Cerro K13 ciudad: buscar camara")]
        public static void BuscarCamara()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            StringBuilder log = new StringBuilder("T-1416: búsqueda de la cámara K13b\n");
            Datos d = Reunir();
            log.AppendLine("Ciudad (muestras): " + d.Ciudad.Count + ", casas del Cerro: " + d.Casas.Count + ", escalera: " + d.Escalera.Count + ".");
            Vector3 desde, hacia;
            float puntaje;
            int lado;
            Buscar(d, out desde, out hacia, out puntaje, out lado, log);
            Escribir(RutaBusqueda, log);
            Debug.Log("[ZonaCerroT1416] " + log.ToString());
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        /// <summary>
        /// K13b (busca la mejor cámara cerca de las constantes) con la referencia a la izquierda y el juego a la derecha en docs/capturas/t-1416/cerro-k13.png.
        /// No guarda nada; corre en una corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1416/Cerro K13 ciudad: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1416");
            Physics.SyncTransforms();
            StringBuilder log = new StringBuilder("T-1416: fotos de K13b\n");
            Datos d = Reunir();
            log.AppendLine("Ciudad (muestras): " + d.Ciudad.Count + ", casas del Cerro: " + d.Casas.Count + ", escalera: " + d.Escalera.Count + ".");
            Vector3 desde = K13bDesde, hacia = K13bHacia;
            if (d.Ciudad.Count > 0)
            {
                float puntaje;
                int lado;
                Buscar(d, out desde, out hacia, out puntaje, out lado, log);
            }
            else
            {
                log.AppendLine("AVISO: no se encontró la ciudad iluminada en la escena; se usan las constantes.");
            }
            Escribir(RutaBusqueda, log);
            Debug.Log("[ZonaCerroT1416] " + log.ToString());
            ColocarS2cLook.Foto(desde, hacia, "docs/capturas/t-1416/cerro-k13-juego.png", CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/t-1416/cerro-k13-juego.png", "docs/capturas/t-1416/cerro-k13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        // ================================================================ Ayudas

        private static void Log(string texto)
        {
            if (informe != null)
            {
                informe.AppendLine(texto);
            }
            Debug.Log("[ZonaCerroT1416] " + texto);
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
    }
}
