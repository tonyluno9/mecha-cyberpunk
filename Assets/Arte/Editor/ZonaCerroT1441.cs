using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1441: detalle y vida de la Colonia del Cerro (K13 y A13). Corre DESPUÉS de T-1440 (alturas de casas, muros, escaleras y postes). Todo lo nuevo cuelga de la raíz
    /// "T1441_Detalle" del prefab T1355_Cerro, que Aplicar borra y rehace (se puede correr dos veces). No apaga ni mueve nada: todo se coloca al momento de Aplicar
    /// por rayo / bounds sobre lo que haya (nada con alturas fijas):
    ///  - Fachadas (casas del kit a menos de 100 m de la cámara K13): aires, medidores, lámparas de pared con emisión cálida, rejas en las ventanas, toldos de lámina
    ///    en los locales, macetas junto a la puerta y letreros de neón chicos (magenta, turquesa y verde, emisivos, sin luces).
    ///  - Azoteas (todas las casas): hasta quedar con 2 a 5 cosas (tinacos negros con base, antenas, parabólicas, tendederos, lavaderos, varillas de castillo, cajas,
    ///    tambos, chatarra, aires), sin cerrar un pasillo de 1.2 m entre los enlaces y accesos de azotea y sin encimarse.
    ///  - Cables: racimos de 3 hilos entre postes, de poste a fachada y de fachada a fachada cruzando la calle (catenaria con el ayudante de KitS2c); nada a menos de 4 m de K13.
    ///  - Calle: carros estacionados pegados a la banqueta (uno con faros encendidos y luz de piso turquesa), basura, bolsas, huacales, macetas y charcos.
    ///  - Gente: NPCs de adorno (NP07 a NP14) parados en las banquetas y en azoteas, con su Animator y en pose de reposo, sin colliders.
    /// Orden: T1385, T1394, T1401, T1405, T1406, T1416, T1440 (Ladera), luego T1441.Aplicar; T1441.Validar; Fotos en una corrida de Unity aparte.
    /// </summary>
    public static class ZonaCerroT1441
    {
        public const string RutaPrefab = ZonaCerroT1385.RutaPrefab;
        public const string Raiz = "T1441_Detalle";
        public const string RutaInforme = "Logs/t1441-aplicar.txt";
        public const string RutaValidar = "Logs/t1441-validar.txt";
        public const string CarpetaMateriales = "Assets/Arte/Mundo/Cerro";
        public const string CarpetaFotos = "docs/capturas/_vista19b";
        public const int Semilla = 14410;
        public const long TopeBytesPrefab = 60L * 1024 * 1024;
        public const int TopeLucesNuevas = 8;
        public const int TopeObjetosNuevos = 3000;

        public static readonly Vector3 K13Desde = ZonaCerroT1416.K13bDesde;
        public static readonly Vector3 K13Hacia = ZonaCerroT1416.K13bHacia;
        public const float CampoK13 = ZonaCerroT1416.CampoFoto;
        public const float CampoA13 = ZonaCerroT1440.CampoA13;

        private const float DistanciaFachadas = 100f;
        private const float DistanciaCable = 4.5f;
        private const float AnchoPasillo = 1.2f;
        private const float XCentroCalle = CerroT1355Trazado.XPierna2;
        private const float MitadCarril = 1.9f;
        private const float ZVistaNorte = 148f;
        private const float ZVistaSur = 24f;
        private const float AltoBanqueta = 0.2f;

        private static readonly string[] RutasCarros =
        {
            "Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab", "Assets/Arte/Vehiculos/Prefabs/V01b_CarroPro.prefab", "Assets/Arte/Vehiculos/Prefabs/V02.prefab",
            "Assets/Arte/Vehiculos/Prefabs/V03.prefab", "Assets/Arte/Vehiculos/Prefabs/V04.prefab", "Assets/Arte/Vehiculos/Prefabs/V05.prefab",
        };

        private static readonly string[] Gente =
        {
            "NP10_Elotero_a", "NP12_Abuela_a", "NP13_Repartidor_a", "NP13_Repartidor_b", "NP14_Oficinista_a", "NP14_Oficinista_b", "NP09_Corredor_a", "NP09_Corredor_b",
            "NP08_Turista_a", "NP08_Turista_b", "NP07_Pescador_a", "NP11_Estibador_a",
        };

        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[256];
        private static readonly Collider[] bufCol = new Collider[256];
        private static readonly Dictionary<string, Bounds> medidas = new Dictionary<string, Bounds>();
        private static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        private static readonly List<string> faltan = new List<string>();
        private static readonly Dictionary<string, Vector3> planNpc = new Dictionary<string, Vector3>();

        private sealed class Techo
        {
            public VariarCerroT1364.CasaT H;
            public float Nivel;
            public int Existentes;
            /// <summary>Altura exacta de la superficie en el último hueco encontrado.</summary>
            public float Y;
            public readonly List<Bounds> Enlaces = new List<Bounds>();
            public readonly List<(Vector2 a, Vector2 b)> Pasillos = new List<(Vector2, Vector2)>();
            /// <summary>Corredores de salto entre azoteas vecinas (como los recorre RecorridoCerroT1361Pruebas.SaltarEntre): del punto 1 m dentro del borde lejano al centro de la vecina.</summary>
            public readonly List<(Vector2 a, Vector2 b)> Corredores = new List<(Vector2, Vector2)>();
            public Transform Grupo;
        }

        private sealed class Ctx
        {
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Terreno, Calles, Escaleras, Nuevo;
            public KitS2c.Obra Obra;
            public List<VariarCerroT1364.CasaT> Casas;
            public readonly List<Techo> Techos = new List<Techo>();
            public readonly List<Bounds> Ocupado = new List<Bounds>();
            public readonly List<Bounds> OcupadoPared = new List<Bounds>();
            public readonly List<Bounds> Enlaces = new List<Bounds>();
            public readonly List<Vector3> Postes = new List<Vector3>();
            public int Contador;
            /// <summary>Cuadros libres (al pie y en la salida de cada escalera, junto a cada PuntoTrepar): nada mío adentro (ni carros, basura, gente, postes, tinacos, rejas ni neones).</summary>
            public readonly List<Bounds> Libres = new List<Bounds>();
            /// <summary>Posición de las lámparas y neones de fachada (la gente se pone junto a ellos para que se lea).</summary>
            public readonly List<(Vector3 pos, float normal)> LucesFachada = new List<(Vector3, float)>();
            public int Carros, Letreros, Fachadas, Azoteas, Cables, Gente, Basura, Charcos;
            public Material Marco, MarcoOxido, Concreto, Gris, Negro, Madera;
            public Material[] Neon;
            public Material Calido;
            public Material[] Bolsas;
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1441/Cerro detalle y vida: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1441: detalle y vida del Cerro (" + RutaPrefab + ")\n");
            medidas.Clear();
            cache.Clear();
            faltan.Clear();
            lotes.Clear();
            planNpc.Clear();
            rechazadosApoyo = 0;
            pegados = 0;
            conservados = 0;
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                int lucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                Log("Luces antes: " + lucesAntes + ". Tamaño del prefab antes: " + Megas(RutaPrefab) + " MB.");
                Ctx c = new Ctx
                {
                    Zona = zona,
                    Fisica = zona.scene.GetPhysicsScene(),
                    Terreno = zona.transform.Find("Terreno"),
                    Calles = zona.transform.Find("Calles"),
                    Escaleras = zona.transform.Find("Escaleras"),
                };
                LimpiarRejasViejas(zona);
                c.Obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                Physics.SyncTransforms();
                actual = c;
                calleMedida.Clear();
                Materiales(c);
                c.Casas = VariarCerroT1364.Leer(zona.transform).Where(h => h.Raiz != null && h.Raiz.gameObject.activeInHierarchy).ToList();
                Log("Casas activas: " + c.Casas.Count + " (kit " + c.Casas.Count(h => h.Kit) + ").");
                Escanear(c);
                Paso("carros", () => Carros(c));
                Paso("fachadas", () => Fachadas(c));
                Paso("azoteas", () => Azoteas(c));
                Paso("basura, bolsas, macetas y charcos", () => Basura(c));
                Paso("gente", () => PonerGente(c));
                Paso("postes del paseo", () => PostesPaseo(c));
                Paso("cables", () => Cableado(c));
                Paso("tendederos a la vista de K13", () => TendederosK13(c));
                Paso("tendederos entre casas", () => Tendederos(c));
                Paso("lotes", () => Volcar(c));
                Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs"));
                File.WriteAllLines(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "t1441-npcs.txt"),
                    planNpc.Select(p => p.Key + "|" + p.Value.x.ToString("R", CultureInfo.InvariantCulture) + "|" + p.Value.y.ToString("R", CultureInfo.InvariantCulture) + "|" + p.Value.z.ToString("R", CultureInfo.InvariantCulture)).ToArray());
                Paso("poda", () => Podar(c));
                Paso("LOD", () => AgruparLod(c));
                Estatico(c);
                KitS2c.Terminar(c.Obra);
                int luces = zona.GetComponentsInChildren<Light>(true).Length;
                int nuevas = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                int objetos = c.Nuevo.GetComponentsInChildren<Transform>(true).Length - 1;
                Log("Luces después: " + luces + " (nuevas " + nuevas + ", tope " + TopeLucesNuevas + "). Objetos nuevos: " + objetos + " (tope " + TopeObjetosNuevos + ").");
                Log("Resumen: carros " + c.Carros + ", casas con fachada " + c.Fachadas + ", letreros " + c.Letreros + ", azoteas con objetos nuevos " + c.Azoteas + ", cables " + c.Cables
                    + ", basura " + c.Basura + ", charcos " + c.Charcos + ", gente " + c.Gente + ".");
                if (nuevas > TopeLucesNuevas || objetos > TopeObjetosNuevos)
                {
                    Log("AVISO: se pasó de un tope.");
                }
                Physics.SyncTransforms();
                bool ok;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(zona, RutaPrefab, out ok);
                Log(ok && guardado != null ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log("Tamaño del prefab después: " + Megas(RutaPrefab) + " MB (tope 60).");
                if (faltan.Count > 0)
                {
                    Log("FALTAN assets: " + string.Join(", ", faltan.Distinct()) + ".");
                }
            }
            catch (Exception e)
            {
                Log("ERROR: " + e);
                Debug.LogError("[ZonaCerroT1441] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
            Escribir(RutaInforme, informe);
        }

        /// <summary>
        /// Las primeras rondas colgaron las rejas como hijos "Reja" de la planta baja de cada casa (fuera de la raíz T1441_Detalle, así que Aplicar no las borraba y quedaban
        /// fuera del LODGroup de su casa). Ahora las rejas van combinadas en los lotes: se borran las viejas (mallas T1441_Reja, o con la malla perdida) bajo C01/C02_Fachada.
        /// </summary>
        private static void LimpiarRejasViejas(GameObject zona)
        {
            int borradas = 0;
            foreach (Transform t in zona.GetComponentsInChildren<Transform>(true).ToList())
            {
                if (t == null || t.name != "Reja" || t.parent == null || !(t.parent.name.StartsWith("C01_Fachada") || t.parent.name.StartsWith("C02_Fachada")))
                {
                    continue;
                }
                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name != "T1441_Reja")
                {
                    continue;
                }
                Object.DestroyImmediate(t.gameObject);
                borradas++;
            }
            Log("Rejas de rondas anteriores (hijas de las casas) borradas: " + borradas + ".");
        }

        private static int rechazadosApoyo;

        /// <summary>La misma prueba que Validar: rayo desde el centro, 0.4 m sobre el punto más bajo, hacia abajo; sin hit no se rechaza.</summary>
        private static bool Apoyado(Ctx c, Transform g, Bounds b)
        {
            int n = c.Fisica.Raycast(new Vector3(b.center.x, b.min.y + 0.4f, b.center.z), Vector3.down, bufRayos, 3f, ~0, QueryTriggerInteraction.Ignore);
            float y = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                if (bufRayos[i].collider.transform.IsChildOf(g) || EsPiso(c, bufRayos[i].collider.transform) || bufRayos[i].normal.y < 0.9f)
                {
                    continue;
                }
                y = Mathf.Max(y, bufRayos[i].point.y);
            }
            return y == float.MinValue || Mathf.Abs(b.min.y - y) <= 0.05f;
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
                Debug.LogError("[ZonaCerroT1441] " + nombre + ": " + e);
            }
        }

        // ================================================================ Materiales y mallas generadas

        private static void CarpetaMat()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/Arte/Mundo", "Cerro");
            }
        }

        private static Material MatLit(string nombre, Color color, float suavidad, float metal = 0f)
        {
            CarpetaMat();
            string ruta = CarpetaMateriales + "/T1441_" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "T1441_" + nombre };
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", suavidad);
            m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material emisivo propio (RealtimeEmissive; con None URP quita _EMISSION al reimportar).</summary>
        private static Material MatEmi(string nombre, Color color, float intensidad)
        {
            Material m = MatLit(nombre, new Color(color.r * 0.2f, color.g * 0.2f, color.b * 0.2f, 1f), 0.3f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(color.r * intensidad, color.g * intensidad, color.b * intensidad, 1f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Materiales(Ctx c)
        {
            c.Marco = MatLit("MarcoNegro", new Color(0.06f, 0.06f, 0.07f), 0.35f, 0.4f);
            c.MarcoOxido = MatLit("MetalOxido", new Color(0.36f, 0.2f, 0.12f), 0.3f, 0.3f);
            c.Concreto = MatLit("Concreto", new Color(0.45f, 0.44f, 0.42f), 0.1f);
            c.Gris = MatLit("GrisClaro", new Color(0.62f, 0.64f, 0.66f), 0.5f, 0.5f);
            c.Negro = MatLit("Negro", new Color(0.03f, 0.03f, 0.035f), 0.4f);
            c.Madera = MatLit("Madera", new Color(0.3f, 0.2f, 0.12f), 0.15f);
            c.Neon = new[]
            {
                MatEmi("NeonMagenta", KitS2c.Magenta, 3.2f),
                MatEmi("NeonTurquesa", KitS2c.Turquesa, 3.0f),
                MatEmi("NeonVerde", new Color(0.35f, 1f, 0.3f), 2.8f),
            };
            c.Calido = MatEmi("LamparaCalida", new Color(1f, 0.7f, 0.35f), 3.4f);
            c.Bolsas = new[]
            {
                MatLit("BolsaNegra", new Color(0.04f, 0.04f, 0.05f), 0.6f),
                MatLit("BolsaAzul", new Color(0.12f, 0.2f, 0.45f), 0.6f),
                MatLit("BolsaBlanca", new Color(0.7f, 0.72f, 0.7f), 0.5f),
            };
        }

        private sealed class Malla
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<int>[] T;

            public Malla(int submallas)
            {
                T = new List<int>[submallas];
                for (int i = 0; i < submallas; i++)
                {
                    T[i] = new List<int>();
                }
            }

            /// <summary>Cuadrilátero a-b-c-d (en orden alrededor); la cara visible mira hacia <paramref name="normal"/>.</summary>
            public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                normal.Normalize();
                bool invertir = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f;
                int i0 = V.Count;
                V.Add(a); V.Add(b); V.Add(c); V.Add(d);
                for (int i = 0; i < 4; i++)
                {
                    N.Add(normal);
                }
                Uv.Add(new Vector2(0f, 0f)); Uv.Add(new Vector2(1f, 0f)); Uv.Add(new Vector2(1f, 1f)); Uv.Add(new Vector2(0f, 1f));
                if (invertir)
                {
                    T[sub].AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
                }
                else
                {
                    T[sub].AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
                }
            }

            private void Cara(int sub, Vector3 centro, Vector3 n, Vector3 u, Vector3 v, float hn, float hu, float hv)
            {
                Vector3 p = centro + n * hn;
                Quad(sub, p + u * hu + v * hv, p - u * hu + v * hv, p - u * hu - v * hv, p + u * hu - v * hv, n);
            }

            public void Caja(int sub, Vector3 centro, Vector3 tam)
            {
                Vector3 h = tam * 0.5f;
                Cara(sub, centro, Vector3.right, Vector3.up, Vector3.forward, h.x, h.y, h.z);
                Cara(sub, centro, Vector3.left, Vector3.up, Vector3.forward, h.x, h.y, h.z);
                Cara(sub, centro, Vector3.up, Vector3.right, Vector3.forward, h.y, h.x, h.z);
                Cara(sub, centro, Vector3.down, Vector3.right, Vector3.forward, h.y, h.x, h.z);
                Cara(sub, centro, Vector3.forward, Vector3.right, Vector3.up, h.z, h.x, h.y);
                Cara(sub, centro, Vector3.back, Vector3.right, Vector3.up, h.z, h.x, h.y);
            }

            public void Tubo(int sub, Vector3 a, Vector3 b, float radio, int lados = 5)
            {
                Vector3 eje = (b - a).normalized;
                Vector3 u = Vector3.Cross(eje, Vector3.up);
                if (u.sqrMagnitude < 0.01f)
                {
                    u = Vector3.right;
                }
                u.Normalize();
                Vector3 w = Vector3.Cross(eje, u).normalized;
                for (int k = 0; k < lados; k++)
                {
                    float a0 = k * Mathf.PI * 2f / lados, a1 = (k + 1) * Mathf.PI * 2f / lados;
                    Vector3 n0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0);
                    Vector3 n1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                    Quad(sub, a + n0 * radio, a + n1 * radio, b + n1 * radio, b + n0 * radio, n0 + n1);
                }
            }

            /// <summary>Superficie de revolución alrededor de Y con el perfil (radio, altura), girada y movida. Las normales miran hacia o lejos de <paramref name="interior"/> (local).</summary>
            public void Revolver(int sub, Vector2[] perfil, int lados, Quaternion giro, Vector3 pos, Vector3 interior, bool haciaInterior)
            {
                for (int i = 0; i + 1 < perfil.Length; i++)
                {
                    for (int k = 0; k < lados; k++)
                    {
                        float a0 = k * Mathf.PI * 2f / lados, a1 = (k + 1) * Mathf.PI * 2f / lados;
                        Vector3 pa = new Vector3(Mathf.Cos(a0) * perfil[i].x, perfil[i].y, Mathf.Sin(a0) * perfil[i].x);
                        Vector3 pb = new Vector3(Mathf.Cos(a1) * perfil[i].x, perfil[i].y, Mathf.Sin(a1) * perfil[i].x);
                        Vector3 pc = new Vector3(Mathf.Cos(a1) * perfil[i + 1].x, perfil[i + 1].y, Mathf.Sin(a1) * perfil[i + 1].x);
                        Vector3 pd = new Vector3(Mathf.Cos(a0) * perfil[i + 1].x, perfil[i + 1].y, Mathf.Sin(a0) * perfil[i + 1].x);
                        Vector3 n = Vector3.Cross(pc - pa, pd - pb);
                        if (n.sqrMagnitude < 1e-10f)
                        {
                            continue;
                        }
                        Vector3 centro = (pa + pb + pc + pd) * 0.25f;
                        float lado = Vector3.Dot(n, interior - centro);
                        if ((lado > 0f) != haciaInterior)
                        {
                            n = -n;
                        }
                        Quad(sub, giro * pa + pos, giro * pb + pos, giro * pc + pos, giro * pd + pos, giro * n);
                    }
                }
            }

            public Mesh Crear(string nombre)
            {
                Mesh m = new Mesh { name = nombre, indexFormat = IndexFormat.UInt32 };
                m.SetVertices(V);
                m.SetNormals(N);
                m.SetUVs(0, Uv);
                m.subMeshCount = T.Length;
                for (int i = 0; i < T.Length; i++)
                {
                    m.SetTriangles(T[i], i);
                }
                m.RecalculateBounds();
                return m;
            }
        }

        private static Mesh MallaReja()
        {
            // Unidad: x e y de -0.5 a 0.5; z de 0 a -0.07 (sale hacia -Z local, el frente de las fachadas del kit).
            Malla m = new Malla(1);
            const float g = 0.03f, p = 0.07f;
            m.Caja(0, new Vector3(0f, 0.5f - g * 0.5f, -p * 0.5f), new Vector3(1f, g, p));
            m.Caja(0, new Vector3(0f, -0.5f + g * 0.5f, -p * 0.5f), new Vector3(1f, g, p));
            m.Caja(0, new Vector3(-0.5f + g * 0.5f, 0f, -p * 0.5f), new Vector3(g, 1f, p));
            m.Caja(0, new Vector3(0.5f - g * 0.5f, 0f, -p * 0.5f), new Vector3(g, 1f, p));
            for (int i = 1; i < 6; i++)
            {
                m.Caja(0, new Vector3(-0.5f + i / 6f, 0f, -p * 0.5f), new Vector3(0.022f, 1f, 0.03f));
            }
            m.Caja(0, new Vector3(0f, 0.02f, -p * 0.5f), new Vector3(1f, 0.025f, 0.03f));
            return m.Crear("T1441_Reja");
        }

        /// <summary>Letrero de bandera (sale de la pared, sus dos caras se leen a lo largo de la calle). Local: +Z sale de la pared, X a lo largo, Y arriba; origen en la pared.</summary>
        private static Mesh MallaBandera(string nombre, float fondo, float alto)
        {
            Malla m = new Malla(2);
            const float grueso = 0.07f, salida = 0.14f;
            m.Caja(0, new Vector3(0f, 0f, salida + fondo * 0.5f), new Vector3(grueso, alto, fondo));
            m.Caja(0, new Vector3(0f, alto * 0.3f, salida * 0.5f), new Vector3(0.035f, 0.035f, salida));
            m.Caja(0, new Vector3(0f, -alto * 0.3f, salida * 0.5f), new Vector3(0.035f, 0.035f, salida));
            float zc = salida + fondo * 0.5f, hz = fondo * 0.5f - 0.04f, hy = alto * 0.5f - 0.04f;
            foreach (float s in new[] { 1f, -1f })
            {
                float x = s * (grueso * 0.5f + 0.004f);
                m.Quad(1, new Vector3(x, hy, zc + hz), new Vector3(x, hy, zc - hz), new Vector3(x, -hy, zc - hz), new Vector3(x, -hy, zc + hz), new Vector3(s, 0f, 0f));
            }
            return m.Crear(nombre);
        }

        /// <summary>Letrero plano pegado a la pared. Local: +Z sale de la pared.</summary>
        private static Mesh MallaPlano(string nombre, float ancho, float alto)
        {
            Malla m = new Malla(2);
            m.Caja(0, new Vector3(0f, 0f, 0.04f), new Vector3(ancho, alto, 0.08f));
            float z = 0.084f, hx = ancho * 0.5f - 0.05f, hy = alto * 0.5f - 0.05f;
            m.Quad(1, new Vector3(-hx, hy, z), new Vector3(hx, hy, z), new Vector3(hx, -hy, z), new Vector3(-hx, -hy, z), Vector3.forward);
            return m.Crear(nombre);
        }

        /// <summary>Lámpara de pared: jaula oscura y vidrio cálido emisivo. Local: +Z sale de la pared.</summary>
        private static Mesh MallaLampara()
        {
            Malla m = new Malla(2);
            m.Caja(0, new Vector3(0f, 0f, 0.05f), new Vector3(0.2f, 0.34f, 0.1f));
            m.Caja(0, new Vector3(0f, 0f, 0.14f), new Vector3(0.2f, 0.04f, 0.08f));
            m.Caja(0, new Vector3(0f, 0.19f, 0.15f), new Vector3(0.24f, 0.04f, 0.14f));
            m.Caja(1, new Vector3(0f, -0.02f, 0.15f), new Vector3(0.14f, 0.22f, 0.07f));
            return m.Crear("T1441_Lampara");
        }

        private static Mesh MallaParabolica(int variante)
        {
            Malla m = new Malla(2);
            m.Tubo(0, Vector3.zero, new Vector3(0f, 0.9f + 0.1f * variante, 0f), 0.035f, 6);
            Vector2[] perfil = new Vector2[7];
            for (int i = 0; i < perfil.Length; i++)
            {
                float r = 0.42f * i / (perfil.Length - 1);
                perfil[i] = new Vector2(r, 0.16f * (r / 0.42f) * (r / 0.42f));
            }
            Quaternion giro = Quaternion.Euler(-55f - 8f * variante, 0f, 0f);
            Vector3 pos = new Vector3(0f, 0.95f + 0.1f * variante, 0f);
            m.Revolver(1, perfil, 10, giro, pos, new Vector3(0f, 5f, 0f), true);
            Vector2[] atras = perfil.Select(p => new Vector2(p.x, p.y - 0.015f)).ToArray();
            m.Revolver(1, atras, 10, giro, pos, new Vector3(0f, 5f, 0f), false);
            Vector3 foco = pos + giro * new Vector3(0f, 0.3f, 0f);
            m.Tubo(0, pos, foco, 0.012f, 4);
            m.Caja(0, foco, new Vector3(0.06f, 0.06f, 0.06f));
            return m.Crear("T1441_Parabolica" + variante);
        }

        /// <summary>Castillo de concreto con varillas que salen (sub 0 concreto, sub 1 varilla).</summary>
        private static Mesh MallaVarillas(int variante)
        {
            System.Random az = new System.Random(77 + variante);
            Malla m = new Malla(2);
            m.Caja(0, new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f));
            int n = 5 + variante % 3;
            for (int i = 0; i < n; i++)
            {
                float ang = i * Mathf.PI * 2f / n;
                Vector3 b = new Vector3(Mathf.Cos(ang) * 0.09f, 0.5f, Mathf.Sin(ang) * 0.09f);
                float alto = 0.8f + (float)az.NextDouble() * 0.9f;
                Vector3 a = b + new Vector3((float)(az.NextDouble() - 0.5) * 0.12f, alto, (float)(az.NextDouble() - 0.5) * 0.12f);
                m.Tubo(1, b, a, 0.011f, 4);
            }
            return m.Crear("T1441_Varillas" + variante);
        }

        private static Mesh MallaBolsa(int variante)
        {
            Malla m = new Malla(1);
            Vector2[] perfil = { new Vector2(0f, 0f), new Vector2(0.17f, 0.01f), new Vector2(0.25f, 0.12f), new Vector2(0.22f, 0.25f), new Vector2(0.12f, 0.34f), new Vector2(0.03f, 0.42f), new Vector2(0f, 0.4f) };
            m.Revolver(0, perfil, 7, Quaternion.Euler(0f, 40f * variante, 0f), Vector3.zero, new Vector3(0f, 0.2f, 0f), false);
            return m.Crear("T1441_Bolsa" + variante);
        }

        private static GameObject Generado(Transform padre, string nombre, Mesh malla, Material[] materiales, Vector3 pos, Quaternion giro, Vector3 escala, bool collider, bool sombra = false)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(padre, false);
            g.transform.SetPositionAndRotation(pos, giro);
            g.transform.localScale = escala;
            g.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterials = materiales;
            mr.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (collider)
            {
                g.AddComponent<MeshCollider>().sharedMesh = malla;
            }
            return g;
        }

        // ================================================================ Lotes: mallas combinadas por material y por cuadro de 40 m (sin collider)

        private sealed class LoteDatos
        {
            public Material M;
            public int Cx, Cz;
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<int> T = new List<int>();
        }

        private const float LadoLod = 40f;

        private static readonly Dictionary<string, LoteDatos> lotes = new Dictionary<string, LoteDatos>();
        private static int pegados, conservados;

        private static LoteDatos Lote(Material m, Vector3 donde)
        {
            // Una sola malla por material para todo el Cerro (menos batches); la ubicación solo se usa para nombrar.
            int cx = 0, cz = 0;
            string clave = m.GetInstanceID() + "|" + cx + "|" + cz;
            if (!lotes.TryGetValue(clave, out LoteDatos l))
            {
                l = new LoteDatos { M = m, Cx = cx, Cz = cz };
                lotes[clave] = l;
            }
            return l;
        }

        /// <summary>Pega una malla generada (leíble) en los lotes con la matriz del mundo; el material de cada submalla viene en mats.</summary>
        private static void Pegar(Mesh malla, Material[] mats, Matrix4x4 mtx)
        {
            Vector3[] v = malla.vertices;
            Vector3[] n = malla.normals;
            Vector2[] uv = malla.uv;
            bool espejo = mtx.determinant < 0f;
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                Material m = mats[Mathf.Min(s, mats.Length - 1)];
                LoteDatos l = Lote(m, mtx.GetColumn(3));
                int[] tris = malla.GetTriangles(s);
                Dictionary<int, int> mapa = new Dictionary<int, int>();
                foreach (int idx in tris)
                {
                    if (!mapa.ContainsKey(idx))
                    {
                        mapa[idx] = l.V.Count;
                        l.V.Add(mtx.MultiplyPoint3x4(v[idx]));
                        l.N.Add(n.Length > idx ? mtx.MultiplyVector(n[idx]).normalized : Vector3.up);
                        l.Uv.Add(uv.Length > idx ? uv[idx] : Vector2.zero);
                    }
                }
                for (int i = 0; i + 2 < tris.Length; i += 3)
                {
                    if (espejo)
                    {
                        l.T.Add(mapa[tris[i]]); l.T.Add(mapa[tris[i + 2]]); l.T.Add(mapa[tris[i + 1]]);
                    }
                    else
                    {
                        l.T.Add(mapa[tris[i]]); l.T.Add(mapa[tris[i + 1]]); l.T.Add(mapa[tris[i + 2]]);
                    }
                }
            }
            pegados++;
        }

        /// <summary>Pega los renderers de una instancia ya colocada (sin collider que importe) en los lotes y la borra. Si alguna malla no es leíble la deja como está y devuelve false.</summary>
        private static bool PegarInstancia(GameObject g)
        {
            List<(MeshFilter mf, MeshRenderer mr)> piezas = new List<(MeshFilter, MeshRenderer)>();
            foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(false))
            {
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled || mf.sharedMesh == null)
                {
                    continue;
                }
                if (!mf.sharedMesh.isReadable || mr.sharedMaterials.Length == 0)
                {
                    conservados++;
                    return false;
                }
                piezas.Add((mf, mr));
            }
            if (piezas.Count == 0)
            {
                return false;
            }
            foreach ((MeshFilter mf, MeshRenderer mr) in piezas)
            {
                Pegar(mf.sharedMesh, mr.sharedMaterials, mf.transform.localToWorldMatrix);
            }
            Object.DestroyImmediate(g);
            return true;
        }

        private static void Volcar(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Lotes");
            int n = 0;
            foreach (LoteDatos l in lotes.Values)
            {
                if (l.V.Count == 0)
                {
                    continue;
                }
                Mesh m = new Mesh { name = "T1441_Lote_" + l.M.name + "_" + l.Cx + "_" + l.Cz, indexFormat = IndexFormat.UInt32 };
                m.SetVertices(l.V);
                m.SetNormals(l.N);
                m.SetUVs(0, l.Uv);
                m.SetTriangles(l.T, 0);
                m.RecalculateBounds();
                c.Obra.Mallas.Add(m);
                GameObject g = new GameObject("Lote_" + l.M.name + "_" + l.Cx + "_" + l.Cz);
                g.transform.SetParent(padre, false);
                g.AddComponent<MeshFilter>().sharedMesh = m;
                MeshRenderer mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = l.M;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            int sueltos = 0, skin = 0;
            foreach (Renderer r in c.Nuevo.GetComponentsInChildren<Renderer>(true))
            {
                if (r.transform.IsChildOf(padre) || r is ParticleSystemRenderer)
                {
                    continue;
                }
                if (r is SkinnedMeshRenderer)
                {
                    skin++;
                }
                else
                {
                    sueltos++;
                }
            }
            Log("Lotes combinados: " + n + " mallas (una por material, todo el Cerro) con " + pegados + " piezas pegadas; piezas de prefab que no se pudieron combinar (malla no leíble): " + conservados + ".");
            Log("Renderers de " + Raiz + ": antes de combinar " + (pegados + sueltos + skin) + " (" + pegados + " piezas + " + sueltos + " sueltos + " + skin + " con huesos); después " + (n + sueltos + skin) + " (" + n + " lotes + " + sueltos + " sueltos + " + skin + " con huesos).");
        }

        /// <summary>Las piezas de prefab que no se pudieron combinar (malla no leíble) se agrupan por cuadro de 40 m bajo un LODGroup de un solo nivel que se apaga bajo el 2 % de la pantalla.</summary>
        private static bool EnCuadroDe(Vector3 desde, Vector3 hacia, float campo, Vector3 punto, float min, float max)
        {
            Vector3 adelante = (hacia - desde).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
            Vector3 arriba = Vector3.Cross(adelante, derecha);
            Vector3 d = punto - desde;
            float prof = Vector3.Dot(d, adelante);
            if (prof < min || prof > max)
            {
                return false;
            }
            float tanV = Mathf.Tan(campo * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * (1376f / 768f);
            return Mathf.Abs(Vector3.Dot(d, derecha) / prof) < tanH && Mathf.Abs(Vector3.Dot(d, arriba) / prof) < tanV;
        }

        private static int ContarRenderers(Transform raiz)
        {
            return raiz.GetComponentsInChildren<Renderer>(true).Count(r => !(r is ParticleSystemRenderer));
        }

        /// <summary>
        /// Rendimiento: las piezas de prefab sueltas (mallas no leíbles, no se pueden combinar) que son puro adorno (antenas, chatarra, cajas, tambos, lavaderos, tendederos, aires, medidores,
        /// toldos, macetas, basura) y que no están en el cuadro de K13, ni a 60 m o menos de su cámara, ni en el cuadro de A13, se quitan. Tinacos y gente no se tocan.
        /// </summary>
        private static void Podar(Ctx c)
        {
            int antes = ContarRenderers(c.Nuevo);
            int quitados = 0, revisados = 0;
            Dictionary<string, int> porTipo = new Dictionary<string, int>();
            foreach (string ruta in new[] { "Techos", "Fachadas", "Calle/Basura" })
            {
                Transform padre = c.Nuevo.Find(ruta);
                if (padre == null)
                {
                    continue;
                }
                foreach (Transform t in padre.Cast<Transform>().ToList())
                {
                    if (t.name.StartsWith("NPC_") || t.name.StartsWith("Tinaco_") || t.GetComponentInChildren<Animator>() != null || t.GetComponentInChildren<Light>() != null || t.GetComponentInChildren<Renderer>() == null)
                    {
                        continue;
                    }
                    revisados++;
                    Vector3 p = Cubierta(t).center;
                    bool enK13 = EnCuadroDe(K13Desde, K13Hacia, CampoK13, p, 1f, 400f);
                    bool cerca = new Vector2(p.x - K13Desde.x, p.z - K13Desde.z).magnitude <= 60f;
                    bool enA13 = EnCuadroDe(ZonaCerroT1440.A13Desde, ZonaCerroT1440.A13Hacia, CampoA13, p, 1f, 600f);
                    if (enK13 || cerca || enA13)
                    {
                        continue;
                    }
                    string tipo = t.name.Split('_')[0];
                    porTipo[tipo] = (porTipo.TryGetValue(tipo, out int v) ? v : 0) + 1;
                    Object.DestroyImmediate(t.gameObject);
                    quitados++;
                }
            }
            Physics.SyncTransforms();
            Log("Poda de adornos sueltos fuera de K13 (cuadro y 60 m) y de A13: " + quitados + " quitados de " + revisados + " (" + string.Join(", ", porTipo.Select(q => q.Key + " " + q.Value)) + "); quedan " + (revisados - quitados) + " sueltos.");
            Log("Renderers de " + Raiz + " antes de la poda: " + antes + ", después: " + ContarRenderers(c.Nuevo) + ".");
        }

        private static void AgruparLod(Ctx c)
        {
            int grupos = 0, piezas = 0;
            foreach (string ruta in new[] { "Techos", "Fachadas", "Calle/Basura" })
            {
                Transform padre = c.Nuevo.Find(ruta);
                if (padre == null)
                {
                    continue;
                }
                Dictionary<string, List<Transform>> celdas = new Dictionary<string, List<Transform>>();
                foreach (Transform t in padre)
                {
                    if (t.name.StartsWith("NPC_") || t.GetComponentInChildren<Animator>() != null || t.GetComponentInChildren<Light>() != null || t.GetComponentInChildren<Renderer>() == null)
                    {
                        continue;
                    }
                    Bounds b = Cubierta(t);
                    string clave = Mathf.FloorToInt(b.center.x / LadoLod) + "_" + Mathf.FloorToInt(b.center.z / LadoLod);
                    if (!celdas.TryGetValue(clave, out List<Transform> lista))
                    {
                        lista = new List<Transform>();
                        celdas[clave] = lista;
                    }
                    lista.Add(t);
                }
                foreach (KeyValuePair<string, List<Transform>> par in celdas)
                {
                    GameObject g = new GameObject("Lod_" + par.Key);
                    g.transform.SetParent(padre, false);
                    foreach (Transform t in par.Value)
                    {
                        t.SetParent(g.transform, true);
                    }
                    Renderer[] rs = g.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
                    LODGroup lg = g.AddComponent<LODGroup>();
                    lg.SetLODs(new[] { new LOD(0.02f, rs) });
                    lg.RecalculateBounds();
                    grupos++;
                    piezas += par.Value.Count;
                }
            }
            Log("LODGroup (culling al 2 %) por cuadro de 40 m: " + grupos + " grupos con " + piezas + " objetos.");
        }

        // ================================================================ Ayudas de piso, bounds y piezas

        private static bool EsPiso(Ctx c, Transform t)
        {
            return (c.Terreno != null && t.IsChildOf(c.Terreno)) || (c.Calles != null && t.IsChildOf(c.Calles));
        }

        /// <summary>Piso caminable en (x, z): el punto más alto con normal hacia arriba entre yRef + 2.5 m y 4 m más abajo (no cuenta lo nuevo).</summary>
        private static bool Suelo(Ctx c, float x, float z, float yRef, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, yRef + 2.5f, z), Vector3.down, bufRayos, 6.5f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = bufRayos[i].collider.transform;
                if (t.IsChildOf(c.Nuevo) || bufRayos[i].normal.y < 0.9f)
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

        private static Ctx actual;
        private static readonly Dictionary<int, float> calleMedida = new Dictionary<int, float>();

        /// <summary>Altura de la banqueta en z: la calle medida con un rayo contra Terreno/Calles (T-1440 puede haber movido la calle) + 0.2 m; si no se mide, la del trazado.</summary>
        private static float YCalle(float z)
        {
            float analitica = CerroT1355Trazado.AlturaPierna2(z) + AltoBanqueta;
            if (actual == null)
            {
                return analitica;
            }
            int clave = Mathf.RoundToInt(z);
            if (calleMedida.TryGetValue(clave, out float ya))
            {
                return ya;
            }
            float y = float.MinValue;
            int n = actual.Fisica.Raycast(new Vector3(XCentroCalle, analitica + 25f, clave), Vector3.down, bufRayos, 60f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (EsPiso(actual, bufRayos[i].collider.transform) && bufRayos[i].normal.y > 0.9f && bufRayos[i].point.y > y)
                {
                    y = bufRayos[i].point.y;
                }
            }
            float r = y > float.MinValue && Mathf.Abs(y + AltoBanqueta - analitica) < 6f ? y + AltoBanqueta : analitica;
            calleMedida[clave] = r;
            return r;
        }

        private static bool Libre(Ctx c, Vector3 centro, Vector3 mitad)
        {
            int n = c.Fisica.OverlapBox(centro, mitad, bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!EsPiso(c, bufCol[i].transform))
                {
                    return false;
                }
            }
            return true;
        }

        private static Bounds Cubierta(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                // Sin malla propia (p. ej. el tinaco: la malla va en el lote): vale la caja de su collider.
                Collider col = t.GetComponentInChildren<Collider>();
                return col != null ? col.bounds : new Bounds(t.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
            {
                b.Encapsulate(rs[i].bounds);
            }
            return b;
        }

        private static GameObject Cargar(string codigo)
        {
            if (cache.TryGetValue(codigo, out GameObject ya))
            {
                return ya;
            }
            GameObject p = null;
            string guid = AssetDatabase.FindAssets(codigo + " t:Prefab").FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == codigo);
            if (guid != null)
            {
                p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            }
            if (p == null)
            {
                faltan.Add(codigo);
            }
            cache[codigo] = p;
            return p;
        }

        private static GameObject CargarRuta(string ruta)
        {
            if (cache.TryGetValue(ruta, out GameObject ya))
            {
                return ya;
            }
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (p == null)
            {
                faltan.Add(ruta);
            }
            cache[ruta] = p;
            return p;
        }

        /// <summary>Caja del prefab sin girar ni escalar, en el origen.</summary>
        private static Bounds Medida(GameObject prefab)
        {
            string clave = AssetDatabase.GetAssetPath(prefab) + "|" + prefab.name;
            if (medidas.TryGetValue(clave, out Bounds ya))
            {
                return ya;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds b = Cubierta(g.transform);
            Object.DestroyImmediate(g);
            medidas[clave] = b;
            return b;
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static GameObject Instanciar(GameObject prefab, Transform padre, string nombre)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = nombre;
            return g;
        }

        private static float Distancia2D(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 1e-4f)
            {
                return (p - a).magnitude;
            }
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            return (p - (a + ab * t)).magnitude;
        }

        private static bool SolapaXZ(Bounds a, Bounds b, float margen)
        {
            return a.min.x < b.max.x + margen && a.max.x > b.min.x - margen && a.min.z < b.max.z + margen && a.max.z > b.min.z - margen;
        }

        // ================================================================ Escaneo (enlaces, postes, detalles viejos, azoteas)

        private static bool EsDetalleTecho(Transform t)
        {
            string n = t.name;
            return n.StartsWith("C09_Tinaco") || n.StartsWith("Tinaco_") || n.StartsWith("TK01") || n.StartsWith("CY04") || n.StartsWith("Antena_") || n.StartsWith("CC04") || n.StartsWith("CC05")
                || n.StartsWith("Extra_Det_") || n.StartsWith("Aire_") || n.StartsWith("C10_Aire") || (n.StartsWith("Extra_Mov|") && (n.Contains("CC04") || n.Contains("CC05") || n.Contains("CY04")));
        }

        private static bool EsEnlace(Transform t)
        {
            return t.name.StartsWith("Enlace_") || t.name.StartsWith("Acceso_");
        }

        private static bool EsPoste(Transform t)
        {
            return t.name.StartsWith("Poste_") || t.name.StartsWith("C07_PosteLuz");
        }

        private static bool Ancestro(Transform t, Transform tope, Func<Transform, bool> prueba)
        {
            for (Transform p = t.parent; p != null && p != tope; p = p.parent)
            {
                if (prueba(p))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>true si el punto (con un radio) cae en un cuadro libre de escaleras o accesos (también en altura: ±4 m del punto de la escalera).</summary>
        private static bool EnLibre(Ctx c, Vector3 p, float radio)
        {
            foreach (Bounds b in c.Libres)
            {
                if (Mathf.Abs(p.x - b.center.x) < b.extents.x + radio && Mathf.Abs(p.z - b.center.z) < b.extents.z + radio && Mathf.Abs(p.y - b.center.y) < b.extents.y + radio)
                {
                    return true;
                }
            }
            return false;
        }

        private static void AgregarLibre(Ctx c, Vector3 centro, float mitad)
        {
            c.Libres.Add(new Bounds(centro, new Vector3(mitad * 2f, 10f, mitad * 2f)));
        }

        /// <summary>
        /// Cuadros libres (5 x 5 m: 4 x 4 más margen) al pie y en la salida de cada escalera CE01 (se agrupan por z como lo hace RecorridoCerroT1361Pruebas), de cada escalera de ladera de T-1440 y
        /// junto a cada PuntoTrepar (accesos y enlaces de azotea).
        /// </summary>
        private static void ZonasLibres(Ctx c)
        {
            int escaleras = 0, trepar = 0;
            List<Transform> grupos = new List<Transform>();
            Transform ce = c.Zona.transform.Find("Escaleras");
            Transform ladera = c.Zona.transform.Find(ZonaCerroT1440.Raiz + "/EscalerasLadera");
            foreach (Transform raiz in new[] { ce, ladera })
            {
                if (raiz == null)
                {
                    continue;
                }
                SortedDictionary<int, List<Renderer>> porZ = new SortedDictionary<int, List<Renderer>>();
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    if (!r.gameObject.activeInHierarchy || (raiz == ce && (r.transform.parent == null || !r.transform.parent.name.StartsWith("CE01_EscaleraCerro"))))
                    {
                        continue;
                    }
                    int clave = Mathf.RoundToInt(r.bounds.center.z / 2f);
                    if (!porZ.ContainsKey(clave))
                    {
                        porZ[clave] = new List<Renderer>();
                    }
                    porZ[clave].Add(r);
                }
                foreach (List<Renderer> lista in porZ.Values)
                {
                    if (lista.Count < 5)
                    {
                        continue;
                    }
                    Renderer bajo = lista.OrderBy(r => r.bounds.center.y).First();
                    Renderer alto = lista.OrderByDescending(r => r.bounds.center.y).First();
                    Vector3 pb = new Vector3(bajo.bounds.center.x, bajo.bounds.max.y, bajo.bounds.center.z);
                    Vector3 pa = new Vector3(alto.bounds.center.x, alto.bounds.max.y, alto.bounds.center.z);
                    Vector3 d = pa - pb;
                    d.y = 0f;
                    d = d.sqrMagnitude < 0.01f ? Vector3.right : d.normalized;
                    AgregarLibre(c, pb - d * 1.5f, 2.5f);
                    AgregarLibre(c, pa + d * 1.5f, 2.5f);
                    escaleras++;
                }
            }
            foreach (PuntoTrepar p in c.Zona.GetComponentsInChildren<PuntoTrepar>(false))
            {
                if (p.transform.IsChildOf(c.Nuevo))
                {
                    continue;
                }
                AgregarLibre(c, p.transform.position, 3f);
                trepar++;
            }
            Log("Cuadros libres de 5 x 5 m: " + c.Libres.Count + " (escaleras " + escaleras + ", PuntoTrepar " + trepar + ").");
        }

        private static void Escanear(Ctx c)
        {
            ZonasLibres(c);
            List<Vector3> detalles = new List<Vector3>();
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (t == c.Zona.transform || t.IsChildOf(c.Nuevo))
                {
                    continue;
                }
                if (EsEnlace(t) && !Ancestro(t, c.Zona.transform, EsEnlace))
                {
                    Bounds b = Cubierta(t);
                    if (b.size.sqrMagnitude > 0.01f)
                    {
                        c.Enlaces.Add(b);
                    }
                }
                else if (EsPoste(t) && !Ancestro(t, c.Zona.transform, EsPoste))
                {
                    Bounds b = Cubierta(t);
                    c.Postes.Add(new Vector3(t.position.x, b.max.y > t.position.y + 2f ? b.max.y - 0.2f : t.position.y + 6.5f, t.position.z));
                }
                else if (EsDetalleTecho(t) && !Ancestro(t, c.Zona.transform, EsDetalleTecho))
                {
                    detalles.Add(t.position);
                }
            }
            Log("Enlaces y accesos de azotea: " + c.Enlaces.Count + ". Postes: " + c.Postes.Count + ". Detalles de azotea ya puestos: " + detalles.Count + ".");
            foreach (VariarCerroT1364.CasaT h in c.Casas)
            {
                float nivel;
                if (!NivelTecho(c, h, out nivel))
                {
                    continue;
                }
                Techo t = new Techo { H = h, Nivel = nivel };
                Bounds huella = new Bounds(new Vector3((h.X0 + h.X1) * 0.5f, nivel, (h.Z0 + h.Z1) * 0.5f), new Vector3(h.X1 - h.X0, 1f, h.Z1 - h.Z0));
                foreach (Bounds e in c.Enlaces)
                {
                    if (SolapaXZ(huella, e, 1.0f))
                    {
                        t.Enlaces.Add(e);
                    }
                }
                List<Vector2> puntos = t.Enlaces.Take(5).Select(e => new Vector2(Mathf.Clamp(e.center.x, h.X0, h.X1), Mathf.Clamp(e.center.z, h.Z0, h.Z1))).ToList();
                for (int i = 0; i < puntos.Count; i++)
                {
                    for (int j = i + 1; j < puntos.Count; j++)
                    {
                        t.Pasillos.Add((puntos[i], puntos[j]));
                    }
                }
                foreach (Vector3 d in detalles)
                {
                    if (d.x > h.X0 - 0.3f && d.x < h.X1 + 0.3f && d.z > h.Z0 - 0.3f && d.z < h.Z1 + 0.3f && d.y > nivel - 1f && d.y < nivel + 5f)
                    {
                        t.Existentes++;
                    }
                }
                c.Techos.Add(t);
            }
            // Corredores de salto de las cadenas de parkour: para cada par de casas del kit a menos de 4 m (hueco) y con menos de 3 m de desnivel, del punto 1 m dentro del borde lejano de una
            // al centro de la otra (como SaltarEntre), 1.5 m de ancho; se guardan en las dos azoteas.
            List<Techo> kit = c.Techos.Where(q => q.H.Kit).ToList();
            int corredores = 0;
            for (int i = 0; i < kit.Count; i++)
            {
                VariarCerroT1364.CasaT h1 = kit[i].H;
                Vector2 c1 = new Vector2((h1.X0 + h1.X1) * 0.5f, (h1.Z0 + h1.Z1) * 0.5f);
                float ex1 = (h1.X1 - h1.X0) * 0.5f, ez1 = (h1.Z1 - h1.Z0) * 0.5f;
                for (int j = 0; j < kit.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    VariarCerroT1364.CasaT h2 = kit[j].H;
                    Vector2 c2 = new Vector2((h2.X0 + h2.X1) * 0.5f, (h2.Z0 + h2.Z1) * 0.5f);
                    if ((c2 - c1).sqrMagnitude > 900f)
                    {
                        continue;
                    }
                    float ex2 = (h2.X1 - h2.X0) * 0.5f, ez2 = (h2.Z1 - h2.Z0) * 0.5f;
                    float dx = Mathf.Max(0f, Mathf.Abs(c1.x - c2.x) - (ex1 + ex2));
                    float dz = Mathf.Max(0f, Mathf.Abs(c1.y - c2.y) - (ez1 + ez2));
                    if (new Vector2(dx, dz).magnitude >= 4f || Mathf.Abs(h1.Top - h2.Top) > 3f)
                    {
                        continue;
                    }
                    Vector2 dir = (c2 - c1).normalized;
                    float mitad = Mathf.Abs(dir.x) * ex1 + Mathf.Abs(dir.y) * ez1;
                    Vector2 ini = c1 - dir * Mathf.Max(0f, mitad - 1f);
                    kit[i].Corredores.Add((ini, c2));
                    kit[j].Corredores.Add((ini, c2));
                    corredores++;
                }
            }
            Log("Corredores de salto entre azoteas (1.5 m de ancho): " + corredores + ".");
            Log("Azoteas planas medidas: " + c.Techos.Count + " de " + c.Casas.Count + " casas.");
        }

        /// <summary>Nivel de la azotea: el valor más común (3 de 9 rayos o más) entre los puntos planos que ve un rayo desde arriba.</summary>
        private static bool NivelTecho(Ctx c, VariarCerroT1364.CasaT h, out float nivel)
        {
            nivel = h.Top;
            if (h.X1 - h.X0 < 2.5f || h.Z1 - h.Z0 < 2.5f)
            {
                return false;
            }
            List<float> ys = new List<float>();
            float[] f = { 0.2f, 0.5f, 0.8f };
            foreach (float fx in f)
            {
                foreach (float fz in f)
                {
                    int n = c.Fisica.Raycast(new Vector3(Mathf.Lerp(h.X0, h.X1, fx), h.Top + 8f, Mathf.Lerp(h.Z0, h.Z1, fz)), Vector3.down, bufRayos, 16f, ~0, QueryTriggerInteraction.Ignore);
                    float mejor = float.MinValue;
                    for (int i = 0; i < n; i++)
                    {
                        Transform t = bufRayos[i].collider.transform;
                        if (t.IsChildOf(c.Nuevo) || EsPiso(c, t) || bufRayos[i].normal.y < 0.95f)
                        {
                            continue;
                        }
                        if (bufRayos[i].point.y > mejor)
                        {
                            mejor = bufRayos[i].point.y;
                        }
                    }
                    if (mejor > h.Top - 4f && mejor < h.Top + 7f)
                    {
                        ys.Add(mejor);
                    }
                }
            }
            if (ys.Count < 3)
            {
                return false;
            }
            float modo = 0f;
            int cuenta = 0;
            foreach (float y in ys)
            {
                int k = ys.Count(val => Mathf.Abs(val - y) < 0.15f);
                if (k > cuenta || (k == cuenta && y < modo))
                {
                    cuenta = k;
                    modo = y;
                }
            }
            if (cuenta < 3)
            {
                return false;
            }
            nivel = ys.Where(val => Mathf.Abs(val - modo) < 0.15f).Average();
            return true;
        }

        private static bool TechoEn(Ctx c, float x, float z, float nivel)
        {
            RaycastHit h;
            return c.Fisica.Raycast(new Vector3(x, nivel + 1.5f, z), Vector3.down, out h, 3f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.9f
                && Mathf.Abs(h.point.y - nivel) < 0.25f && !EsPiso(c, h.collider.transform) && !h.collider.transform.IsChildOf(c.Nuevo);
        }

        /// <summary>Superficie medida de la azotea en (x, z): el punto del rayo (normal hacia arriba, a menos de 25 cm del nivel de la azotea).</summary>
        private static bool TechoY(Ctx c, float x, float z, float nivel, out float y)
        {
            y = 0f;
            RaycastHit h;
            if (c.Fisica.Raycast(new Vector3(x, nivel + 1.5f, z), Vector3.down, out h, 3f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.9f
                && Mathf.Abs(h.point.y - nivel) < 0.25f && !EsPiso(c, h.collider.transform) && !h.collider.transform.IsChildOf(c.Nuevo))
            {
                y = h.point.y;
                return true;
            }
            return false;
        }

        /// <summary>Un centro libre de azotea para algo de medio tamaño (hx, hz) y alto: plano, sin colliders encima, sin encimarse ni cerrar los pasillos de 1.2 m.</summary>
        private static bool BuscarHueco(Ctx c, Techo t, float hx, float hz, float alto, List<Vector2> candidatos, bool conPlano, out Vector2 pos)
        {
            pos = default;
            VariarCerroT1364.CasaT h = t.H;
            float margen = 0.4f;
            foreach (Vector2 q in candidatos)
            {
                float x = q.x, z = q.y;
                if (x - hx < h.X0 + margen || x + hx > h.X1 - margen || z - hz < h.Z0 + margen || z + hz > h.Z1 - margen)
                {
                    continue;
                }
                if (EnPasillo(t, x, z, Mathf.Max(hx, hz) * 1.15f + 0.1f) || EnLibre(c, new Vector3(x, t.Nivel, z), Mathf.Max(hx, hz)))
                {
                    continue;
                }
                bool choca = false;
                Bounds mio = new Bounds(new Vector3(x, t.Nivel, z), new Vector3(hx * 2f, 1f, hz * 2f));
                foreach (Bounds o in c.Ocupado)
                {
                    if (SolapaXZ(mio, o, 0.35f) && o.max.y > t.Nivel - 0.5f && o.min.y < t.Nivel + 5f)
                    {
                        choca = true;
                        break;
                    }
                }
                if (choca)
                {
                    continue;
                }
                // Altura exacta: el rayo del centro (la misma que mide Validar); las 4 esquinas deben estar a menos de 4 cm.
                float y0;
                if (!TechoY(c, x, z, t.Nivel, out y0))
                {
                    continue;
                }
                bool plano = true;
                foreach (Vector2 d in new[] { new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f) })
                {
                    float ye;
                    plano &= TechoY(c, x + d.x * hx * 0.95f, z + d.y * hz * 0.95f, t.Nivel, out ye) && Mathf.Abs(ye - y0) < 0.04f;
                }
                if (!plano)
                {
                    continue;
                }
                t.Y = y0;
                float a = Mathf.Max(0.2f, alto - 0.3f);
                if (!Libre(c, new Vector3(x, t.Nivel + 0.2f + a * 0.5f, z), new Vector3(Mathf.Max(0.05f, hx - 0.05f), a * 0.5f, Mathf.Max(0.05f, hz - 0.05f))))
                {
                    continue;
                }
                pos = q;
                return true;
            }
            return false;
        }

        /// <summary>true si algo con radio <paramref name="radio"/> en (x, z) cierra un pasillo entre enlaces (1.2 m de ancho) o está a menos de 1.2 m de un enlace.</summary>
        private static bool EnPasillo(Techo t, float x, float z, float radio)
        {
            Vector2 p = new Vector2(x, z);
            foreach (Bounds e in t.Enlaces)
            {
                if (x > e.min.x - 1.2f - radio && x < e.max.x + 1.2f + radio && z > e.min.z - 1.2f - radio && z < e.max.z + 1.2f + radio)
                {
                    return true;
                }
            }
            foreach ((Vector2 a, Vector2 b) in t.Pasillos)
            {
                if (Distancia2D(p, a, b) < AnchoPasillo * 0.5f + radio)
                {
                    return true;
                }
            }
            foreach ((Vector2 a, Vector2 b) in t.Corredores)
            {
                if (Distancia2D(p, a, b) < 0.75f + radio)
                {
                    return true;
                }
            }
            return false;
        }

        private static List<Vector2> Candidatos(Ctx c, VariarCerroT1364.CasaT h, int n)
        {
            List<Vector2> r = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                r.Add(new Vector2(c.Obra.Rango(h.X0, h.X1), c.Obra.Rango(h.Z0, h.Z1)));
            }
            return r;
        }

        // ================================================================ Azoteas

        /// <summary>Base de ladrillo (sub 0) y tanque con dos aros y tapa (sub 1). Origen en el piso, el tanque sube hasta ~2.1 m. Lleva MeshCollider (es un obstáculo real y su collider es su malla).</summary>
        private static Mesh MallaTinaco()
        {
            Malla m = new Malla(2);
            m.Caja(0, new Vector3(0f, 0.35f, 0f), new Vector3(1.5f, 0.7f, 1.5f));
            Vector2[] perfil =
            {
                new Vector2(0f, 0.7f), new Vector2(0.78f, 0.7f), new Vector2(0.8f, 0.76f), new Vector2(0.8f, 0.95f), new Vector2(0.86f, 0.98f), new Vector2(0.86f, 1.06f), new Vector2(0.8f, 1.09f),
                new Vector2(0.8f, 1.45f), new Vector2(0.86f, 1.48f), new Vector2(0.86f, 1.56f), new Vector2(0.8f, 1.59f), new Vector2(0.8f, 1.75f), new Vector2(0.7f, 1.9f), new Vector2(0.5f, 2.0f),
                new Vector2(0.25f, 2.05f), new Vector2(0f, 2.06f),
            };
            m.Revolver(1, perfil, 12, Quaternion.identity, Vector3.zero, new Vector3(0f, 1.3f, 0f), false);
            m.Caja(1, new Vector3(0f, 2.1f, 0f), new Vector3(0.3f, 0.06f, 0.3f));
            return m.Crear("T1441_Tinaco");
        }

        private static void Azoteas(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Techos");
            GameObject antena = Cargar("CY04_AntenaAzotea");
            GameObject tendedero = Cargar("CC04_Tendedero"), lavadero = Cargar("CC05_Lavadero"), huacales = Cargar("C17_Huacales"), tambo = Cargar("C16_Tambo");
            GameObject chatarra = Cargar("IN02_Chatarra"), aire = Cargar("C10_AireAcondicionado");
            Mesh[] parabolicas = { MallaParabolica(0), MallaParabolica(1), MallaParabolica(2) };
            Mesh[] varillas = { MallaVarillas(0), MallaVarillas(1), MallaVarillas(2) };
            Mesh tinaco = MallaTinaco();
            c.Obra.Mallas.Add(tinaco);
            Material ladrillo = MatLit("Ladrillo", new Color(0.5f, 0.26f, 0.19f), 0.1f);
            // Tinacos: negros (los más), azules, blancos y terracota, de varios tamaños.
            (Material mat, float peso)[] colores =
            {
                (MatLit("TinacoNegro", new Color(0.04f, 0.04f, 0.05f), 0.45f), 40f), (MatLit("TinacoAzul", new Color(0.1f, 0.32f, 0.72f), 0.4f), 22f),
                (MatLit("TinacoBlanco", new Color(0.86f, 0.87f, 0.9f), 0.35f), 24f), (MatLit("TinacoTerracota", new Color(0.7f, 0.32f, 0.2f), 0.3f), 14f),
            };
            (string tipo, float peso)[] tipos =
            {
                ("tinaco", 18f), ("antena", 12f), ("parabolica", 6f), ("tendedero", 16f), ("lavadero", 10f), ("varillas", 10f), ("caja", 6f), ("tambo", 4f), ("chatarra", 4f), ("aire", 6f),
            };
            float suma = tipos.Sum(x => x.peso);
            int total = 0, conObjetos = 0, id = 0, malAsentados = 0;
            Dictionary<string, int> porTipo = new Dictionary<string, int>();
            foreach (Techo t in c.Techos.OrderBy(q => q.H.X0).ThenBy(q => q.H.Z0))
            {
                id++;
                t.Grupo = padre;
                float r = c.Obra.Rango(0f, 1f);
                int objetivo = r < 0.2f ? 2 : (r < 0.55f ? 3 : (r < 0.8f ? 4 : 5));
                int falta = objetivo - t.Existentes;
                if (falta <= 0 || total >= 1500)
                {
                    continue;
                }
                bool conTinaco = false, conParabolica = false, conVarillas = false;
                int puestos = 0;
                for (int k = 0; k < falta + 2 && puestos < falta; k++)
                {
                    float q = c.Obra.Rango(0f, suma);
                    string tipo = tipos[0].tipo;
                    foreach ((string nombre, float peso) in tipos)
                    {
                        if (q < peso)
                        {
                            tipo = nombre;
                            break;
                        }
                        q -= peso;
                    }
                    if ((tipo == "tinaco" && conTinaco) || (tipo == "parabolica" && conParabolica) || (tipo == "varillas" && conVarillas))
                    {
                        continue;
                    }
                    bool ok = false;
                    switch (tipo)
                    {
                        case "tinaco":
                            ok = PonerTinaco(c, t, tinaco, ladrillo, colores);
                            conTinaco |= ok;
                            break;
                        case "antena":
                            ok = PonerPrefab(c, t, antena, Vector3.one * 1.1f, "Antena", ref malAsentados);
                            break;
                        case "parabolica":
                            ok = PonerGenerado(c, t, parabolicas[id % 3], new[] { c.Gris, c.Gris }, 0.45f, 1.6f);
                            conParabolica |= ok;
                            break;
                        case "tendedero":
                            ok = PonerPrefab(c, t, tendedero, Vector3.one, "Tendedero", ref malAsentados);
                            break;
                        case "lavadero":
                            ok = PonerPrefab(c, t, lavadero, Vector3.one, "Lavadero", ref malAsentados);
                            break;
                        case "varillas":
                            ok = PonerEsquina(c, t, varillas[id % 3], new[] { c.Concreto, c.MarcoOxido });
                            conVarillas |= ok;
                            break;
                        case "caja":
                            ok = PonerPrefab(c, t, huacales, Vector3.one, "Huacales", ref malAsentados);
                            break;
                        case "tambo":
                            ok = PonerPrefab(c, t, tambo, Vector3.one, "Tambo", ref malAsentados);
                            break;
                        case "chatarra":
                            if (chatarra != null)
                            {
                                Bounds m = Medida(chatarra);
                                float e = Mathf.Min(1f, 1.8f / Mathf.Max(0.1f, Mathf.Max(m.size.x, m.size.z)));
                                ok = PonerPrefab(c, t, chatarra, Vector3.one * e, "Chatarra", ref malAsentados);
                            }
                            break;
                        case "aire":
                            ok = PonerPrefab(c, t, aire, Vector3.one, "Aire", ref malAsentados);
                            break;
                    }
                    if (ok)
                    {
                        puestos++;
                        total++;
                        porTipo[tipo] = (porTipo.TryGetValue(tipo, out int v) ? v : 0) + 1;
                    }
                }
                if (puestos > 0)
                {
                    conObjetos++;
                }
            }
            c.Azoteas = conObjetos;
            Physics.SyncTransforms();
            Log("Azoteas con objetos nuevos: " + conObjetos + "; objetos: " + total + " (" + string.Join(", ", porTipo.Select(p => p.Key + " " + p.Value)) + "). Mal asentados al colocar (más de 5 cm): " + malAsentados + "; rechazados por no tener apoyo debajo (misma prueba que Validar): " + rechazadosApoyo + ".");
        }

        /// <summary>Pone un prefab en la azotea con su punto más bajo exactamente en la superficie medida (t.Y); los que no necesitan collider se combinan en lotes.</summary>
        private static bool PonerPrefab(Ctx c, Techo t, GameObject prefab, Vector3 escala, string nombre, ref int malAsentados)
        {
            if (prefab == null)
            {
                return false;
            }
            Bounds m = Medida(prefab);
            Vector3 tam = new Vector3(m.size.x * escala.x, m.size.y * escala.y, m.size.z * escala.z);
            int giroRand = c.Obra.Azar.Next(4);
            bool cruzado = giroRand % 2 == 1;
            float hx = (cruzado ? tam.z : tam.x) * 0.5f, hz = (cruzado ? tam.x : tam.z) * 0.5f;
            Vector2 pos;
            if (!BuscarHueco(c, t, hx, hz, tam.y, Candidatos(c, t.H, 14), true, out pos))
            {
                return false;
            }
            GameObject g = Instanciar(prefab, t.Grupo, nombre + "_" + (c.Contador++));
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f * giroRand, 0f));
            g.transform.localScale = escala;
            Bounds b = Cubierta(g.transform);
            g.transform.position += new Vector3(pos.x - b.center.x, t.Y - b.min.y, pos.y - b.center.z);
            Physics.SyncTransforms();
            Bounds fin = Cubierta(g.transform);
            if (Mathf.Abs(fin.min.y - t.Y) > 0.05f)
            {
                malAsentados++;
            }
            // Con la caja real (no la predicha): si cierra el pasillo de 1.2 m o se sale de la casa, se quita.
            VariarCerroT1364.CasaT hh = t.H;
            if (EnPasillo(t, fin.center.x, fin.center.z, Mathf.Max(fin.extents.x, fin.extents.z) + 0.1f) || fin.min.x < hh.X0 || fin.max.x > hh.X1 || fin.min.z < hh.Z0 || fin.max.z > hh.Z1)
            {
                Object.DestroyImmediate(g);
                Physics.SyncTransforms();
                return false;
            }
            if (!Apoyado(c, g.transform, fin))
            {
                rechazadosApoyo++;
                Object.DestroyImmediate(g);
                Physics.SyncTransforms();
                return false;
            }
            c.Ocupado.Add(fin);
            PegarInstancia(g);
            return true;
        }

        private static bool PonerTinaco(Ctx c, Techo t, Mesh malla, Material ladrillo, (Material mat, float peso)[] colores)
        {
            float e = c.Obra.Rango(0.8f, 1.2f);
            Vector2 pos;
            if (!BuscarHueco(c, t, 0.95f * e, 0.95f * e, 2.2f * e, Candidatos(c, t.H, 14), true, out pos))
            {
                return false;
            }
            float q = c.Obra.Rango(0f, colores.Sum(x => x.peso));
            Material color = colores[0].mat;
            foreach ((Material mat, float peso) in colores)
            {
                if (q < peso)
                {
                    color = mat;
                    break;
                }
                q -= peso;
            }
            // Un objeto vacío con un BoxCollider del tamaño del tinaco (base de ladrillo + tanque, ~1.7 x 2.1 x 1.7 m a escala 1): la malla visible va en el lote por material.
            Quaternion giro = Quaternion.Euler(0f, c.Obra.Rango(0f, 360f), 0f);
            Vector3 donde = new Vector3(pos.x, t.Y, pos.y);
            GameObject g = new GameObject("Tinaco_" + (c.Contador++));
            g.transform.SetParent(t.Grupo, false);
            g.transform.SetPositionAndRotation(donde, giro);
            BoxCollider caja = g.AddComponent<BoxCollider>();
            caja.size = new Vector3(1.72f, 2.1f, 1.72f) * e;
            caja.center = new Vector3(0f, 1.05f * e, 0f);
            Physics.SyncTransforms();
            Bounds fin = caja.bounds;
            VariarCerroT1364.CasaT hh = t.H;
            if (EnPasillo(t, fin.center.x, fin.center.z, Mathf.Max(fin.extents.x, fin.extents.z) + 0.1f) || fin.min.x < hh.X0 || fin.max.x > hh.X1 || fin.min.z < hh.Z0 || fin.max.z > hh.Z1)
            {
                Object.DestroyImmediate(g);
                Physics.SyncTransforms();
                return false;
            }
            if (!Apoyado(c, g.transform, fin))
            {
                rechazadosApoyo++;
                Object.DestroyImmediate(g);
                Physics.SyncTransforms();
                return false;
            }
            Pegar(malla, new[] { ladrillo, color }, Matrix4x4.TRS(donde, giro, Vector3.one * e));
            c.Ocupado.Add(fin);
            return true;
        }

        private static bool PonerGenerado(Ctx c, Techo t, Mesh malla, Material[] mats, float radio, float alto)
        {
            Vector2 pos;
            if (!BuscarHueco(c, t, radio, radio, alto, Candidatos(c, t.H, 14), true, out pos))
            {
                return false;
            }
            Quaternion giro = Quaternion.Euler(0f, c.Obra.Rango(0f, 360f), 0f);
            Pegar(malla, mats, Matrix4x4.TRS(new Vector3(pos.x, t.Y, pos.y), giro, Vector3.one));
            c.Ocupado.Add(new Bounds(new Vector3(pos.x, t.Y + alto * 0.5f, pos.y), new Vector3(radio * 2f, alto, radio * 2f)));
            return true;
        }

        private static bool PonerEsquina(Ctx c, Techo t, Mesh malla, Material[] mats)
        {
            VariarCerroT1364.CasaT h = t.H;
            List<Vector2> esquinas = new List<Vector2>
            {
                new Vector2(h.X0 + 0.7f, h.Z0 + 0.7f), new Vector2(h.X1 - 0.7f, h.Z0 + 0.7f), new Vector2(h.X0 + 0.7f, h.Z1 - 0.7f), new Vector2(h.X1 - 0.7f, h.Z1 - 0.7f),
            };
            Vector2 pos;
            if (!BuscarHueco(c, t, 0.4f, 0.4f, 1.6f, esquinas.OrderBy(q => c.Obra.Azar.Next()).ToList(), true, out pos))
            {
                return false;
            }
            Pegar(malla, mats, Matrix4x4.TRS(new Vector3(pos.x, t.Y, pos.y), Quaternion.Euler(0f, c.Obra.Rango(0f, 90f), 0f), Vector3.one));
            c.Ocupado.Add(new Bounds(new Vector3(pos.x, t.Y + 0.8f, pos.y), new Vector3(0.8f, 1.6f, 0.8f)));
            return true;
        }

        // ================================================================ Fachadas

        private static bool Pared(Ctx c, VariarCerroT1364.CasaT h, float y, float z, out Vector3 punto)
        {
            punto = default;
            float xFrente = h.Normal > 0f ? h.X1 : h.X0;
            Vector3 salida = new Vector3(h.Normal, 0f, 0f);
            int n = c.Fisica.Raycast(new Vector3(xFrente + h.Normal * 1.5f, y, z), -salida, bufRayos, 3f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            bool ok = false;
            for (int i = 0; i < n; i++)
            {
                Transform t = bufRayos[i].collider.transform;
                if (t.IsChildOf(c.Nuevo) || EsPiso(c, t) || bufRayos[i].normal.x * h.Normal < 0.8f || Mathf.Abs(bufRayos[i].point.x - xFrente) > 0.6f)
                {
                    continue;
                }
                if (bufRayos[i].distance < mejor)
                {
                    mejor = bufRayos[i].distance;
                    punto = bufRayos[i].point;
                    ok = true;
                }
            }
            return ok;
        }

        /// <summary>Pone una pieza del kit (que mira a -Z) contra la pared: la cara de atrás sobre el punto, centrada en z y apoyada en yBase.</summary>
        private static GameObject Montar(GameObject g, Vector3 pared, float yBase, float normal)
        {
            Vector3 salida = new Vector3(normal, 0f, 0f);
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(-salida));
            Bounds b = Cubierta(g.transform);
            float mitadFondo = Mathf.Abs(salida.x) * b.extents.x + Mathf.Abs(salida.z) * b.extents.z;
            float desfase = Vector3.Dot(pared, salida) - (Vector3.Dot(b.center, salida) - mitadFondo);
            g.transform.position += salida * desfase + new Vector3(0f, yBase - b.min.y, pared.z - b.center.z);
            return g;
        }

        private static void Fachadas(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Fachadas");
            GameObject aire = Cargar("C10_AireAcondicionado"), medidor = Cargar("C14_RegistroMedidores"), macetas = Cargar("C20d_Macetas"), toldo = Cargar(KitS2c.CodigoToldo);
            Mesh reja = MallaReja();
            Mesh lampara = MallaLampara();
            Mesh[] banderas = { MallaBandera("T1441_BanderaAlta", 0.5f, 1.25f), MallaBandera("T1441_BanderaMedia", 0.7f, 0.7f), MallaBandera("T1441_BanderaChica", 0.45f, 0.5f) };
            Mesh[] planos = { MallaPlano("T1441_PlanoAncho", 1.1f, 0.4f), MallaPlano("T1441_PlanoChico", 0.6f, 0.6f) };
            c.Obra.Mallas.Add(reja);
            c.Obra.Mallas.Add(lampara);
            c.Obra.Mallas.AddRange(banderas);
            c.Obra.Mallas.AddRange(planos);
            Vector3 camara = K13Desde;
            int casas = 0, ac = 0, med = 0, lam = 0, rej = 0, tol = 0, mac = 0, neon = 0, intento = 0;
            foreach (VariarCerroT1364.CasaT h in c.Casas.Where(x => x.Kit))
            {
                Vector3 centro = new Vector3((h.X0 + h.X1) * 0.5f, h.Base, (h.Z0 + h.Z1) * 0.5f);
                if (new Vector2(centro.x - camara.x, centro.z - camara.z).magnitude > DistanciaFachadas)
                {
                    continue;
                }
                Transform baja = null;
                foreach (Transform t in h.Raiz)
                {
                    if (t.name.StartsWith("C01_Fachada") || t.name.StartsWith("C02_Fachada"))
                    {
                        baja = t;
                        break;
                    }
                }
                KitS2c.Medida mb = null;
                if (baja != null)
                {
                    try
                    {
                        mb = KitS2c.Medir(baja.name.Split(' ')[0]);
                    }
                    catch (Exception e)
                    {
                        Log("Medir " + baja.name + ": " + e.Message);
                    }
                }
                intento++;
                // Lo que ya está en la fachada (puerta, toldo, foco, neones, ventanas con luz) para no taparlo.
                List<Bounds> prohibidos = new List<Bounds>();
                foreach (Transform t in h.Raiz.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name == "Puerta" || t.name == "Toldo" || t.name == "FocoPared" || t.name == "Neon" || t.name == "VentanaCalida")
                    {
                        Bounds b = Cubierta(t);
                        b.Expand(0.3f);
                        prohibidos.Add(b);
                    }
                }
                Vector3 salida = new Vector3(h.Normal, 0f, 0f);
                // Las casas del oeste entre z = 60 y 150 son las de la derecha del cuadro de K13 (la ciudad queda espejada): ahí va el detalle fuerte.
                bool fuerte = h.Normal > 0f && centro.z > 60f && centro.z < 150f;
                Func<Transform> asegurar = () => padre;
                int rejasCasa = 0;
                // Puerta en el mundo (si la planta baja la tiene).
                bool hayPuerta = false;
                Vector3 puerta = default;
                float anchoPuerta = 1f;
                if (baja != null && mb != null && mb.Vano.HasValue)
                {
                    Rect v = mb.Vano.Value;
                    puerta = baja.TransformPoint(new Vector3(v.center.x, v.yMin, mb.Muro));
                    anchoPuerta = v.width * Mathf.Abs(baja.lossyScale.x);
                    hayPuerta = true;
                }
                float zc = centro.z;
                int cuantas = 0;
                // Lámpara de pared junto a la puerta.
                if (c.Obra.Chance(fuerte ? 0.95f : 0.7f) && cuantas < 5)
                {
                    float lado = c.Obra.Chance(0.5f) ? 1f : -1f;
                    float zl = hayPuerta ? puerta.z + lado * (anchoPuerta * 0.5f + 0.5f) : zc + lado * (h.Z1 - h.Z0) * 0.36f;
                    zl = Mathf.Clamp(zl, h.Z0 + 0.4f, h.Z1 - 0.4f);
                    float yl = (hayPuerta ? puerta.y : h.Base) + 2.4f;
                    Vector3 p;
                    if (Pared(c, h, yl, zl, out p) && !EnLibre(c, p, 0.5f) && Libre3D(prohibidos, c.OcupadoPared, p, yl, zl, 0.3f, 0.4f))
                    {
                        Pegar(lampara, new[] { c.Marco, c.Calido }, Matrix4x4.TRS(p, Quaternion.LookRotation(salida), Vector3.one));
                        c.OcupadoPared.Add(new Bounds(new Vector3(p.x, yl, zl), new Vector3(0.6f, 0.6f, 0.6f)));
                        c.LucesFachada.Add((p, h.Normal));
                        lam++;
                        cuantas++;
                    }
                }
                // Medidor al otro lado de la puerta.
                if (medidor != null && hayPuerta && c.Obra.Chance(fuerte ? 0.85f : 0.5f) && cuantas < 6)
                {
                    float lado = c.Obra.Chance(0.5f) ? 1f : -1f;
                    float zm = Mathf.Clamp(puerta.z + lado * (anchoPuerta * 0.5f + 0.6f), h.Z0 + 0.4f, h.Z1 - 0.4f);
                    Vector3 p;
                    if (Pared(c, h, puerta.y + 1.1f, zm, out p) && !EnLibre(c, p, 0.5f))
                    {
                        GameObject g = Instanciar(medidor, asegurar(), "Medidor");
                        Montar(g, p, puerta.y + 0.9f, h.Normal);
                        Physics.SyncTransforms();
                        if (LibreObjeto(prohibidos, c.OcupadoPared, g))
                        {
                            c.OcupadoPared.Add(Cubierta(g.transform));
                            PegarInstancia(g);
                            med++;
                            cuantas++;
                        }
                        else
                        {
                            Object.DestroyImmediate(g);
                        }
                    }
                }
                // Aire acondicionado en la pared.
                if (aire != null && c.Obra.Chance(fuerte ? 0.85f : 0.6f) && cuantas < 6 && h.Top - h.Base > 3.4f)
                {
                    float za = c.Obra.Rango(h.Z0 + 0.9f, h.Z1 - 0.9f);
                    float ya = Mathf.Min(h.Base + (h.Top - h.Base > 5.4f ? 3.5f : 2.1f), h.Top - 1.4f);
                    Vector3 p;
                    if (Pared(c, h, ya + 0.3f, za, out p) && !EnLibre(c, p, 0.5f))
                    {
                        GameObject g = Instanciar(aire, asegurar(), "Aire");
                        Montar(g, p, ya, h.Normal);
                        Physics.SyncTransforms();
                        if (LibreObjeto(prohibidos, c.OcupadoPared, g))
                        {
                            c.OcupadoPared.Add(Cubierta(g.transform));
                            PegarInstancia(g);
                            ac++;
                            cuantas++;
                        }
                        else
                        {
                            Object.DestroyImmediate(g);
                        }
                    }
                }
                // Toldo de lámina sobre la puerta de los locales.
                if (toldo != null && hayPuerta && !EnLibre(c, puerta, 0.8f) && baja.name.StartsWith("C01_Fachada") && c.Obra.Chance(fuerte ? 0.95f : 0.6f) && anchoPuerta > 1.1f)
                {
                    Bounds bt = Medida(toldo);
                    float e = (anchoPuerta + 0.5f) / Mathf.Max(0.1f, bt.size.x);
                    Rect v = mb.Vano.Value;
                    Vector3 dintel = baja.TransformPoint(new Vector3(v.center.x, v.yMax, mb.Muro));
                    GameObject g = Instanciar(toldo, asegurar(), "Toldo");
                    g.transform.SetPositionAndRotation(dintel - Vector3.up * 0.1f + salida * (bt.size.z * e * 0.5f), Quaternion.LookRotation(-salida));
                    g.transform.localScale = Vector3.one * e;
                    Physics.SyncTransforms();
                    if (prohibidos.Any(b => b.Intersects(Cubierta(g.transform)) && b.size.y < 1f && b.center.y > dintel.y - 0.7f))
                    {
                        // ya hay algo sobre el dintel (neón o foco): no se encima
                        Object.DestroyImmediate(g);
                    }
                    else
                    {
                        c.OcupadoPared.Add(Cubierta(g.transform));
                        PegarInstancia(g);
                        tol++;
                        cuantas++;
                    }
                }
                // Macetas junto a la puerta.
                if (macetas != null && hayPuerta && c.Obra.Chance(fuerte ? 0.55f : 0.2f) && cuantas < 6)
                {
                    float lado = c.Obra.Chance(0.5f) ? 1f : -1f;
                    float zm = Mathf.Clamp(puerta.z + lado * (anchoPuerta * 0.5f + 0.7f), h.Z0 + 0.5f, h.Z1 - 0.5f);
                    float yg;
                    float xBanq = (h.Normal > 0f ? h.X1 : h.X0) + h.Normal * 0.4f;
                    if (Suelo(c, xBanq, zm, puerta.y, out yg) && Mathf.Abs(yg - puerta.y) < 0.4f && !EnLibre(c, new Vector3(xBanq, yg, zm), 0.6f))
                    {
                        GameObject g = Instanciar(macetas, asegurar(), "Macetas");
                        g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(-salida));
                        Bounds b = Cubierta(g.transform);
                        g.transform.position += new Vector3(xBanq - b.center.x, yg - b.min.y, zm - b.center.z);
                        Physics.SyncTransforms();
                        Bounds mio = Cubierta(g.transform);
                        if (b.size.x < 1.6f && b.size.z < 1.6f && Libre(c, mio.center + Vector3.up * 0.05f, mio.extents * 0.9f) && !prohibidos.Any(pb => pb.Intersects(mio)))
                        {
                            c.OcupadoPared.Add(mio);
                            PegarInstancia(g);
                            mac++;
                            cuantas++;
                        }
                        else
                        {
                            Object.DestroyImmediate(g);
                        }
                    }
                }
                // Rejas en las ventanas de la planta baja.
                if (baja != null && mb != null)
                {
                    List<Rect> puestas = new List<Rect>();
                    foreach ((Rect r, float fondo) in mb.Huecos)
                    {
                        if (puestas.Any(p => p.Overlaps(r)))
                        {
                            continue;
                        }
                        puestas.Add(r);
                        if (r.yMin < 1f || r.height > 2.2f || r.width > 2.4f || !c.Obra.Chance(fuerte ? 0.85f : 0.55f))
                        {
                            continue;
                        }
                        if (EnLibre(c, baja.TransformPoint(new Vector3(r.center.x, r.center.y, mb.Muro)), 0.4f))
                        {
                            continue;
                        }
                        Matrix4x4 local = Matrix4x4.TRS(new Vector3(r.center.x, r.center.y, mb.Muro), Quaternion.identity, new Vector3(r.width * 1.04f, r.height * 1.04f, 1f));
                        Pegar(reja, new[] { c.MarcoOxido }, baja.localToWorldMatrix * local);
                        rej++;
                        rejasCasa++;
                    }
                }
                // Letrero de neón chico (1 de cada 3 a 4 casas).
                if (c.Obra.Chance(fuerte ? 0.85f : 0.5f))
                {
                    float lado = c.Obra.Chance(0.5f) ? 1f : -1f;
                    bool bandera = c.Obra.Chance(0.7f);
                    int variante = bandera ? c.Obra.Azar.Next(banderas.Length) : c.Obra.Azar.Next(planos.Length);
                    float alto = bandera ? (variante == 0 ? 1.25f : (variante == 1 ? 0.7f : 0.5f)) : (variante == 0 ? 0.4f : 0.6f);
                    float zn = Mathf.Clamp(zc + lado * (h.Z1 - h.Z0) * 0.42f, h.Z0 + 0.5f, h.Z1 - 0.5f);
                    float yn = Mathf.Min(h.Base + 3.0f + alto * 0.5f + c.Obra.Rango(0f, 0.5f), h.Top - 0.4f - alto * 0.5f);
                    Vector3 p;
                    if (yn - alto * 0.5f > h.Base + 2.0f && Pared(c, h, yn, zn, out p) && !EnLibre(c, p, 0.5f))
                    {
                        Material mat = c.Neon[(c.Letreros) % c.Neon.Length];
                        Mesh malla = bandera ? banderas[variante] : planos[variante];
                        Bounds caja = new Bounds(new Vector3(p.x + h.Normal * (bandera ? 0.4f : 0.05f), yn, zn), new Vector3(bandera ? 0.9f : 0.2f, alto + 0.1f, bandera ? 0.7f : (variante == 0 ? 1.2f : 0.7f)));
                        if (Libre3D(prohibidos, c.OcupadoPared, caja.center, caja.center.y, caja.center.z, caja.extents.z, caja.extents.y, caja.extents.x))
                        {
                            Pegar(malla, new[] { c.Marco, mat }, Matrix4x4.TRS(new Vector3(p.x, yn, zn), Quaternion.LookRotation(salida), Vector3.one));
                            c.OcupadoPared.Add(caja);
                            c.LucesFachada.Add((new Vector3(p.x, yn, zn), h.Normal));
                            c.Letreros++;
                            neon++;
                            cuantas++;
                        }
                    }
                }
                if (cuantas > 0 || rejasCasa > 0)
                {
                    casas++;
                }
            }
            c.Fachadas = casas;
            Physics.SyncTransforms();
            Log("Fachadas: " + casas + " casas con detalle de " + intento + " revisadas (aires " + ac + ", medidores " + med + ", lámparas " + lam + ", rejas " + rej + ", toldos " + tol + ", macetas " + mac + ", neones " + neon + ").");
        }

        private static bool Libre3D(List<Bounds> prohibidos, List<Bounds> puestos, Vector3 centro, float y, float z, float mitadZ, float mitadY, float mitadX = 0.3f)
        {
            Bounds mio = new Bounds(new Vector3(centro.x, y, z), new Vector3(mitadX * 2f, mitadY * 2f, mitadZ * 2f));
            return LibreCaja(prohibidos, puestos, mio);
        }

        private static bool LibreObjeto(List<Bounds> prohibidos, List<Bounds> puestos, GameObject g)
        {
            return LibreCaja(prohibidos, puestos, Cubierta(g.transform));
        }

        private static bool LibreCaja(List<Bounds> prohibidos, List<Bounds> puestos, Bounds mio)
        {
            foreach (Bounds b in prohibidos)
            {
                if (b.Intersects(mio))
                {
                    return false;
                }
            }
            foreach (Bounds b in puestos)
            {
                if (b.Intersects(mio))
                {
                    return false;
                }
            }
            return true;
        }

        // ================================================================ Calle: carros, basura, charcos

        private static void Carros(Ctx c)
        {
            Transform calle = Hijo(c.Nuevo, "Calle");
            Transform padre = Hijo(calle, "Carros");
            List<GameObject> prefabs = RutasCarros.Select(CargarRuta).Where(p => p != null).ToList();
            if (prefabs.Count == 0)
            {
                Log("FALTAN los visuales de carros en Assets/Arte/Vehiculos/Prefabs; no hay carros.");
                return;
            }
            // Pegados a la banqueta (calle de 9 m, banquetas en ±4.5 m): lado -1 = oeste (mira al norte, hacia K13), +1 = este (mira al sur). El primero lleva los faros encendidos.
            (float lado, float z, bool hero)[] sitios =
            {
                (-1f, 140f, true), (1f, 118f, false), (-1f, 108f, false), (1f, 92f, false), (-1f, 76f, false), (1f, 58f, false), (-1f, 44f, false), (1f, 36f, false),
            };
            int n = 0, usados = 0;
            foreach (var s in sitios)
            {
                if (n >= 5)
                {
                    break;
                }
                bool puesto = false;
                foreach (float dz in new[] { 0f, 3f, -3f, 6f, -6f })
                {
                    for (int pi = 0; pi < prefabs.Count && !puesto; pi++)
                    {
                        GameObject prefab = prefabs[(usados + pi) % prefabs.Count];
                        Bounds m = Medida(prefab);
                        // El eje largo del modelo puede ser X o Z: el frente se toma como el lado + de ese eje.
                        bool largoX = m.size.x > m.size.z;
                        float largo = largoX ? m.size.x : m.size.z, ancho = largoX ? m.size.z : m.size.x;
                        if (largo < 3f || largo > 5.6f || ancho > 2.4f || m.size.y > 2.4f)
                        {
                            continue;
                        }
                        float z = s.z + dz;
                        float x = XCentroCalle + s.lado * (4.5f - ancho * 0.5f - 0.1f);
                        // Nada a menos de 10 m del lente de K13 (tapaba el cuarto inferior de la foto); la referencia tiene el carro cercano a ~8–10 m.
                        if (new Vector2(x - K13Desde.x, z - K13Desde.z).magnitude < 10f || EnLibre(c, new Vector3(x, YCalle(z), z), largo * 0.5f + 0.5f))
                        {
                            continue;
                        }
                        float yN, yS;
                        if (!Suelo(c, x, z + largo * 0.35f, YCalle(z), out yN) || !Suelo(c, x, z - largo * 0.35f, YCalle(z), out yS))
                        {
                            continue;
                        }
                        float yMedio = (yN + yS) * 0.5f;
                        if (!Libre(c, new Vector3(x, yMedio + 0.95f, z), new Vector3(ancho * 0.5f + 0.1f, 0.6f, largo * 0.5f + 0.25f)))
                        {
                            continue;
                        }
                        bool haciaSur = s.lado > 0f;
                        float pendiente = (yN - yS) / (largo * 0.7f);
                        Vector3 dir = haciaSur ? new Vector3(0f, -pendiente, -1f) : new Vector3(0f, pendiente, 1f);
                        Quaternion giro = Quaternion.LookRotation(dir.normalized) * (largoX ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.identity);
                        GameObject car = Instanciar(prefab, padre, "Carro_" + prefab.name + "_" + n);
                        car.transform.SetPositionAndRotation(new Vector3(x, yMedio, z), giro);
                        Physics.SyncTransforms();
                        Bounds b = Cubierta(car.transform);
                        car.transform.position += Vector3.up * (Mathf.Min(yN, yS) - b.min.y);
                        Physics.SyncTransforms();
                        foreach (Rigidbody rb in car.GetComponentsInChildren<Rigidbody>(true))
                        {
                            rb.isKinematic = true;
                        }
                        if (s.hero)
                        {
                            // Faros encendidos y luz de piso turquesa (como el carro de la referencia). Coordenadas locales de la caja del prefab; el frente es el + del eje largo.
                            Malla fm = new Malla(2);
                            float yf = m.min.y + m.size.y * 0.36f;
                            Vector3 frenteLocal = largoX ? Vector3.right : Vector3.forward;
                            Vector3 ladoLocal = largoX ? Vector3.forward : Vector3.right;
                            float tope = largoX ? m.max.x : m.max.z;
                            Vector3 centroLocal = new Vector3(m.center.x, 0f, m.center.z);
                            foreach (float sl in new[] { -1f, 1f })
                            {
                                Vector3 c0 = centroLocal + ladoLocal * (sl * ancho * 0.3f) + frenteLocal * (tope + 0.012f - Vector3.Dot(centroLocal, frenteLocal));
                                c0.y = yf;
                                fm.Quad(0, c0 + ladoLocal * 0.16f + Vector3.up * 0.07f, c0 - ladoLocal * 0.16f + Vector3.up * 0.07f, c0 - ladoLocal * 0.16f - Vector3.up * 0.07f, c0 + ladoLocal * 0.16f - Vector3.up * 0.07f, frenteLocal);
                            }
                            Vector3 cb = centroLocal + frenteLocal * (tope - 0.15f - Vector3.Dot(centroLocal, frenteLocal));
                            cb.y = m.min.y + 0.12f;
                            fm.Caja(1, cb, largoX ? new Vector3(0.04f, 0.03f, ancho * 0.8f) : new Vector3(ancho * 0.8f, 0.03f, 0.04f));
                            Mesh farosMalla = fm.Crear("T1441_FarosCarro");
                            c.Obra.Mallas.Add(farosMalla);
                            Material faros = MatEmi("Faros", new Color(1f, 0.92f, 0.7f), 4.5f);
                            Generado(car.transform, "Faros", farosMalla, new[] { faros, c.Neon[1] }, car.transform.position, car.transform.rotation, Vector3.one, false);
                            Vector3 adelante = car.transform.rotation * frenteLocal;
                            Vector3 luzPos = car.transform.position + adelante * (largo * 0.5f + 0.5f) + Vector3.up * 0.7f;
                            KitS2c.Luz(car.transform, luzPos, LightType.Spot, new Color(1f, 0.85f, 0.6f), 90f, 16f, 55f, adelante + Vector3.down * 0.12f);
                        }
                        // Rendimiento: los carros de Tripo tienen ~90 000 triángulos; se apagan bajo el 5 % de la pantalla.
                        LODGroup lgCarro = car.GetComponent<LODGroup>();
                        if (lgCarro == null)
                        {
                            Renderer[] rsCarro = car.GetComponentsInChildren<Renderer>().Where(rr => !(rr is ParticleSystemRenderer)).ToArray();
                            lgCarro = car.AddComponent<LODGroup>();
                            lgCarro.SetLODs(new[] { new LOD(0.05f, rsCarro) });
                            lgCarro.RecalculateBounds();
                        }
                        else
                        {
                            LOD[] niveles = lgCarro.GetLODs();
                            if (niveles.Length > 0)
                            {
                                if (niveles.Length > 1 && niveles[niveles.Length - 2].screenRelativeTransitionHeight <= 0.05f)
                                {
                                    niveles[niveles.Length - 2].screenRelativeTransitionHeight = 0.1f;
                                }
                                niveles[niveles.Length - 1].screenRelativeTransitionHeight = 0.05f;
                                lgCarro.SetLODs(niveles);
                            }
                        }
                        c.Carros++;
                        n++;
                        puesto = true;
                    }
                    if (puesto)
                    {
                        break;
                    }
                }
                usados++;
            }
            Log("Carros estacionados: " + n + " (visual sin lógica de manejo, de Assets/Arte/Vehiculos/Prefabs; sus Rigidbody, si los hay, quedan kinemáticos).");
        }

        private static void Basura(Ctx c)
        {
            Transform calle = c.Nuevo.Find("Calle") ?? Hijo(c.Nuevo, "Calle");
            Transform padre = Hijo(calle, "Basura");
            GameObject basurero = Cargar("C11_Basurero"), tambo = Cargar("C16_Tambo"), huacales = Cargar("C17_Huacales"), macetas = Cargar("C20d_Macetas");
            Mesh[] bolsas = { MallaBolsa(0), MallaBolsa(1), MallaBolsa(2) };
            c.Obra.Mallas.AddRange(bolsas);
            float xBordeOeste = XCentroCalle - 4.5f - 0.45f;
            List<float> usadasZ = new List<float>();
            int nPrefabs = 0, nBolsas = 0;
            GameObject[] lista = { basurero, tambo, huacales, macetas, tambo, huacales, basurero, huacales, macetas, tambo };
            for (int i = 0; i < 70 && (nPrefabs < lista.Length || nBolsas < 14); i++)
            {
                // La mitad en el cuadro de K13 (z de 62 a 140), sobre la banqueta oeste, la este y el paseo escalonado de T1416.
                float z = i % 2 == 0 ? c.Obra.Rango(62f, 140f) : c.Obra.Rango(ZVistaSur + 4f, ZVistaNorte - 4f);
                if (usadasZ.Any(u => Mathf.Abs(u - z) < 2.5f))
                {
                    continue;
                }
                float sorteo = c.Obra.Rango(0f, 1f);
                float x = sorteo < 0.4f ? xBordeOeste : (sorteo < 0.7f ? XCentroCalle + 4.5f + 0.45f : XCentroCalle + 7f);
                float y;
                if (!Suelo(c, x, z, YCalle(z), out y) || Mathf.Abs(y - YCalle(z)) > 0.6f)
                {
                    continue;
                }
                if (EnLibre(c, new Vector3(x, y, z), 0.8f))
                {
                    continue;
                }
                bool prefabToca = nPrefabs < lista.Length && (nBolsas >= 14 || c.Obra.Chance(0.5f));
                if (prefabToca && lista[nPrefabs] != null)
                {
                    GameObject p = lista[nPrefabs];
                    Bounds m = Medida(p);
                    if (m.size.x > 1.6f || m.size.z > 1.6f)
                    {
                        nPrefabs++;
                        continue;
                    }
                    if (!Libre(c, new Vector3(x, y + 0.1f + m.size.y * 0.5f, z), new Vector3(m.size.x * 0.5f, m.size.y * 0.5f - 0.05f, m.size.z * 0.5f)))
                    {
                        continue;
                    }
                    GameObject g = Instanciar(p, padre, p.name + "_" + padre.childCount);
                    g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, c.Obra.Rango(0f, 360f), 0f));
                    Bounds b = Cubierta(g.transform);
                    g.transform.position += new Vector3(x - b.center.x, y - b.min.y, z - b.center.z);
                    Physics.SyncTransforms();
                    PegarInstancia(g);
                    usadasZ.Add(z);
                    nPrefabs++;
                    c.Basura++;
                }
                else if (nBolsas < 14)
                {
                    int v = c.Obra.Azar.Next(3);
                    float e = c.Obra.Rango(0.9f, 1.4f);
                    if (!Libre(c, new Vector3(x, y + 0.3f, z), new Vector3(0.25f * e, 0.15f, 0.25f * e)))
                    {
                        continue;
                    }
                    Pegar(bolsas[v], new[] { c.Bolsas[v] }, Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0f, c.Obra.Rango(0f, 360f), 0f), Vector3.one * e));
                    usadasZ.Add(z - 1f);
                    nBolsas++;
                    c.Basura++;
                }
            }
            Log("Basura en banquetas: " + nPrefabs + " piezas (basureros, tambos, huacales, macetas) y " + nBolsas + " bolsas.");
            // Charcos: en la calle y las banquetas (decal que refleja; no estorban el paso).
            int charcos = 0;
            Transform charcosPadre = Hijo(calle, "Charcos");
            for (int i = 0; i < 120 && charcos < 22; i++)
            {
                // 12 en la calle y 10 en las banquetas y el paseo (donde reflejan los neones), la mitad dentro del cuadro de K13.
                bool acera = charcos >= 12;
                float z = i % 2 == 0 ? c.Obra.Rango(62f, 140f) : c.Obra.Rango(ZVistaSur, ZVistaNorte);
                float x = acera ? (c.Obra.Chance(0.4f) ? XCentroCalle - 5.0f : (c.Obra.Chance(0.5f) ? XCentroCalle + 5.0f : XCentroCalle + 7f)) : XCentroCalle + c.Obra.Rango(-4.4f, 4.4f);
                float y;
                if (!Suelo(c, x, z, YCalle(z), out y) || Mathf.Abs(y - YCalle(z)) > (acera ? 0.6f : 0.3f))
                {
                    continue;
                }
                float largo = acera ? c.Obra.Rango(0.8f, 1.5f) : c.Obra.Rango(1.4f, 3.2f), ancho = acera ? c.Obra.Rango(0.6f, 1.1f) : c.Obra.Rango(1.0f, 2.2f);
                KitS2c.Charco(c.Obra, charcosPadre, new Vector3(x, y, z), largo, ancho, c.Obra.Rango(0f, 360f));
                charcos++;
            }
            c.Charcos = charcos;
            Log("Charcos: " + charcos + ".");
        }

        // ================================================================ Gente

        private static void PonerGente(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Gente");
            List<string> disponibles = Gente.Where(n => AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(n)) != null).ToList();
            if (disponibles.Count == 0)
            {
                Log("FALTAN los NPCs NP07..NP14 en Assets/Arte/Personajes; no hay gente.");
                faltan.Add("NP07..NP14 (fbx en Assets/Arte/Personajes)");
                return;
            }
            int total = 0;
            List<Vector3> puestos = new List<Vector3>();
            // Banquetas (oeste y este de la calle, entre las zonas visibles de K13).
            // 2 personas a 8–12 m del lente, en la banqueta bajo una lámpara o un neón de fachada, mirando hacia la cámara, para que se lean.
            int bajoLuz = 0;
            foreach ((Vector3 luz, float normal) in c.LucesFachada.OrderBy(q => Mathf.Abs(Vector3.Distance(new Vector3(q.pos.x, K13Desde.y, q.pos.z), K13Desde) - 10f)))
            {
                if (bajoLuz >= 2)
                {
                    break;
                }
                float xp = luz.x + normal * 1.0f;
                float zp = luz.z + (bajoLuz == 0 ? 0.8f : -0.8f);
                float yRefL = YCalle(zp);
                float yp;
                if (!EnCuadroPunto(new Vector3(xp, yRefL + 0.9f, zp), 8f, 12f) || !Suelo(c, xp, zp, yRefL, out yp) || Mathf.Abs(yp - yRefL) > 0.6f)
                {
                    continue;
                }
                Vector3 pl = new Vector3(xp, yp, zp);
                if (EnLibre(c, pl, 0.6f) || puestos.Any(q => (q - pl).magnitude < 3f) || !Libre(c, pl + Vector3.up * 0.95f, new Vector3(0.3f, 0.8f, 0.3f)))
                {
                    continue;
                }
                if (PonerNpc(c, padre, disponibles[c.Obra.Azar.Next(disponibles.Count)], pl, c.Obra.Rango(-20f, 20f)))
                {
                    puestos.Add(pl);
                    total++;
                    bajoLuz++;
                    Log("  NPC bajo una luz de fachada en " + pl.ToString("F1") + " (" + Vector3.Distance(pl, K13Desde).ToString("0.0", CultureInfo.InvariantCulture) + " m del lente).");
                }
            }
            if (bajoLuz < 2)
            {
                Log("AVISO: solo " + bajoLuz + " personas bajo una luz a 8–12 m del lente (" + c.LucesFachada.Count + " luces de fachada).");
            }
            float[] columnas = { XCentroCalle - 5.2f, XCentroCalle + 5.2f, XCentroCalle + 7f, XCentroCalle - 4.0f, XCentroCalle + 4.0f };
            int rechazos = 0;
            Action<string> rechazo = motivo =>
            {
                if (rechazos++ < 60)
                {
                    Log("  NPC rechazado: " + motivo);
                }
            };
            for (int i = 0; i < 260 && total < 11; i++)
            {
                // Los primeros 4 dentro del cuadro de K13 (de 7 a 28 m del lente; la cámara está en z = 152): banquetas, paseo y borde de la calle junto a los carros.
                bool enCuadro = total < 4;
                float z = enCuadro ? c.Obra.Rango(124f, 146f) : c.Obra.Rango(ZVistaSur, ZVistaNorte - 4f);
                float x = columnas[c.Obra.Azar.Next(columnas.Length)];
                string donde = "(x " + x.ToString("0.0", CultureInfo.InvariantCulture) + ", z " + z.ToString("0.0", CultureInfo.InvariantCulture) + ")";
                float yRef = YCalle(z);
                if (enCuadro && !EnCuadroK13(new Vector3(x, yRef, z)))
                {
                    rechazo(donde + " fuera del cuadro de K13");
                    continue;
                }
                float y;
                if (!Suelo(c, x, z, yRef, out y))
                {
                    rechazo(donde + " sin piso medible (rayo desde y " + (yRef + 2.5f).ToString("0.0", CultureInfo.InvariantCulture) + ")");
                    continue;
                }
                if (Mathf.Abs(y - yRef) > 0.6f)
                {
                    rechazo(donde + " piso a " + y.ToString("0.00", CultureInfo.InvariantCulture) + " y la banqueta medida a " + yRef.ToString("0.00", CultureInfo.InvariantCulture));
                    continue;
                }
                Vector3 p = new Vector3(x, y, z);
                if (EnLibre(c, p, 0.6f))
                {
                    rechazo(donde + " dentro del cuadro libre de una escalera o un acceso");
                    continue;
                }
                if (puestos.Any(q => (q - p).magnitude < (enCuadro ? 3.5f : 5f)))
                {
                    rechazo(donde + " muy cerca de otra persona");
                    continue;
                }
                if (!Libre(c, p + Vector3.up * 0.95f, new Vector3(0.3f, 0.8f, 0.3f)))
                {
                    rechazo(donde + " hay un collider (carro, poste, barandal u otra cosa)");
                    continue;
                }
                if (PonerNpc(c, padre, disponibles[c.Obra.Azar.Next(disponibles.Count)], p, x < XCentroCalle ? 90f : -90f + c.Obra.Rango(-40f, 40f)))
                {
                    puestos.Add(p);
                    total++;
                    if (enCuadro)
                    {
                        Log("  NPC dentro del cuadro de K13 en " + donde + " y " + y.ToString("0.00", CultureInfo.InvariantCulture) + ".");
                    }
                }
                else
                {
                    rechazo(donde + " no se pudo crear el fbx");
                }
            }
            int enCalle = total;
            // Azoteas: cerca de los accesos o a un lado del pasillo, donde hay hueco.
            int techos = 0;
            foreach (Techo t in c.Techos.Where(q => q.Enlaces.Count > 0).OrderBy(q => c.Obra.Azar.Next()))
            {
                if (techos >= 3)
                {
                    break;
                }
                Vector2 pos;
                if (t.Grupo == null)
                {
                    t.Grupo = Hijo(c.Nuevo.Find("Techos") ?? c.Nuevo, "Azotea_N" + techos);
                }
                if (!BuscarHueco(c, t, 0.4f, 0.4f, 1.8f, Candidatos(c, t.H, 18), true, out pos))
                {
                    continue;
                }
                Vector3 p = new Vector3(pos.x, t.Y, pos.y);
                if (PonerNpc(c, t.Grupo, disponibles[c.Obra.Azar.Next(disponibles.Count)], p, c.Obra.Rango(0f, 360f)))
                {
                    c.Ocupado.Add(new Bounds(p + Vector3.up * 0.9f, new Vector3(0.8f, 1.8f, 0.8f)));
                    techos++;
                    total++;
                }
            }
            c.Gente = total;
            Log("Gente de adorno: " + total + " (banquetas " + enCalle + ", azoteas " + techos + "). Todos parados en su reposo: el Animator no tiene animación de sentarse.");
        }

        /// <summary>true si el punto (pies de una persona) cae dentro del cuadro de la foto K13 (campo 66, 1376x768) a 6–30 m del lente, con un 10 % de margen interior.</summary>
        private static bool EnCuadroK13(Vector3 p)
        {
            return EnCuadroPunto(p + Vector3.up * 0.9f, 7f, 28f);
        }

        /// <summary>true si el punto cae dentro del cuadro de K13 a una profundidad entre min y max m del lente (10 % de margen interior).</summary>
        private static bool EnCuadroPunto(Vector3 punto, float min, float max)
        {
            Vector3 adelante = (K13Hacia - K13Desde).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
            Vector3 arriba = Vector3.Cross(adelante, derecha);
            Vector3 d = punto - K13Desde;
            float prof = Vector3.Dot(d, adelante);
            if (prof < min || prof > max)
            {
                return false;
            }
            float tanV = Mathf.Tan(CampoK13 * 0.5f * Mathf.Deg2Rad) * 0.9f;
            float tanH = tanV * (1376f / 768f);
            return Mathf.Abs(Vector3.Dot(d, derecha) / prof) < tanH && Mathf.Abs(Vector3.Dot(d, arriba) / prof) < tanV;
        }

        private static string RutaFbx(string nombre)
        {
            string codigo = nombre.Substring(0, nombre.LastIndexOf('_'));
            return "Assets/Arte/Personajes/" + codigo + "/" + nombre + ".fbx";
        }

        /// <summary>Los fbx de NP salen blancos en Play si no traen su material: se asigna NP*_Base.mat de su carpeta Materiales (o una copia con la textura base si el material no la trae) a toda ranura vacía o sin textura.</summary>
        private static void ArreglarBlanco(GameObject go, string nombre)
        {
            string carpeta = "Assets/Arte/Personajes/" + nombre.Substring(0, nombre.LastIndexOf('_'));
            Material baseMat = AssetDatabase.LoadAssetAtPath<Material>(carpeta + "/Materiales/" + nombre + "_Base.mat");
            if (baseMat == null)
            {
                Log("AVISO: no existe " + carpeta + "/Materiales/" + nombre + "_Base.mat; " + nombre + " puede salir blanco.");
                faltan.Add(carpeta + "/Materiales/" + nombre + "_Base.mat");
                return;
            }
            Material usar = baseMat;
            string prop = baseMat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            if (baseMat.GetTexture(prop) == null)
            {
                string[] rutas = AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta + "/Texturas" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
                string elegida = rutas.FirstOrDefault(r => Path.GetFileName(r).ToLowerInvariant().Contains("base") && r.Contains(nombre))
                    ?? rutas.FirstOrDefault(r => r.Contains(nombre) && !Path.GetFileName(r).ToLowerInvariant().Contains("normal")) ?? rutas.FirstOrDefault();
                Texture2D tex = elegida == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(elegida);
                if (tex != null)
                {
                    CarpetaMat();
                    string rutaCopia = CarpetaMateriales + "/T1441_" + nombre + "_Base.mat";
                    Material copia = AssetDatabase.LoadAssetAtPath<Material>(rutaCopia);
                    if (copia == null)
                    {
                        copia = new Material(baseMat) { name = "T1441_" + nombre + "_Base" };
                        AssetDatabase.CreateAsset(copia, rutaCopia);
                    }
                    copia.SetTexture(prop, tex);
                    EditorUtility.SetDirty(copia);
                    usar = copia;
                }
                else
                {
                    Log("AVISO: " + nombre + " no tiene textura base en " + carpeta + "/Texturas; queda con el color de su material.");
                }
            }
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null || !ms[i].HasProperty("_BaseMap") || ms[i].GetTexture("_BaseMap") == null)
                    {
                        ms[i] = usar;
                    }
                }
                r.sharedMaterials = ms;
            }
        }

        private static bool PonerNpc(Ctx c, Transform padre, string nombre, Vector3 pos, float yaw, bool muestrear = true)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(nombre));
            if (fbx == null)
            {
                return false;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(fbx, padre);
            g.name = "NPC_" + nombre + "_" + (c.Contador++);
            Vector3 escalaNpc = g.transform.localScale;
            ArreglarBlanco(g, nombre);
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaFbx(nombre).Replace(".fbx", ".controller"));
            Animator an = g.GetComponent<Animator>();
            if (an == null)
            {
                an = g.GetComponentInChildren<Animator>();
            }
            if (an == null)
            {
                an = g.AddComponent<Animator>();
            }
            if (ctrl != null)
            {
                an.runtimeAnimatorController = ctrl;
                try
                {
                    AnimationClip clip = null;
                    AnimatorController ac = ctrl as AnimatorController;
                    if (ac != null && ac.layers.Length > 0 && ac.layers[0].stateMachine.defaultState != null)
                    {
                        clip = ac.layers[0].stateMachine.defaultState.motion as AnimationClip;
                    }
                    if (clip == null && ctrl.animationClips.Length > 0)
                    {
                        clip = ctrl.animationClips[0];
                    }
                    if (clip != null && muestrear)
                    {
                        clip.SampleAnimation(g, Mathf.Min(0.3f, clip.length * 0.5f));
                        // SampleAnimation aplica también la posición y rotación raíz del clip (los dejaba en el origen): se devuelve la colocación.
                        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                        g.transform.localScale = escalaNpc;
                    }
                }
                catch (Exception e)
                {
                    Log("NPC " + nombre + ": no se pudo muestrear el reposo (" + e.Message + ").");
                }
            }
            else
            {
                Log("NPC " + nombre + ": sin .controller.");
            }
            an.applyRootMotion = false;
            // Asentar por el hueso más bajo de la pose (los bounds del fbx no sirven, ver altura animada).
            float bajo = float.MaxValue, alto = float.MinValue;
            foreach (Transform t in g.GetComponentsInChildren<Transform>())
            {
                bajo = Mathf.Min(bajo, t.position.y);
                alto = Mathf.Max(alto, t.position.y);
            }
            float medida = alto - bajo;
            if (float.IsNaN(medida) || medida < 0.9f || medida > 2.4f)
            {
                Log("AVISO: " + g.name + " mide " + medida.ToString("0.00", CultureInfo.InvariantCulture) + " m entre huesos" + (muestrear ? "; se rehace sin muestrear la pose." : "."));
                if (muestrear)
                {
                    Object.DestroyImmediate(g);
                    return PonerNpc(c, padre, nombre, pos, yaw, false);
                }
            }
            g.transform.position += Vector3.up * (pos.y + 0.03f - bajo);
            // Que se dibuje siempre: la malla con huesos pierde su caja cuando el Animator no corre.
            g.SetActive(true);
            foreach (SkinnedMeshRenderer sr in g.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                sr.updateWhenOffscreen = true;
                sr.enabled = true;
            }
            if (new Vector2(g.transform.position.x - pos.x, g.transform.position.z - pos.z).magnitude > 0.5f)
            {
                g.transform.position = new Vector3(pos.x, g.transform.position.y, pos.z);
                Log("AVISO: " + g.name + " se corrió al muestrear; se devolvió a su lugar.");
            }
            planNpc[g.name] = pos;
            return true;
        }

        // ================================================================ Cables

        private static bool CableOk(Ctx c, Vector3 a, Vector3 b, float flecha, int sinProbarFinal)
        {
            Vector3[] p = CalculoCatenaria.Puntos(a, b, flecha, 14);
            for (int i = 0; i < p.Length; i++)
            {
                if (Vector3.Distance(p[i], K13Desde) < DistanciaCable)
                {
                    return false;
                }
                if (i < sinProbarFinal || i >= p.Length - sinProbarFinal)
                {
                    continue;
                }
                int n = c.Fisica.OverlapBox(p[i], new Vector3(0.12f, 0.12f, 0.12f), bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++)
                {
                    if (!EsPiso(c, bufCol[k].transform))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static int Racimo(Ctx c, Vector3 a, Vector3 b, int hilos, float flecha, int sinProbarFinal)
        {
            int puestos = 0;
            Vector3 lado = Vector3.Cross((b - a).normalized, Vector3.up).normalized;
            for (int i = 0; i < hilos; i++)
            {
                float desv = (i - (hilos - 1) * 0.5f) * 0.22f;
                Vector3 a2 = a + lado * desv + Vector3.down * (0.32f * i);
                Vector3 b2 = b + lado * desv + Vector3.down * (0.32f * i + c.Obra.Rango(-0.1f, 0.1f));
                float f = flecha * (1f + 0.18f * i);
                if (CableOk(c, a2, b2, f, sinProbarFinal))
                {
                    KitS2c.Cable(c.Obra, a2, b2, f);
                    puestos++;
                }
            }
            return puestos;
        }

        private static void Cableado(Ctx c)
        {
            int porPostes = 0, aFachada = 0, cruzados = 0;
            // 1. De poste a poste: cada poste con sus 2 vecinos más cercanos a menos de 42 m (3 hilos).
            HashSet<long> hechos = new HashSet<long>();
            for (int i = 0; i < c.Postes.Count; i++)
            {
                List<int> vecinos = Enumerable.Range(0, c.Postes.Count).Where(j => j != i && (c.Postes[j] - c.Postes[i]).magnitude < 42f && (c.Postes[j] - c.Postes[i]).magnitude > 4f)
                    .OrderBy(j => (c.Postes[j] - c.Postes[i]).sqrMagnitude).Take(2).ToList();
                foreach (int j in vecinos)
                {
                    long clave = (long)Mathf.Min(i, j) * 100000 + Mathf.Max(i, j);
                    if (!hechos.Add(clave))
                    {
                        continue;
                    }
                    float largo = (c.Postes[j] - c.Postes[i]).magnitude;
                    porPostes += Racimo(c, c.Postes[i] + Vector3.down * 0.15f, c.Postes[j] + Vector3.down * 0.15f, 3, 0.3f + 0.02f * largo, 1);
                }
            }
            // 2. De cada poste a las 2 fachadas más cercanas (a menos de 16 m).
            foreach (Vector3 poste in c.Postes)
            {
                List<VariarCerroT1364.CasaT> cerca = c.Casas.Where(h => h.Kit && PlanoADistancia(h, poste) < 16f && PlanoADistancia(h, poste) > 2.5f)
                    .OrderBy(h => PlanoADistancia(h, poste)).Take(2).ToList();
                foreach (VariarCerroT1364.CasaT h in cerca)
                {
                    float xf = (h.Normal > 0f ? h.X1 : h.X0) + h.Normal * 0.06f;
                    float z = Mathf.Clamp(poste.z + c.Obra.Rango(-2f, 2f), h.Z0 + 0.4f, h.Z1 - 0.4f);
                    float y = Mathf.Clamp(Mathf.Min(h.Top - 0.7f, poste.y - 0.9f), h.Base + 3.2f, poste.y + 0.5f);
                    Vector3 pared = new Vector3(xf, y, z);
                    float largo = (pared - poste).magnitude;
                    if (Mathf.Abs(pared.y - poste.y) > 4.5f)
                    {
                        continue;
                    }
                    aFachada += Racimo(c, poste + Vector3.down * 0.5f, pared, 2, 0.25f + 0.02f * largo, 2);
                }
            }
            // 3. De fachada a fachada cruzando la calle (arriba, a más de 5 m del piso).
            List<VariarCerroT1364.CasaT> oeste = c.Casas.Where(h => h.Normal > 0f && Mathf.Abs(h.Frente - (XCentroCalle - 6.5f)) < 3.5f).OrderBy(h => h.Z0).ToList();
            List<VariarCerroT1364.CasaT> este = c.Casas.Where(h => h.Normal < 0f && Mathf.Abs(h.Frente - (XCentroCalle + 6.5f)) < 3.5f).ToList();
            foreach (VariarCerroT1364.CasaT w in oeste)
            {
                if (cruzados >= 40)
                {
                    break;
                }
                VariarCerroT1364.CasaT e = este.OrderBy(h => Mathf.Abs((h.Z0 + h.Z1) * 0.5f - (w.Z0 + w.Z1) * 0.5f)).FirstOrDefault();
                if (e == null || Mathf.Abs((e.Z0 + e.Z1) * 0.5f - (w.Z0 + w.Z1) * 0.5f) > 4f || !c.Obra.Chance(0.55f))
                {
                    continue;
                }
                float z = (w.Z0 + w.Z1) * 0.5f;
                float y = Mathf.Min(w.Top, e.Top) - c.Obra.Rango(0.6f, 1.6f);
                Vector3 a = new Vector3(w.X1 + 0.06f, y, z + c.Obra.Rango(-1f, 1f));
                Vector3 b = new Vector3(e.X0 - 0.06f, y + c.Obra.Rango(-0.4f, 0.4f), z + c.Obra.Rango(-1f, 1f));
                float suelo;
                if (!Suelo(c, XCentroCalle, z, YCalle(z), out suelo) || y - suelo < 5f)
                {
                    continue;
                }
                cruzados += Racimo(c, a, b, 2, 0.35f + 0.02f * (b - a).magnitude, 2);
            }
            c.Cables = porPostes + aFachada + cruzados;
            Log("Cables: de poste a poste " + porPostes + ", de poste a fachada " + aFachada + ", cruzando la calle " + cruzados + " (en racimos; ninguno a menos de " + DistanciaCable + " m de K13).");
        }

        private static float PlanoADistancia(VariarCerroT1364.CasaT h, Vector3 p)
        {
            float dx = Mathf.Max(h.X0 - p.x, 0f, p.x - h.X1);
            float dz = Mathf.Max(h.Z0 - p.z, 0f, p.z - h.Z1);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ================================================================ Poste con transformador sobre el paseo (izquierda de K13)

        private static void PostesPaseo(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Postes");
            int n = 0;
            foreach (float zq in new[] { 130f, 106f, 80f })
            {
                if (n >= 2)
                {
                    break;
                }
                float y;
                float x = ZonaCerroT1416.XPaseo0 + 0.7f;
                if (!Suelo(c, x, zq, YCalle(zq), out y) || Mathf.Abs(y - YCalle(zq)) > 0.6f)
                {
                    Log("Poste del paseo en z = " + zq + ": sin piso; no se pone.");
                    continue;
                }
                Vector3 pie = new Vector3(x, y, zq);
                if (EnLibre(c, pie, 1f) || !Libre(c, pie + Vector3.up * 2f, new Vector3(0.45f, 2f, 0.45f)) || c.Postes.Any(p => new Vector2(p.x - x, p.z - zq).magnitude < 8f))
                {
                    Log("Poste del paseo en z = " + zq + ": hay algo o ya hay un poste a menos de 8 m; no se pone.");
                    continue;
                }
                Vector3 punta = KitS2c.Poste(c.Obra, padre, pie, Vector3.left, pie.y, n == 0);
                c.Postes.Add(punta);
                Physics.SyncTransforms();
                n++;
            }
            Log("Postes nuevos sobre el paseo (el primero con transformador): " + n + ".");
        }

        // ================================================================ Ropa de tamaño real (cuelga de y = 0 hacia abajo, en el plano XY, dos caras)

        private static void Doble(Malla m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            m.Quad(0, a, b, c, d, Vector3.forward);
            m.Quad(0, a, b, c, d, Vector3.back);
        }

        /// <summary>Camisa (0.6 x 0.7 m con mangas), pantalón (0.4 x 0.9 m con dos piernas) y toalla (0.5 x 0.8 m).</summary>
        private static Mesh[] MallasRopa(Ctx c)
        {
            Malla camisa = new Malla(1);
            Doble(camisa, new Vector3(-0.2f, 0f, 0f), new Vector3(0.2f, 0f, 0f), new Vector3(0.2f, -0.7f, 0f), new Vector3(-0.2f, -0.7f, 0f));
            Doble(camisa, new Vector3(-0.2f, 0f, 0f), new Vector3(-0.3f, -0.06f, 0f), new Vector3(-0.3f, -0.25f, 0f), new Vector3(-0.2f, -0.2f, 0f));
            Doble(camisa, new Vector3(0.2f, 0f, 0f), new Vector3(0.3f, -0.06f, 0f), new Vector3(0.3f, -0.25f, 0f), new Vector3(0.2f, -0.2f, 0f));
            Malla pantalon = new Malla(1);
            Doble(pantalon, new Vector3(-0.2f, 0f, 0f), new Vector3(0.2f, 0f, 0f), new Vector3(0.2f, -0.2f, 0f), new Vector3(-0.2f, -0.2f, 0f));
            Doble(pantalon, new Vector3(-0.2f, -0.2f, 0f), new Vector3(-0.01f, -0.2f, 0f), new Vector3(-0.03f, -0.9f, 0f), new Vector3(-0.18f, -0.9f, 0f));
            Doble(pantalon, new Vector3(0.01f, -0.2f, 0f), new Vector3(0.2f, -0.2f, 0f), new Vector3(0.18f, -0.9f, 0f), new Vector3(0.03f, -0.9f, 0f));
            Malla toalla = new Malla(1);
            Doble(toalla, new Vector3(-0.25f, 0f, 0f), new Vector3(0.25f, 0f, 0f), new Vector3(0.25f, -0.8f, 0f), new Vector3(-0.25f, -0.8f, 0f));
            Mesh[] r = { camisa.Crear("T1441_Camisa"), pantalon.Crear("T1441_Pantalon"), toalla.Crear("T1441_Toalla") };
            c.Obra.Mallas.AddRange(r);
            return r;
        }

        // ================================================================ Tendederos a la vista de K13 (casas de la derecha a postes de la izquierda)

        private static void TendederosK13(Ctx c)
        {
            Material[] ropa =
            {
                MatLit("RopaRosa", new Color(0.9f, 0.3f, 0.6f), 0.1f), MatLit("RopaAzul", new Color(0.2f, 0.45f, 0.85f), 0.1f), MatLit("RopaAmarilla", new Color(0.95f, 0.8f, 0.2f), 0.1f),
                MatLit("RopaBlanca", new Color(0.85f, 0.85f, 0.82f), 0.1f), MatLit("RopaVerde", new Color(0.3f, 0.7f, 0.45f), 0.1f), MatLit("RopaNaranja", new Color(0.95f, 0.5f, 0.15f), 0.1f),
            };
            Mesh[] mallasRopa = MallasRopa(c);
            List<VariarCerroT1364.CasaT> oeste = c.Casas.Where(h => h.Normal > 0f && Mathf.Abs(h.Frente - (XCentroCalle - 6.5f)) < 3.5f).ToList();
            List<Vector3> postes = c.Postes.Where(q => q.x > XCentroCalle + 2f && q.z > 40f && q.z < 150f).OrderBy(q => Mathf.Abs(q.z - 110f)).ToList();
            Log("Tendederos K13: " + oeste.Count + " casas del oeste y " + postes.Count + " postes del lado del paseo (casas con la fachada hacia el este: " + c.Casas.Count(h => h.Normal > 0f)
                + "; planos de fachada entre " + (c.Casas.Count(h => h.Normal > 0f) == 0 ? 0f : c.Casas.Where(h => h.Normal > 0f).Min(h => h.Frente)).ToString("0.0", CultureInfo.InvariantCulture) + " y "
                + (c.Casas.Count(h => h.Normal > 0f) == 0 ? 0f : c.Casas.Where(h => h.Normal > 0f).Max(h => h.Frente)).ToString("0.0", CultureInfo.InvariantCulture) + ").");
            HashSet<VariarCerroT1364.CasaT> usadas = new HashSet<VariarCerroT1364.CasaT>();
            int lineas = 0, prendas = 0;
            foreach (Vector3 poste in postes)
            {
                foreach (float off in new[] { 0f, -5f, 5f, -9f, 9f })
                {
                    if (lineas >= 3)
                    {
                        break;
                    }
                    float zt = poste.z + off;
                    VariarCerroT1364.CasaT w = oeste.Where(h => !usadas.Contains(h)).OrderBy(h => Mathf.Abs((h.Z0 + h.Z1) * 0.5f - zt)).FirstOrDefault();
                    if (w == null || Mathf.Abs((w.Z0 + w.Z1) * 0.5f - zt) > 4f)
                    {
                        continue;
                    }
                    float zc = (w.Z0 + w.Z1) * 0.5f;
                    float suelo;
                    if (!Suelo(c, XCentroCalle, zc, YCalle(zc), out suelo))
                    {
                        continue;
                    }
                    float y = Mathf.Min(suelo + c.Obra.Rango(4.0f, 5.0f), w.Top - 0.6f);
                    if (y < suelo + 3.8f)
                    {
                        Log("  tendedero K13 en z " + zc.ToString("0", CultureInfo.InvariantCulture) + ": la casa es muy baja (" + (w.Top - suelo).ToString("0.0", CultureInfo.InvariantCulture) + " m sobre la calle).");
                        continue;
                    }
                    float za = zc + c.Obra.Rango(-0.8f, 0.8f);
                    Vector3 pw;
                    if (!Pared(c, w, y, za, out pw))
                    {
                        pw = new Vector3(w.Frente, y, za);
                        Log("  tendedero K13 en z " + zc.ToString("0", CultureInfo.InvariantCulture) + ": el rayo horizontal no halló pared; se usa el plano de la fachada x = " + w.Frente.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                    }
                    Vector3 a = new Vector3(pw.x + w.Normal * 0.06f, y, za);
                    Vector3 b = new Vector3(poste.x - 0.1f, Mathf.Min(poste.y - 0.6f, y + c.Obra.Rango(-0.3f, 0.4f)), poste.z);
                    const float flecha = 0.45f;
                    Vector3 medio = (a + b) * 0.5f + Vector3.down * 0.3f;
                    if (!EnCuadroPunto(medio, 8f, 45f))
                    {
                        Log("  tendedero K13 en z " + zc.ToString("0", CultureInfo.InvariantCulture) + ": su punto medio cae fuera del cuadro.");
                        continue;
                    }
                    if (!CableOk(c, a, b, flecha, 2))
                    {
                        Log("  tendedero K13 en z " + zc.ToString("0", CultureInfo.InvariantCulture) + ": el cable choca con algo o pasa a menos de " + DistanciaCable + " m de la cámara.");
                        continue;
                    }
                    KitS2c.Cable(c.Obra, a, b, flecha, 0.012f);
                    int n = c.Obra.Azar.Next(6, 9);
                    for (int i = 0; i < n; i++)
                    {
                        float t = (i + 0.7f) / (n + 0.4f);
                        Vector3 p = CalculoCatenaria.Punto(a, b, flecha, t);
                        Quaternion giro = Quaternion.Euler(0f, c.Obra.Rango(-12f, 12f), c.Obra.Rango(-6f, 6f));
                        Pegar(mallasRopa[c.Obra.Azar.Next(mallasRopa.Length)], new[] { ropa[c.Obra.Azar.Next(ropa.Length)] }, Matrix4x4.TRS(p, giro, Vector3.one * c.Obra.Rango(0.9f, 1.1f)));
                        prendas++;
                    }
                    usadas.Add(w);
                    lineas++;
                    Log("  tendedero K13 " + lineas + ": de la fachada (" + a.x.ToString("0.0", CultureInfo.InvariantCulture) + ", " + a.y.ToString("0.0", CultureInfo.InvariantCulture) + ", " + a.z.ToString("0.0", CultureInfo.InvariantCulture)
                        + ") al poste en z " + poste.z.ToString("0", CultureInfo.InvariantCulture) + ", " + (a.y - suelo).ToString("0.0", CultureInfo.InvariantCulture) + " m sobre la calle.");
                }
            }
            Log("Tendederos a la vista de K13: " + lineas + " (" + prendas + " prendas).");
        }

        // ================================================================ Tendederos con ropa entre casas

        private static void Tendederos(Ctx c)
        {
            Material[] ropa =
            {
                MatLit("RopaRosa", new Color(0.9f, 0.3f, 0.6f), 0.1f), MatLit("RopaAzul", new Color(0.2f, 0.45f, 0.85f), 0.1f), MatLit("RopaAmarilla", new Color(0.95f, 0.8f, 0.2f), 0.1f),
                MatLit("RopaBlanca", new Color(0.85f, 0.85f, 0.82f), 0.1f), MatLit("RopaVerde", new Color(0.3f, 0.7f, 0.45f), 0.1f), MatLit("RopaNaranja", new Color(0.95f, 0.5f, 0.15f), 0.1f),
            };
            Mesh[] mallasRopa = MallasRopa(c);
            List<VariarCerroT1364.CasaT> oeste = c.Casas.Where(h => h.Normal > 0f && Mathf.Abs(h.Frente - (XCentroCalle - 6.5f)) < 3.5f && (h.Z0 + h.Z1) * 0.5f > ZVistaSur && (h.Z0 + h.Z1) * 0.5f < ZVistaNorte).OrderBy(h => Mathf.Abs((h.Z0 + h.Z1) * 0.5f - 110f)).ToList();
            List<VariarCerroT1364.CasaT> este = c.Casas.Where(h => h.Normal < 0f && Mathf.Abs(h.Frente - (XCentroCalle + 6.5f)) < 3.5f).ToList();
            int lineas = 0, prendas = 0;
            foreach (VariarCerroT1364.CasaT w in oeste)
            {
                if (lineas >= 14)
                {
                    break;
                }
                float z = (w.Z0 + w.Z1) * 0.5f;
                VariarCerroT1364.CasaT e = este.OrderBy(h => Mathf.Abs((h.Z0 + h.Z1) * 0.5f - z)).FirstOrDefault();
                bool conCasa = e != null && Mathf.Abs((e.Z0 + e.Z1) * 0.5f - z) <= 4f;
                // Si la casa de enfrente está apagada (T1416 abre la vista), el cordel se ata a un poste del lado del paseo.
                Vector3 poste = default;
                bool conPoste = false;
                if (!conCasa)
                {
                    foreach (Vector3 pp in c.Postes.Where(q => q.x > XCentroCalle + 2f && Mathf.Abs(q.z - z) < 10f).OrderBy(q => Mathf.Abs(q.z - z)).Take(1))
                    {
                        poste = pp;
                        conPoste = true;
                    }
                }
                if ((!conCasa && !conPoste) || !c.Obra.Chance(0.75f))
                {
                    continue;
                }
                float suelo;
                if (!Suelo(c, XCentroCalle, z, YCalle(z), out suelo))
                {
                    continue;
                }
                float y = suelo + c.Obra.Rango(3.6f, 4.6f);
                if (y > (conCasa ? Mathf.Min(w.Top, e.Top) : w.Top) - 0.8f)
                {
                    continue;
                }
                Vector3 a = new Vector3(w.X1 + 0.06f, y, z + c.Obra.Rango(-1f, 1f));
                Vector3 b = conCasa ? new Vector3(e.X0 - 0.06f, y + c.Obra.Rango(-0.3f, 0.3f), z + c.Obra.Rango(-1f, 1f)) : new Vector3(poste.x, Mathf.Min(poste.y - 0.8f, y + c.Obra.Rango(-0.3f, 0.3f)), poste.z);
                const float flecha = 0.4f;
                if (!CableOk(c, a, b, flecha, 2))
                {
                    continue;
                }
                KitS2c.Cable(c.Obra, a, b, flecha, 0.012f);
                int n = c.Obra.Azar.Next(5, 8);
                for (int i = 0; i < n; i++)
                {
                    float t = (i + 0.7f) / (n + 0.4f);
                    Vector3 p = CalculoCatenaria.Punto(a, b, flecha, t);
                    Quaternion giro = Quaternion.Euler(0f, c.Obra.Rango(-12f, 12f), c.Obra.Rango(-6f, 6f));
                    Pegar(mallasRopa[c.Obra.Azar.Next(mallasRopa.Length)], new[] { ropa[c.Obra.Azar.Next(ropa.Length)] }, Matrix4x4.TRS(p, giro, Vector3.one * c.Obra.Rango(0.9f, 1.1f)));
                    prendas++;
                }
                lineas++;
            }
            Log("Tendederos con ropa de casa a casa cruzando la calle: " + lineas + " (" + prendas + " prendas).");
        }

        // ================================================================ Estático

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t == c.Nuevo || t.GetComponent<Light>() != null || t.GetComponentInParent<Rigidbody>() != null || t.GetComponentInParent<Animator>() != null)
                {
                    continue;
                }
                bool enCarro = false;
                for (Transform p = t; p != null && p != c.Nuevo; p = p.parent)
                {
                    if (p.name.StartsWith("Carro_"))
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
        /// Revisa el prefab ya con Aplicar y escribe Logs/t1441-validar.txt: objetos y luces nuevas, tamaño; cosas de azotea flotando o enterradas (5 cm), encimadas o en el pasillo de 1.2 m;
        /// carros, basura y gente en el carril de tránsito; carros asentados; cables a más de 4 m de K13; colliders nuevos sin malla visible.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1441/Cerro detalle y vida: validar")]
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1441: validación de " + RutaPrefab + "\n");
            int fallos = 0, avisos = 0;
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Ctx c = new Ctx
                {
                    Zona = zona,
                    Fisica = zona.scene.GetPhysicsScene(),
                    Terreno = zona.transform.Find("Terreno"),
                    Calles = zona.transform.Find("Calles"),
                    Escaleras = zona.transform.Find("Escaleras"),
                    Nuevo = zona.transform.Find(Raiz),
                };
                Physics.SyncTransforms();
                actual = c;
                calleMedida.Clear();
                if (c.Nuevo == null)
                {
                    sb.AppendLine("FALLO: no existe " + Raiz + " (corre Aplicar).");
                    fallos++;
                }
                else
                {
                    int objetos = c.Nuevo.GetComponentsInChildren<Transform>(true).Length - 1;
                    int luces = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                    sb.AppendLine("Objetos nuevos: " + objetos + " (tope " + TopeObjetosNuevos + "). Luces nuevas: " + luces + " (tope " + TopeLucesNuevas + "). Luces del prefab: " + zona.GetComponentsInChildren<Light>(true).Length + ".");
                    if (objetos > TopeObjetosNuevos || luces > TopeLucesNuevas)
                    {
                        sb.AppendLine("FALLO: se pasó de un tope.");
                        fallos++;
                    }
                    sb.AppendLine("Prefab: " + Megas(RutaPrefab) + " MB (tope 60).");
                    if (new FileInfo(Path.Combine(Directory.GetParent(Application.dataPath).FullName, RutaPrefab)).Length > TopeBytesPrefab)
                    {
                        sb.AppendLine("FALLO: el prefab pasa de 60 MB.");
                        fallos++;
                    }
                    ValidarAzoteas(c, sb, ref fallos, ref avisos);
                    ValidarCalle(c, sb, ref fallos, ref avisos);
                    ValidarColliders(c, sb, ref fallos);
                    ValidarCables(c, sb, ref fallos);
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
            sb.AppendLine(fallos == 0 ? "RESULTADO: bien (" + avisos + " avisos)." : "RESULTADO: " + fallos + " FALLOS (" + avisos + " avisos).");
            Escribir(RutaValidar, sb);
            Debug.Log("[ZonaCerroT1441] " + sb);
        }

        private static void ValidarAzoteas(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            Transform techos = c.Nuevo.Find("Techos");
            if (techos == null)
            {
                sb.AppendLine("AVISO: no hay Techos.");
                avisos++;
                return;
            }
            List<VariarCerroT1364.CasaT> casas = VariarCerroT1364.Leer(c.Zona.transform).Where(h => h.Raiz != null && h.Raiz.gameObject.activeInHierarchy).ToList();
            List<Bounds> enlaces = new List<Bounds>();
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (!t.IsChildOf(c.Nuevo) && EsEnlace(t) && !Ancestro(t, c.Zona.transform, EsEnlace))
                {
                    Bounds b = Cubierta(t);
                    if (b.size.sqrMagnitude > 0.01f)
                    {
                        enlaces.Add(b);
                    }
                }
            }
            int cosas = 0, flotan = 0, enterradas = 0, encimadas = 0, enPasillo = 0, grupos = 0;
            List<(Transform t, Bounds b)> todas = new List<(Transform, Bounds)>();
            // Lo combinado en lotes (antenas, parabólicas, varillas, tendederos, etc.) se asienta por construcción (t.Y); aquí quedan los objetos reales (tinacos con collider).
            List<Transform> items = new List<Transform>();
            foreach (Transform t in techos)
            {
                if (t.name.StartsWith("Lod_"))
                {
                    foreach (Transform u in t)
                    {
                        items.Add(u);
                    }
                }
                else
                {
                    items.Add(t);
                }
            }
            foreach (Transform t in items)
            {
                if (t.GetComponentInChildren<Animator>() != null || (t.GetComponentInChildren<Renderer>() == null && t.GetComponent<Collider>() == null))
                {
                    continue;
                }
                grupos++;
                todas.Add((t, Cubierta(t)));
            }
            foreach ((Transform t, Bounds b) in todas)
            {
                cosas++;
                Vector3 origen = new Vector3(b.center.x, b.min.y + 0.4f, b.center.z);
                int n = c.Fisica.Raycast(origen, Vector3.down, bufRayos, 3f, ~0, QueryTriggerInteraction.Ignore);
                float y = float.MinValue;
                for (int i = 0; i < n; i++)
                {
                    if (bufRayos[i].collider.transform.IsChildOf(t) || EsPiso(c, bufRayos[i].collider.transform) || bufRayos[i].normal.y < 0.9f)
                    {
                        continue;
                    }
                    y = Mathf.Max(y, bufRayos[i].point.y);
                }
                if (y == float.MinValue)
                {
                    sb.AppendLine("AVISO: " + Ruta(t) + " sin superficie debajo en el centro (tinaco sobre base hueca o borde).");
                    avisos++;
                }
                else if (b.min.y - y > 0.05f)
                {
                    flotan++;
                    sb.AppendLine("FALLO: flota " + (b.min.y - y).ToString("0.00", CultureInfo.InvariantCulture) + " m: " + Ruta(t));
                }
                else if (y - b.min.y > 0.05f)
                {
                    enterradas++;
                    sb.AppendLine("FALLO: enterrada " + (y - b.min.y).ToString("0.00", CultureInfo.InvariantCulture) + " m: " + Ruta(t));
                }
                foreach (VariarCerroT1364.CasaT h in casas)
                {
                    if (b.center.x < h.X0 || b.center.x > h.X1 || b.center.z < h.Z0 || b.center.z > h.Z1 || b.center.y < h.Base)
                    {
                        continue;
                    }
                    List<Bounds> sus = enlaces.Where(e => SolapaXZ(new Bounds(new Vector3((h.X0 + h.X1) * 0.5f, b.center.y, (h.Z0 + h.Z1) * 0.5f), new Vector3(h.X1 - h.X0, 1f, h.Z1 - h.Z0)), e, 1f)).Take(5).ToList();
                    List<Vector2> puntos = sus.Select(e => new Vector2(Mathf.Clamp(e.center.x, h.X0, h.X1), Mathf.Clamp(e.center.z, h.Z0, h.Z1))).ToList();
                    float radio = Mathf.Max(b.extents.x, b.extents.z);
                    bool malo = sus.Any(e => SolapaXZ(b, e, 1.0f));
                    for (int i = 0; i < puntos.Count && !malo; i++)
                    {
                        for (int j = i + 1; j < puntos.Count && !malo; j++)
                        {
                            malo = Distancia2D(new Vector2(b.center.x, b.center.z), puntos[i], puntos[j]) < AnchoPasillo * 0.5f + radio - 0.1f;
                        }
                    }
                    if (malo)
                    {
                        enPasillo++;
                        sb.AppendLine("FALLO: cierra el pasillo de " + AnchoPasillo + " m: " + Ruta(t));
                    }
                    break;
                }
            }
            for (int i = 0; i < todas.Count; i++)
            {
                for (int j = i + 1; j < todas.Count; j++)
                {
                    if (todas[i].t.parent != todas[j].t.parent)
                    {
                        continue;
                    }
                    string a = todas[i].t.name, b = todas[j].t.name;
                    bool par = (a.StartsWith("Tinaco_") && b.StartsWith("BaseTinaco_")) || (b.StartsWith("Tinaco_") && a.StartsWith("BaseTinaco_"));
                    Bounds bi = todas[i].b, bj = todas[j].b;
                    bi.Expand(-0.1f);
                    bj.Expand(-0.1f);
                    if (!par && bi.Intersects(bj))
                    {
                        encimadas++;
                        sb.AppendLine("FALLO: encimadas " + Ruta(todas[i].t) + " y " + a + " con " + b + ".");
                    }
                }
            }
            sb.AppendLine("Azoteas: " + grupos + " con objetos nuevos, " + cosas + " cosas; flotando " + flotan + ", enterradas " + enterradas + ", encimadas " + encimadas + ", cerrando pasillo " + enPasillo + ".");
            fallos += flotan + enterradas + encimadas + enPasillo;
        }

        private static void ValidarCalle(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            Transform calle = c.Nuevo.Find("Calle");
            int carros = 0, enCarril = 0, flotan = 0;
            if (calle != null)
            {
                Transform padreCarros = calle.Find("Carros");
                if (padreCarros != null)
                {
                    foreach (Transform t in padreCarros)
                    {
                        carros++;
                        Bounds b = Cubierta(t);
                        float yN, yS;
                        float yRef = YCalle(b.center.z);
                        if (Mathf.Abs(b.center.x - XCentroCalle) - b.extents.x < MitadCarril)
                        {
                            enCarril++;
                            sb.AppendLine("FALLO: carro en el carril de tránsito: " + t.name + " x = " + b.center.x.ToString("0.0", CultureInfo.InvariantCulture));
                        }
                        if (Suelo(c, b.center.x, b.center.z + b.extents.z * 0.7f, yRef, out yN) && Suelo(c, b.center.x, b.center.z - b.extents.z * 0.7f, yRef, out yS))
                        {
                            float gap = b.min.y - Mathf.Min(yN, yS);
                            if (Mathf.Abs(gap) > 0.08f)
                            {
                                flotan++;
                                sb.AppendLine("FALLO: carro " + t.name + " a " + gap.ToString("0.00", CultureInfo.InvariantCulture) + " m del piso.");
                            }
                        }
                        else
                        {
                            avisos++;
                            sb.AppendLine("AVISO: no se pudo medir el piso bajo " + t.name + ".");
                        }
                    }
                }
                Transform basura = calle.Find("Basura");
                if (basura != null)
                {
                    // Se miden las piezas, no los contenedores Lod_* (agrupan por cuadro de 40 m y su caja abarca todo).
                    List<Transform> piezasBasura = new List<Transform>();
                    foreach (Transform g in basura)
                    {
                        if (g.name.StartsWith("Lod_"))
                        {
                            foreach (Transform u in g)
                            {
                                piezasBasura.Add(u);
                            }
                        }
                        else
                        {
                            piezasBasura.Add(g);
                        }
                    }
                    foreach (Transform t in piezasBasura)
                    {
                        Bounds b = Cubierta(t);
                        if (Mathf.Abs(b.center.x - XCentroCalle) - b.extents.x < MitadCarril)
                        {
                            enCarril++;
                            sb.AppendLine("FALLO: basura en el carril: " + t.name);
                        }
                        float y;
                        if (Suelo(c, b.center.x, b.center.z, YCalle(b.center.z), out y) && Mathf.Abs(b.min.y - y) > 0.06f)
                        {
                            flotan++;
                            sb.AppendLine("FALLO: " + t.name + " a " + (b.min.y - y).ToString("0.00", CultureInfo.InvariantCulture) + " m del piso.");
                        }
                    }
                }
            }
            Transform gente = c.Nuevo.Find("Gente");
            int personas = 0, enK13 = 0, desplazados = 0;
            // Lo planeado por Aplicar (Logs/t1441-npcs.txt): cada NPC debe estar a menos de 0.5 m (en planta) de donde se puso.
            Dictionary<string, Vector3> plan = new Dictionary<string, Vector3>();
            string rutaPlan = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "t1441-npcs.txt");
            if (File.Exists(rutaPlan))
            {
                foreach (string linea in File.ReadAllLines(rutaPlan))
                {
                    string[] partes = linea.Split('|');
                    if (partes.Length == 4)
                    {
                        plan[partes[0]] = new Vector3(float.Parse(partes[1], CultureInfo.InvariantCulture), float.Parse(partes[2], CultureInfo.InvariantCulture), float.Parse(partes[3], CultureInfo.InvariantCulture));
                    }
                }
            }
            else
            {
                sb.AppendLine("AVISO: no hay Logs/t1441-npcs.txt; no se revisa la posición planeada de los NPC.");
                avisos++;
            }
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("NPC_"))
                {
                    personas++;
                    if (plan.TryGetValue(t.name, out Vector3 planeado) && new Vector2(t.position.x - planeado.x, t.position.z - planeado.z).magnitude > 0.5f)
                    {
                        desplazados++;
                        sb.AppendLine("FALLO: " + t.name + " está en " + t.position.ToString("F2") + " y se planeó en " + planeado.ToString("F2") + ".");
                    }
                    if (t.GetComponentInChildren<Collider>() != null)
                    {
                        fallos++;
                        sb.AppendLine("FALLO: el NPC " + t.name + " tiene collider.");
                    }
                    Vector3 p = t.position;
                    if (EnCuadroK13(p))
                    {
                        enK13++;
                    }
                    if (Mathf.Abs(p.x - XCentroCalle) < MitadCarril && p.y < YCalle(p.z) + 1f)
                    {
                        enCarril++;
                        sb.AppendLine("FALLO: NPC en el carril: " + t.name);
                    }
                }
            }
            sb.AppendLine("Gente dentro del cuadro de K13 (6 a 30 m del lente, campo " + CampoK13 + "): " + enK13 + " (pedidas 3 a 5).");
            if (enK13 < 3)
            {
                sb.AppendLine("AVISO: menos de 3 personas en el cuadro de K13.");
                avisos++;
            }
            if (carros > 5)
            {
                sb.AppendLine("FALLO: máximo 5 carros por rendimiento (hay " + carros + ").");
                fallos++;
            }
            sb.AppendLine("Calle: carros " + carros + ", gente " + personas + " (8 a 20 pedidos);en carril " + enCarril + ", flotando/enterrados " + flotan + ".");
            if (personas < 8 || personas > 20)
            {
                sb.AppendLine("FALLO: la gente debe ser de 8 a 20.");
                fallos++;
            }
            fallos += enCarril + flotan + desplazados;
        }

        private static void ValidarColliders(Ctx c, StringBuilder sb, ref int fallos)
        {
            int sinMalla = 0, total = 0;
            foreach (Collider col in c.Nuevo.GetComponentsInChildren<Collider>(true))
            {
                if (!col.enabled)
                {
                    continue;
                }
                total++;
                Renderer r = col.GetComponent<Renderer>() ?? col.GetComponentInParent<Renderer>();
                // El tinaco lleva un BoxCollider del tamaño de su malla, que va combinada en el lote por material (a petición de rendimiento): no es un muro invisible.
                if (r == null && !col.name.StartsWith("Tinaco_"))
                {
                    bool hijo = col.GetComponentInChildren<Renderer>() != null;
                    if (!hijo)
                    {
                        sinMalla++;
                        sb.AppendLine("FALLO: collider sin malla visible: " + Ruta(col.transform));
                    }
                }
            }
            sb.AppendLine("Colliders nuevos activos: " + total + ", sin malla visible: " + sinMalla + ".");
            fallos += sinMalla;
        }

        private static void ValidarCables(Ctx c, StringBuilder sb, ref int fallos)
        {
            Transform cables = c.Nuevo.Find("Cables");
            if (cables == null || cables.GetComponent<MeshFilter>() == null || cables.GetComponent<MeshFilter>().sharedMesh == null)
            {
                sb.AppendLine("AVISO: no hay malla de cables nuevos.");
                return;
            }
            Mesh m = cables.GetComponent<MeshFilter>().sharedMesh;
            float minimo = float.MaxValue;
            foreach (Vector3 v in m.vertices)
            {
                minimo = Mathf.Min(minimo, Vector3.Distance(cables.TransformPoint(v), K13Desde));
            }
            sb.AppendLine("Cables nuevos: " + m.vertexCount / 4 + " anillos; distancia mínima a la cámara K13: " + minimo.ToString("0.0", CultureInfo.InvariantCulture) + " m (mínimo 4).");
            if (minimo < 4f)
            {
                sb.AppendLine("FALLO: hay cables a menos de 4 m de la cámara K13.");
                fallos++;
            }
        }

        // ================================================================ Fotos

        /// <summary>
        /// Fotos de K13 (cámara de T1416: la ciudad queda a la izquierda al mirar cuesta arriba; la referencia la tiene a la derecha, pero mirando cuesta arriba por la calle sur no hay
        /// otra salida sin espejar el mundo) y de A13 (la marca CamaraA de T1414 si existe) en docs/capturas/_vista19b/. No guarda nada en el prefab ni en la escena.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1441/Cerro detalle y vida: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            Directory.CreateDirectory("Logs");
            Physics.SyncTransforms();
            // K13: los ojos 1.7 m sobre el piso que haya (T-1440 puede haber cambiado la altura de la calle).
            Vector3 desde = K13Desde, hacia = K13Hacia;
            RaycastHit golpe;
            if (Physics.Raycast(new Vector3(desde.x, desde.y + 8f, desde.z), Vector3.down, out golpe, 20f) && Mathf.Abs(golpe.point.y + 1.7f - desde.y) < 3f)
            {
                float dy = golpe.point.y + 1.7f - desde.y;
                desde.y += dy;
                hacia.y += dy;
            }
            DiagnosticoGente(desde, hacia);
            string juego = "Logs/t1441-cerro-k13-juego.png";
            ColocarS2cLook.Foto(desde, hacia, juego, CampoK13);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", juego, CarpetaFotos + "/cerro-k13-t1441.png");
            // A13: la marca de T1414 (CamaraA/Mira bajo T1414_Cerro) o la cámara de T1394.
            Vector3 desdeA = ZonaCerroT1440.A13Desde, haciaA = ZonaCerroT1440.A13Hacia;
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // La marca de T-1440 (CamaraA13 bajo T1440_Ladera) manda; si no está, la de T-1414.
            Transform raiz = todos.FirstOrDefault(t => t.name == ZonaCerroT1440.Raiz);
            Transform marca = raiz != null ? raiz.Find(ZonaCerroT1440.NombreCamara) : null;
            if (marca == null || marca.Find("Mira") == null)
            {
                raiz = todos.FirstOrDefault(t => t.name == "T1414_Cerro");
                marca = raiz != null ? raiz.Find(AereasT1414.NombreCamara) : null;
            }
            Transform mira = marca != null ? (marca.Find("Mira") ?? marca.Find(AereasT1414.NombreMira)) : null;
            if (marca != null && mira != null)
            {
                desdeA = marca.position;
                haciaA = mira.position;
                Debug.Log("[ZonaCerroT1441] A13 con la marca " + Ruta(marca) + ".");
            }
            string juegoA = "Logs/t1441-cerro-a13-juego.png";
            MethodInfo aerea = typeof(AereasT1414).GetMethod("FotoAerea", BindingFlags.NonPublic | BindingFlags.Static);
            if (aerea != null)
            {
                aerea.Invoke(null, new object[] { desdeA, haciaA, juegoA, CampoA13, AereasT1414.DensidadNieblaAerea });
            }
            else
            {
                ColocarS2cLook.Foto(desdeA, haciaA, juegoA, CampoA13);
            }
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", juegoA, CarpetaFotos + "/cerro-a13-t1441.png");
            Debug.Log("[ZonaCerroT1441] Fotos: K13 desde " + desde + " hacia " + hacia + "; A13 desde " + desdeA + " hacia " + haciaA + ".");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ColocarS2.RutaEscena, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        /// <summary>Logs/t1441-fotos.txt: para cada NPC de la escena, si está activo, sus renderers (tipo, enabled, bounds, capa) y dónde cae en pantalla con la cámara de la foto K13.</summary>
        private static void DiagnosticoGente(Vector3 desde, Vector3 hacia)
        {
            StringBuilder sb = new StringBuilder("T-1441: diagnóstico de la gente en la foto K13\nCámara: desde " + desde.ToString("F2") + " hacia " + hacia.ToString("F2") + ", campo " + CampoK13 + ", aspecto 1376/768.\n");
            GameObject go = new GameObject("T1441_CamaraDiagnostico");
            go.hideFlags = HideFlags.HideAndDontSave;
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = CampoK13;
            cam.aspect = 1376f / 768f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            int n = 0, activos = 0, enPantalla = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("NPC_"))
                {
                    continue;
                }
                n++;
                if (t.gameObject.activeInHierarchy)
                {
                    activos++;
                }
                Vector3 vp = cam.WorldToViewportPoint(t.position + Vector3.up * 0.9f);
                bool dentro = vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
                if (dentro)
                {
                    enPantalla++;
                }
                sb.AppendLine(t.name + " en " + t.position.ToString("F2") + " activo=" + t.gameObject.activeInHierarchy + " viewport=" + vp.ToString("F2") + (dentro ? " DENTRO" : " fuera") + " distancia=" + vp.z.ToString("F1"));
                sb.AppendLine("   ruta: " + Ruta(t));
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                {
                    SkinnedMeshRenderer sk = r as SkinnedMeshRenderer;
                    sb.AppendLine("   " + r.GetType().Name + " " + r.name + " enabled=" + r.enabled + " activo=" + r.gameObject.activeInHierarchy + " capa=" + r.gameObject.layer + " bounds centro " + r.bounds.center.ToString("F2") + " tamaño " + r.bounds.size.ToString("F2")
                        + (sk != null ? " malla=" + (sk.sharedMesh != null ? sk.sharedMesh.name : "NULA") + " updateWhenOffscreen=" + sk.updateWhenOffscreen : "") + " materiales=" + r.sharedMaterials.Length);
                }
            }
            sb.AppendLine("NPCs: " + n + ", activos " + activos + ", dentro del cuadro " + enPantalla + ".");
            Object.DestroyImmediate(go);
            Escribir("Logs/t1441-fotos.txt", sb);
            Debug.Log("[ZonaCerroT1441] NPCs " + n + ", activos " + activos + ", dentro del cuadro " + enPantalla + " (detalle en Logs/t1441-fotos.txt).");
        }

        // ================================================================ Ayudas

        private static void Log(string texto)
        {
            if (informe != null)
            {
                informe.AppendLine(texto);
            }
            Debug.Log("[ZonaCerroT1441] " + texto);
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

        private static string Ruta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }
    }
}
