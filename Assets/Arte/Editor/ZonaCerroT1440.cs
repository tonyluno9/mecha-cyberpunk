using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
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
        public static readonly Vector3 A13Desde = new Vector3(-450f, 55f, 100f);
        public static readonly Vector3 A13Hacia = new Vector3(-330f, 5f, 20f);
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
        public const int PisosMaximos = 2;
        public const int MaxCarros = 6;
        public const long MaxTriangulosModelo = 20000;
        public const float CorteCarro = 0.05f;
        public const float CortePiso = 0.04f;
        /// <summary>Punto de la prueba de rendimiento "calle base al sur": las casas a menos de 80 m que no caen en A13 no llevan pisos extra.</summary>
        public static readonly Vector3 PuntoBaseSur = new Vector3(-305f, 1.7f, -86f);
        public const float RadioBaseSur = 80f;
        public const float CorteModelo = 0.04f;
        public const int MaxModelosPiso = 40;
        public const int MaxPisos = 110;
        public const float DistanciaModelo = 120f;
        public const float CeldaMalla = 40f;
        /// <summary>Alto (m) al que se escala el modelo de casa que va de piso extra (el modelo mide ~7.5 m: se aplasta un poco).</summary>
        public const float AltoPisoModelo = 4.8f;

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
        public const int EscalerasMaximas = 3;

        // Postes
        public const float SeparacionPostes = 11f;
        public const float DesdeEje = 5.25f;
        public const float DistanciaPosteViejo = 6f;
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
            public Material Concreto, Tubo, Lampara, Pila, Oscuro;
            public List<Rend> Rends = new List<Rend>();
            public readonly HashSet<Transform> Apagadas = new HashSet<Transform>();
            public int Volumenes, ConPeldanos, ConEscalera, Altura;
            public Malla Peldanos = new Malla();
            public Malla Escalerillas = new Malla();
            public Malla Brillo = new Malla();
            public Malla Charcos = new Malla();
            public Malla Techos = new Malla();
            public Dictionary<Casa, float> TechosCasa = new Dictionary<Casa, float>();
            public readonly List<Mesh> Extra = new List<Mesh>();
            public readonly List<Vector3> PtsAsfalto = new List<Vector3>(), PtsEscalera = new List<Vector3>(), PtsPoste = new List<Vector3>(), PtsCasa = new List<Vector3>(), PtsCiudad = new List<Vector3>();
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
                int rendAntes = 0;
                Transform viejaRaiz = zona.transform.Find(Raiz);
                long batchesAntes = viejaRaiz == null ? 0 : Batches(viejaRaiz, out rendAntes);
                Log("T1440_Ladera antes de esta corrida: " + rendAntes + " renderers, ~" + batchesAntes + " batches estimados (renderers x materiales).");
                int lucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                long trisAntes = Triangulos(zona.transform);
                Log("Triángulos visibles de la zona antes: " + trisAntes + ".");
                int objetosAntes = zona.GetComponentsInChildren<Transform>(true).Length;
                Log("Luces antes: " + lucesAntes + ". Objetos antes: " + objetosAntes + ". Tamaño del prefab antes: " + Megas(RutaPrefab) + " MB.");
                c.Obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                AssetDatabase.DeleteAsset(CarpetaMat + "/T1440_Combinados.asset"); // la raíz vieja ya no existe: se empieza de cero
                c.Marcas = Hijo(c.Nuevo, "Apagados");
                c.Concreto = AssetDatabase.LoadAssetAtPath<Material>(ZonaCerroT1385.RutaM09) ?? KitS2c.MatBanqueta();
                c.Tubo = MatPropio("T1440_Tubo", () => KitS2c.Lit(new Color(0.3f, 0.17f, 0.11f), 0.3f, KitS2c.TexMuro()));
                c.Lampara = MatPropio("T1440_LamparaNaranja", () => Emisivo(new Color(0.15f, 0.09f, 0.04f), new Color(1f, 0.5f, 0.12f) * 4f));
                c.Oscuro = MatPropio("T1440_ConcretoOscuro", () =>
                {
                    Material o = AssetDatabase.LoadAssetAtPath<Material>(ZonaCerroT1385.RutaM09);
                    Material m = o != null ? new Material(o) : KitS2c.Lit(Color.white, 0.2f, KitS2c.TexMuro());
                    m.SetColor("_BaseColor", new Color(0.34f, 0.29f, 0.26f));
                    m.SetFloat("_Smoothness", 0.2f);
                    m.SetTextureScale("_BaseMap", Vector2.one);
                    return m;
                });
                c.Pila = MatPropio("T1440_PeldanoPiso", () => KitS2c.Lit(new Color(0.55f, 0.52f, 0.5f), 0.25f, KitS2c.TexMuro()));
                Physics.SyncTransforms();
                c.Rends = Reunir(c);
                Log("Renderers considerados: " + c.Rends.Count + ".");
                Paso("escaleras de ladera", () => Escaleras(c));
                Paso("pisos extra", () => Pisos(c));
                Paso("postes y cables", () => Postes(c));
                Paso("carros", () => Carros(c));
                Paso("taludes con muros CC08", () => Taludes(c));
                Paso("neones y ventanas calidas", () => Fachadas(c));
                Paso("combinar accesos viejos", () => CombinarAccesosViejos(c, "AccesosCombinados"));
                Paso("cámara A13 (fija)", () => Camara(c));
                Paso("mallas sueltas", () => MallasSueltas(c));
                Estatico(c);
                KitS2c.Terminar(c.Obra);
                int luces = zona.GetComponentsInChildren<Light>(true).Length;
                int nuevas = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                int objetos = c.Nuevo.GetComponentsInChildren<Transform>(true).Length;
                Log("Triángulos visibles de la zona después: " + Triangulos(zona.transform) + " (solo T1440_Ladera: " + Triangulos(c.Nuevo) + "; antes de esta corrida: " + trisAntes + ").");
                int rendDespues;
                long batchesDespues = Batches(c.Nuevo, out rendDespues);
                int rendZona;
                long batchesZona = Batches(zona.transform, out rendZona);
                Log("T1440_Ladera después: " + rendDespues + " renderers, ~" + batchesDespues + " batches estimados (antes " + batchesAntes + "); zona entera: " + rendZona + " renderers, ~" + batchesZona + " batches.");
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
            int reencendidos = 0;
            foreach (Transform t in zona.GetComponentsInChildren<Transform>(true))
            {
                if (!t.gameObject.activeSelf && (t.name == "Riel" || t.name == "Peldano") && t.parent != null && t.parent.name.Contains("Acceso") && (raiz == null || !t.IsChildOf(raiz)))
                {
                    t.gameObject.SetActive(true);
                    reencendidos++;
                }
            }
            if (reencendidos > 0)
            {
                Log("Rieles y peldaños de accesos viejos vueltos a encender (los había combinado una corrida anterior): " + reencendidos + ".");
            }
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

        private sealed class GrupoMalla
        {
            public Material Mat;
            public bool Colision;
            public readonly Malla M = new Malla();
        }

        /// <summary>Agrega la malla de un MeshRenderer, en coordenadas de mundo, al grupo de su material (un grupo por material y por si lleva collider).</summary>
        private static void Acumular(Dictionary<string, GrupoMalla> grupos, MeshRenderer r, bool colision)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                return;
            }
            Mesh mesh = mf.sharedMesh;
            Matrix4x4 mat4 = r.transform.localToWorldMatrix;
            bool invertir = mat4.determinant < 0f;
            Matrix4x4 normales = mat4.inverse.transpose;
            Vector3[] v = mesh.vertices;
            Vector3[] nm = mesh.normals;
            Vector2[] uv = mesh.uv;
            Material[] ms = r.sharedMaterials;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                Material mt = ms.Length == 0 ? null : ms[Mathf.Min(s, ms.Length - 1)];
                if (mt == null)
                {
                    continue;
                }
                string clave = mt.GetInstanceID() + (colision ? "c" : "n");
                GrupoMalla g;
                if (!grupos.TryGetValue(clave, out g))
                {
                    g = new GrupoMalla { Mat = mt, Colision = colision };
                    grupos[clave] = g;
                }
                int[] tri = mesh.GetTriangles(s);
                Dictionary<int, int> map = new Dictionary<int, int>();
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    int[] idx = invertir ? new[] { tri[i], tri[i + 2], tri[i + 1] } : new[] { tri[i], tri[i + 1], tri[i + 2] };
                    foreach (int viejo in idx)
                    {
                        int nuevo;
                        if (!map.TryGetValue(viejo, out nuevo))
                        {
                            nuevo = g.M.V.Count;
                            g.M.V.Add(mat4.MultiplyPoint3x4(v[viejo]));
                            g.M.N.Add(nm.Length > viejo ? normales.MultiplyVector(nm[viejo]).normalized : Vector3.up);
                            g.M.Uv.Add(uv.Length > viejo ? uv[viejo] : Vector2.zero);
                            map[viejo] = nuevo;
                        }
                        g.M.T.Add(nuevo);
                    }
                }
            }
        }

        private static void EmitirGrupos(Ctx c, Transform padre, string nombre, Dictionary<string, GrupoMalla> grupos)
        {
            int k = 0;
            foreach (GrupoMalla g in grupos.Values)
            {
                ObjetoMalla(c, padre, nombre + "_" + g.Mat.name + "_" + k, g.M, g.Mat, g.Colision, true, false);
                k++;
            }
        }

        /// <summary>
        /// Junta en una malla por material las piezas visibles de los hijos de <paramref name="padre"/> que cumplan el filtro (barandales CE03, muros CC08) y borra esos hijos.
        /// Si la pieza traía collider en su objeto, la malla combinada lleva MeshCollider (igual a lo visible); si no, no lleva.
        /// </summary>
        private static void CombinarHijos(Ctx c, Transform padre, string nombre, Func<string, bool> filtro)
        {
            List<Transform> raices = padre.Cast<Transform>().Where(t => filtro(t.name)).ToList();
            Dictionary<string, GrupoMalla> grupos = new Dictionary<string, GrupoMalla>();
            int antes = 0;
            foreach (Transform raiz in raices)
            {
                foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!r.enabled)
                    {
                        continue;
                    }
                    antes++;
                    Collider col = r.GetComponent<Collider>();
                    Acumular(grupos, r, col != null && col.enabled && !col.isTrigger);
                }
            }
            if (grupos.Count == 0)
            {
                return;
            }
            Transform destino = Hijo(padre, nombre);
            EmitirGrupos(c, destino, nombre, grupos);
            foreach (Transform raiz in raices)
            {
                Object.DestroyImmediate(raiz.gameObject);
            }
            Log(nombre + ": " + antes + " renderers combinados en " + grupos.Count + " mallas (una por material y collider).");
        }

        /// <summary>
        /// Los rieles y peldaños de las escaleras de servicio viejas (objetos "Riel" y "Peldano" bajo un "Acceso_...", sin collider) se juntan en una malla por material.
        /// Los originales se APAGAN (reversible: Restaurar los vuelve a encender por nombre). Los PuntoTrepar no se tocan.
        /// </summary>
        private static void CombinarAccesosViejos(Ctx c, string nombre)
        {
            List<MeshRenderer> rs = new List<MeshRenderer>();
            foreach (MeshRenderer r in c.Zona.GetComponentsInChildren<MeshRenderer>(false))
            {
                string n = r.gameObject.name;
                if (!r.enabled || r.transform.IsChildOf(c.Nuevo) || (n != "Riel" && n != "Peldano") || r.transform.parent == null || !r.transform.parent.name.Contains("Acceso") || r.GetComponent<Collider>() != null)
                {
                    continue;
                }
                rs.Add(r);
            }
            if (rs.Count == 0)
            {
                Log(nombre + ": no hay rieles ni peldaños viejos por combinar.");
                return;
            }
            Dictionary<string, GrupoMalla> grupos = new Dictionary<string, GrupoMalla>();
            foreach (MeshRenderer r in rs)
            {
                Acumular(grupos, r, false);
            }
            EmitirGrupos(c, Hijo(c.Nuevo, nombre), nombre, grupos);
            foreach (MeshRenderer r in rs)
            {
                r.gameObject.SetActive(false);
            }
            Log(nombre + ": " + rs.Count + " rieles y peldaños viejos combinados en " + grupos.Count + " mallas (originales apagados, reversible).");
        }

        /// <summary>
        /// Pisos del kit FUERA del cuadro de A13: todas sus piezas visibles pasan a una malla por material (sin culling) y cada piso se reduce a un nodo con su collider de cuerpo
        /// (la misma caja de lo visible). Se corre al final de DespejarDetalles, cuando ya se sabe qué pisos se quedan. Los pisos con modelo CC no se tocan.
        /// </summary>
        private static void CombinarPisosKit(Ctx c, Transform pisos)
        {
            Dictionary<string, GrupoMalla> grupos = new Dictionary<string, GrupoMalla>();
            List<(Transform piso, Vector3 pos, Quaternion rot, Vector3 centro, Vector3 tam)> lista = new List<(Transform, Vector3, Quaternion, Vector3, Vector3)>();
            int renderers = 0;
            foreach (Transform piso in pisos.Cast<Transform>().Where(t => t.gameObject.activeSelf && t.name.StartsWith("Piso_")).ToList())
            {
                Transform cuerpo = piso.Find("Cuerpo");
                BoxCollider bc = cuerpo == null ? null : cuerpo.GetComponent<BoxCollider>();
                if (bc == null)
                {
                    continue;
                }
                float u, v;
                Bounds cb = CajaPiso(piso);
                if (EnCuadroA13(cb.center, out u, out v) && u > 0.03f && u < 0.97f && v > 0.05f && v < 0.95f)
                {
                    continue;
                }
                foreach (MeshRenderer r in piso.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (r.enabled)
                    {
                        Acumular(grupos, r, false);
                        renderers++;
                    }
                }
                lista.Add((piso, cuerpo.position, cuerpo.rotation, bc.center, bc.size));
            }
            if (lista.Count == 0)
            {
                Log("Pisos del kit fuera del cuadro de A13 por combinar: 0.");
                return;
            }
            EmitirGrupos(c, Hijo(c.Nuevo, "PisosKitCombinados"), "PisosKit", grupos);
            foreach (var d in lista)
            {
                string nombre = d.piso.name;
                Object.DestroyImmediate(d.piso.gameObject);
                GameObject nuevo = new GameObject(nombre);
                nuevo.transform.SetParent(pisos, false);
                GameObject cuerpo = new GameObject("Cuerpo");
                cuerpo.transform.SetParent(nuevo.transform, false);
                cuerpo.transform.SetPositionAndRotation(d.pos, d.rot);
                BoxCollider caja = cuerpo.AddComponent<BoxCollider>();
                caja.center = d.centro;
                caja.size = d.tam;
            }
            Physics.SyncTransforms();
            Log("Pisos del kit combinados: " + lista.Count + " pisos, " + renderers + " renderers -> " + grupos.Count + " mallas; cada piso queda con su collider de cuerpo.");
        }

        /// <summary>Guarda las mallas creadas fuera de Aplicar (DespejarDetalles) en un asset propio, para que el prefab no las pierda.</summary>
        private static void GuardarExtra(Ctx c)
        {
            if (c.Extra.Count == 0)
            {
                return;
            }
            if (!AssetDatabase.IsValidFolder(CarpetaMat))
            {
                AssetDatabase.CreateFolder("Assets/Arte/Mundo", "Cerro");
            }
            string ruta = CarpetaMat + "/T1440_Combinados.asset";
            int desde = 0;
            if (AssetDatabase.LoadMainAssetAtPath(ruta) == null)
            {
                AssetDatabase.CreateAsset(c.Extra[0], ruta);
                desde = 1;
            }
            for (int i = desde; i < c.Extra.Count; i++)
            {
                AssetDatabase.AddObjectToAsset(c.Extra[i], ruta); // se suma al asset: una corrida anterior de DespejarDetalles puede seguir usando sus mallas
            }
            AssetDatabase.SaveAssets();
            Log("Mallas combinadas guardadas en " + ruta + " (" + c.Extra.Count + ").");
        }

        /// <summary>Triángulos de los MeshFilter activos con renderer encendido bajo la raíz.</summary>
        private static long Triangulos(Transform raiz)
        {
            long n = 0;
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(false))
            {
                Renderer r = mf.GetComponent<Renderer>();
                if (mf.sharedMesh == null || r == null || !r.enabled)
                {
                    continue;
                }
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    n += mf.sharedMesh.GetIndexCount(s) / 3;
                }
            }
            return n;
        }

        /// <summary>Batches estimados: renderers activos x materiales (cada material de cada renderer es una llamada de dibujo, antes de agrupar).</summary>
        private static long Batches(Transform raiz, out int renderers)
        {
            long n = 0;
            renderers = 0;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (r is ParticleSystemRenderer || !r.enabled)
                {
                    continue;
                }
                renderers++;
                n += Mathf.Max(1, r.sharedMaterials.Length);
            }
            return n;
        }

        /// <summary>Triángulos de un prefab (sin instanciarlo).</summary>
        private static long TriangulosPrefab(GameObject prefab)
        {
            long n = 0;
            foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null)
                {
                    continue;
                }
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    n += mf.sharedMesh.GetIndexCount(s) / 3;
                }
            }
            return n;
        }

        /// <summary>LODGroup de un solo nivel con culling a <paramref name="corte"/> de la altura de pantalla (si el objeto ya trae LODGroup, se sube el corte de su último nivel).</summary>
        private static void Lod(GameObject go, float corte)
        {
            LODGroup lg = go.GetComponent<LODGroup>();
            if (lg != null)
            {
                LOD[] ls = lg.GetLODs();
                if (ls.Length > 0)
                {
                    ls[ls.Length - 1].screenRelativeTransitionHeight = Mathf.Max(ls[ls.Length - 1].screenRelativeTransitionHeight, corte);
                    lg.SetLODs(ls);
                    lg.RecalculateBounds();
                }
                return;
            }
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return;
            }
            lg = go.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(corte, rs) });
            lg.RecalculateBounds();
        }

        /// <summary>Parte una malla sin collider en una por cada cuadro de 40 m (por el primer vértice de cada triángulo), todas con el mismo material: así se descartan por vista.</summary>
        private static GameObject ObjetoCeldas(Ctx c, Transform padre, string nombre, Malla m, Material mat, bool sinSombras)
        {
            if (m.T.Count == 0)
            {
                return null;
            }
            Dictionary<long, Malla> celdas = new Dictionary<long, Malla>();
            Dictionary<long, Dictionary<int, int>> mapas = new Dictionary<long, Dictionary<int, int>>();
            for (int i = 0; i + 2 < m.T.Count; i += 3)
            {
                Vector3 p = m.V[m.T[i]];
                long clave = ((long)Mathf.FloorToInt(p.x / CeldaMalla) << 32) ^ (uint)Mathf.FloorToInt(p.z / CeldaMalla);
                Malla sub;
                Dictionary<int, int> map;
                if (!celdas.TryGetValue(clave, out sub))
                {
                    sub = new Malla();
                    map = new Dictionary<int, int>();
                    celdas[clave] = sub;
                    mapas[clave] = map;
                }
                else
                {
                    map = mapas[clave];
                }
                for (int k = 0; k < 3; k++)
                {
                    int viejo = m.T[i + k], nuevo;
                    if (!map.TryGetValue(viejo, out nuevo))
                    {
                        nuevo = sub.V.Count;
                        sub.V.Add(m.V[viejo]);
                        sub.N.Add(m.N[viejo]);
                        sub.Uv.Add(m.Uv[viejo]);
                        map[viejo] = nuevo;
                    }
                    sub.T.Add(nuevo);
                }
            }
            GameObject grupo = new GameObject(nombre);
            grupo.transform.SetParent(padre, false);
            int n = 0;
            foreach (Malla sub in celdas.Values)
            {
                ObjetoMalla(c, grupo.transform, nombre + "_" + n, sub, mat, false, sinSombras, false);
                n++;
            }
            Log("Malla " + nombre + ": " + (m.T.Count / 3) + " triángulos en " + n + " cuadros de " + CeldaMalla + " m.");
            return grupo;
        }

        /// <summary>Un GameObject con la malla (y su collider igual a ella, si se pide). Devuelve null si la malla está vacía.</summary>
        private static GameObject ObjetoMalla(Ctx c, Transform padre, string nombre, Malla m, Material mat, bool collider, bool sinSombras, bool porCeldas = false)
        {
            if (m.T.Count == 0)
            {
                return null;
            }
            if (!collider && porCeldas)
            {
                return ObjetoCeldas(c, padre, nombre, m, mat, sinSombras);
            }
            Mesh malla = m.Crear("T1440_" + nombre);
            if (c.Obra != null)
            {
                c.Obra.Mallas.Add(malla);
            }
            else
            {
                c.Extra.Add(malla);
            }
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
            return MatPropio("T1440_CharcoLuz_b", () =>
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
                m.SetColor("_BaseColor", new Color(1f, 0.5f, 0.15f, 1f) * 1.3f);
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

        /// <summary>
        /// Busca la cámara A13 como T-1443: prueba posiciones (rejilla de 10 x 15 m sobre la ladera), alturas 45-75 m, cabeceos 14/18/22 grados y 24 rumbos, y puntúa lo que cae en el cuadro:
        /// asfalto del zigzag cruzando en diagonal (abajo y arriba, corrido de lado), una escalera larga en el centro-izquierda, postes, casas (y cuartos del cuadro con algo) y la franja de
        /// la ciudad arriba. Se queda con la mejor que tenga espacio libre. Deja CamaraA13 y CamaraA13/Mira bajo la raíz.
        /// </summary>
        private static void BuscarCamara(Ctx c)
        {
            List<Vector3> eje = CerroT1355Trazado.Zigzag();
            foreach (Vector3 q in eje)
            {
                c.PtsAsfalto.Add(new Vector3(q.x, q.y + 0.3f, q.z));
            }
            Vector3[] pa = c.PtsAsfalto.ToArray(), pe = c.PtsEscalera.ToArray(), pp = c.PtsPoste.ToArray(), pc = c.PtsCasa.ToArray(), pci = c.PtsCiudad.ToArray();
            float tanV = Mathf.Tan(CampoA13 * 0.5f * Mathf.Deg2Rad), tanH = tanV * (AnchoFoto / AltoFoto);
            List<(float s, Vector3 p, float yaw, float pitch)> tops = new List<(float, Vector3, float, float)>();
            float[] alturas = { 45f, 55f, 65f, 75f };
            float[] cabeceos = { 14f, 18f, 22f };
            for (float x = -470f; x <= -380f; x += 10f)
            {
                for (float z = -110f; z <= 120f; z += 15f)
                {
                    foreach (float h in alturas)
                    {
                        Vector3 pos = new Vector3(x, h, z);
                        foreach (float pitch in cabeceos)
                        {
                            float cp = Mathf.Cos(pitch * Mathf.Deg2Rad), sp = Mathf.Sin(pitch * Mathf.Deg2Rad);
                            for (int ya = 0; ya < 24; ya++)
                            {
                                float yaw = ya * 15f;
                                float sy = Mathf.Sin(yaw * Mathf.Deg2Rad), cy = Mathf.Cos(yaw * Mathf.Deg2Rad);
                                Vector3 f = new Vector3(sy * cp, -sp, cy * cp);
                                Vector3 rr = new Vector3(cy, 0f, -sy);
                                Vector3 uu = Vector3.Cross(f, rr);
                                int nBajo = 0, nAlto = 0, nTotal = 0;
                                float uBajo = 0f, uAlto = 0f;
                                for (int i = 0; i < pa.Length; i++)
                                {
                                    float u, v;
                                    if (!Proy(pa[i], pos, f, rr, uu, tanH, tanV, 12f, 260f, out u, out v))
                                    {
                                        continue;
                                    }
                                    nTotal++;
                                    if (v < 0.4f)
                                    {
                                        nBajo++;
                                        uBajo += u;
                                    }
                                    else if (v > 0.5f)
                                    {
                                        nAlto++;
                                        uAlto += u;
                                    }
                                }
                                float sa = Mathf.Min(nTotal, 50) / 50f;
                                float diag = nBajo >= 3 && nAlto >= 3 && Mathf.Abs(uBajo / nBajo - uAlto / nAlto) > 0.15f ? 1f : 0f;
                                int esc = 0;
                                for (int i = 0; i < pe.Length; i++)
                                {
                                    float u, v;
                                    if (Proy(pe[i], pos, f, rr, uu, tanH, tanV, 12f, 220f, out u, out v) && u > 0.1f && u < 0.55f && v > 0.25f && v < 0.75f)
                                    {
                                        esc++;
                                    }
                                }
                                int post = 0;
                                for (int i = 0; i < pp.Length; i++)
                                {
                                    float u, v;
                                    if (Proy(pp[i], pos, f, rr, uu, tanH, tanV, 12f, 220f, out u, out v))
                                    {
                                        post++;
                                    }
                                }
                                int casas = 0, mask = 0;
                                for (int i = 0; i < pc.Length; i++)
                                {
                                    float u, v;
                                    if (Proy(pc[i], pos, f, rr, uu, tanH, tanV, 12f, 220f, out u, out v))
                                    {
                                        casas++;
                                        mask |= 1 << (Mathf.Min(3, (int)(u * 4f)) * 4 + Mathf.Min(3, (int)(v * 4f)));
                                    }
                                }
                                int celdas = 0;
                                for (int m = mask; m != 0; m &= m - 1)
                                {
                                    celdas++;
                                }
                                int ciu = 0;
                                for (int i = 0; i < pci.Length; i++)
                                {
                                    float u, v;
                                    if (Proy(pci[i], pos, f, rr, uu, tanH, tanV, 100f, 900f, out u, out v) && v > 0.72f)
                                    {
                                        ciu++;
                                    }
                                }
                                float s = 2.5f * sa + 1.5f * diag + 2f * Mathf.Min(esc, 3) / 3f + 1f * Mathf.Min(post, 8) / 8f + 1f * Mathf.Min(casas, 40) / 40f + 1f * celdas / 16f + 1.5f * Mathf.Min(ciu, 12) / 12f;
                                if (s > 4f)
                                {
                                    tops.Add((s, pos, yaw, pitch));
                                }
                            }
                        }
                    }
                }
            }
            bool puesta = false;
            foreach (var t3 in tops.OrderByDescending(a => a.s).Take(60))
            {
                int hay = c.Fisica.OverlapBox(t3.p, new Vector3(2f, 2f, 2f), bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                int abajo = c.Fisica.Raycast(t3.p, Vector3.down, bufRayos, 8f, ~0, QueryTriggerInteraction.Ignore);
                if (hay > 0 || abajo > 0)
                {
                    continue;
                }
                float sy = Mathf.Sin(t3.yaw * Mathf.Deg2Rad), cy = Mathf.Cos(t3.yaw * Mathf.Deg2Rad), cp = Mathf.Cos(t3.pitch * Mathf.Deg2Rad), sp = Mathf.Sin(t3.pitch * Mathf.Deg2Rad);
                Vector3 f = new Vector3(sy * cp, -sp, cy * cp);
                Transform marca = new GameObject(NombreCamara).transform;
                marca.SetParent(c.Nuevo, false);
                marca.position = t3.p;
                marca.rotation = Quaternion.LookRotation(f);
                Transform mira = new GameObject(NombreMira).transform;
                mira.SetParent(marca, true);
                mira.position = t3.p + f * 70f;
                Log("CÁMARA A13 ELEGIDA: " + t3.p.ToString("F1") + " mirando a " + mira.position.ToString("F1") + " (rumbo " + t3.yaw + ", cabeceo " + t3.pitch + ", campo " + CampoA13 + "), puntaje " + t3.s.ToString("F2") + " de 10.5.");
                puesta = true;
                break;
            }
            if (!puesta)
            {
                Log("AVISO: la búsqueda no encontró cámara libre (" + tops.Count + " candidatas); se usa la fija.");
                Camara(c);
            }
        }

        /// <summary>true si el punto está a menos de 3.5 m (en z) de una escalera CE01 del paseo (z 120, 60, 0) entre sus pies y la pierna 2: ahí no van postes ni carros.</summary>
        private static bool CercaDeEscaleraCE01(Vector3 p)
        {
            foreach (float z in ColocarT1355Cerro.ZEscaleras)
            {
                if (Mathf.Abs(p.z - z) < 3.5f && p.x < -340f && p.x > -372f)
                {
                    return true;
                }
            }
            return false;
        }

        private static float HuecoCajas(Bounds a, Bounds b)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(a.center.x - b.center.x) - (a.extents.x + b.extents.x));
            float dz = Mathf.Max(0f, Mathf.Abs(a.center.z - b.center.z) - (a.extents.z + b.extents.z));
            return new Vector2(dx, dz).magnitude;
        }

        /// <summary>
        /// Las azoteas de las 6 cadenas de parkour de RecorridoCerroT1361Pruebas (misma lógica: casas del kit con Cuerpo y AzoteaT1318, hueco menor de 4 m, desnivel hasta 3 m, 5 por cadena,
        /// arrancando junto a la cima de cada CE01 y a 20, 50 y 80 % de la calle base). La prueba corre por el centro de esas azoteas: ahí no se apila un piso.
        /// </summary>
        private static HashSet<Transform> CadenasReservadas(Ctx c)
        {
            HashSet<Transform> reservadas = new HashSet<Transform>();
            Transform casas = c.Zona.transform.Find("Casas");
            if (casas == null)
            {
                return reservadas;
            }
            List<Transform> raices = new List<Transform>();
            List<Bounds> cajas = new List<Bounds>();
            foreach (Transform casa in casas)
            {
                if (!casa.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Transform cuerpo = casa.Find("Cuerpo");
                if (cuerpo == null || casa.Find("AzoteaT1318") == null)
                {
                    continue;
                }
                Collider col = cuerpo.GetComponent<Collider>();
                if (col == null)
                {
                    continue;
                }
                raices.Add(casa);
                cajas.Add(col.bounds);
            }
            List<Vector3> puntos = new List<Vector3>();
            foreach (float z in ColocarT1355Cerro.ZEscaleras)
            {
                puntos.Add(new Vector3(-354f, CerroT1355Trazado.AlturaPierna2(z) + 0.2f, z));
            }
            foreach (float f in new[] { 0.2f, 0.5f, 0.8f })
            {
                puntos.Add(new Vector3(CerroT1355Trazado.XBase, 0f, CerroT1355Trazado.ZBaseSur + (CerroT1355Trazado.ZBaseNorte - CerroT1355Trazado.ZBaseSur) * f));
            }
            Func<int, List<int>> vecinas = i =>
            {
                List<int> l = new List<int>();
                for (int j = 0; j < cajas.Count; j++)
                {
                    if (j != i && HuecoCajas(cajas[i], cajas[j]) < 4f && Mathf.Abs(cajas[i].max.y - cajas[j].max.y) <= 3f)
                    {
                        l.Add(j);
                    }
                }
                return l;
            };
            HashSet<int> usadas = new HashSet<int>();
            foreach (Vector3 punto in puntos)
            {
                int inicio = -1;
                float distancia = float.MaxValue;
                for (int i = 0; i < cajas.Count; i++)
                {
                    if (usadas.Contains(i) || vecinas(i).Count == 0)
                    {
                        continue;
                    }
                    float d = new Vector2(cajas[i].center.x - punto.x, cajas[i].center.z - punto.z).magnitude;
                    if (d < distancia)
                    {
                        distancia = d;
                        inicio = i;
                    }
                }
                if (inicio < 0)
                {
                    continue;
                }
                usadas.Add(inicio);
                List<int> indices = new List<int> { inicio };
                Vector3 anterior = Vector3.zero;
                while (indices.Count < 5)
                {
                    int actual = indices[indices.Count - 1];
                    int mejor = -1;
                    float puntaje = float.MaxValue;
                    foreach (int j in vecinas(actual))
                    {
                        if (indices.Contains(j))
                        {
                            continue;
                        }
                        Vector3 dir = cajas[j].center - cajas[actual].center;
                        dir.y = 0f;
                        float p = HuecoCajas(cajas[actual], cajas[j]) + (Vector3.Dot(anterior, dir.normalized) < 0f ? 5f : 0f);
                        if (p < puntaje)
                        {
                            puntaje = p;
                            mejor = j;
                        }
                    }
                    if (mejor < 0)
                    {
                        break;
                    }
                    anterior = cajas[mejor].center - cajas[actual].center;
                    anterior.y = 0f;
                    anterior.Normalize();
                    indices.Add(mejor);
                }
                foreach (int i in indices)
                {
                    reservadas.Add(raices[i]);
                }
            }
            return reservadas;
        }

        /// <summary>true si el punto cae en el cuadro de la cámara A13 fija (u, v en 0..1).</summary>
        private static bool EnCuadroA13(Vector3 q, out float u, out float v)
        {
            Vector3 f = (A13Hacia - A13Desde).normalized;
            Vector3 rr = Vector3.Cross(Vector3.up, f);
            rr.y = 0f;
            rr.Normalize();
            Vector3 uu = Vector3.Cross(f, rr);
            float tanV = Mathf.Tan(CampoA13 * 0.5f * Mathf.Deg2Rad), tanH = tanV * (AnchoFoto / AltoFoto);
            return Proy(q, A13Desde, f, rr, uu, tanH, tanV, 10f, 450f, out u, out v);
        }

        private static float Golpe(Ctx c, Transform propio, Vector3 o, float dist)
        {
            int n = c.Fisica.Raycast(o, Vector3.down, bufRayos, dist, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                if (!bufRayos[i].collider.transform.IsChildOf(propio) && bufRayos[i].point.y > mejor)
                {
                    mejor = bufRayos[i].point.y;
                }
            }
            return mejor;
        }

        /// <summary>
        /// Apoyo del piso: 5 rayos bajo su base. Primero desde 0.3 m sobre la base; si no tocan nada (el piso puede estar hundido y el rayo nace dentro del collider de abajo), desde 1.5 m.
        /// Devuelve false si ningún rayo encuentra algo en que apoyar; <paramref name="peor"/> es la mayor diferencia con la base.
        /// </summary>
        private static bool MedirApoyo(Ctx c, Transform p, Bounds b, out float peor)
        {
            peor = 0f;
            bool toca = false;
            foreach ((float fx, float fz) in new[] { (0.5f, 0.5f), (0.2f, 0.2f), (0.8f, 0.2f), (0.2f, 0.8f), (0.8f, 0.8f) })
            {
                float x = Mathf.Lerp(b.min.x, b.max.x, fx), z = Mathf.Lerp(b.min.z, b.max.z, fz);
                float mejor = Golpe(c, p, new Vector3(x, b.min.y + 0.3f, z), 3f);
                if (mejor == float.MinValue)
                {
                    float m2 = Golpe(c, p, new Vector3(x, b.min.y + 1.5f, z), 4.5f);
                    if (m2 > float.MinValue && m2 <= b.min.y + 1.2f)
                    {
                        mejor = m2;
                    }
                }
                if (mejor > float.MinValue)
                {
                    toca = true;
                    peor = Mathf.Max(peor, Mathf.Abs(mejor - b.min.y));
                }
            }
            return toca;
        }

        /// <summary>Proyección de un punto al cuadro de la cámara (u, v en 0..1; v crece hacia arriba); false si no cae dentro o fuera de la profundidad pedida.</summary>
        private static bool Proy(Vector3 q, Vector3 pos, Vector3 f, Vector3 rr, Vector3 uu, float tanH, float tanV, float profMin, float profMax, out float u, out float v)
        {
            u = 0f;
            v = 0f;
            Vector3 d = q - pos;
            float prof = d.x * f.x + d.y * f.y + d.z * f.z;
            if (prof < profMin || prof > profMax)
            {
                return false;
            }
            float xx = (d.x * rr.x + d.z * rr.z) / (prof * tanH);
            float yy = (d.x * uu.x + d.y * uu.y + d.z * uu.z) / (prof * tanV);
            if (xx < -1f || xx > 1f || yy < -1f || yy > 1f)
            {
                return false;
            }
            u = 0.5f + 0.5f * xx;
            v = 0.5f + 0.5f * yy;
            return true;
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
            Bounds pasillo = new Bounds(new Vector3((XEscaleraArriba + XEscaleraAbajo) * 0.5f, (yA + yB) * 0.5f + 1f, z), new Vector3(XEscaleraAbajo - XEscaleraArriba, Mathf.Abs(yA - yB) + 26f, AnchoEscalera + 2.6f));
            foreach (Rend r in c.Rends)
            {
                if (!r.R.gameObject.activeInHierarchy || !r.B.Intersects(pasillo))
                {
                    continue;
                }
                Transform poste = null;
                for (Transform q = r.R.transform; q != null && q != c.Zona.transform; q = q.parent)
                {
                    if (q.name.StartsWith("Poste_") || q.name.StartsWith("PosteLejano_"))
                    {
                        poste = q;
                    }
                }
                if (poste != null)
                {
                    p.Unidades.Add(poste); // un poste en el pasillo se apaga (reversible)
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
            for (float z = -24f; z <= 36f; z += 4f)
            {
                candidatas.Add(Evaluar(c, z));
            }
            foreach (PlanEscalera p in candidatas)
            {
                Log("Pasillo z = " + p.Z.ToString("0", CultureInfo.InvariantCulture) + ": desnivel " + (p.YArriba - p.YAbajo).ToString("0.0", CultureInfo.InvariantCulture) + " m, terreno sobre la escalera "
                    + p.Mala.ToString("0.00", CultureInfo.InvariantCulture) + " m, hueco " + p.Hueco.ToString("0.0", CultureInfo.InvariantCulture) + " m, casas a apagar " + p.Unidades.Count + (p.Protegido ? ", estorba un poste o escalera vieja" : "") + ".");
            }
            // El terreno se aplana bajo la escalera (AplanarTerreno): ya no importa el entierro; se prefieren los pasillos con menos casas a apagar.
            // Primero las que caen en el cuadro de la cámara A13 fija (centro-izquierda), luego las de menos casas a apagar.
            Func<PlanEscalera, float> clave = p =>
            {
                float u, v;
                bool dentro = EnCuadroA13(new Vector3((XEscaleraArriba + XEscaleraAbajo) * 0.5f, (p.YArriba + p.YAbajo) * 0.5f, p.Z), out u, out v) && u > 0.05f && u < 0.95f && v > 0.08f && v < 0.95f;
                return (dentro ? 0f : 1000f) + p.Unidades.Count + (dentro ? Mathf.Abs(u - 0.4f) * 25f : 0f);
            };
            List<PlanEscalera> buenas = candidatas.Where(p => !p.Protegido && p.Pasos > 0 && p.Unidades.Count <= 40).OrderBy(clave).ToList();
            foreach (PlanEscalera pb in buenas.Take(4))
            {
                Log("Pasillo candidato z = " + pb.Z.ToString("0", CultureInfo.InvariantCulture) + ": " + (clave(pb) < 1000f ? "EN el cuadro de A13" : "fuera del cuadro de A13") + ", casas " + pb.Unidades.Count + ".");
            }
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
                Log("AVISO: ningún pasillo entre la pierna 3 y la pierna 2 está libre de escaleras o muros viejos; no se pone ninguna (ver los pasillos de arriba).");
                return;
            }
            Transform padre = Hijo(c.Nuevo, "EscalerasLadera");
            foreach (PlanEscalera pe in elegidas)
            {
                foreach (Transform u in pe.Unidades)
                {
                    Apagar(c, u, "pasillo de la escalera z " + pe.Z);
                }
            }
            Physics.SyncTransforms();
            AplanarTerreno(c, elegidas);
            Malla rampa = new Malla();
            Malla tubos = new Malla();
            Malla brillo = c.Brillo;
            int numero = 0;
            foreach (PlanEscalera p in elegidas.OrderBy(e => e.Z))
            {
                numero++;
                ConstruirEscalera(c, padre, p, numero, rampa, tubos, brillo);
            }
            ObjetoMalla(c, padre, "Peldanos", rampa, c.Concreto, true, false);
            ObjetoMalla(c, padre, "TubosBarandal", tubos, c.Tubo, false, true);
            Physics.SyncTransforms();
            Log("Escaleras de ladera: " + elegidas.Count + ".");
            CombinarHijos(c, padre, "BarandalesCombinados", n => n.StartsWith("CE03_Barandal_"));
            CombinarHijos(c, padre, "MurosCombinados", n => n.StartsWith("Muros_"));
            Physics.SyncTransforms();
        }

        /// <summary>
        /// La malla del terreno (Terreno/Ladera: vértices sueltos sobre una rejilla de 4 m, x múltiplo de 4 y z = 2 mod 4) se copia y los vértices de las dos columnas
        /// z = Z - 2 y Z + 2, de x = -408 a -368, se llevan a 0.5 m bajo los peldaños: el terreno ni entierra la escalera ni la deja colgando. La copia se guarda en
        /// Assets/Arte/Mundo/Cerro/T1440_Cerro_Ladera.asset. Una corrida nueva parte de la copia anterior (los pasillos viejos quedan aplanados).
        /// </summary>
        private static void AplanarTerreno(Ctx c, List<PlanEscalera> planes)
        {
            Transform ladera = c.Terreno == null ? null : c.Terreno.Find("Ladera");
            MeshFilter mf = ladera == null ? null : ladera.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Log("AVISO: no hay Terreno/Ladera; el terreno no se aplana bajo las escaleras.");
                return;
            }
            Mesh origen = mf.sharedMesh;
            Vector3[] v = origen.vertices;
            int cambiados = 0;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 w = ladera.TransformPoint(v[i]);
                foreach (PlanEscalera p in planes)
                {
                    if (w.x >= XEscaleraArriba + 0.5f && w.x <= XEscaleraAbajo + 0.5f && Mathf.Abs(w.z - p.Z) <= 2.01f)
                    {
                        w.y = AlturaEscalera(p, w.x) - 0.5f;
                        v[i] = ladera.InverseTransformPoint(w);
                        cambiados++;
                        break;
                    }
                }
            }
            Mesh copia = Object.Instantiate(origen);
            copia.name = "T1440_Cerro_Ladera";
            copia.vertices = v;
            copia.RecalculateBounds();
            if (!AssetDatabase.IsValidFolder(CarpetaMat))
            {
                AssetDatabase.CreateFolder("Assets/Arte/Mundo", "Cerro");
            }
            string ruta = CarpetaMat + "/T1440_Cerro_Ladera.asset";
            AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.CreateAsset(copia, ruta);
            mf.sharedMesh = copia;
            MeshCollider mc = ladera.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = copia;
            }
            Physics.SyncTransforms();
            Log("Terreno: " + cambiados + " vértices aplanados bajo " + planes.Count + " escaleras (malla en " + ruta + ").");
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
            for (float xp = XEscaleraArriba; xp < XEscaleraAbajo; xp += 4f)
            {
                c.PtsEscalera.Add(new Vector3(xp, AlturaEscalera(p, xp), p.Z));
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
                    float superficie = float.MinValue, minimo = float.MaxValue;
                    for (float xs = x; xs <= x1 + 0.01f; xs += 1f)
                    {
                        float ya = AlturaEscalera(p, xs);
                        superficie = Mathf.Max(superficie, ya);
                        minimo = Mathf.Min(minimo, ya);
                    }
                    // El terreno queda 0.5 m bajo los peldaños (AplanarTerreno): el muro baja 1.2 m bajo el más bajo para quedar metido.
                    float suelo = minimo - 1.2f;
                    float alto = superficie + 0.9f - suelo;
                    if (alto < 0.8f || alto > 9f)
                    {
                        continue;
                    }
                    GameObject g2 = (GameObject)PrefabUtility.InstantiatePrefab(muro, muros);
                    g2.name = "CC08_Muro_" + numero + "_" + puestos;
                    g2.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    g2.transform.localScale = new Vector3(largo / mm.x, alto / mm.y, 1f);
                    Bounds b = Cubierta(g2.transform);
                    g2.transform.position += new Vector3((x + x1) * 0.5f - b.center.x, suelo - b.min.y, zm - b.center.z);
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
            float baseT = 2.6f * Mathf.Pow(u, 0.8f);
            int h = Mathf.FloorToInt((k.Z0 + k.Z1) * 0.5f / 28f) * 73856093 ^ Mathf.FloorToInt(k.Centro / 34f) * 19349663;
            float ruido = ((Mathf.Abs(h) % 3) - 1) * 0.95f;
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

        /// <summary>Hijo de <paramref name="raiz"/> (directo) que contiene a <paramref name="t"/>.</summary>
        private static Transform HijoDirecto(Transform raiz, Transform t)
        {
            Transform u = t;
            while (u.parent != null && u.parent != raiz)
            {
                u = u.parent;
            }
            return u.parent == raiz ? u : null;
        }

        /// <summary>
        /// Lo que estorba en la caja del piso nuevo: apaga (reversible) los adornos sueltos de la azotea, también los que cuelgan de la propia casa (tinaco, antena, tendedero,
        /// aire, los de T-1441); devuelve false si hay algo que no se puede quitar (el cuerpo de la casa de al lado, un poste, una escalera vieja).
        /// </summary>
        private static bool Despejar(Ctx c, Casa k, Bounds caja, float azotea, List<Transform> aApagar)
        {
            foreach (Rend r in c.Rends)
            {
                if (!r.R.gameObject.activeInHierarchy || !r.B.Intersects(caja) || c.Apagadas.Contains(r.R.transform))
                {
                    continue;
                }
                if (r.R.transform.IsChildOf(k.Raiz))
                {
                    if (r.B.min.y >= azotea - 0.4f && r.B.size.x <= 5f && r.B.size.z <= 5f)
                    {
                        Transform h = HijoDirecto(k.Raiz, r.R.transform);
                        if (h != null && !aApagar.Contains(h))
                        {
                            aApagar.Add(h);
                        }
                    }
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
                if (EsPiso(c, t) || t.IsChildOf(c.Nuevo) || bufCol[i].isTrigger || c.Apagadas.Contains(t) || !t.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Bounds bc = bufCol[i].bounds;
                if (t.IsChildOf(k.Raiz))
                {
                    if (bc.min.y >= azotea - 0.4f && bc.size.x <= 5f && bc.size.z <= 5f)
                    {
                        Transform h = HijoDirecto(k.Raiz, t);
                        if (h != null && !aApagar.Contains(h))
                        {
                            aApagar.Add(h);
                        }
                    }
                    continue;
                }
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

        private static readonly Color[] Paleta =
        {
            new Color(0.1f, 0.72f, 0.7f), new Color(0.95f, 0.5f, 0.14f), new Color(0.9f, 0.3f, 0.52f),
            new Color(0.95f, 0.78f, 0.2f), new Color(0.3f, 0.68f, 0.32f), new Color(0.25f, 0.42f, 0.85f),
        };

        /// <summary>Fachada y cuerpo del piso nuevo en un color saturado de la paleta (materiales propios; las ventanas emisivas no se tocan).</summary>
        private static void PintarPiso(Transform casa, int idx)
        {
            Color col = Paleta[idx];
            Material cuerpo = MatPropio("T1440_Cuerpo_" + idx, () => KitS2c.Lit(new Color(col.r * 0.85f, col.g * 0.85f, col.b * 0.85f), 0.15f, KitS2c.TexMuro()));
            foreach (Renderer r in casa.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name == "Cuerpo")
                {
                    r.sharedMaterial = cuerpo;
                    continue;
                }
                if (r.gameObject.name == "AzoteaT1318")
                {
                    continue;
                }
                Material[] ms = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material o = ms[i];
                    if (o == null || o.name.Contains("Ventana") || o.name.Contains("Neon") || (o.HasProperty("_EmissionColor") && o.IsKeywordEnabled("_EMISSION")))
                    {
                        continue;
                    }
                    string nombre = "T1440_" + o.name.Replace(" ", "_") + "_" + idx;
                    ms[i] = MatPropio(nombre, () =>
                    {
                        Material m = new Material(o);
                        m.SetColor("_BaseColor", col);
                        return m;
                    });
                    cambio = true;
                }
                if (cambio)
                {
                    r.sharedMaterials = ms;
                }
            }
        }

        /// <summary>Caja del piso: el collider de su Cuerpo (casa lisa del kit) o lo visible del modelo.</summary>
        private static Bounds CajaPiso(Transform piso)
        {
            Transform cuerpo = piso.Find("Cuerpo");
            BoxCollider col = cuerpo == null ? null : cuerpo.GetComponent<BoxCollider>();
            return col != null ? col.bounds : Cubierta(piso);
        }

        /// <summary>El piso apoya en su azotea: 5 rayos bajo su base deben coincidir (6 cm); devuelve cuánto hay que subirlo (o bajarlo) para que quede a ras.</summary>
        private static bool Asentado(Ctx c, Transform piso, out float ajuste)
        {
            ajuste = 0f;
            Bounds b = CajaPiso(piso);
            if (b.size.y < 0.1f)
            {
                return false;
            }
            List<float> hs = new List<float>();
            foreach ((float fx, float fz) in new[] { (0.5f, 0.5f), (0.2f, 0.2f), (0.8f, 0.2f), (0.2f, 0.8f), (0.8f, 0.8f) })
            {
                Vector3 o = new Vector3(Mathf.Lerp(b.min.x, b.max.x, fx), b.min.y + 0.3f, Mathf.Lerp(b.min.z, b.max.z, fz));
                int n = c.Fisica.Raycast(o, Vector3.down, bufRayos, 3f, ~0, QueryTriggerInteraction.Ignore);
                float mejor = float.MinValue;
                for (int i = 0; i < n; i++)
                {
                    if (!bufRayos[i].collider.transform.IsChildOf(piso) && bufRayos[i].point.y > mejor)
                    {
                        mejor = bufRayos[i].point.y;
                    }
                }
                if (mejor > float.MinValue)
                {
                    hs.Add(mejor);
                }
            }
            if (hs.Count < 3 || hs.Max() - hs.Min() > 0.06f)
            {
                return false;
            }
            ajuste = hs.Average() - b.min.y;
            return Mathf.Abs(ajuste) <= 0.5f;
        }

        private static void Techo(Ctx c, float x0, float x1, float z0, float z1, float y)
        {
            float yy = y + 0.07f;
            c.Techos.Quad(new Vector3(x0, yy, z0), new Vector3(x0, yy, z1), new Vector3(x1, yy, z1), new Vector3(x1, yy, z0), Vector3.up);
        }

        private static void Pisos(Ctx c)
        {
            c.Pisos = Hijo(c.Nuevo, "Pisos");
            List<Casa> casas = Casas(c);
            Vector3 k13 = new Vector3(-360f, 13f, 100f);
            // Azoteas medidas de todas las casas: se cubren con una losa de concreto manchado oscuro (malla propia, reversible porque se rehace).
            Dictionary<Casa, float> techos = new Dictionary<Casa, float>();
            foreach (Casa kk in casas)
            {
                float ry;
                if (Azotea(c, kk, out ry))
                {
                    techos[kk] = ry;
                    Techo(c, kk.X0 + 0.25f, kk.X1 - 0.25f, kk.Z0 + 0.25f, kk.Z1 - 0.25f, ry);
                }
            }
            c.TechosCasa = techos;
            HashSet<Transform> reservadas = CadenasReservadas(c);
            Log("Azoteas reservadas para las cadenas de parkour de RecorridoCerroT1361Pruebas (sin piso extra): " + reservadas.Count + ".");
            Log("Azoteas viejas cubiertas con concreto oscuro: " + techos.Count + " de " + casas.Count + " casas.");
            List<Casa> orden = casas.Where(k => TiersDe(k) >= 1f && techos.ContainsKey(k) && !reservadas.Contains(k.Raiz)).OrderBy(k =>
            {
                Vector3 centro = new Vector3(k.Centro, k.Top, (k.Z0 + k.Z1) * 0.5f);
                return Mathf.Min(Vector3.Distance(centro, A13Hacia), Vector3.Distance(centro, k13));
            }).ToList();
            Log("Casas de la ladera: " + casas.Count + ", con pisos extra planeados: " + orden.Count + ".");
            List<GameObject> modelos = new List<GameObject>();
            foreach (string nom in new[] { "CC07_CasaCerro_a", "CC06_CasaCerro_b", "CC07_CasaCerro_b", "CC06_CasaCerro_a" })
            {
                GameObject mo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Zonas/CC/" + nom + ".prefab");
                if (mo == null)
                {
                    Log("FALTA Assets/Arte/Zonas/CC/" + nom + ".prefab.");
                    continue;
                }
                Vector3 mm = Medida(mo);
                if (mm.x < 0.5f || mm.y < 0.5f || mm.z < 0.5f)
                {
                    continue;
                }
                long tris = TriangulosPrefab(mo);
                Log("Modelo " + nom + ": " + tris + " triángulos; solo para los pisos del cuadro de A13 a menos de " + DistanciaModelo + " m (máximo " + MaxModelosPiso + ").");
                modelos.Add(mo);
            }
            Log("Modelos de casa para los pisos extra: " + modelos.Count + (modelos.Count == 0 ? " (se usa la casa lisa del kit)." : "."));
            foreach (KeyValuePair<Casa, float> par in techos)
            {
                c.PtsCasa.Add(new Vector3(par.Key.Centro, par.Value, (par.Key.Z0 + par.Key.Z1) * 0.5f));
            }
            int estrechas = 0, estorbo = 0, hechas = 0, malAsentados = 0, modelosUsados = 0;
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
                float roof = techos[k];
                int tiers = (int)TiersDe(k);
                float uF, vF;
                bool enCuadro = EnCuadroA13(new Vector3(k.Centro, roof, (k.Z0 + k.Z1) * 0.5f), out uF, out vF) && uF > 0.03f && uF < 0.97f && vF > 0.05f && vF < 0.95f;
                if (!enCuadro && Vector3.Distance(new Vector3(k.Centro, roof, (k.Z0 + k.Z1) * 0.5f), PuntoBaseSur) < RadioBaseSur)
                {
                    continue; // vista "calle base al sur, mirando a la ladera" de la prueba de rendimiento
                }
                if (!enCuadro)
                {
                    tiers = Mathf.Min(tiers, 1); // fuera del cuadro de A13 solo un piso extra (menos batches)
                }
                if (c.Volumenes >= MaxPisos)
                {
                    Log("Tope de pisos extra (" + MaxPisos + "): se detiene.");
                    break;
                }
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
                    bool usaModelo = modelos.Count > 0 && modelosUsados < MaxModelosPiso && enCuadro && Vector3.Distance(A13Desde, new Vector3(k.Centro, roof, zc)) < DistanciaModelo;
                    Vector3 mitad = new Vector3(fondo * 0.5f - 0.05f, usaModelo ? AltoPisoModelo * 0.5f : 1.65f, anchoV * 0.5f - 0.05f);
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
                    Transform nuevo;
                    float alto;
                    GameObject modelo = usaModelo ? modelos[(c.Volumenes + t) % modelos.Count] : null;
                    if (modelo != null)
                    {
                        // Casa REAL (CC06/CC07/CC01/CC02) escalada al hueco: ventanas con reja, puertas, pretil y su textura.
                        Vector3 m = Medida(modelo);
                        float sx = Mathf.Clamp(anchoV / m.x, 0.55f, 1.3f), sz = Mathf.Clamp(fondo / m.z, 0.55f, 1.3f), sy = Mathf.Clamp(AltoPisoModelo / m.y, 0.5f, 0.9f);
                        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(modelo, c.Pisos);
                        g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(new Vector3(k.Normal, 0f, 0f)));
                        g.transform.localScale = new Vector3(sx, sy, sz);
                        Bounds bm = Cubierta(g.transform);
                        g.transform.position += new Vector3(xF - k.Normal * fondo * 0.5f - bm.center.x, roof - bm.min.y, zc - bm.center.z);
                        Colisionar(g);
                        nuevo = g.transform;
                        nuevo.name = "Piso_" + c.Volumenes + "_t" + t;
                        Physics.SyncTransforms();
                        Bounds bn = Cubierta(nuevo);
                        Casa tmp = new Casa { Raiz = nuevo, X0 = bn.min.x, X1 = bn.max.x, Z0 = bn.min.z, Z1 = bn.max.z, Base = bn.min.y, Top = bn.max.y, Normal = k.Normal };
                        float techoNuevo;
                        alto = Azotea(c, tmp, out techoNuevo) && techoNuevo > roof + 1.5f ? techoNuevo - roof : bn.size.y * 0.85f;
                    }
                    else
                    {
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
                        alto = KitS2c.Casa(c.Obra, c.Pisos, new Vector3(xF, roof, zc), new Vector3(k.Normal, 0f, 0f), anchoV, fondo, e);
                        nuevo = c.Pisos.GetChild(c.Pisos.childCount - 1);
                        nuevo.name = "Piso_" + c.Volumenes + "_t" + t;
                        PintarPiso(nuevo, c.Obra.Azar.Next(Paleta.Length));
                    }
                    Physics.SyncTransforms();
                    float ajuste;
                    if (!Asentado(c, nuevo, out ajuste))
                    {
                        Log("Piso " + nuevo.name + " (casa " + k.Raiz.name + "): no se asienta en la azotea medida (" + roof.ToString("0.00", CultureInfo.InvariantCulture) + "); se quita.");
                        Object.DestroyImmediate(nuevo.gameObject);
                        Physics.SyncTransforms();
                        malAsentados++;
                        break;
                    }
                    if (Mathf.Abs(ajuste) > 0.005f)
                    {
                        nuevo.position += Vector3.up * ajuste;
                        roof += ajuste;
                        Physics.SyncTransforms();
                    }
                    Lod(nuevo.gameObject, modelo != null ? CorteModelo : CortePiso);
                    if (modelo != null)
                    {
                        modelosUsados++;
                    }
                    c.Volumenes++;
                    puestos++;
                    if (modelo == null)
                    {
                        float xa = xF - k.Normal * fondo;
                        Techo(c, Mathf.Min(xF, xa) + 0.05f, Mathf.Max(xF, xa) - 0.05f, zc - anchoV * 0.5f + 0.05f, zc + anchoV * 0.5f - 0.05f, roof + alto - 0.02f);
                    }
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
            Log("Pisos con casa real (CC06/CC07): " + modelosUsados + " de " + c.Volumenes + "; el resto con la casa del kit.");
            Log("Pisos extra: " + c.Volumenes + " volúmenes en " + hechas + " casas (" + c.ConPeldanos + " con escalera de peldaños, " + c.ConEscalera + " con escalera de servicio); casas saltadas: "
                + estrechas + " muy estrechas, " + estorbo + " con algo que estorbaba, " + malAsentados + " pisos quitados por no asentar.");
        }

        /// <summary>
        /// T-1441 pone adornos (antenas, lavaderos, tinacos) sobre las azoteas viejas, también donde ya hay un piso nuevo. Esto los apaga (reversible, en T1440_Ladera/Apagados)
        /// cuando caen dentro del cuerpo de un piso. Se corre DESPUÉS de T-1441.Aplicar y ANTES de Validar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1440/Cerro ladera: despejar adornos bajo los pisos")]
        public static void DespejarDetalles()
        {
            informe = new StringBuilder("T-1440: despejar adornos bajo los pisos nuevos\n");
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Ctx c = Abrir(zona);
                c.Nuevo = zona.transform.Find(Raiz);
                Transform pisos = c.Nuevo == null ? null : c.Nuevo.Find("Pisos");
                if (pisos == null)
                {
                    Log("No hay " + Raiz + "/Pisos; corre Aplicar.");
                }
                else
                {
                    c.Marcas = c.Nuevo.Find("Apagados") ?? Hijo(c.Nuevo, "Apagados");
                    Physics.SyncTransforms();
                    c.Rends = Reunir(c);
                    int n = 0;
                    foreach (Transform piso in pisos.Cast<Transform>().Where(t => t.name.StartsWith("Piso_")).ToList())
                    {
                        Bounds cb = CajaPiso(piso);
                        Bounds b = cb;
                        b.Expand(-0.1f);
                        foreach (Rend r in c.Rends)
                        {
                            if (!r.R.gameObject.activeInHierarchy || !r.B.Intersects(b) || c.Apagadas.Contains(r.R.transform) || EsProtegido(c, r.R.transform))
                            {
                                continue;
                            }
                            if (r.B.min.y < cb.min.y - 0.4f || r.B.size.x > 5f || r.B.size.z > 5f)
                            {
                                continue;
                            }
                            Apagar(c, Unidad(c, r.R.transform, 4.5f, 6f), "adorno dentro de " + piso.name);
                            n++;
                        }
                    }
                    Transform pel = c.Nuevo.Find("EscalerasLadera/Peldanos");
                    MeshCollider mcE = pel == null ? null : pel.GetComponent<MeshCollider>();
                    if (mcE != null)
                    {
                        Bounds bb = mcE.bounds;
                        List<float> zs = new List<float>();
                        for (float z = bb.min.z + 1.5f; z <= bb.max.z - 1.5f; z += 0.5f)
                        {
                            float yy;
                            if (SuperficieEscalera(c, mcE, XEscaleraArriba + 3f, z, out yy) && !zs.Any(q => Mathf.Abs(q - z) < 4f))
                            {
                                zs.Add(z);
                            }
                        }
                        foreach (float z in zs)
                        {
                            for (float xs = XEscaleraArriba; xs < XEscaleraAbajo - 0.5f; xs += 4f)
                            {
                                float ys;
                                if (!SuperficieEscalera(c, mcE, xs + 2f, z, out ys))
                                {
                                    continue;
                                }
                                Bounds pas = new Bounds(new Vector3(xs + 2f, ys + 1.5f, z), new Vector3(4.4f, 3.4f, AnchoEscalera + 0.6f));
                                foreach (Rend r in c.Rends)
                                {
                                    if (!r.R.gameObject.activeInHierarchy || !r.B.Intersects(pas) || c.Apagadas.Contains(r.R.transform) || EsProtegido(c, r.R.transform) || r.B.size.x > 40f)
                                    {
                                        continue;
                                    }
                                    Apagar(c, Unidad(c, r.R.transform, 14f, 30f), "sobre la escalera de z " + z.ToString("0", CultureInfo.InvariantCulture));
                                    n++;
                                }
                            }
                        }
                    }
                    // Pisos sin apoyo (la casa de abajo se apagó, o el apoyo quedó a más de 0.3 m): se apagan, en pasadas porque un segundo piso puede apoyar en el primero.
                    for (int pasada = 0; pasada < 3; pasada++)
                    {
                        Physics.SyncTransforms();
                        int apagados = 0;
                        foreach (Transform piso in pisos.Cast<Transform>().Where(t => t.gameObject.activeSelf && t.name.StartsWith("Piso_")).ToList())
                        {
                            float pr;
                            if (!MedirApoyo(c, piso, CajaPiso(piso), out pr) || pr > 0.3f)
                            {
                                Transform acc = pisos.Find("Acceso_" + piso.name);
                                if (acc != null)
                                {
                                    Apagar(c, acc, "acceso de un piso sin apoyo");
                                }
                                Apagar(c, piso, "piso sin apoyo (diferencia " + pr.ToString("0.00", CultureInfo.InvariantCulture) + " m)");
                                apagados++;
                                n++;
                            }
                        }
                        if (apagados == 0)
                        {
                            break;
                        }
                    }
                    Physics.SyncTransforms();
                    int rA, rB;
                    long bA = Batches(zona.transform, out rA);
                    CombinarAccesosViejos(c, "AccesosCombinados2");
                    CombinarPisosKit(c, pisos);
                    GuardarExtra(c);
                    Physics.SyncTransforms();
                    long bB = Batches(zona.transform, out rB);
                    Log("Renderers de la zona: " + rA + " -> " + rB + "; batches estimados " + bA + " -> " + bB + ".");
                    Log("Adornos apagados: " + n + ".");
                    bool ok;
                    PrefabUtility.SaveAsPrefabAsset(zona, RutaPrefab, out ok);
                    Log(ok ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                    AssetDatabase.SaveAssets();
                }
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
            Escribir("Logs/t1440-despejar.txt", informe);
        }

        /// <summary>
        /// <summary>
        /// Peldaños de concreto sobre la terraza al frente (alza ≤ 0.2 m, huella 0.3 m) hasta la azotea del piso nuevo; si no caben a lo largo del frente,
        /// una escalera de servicio PuntoTrepar con su riel y peldaños (como las de T-1355).
        /// </summary>
        private static void Acceso(Ctx c, Casa k, Transform piso, float xF, float roofAbajo, float alto, float zc, float anchoV, float terraza)
        {
            int pasos = Mathf.Max(1, Mathf.CeilToInt(alto / AltoPasoMaximo));
            float alza = alto / pasos;
            float largo = pasos * FondoPeldanoPiso;
            bool cabe = false; // ronda 3: nada de peldaños en medio de la terraza (salían blancos y dominaban la foto): escalera de servicio contra la pared
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
            go.transform.SetParent(c.Pisos, false); // no del piso: el modelo va escalado y torcería el trigger
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
            ObjetoMalla(c, Hijo(c.Nuevo, "Techos"), "TechosOscuros", c.Techos, c.Oscuro, false, true);
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

        private static Vector3 EnCalle(Ctx c, float x, float z, float yRef)
        {
            float y;
            if (Piso(c, x, z, yRef + 10f, true, out y) && Mathf.Abs(y - yRef) < 2f)
            {
                return new Vector3(x, y + 0.05f, z);
            }
            return new Vector3(x, yRef + 0.05f, z);
        }

        /// <summary>Charco de luz falso: un cuadrado de 2 x mitad (textura radial de borde suave) que sigue la altura de la calle.</summary>
        private static void CharcoLuz(Ctx c, Vector3 centro, float yRef, float mitad)
        {
            Vector3 a = EnCalle(c, centro.x - mitad, centro.z - mitad, yRef), b = EnCalle(c, centro.x - mitad, centro.z + mitad, yRef);
            Vector3 d = EnCalle(c, centro.x + mitad, centro.z + mitad, yRef), f = EnCalle(c, centro.x + mitad, centro.z - mitad, yRef);
            c.Charcos.Quad(a, b, d, f, Vector3.up, new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
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
                int ia = Mathf.Max(0, i - 4), ib = Mathf.Min(eje.Count - 1, i + 4);
                Vector3 t1 = new Vector3(eje[i].x - eje[ia].x, 0f, eje[i].z - eje[ia].z), t2 = new Vector3(eje[ib].x - eje[i].x, 0f, eje[ib].z - eje[i].z);
                if (t1.sqrMagnitude > 0.01f && t2.sqrMagnitude > 0.01f && Vector3.Angle(t1, t2) > 5f)
                {
                    // En las curvas no van postes (el carro se abre hacia afuera): solo el charco de luz sobre el eje.
                    float yc;
                    if (Piso(c, eje[i].x, eje[i].z, 200f, true, out yc))
                    {
                        CharcoLuz(c, new Vector3(eje[i].x, 0f, eje[i].z), yc, 6f);
                    }
                    descartados++;
                    continue;
                }
                float yCalle, yTodo;
                if (!Piso(c, xz.x, xz.z, 200f, true, out yCalle) || !Piso(c, xz.x, xz.z, 200f, false, out yTodo) || yTodo - yCalle > 0.6f)
                {
                    descartados++;
                    continue;
                }
                float dEje = float.MaxValue;
                foreach (Vector3 q in eje)
                {
                    dEje = Mathf.Min(dEje, new Vector2(q.x - xz.x, q.z - xz.z).magnitude);
                }
                if (dEje < 4.9f)
                {
                    descartados++; // en el carril (el arroyo mide 4.5 m de mitad)
                    continue;
                }
                Vector3 pie = new Vector3(xz.x, yTodo, xz.z); // el piso más alto bajo el poste (terreno o calle): asentado sobre lo que toca
                if (CercaDeEscaleraCE01(pie) || viejos.Any(v => new Vector2(v.x - pie.x, v.z - pie.z).magnitude < DistanciaPosteViejo) || !LibreCaja(c, pie + Vector3.up * 2f, new Vector3(0.35f, 2f, 0.35f)))
                {
                    descartados++;
                    continue;
                }
                plan.Add(new PostePlan { Pie = pie, HaciaCalle = -izq * lado, Dist = Mathf.Min(Vector3.Distance(pie, A13Hacia), Vector3.Distance(pie, k13)), Pierna = i });
            }
            Log("Postes planeados: " + plan.Count + " (" + descartados + " descartados por poste viejo a menos de " + DistanciaPosteViejo + " m, sin calle o con algo en el lugar).");
            HashSet<PostePlan> conLuz = new HashSet<PostePlan>(plan.OrderBy(p => p.Dist).Take(LucesPostes));
            List<Vector3> puntas = new List<Vector3>();
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
                CharcoLuz(c, new Vector3(cabeza.x, 0f, cabeza.z), p.Pie.y, 6.5f);
                puntas.Add(punta);
                c.PtsPoste.Add(punta);
            }
            // Los postes viejos del zigzag también reciben su charco (sin Light nueva): así toda la calle queda iluminada en la vista aérea.
            int charcosViejos = 0;
            foreach (Vector3 v in viejos)
            {
                float dmin = float.MaxValue;
                Vector3 q0 = v;
                foreach (Vector3 q in eje)
                {
                    float d = new Vector2(q.x - v.x, q.z - v.z).magnitude;
                    if (d < dmin)
                    {
                        dmin = d;
                        q0 = q;
                    }
                }
                if (dmin > 9f)
                {
                    continue;
                }
                Vector3 haciaEje = new Vector3(q0.x - v.x, 0f, q0.z - v.z).normalized;
                CharcoLuz(c, new Vector3(v.x + haciaEje.x * 2.5f, 0f, v.z + haciaEje.z * 2.5f), v.y, 6f);
                charcosViejos++;
                c.PtsPoste.Add(v + Vector3.up * 6.5f);
            }
            int cables = 0;
            for (int i = 0; i + 1 < puntas.Count; i++)
            {
                float d = Vector3.Distance(puntas[i], puntas[i + 1]);
                if (d > 30f || d < 3f)
                {
                    continue;
                }
                foreach ((Vector3 a, Vector3 b, float flecha) in new[]
                {
                    (puntas[i] + Vector3.up * 0.2f, puntas[i + 1] + Vector3.up * 0.2f, 0.7f),
                    (puntas[i] - Vector3.up * 0.35f, puntas[i + 1] - Vector3.up * 0.35f, 0.9f),
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
            int bajadas = 0;
            foreach (Light l in c.Nuevo.GetComponentsInChildren<Light>(true))
            {
                if (l.range > RendimientoCerroT1365.AlcanceMaximoLuz)
                {
                    l.range = RendimientoCerroT1365.AlcanceMaximoLuz;
                    bajadas++;
                }
            }
            Log("Luces de T1440_Ladera con el alcance bajado a " + RendimientoCerroT1365.AlcanceMaximoLuz + " m (tope de RendimientoCerroT1365): " + bajadas + ".");
            Log("Postes nuevos: " + plan.Count + " (" + reales + " con Light real y sodio, " + baratos + " con cabeza emisiva y charco falso), charcos extra en postes viejos " + charcosViejos + ", cables " + cables + ".");
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

        // ================================================================ Taludes: muros CC08 con lámpara donde la ladera queda sobre la calle

        private static void Taludes(Ctx c)
        {
            GameObject muro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Zonas/CC/CC08_MuroContencion_a.prefab");
            if (muro == null)
            {
                Log("FALTA CC08_MuroContencion_a.prefab; sin taludes.");
                return;
            }
            Vector3 mm = Medida(muro);
            if (mm.x < 0.1f || mm.y < 0.1f || mm.z < 0.1f)
            {
                return;
            }
            Transform padre = Hijo(c.Nuevo, "Taludes");
            List<Vector3> eje = CerroT1355Trazado.Zigzag();
            int n = 0, rechazados = 0;
            float acum = 0f;
            for (int i = 1; i + 1 < eje.Count && n < 40; i++)
            {
                acum += new Vector2(eje[i].x - eje[i - 1].x, eje[i].z - eje[i - 1].z).magnitude;
                if (acum < 6f)
                {
                    continue;
                }
                Vector3 tan = new Vector3(eje[i + 1].x - eje[i - 1].x, 0f, eje[i + 1].z - eje[i - 1].z);
                if (tan.sqrMagnitude < 0.01f)
                {
                    continue;
                }
                tan.Normalize();
                if (Mathf.Abs(tan.z) < 0.97f)
                {
                    continue; // tramos rectos norte-sur: el muro corre a lo largo de z
                }
                acum = 0f;
                Vector3 izq = new Vector3(-tan.z, 0f, tan.x);
                foreach (float lado in new[] { 1f, -1f })
                {
                    Vector3 baseXz = new Vector3(eje[i].x, 0f, eje[i].z) + izq * lado * 5.25f;
                    Vector3 altoXz = new Vector3(eje[i].x, 0f, eje[i].z) + izq * lado * 7.2f;
                    float yB, yT;
                    if (!Piso(c, baseXz.x, baseXz.z, 200f, true, out yB) || !Piso(c, altoXz.x, altoXz.z, 200f, false, out yT) || yT - yB < 0.9f)
                    {
                        continue;
                    }
                    Vector3 haciaCalle = -izq * lado;
                    Vector3 centro = new Vector3(eje[i].x, yB, eje[i].z) + izq * lado * 6.4f;
                    float u, v;
                    if (!EnCuadroA13(centro + Vector3.up * 1.5f, out u, out v))
                    {
                        continue;
                    }
                    float alto = Mathf.Clamp(yT - yB + 0.5f, 1.2f, 3.2f);
                    if (!LibreCaja(c, centro + Vector3.up * (alto * 0.5f + 0.3f), new Vector3(0.4f, alto * 0.5f - 0.1f, 2.8f)))
                    {
                        rechazados++;
                        continue;
                    }
                    GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(muro, padre);
                    g.name = "CC08_Talud_" + n;
                    g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(haciaCalle));
                    g.transform.localScale = new Vector3(6f / mm.x, alto / mm.y, 1f);
                    Bounds b = Cubierta(g.transform);
                    g.transform.position += new Vector3(centro.x - b.center.x, yB - 0.3f - b.min.y, centro.z - b.center.z);
                    Colisionar(g);
                    Physics.SyncTransforms();
                    if (n % 2 == 0)
                    {
                        // Lámpara en lo alto del muro: poste oscuro y cabeza emisiva naranja (sin Light).
                        float yTop = yB - 0.3f + alto;
                        Vector3 pie = new Vector3(centro.x, yTop, centro.z) + haciaCalle * 0.2f;
                        c.Escalerillas.Caja(pie - new Vector3(0.04f, 0f, 0.04f), pie + new Vector3(0.04f, 0.7f, 0.04f));
                        Vector3 cab = pie + Vector3.up * 0.8f;
                        c.Brillo.Caja(cab - new Vector3(0.2f, 0.1f, 0.2f), cab + new Vector3(0.2f, 0.1f, 0.2f));
                        CharcoLuz(c, new Vector3(cab.x + haciaCalle.x * 2f, 0f, cab.z), yB, 4.5f);
                    }
                    n++;
                }
            }
            Log("Taludes con muro CC08 en el cuadro de A13: " + n + " (" + rechazados + " rechazados por haber algo en el lugar).");
        }

        // ================================================================ Neones chicos y ventanas con luz cálida en las fachadas que miran a la cámara

        private static void Placa(Malla m, Vector3 centro, Vector3 normal, Vector3 along, float w, float h)
        {
            Vector3 a = centro - along * w * 0.5f - Vector3.up * h * 0.5f, b = centro + along * w * 0.5f - Vector3.up * h * 0.5f;
            Vector3 cc = centro + along * w * 0.5f + Vector3.up * h * 0.5f, d = centro - along * w * 0.5f + Vector3.up * h * 0.5f;
            m.Quad(a, b, cc, d, normal);
        }

        private sealed class Cara
        {
            public float X0, X1, Z0, Z1, Base, Roof;
        }

        private static void Fachadas(Ctx c)
        {
            Material magenta = MatPropio("T1440_PantallaMagenta", () => Emisivo(new Color(0.05f, 0.01f, 0.04f), new Color(1f, 0.2f, 0.78f) * 3.5f));
            Material turquesa = MatPropio("T1440_PantallaTurquesa", () => Emisivo(new Color(0.01f, 0.05f, 0.05f), new Color(0.1f, 0.95f, 0.88f) * 3.5f));
            Material calida = MatPropio("T1440_VentanaCalida", () => Emisivo(new Color(0.06f, 0.04f, 0.02f), new Color(1f, 0.62f, 0.25f) * 3.2f));
            Malla nm = new Malla(), nt = new Malla(), vc = new Malla();
            List<Cara> obj = new List<Cara>();
            foreach (KeyValuePair<Casa, float> par in c.TechosCasa)
            {
                obj.Add(new Cara { X0 = par.Key.X0, X1 = par.Key.X1, Z0 = par.Key.Z0, Z1 = par.Key.Z1, Base = par.Value - 5.5f, Roof = par.Value });
            }
            Transform pisos = c.Nuevo.Find("Pisos");
            if (pisos != null)
            {
                foreach (Transform p in pisos.Cast<Transform>().Where(t => t.gameObject.activeSelf && t.name.StartsWith("Piso_")))
                {
                    Bounds b = CajaPiso(p);
                    obj.Add(new Cara { X0 = b.min.x, X1 = b.max.x, Z0 = b.min.z, Z1 = b.max.z, Base = b.min.y, Roof = b.min.y + b.size.y * 0.85f });
                }
            }
            Vector3 cam = A13Desde;
            List<Cara> orden = obj.OrderBy(o => Vector3.Distance(new Vector3((o.X0 + o.X1) * 0.5f, o.Roof, (o.Z0 + o.Z1) * 0.5f), A13Hacia)).ToList();
            int neones = 0, casasConLuz = 0, enCuadro = 0, ventanas = 0;
            foreach (Cara o in orden)
            {
                float u, v;
                Vector3 centro = new Vector3((o.X0 + o.X1) * 0.5f, o.Roof - 2f, (o.Z0 + o.Z1) * 0.5f);
                if (!EnCuadroA13(centro, out u, out v) || u < 0.03f || u > 0.97f || v < 0.05f || v > 0.95f)
                {
                    continue;
                }
                enCuadro++;
                // La cara que mira a la cámara y se ve (sin nada en medio): oeste (x0) o norte (z1).
                Vector3 normal = Vector3.zero, along = Vector3.zero, punto = Vector3.zero;
                float ancho = 0f;
                for (int intento = 0; intento < 2 && normal == Vector3.zero; intento++)
                {
                    Vector3 n2, a2, p2;
                    float w2;
                    if (intento == 0 && cam.x < o.X0 - 1f)
                    {
                        n2 = Vector3.left;
                        a2 = Vector3.forward;
                        p2 = new Vector3(o.X0, o.Roof - 2f, (o.Z0 + o.Z1) * 0.5f);
                        w2 = o.Z1 - o.Z0;
                    }
                    else if (intento == 1 && cam.z > o.Z1 + 1f)
                    {
                        n2 = Vector3.forward;
                        a2 = Vector3.right;
                        p2 = new Vector3((o.X0 + o.X1) * 0.5f, o.Roof - 2f, o.Z1);
                        w2 = o.X1 - o.X0;
                    }
                    else
                    {
                        continue;
                    }
                    Vector3 origen = p2 + n2 * 0.3f;
                    Vector3 dir = cam - origen;
                    if (c.Fisica.Raycast(origen, dir.normalized, bufRayos, dir.magnitude, ~0, QueryTriggerInteraction.Ignore) == 0)
                    {
                        normal = n2;
                        along = a2;
                        punto = p2;
                        ancho = w2;
                    }
                }
                if (normal == Vector3.zero || ancho < 3f)
                {
                    continue;
                }
                if (c.Obra.Chance(0.3f))
                {
                    int nv = ancho > 5.5f ? 3 : 2;
                    for (int k = 0; k < nv; k++)
                    {
                        float t = (k + 1f) / (nv + 1f) - 0.5f;
                        Placa(vc, new Vector3(punto.x, o.Roof - 1.7f, punto.z) + along * (t * ancho * 0.85f) + normal * 0.07f, normal, along, 0.9f, 1.1f);
                        ventanas++;
                        if (o.Roof - o.Base > 7f && c.Obra.Chance(0.5f))
                        {
                            Placa(vc, new Vector3(punto.x, o.Roof - 4.6f, punto.z) + along * (t * ancho * 0.85f) + normal * 0.07f, normal, along, 0.9f, 1.1f);
                            ventanas++;
                        }
                    }
                    casasConLuz++;
                }
                if (neones < 50 && c.Obra.Chance(0.45f))
                {
                    bool horizontal = neones % 3 != 2;
                    Vector3 pn = new Vector3(punto.x, o.Roof - (horizontal ? 3.3f : 3.8f), punto.z) + along * (c.Obra.Rango(-0.3f, 0.3f) * ancho) + normal * 0.09f;
                    Malla destino = neones % 2 == 0 ? nm : nt;
                    Placa(destino, pn, normal, along, horizontal ? 1.9f : 0.5f, horizontal ? 0.55f : 1.6f);
                    neones++;
                }
            }
            Transform padre = Hijo(c.Nuevo, "Fachadas");
            ObjetoMalla(c, padre, "NeonesMagenta", nm, magenta, false, true);
            ObjetoMalla(c, padre, "NeonesTurquesa", nt, turquesa, false, true);
            ObjetoMalla(c, padre, "VentanasCalidas", vc, calida, false, true);
            Log("Fachadas hacia A13: " + enCuadro + " casas y pisos en el cuadro, " + neones + " neones chicos, " + ventanas + " ventanas cálidas en " + casasConLuz + " de ellos (emisión, sin Light).");
        }

        /// <summary>Muestras (cada 0.5 m) de los ejes de todas las CalleZona activas de la zona: lo que usa BarridoZonasT1360 para "carro en el carril".</summary>
        private static List<Vector3> EjesCalleZona(Ctx c)
        {
            List<Vector3> l = new List<Vector3>();
            foreach (CalleZona cz in c.Zona.GetComponentsInChildren<CalleZona>(false))
            {
                IList<Vector3> pts = cz.Puntos();
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float largo = Vector3.Distance(pts[i], pts[i + 1]);
                    int k = Mathf.Max(1, Mathf.CeilToInt(largo / 0.5f));
                    for (int j = 0; j < k; j++)
                    {
                        l.Add(Vector3.Lerp(pts[i], pts[i + 1], j / (float)k));
                    }
                }
                if (pts.Count > 0)
                {
                    l.Add(pts[pts.Count - 1]);
                }
            }
            return l;
        }

        /// <summary>Distancia en planta del rectángulo de <paramref name="b"/> a la muestra de eje más cercana (0 si un eje pasa por dentro).</summary>
        private static float DistanciaAEjes(Bounds b, List<Vector3> ejes)
        {
            float min = float.MaxValue;
            foreach (Vector3 s in ejes)
            {
                float dx = Mathf.Max(0f, Mathf.Max(b.min.x - s.x, s.x - b.max.x));
                float dz = Mathf.Max(0f, Mathf.Max(b.min.z - s.z, s.z - b.max.z));
                min = Mathf.Min(min, Mathf.Sqrt(dx * dx + dz * dz));
            }
            return min;
        }

        // ================================================================ Carros viejos sobre las piernas rectas del zigzag

        private static void Carros(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Carros");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab");
            if (prefab == null)
            {
                Log("FALTA V01a_CarroViejito.prefab; sin carros.");
                return;
            }
            List<Vector3> eje = CerroT1355Trazado.Zigzag();
            List<Vector3> ejesZona = EjesCalleZona(c);
            int n = 0, lado = 1;
            float acum = 0f;
            for (int i = 1; i + 1 < eje.Count && n < MaxCarros; i++)
            {
                acum += new Vector2(eje[i].x - eje[i - 1].x, eje[i].z - eje[i - 1].z).magnitude;
                if (acum < 40f)
                {
                    continue;
                }
                Vector3 tan = new Vector3(eje[i + 1].x - eje[i - 1].x, 0f, eje[i + 1].z - eje[i - 1].z);
                if (tan.sqrMagnitude < 0.01f)
                {
                    continue;
                }
                tan.Normalize();
                if (Mathf.Abs(tan.z) < 0.97f)
                {
                    continue; // solo tramos rectos de norte a sur (la caja de revisión va alineada a los ejes)
                }
                acum = 0f;
                lado = -lado;
                Vector3 izq = new Vector3(-tan.z, 0f, tan.x);
                Vector3 xz = new Vector3(eje[i].x, 0f, eje[i].z) + izq * lado * 3.2f;
                float y;
                if (!Piso(c, xz.x, xz.z, 200f, true, out y))
                {
                    continue;
                }
                Vector3 pos = new Vector3(xz.x, y + 0.03f, xz.z);
                if (CercaDeEscaleraCE01(pos) || !LibreCaja(c, pos + Vector3.up * 0.9f, new Vector3(1.0f, 0.6f, 2.2f)))
                {
                    continue;
                }
                GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                car.name = "CarroViejo_T1440_" + n;
                car.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(lado > 0 ? tan : -tan));
                Physics.SyncTransforms();
                Bounds b = Cubierta(car.transform);
                car.transform.position += Vector3.up * (y - b.min.y);
                Physics.SyncTransforms();
                b = Cubierta(car.transform);
                if (DistanciaAEjes(b, ejesZona) < 1.5f)
                {
                    Object.DestroyImmediate(car); // BarridoZonasT1360: "carro en el carril" a menos de 1 m de un eje de CalleZona
                    Physics.SyncTransforms();
                    continue;
                }
                Lod(car, CorteCarro);
                Physics.SyncTransforms();
                n++;
            }
            Log("Carros viejos sobre el zigzag: " + n + " (tope " + MaxCarros + ", " + TriangulosPrefab(prefab) + " triángulos cada uno, LODGroup con culling al " + (CorteCarro * 100f) + " % de pantalla).");
        }

        // ================================================================ 4. Ciudad brillante abajo (YA NO SE USA: la ciudad de verdad está en la escena)

        /// <summary>
        /// La ciudad es una FRANJA en el horizonte, bajo el nivel del Cerro: paneles anchos (25 a 70 m) y bajos (15 a 60 m de alto) a 300-700 m de las cámaras, ninguno a menos de
        /// 250 m de A13 ni de K13b, con la base bajo el piso de la ciudad (-30 a -15 m) para que no cuelguen del cielo.
        /// </summary>
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
            int hechas = 0, intentos = 0;
            float distMin = float.MaxValue;
            while (hechas < 150 && intentos < 4000)
            {
                intentos++;
                bool paraK13 = hechas >= 100;
                Vector3 origen = paraK13 ? k13 : A13Desde;
                Vector3 dir;
                if (paraK13)
                {
                    // K13b mira al sur: la ciudad queda a la izquierda (este).
                    float ang = -(float)azar.NextDouble() * 75f;
                    dir = Quaternion.Euler(0f, 180f + ang, 0f) * Vector3.forward;
                }
                else
                {
                    float ang = ((float)azar.NextDouble() * 2f - 1f) * 58f;
                    dir = Quaternion.Euler(0f, ang, 0f) * horizA;
                }
                float dist = 300f + (float)azar.NextDouble() * 400f;
                Vector3 centro = new Vector3(origen.x + dir.x * dist, 0f, origen.z + dir.z * dist);
                float dA = new Vector2(centro.x - A13Desde.x, centro.z - A13Desde.z).magnitude;
                float dK = new Vector2(centro.x - k13.x, centro.z - k13.z).magnitude;
                if (centro.x < -262f || dA < 250f || dK < 250f)
                {
                    continue;
                }
                distMin = Mathf.Min(distMin, Mathf.Min(dA, dK));
                float ancho = 25f + (float)azar.NextDouble() * 45f;
                float alto = 15f + (float)azar.NextDouble() * 45f;
                float suelo = -30f + (float)azar.NextDouble() * 15f;
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
                    float aw = ancho * 0.22f, ah = 3f + (float)azar.NextDouble() * 8f;
                    pantallas[tipo].Quad(pc - derecha * aw * 0.5f - Vector3.up * ah * 0.5f, pc + derecha * aw * 0.5f - Vector3.up * ah * 0.5f, pc + derecha * aw * 0.5f + Vector3.up * ah * 0.5f,
                        pc - derecha * aw * 0.5f + Vector3.up * ah * 0.5f, normal, new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                }
                c.PtsCiudad.Add(centro + Vector3.up * (suelo + alto));
                hechas++;
            }
            ObjetoMalla(c, padre, "PanelesVentanas", torres, paneles, false, true);
            ObjetoMalla(c, padre, "PantallasMagenta", pantallas[0], magenta, false, true);
            ObjetoMalla(c, padre, "PantallasTurquesa", pantallas[1], turquesa, false, true);
            ObjetoMalla(c, padre, "PantallasAmbar", pantallas[2], ambar, false, true);
            Log("Ciudad lejana: " + hechas + " paneles de ventanas (300 a 700 m, 25-70 m de ancho, 15-60 m de alto, base entre -30 y -15 m; el más cercano a " + distMin.ToString("0", CultureInfo.InvariantCulture)
                + " m de A13/K13b), sin colliders ni sombras. SK01_Skyline ya lo pone la escena (arco de 900 m); aquí no se repite.");
        }

        // ================================================================ Estático

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t == c.Nuevo || t.name.StartsWith("CarroViejo_") || t.GetComponentInParent<Rigidbody>() != null || t.GetComponent<Light>() != null || t.GetComponentInParent<Rigidbody>() != null || t.GetComponent<MeshRenderer>() == null)
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
            List<Transform> lista = pisos.Cast<Transform>().Where(t => t.gameObject.activeSelf && t.name.StartsWith("Piso_")).ToList();
            foreach (Transform p in lista)
            {
                total++;
                Bounds b = CajaPiso(p);
                float peor;
                bool tocaAbajo = MedirApoyo(c, p, b, out peor);
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
                        sb.AppendLine("AVISO: " + p.name + " desajustado " + peor.ToString("0.000", CultureInfo.InvariantCulture) + " m.");
                        avisos++;
                    }
                }
                // Encimado: ningún collider ajeno dentro de su caja (se encoge 8 cm por lado).
                int k = c.Fisica.OverlapBox(b.center, b.extents - new Vector3(0.08f, 0.08f, 0.08f), bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < k; i++)
                {
                    Transform t = bufCol[i].transform;
                    if (t.IsChildOf(p) || bufCol[i].isTrigger || EsPiso(c, t))
                    {
                        continue;
                    }
                    Bounds bc = bufCol[i].bounds;
                    if (bc.min.y > b.min.y - 0.05f && !t.IsChildOf(c.Nuevo))
                    {
                        encimados++;
                        sb.AppendLine("FALLO: " + p.name + " se encima con " + Ruta(t));
                        fallos++;
                        break;
                    }
                }
                // Acceso: una escalera de servicio PuntoTrepar propia (Acceso_<piso>).
                Transform acc = pisos.Find("Acceso_" + p.name);
                if (acc == null || acc.GetComponent<PuntoTrepar>() == null)
                {
                    sinAcceso++;
                    sb.AppendLine("FALLO: " + p.name + " sin escalera de servicio PuntoTrepar.");
                    fallos++;
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
                    if (p.name.StartsWith("Poste") || p.name.StartsWith("CC08_") || p.name.StartsWith("CE0") || p.name.StartsWith("CarroViejo_"))
                    {
                        piezaDeKit = true;
                    }
                }
                if (piezaDeKit)
                {
                    continue;
                }
                Renderer r = col.GetComponent<Renderer>();
                if ((r == null || !r.enabled) && col.gameObject.name == "Cuerpo" && col.transform.parent != null && col.transform.parent.name.StartsWith("Piso_"))
                {
                    continue; // piso del kit combinado: la caja es la de la malla combinada (PisosKitCombinados)
                }
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
