using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1440: la Colonia del Cerro vista desde arriba (A13) como una LADERA y no como una rejilla de cajas a la misma altura.
    /// Sobre el prefab T1355_Cerro (sin correr el generador) y bajo la raíz propia "T1440_Ladera" (Aplicar la rehace):
    ///  1. Terrazas: sobre las casas de la ladera se apilan pisos extra (1 a 3, según una función de altura que crece hacia el oeste = cuesta arriba) con
    ///     retiro hacia atrás (queda una terraza al frente, como escalones de pastel). Cada piso lleva una escalera exterior de peldaños (o una escalera de
    ///     servicio PuntoTrepar si no cabe), así la regla del parkour (de 0.5 a 2.3 m, o una escalera que una) se conserva. No se mueve ningún collider viejo.
    ///  2. Escaleras largas entre la pierna 3 (x = -415) y la pierna 2 (x = -360) del zigzag: peldaños de menos de 0.2 m, barandal CE03 y lámparas emisivas,
    ///     con muros de contención CC08 a los lados. Se escogen los pasillos donde el terreno no entierra la escalera; las casas del pasillo se APAGAN
    ///     (reversible: T1440_Ladera/Apagados, Aplicar las vuelve a encender antes de rehacer).
    ///  3. Postes de luz a lo largo del zigzag (cada ~22 m, alternando lados): máximo 8 con Light real (los más cercanos a las cámaras), el resto con
    ///     la cabeza emisiva naranja y un charco de luz falso (malla aditiva), cables entre postes.
    ///  4. Ciudad brillante abajo: paneles verticales con la textura T1363_VentanasLejanas (emisiva) y pantallas de neón magenta, turquesa y ámbar a 110-520 m,
    ///     bajo el nivel del Cerro, hacia el lado de la ciudad (este) en el encuadre de A13 y de K13b.
    /// Orden: T1385, T1394, T1401, (T1405), T1406, T1416, T1414 (Aereas), T1440.Aplicar; luego T1440.Validar; Fotos en una corrida de Unity aparte.
    /// </summary>
    public static class ZonaCerroT1440
    {
        public const string RutaPrefab = ZonaCerroT1385.RutaPrefab;
        public const string Raiz = "T1440_Ladera";
        public const string RutaInforme = "Logs/t1440-aplicar.txt";
        public const string RutaValidar = "Logs/t1440-validar.txt";
        public const string CarpetaFotos = "docs/capturas/_vista19b";
        public const string CarpetaMat = "Assets/Arte/Mundo/Cerro";
        public const int Semilla = 14400;
        public const long TopeBytesPrefab = 60L * 1024 * 1024;
        public const int TopeLucesNuevas = 8;
        public const int TopeObjetos = 3000;
        public const int TopeObjetosPisos = 2100;
        public const string NombreCamara = "CamaraA13";
        public const string NombreMira = "Mira";

        // A13b: sobre la ladera, al suroeste de la zona de escaleras, mirando al noreste (la ciudad, al este, queda a la derecha como en la referencia).
        public static readonly Vector3 A13Desde = new Vector3(-420f, 58f, -40f);
        public static readonly Vector3 A13Hacia = new Vector3(-340f, 8f, 35f);
        public const float CampoA13 = 60f;
        public const float DensidadNieblaAerea = 0.008f;
        private const float AnchoFoto = 1376f, AltoFoto = 768f;

        // Pisos extra
        public const float RetiroPrimero = 2.2f;
        public const float RetiroSiguiente = 1.5f;
        public const float FondoMinimo = 2.4f;
        public const float AltoPasoMaximo = 0.2f;
        public const float AnchoPeldanoPiso = 0.9f;
        public const float FondoPeldanoPiso = 0.3f;
        public const float XOesteDeLaCiudad = -358f;
        public const int PisosMaximos = 3;

        // Escaleras de ladera (pierna 3, x = -415, arriba; pierna 2, x = -360, abajo)
        public const float XEscaleraArriba = -409f;
        public const float XEscaleraAbajo = -366f;
        public const float XBanquetaArriba = -409.7f;
        public const float XBanquetaAbajo = -365.3f;
        public const float AnchoEscalera = 3f;
        public const int ModulosRampa = 10;
        public const float LargoModulo = 4f;
        public const float AltoModulo = 2.62f;
        public const float AnchoModulo = 2.3f;
        public const float EntierroMaximo = 0.35f;
        public const float HuecoMaximo = 2.3f;
        public const int EscalerasMaximas = 2;

        // Postes
        public const float SeparacionPostes = 22f;
        public const float DesdeEje = 5.25f;
        public const float DistanciaPosteViejo = 9f;
        public const int LucesPostes = 8;

        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[256];
        private static readonly Collider[] bufCol = new Collider[256];
        private static readonly Dictionary<string, Vector3> medidas = new Dictionary<string, Vector3>();
        private static readonly string[] Protegidos = { "Terreno", "Calles", "Escaleras", "Postes", "Cables", "Carros", "Huecos", "T1406_Cerro", "T1416_Cerro" };

        private sealed class Rend
        {
            public Renderer R;
            public Bounds B;
        }

        private sealed class Casa
        {
            public Transform Raiz, Cimiento;
            public float X0, X1, Z0, Z1, Base, Top, Normal;
            public float Frente => Normal > 0f ? X1 : X0;
            public float Centro => (X0 + X1) * 0.5f;
        }

        private sealed class Ctx
        {
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Terreno, Calles, Nuevo, Pisos, Marcas;
            public KitS2c.Obra Obra;
            public Material Concreto, Tubo, Lampara, Pila;
            public List<Rend> Rends = new List<Rend>();
            public readonly HashSet<Transform> Apagadas = new HashSet<Transform>();
            public int Volumenes, ConPeldanos, ConEscalera, Altura;
            public Malla Peldanos = new Malla();
            public Malla Escalerillas = new Malla();
            public Malla Brillo = new Malla();
            public Malla Charcos = new Malla();
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1440/Cerro ladera: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1440: Cerro en ladera (" + RutaPrefab + ")\n");
            medidas.Clear();
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                int devueltas = Restaurar(zona);
                Log("Apagadas por una corrida anterior y vueltas a encender: " + devueltas + ".");
                Ctx c = Abrir(zona);
                int lucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                int objetosAntes = zona.GetComponentsInChildren<Transform>(true).Length;
                Log("Luces antes: " + lucesAntes + ". Objetos antes: " + objetosAntes + ". Tamaño del prefab antes: " + Megas(RutaPrefab) + " MB.");
                c.Obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                c.Marcas = Hijo(c.Nuevo, "Apagados");
                c.Concreto = AssetDatabase.LoadAssetAtPath<Material>(ZonaCerroT1385.RutaM09) ?? KitS2c.MatBanqueta();
                c.Tubo = MatPropio("T1440_Tubo", () => KitS2c.Lit(new Color(0.3f, 0.17f, 0.11f), 0.3f, KitS2c.TexMuro()));
                c.Lampara = MatPropio("T1440_LamparaNaranja", () => Emisivo(new Color(0.15f, 0.09f, 0.04f), new Color(1f, 0.5f, 0.12f) * 4f));
                c.Pila = MatPropio("T1440_PeldanoPiso", () => KitS2c.Lit(new Color(0.55f, 0.52f, 0.5f), 0.25f, KitS2c.TexMuro()));
                Physics.SyncTransforms();
                c.Rends = Reunir(c);
                Log("Renderers considerados: " + c.Rends.Count + ".");
                Paso("cámara A13", () => Camara(c));
                Paso("escaleras de ladera", () => Escaleras(c));
                Paso("pisos extra", () => Pisos(c));
                Paso("postes y cables", () => Postes(c));
                Paso("ciudad brillante", () => Ciudad(c));
                Paso("mallas sueltas", () => MallasSueltas(c));
                Estatico(c);
                KitS2c.Terminar(c.Obra);
                int luces = zona.GetComponentsInChildren<Light>(true).Length;
                int nuevas = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                int objetos = c.Nuevo.GetComponentsInChildren<Transform>(true).Length;
                Log("Luces después: " + luces + " (nuevas " + nuevas + ", tope " + TopeLucesNuevas + "). Objetos nuevos: " + objetos + " (tope " + TopeObjetos + ").");
                if (nuevas > TopeLucesNuevas || objetos > TopeObjetos)
                {
                    Log("AVISO: se pasó del tope de luces u objetos nuevos.");
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
                Debug.LogError("[ZonaCerroT1440] " + e);
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
                Debug.LogError("[ZonaCerroT1440] " + nombre + ": " + e);
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

        private static string Indices(Ctx c, Transform t)
        {
            List<string> l = new List<string>();
            for (Transform p = t; p != null && p != c.Zona.transform; p = p.parent)
            {
                l.Insert(0, p.GetSiblingIndex().ToString(CultureInfo.InvariantCulture));
            }
            return string.Join("/", l);
        }

        private static void Apagar(Ctx c, Transform unidad, string motivo)
        {
            if (unidad == null || !unidad.gameObject.activeSelf || !c.Apagadas.Add(unidad))
            {
                return;
            }
            string nombre = Indices(c, unidad) + "|" + unidad.name;
            new GameObject(nombre).transform.SetParent(c.Marcas, false);
            unidad.gameObject.SetActive(false);
            Log("Apagada (" + motivo + "): " + nombre);
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

        private static Transform Unidad(Ctx c, Transform t, float limite, float limiteAlto)
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
                if (bp.size.x > limite || bp.size.z > limite || bp.size.y > limiteAlto)
                {
                    break;
                }
                u = p;
            }
            return u;
        }

        /// <summary>Renderers activos de la zona que pueden estorbar (ni piso, ni lo nuevo, ni partículas, ni gigantes).</summary>
        private static List<Rend> Reunir(Ctx c)
        {
            List<Rend> l = new List<Rend>();
            foreach (Renderer r in c.Zona.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r.transform.IsChildOf(c.Nuevo) || EsPiso(c, r.transform))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.x > 60f || b.size.z > 60f || b.size.magnitude < 0.2f)
                {
                    continue;
                }
                l.Add(new Rend { R = r, B = b });
            }
            return l;
        }

        // ================================================================ Piso y cajas libres

        private static bool EsPiso(Ctx c, Transform t)
        {
            return (c.Terreno != null && t.IsChildOf(c.Terreno)) || (c.Calles != null && t.IsChildOf(c.Calles));
        }

        /// <summary>Piso (terreno o calles, no lo nuevo) más alto bajo yDesde.</summary>
        private static bool Piso(Ctx c, float x, float z, float yDesde, bool soloCalles, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, yDesde, z), Vector3.down, bufRayos, 400f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = bufRayos[i].collider.transform;
                if ((c.Nuevo != null && t.IsChildOf(c.Nuevo)) || !EsPiso(c, t))
                {
                    continue;
                }
                if (soloCalles && (c.Calles == null || !t.IsChildOf(c.Calles)))
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

        /// <summary>True si la caja no toca ningún collider que no sea piso ni lo nuevo.</summary>
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

            /// <summary>Cuadrilátero a-b-c-d; la cara visible mira hacia <paramref name="normal"/>. UV en metros (o las dadas).</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2[] uv = null)
            {
                normal.Normalize();
                bool invertir = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f;
                int i0 = V.Count;
                Vector3[] p = { a, b, c, d };
                Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                for (int k = 0; k < 4; k++)
                {
                    Vector3 q = p[k];
                    V.Add(q);
                    N.Add(normal);
                    if (uv != null)
                    {
                        Uv.Add(uv[k]);
                    }
                    else
                    {
                        Uv.Add(abs.y >= abs.x && abs.y >= abs.z ? new Vector2(q.x / 2f, q.z / 2f) : (abs.x >= abs.z ? new Vector2(q.z / 2f, q.y / 2f) : new Vector2(q.x / 2f, q.y / 2f)));
                    }
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

            /// <summary>Caja alineada a los ejes de min a max (6 caras).</summary>
            public void Caja(Vector3 min, Vector3 max)
            {
                Vector3 a = min, b = max;
                Quad(new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, a.z), Vector3.up);
                Quad(new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), Vector3.down);
                Quad(new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, a.y, b.z), Vector3.forward);
                Quad(new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(a.x, b.y, a.z), Vector3.back);
                Quad(new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, a.z), Vector3.right);
                Quad(new Vector3(a.x, a.y, a.z), new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, a.y, b.z), Vector3.left);
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

        /// <summary>Un GameObject con la malla (y su collider igual a ella, si se pide). Devuelve null si la malla está vacía.</summary>
        private static GameObject ObjetoMalla(Ctx c, Transform padre, string nombre, Malla m, Material mat, bool collider, bool sinSombras)
        {
            if (m.T.Count == 0)
            {
                return null;
            }
            Mesh malla = m.Crear("T1440_" + nombre);
            c.Obra.Mallas.Add(malla);
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            if (sinSombras)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            if (collider)
            {
                go.AddComponent<MeshCollider>().sharedMesh = malla;
            }
            return go;
        }

        // ================================================================ Materiales propios (Assets/Arte/Mundo/Cerro)

        private static Material MatPropio(string nombre, Func<Material> crear)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMat))
            {
                AssetDatabase.CreateFolder("Assets/Arte/Mundo", "Cerro");
            }
            string ruta = CarpetaMat + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null)
            {
                return m;
            }
            m = crear();
            m.name = nombre;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        /// <summary>URP/Lit con _EMISSION en tiempo real (con la bandera None se pierde la emisión al reimportar).</summary>
        private static Material Emisivo(Color baseColor, Color emision, Texture mapa = null)
        {
            Material m = KitS2c.Lit(baseColor, 0.1f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision);
            if (mapa != null)
            {
                m.SetTexture("_EmissionMap", mapa);
                m.SetTexture("_BaseMap", mapa);
            }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        /// <summary>Charco de luz falso: textura radial (negro fuera) en un material aditivo sin sombras.</summary>
        private static Material MatCharcoLuz()
        {
            return MatPropio("T1440_CharcoLuz", () =>
            {
                string ruta = CarpetaMat + "/T1440_CharcoLuz.png";
                const int lado = 64;
                Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
                for (int y = 0; y < lado; y++)
                {
                    for (int x = 0; x < lado; x++)
                    {
                        float d = new Vector2((x + 0.5f) / lado - 0.5f, (y + 0.5f) / lado - 0.5f).magnitude * 2f;
                        float v = Mathf.Clamp01(1f - d);
                        v = v * v * (3f - 2f * v);
                        t.SetPixel(x, y, new Color(v, v, v, 1f));
                    }
                }
                t.Apply();
                File.WriteAllBytes(ruta, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(ruta);
                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.sRGBTexture = true;
                imp.SaveAndReimport();
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
                Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                m.SetTexture("_BaseMap", tex);
                m.SetTexture("_MainTex", tex);
                m.SetColor("_BaseColor", new Color(1f, 0.5f, 0.15f, 1f) * 0.75f);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f);
                m.SetFloat("_SrcBlend", 1f);
                m.SetFloat("_DstBlend", 1f);
                m.SetFloat("_SrcBlendAlpha", 1f);
                m.SetFloat("_DstBlendAlpha", 1f);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                m.SetShaderPassEnabled("ShadowCaster", false);
                return m;
            });
        }

        // ================================================================ Cámara A13b

        private static void Camara(Ctx c)
        {
            Transform marca = new GameObject(NombreCamara).transform;
            marca.SetParent(c.Nuevo, false);
            marca.position = A13Desde;
            marca.rotation = Quaternion.LookRotation(A13Hacia - A13Desde);
            Transform mira = new GameObject(NombreMira).transform;
            mira.SetParent(marca, true);
            mira.position = A13Hacia;
            Log("Cámara A13b: desde " + A13Desde.ToString("F1") + " hacia " + A13Hacia.ToString("F1") + " (campo " + CampoA13 + ").");
        }

        // ================================================================ Casas (las de la ladera, activas)

        private static List<Casa> Casas(Ctx c)
        {
            List<Casa> l = new List<Casa>();
            foreach (VariarCerroT1364.CasaT k in VariarCerroT1364.Leer(c.Zona.transform))
            {
                if (k.Raiz == null || !k.Raiz.gameObject.activeInHierarchy)
                {
                    continue;
                }
                l.Add(new Casa { Raiz = k.Raiz, Cimiento = k.Cimiento, X0 = k.X0, X1 = k.X1, Z0 = k.Z0, Z1 = k.Z1, Base = k.Base, Top = k.Top, Normal = k.Normal });
            }
            foreach (string nom in new[] { "T1385_Cerro", "T1394_Cerro" })
            {
                Transform r = c.Zona.transform.Find(nom);
                Transform cs = r == null ? null : r.Find("Casas");
                if (cs == null)
                {
                    continue;
                }
                for (int i = 0; i < cs.childCount; i++)
                {
                    Transform t = cs.GetChild(i);
                    if (!t.name.StartsWith("CC0") || !t.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    Bounds b = Cubierta(t);
                    if (b.size.x < 1f || b.size.z < 1f)
                    {
                        continue;
                    }
                    l.Add(new Casa { Raiz = t, X0 = b.min.x, X1 = b.max.x, Z0 = b.min.z, Z1 = b.max.z, Base = b.min.y, Top = b.max.y, Normal = t.forward.x >= 0f ? 1f : -1f });
                }
            }
            return l;
        }

        // ================================================================ 2. Escaleras largas de ladera

        private sealed class PlanEscalera
        {
            public float Z, YArriba, YAbajo;
            public int Pasos;
            public float Mala; // peor entierro (m) del terreno sobre la escalera
            public float Hueco; // mayor hueco (m) entre la escalera y el terreno
            public readonly HashSet<Transform> Unidades = new HashSet<Transform>();
            public bool Protegido;
        }

        /// <summary>Altura del peldaño bajo x (el peldaño i va de x_i a x_i+1; después de la rampa, el descanso).</summary>
        private static float AlturaEscalera(PlanEscalera p, float x)
        {
            float largoRampa = ModulosRampa * LargoModulo;
            float t = (x - XEscaleraArriba) / largoRampa;
            if (t >= 1f)
            {
                return p.YAbajo;
            }
            int i = Mathf.Clamp(Mathf.FloorToInt(t * p.Pasos), 0, p.Pasos - 1);
            return p.YArriba - i * (p.YArriba - p.YAbajo) / p.Pasos;
        }

        private static PlanEscalera Evaluar(Ctx c, float z)
        {
            PlanEscalera p = new PlanEscalera { Z = z };
            float yA, yB;
            bool okA = Piso(c, XBanquetaArriba, z, 200f, true, out yA);
            bool okB = Piso(c, XBanquetaAbajo, z, 200f, true, out yB);
            if (!okA)
            {
                yA = CerroT1355Trazado.AlturaPierna3(z) + ColocarT1355Cerro.AltoBanqueta;
            }
            if (!okB)
            {
                yB = CerroT1355Trazado.AlturaPierna2(z) + ColocarT1355Cerro.AltoBanqueta;
            }
            p.YArriba = yA;
            p.YAbajo = yB;
            float dy = yA - yB;
            if (dy < 1.5f)
            {
                p.Mala = 99f;
                return p;
            }
            p.Pasos = Mathf.CeilToInt(dy / AltoPasoMaximo);
            for (float x = XEscaleraArriba + 0.5f; x < XEscaleraAbajo - 0.5f; x += 1f)
            {
                foreach (float dz in new[] { -1.5f, 0f, 1.5f })
                {
                    float g;
                    if (!Piso(c, x, z + dz, 200f, false, out g))
                    {
                        continue;
                    }
                    float s = AlturaEscalera(p, x);
                    p.Mala = Mathf.Max(p.Mala, g - s);
                    p.Hueco = Mathf.Max(p.Hueco, s - g);
                }
            }
            // Lo que estorba en el pasillo (de la superficie para arriba): casas se apagan; postes y escaleras viejas lo descartan.
            Bounds pasillo = new Bounds(new Vector3((XEscaleraArriba + XEscaleraAbajo) * 0.5f, (yA + yB) * 0.5f, z), new Vector3(XEscaleraAbajo - XEscaleraArriba, 40f, AnchoEscalera + 2.6f));
            foreach (Rend r in c.Rends)
            {
                if (!r.R.gameObject.activeInHierarchy || !r.B.Intersects(pasillo))
                {
                    continue;
                }
                if (EsProtegido(c, r.R.transform))
                {
                    p.Protegido = true;
                    continue;
                }
                p.Unidades.Add(Unidad(c, r.R.transform, 14f, 30f));
            }
            return p;
        }

        private static void Escaleras(Ctx c)
        {
            List<PlanEscalera> candidatas = new List<PlanEscalera>();
            for (float z = -24f; z <= 36f; z += 3f)
            {
                candidatas.Add(Evaluar(c, z));
            }
            foreach (PlanEscalera p in candidatas)
            {
                Log("Pasillo z = " + p.Z.ToString("0", CultureInfo.InvariantCulture) + ": desnivel " + (p.YArriba - p.YAbajo).ToString("0.0", CultureInfo.InvariantCulture) + " m, terreno sobre la escalera "
                    + p.Mala.ToString("0.00", CultureInfo.InvariantCulture) + " m, hueco " + p.Hueco.ToString("0.0", CultureInfo.InvariantCulture) + " m, casas a apagar " + p.Unidades.Count + (p.Protegido ? ", estorba un poste o escalera vieja" : "") + ".");
            }
            List<PlanEscalera> buenas = candidatas.Where(p => p.Mala <= EntierroMaximo && p.Hueco <= HuecoMaximo && !p.Protegido && p.Pasos > 0 && p.Unidades.Count <= 8)
                .OrderBy(p => p.Unidades.Count).ThenBy(p => p.Mala).ToList();
            List<PlanEscalera> elegidas = new List<PlanEscalera>();
            foreach (PlanEscalera p in buenas)
            {
                if (elegidas.Count < EscalerasMaximas && elegidas.All(e => Mathf.Abs(e.Z - p.Z) >= 12f))
                {
                    elegidas.Add(p);
                }
            }
            if (elegidas.Count == 0)
            {
                Log("AVISO: ningún pasillo entre la pierna 3 y la pierna 2 deja la escalera sin enterrar; no se pone ninguna (ver los pasillos de arriba).");
                return;
            }
            Transform padre = Hijo(c.Nuevo, "EscalerasLadera");
            Malla rampa = new Malla();
            Malla tubos = new Malla();
            Malla brillo = c.Brillo;
            int numero = 0;
            foreach (PlanEscalera p in elegidas.OrderBy(e => e.Z))
            {
                numero++;
                foreach (Transform u in p.Unidades)
                {
                    Apagar(c, u, "pasillo de la escalera " + numero);
                }
                Physics.SyncTransforms();
                ConstruirEscalera(c, padre, p, numero, rampa, tubos, brillo);
            }
            ObjetoMalla(c, padre, "Peldanos", rampa, c.Concreto, true, false);
            ObjetoMalla(c, padre, "TubosBarandal", tubos, c.Tubo, false, true);
            Physics.SyncTransforms();
            Log("Escaleras de ladera: " + elegidas.Count + ".");
        }

        private static void ConstruirEscalera(Ctx c, Transform padre, PlanEscalera p, int numero, Malla rampa, Malla tubos, Malla brillo)
        {
            float z0 = p.Z - AnchoEscalera * 0.5f, z1 = p.Z + AnchoEscalera * 0.5f;
            float largoRampa = ModulosRampa * LargoModulo;
            float huella = largoRampa / p.Pasos;
            float alza = (p.YArriba - p.YAbajo) / p.Pasos;
            for (int i = 0; i < p.Pasos; i++)
            {
                float xa = XEscaleraArriba + i * huella, xb = xa + huella;
                float y = p.YArriba - i * alza;
                float fondo = y - alza - 2.5f;
                rampa.Quad(new Vector3(xa, y, z0), new Vector3(xa, y, z1), new Vector3(xb, y, z1), new Vector3(xb, y, z0), Vector3.up);
                rampa.Quad(new Vector3(xb, y, z0), new Vector3(xb, y, z1), new Vector3(xb, y - alza, z1), new Vector3(xb, y - alza, z0), Vector3.right);
                rampa.Quad(new Vector3(xa, y, z0), new Vector3(xb, y, z0), new Vector3(xb, fondo, z0), new Vector3(xa, fondo, z0), Vector3.back);
                rampa.Quad(new Vector3(xa, y, z1), new Vector3(xb, y, z1), new Vector3(xb, fondo, z1), new Vector3(xa, fondo, z1), Vector3.forward);
            }
            // Descanso de abajo (a la altura de la banqueta de la pierna 2), con su collider.
            float xr = XEscaleraArriba + largoRampa;
            rampa.Caja(new Vector3(xr, p.YAbajo - 2.5f, z0), new Vector3(XEscaleraAbajo, p.YAbajo, z1));
            // Barandal: módulos CE03 (como las escaleras viejas) o tubo propio; lámparas emisivas cada ~5 m.
            GameObject cE03 = AssetDatabase.LoadAssetAtPath<GameObject>(CorrerZonasT1350.RutaModulo("CE03_BarandalTuboEscalera"));
            if (cE03 == null)
            {
                Log("FALTA CE03_BarandalTuboEscalera: el barandal de la escalera " + numero + " es de tubo propio.");
            }
            float subidaModulo = (p.YArriba - p.YAbajo) / ModulosRampa;
            float ys = subidaModulo / AltoModulo;
            for (int i = 0; i < ModulosRampa; i++)
            {
                float t = (i + 0.5f) / ModulosRampa;
                float x = XEscaleraArriba + (i + 0.5f) * LargoModulo;
                float y = Mathf.Lerp(p.YArriba, p.YAbajo, t);
                foreach (float lado in new[] { 1f, -1f })
                {
                    float zr = p.Z + lado * (AnchoEscalera * 0.5f + 0.05f);
                    if (cE03 != null)
                    {
                        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(cE03, padre);
                        g.name = "CE03_Barandal_" + numero + "_" + i + (lado > 0f ? "a" : "b");
                        g.transform.SetPositionAndRotation(new Vector3(x, y - ys, zr), Quaternion.Euler(0f, 180f, 0f));
                        g.transform.localScale = new Vector3(1f, ys, lado > 0f ? -1f : 1f);
                    }
                    else
                    {
                        Vector3 a = new Vector3(x - LargoModulo * 0.5f, y + subidaModulo * 0.5f, zr), b = new Vector3(x + LargoModulo * 0.5f, y - subidaModulo * 0.5f, zr);
                        tubos.Tubo(a + Vector3.up * 1.0f, b + Vector3.up * 1.0f, 0.03f);
                        tubos.Tubo(a + Vector3.up * 0.5f, b + Vector3.up * 0.5f, 0.025f);
                        tubos.Tubo(a, a + Vector3.up * 1.0f, 0.03f);
                    }
                    if (i % 2 == 0 && ((i / 2) % 2 == 0) == (lado > 0f))
                    {
                        // cabeza de farol en el barandal (emisiva naranja)
                        Vector3 cab = new Vector3(x, y + 1.35f, zr);
                        tubos.Tubo(new Vector3(x, y + 0.9f, zr), cab, 0.04f);
                        brillo.Caja(cab - new Vector3(0.18f, 0.1f, 0.18f), cab + new Vector3(0.18f, 0.12f, 0.18f));
                    }
                }
            }
            // Muros de contención CC08 a los lados (hilera de módulos desde el terreno hasta un poco más que la escalera).
            GameObject muro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Zonas/CC/CC08_MuroContencion_a.prefab");
            if (muro == null)
            {
                Log("FALTA CC08_MuroContencion_a.prefab: sin muros a los lados de la escalera " + numero + ".");
                return;
            }
            Vector3 mm = Medida(muro);
            if (mm.x < 0.1f || mm.y < 0.1f || mm.z < 0.1f)
            {
                return;
            }
            Transform muros = Hijo(padre, "Muros_" + numero);
            int puestos = 0;
            float tramo = 6f;
            foreach (float lado in new[] { 1f, -1f })
            {
                float zm = p.Z + lado * (AnchoEscalera * 0.5f + 0.75f);
                for (float x = XEscaleraArriba; x < XEscaleraAbajo - 0.5f; x += tramo)
                {
                    float x1 = Mathf.Min(x + tramo, XEscaleraAbajo);
                    float largo = x1 - x;
                    if (largo < 1.5f)
                    {
                        continue;
                    }
                    float suelo = float.MaxValue;
                    float superficie = float.MinValue;
                    for (float xs = x; xs <= x1 + 0.01f; xs += 1f)
                    {
                        float g;
                        if (Piso(c, xs, zm, 200f, false, out g))
                        {
                            suelo = Mathf.Min(suelo, g);
                        }
                        superficie = Mathf.Max(superficie, AlturaEscalera(p, xs));
                    }
                    if (suelo == float.MaxValue)
                    {
                        continue;
                    }
                    float alto = superficie + 0.9f - suelo;
                    if (alto < 0.8f || alto > 6f)
                    {
                        continue;
                    }
                    GameObject g2 = (GameObject)PrefabUtility.InstantiatePrefab(muro, muros);
                    g2.name = "CC08_Muro_" + numero + "_" + puestos;
                    g2.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    g2.transform.localScale = new Vector3(largo / mm.x, alto / mm.y, 1f);
                    Bounds b = Cubierta(g2.transform);
                    g2.transform.position += new Vector3((x + x1) * 0.5f - b.center.x, suelo - 0.25f - b.min.y, zm - b.center.z);
                    Colisionar(g2);
                    puestos++;
                }
            }
            Physics.SyncTransforms();
            Log("Escalera " + numero + " (z " + p.Z.ToString("0", CultureInfo.InvariantCulture) + "): " + p.Pasos + " peldaños de " + alza.ToString("0.000", CultureInfo.InvariantCulture) + " m de alza y "
                + huella.ToString("0.00", CultureInfo.InvariantCulture) + " m de huella, de y " + p.YArriba.ToString("0.00", CultureInfo.InvariantCulture) + " a " + p.YAbajo.ToString("0.00", CultureInfo.InvariantCulture)
                + "; " + puestos + " muros CC08.");
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
                if (mf.sharedMesh != null && mf.GetComponent<Renderer>() != null)
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
        }

        // ================================================================ 1. Pisos extra (terrazas)

        private static float TiersDe(Casa k)
        {
            if (k.Centro > XOesteDeLaCiudad)
            {
                return 0f; // lado de la ciudad: no se tapa la vista de K13b
            }
            float u = Mathf.Clamp01((-318f - k.Centro) / 130f);
            float baseT = 3.2f * Mathf.Pow(u, 0.9f);
            int h = Mathf.FloorToInt((k.Z0 + k.Z1) * 0.5f / 28f) * 73856093 ^ Mathf.FloorToInt(k.Centro / 34f) * 19349663;
            float ruido = ((Mathf.Abs(h) % 3) - 1) * 0.7f;
            return Mathf.Clamp(Mathf.Round(baseT + ruido), 0f, PisosMaximos);
        }

        /// <summary>Azotea (y) de la casa: la altura que comparten al menos 3 de 5 rayos contra sus colliders.</summary>
        private static bool Azotea(Ctx c, Casa k, out float y)
        {
            y = 0f;
            List<float> ys = new List<float>();
            foreach ((float fx, float fz) in new[] { (0.5f, 0.5f), (0.3f, 0.3f), (0.7f, 0.3f), (0.3f, 0.7f), (0.7f, 0.7f) })
            {
                float x = Mathf.Lerp(k.X0, k.X1, fx), z = Mathf.Lerp(k.Z0, k.Z1, fz);
                int n = c.Fisica.Raycast(new Vector3(x, k.Top + 3f, z), Vector3.down, bufRayos, 40f, ~0, QueryTriggerInteraction.Ignore);
                float mejor = float.MinValue;
                for (int i = 0; i < n; i++)
                {
                    if (bufRayos[i].collider.transform.IsChildOf(k.Raiz) && bufRayos[i].point.y > mejor)
                    {
                        mejor = bufRayos[i].point.y;
                    }
                }
                if (mejor > float.MinValue)
                {
                    ys.Add(mejor);
                }
            }
            if (ys.Count < 3)
            {
                return false;
            }
            int mejorN = 0;
            float mejorY = 0f;
            foreach (float a in ys)
            {
                List<float> cerca = ys.Where(b => Mathf.Abs(b - a) < 0.1f).ToList();
                if (cerca.Count > mejorN)
                {
                    mejorN = cerca.Count;
                    mejorY = cerca.Average();
                }
            }
            if (mejorN < 3)
            {
                return false;
            }
            y = mejorY;
            return true;
        }

        /// <summary>Lo que estorba en la caja del piso nuevo: quita (apaga) los adornos sueltos de la azotea y devuelve false si hay algo que no se puede quitar.</summary>
        private static bool Despejar(Ctx c, Casa k, Bounds caja, float azotea, List<Transform> aApagar)
        {
            foreach (Rend r in c.Rends)
            {
                if (!r.R.gameObject.activeInHierarchy || !r.B.Intersects(caja) || r.R.transform.IsChildOf(k.Raiz) || c.Apagadas.Contains(r.R.transform))
                {
                    continue;
                }
                if (EsProtegido(c, r.R.transform))
                {
                    return false;
                }
                if (r.B.min.y < azotea - 0.4f || r.B.size.x > 5f || r.B.size.z > 5f)
                {
                    return false;
                }
                Transform u = Unidad(c, r.R.transform, 4.5f, 6f);
                if (!aApagar.Contains(u))
                {
                    aApagar.Add(u);
                }
            }
            int n = c.Fisica.OverlapBox(caja.center, caja.extents, bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = bufCol[i].transform;
                if (EsPiso(c, t) || t.IsChildOf(c.Nuevo) || t.IsChildOf(k.Raiz) || bufCol[i].isTrigger)
                {
                    continue;
                }
                Bounds bc = bufCol[i].bounds;
                if (bc.min.y < azotea - 0.4f || bc.size.x > 5f || bc.size.z > 5f || EsProtegido(c, t))
                {
                    return false;
                }
                Transform u = Unidad(c, t, 4.5f, 6f);
                if (!aApagar.Contains(u))
                {
                    aApagar.Add(u);
                }
            }
            return true;
        }

        private static void Pisos(Ctx c)
        {
            c.Pisos = Hijo(c.Nuevo, "Pisos");
            List<Casa> casas = Casas(c);
            Vector3 k13 = new Vector3(-360f, 13f, 100f);
            List<Casa> orden = casas.Where(k => TiersDe(k) >= 1f).OrderBy(k =>
            {
                Vector3 centro = new Vector3(k.Centro, k.Top, (k.Z0 + k.Z1) * 0.5f);
                return Mathf.Min(Vector3.Distance(centro, A13Hacia), Vector3.Distance(centro, k13));
            }).ToList();
            Log("Casas de la ladera: " + casas.Count + ", con pisos extra planeados: " + orden.Count + ".");
            int sinAzotea = 0, estrechas = 0, estorbo = 0, hechas = 0;
            foreach (Casa k in orden)
            {
                if (c.Nuevo.GetComponentsInChildren<Transform>(true).Length > TopeObjetosPisos)
                {
                    Log("Tope de objetos de los pisos (" + TopeObjetosPisos + "): se detiene.");
                    break;
                }
                float w = k.X1 - k.X0, ancho = k.Z1 - k.Z0;
                if (w < 5.5f || ancho < 4.2f)
                {
                    estrechas++;
                    continue;
                }
                float roof;
                if (!Azotea(c, k, out roof))
                {
                    sinAzotea++;
                    continue;
                }
                int tiers = (int)TiersDe(k);
                float xAtras = k.Normal > 0f ? k.X0 : k.X1;
                float xF = k.Frente - k.Normal * RetiroPrimero;
                float zc = (k.Z0 + k.Z1) * 0.5f;
                float anchoV = ancho - 0.6f;
                int puestos = 0;
                for (int t = 1; t <= tiers; t++)
                {
                    float fondo = Mathf.Abs(xF - xAtras) - 0.1f;
                    if (fondo < FondoMinimo)
                    {
                        break;
                    }
                    Vector3 mitad = new Vector3(fondo * 0.5f - 0.05f, 1.65f, anchoV * 0.5f - 0.05f);
                    Bounds caja = new Bounds(new Vector3(xF - k.Normal * fondo * 0.5f, roof + 0.1f + mitad.y, zc), mitad * 2f);
                    List<Transform> aApagar = new List<Transform>();
                    if (!Despejar(c, k, caja, roof, aApagar))
                    {
                        estorbo++;
                        break;
                    }
                    foreach (Transform u in aApagar)
                    {
                        Apagar(c, u, "adorno de azotea bajo un piso nuevo");
                    }
                    if (aApagar.Count > 0)
                    {
                        Physics.SyncTransforms();
                    }
                    KitS2c.Estilo e = new KitS2c.Estilo
                    {
                        PlantaBaja = KitS2c.PisoFachada,
                        PisoAlto = KitS2c.PisoFachada,
                        ColorPiso = c.Obra.Azar.Next(KitS2c.ColoresPiso.Length),
                        Tinaco = false,
                        Foco = false,
                        Neon = null,
                        Antena = false,
                        AireAzotea = false,
                        LucesVentanas = 0.5f,
                        Pisos = 1,
                        Toldo = false,
                    };
                    float alto = KitS2c.Casa(c.Obra, c.Pisos, new Vector3(xF, roof, zc), new Vector3(k.Normal, 0f, 0f), anchoV, fondo, e);
                    Transform nuevo = c.Pisos.GetChild(c.Pisos.childCount - 1);
                    nuevo.name = "Piso_" + c.Volumenes + "_t" + t;
                    c.Volumenes++;
                    puestos++;
                    Acceso(c, k, nuevo, xF, roof, alto - 0.02f, zc, anchoV, t == 1 ? RetiroPrimero : RetiroSiguiente);
                    Physics.SyncTransforms();
                    roof += alto - 0.02f;
                    xF -= k.Normal * RetiroSiguiente;
                    c.Altura += 1;
                }
                if (puestos > 0)
                {
                    hechas++;
                }
            }
            Log("Pisos extra: " + c.Volumenes + " volúmenes en " + hechas + " casas (" + c.ConPeldanos + " con escalera de peldaños, " + c.ConEscalera + " con escalera de servicio); casas saltadas: "
                + sinAzotea + " sin azotea medible, " + estrechas + " muy estrechas, " + estorbo + " con algo que estorbaba.");
        }

        /// <summary>
        /// Peldaños de concreto sobre la terraza al frente (alza ≤ 0.2 m, huella 0.3 m) hasta la azotea del piso nuevo; si no caben a lo largo del frente,
        /// una escalera de servicio PuntoTrepar con su riel y peldaños (como las de T-1355).
        /// </summary>
        private static void Acceso(Ctx c, Casa k, Transform piso, float xF, float roofAbajo, float alto, float zc, float anchoV, float terraza)
        {
            int pasos = Mathf.Max(1, Mathf.CeilToInt(alto / AltoPasoMaximo));
            float alza = alto / pasos;
            float largo = pasos * FondoPeldanoPiso;
            bool cabe = largo <= anchoV - 0.4f && terraza >= AnchoPeldanoPiso + 0.2f;
            float n = k.Normal;
            if (cabe)
            {
                float d = c.Obra.Chance(0.5f) ? 1f : -1f;
                float zTop = zc + d * (anchoV * 0.5f - 0.3f);
                float xb = xF + n * AnchoPeldanoPiso;
                float x0 = Mathf.Min(xF, xb), x1 = Mathf.Max(xF, xb);
                float zMin = Mathf.Min(zTop, zTop - d * largo), zMax = Mathf.Max(zTop, zTop - d * largo);
                Bounds cajaPeldanos = new Bounds(new Vector3((x0 + x1) * 0.5f, roofAbajo + 0.1f + (alto - 0.1f) * 0.5f, (zMin + zMax) * 0.5f), new Vector3(x1 - x0, alto - 0.1f, zMax - zMin));
                List<Transform> aApagar = new List<Transform>();
                if (Despejar(c, k, cajaPeldanos, roofAbajo, aApagar))
                {
                    foreach (Transform u in aApagar)
                    {
                        Apagar(c, u, "adorno de la terraza bajo los peldaños");
                    }
                    // El peldaño 0 (junto al piso nuevo) es el más alto; el último, el más bajo, sale a la azotea de abajo.
                    for (int i = 0; i < pasos; i++)
                    {
                        float za = zTop - d * ((i + 1) * FondoPeldanoPiso), zb = zTop - d * (i * FondoPeldanoPiso);
                        float y = roofAbajo + (pasos - i) * alza;
                        c.Peldanos.Caja(new Vector3(x0, roofAbajo, Mathf.Min(za, zb)), new Vector3(x1, y, Mathf.Max(za, zb)));
                    }
                    c.ConPeldanos++;
                    return;
                }
            }
            Vector3 pos = new Vector3(xF + n * 0.05f, roofAbajo, zc + anchoV * 0.5f - 0.5f);
            GameObject go = new GameObject("Acceso_" + piso.name);
            go.transform.SetParent(piso, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(new Vector3(-n, 0f, 0f)));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
            so.FindProperty("alturaSubida").floatValue = alto;
            so.FindProperty("salida").vector3Value = new Vector3(0f, alto, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
            foreach (float lado in new[] { -0.25f, 0.25f })
            {
                c.Escalerillas.Caja(new Vector3(xF + n * 0.15f - 0.03f, roofAbajo, pos.z + lado - 0.03f), new Vector3(xF + n * 0.15f + 0.03f, roofAbajo + alto, pos.z + lado + 0.03f));
            }
            for (float y = 0.4f; y < alto; y += 0.5f)
            {
                c.Escalerillas.Caja(new Vector3(xF + n * 0.15f - 0.025f, roofAbajo + y - 0.025f, pos.z - 0.25f), new Vector3(xF + n * 0.15f + 0.025f, roofAbajo + y + 0.025f, pos.z + 0.25f));
            }
            c.ConEscalera++;
        }

        private static void MallasSueltas(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Accesos");
            ObjetoMalla(c, padre, "PeldanosPisos", c.Peldanos, c.Pila, true, false);
            ObjetoMalla(c, padre, "EscalerillasServicio", c.Escalerillas, c.Tubo, false, true);
            Transform luz = Hijo(c.Nuevo, "LuzNaranja");
            ObjetoMalla(c, luz, "CabezasEmisivas", c.Brillo, c.Lampara, false, true);
            ObjetoMalla(c, luz, "CharcosLuz", c.Charcos, MatCharcoLuz(), false, true);
        }

        // ================================================================ 3. Postes a lo largo del zigzag

        private sealed class PostePlan
        {
            public Vector3 Pie, HaciaCalle;
            public float Dist;
            public int Pierna;
        }

        private static void Postes(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Postes");
            if (KitS2c.Prefab("C07_PosteLuz") == null)
            {
                Log("FALTA C07_PosteLuz: sin postes nuevos.");
                return;
            }
            List<Vector3> viejos = new List<Vector3>();
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (!t.IsChildOf(c.Nuevo) && (t.name.StartsWith("Poste_") || t.name.Contains("C07_PosteLuz")))
                {
                    viejos.Add(t.position);
                }
            }
            List<Vector3> eje = CerroT1355Trazado.Zigzag();
            List<PostePlan> plan = new List<PostePlan>();
            float acumulado = SeparacionPostes * 0.5f;
            int lado = 1, descartados = 0;
            Vector3 k13 = ZonaCerroT1416.K13bDesde;
            for (int i = 1; i + 1 < eje.Count; i++)
            {
                acumulado += new Vector2(eje[i].x - eje[i - 1].x, eje[i].z - eje[i - 1].z).magnitude;
                if (acumulado < SeparacionPostes)
                {
                    continue;
                }
                acumulado = 0f;
                Vector3 tan = new Vector3(eje[i + 1].x - eje[i - 1].x, 0f, eje[i + 1].z - eje[i - 1].z);
                if (tan.sqrMagnitude < 0.01f)
                {
                    continue;
                }
                tan.Normalize();
                Vector3 izq = new Vector3(-tan.z, 0f, tan.x);
                lado = -lado;
                Vector3 xz = new Vector3(eje[i].x, 0f, eje[i].z) + izq * lado * DesdeEje;
                float y;
                if (!Piso(c, xz.x, xz.z, 200f, true, out y))
                {
                    descartados++;
                    continue;
                }
                Vector3 pie = new Vector3(xz.x, y, xz.z);
                if (viejos.Any(v => new Vector2(v.x - pie.x, v.z - pie.z).magnitude < DistanciaPosteViejo) || !LibreCaja(c, pie + Vector3.up * 2f, new Vector3(0.35f, 2f, 0.35f)))
                {
                    descartados++;
                    continue;
                }
                plan.Add(new PostePlan { Pie = pie, HaciaCalle = -izq * lado, Dist = Mathf.Min(Vector3.Distance(pie, A13Hacia), Vector3.Distance(pie, k13)), Pierna = i });
            }
            Log("Postes planeados: " + plan.Count + " (" + descartados + " descartados por poste viejo a menos de " + DistanciaPosteViejo + " m, sin calle o con algo en el lugar).");
            HashSet<PostePlan> conLuz = new HashSet<PostePlan>(plan.OrderBy(p => p.Dist).Take(LucesPostes));
            List<Vector3> puntas = new List<Vector3>();
            List<int> piernas = new List<int>();
            int reales = 0, baratos = 0;
            foreach (PostePlan p in plan)
            {
                Vector3 punta, cabeza;
                if (conLuz.Contains(p))
                {
                    punta = KitS2c.Poste(c.Obra, padre, p.Pie, p.HaciaCalle, p.Pie.y, reales % 2 == 0);
                    reales++;
                    cabeza = punta - Vector3.up * 0.15f + p.HaciaCalle * 1.0f;
                }
                else
                {
                    punta = PosteBarato(c, padre, p, out cabeza);
                    baratos++;
                }
                c.Charcos.Quad(new Vector3(cabeza.x - 4.5f, p.Pie.y + 0.04f, cabeza.z - 4.5f), new Vector3(cabeza.x - 4.5f, p.Pie.y + 0.04f, cabeza.z + 4.5f), new Vector3(cabeza.x + 4.5f, p.Pie.y + 0.04f, cabeza.z + 4.5f),
                    new Vector3(cabeza.x + 4.5f, p.Pie.y + 0.04f, cabeza.z - 4.5f), Vector3.up, new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
                puntas.Add(punta);
                piernas.Add(p.Pierna);
            }
            int cables = 0;
            for (int i = 0; i + 1 < puntas.Count; i++)
            {
                float d = Vector3.Distance(puntas[i], puntas[i + 1]);
                if (d > 34f || d < 3f)
                {
                    continue;
                }
                foreach ((Vector3 a, Vector3 b, float flecha) in new[]
                {
                    (puntas[i] + Vector3.up * 0.2f, puntas[i + 1] + Vector3.up * 0.2f, 0.8f),
                    (puntas[i] - Vector3.up * 0.35f, puntas[i + 1] - Vector3.up * 0.35f, 1.0f),
                })
                {
                    bool lejos = true;
                    foreach (Vector3 q in CalculoCatenaria.Puntos(a, b, flecha, 12))
                    {
                        if (Vector3.Distance(q, k13) < 4.5f)
                        {
                            lejos = false;
                        }
                    }
                    if (lejos)
                    {
                        KitS2c.Cable(c.Obra, a, b, flecha);
                        cables++;
                    }
                }
            }
            Log("Postes nuevos: " + plan.Count + " (" + reales + " con Light real y sodio, " + baratos + " con cabeza emisiva y charco falso), cables " + cables + ".");
        }

        /// <summary>Poste C07 sin Light: la cabeza lleva un bloque emisivo naranja (malla común) y el charco de luz falso va aparte.</summary>
        private static Vector3 PosteBarato(Ctx c, Transform padre, PostePlan p, out Vector3 cabeza)
        {
            c.Obra.Postes++;
            GameObject poste = KitS2c.Pieza(padre, "C07_PosteLuz", p.Pie, Quaternion.identity, Vector3.one);
            poste.name = "PosteLejano_" + c.Obra.Postes;
            MeshFilter mf = poste.GetComponentsInChildren<MeshFilter>().OrderByDescending(m => m.sharedMesh.vertexCount).First();
            if (ColocarS2.CabezaLampara(mf, out cabeza))
            {
                Vector3 brazo = cabeza - p.Pie;
                brazo.y = 0f;
                float giro = Vector3.SignedAngle(brazo, p.HaciaCalle, Vector3.up);
                poste.transform.rotation = Quaternion.Euler(0f, giro, 0f);
                ColocarS2.CabezaLampara(mf, out cabeza);
            }
            else
            {
                cabeza = p.Pie + Vector3.up * 6.6f + p.HaciaCalle * 1.2f;
            }
            foreach (Renderer r in poste.GetComponentsInChildren<Renderer>())
            {
                r.renderingLayerMask = 1u << ColocarS2.CapaPostes;
            }
            c.Brillo.Caja(cabeza - new Vector3(0.28f, 0.2f, 0.28f), cabeza + new Vector3(0.28f, -0.08f, 0.28f));
            return p.Pie + Vector3.up * 6.75f;
        }

        // ================================================================ 4. Ciudad brillante abajo

        private static void Ciudad(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "CiudadLejana");
            Texture2D ventanas = AssetDatabase.LoadAssetAtPath<Texture2D>(ColocarT1354Malecon.RutaTexVentanasLejanas);
            if (ventanas == null)
            {
                Log("No está " + ColocarT1354Malecon.RutaTexVentanasLejanas + ": se genera con ColocarT1354Malecon.TexturaVentanasLejanas.");
                ventanas = ColocarT1354Malecon.TexturaVentanasLejanas();
            }
            Material paneles = MatPropio("T1440_VentanasCiudad", () =>
            {
                Material m = Emisivo(new Color(0.02f, 0.022f, 0.03f), new Color(2.4f, 2.4f, 2.4f), ventanas);
                m.SetColor("_BaseColor", new Color(0.35f, 0.35f, 0.4f));
                return m;
            });
            Material magenta = MatPropio("T1440_PantallaMagenta", () => Emisivo(new Color(0.05f, 0.01f, 0.04f), new Color(1f, 0.2f, 0.78f) * 3.5f));
            Material turquesa = MatPropio("T1440_PantallaTurquesa", () => Emisivo(new Color(0.01f, 0.05f, 0.05f), new Color(0.1f, 0.95f, 0.88f) * 3.5f));
            Material ambar = MatPropio("T1440_PantallaAmbar", () => Emisivo(new Color(0.05f, 0.03f, 0.01f), new Color(1f, 0.6f, 0.15f) * 3.5f));
            Malla torres = new Malla();
            Malla[] pantallas = { new Malla(), new Malla(), new Malla() };
            System.Random azar = new System.Random(Semilla + 7);
            Vector3 k13 = ZonaCerroT1416.K13bDesde;
            Vector3 horizA = A13Hacia - A13Desde;
            horizA.y = 0f;
            horizA.Normalize();
            int hechas = 0;
            for (int i = 0; i < 110; i++)
            {
                bool paraK13 = i >= 72;
                Vector3 origen = paraK13 ? k13 : A13Desde;
                Vector3 dir;
                float dist;
                if (paraK13)
                {
                    // K13b mira al sur: la ciudad queda a la izquierda (este).
                    float ang = -(float)azar.NextDouble() * 70f;
                    dir = Quaternion.Euler(0f, 180f + ang, 0f) * Vector3.forward;
                    dist = 110f + (float)azar.NextDouble() * 270f;
                }
                else
                {
                    float ang = ((float)azar.NextDouble() * 2f - 1f) * 52f;
                    dir = Quaternion.Euler(0f, ang, 0f) * horizA;
                    dist = 190f + (float)azar.NextDouble() * 330f;
                }
                Vector3 centro = new Vector3(origen.x + dir.x * dist, 0f, origen.z + dir.z * dist);
                if (centro.x < -262f)
                {
                    continue; // dentro o pegada a la ladera
                }
                float ancho = 12f + (float)azar.NextDouble() * 18f;
                float alto = 28f + (float)azar.NextDouble() * 70f + dist * 0.06f;
                float suelo = -8f + (float)azar.NextDouble() * 8f;
                Vector3 normal = new Vector3(origen.x - centro.x, 0f, origen.z - centro.z).normalized;
                Vector3 derecha = Vector3.Cross(Vector3.up, normal).normalized;
                Vector3 a = centro - derecha * ancho * 0.5f + Vector3.up * suelo, b = centro + derecha * ancho * 0.5f + Vector3.up * suelo;
                Vector3 a2 = a + Vector3.up * alto, b2 = b + Vector3.up * alto;
                float u0 = (float)azar.NextDouble(), v0 = (float)azar.NextDouble();
                float du = ancho / 48f, dv = alto / 48f;
                Vector2[] uv = { new Vector2(u0, v0), new Vector2(u0 + du, v0), new Vector2(u0 + du, v0 + dv), new Vector2(u0, v0 + dv) };
                torres.Quad(a, b, b2, a2, normal, uv);
                torres.Quad(a, b, b2, a2, -normal, uv);
                if (azar.NextDouble() < 0.4)
                {
                    int tipo = azar.Next(3);
                    Vector3 pc = centro + normal * 0.3f + Vector3.up * (suelo + alto * (0.35f + (float)azar.NextDouble() * 0.5f));
                    float aw = ancho * 0.28f, ah = 4f + (float)azar.NextDouble() * 10f;
                    pantallas[tipo].Quad(pc - derecha * aw * 0.5f - Vector3.up * ah * 0.5f, pc + derecha * aw * 0.5f - Vector3.up * ah * 0.5f, pc + derecha * aw * 0.5f + Vector3.up * ah * 0.5f,
                        pc - derecha * aw * 0.5f + Vector3.up * ah * 0.5f, normal, new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                }
                hechas++;
            }
            ObjetoMalla(c, padre, "PanelesVentanas", torres, paneles, false, true);
            ObjetoMalla(c, padre, "PantallasMagenta", pantallas[0], magenta, false, true);
            ObjetoMalla(c, padre, "PantallasTurquesa", pantallas[1], turquesa, false, true);
            ObjetoMalla(c, padre, "PantallasAmbar", pantallas[2], ambar, false, true);
            Log("Ciudad lejana: " + hechas + " paneles de ventanas (110 a 520 m, base entre -8 y 0 m, bajo el Cerro), sin colliders ni sombras. SK01_Skyline ya lo pone la escena (arco de 900 m); aquí no se repite.");
        }

        // ================================================================ Estático

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t == c.Nuevo || t.GetComponent<Light>() != null || t.GetComponentInParent<Rigidbody>() != null || t.GetComponent<MeshRenderer>() == null)
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
        /// Escribe Logs/t1440-validar.txt: pisos asentados (5 cm) y sin encimarse, escaleras (alza ≤ 0.3 m, sin casas encima, enlazadas a las banquetas), postes asentados y fuera del carril,
        /// colliders con su malla visible, accesos a cada piso (peldaños o PuntoTrepar), luces (tope 8, sin sombras), objetos (tope 3000) y MB (tope 60).
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1440/Cerro ladera: validar")]
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1440: validación de " + RutaPrefab + "\n");
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
                    Physics.SyncTransforms();
                    ValidarPisos(c, sb, ref fallos, ref avisos);
                    ValidarEscaleras(c, sb, ref fallos, ref avisos);
                    ValidarPostes(c, sb, ref fallos, ref avisos);
                    ValidarColliders(c, sb, ref fallos);
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
                    int objetos = c.Nuevo.GetComponentsInChildren<Transform>(true).Length;
                    sb.AppendLine("Objetos nuevos: " + objetos + " (tope " + TopeObjetos + ").");
                    if (objetos > TopeObjetos)
                    {
                        sb.AppendLine("FALLO: demasiados objetos nuevos.");
                        fallos++;
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
            Debug.Log("[ZonaCerroT1440] Validar: " + fallos + " fallos, " + avisos + " avisos (" + RutaValidar + ").");
        }

        private static void ValidarPisos(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            Transform pisos = c.Nuevo.Find("Pisos");
            if (pisos == null)
            {
                sb.AppendLine("AVISO: no hay pisos extra.");
                avisos++;
                return;
            }
            int total = 0, flotan = 0, enterrados = 0, encimados = 0, sinAcceso = 0;
            List<Transform> lista = pisos.Cast<Transform>().Where(t => t.name.StartsWith("Piso_")).ToList();
            foreach (Transform p in lista)
            {
                total++;
                Transform cuerpo = p.Find("Cuerpo");
                BoxCollider col = cuerpo == null ? null : cuerpo.GetComponent<BoxCollider>();
                if (col == null)
                {
                    sb.AppendLine("FALLO: " + p.name + " sin Cuerpo con collider.");
                    fallos++;
                    continue;
                }
                Bounds b = col.bounds;
                // Asentado: el rayo desde justo sobre la base del cuerpo, hacia abajo, debe tocar algo (no del propio piso) a menos de 5 cm.
                bool tocaAbajo = false;
                float peor = 0f;
                foreach ((float fx, float fz) in new[] { (0.5f, 0.5f), (0.2f, 0.2f), (0.8f, 0.2f), (0.2f, 0.8f), (0.8f, 0.8f) })
                {
                    Vector3 o = new Vector3(Mathf.Lerp(b.min.x, b.max.x, fx), b.min.y + 0.3f, Mathf.Lerp(b.min.z, b.max.z, fz));
                    int n = c.Fisica.Raycast(o, Vector3.down, bufRayos, 3f, ~0, QueryTriggerInteraction.Ignore);
                    float mejor = float.MinValue;
                    for (int i = 0; i < n; i++)
                    {
                        if (!bufRayos[i].collider.transform.IsChildOf(p) && bufRayos[i].point.y > mejor)
                        {
                            mejor = bufRayos[i].point.y;
                        }
                    }
                    if (mejor > float.MinValue)
                    {
                        tocaAbajo = true;
                        peor = Mathf.Max(peor, Mathf.Abs(mejor - b.min.y));
                    }
                }
                if (!tocaAbajo || peor > 0.05f)
                {
                    if (!tocaAbajo || peor > 0.3f)
                    {
                        flotan++;
                        sb.AppendLine("FALLO: " + p.name + " flota o está enterrado (diferencia " + peor.ToString("0.00", CultureInfo.InvariantCulture) + " m).");
                        fallos++;
                    }
                    else
                    {
                        enterrados++;
                        sb.AppendLine("AVISO: " + p.name + " desajustado " + peor.ToString("0.000", CultureInfo.InvariantCulture) + " m (la azotea de abajo tiene losa de 2 cm).");
                        avisos++;
                    }
                }
                // Encimado: ningún collider ajeno dentro del cuerpo (se encoge 8 cm por lado).
                int k = c.Fisica.OverlapBox(b.center, b.extents - new Vector3(0.08f, 0.08f, 0.08f), bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < k; i++)
                {
                    Transform t = bufCol[i].transform;
                    if (t.IsChildOf(p) || bufCol[i].isTrigger || EsPiso(c, t))
                    {
                        continue;
                    }
                    Bounds bc = bufCol[i].bounds;
                    // El piso de abajo (su azotea) y los peldaños propios no cuentan; sí cualquier otra cosa.
                    if (bc.min.y > b.min.y - 0.05f && !t.IsChildOf(c.Nuevo))
                    {
                        encimados++;
                        sb.AppendLine("FALLO: " + p.name + " se encima con " + Ruta(t));
                        fallos++;
                        break;
                    }
                }
                // Acceso: peldaños comunes (mallas "PeldanosPisos") o un PuntoTrepar propio.
                bool conTrepar = p.GetComponentInChildren<PuntoTrepar>(true) != null;
                if (!conTrepar)
                {
                    Transform pel = c.Nuevo.Find("Accesos/PeldanosPisos");
                    bool cerca = false;
                    if (pel != null)
                    {
                        MeshCollider mc = pel.GetComponent<MeshCollider>();
                        Bounds ex = b;
                        ex.Expand(new Vector3(2f, 0.5f, 0.5f));
                        cerca = mc != null && mc.bounds.Intersects(ex);
                    }
                    if (!cerca)
                    {
                        sinAcceso++;
                        sb.AppendLine("FALLO: " + p.name + " sin escalera de peldaños ni PuntoTrepar.");
                        fallos++;
                    }
                }
            }
            sb.AppendLine("Pisos extra: " + total + " (flotan o enterrados " + flotan + ", desajustes menores " + enterrados + ", encimados " + encimados + ", sin acceso " + sinAcceso + ").");
        }

        private static void ValidarEscaleras(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            Transform padre = c.Nuevo.Find("EscalerasLadera");
            Transform pel = padre == null ? null : padre.Find("Peldanos");
            MeshCollider mc = pel == null ? null : pel.GetComponent<MeshCollider>();
            if (mc == null)
            {
                sb.AppendLine("AVISO: no hay escaleras de ladera (ningún pasillo sin enterrar; ver Logs/t1440-aplicar.txt).");
                avisos++;
                return;
            }
            // Cada escalera es una banda z (grosor 3 m). Se detecta por los muros / por el collider: se muestrea por z en pasos de 1.5 m dentro de sus bounds.
            Bounds bb = mc.bounds;
            int escaleras = 0, malos = 0;
            List<float> zs = new List<float>();
            for (float z = bb.min.z + 1.5f; z <= bb.max.z - 1.5f; z += 0.5f)
            {
                float y;
                if (SuperficieEscalera(c, mc, XEscaleraArriba + 3f, z, out y))
                {
                    if (!zs.Any(q => Mathf.Abs(q - z) < 4f))
                    {
                        zs.Add(z);
                    }
                }
            }
            foreach (float z in zs)
            {
                escaleras++;
                float previa = float.NaN, peorAlza = 0f;
                int huecos = 0;
                for (float x = XEscaleraArriba + 0.2f; x < XEscaleraAbajo - 0.2f; x += 0.25f)
                {
                    float y;
                    if (!SuperficieEscalera(c, mc, x, z, out y))
                    {
                        huecos++;
                        continue;
                    }
                    if (!float.IsNaN(previa))
                    {
                        peorAlza = Mathf.Max(peorAlza, Mathf.Abs(previa - y));
                    }
                    previa = y;
                }
                // Casas encima: nada activo sobre la superficie del pasillo (de 0.3 a 2.4 m), en tramos de 4 m que siguen la pendiente.
                int casas = 0;
                for (float xs = XEscaleraArriba; xs < XEscaleraAbajo - 0.5f; xs += 4f)
                {
                    float ys;
                    if (!SuperficieEscalera(c, mc, xs + 2f, z, out ys))
                    {
                        continue;
                    }
                    Bounds pasillo = new Bounds(new Vector3(xs + 2f, ys + 1.5f, z), new Vector3(4f, 2.2f + 1.0f, AnchoEscalera - 0.4f));
                    foreach (Renderer r in c.Zona.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!(r is ParticleSystemRenderer) && !EsPiso(c, r.transform) && !r.transform.IsChildOf(padre) && r.bounds.Intersects(pasillo) && r.bounds.size.x < 40f && !EsProtegido(c, r.transform))
                        {
                            casas++;
                            sb.AppendLine("  encima de la escalera: " + Ruta(r.transform));
                        }
                    }
                }
                sb.AppendLine("Escalera z ~ " + z.ToString("0.0", CultureInfo.InvariantCulture) + ": mayor alza entre muestras " + peorAlza.ToString("0.000", CultureInfo.InvariantCulture) + " m (tope 0.3), huecos " + huecos + ", objetos encima " + casas + ".");
                if (peorAlza > 0.3f || huecos > 2 || casas > 0)
                {
                    sb.AppendLine("FALLO: la escalera de z ~ " + z.ToString("0.0", CultureInfo.InvariantCulture) + " no es caminable o la atraviesa algo.");
                    fallos++;
                    malos++;
                }
            }
            sb.AppendLine("Escaleras de ladera: " + escaleras + ", con fallos " + malos + ".");
        }

        private static bool SuperficieEscalera(Ctx c, MeshCollider mc, float x, float z, out float y)
        {
            y = float.MinValue;
            RaycastHit h;
            if (mc.Raycast(new Ray(new Vector3(x, 200f, z), Vector3.down), out h, 400f))
            {
                y = h.point.y;
                return true;
            }
            return false;
        }

        private static void ValidarPostes(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            Transform padre = c.Nuevo.Find("Postes");
            if (padre == null)
            {
                sb.AppendLine("AVISO: no hay postes nuevos.");
                avisos++;
                return;
            }
            List<Vector3> eje = CerroT1355Trazado.Zigzag();
            int n = 0, malos = 0;
            foreach (Transform p in padre.Cast<Transform>().Where(t => t.name.StartsWith("Poste")))
            {
                n++;
                float y;
                if (!Piso(c, p.position.x, p.position.z, p.position.y + 3f, false, out y) || Mathf.Abs(y - p.position.y) > 0.05f)
                {
                    sb.AppendLine("FALLO: " + p.name + " no está asentado (piso " + (y == float.MinValue ? "no hay" : y.ToString("0.00", CultureInfo.InvariantCulture)) + ", poste " + p.position.y.ToString("0.00", CultureInfo.InvariantCulture) + ").");
                    fallos++;
                    malos++;
                }
                float dmin = eje.Min(q => new Vector2(q.x - p.position.x, q.z - p.position.z).magnitude);
                if (dmin < 4.6f)
                {
                    sb.AppendLine("FALLO: " + p.name + " en el carril (a " + dmin.ToString("0.0", CultureInfo.InvariantCulture) + " m del eje, el arroyo mide 4.5 m de mitad).");
                    fallos++;
                    malos++;
                }
            }
            sb.AppendLine("Postes nuevos: " + n + ", con fallos " + malos + ".");
        }

        private static void ValidarColliders(Ctx c, StringBuilder sb, ref int fallos)
        {
            int total = 0, malos = 0;
            foreach (Collider col in c.Nuevo.GetComponentsInChildren<Collider>(true))
            {
                if (col.isTrigger || !col.enabled)
                {
                    continue;
                }
                total++;
                bool piezaDeKit = false;
                for (Transform p = col.transform; p != null && p != c.Nuevo; p = p.parent)
                {
                    if (p.name.StartsWith("Poste") || p.name.StartsWith("CC08_") || p.name.StartsWith("CE0"))
                    {
                        piezaDeKit = true;
                    }
                }
                if (piezaDeKit)
                {
                    continue;
                }
                Renderer r = col.GetComponent<Renderer>();
                if (r == null || !r.enabled)
                {
                    malos++;
                    sb.AppendLine("FALLO: collider sin malla visible en el mismo objeto: " + Ruta(col.transform));
                    fallos++;
                    continue;
                }
                MeshCollider mc = col as MeshCollider;
                MeshFilter mf = col.GetComponent<MeshFilter>();
                if (mc != null && mf != null && mf.sharedMesh != mc.sharedMesh)
                {
                    malos++;
                    sb.AppendLine("FALLO: MeshCollider distinto de la malla visible: " + Ruta(col.transform));
                    fallos++;
                }
            }
            sb.AppendLine("Colliders nuevos (sin kit): " + total + ", sin malla visible: " + malos + ".");
        }

        // ================================================================ Fotos

        /// <summary>
        /// A13b (la cámara que dejó Aplicar en T1440_Ladera/CamaraA13, con niebla aérea ligera y con la del juego) y K13b: referencia a la izquierda, juego a la derecha, en
        /// docs/capturas/_vista19b/cerro-a13-t1440.png y cerro-k13-t1440.png. No guarda nada; corre en una corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1440/Cerro ladera: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            Directory.CreateDirectory("Logs");
            Physics.SyncTransforms();
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform raiz = todos.FirstOrDefault(t => t.name == Raiz);
            Vector3 desde = A13Desde, hacia = A13Hacia;
            Transform marca = raiz != null ? raiz.Find(NombreCamara) : null;
            if (marca != null && marca.Find(NombreMira) != null)
            {
                desde = marca.position;
                hacia = marca.Find(NombreMira).position;
            }
            else
            {
                Debug.LogWarning("[ZonaCerroT1440] no hay " + Raiz + "/" + NombreCamara + " en la escena (corre Aplicar); se usan las constantes.");
            }
            for (int i = 0; i < 10 && Physics.CheckSphere(desde, 1.5f, ~0, QueryTriggerInteraction.Ignore); i++)
            {
                desde += Vector3.up * 4f;
                Debug.Log("[ZonaCerroT1440] la cámara A13 estaba dentro de algo; sube a " + desde);
            }
            string a13 = "Logs/t1440-a13-juego.png";
            string a13n = "Logs/t1440-a13-niebla-juego.png";
            FotoAerea(desde, hacia, a13, CampoA13, DensidadNieblaAerea);
            FotoAerea(desde, hacia, a13n, CampoA13, -1f);
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", a13, CarpetaFotos + "/cerro-a13-t1440.png");
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", a13n, CarpetaFotos + "/cerro-a13-niebla-t1440.png");
            ColocarS2cLook.Foto(ZonaCerroT1416.K13bDesde, ZonaCerroT1416.K13bHacia, "Logs/t1440-k13-juego.png", ZonaCerroT1416.CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "Logs/t1440-k13-juego.png", CarpetaFotos + "/cerro-k13-t1440.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }

        /// <summary>Como ColocarS2cLook.Foto (noche de las fotos, charcos, posproceso), pero con la densidad de niebla indicada (negativa: la del juego).</summary>
        private static void FotoAerea(Vector3 desde, Vector3 hacia, string ruta, float campo, float densidadNiebla)
        {
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            if (ciclo != null)
            {
                ciclo.Aplicar(ColocarS2cLook.HoraFoto);
                ColocarS2nCielo.SeguirNoche();
            }
            if (densidadNiebla >= 0f)
            {
                RenderSettings.fogDensity = densidadNiebla;
            }
            foreach (LuzNocturna ln in Object.FindObjectsByType<LuzNocturna>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Light l = new SerializedObject(ln).FindProperty("luz").objectReferenceValue as Light;
                if (l != null)
                {
                    l.enabled = true;
                }
            }
            GameObject go = new GameObject("CamaraT1440");
            Camera cam = go.AddComponent<Camera>();
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = ColocarS2cLook.LejosFoto;
            cam.fieldOfView = campo;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            int ancho = Mathf.RoundToInt(AnchoFoto), alto = Mathf.RoundToInt(AltoFoto);
            RenderTexture rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = AnchoFoto / AltoFoto;
            ReflejoCharcos reflejo = Object.FindFirstObjectByType<ReflejoCharcos>();
            if (reflejo != null && reflejo.Actualizar(cam))
            {
                reflejo.Camara.Render();
            }
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            if (reflejo != null)
            {
                reflejo.Soltar();
            }
        }

        // ================================================================ Ayudas

        private static void Log(string texto)
        {
            if (informe != null)
            {
                informe.AppendLine(texto);
            }
            Debug.Log("[ZonaCerroT1440] " + texto);
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
