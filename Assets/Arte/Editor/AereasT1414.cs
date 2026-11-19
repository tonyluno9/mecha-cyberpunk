using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1414: vistas aéreas A13 (Cerro), A15 (Puerto) y A18 (Autopista). Para cada zona, sobre su prefab y bajo la raíz propia "T1414_&lt;Zona&gt;" (Aplicar la rehace):
    ///  - busca la cámara aérea (altura, cabeceo y rumbo) que más se parece al encuadre de la referencia: prueba alturas, distancias y rumbos y puntúa lo que entra en cuadro
    ///    (cosas con peso, luces, rayos que dan en algo, nada pegado a la lente). La deja en T1414_&lt;Zona&gt;/CamaraA (con su hijo Mira) y Fotos() la lee de ahí;
    ///  - faroles de sodio en las calles que se ven oscuras desde esa cámara (máximo 18, sin sombras, con LuzNocturna, al borde de la calle, no en el carril);
    ///  - Cerro y Autopista: azoteas con color (losa fina sobre la gris), tinacos, antenas, aires y cuartitos en las esquinas, ventanas encendidas (S2c_Ventana_*) y neones en las
    ///    paredes que miran a la cámara (máximo 14); todo sin collider y con revisión contra lo que ya hay.
    /// Nada de primitivos. Tope de 40 luces nuevas por zona (faroles + neones). No toca la escena ni otros prefabs.
    /// Orden: Aplicar (o AplicarCerro, AplicarPuerto, AplicarAutopista), luego Validar; Fotos y Diagnostico van en una corrida de Unity aparte.
    /// </summary>
    public static class AereasT1414
    {
        public const string RutaInformeAplicar = "Logs/t1414-aplicar.txt";
        public const string RutaInformeValidar = "Logs/t1414-validar.txt";
        public const string RutaInformeTorres = "Logs/t1414-torres.txt";
        public const long TopeBytesPrefab = 60L * 1024 * 1024;
        public const int TopeLucesNuevas = 40;
        public const int TopeObjetosNuevos = 3000;
        public const string NombreCamara = "CamaraA";
        public const string NombreMira = "Mira";
        public const string CarpetaFotos = "docs/capturas/t-1414";
        /// <summary>Densidad de niebla de la foto aérea (el juego usa 0.02, que a 100 m ya no deja ver nada; las referencias aéreas se ven claras hasta ~150 m).</summary>
        public const float DensidadNieblaAerea = 0.008f;
        private const float AnchoFoto = 1376f, AltoFoto = 768f;
        private const float AlturaVentana = 1.15f, AnchoVentana = 0.9f, ProfundoVentana = 0.04f;
        private const float GrosorLosaColor = 0.012f;
        private const float AltoSobreLosa = 0.045f;
        private const int TopeVentanas = 350;
        private const int TopeProps = 320;

        private sealed class Cfg
        {
            public string Nombre, Ruta, Raiz, Referencia, Foto;
            public Vector3 Desde0, Hacia0;
            public float Campo = 62f;
            public int TopeFaroles = 18, TopeNeones = 14, Semilla;
            public bool Edificios;
            public float Alcance = 130f;
            public (string ficha, float peso)[] Fichas;
        }

        private static readonly Cfg[] Zonas =
        {
            new Cfg
            {
                Nombre = "Cerro", Ruta = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab", Raiz = "T1414_Cerro", Referencia = "A13_ColoniaCerro_Aerea", Foto = "cerro-a13",
                Desde0 = new Vector3(-388f, 66f, -28f), Hacia0 = new Vector3(-336f, 9f, 2f), Campo = 60f, Semilla = 14141, Edificios = true, Alcance = 130f,
                Fichas = new[] { ("neon", 3f), ("tinaco", 1.5f), ("antena", 1.5f), ("poste", 2f), ("tablon", 1f), ("enlace", 1f) },
            },
            new Cfg
            {
                Nombre = "Puerto", Ruta = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab", Raiz = "T1414_Puerto", Referencia = "A15_Puerto_Aerea", Foto = "puerto-a15",
                Desde0 = new Vector3(326f, 66f, -60f), Hacia0 = new Vector3(240f, 0f, -205f), Campo = 62f, Semilla = 14142, Edificios = false, Alcance = 150f,
                Fichas = new[] { ("grua", 8f), ("barco", 10f), ("bodega", 4f), ("poste", 2f), ("neon", 3f) },
            },
            new Cfg
            {
                Nombre = "Autopista", Ruta = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab", Raiz = "T1414_Autopista", Referencia = "A18_Autopista_Aerea", Foto = "autopista-a18",
                Desde0 = new Vector3(-205f, 46f, 150f), Hacia0 = new Vector3(-335f, 12f, 238f), Campo = 62f, Semilla = 14143, Edificios = true, Alcance = 140f,
                Fichas = new[] { ("tunel", 10f), ("rampa", 3f), ("columna", 3f), ("tablero", 3f), ("poste", 2f), ("neon", 3f), ("lampara", 2f) },
            },
        };

        private static readonly string[] FichasCarril = { "calle", "asfalto", "tramo", "tablero", "carretera", "cresta", "calzada" };
        private static readonly string[] FichasAcera = { "banqueta", "acera", "guarnicion", "bordillo" };
        private static readonly string[] FichasPaso = { "tablon", "escalera", "escalon", "peldano", "pasarela", "puente", "enlace", "union", "riel" };

        private static readonly RaycastHit[] bufRayos = new RaycastHit[256];
        private static readonly Collider[] bufCol = new Collider[64];
        private static StringBuilder informe;

        private sealed class Rend
        {
            public Renderer R;
            public Bounds B, Obs;
            public string Ruta;
            public float Peso;
            public bool Fino, Plano, Paso;
        }

        private sealed class Edif
        {
            public Transform T;
            public Vector3 C, Der, Fre;
            public float HX, HZ, Techo, Base;
        }

        private sealed class Ctx
        {
            public Cfg F;
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Nuevo;
            public KitS2c.Obra Obra;
            public readonly List<Rend> Rends = new List<Rend>();
            public readonly Dictionary<long, List<Rend>> Rejilla = new Dictionary<long, List<Rend>>();
            public readonly List<Vector3> Lamparas = new List<Vector3>();
            public readonly List<Vector3> Luces = new List<Vector3>();
            public readonly Dictionary<Transform, string> Rutas = new Dictionary<Transform, string>();
            public readonly Plane[] Planos = new Plane[6];
            public Vector3 Desde, Hacia;
            public float TopeY;
            public Mesh CajaUnidad;
            public readonly List<Vector3> Ventanas = new List<Vector3>();
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1414/Aereas: aplicar las tres")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1414: vistas aéreas\n");
            foreach (Cfg f in Zonas)
            {
                AplicarZona(f);
            }
            Escribir(RutaInformeAplicar, informe);
        }

        [MenuItem("Mecha Cyberpunk/T1414/Aereas: aplicar Cerro (A13)")]
        public static void AplicarCerro() { AplicarUna("Cerro"); }

        [MenuItem("Mecha Cyberpunk/T1414/Aereas: aplicar Puerto (A15)")]
        public static void AplicarPuerto() { AplicarUna("Puerto"); }

        [MenuItem("Mecha Cyberpunk/T1414/Aereas: aplicar Autopista (A18)")]
        public static void AplicarAutopista() { AplicarUna("Autopista"); }

        private static void AplicarUna(string nombre)
        {
            informe = new StringBuilder("T-1414: vistas aéreas (" + nombre + ")\n");
            AplicarZona(Zonas.First(z => z.Nombre == nombre));
            Escribir(RutaInformeAplicar.Replace(".txt", "-" + nombre.ToLowerInvariant() + ".txt"), informe);
        }

        private static void AplicarZona(Cfg f)
        {
            Log("");
            Log("== " + f.Nombre + " (" + f.Ruta + ")");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(f.Ruta) == null)
            {
                Log("ERROR: no existe el prefab.");
                return;
            }
            GameObject zona = PrefabUtility.LoadPrefabContents(f.Ruta);
            try
            {
                Transform vieja = zona.transform.Find(f.Raiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                    Log("Raíz anterior " + f.Raiz + " borrada (se rehace).");
                }
                Physics.SyncTransforms();
                Ctx c = Abrir(zona, f);
                int lucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                Log("Luces antes: " + lucesAntes + ". Renderers: " + c.Rends.Count + ". Lámparas de sodio: " + c.Lamparas.Count + ". Tamaño del prefab antes: " + Megas(f.Ruta) + " MB.");
                c.Obra = KitS2c.Empezar(f.Raiz, f.Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                Paso("cámara", () => BuscarCamara(c));
                Paso("faroles", () => Faroles(c));
                if (f.Edificios)
                {
                    Paso("edificios", () => Edificios(c));
                }
                Estatico(c);
                KitS2c.Terminar(c.Obra);
                int nuevas = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                int objetos = c.Nuevo.GetComponentsInChildren<Transform>(true).Length;
                Log("Luces nuevas: " + nuevas + " (tope " + TopeLucesNuevas + "). Objetos nuevos: " + objetos + " (tope " + TopeObjetosNuevos + ").");
                if (nuevas > TopeLucesNuevas)
                {
                    Log("AVISO: se pasó del tope de luces nuevas.");
                }
                Physics.SyncTransforms();
                bool ok;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(zona, f.Ruta, out ok);
                Log(ok && guardado != null ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log("Tamaño del prefab después: " + Megas(f.Ruta) + " MB (tope 60).");
            }
            catch (System.Exception e)
            {
                Log("ERROR: " + e);
                Debug.LogError("[AereasT1414] " + f.Nombre + ": " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
        }

        private static void Paso(string nombre, System.Action accion)
        {
            try
            {
                accion();
            }
            catch (System.Exception e)
            {
                Log("ERROR en el paso '" + nombre + "': " + e);
                Debug.LogError("[AereasT1414] " + nombre + ": " + e);
            }
        }

        // ================================================================ Contexto

        private static Ctx Abrir(GameObject zona, Cfg f)
        {
            Physics.SyncTransforms();
            Ctx c = new Ctx { F = f, Zona = zona, Fisica = zona.scene.GetPhysicsScene() };
            c.Nuevo = zona.transform.Find(f.Raiz);
            float tope = 20f;
            foreach (Renderer r in zona.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || (c.Nuevo != null && r.transform.IsChildOf(c.Nuevo)) || r is ParticleSystemRenderer)
                {
                    continue;
                }
                Bounds b = r.bounds;
                Rend x = new Rend { R = r, B = b, Obs = b, Ruta = RutaDe(c, r.transform) };
                x.Fino = b.size.y < 0.1f || b.size.magnitude < 0.3f;
                x.Plano = b.size.x > 60f || b.size.z > 60f;
                x.Paso = FichasPaso.Any(t => x.Ruta.Contains(t));
                if (x.Paso)
                {
                    x.Obs.Expand(4.4f);
                }
                x.Peso = x.Plano ? 0.1f : x.Fino ? 0.3f : 1f;
                if (!x.Plano)
                {
                    foreach ((string ficha, float peso) in f.Fichas)
                    {
                        if (x.Ruta.Contains(ficha))
                        {
                            x.Peso = Mathf.Max(x.Peso, 1f + peso);
                        }
                    }
                }
                c.Rends.Add(x);
                tope = Mathf.Max(tope, Mathf.Min(b.max.y + 5f, 300f));
                if (!x.Fino && !x.Plano)
                {
                    Insertar(c, x);
                }
            }
            c.TopeY = tope;
            foreach (Light l in zona.GetComponentsInChildren<Light>(false))
            {
                if (l == null || (c.Nuevo != null && l.transform.IsChildOf(c.Nuevo)) || l.type == LightType.Directional)
                {
                    continue;
                }
                c.Luces.Add(l.transform.position);
                if (EsLampara(l))
                {
                    c.Lamparas.Add(l.transform.position);
                }
            }
            return c;
        }

        private static string RutaDe(Ctx c, Transform t)
        {
            string s;
            if (c.Rutas.TryGetValue(t, out s))
            {
                return s;
            }
            StringBuilder sb = new StringBuilder();
            for (Transform p = t; p != null && p != c.Zona.transform; p = p.parent)
            {
                sb.Insert(0, p.name.ToLowerInvariant() + "/");
            }
            s = sb.ToString();
            c.Rutas[t] = s;
            return s;
        }

        private static bool EsLampara(Light l)
        {
            Color k = l.color;
            return l.name.ToLowerInvariant().Contains("sodio") || (l.type == LightType.Spot && k.r >= 0.85f && k.g >= 0.35f && k.g <= 0.8f && k.b <= 0.5f);
        }

        private const float Celda = 16f;

        private static long Llave(int cx, int cz) { return ((long)cx << 32) ^ (uint)cz; }

        private static void Insertar(Ctx c, Rend r)
        {
            int x0 = Mathf.FloorToInt(r.Obs.min.x / Celda), x1 = Mathf.FloorToInt(r.Obs.max.x / Celda);
            int z0 = Mathf.FloorToInt(r.Obs.min.z / Celda), z1 = Mathf.FloorToInt(r.Obs.max.z / Celda);
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    List<Rend> l;
                    long k = Llave(x, z);
                    if (!c.Rejilla.TryGetValue(k, out l))
                    {
                        l = new List<Rend>();
                        c.Rejilla[k] = l;
                    }
                    l.Add(r);
                }
            }
        }

        /// <summary>true si algo visible de la zona (menos lo fino, el piso enorme y <paramref name="propio"/>) toca la caja. Lo de paso (tablones, escaleras) cuenta con 4 m de margen.</summary>
        private static bool Choca(Ctx c, Bounds caja, Transform propio)
        {
            int x0 = Mathf.FloorToInt((caja.min.x - 4.5f) / Celda), x1 = Mathf.FloorToInt((caja.max.x + 4.5f) / Celda);
            int z0 = Mathf.FloorToInt((caja.min.z - 4.5f) / Celda), z1 = Mathf.FloorToInt((caja.max.z + 4.5f) / Celda);
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    List<Rend> l;
                    if (!c.Rejilla.TryGetValue(Llave(x, z), out l))
                    {
                        continue;
                    }
                    foreach (Rend r in l)
                    {
                        if (propio != null && r.R.transform == propio)
                        {
                            continue;
                        }
                        if (r.Obs.Intersects(caja))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool PuntoEn(Ctx c, Vector3 p, float margen)
        {
            foreach (Plane pl in c.Planos)
            {
                if (pl.GetDistanceToPoint(p) < -margen)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>El golpe más alto (lo que se ve desde arriba) y la altura del más bajo, ignorando lo nuevo.</summary>
        private static bool Terreno(Ctx c, float x, float z, out RaycastHit alto, out float bajo)
        {
            alto = default(RaycastHit);
            bajo = float.MaxValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, c.TopeY, z), Vector3.down, bufRayos, c.TopeY + 30f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = bufRayos[i].collider.transform;
                if (c.Nuevo != null && t.IsChildOf(c.Nuevo))
                {
                    continue;
                }
                if (!ok || bufRayos[i].point.y > alto.point.y)
                {
                    alto = bufRayos[i];
                    ok = true;
                }
                if (bufRayos[i].point.y < bajo)
                {
                    bajo = bufRayos[i].point.y;
                }
            }
            return ok;
        }

        private static bool EsCarril(string ruta)
        {
            return !FichasAcera.Any(t => ruta.Contains(t)) && FichasCarril.Any(t => ruta.Contains(t));
        }

        private static bool EsMarOEscalon(string ruta)
        {
            return ruta == "mar/" || ruta.StartsWith("mar/") || ruta.Contains("/mar/") || FichasPaso.Any(t => ruta.Contains(t));
        }

        // ================================================================ Cámara aérea

        private static void BuscarCamara(Ctx c)
        {
            Cfg f = c.F;
            GameObject g = new GameObject("T1414_CamaraBusqueda");
            g.hideFlags = HideFlags.HideAndDontSave;
            Camera cam = g.AddComponent<Camera>();
            cam.fieldOfView = f.Campo;
            cam.aspect = AnchoFoto / AltoFoto;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 160f;
            cam.enabled = false;
            float mejor = float.MinValue;
            Vector3 mejorDesde = f.Desde0, mejorHacia = f.Hacia0;
            int probados = 0;
            try
            {
                Vector3 horiz = f.Desde0 - f.Hacia0;
                horiz.y = 0f;
                horiz.Normalize();
                foreach (float dx in new[] { -24f, 0f, 24f })
                {
                    foreach (float dz in new[] { -24f, 0f, 24f })
                    {
                        Vector3 t = f.Hacia0 + new Vector3(dx, 0f, dz);
                        RaycastHit h;
                        float bajo;
                        if (Terreno(c, t.x, t.z, out h, out bajo))
                        {
                            t.y = h.point.y;
                        }
                        foreach (float ang in new[] { -45f, -22f, 0f, 22f, 45f })
                        {
                            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * horiz;
                            foreach (float d in new[] { 55f, 75f, 95f })
                            {
                                foreach (float alto in new[] { 30f, 40f, 50f, 65f })
                                {
                                    float cabeceo = Mathf.Atan2(alto, d) * Mathf.Rad2Deg;
                                    if (cabeceo < 20f || cabeceo > 46f)
                                    {
                                        continue;
                                    }
                                    Vector3 p = t + dir * d + Vector3.up * alto;
                                    float s = Puntuar(c, cam, p, t);
                                    probados++;
                                    if (s > mejor)
                                    {
                                        mejor = s;
                                        mejorDesde = p;
                                        mejorHacia = t;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
            if (mejor <= -1e8f)
            {
                Log("AVISO cámara: ninguna candidata libre (" + probados + " probadas); se queda con la anterior " + f.Desde0 + " -> " + f.Hacia0 + ".");
                mejorDesde = f.Desde0;
                mejorHacia = f.Hacia0;
            }
            else
            {
                Log("Cámara aérea: " + probados + " probadas, mejor puntaje " + mejor.ToString("0.0") + ". Desde " + mejorDesde.ToString("F1") + " hacia " + mejorHacia.ToString("F1")
                    + " (cabeceo " + (Mathf.Atan2(mejorDesde.y - mejorHacia.y, new Vector2(mejorDesde.x - mejorHacia.x, mejorDesde.z - mejorHacia.z).magnitude) * Mathf.Rad2Deg).ToString("0") + " grados).");
            }
            c.Desde = mejorDesde;
            c.Hacia = mejorHacia;
            Transform marca = new GameObject(NombreCamara).transform;
            marca.SetParent(c.Nuevo, false);
            marca.position = mejorDesde;
            marca.rotation = Quaternion.LookRotation(mejorHacia - mejorDesde);
            Transform mira = new GameObject(NombreMira).transform;
            mira.SetParent(marca, true);
            mira.position = mejorHacia;
            PonerPlanos(c, mejorDesde, mejorHacia);
        }

        private static void PonerPlanos(Ctx c, Vector3 desde, Vector3 hacia)
        {
            GameObject g = new GameObject("T1414_CamaraPlanos");
            g.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Camera cam = g.AddComponent<Camera>();
                cam.fieldOfView = c.F.Campo;
                cam.aspect = AnchoFoto / AltoFoto;
                cam.nearClipPlane = 0.5f;
                cam.farClipPlane = 160f;
                cam.enabled = false;
                cam.transform.position = desde;
                cam.transform.rotation = Quaternion.LookRotation(hacia - desde);
                GeometryUtility.CalculateFrustumPlanes(cam, c.Planos);
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        private static float Puntuar(Ctx c, Camera cam, Vector3 p, Vector3 t)
        {
            if (c.Fisica.OverlapSphere(p, 2f, bufCol, ~0, QueryTriggerInteraction.Ignore) > 0)
            {
                return -1e9f;
            }
            RaycastHit h;
            if (c.Fisica.Raycast(p, Vector3.down, out h, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                return -1e9f;
            }
            cam.transform.position = p;
            cam.transform.rotation = Quaternion.LookRotation(t - p);
            GeometryUtility.CalculateFrustumPlanes(cam, c.Planos);
            float s = 0f;
            foreach (Rend r in c.Rends)
            {
                if (r.B.Contains(p) && !r.Plano)
                {
                    s -= 40f;
                }
                if (!GeometryUtility.TestPlanesAABB(c.Planos, r.B))
                {
                    continue;
                }
                float d = Vector3.Distance(r.B.center, p);
                if (d < 4f)
                {
                    s -= 2f;
                    continue;
                }
                if (d <= 140f)
                {
                    s += r.Peso;
                }
            }
            foreach (Vector3 l in c.Luces)
            {
                if (Vector3.Distance(l, p) < 140f && PuntoEn(c, l, 0f))
                {
                    s += 1.5f;
                }
            }
            int golpes = 0, cerca = 0;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Ray ray = cam.ViewportPointToRay(new Vector3((i + 0.5f) / 6f, (j + 0.5f) / 4f, 0f));
                    if (c.Fisica.Raycast(ray.origin, ray.direction, out h, 220f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        golpes++;
                        if (h.distance < 5f)
                        {
                            cerca++;
                        }
                    }
                }
            }
            return s + 60f * Mathf.Min(golpes / 24f, 0.9f) - 3f * cerca;
        }

        // ================================================================ Faroles de sodio

        private static void Faroles(Ctx c)
        {
            Cfg f = c.F;
            Transform padre = Hijo(c.Nuevo, "Faroles");
            List<Vector3> lamparas = new List<Vector3>(c.Lamparas);
            List<Vector3> candidatas = new List<Vector3>();
            const float paso = 6f;
            for (float x = c.Hacia.x - 120f; x <= c.Hacia.x + 120f; x += paso)
            {
                for (float z = c.Hacia.z - 120f; z <= c.Hacia.z + 120f; z += paso)
                {
                    RaycastHit alto;
                    float bajo;
                    if (!Terreno(c, x, z, out alto, out bajo) || alto.normal.y < 0.9f || alto.point.y - bajo > 0.8f)
                    {
                        continue;
                    }
                    string ruta = RutaDe(c, alto.collider.transform);
                    if (EsMarOEscalon(ruta) || !EsCarril(ruta) || !PuntoEn(c, alto.point, 4f) || Vector3.Distance(alto.point, c.Desde) > 150f)
                    {
                        continue;
                    }
                    candidatas.Add(alto.point);
                }
            }
            Log("Faroles: " + candidatas.Count + " puntos de calle dentro de cuadro.");
            HashSet<int> descartadas = new HashSet<int>();
            int puestos = 0, intentos = 0;
            int tope = Mathf.Min(f.TopeFaroles, TopeLucesNuevas - c.Nuevo.GetComponentsInChildren<Light>(true).Length);
            while (puestos < tope && intentos < 150)
            {
                intentos++;
                int mejor = -1;
                float distMejor = -1f;
                for (int i = 0; i < candidatas.Count; i++)
                {
                    if (descartadas.Contains(i))
                    {
                        continue;
                    }
                    float d = float.MaxValue;
                    foreach (Vector3 l in lamparas)
                    {
                        d = Mathf.Min(d, new Vector2(l.x - candidatas[i].x, l.z - candidatas[i].z).magnitude);
                    }
                    if (d > distMejor)
                    {
                        distMejor = d;
                        mejor = i;
                    }
                }
                if (mejor < 0 || distMejor < 18f)
                {
                    break;
                }
                descartadas.Add(mejor);
                Vector3 pie, haciaCalle;
                if (!BordeDeCalle(c, candidatas[mejor], out pie, out haciaCalle) || !PosteLibre(c, pie, haciaCalle))
                {
                    continue;
                }
                KitS2c.Poste(c.Obra, padre, pie, haciaCalle, pie.y, false);
                lamparas.Add(pie);
                puestos++;
            }
            Physics.SyncTransforms();
            Log("Faroles de sodio nuevos: " + puestos + " (tope " + f.TopeFaroles + ", solo donde no había sodio a 18 m, al borde de la calle).");
        }

        /// <summary>Desde un punto del carril camina en 8 direcciones hasta salir del carril a otro piso al mismo nivel (banqueta o terreno).</summary>
        private static bool BordeDeCalle(Ctx c, Vector3 q, out Vector3 pie, out Vector3 haciaCalle)
        {
            pie = Vector3.zero;
            haciaCalle = Vector3.zero;
            float mejorS = float.MaxValue;
            for (int k = 0; k < 8; k++)
            {
                Vector3 d = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward;
                for (float s = 1f; s <= 9f; s += 1f)
                {
                    Vector3 qq = q + d * s;
                    RaycastHit h;
                    float bajo;
                    if (!Terreno(c, qq.x, qq.z, out h, out bajo) || Mathf.Abs(h.point.y - q.y) > 0.5f || h.normal.y < 0.9f)
                    {
                        break;
                    }
                    string ruta = RutaDe(c, h.collider.transform);
                    if (EsMarOEscalon(ruta))
                    {
                        break;
                    }
                    if (EsCarril(ruta))
                    {
                        continue;
                    }
                    if (s < mejorS)
                    {
                        Vector3 pp = qq + d * 0.4f;
                        RaycastHit h2;
                        float bajo2;
                        if (Terreno(c, pp.x, pp.z, out h2, out bajo2) && Mathf.Abs(h2.point.y - q.y) <= 0.5f && !EsCarril(RutaDe(c, h2.collider.transform)))
                        {
                            mejorS = s;
                            pie = h2.point;
                            haciaCalle = -d;
                        }
                    }
                    break;
                }
            }
            return mejorS < float.MaxValue;
        }

        private static bool PosteLibre(Ctx c, Vector3 pie, Vector3 haciaCalle)
        {
            if (c.Fisica.OverlapBox(pie + Vector3.up * 3.6f, new Vector3(0.35f, 3.4f, 0.35f), bufCol, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore) > 0)
            {
                return false;
            }
            Bounds tronco = new Bounds(pie + Vector3.up * 3.5f, new Vector3(0.9f, 6.9f, 0.9f));
            Bounds brazo = new Bounds(pie + haciaCalle * 0.9f + Vector3.up * 6.6f, new Vector3(1.8f, 0.8f, 1.8f));
            return !Choca(c, tronco, null) && !Choca(c, brazo, null);
        }

        // ================================================================ Edificios: azoteas, ventanas y neones

        private static List<Edif> Listar(Ctx c)
        {
            List<Edif> lista = new List<Edif>();
            foreach (Transform t in c.Zona.GetComponentsInChildren<Transform>(false))
            {
                if (!(t.name == "Cuerpo" || t.name.EndsWith("|Cuerpo")) || t.IsChildOf(c.Nuevo))
                {
                    continue;
                }
                BoxCollider bc = t.GetComponent<BoxCollider>();
                if (bc == null || !bc.enabled || Mathf.Abs(Vector3.Dot(t.up, Vector3.up)) < 0.99f)
                {
                    continue;
                }
                Vector3 s = t.lossyScale;
                Vector3 mitad = new Vector3(bc.size.x * 0.5f * Mathf.Abs(s.x), bc.size.y * 0.5f * Mathf.Abs(s.y), bc.size.z * 0.5f * Mathf.Abs(s.z));
                Vector3 centro = t.TransformPoint(bc.center);
                Vector3 der = t.right;
                der.y = 0f;
                Vector3 fre = t.forward;
                fre.y = 0f;
                if (der.sqrMagnitude < 0.5f || fre.sqrMagnitude < 0.5f || mitad.y * 2f < 3f)
                {
                    continue;
                }
                lista.Add(new Edif { T = t, C = centro, Der = der.normalized, Fre = fre.normalized, HX = mitad.x, HZ = mitad.z, Techo = centro.y + mitad.y, Base = centro.y - mitad.y });
            }
            return lista.OrderBy(e => e.C.x).ThenBy(e => e.C.z).ToList();
        }

        private static Material MatVentana(int i)
        {
            string ruta = KitS2c.Carpeta + "/S2c_Ventana_" + i + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null)
            {
                return m;
            }
            Color[] colores = { new Color(2.6f, 1.55f, 0.7f), new Color(2.2f, 1.6f, 0.95f), new Color(2.9f, 1.4f, 0.55f) };
            Log("AVISO: falta " + ruta + "; se crea con el shader EmisionNoche sin textura.");
            return KitS2c.Mat("S2c_Ventana_" + i, () => KitS2c.Emision(colores[i], null));
        }

        private static Material MatAzoteaColor(int i)
        {
            Color[] colores =
            {
                new Color(0.62f, 0.32f, 0.24f), // teja oxidada
                new Color(0.2f, 0.22f, 0.25f),  // impermeabilizante negro
                new Color(0.24f, 0.42f, 0.44f), // azul verdoso
                new Color(0.66f, 0.5f, 0.3f),   // ocre
                new Color(0.7f, 0.68f, 0.62f),  // concreto claro
            };
            return KitS2c.Mat("T1414_Azotea_" + i, () =>
            {
                Material m = new Material(KitS2c.MatAzotea());
                m.SetColor("_BaseColor", colores[i]);
                return m;
            });
        }

        private static void Edificios(Ctx c)
        {
            Cfg f = c.F;
            c.CajaUnidad = KitS2c.MallaCaja(Vector3.one, 1f);
            c.CajaUnidad.name = f.Raiz + "_CajaUnidad";
            c.Obra.Mallas.Add(c.CajaUnidad);
            List<Edif> todos = Listar(c);
            List<Edif> vista = todos.Where(e => PuntoEn(c, e.C, 14f) && Vector3.Distance(e.C, c.Desde) <= f.Alcance).ToList();
            Log("Edificios (Cuerpo con BoxCollider): " + todos.Count + "; dentro del cuadro de la cámara aérea: " + vista.Count + ".");
            Transform padreLosas = Hijo(c.Nuevo, "Azoteas");
            Transform padreProps = Hijo(c.Nuevo, "Props");
            Transform padreVentanas = Hijo(c.Nuevo, "Ventanas");
            Transform padreNeones = Hijo(c.Nuevo, "Neones");
            KitS2c.Obra o = c.Obra;
            Material[] colores = Enumerable.Range(0, 5).Select(MatAzoteaColor).ToArray();
            Material[] ventanas = Enumerable.Range(0, 3).Select(MatVentana).ToArray();
            int losas = 0, props = 0, nVentanas = 0, neones = 0;
            int topeNeones = Mathf.Min(f.TopeNeones, TopeLucesNuevas - c.Nuevo.GetComponentsInChildren<Light>(true).Length);

            foreach (Edif e in vista)
            {
                // 1. Losa de color sobre la azotea gris (variedad desde arriba).
                if (e.HX < 1.6f || e.HZ < 1.6f || !AzoteaPlana(c, e))
                {
                    continue;
                }
                KitS2c.Losa(o, padreLosas, "Azotea_" + (++losas), new Vector3(e.C.x, e.Techo + AltoSobreLosa, e.C.z), Quaternion.LookRotation(e.Fre),
                    new Vector3(e.HX * 2f - 0.3f, GrosorLosaColor, e.HZ * 2f - 0.3f), colores[o.Azar.Next(colores.Length)], 2f, false);
                // 2. Cosas de azotea en las esquinas.
                if (props < TopeProps && e.HX >= 2.4f && e.HZ >= 2.4f)
                {
                    props += Props(c, e, padreProps, ventanas, TopeProps - props);
                }
                // 3. Ventanas encendidas en las paredes que miran a la cámara.
                if (nVentanas < TopeVentanas)
                {
                    nVentanas += Ventanas(c, e, padreVentanas, ventanas, TopeVentanas - nVentanas);
                }
                // 4. Un neón en una pared que mira a la cámara.
                if (neones < topeNeones && o.Chance(0.2f) && Neon(c, e, padreNeones, neones))
                {
                    neones++;
                }
            }
            Log("Azoteas de color: " + losas + ". Cosas de azotea: " + props + ". Ventanas encendidas: " + nVentanas + ". Neones: " + neones + " (tope " + topeNeones + ").");
        }

        private static bool AzoteaPlana(Ctx c, Edif e)
        {
            foreach (Vector2 k in new[] { Vector2.zero, new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f) })
            {
                Vector3 p = e.C + e.Der * (k.x * (e.HX - 0.5f)) + e.Fre * (k.y * (e.HZ - 0.5f));
                RaycastHit h;
                float bajo;
                if (!Terreno(c, p.x, p.z, out h, out bajo))
                {
                    return false;
                }
                // El golpe más alto debe ser la azotea (o su losa de 2 cm); si es otra cosa más alta, la azotea está ocupada por algo.
                if (Mathf.Abs(h.point.y - e.Techo) > 0.12f)
                {
                    return false;
                }
            }
            return true;
        }

        private static int Props(Ctx c, Edif e, Transform padre, Material[] ventanas, int cupo)
        {
            KitS2c.Obra o = c.Obra;
            List<Vector2> esquinas = new List<Vector2> { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) };
            int puestos = 0;
            int maximo = Mathf.Min(cupo, e.HX >= 3.4f && e.HZ >= 3.4f ? 2 : 1);
            while (esquinas.Count > 0 && puestos < maximo)
            {
                int i = o.Azar.Next(esquinas.Count);
                Vector2 k = esquinas[i];
                esquinas.RemoveAt(i);
                if (!o.Chance(0.7f))
                {
                    continue;
                }
                Vector3 p = e.C + e.Der * (k.x * (e.HX - 1.4f)) + e.Fre * (k.y * (e.HZ - 1.4f));
                p.y = e.Techo + AltoSobreLosa + GrosorLosaColor * 0.5f;
                RaycastHit h;
                float bajo;
                if (!Terreno(c, p.x, p.z, out h, out bajo) || Mathf.Abs(h.point.y - e.Techo) > 0.12f)
                {
                    continue;
                }
                double suerte = o.Azar.NextDouble();
                string tipo = suerte < 0.4 ? "Tinaco" : suerte < 0.7 ? "Antena" : suerte < 0.85 ? "Aire" : (e.HX >= 3f && e.HZ >= 3f ? "Cuarto" : "Antena");
                float radio = tipo == "Tinaco" ? 1.0f : tipo == "Antena" ? 0.75f : tipo == "Aire" ? 0.7f : 1.15f;
                float alto = tipo == "Tinaco" ? 2.1f : tipo == "Antena" ? 3.4f : tipo == "Aire" ? 1.0f : 2.4f;
                Bounds caja = new Bounds(p + Vector3.up * (alto * 0.5f + 0.05f), new Vector3(radio * 2f, alto, radio * 2f));
                if (Choca(c, caja, e.T))
                {
                    continue;
                }
                GameObject grupo = new GameObject(tipo + "_" + (++c.Obra.Casas));
                grupo.transform.SetParent(padre, false);
                grupo.transform.position = p;
                Quaternion giro = Quaternion.LookRotation(e.Fre);
                bool ok = true;
                if (tipo == "Tinaco")
                {
                    KitS2c.Medida mt = KitS2c.Medir(KitS2c.BaseTinaco);
                    ok = KitS2c.Prefab(KitS2c.BaseTinaco) != null && KitS2c.Prefab("C09_Tinaco") != null;
                    if (ok)
                    {
                        KitS2c.Pieza(grupo.transform, KitS2c.BaseTinaco, p, giro, new Vector3(1.5f / mt.Caja.size.x, 0.7f / mt.Caja.size.y, 1f), false).name = "BaseTinaco";
                        KitS2c.Pieza(grupo.transform, "C09_Tinaco", p + Vector3.up * 0.7f, giro * Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), Vector3.one * o.Rango(1.05f, 1.25f), false);
                    }
                }
                else if (tipo == "Antena")
                {
                    ok = KitS2c.Prefab("CY04_AntenaAzotea") != null;
                    if (ok)
                    {
                        KitS2c.Pieza(grupo.transform, "CY04_AntenaAzotea", p, giro * Quaternion.Euler(0f, o.Rango(-40f, 40f), 0f), Vector3.one * 0.8f, false);
                    }
                }
                else if (tipo == "Aire")
                {
                    ok = KitS2c.Prefab("C10_AireAcondicionado") != null;
                    if (ok)
                    {
                        KitS2c.Pieza(grupo.transform, "C10_AireAcondicionado", p, giro, Vector3.one, false);
                    }
                }
                else
                {
                    Vector3 tam = new Vector3(2.0f, 2.3f, 2.0f);
                    KitS2c.Losa(o, grupo.transform, "Cuarto", p + Vector3.up * (tam.y * 0.5f), giro, tam, KitS2c.MatCuerpo(o.Azar.Next(KitS2c.ColoresPiso.Length)), 3f, false);
                    PonerVentana(c, grupo.transform, p + Vector3.up * 1.3f + e.Fre * (tam.z * 0.5f), e.Fre, ventanas[o.Azar.Next(ventanas.Length)], 0.7f, 0.9f);
                }
                if (!ok)
                {
                    Object.DestroyImmediate(grupo);
                    continue;
                }
                puestos++;
            }
            return puestos;
        }

        private static void PonerVentana(Ctx c, Transform padre, Vector3 centroCara, Vector3 normal, Material m, float ancho, float alto)
        {
            GameObject v = new GameObject("Ventana");
            v.transform.SetParent(padre, false);
            v.transform.SetPositionAndRotation(centroCara + normal * (ProfundoVentana * 0.5f), Quaternion.LookRotation(normal));
            v.transform.localScale = new Vector3(ancho, alto, ProfundoVentana);
            v.AddComponent<MeshFilter>().sharedMesh = c.CajaUnidad;
            MeshRenderer mr = v.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            c.Obra.Ventanas++;
        }

        private static int Ventanas(Ctx c, Edif e, Transform padre, Material[] materiales, int cupo)
        {
            KitS2c.Obra o = c.Obra;
            Vector3[] normales = { e.Der, -e.Der, e.Fre, -e.Fre };
            float[] semi = { e.HX, e.HX, e.HZ, e.HZ };
            float[] largo = { e.HZ * 2f, e.HZ * 2f, e.HX * 2f, e.HX * 2f };
            int puestas = 0;
            for (int k = 0; k < 4 && puestas < Mathf.Min(cupo, 8); k++)
            {
                Vector3 n = normales[k];
                Vector3 centroCara = e.C + n * semi[k];
                if (Vector3.Dot(n, c.Desde - centroCara) <= 0.2f)
                {
                    continue;
                }
                Vector3 lado = Vector3.Cross(Vector3.up, n);
                for (float y = e.Base + 1.7f; y + 0.8f < e.Techo - 0.9f; y += 2.9f)
                {
                    for (float x = -largo[k] * 0.5f + 1.0f; x < largo[k] * 0.5f - 1.0f; x += 2.4f)
                    {
                        if (puestas >= Mathf.Min(cupo, 8) || !o.Chance(0.3f))
                        {
                            continue;
                        }
                        Vector3 centro = new Vector3(centroCara.x, y, centroCara.z) + lado * x;
                        if (!PuntoEn(c, centro, 4f))
                        {
                            continue;
                        }
                        Vector3 tam = new Vector3(Mathf.Abs(lado.x) * AnchoVentana + Mathf.Abs(n.x) * 0.1f, AlturaVentana, Mathf.Abs(lado.z) * AnchoVentana + Mathf.Abs(n.z) * 0.1f);
                        if (Choca(c, new Bounds(centro + n * 0.04f, tam), e.T))
                        {
                            continue;
                        }
                        PonerVentana(c, padre, centro, n, materiales[o.Azar.Next(materiales.Length)], AnchoVentana, AlturaVentana);
                        c.Ventanas.Add(centro);
                        puestas++;
                    }
                }
            }
            return puestas;
        }

        private static bool Neon(Ctx c, Edif e, Transform padre, int numero)
        {
            KitS2c.Obra o = c.Obra;
            Vector3[] normales = { e.Der, -e.Der, e.Fre, -e.Fre };
            float[] semi = { e.HX, e.HX, e.HZ, e.HZ };
            float[] largo = { e.HZ * 2f, e.HZ * 2f, e.HX * 2f, e.HX * 2f };
            int inicio = o.Azar.Next(4);
            for (int j = 0; j < 4; j++)
            {
                int k = (inicio + j) % 4;
                Vector3 n = normales[k];
                Vector3 centroCara = e.C + n * semi[k];
                if (Vector3.Dot(n, c.Desde - centroCara) <= 0.3f || largo[k] < 3f)
                {
                    continue;
                }
                Vector3 lado = Vector3.Cross(Vector3.up, n);
                float y = Mathf.Max(e.Base + 3f, e.Techo - 1.3f);
                Vector3 p = new Vector3(centroCara.x, y, centroCara.z) + lado * (o.Rango(-0.3f, 0.3f) * largo[k]);
                if (!PuntoEn(c, p, 4f) || c.Ventanas.Any(v => Vector3.Distance(v, p) < 1.8f))
                {
                    continue;
                }
                if (Choca(c, new Bounds(p + n * 0.4f, new Vector3(1.4f, 1.8f, 1.4f)), e.T))
                {
                    continue;
                }
                KitS2c.Neon(o, padre, p, n, lado, (numero % 2 == 0) ? KitS2c.Magenta : KitS2c.Turquesa, o.Chance(0.5f));
                return true;
            }
            return false;
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
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
                n++;
            }
            Log("Objetos marcados Static: " + n + ".");
        }

        // ================================================================ Validar

        /// <summary>
        /// Por zona: existe la raíz y la cámara con su mira; faroles al borde de la calle (no en el carril) y asentados; cosas de azotea, losas de color y ventanas asentadas
        /// (sin flotar ni enterrarse) y sin encimarse con lo que ya había; ventanas y neones pegados a una pared; sin colliders nuevos sin malla; tope de luces (40),
        /// de objetos (3000) y tamaño del prefab. Escribe Logs/t1414-validar.txt.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1414/Aereas: validar")]
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1414: validación\n");
            int fallos = 0, avisos = 0;
            foreach (Cfg f in Zonas)
            {
                sb.AppendLine();
                sb.AppendLine("== " + f.Nombre);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(f.Ruta) == null)
                {
                    sb.AppendLine("FALLO: no existe " + f.Ruta);
                    fallos++;
                    continue;
                }
                GameObject zona = PrefabUtility.LoadPrefabContents(f.Ruta);
                try
                {
                    Ctx c = Abrir(zona, f);
                    if (c.Nuevo == null)
                    {
                        sb.AppendLine("FALLO: no existe " + f.Raiz + "; corre Aplicar.");
                        fallos++;
                        continue;
                    }
                    Physics.SyncTransforms();
                    ValidarZona(c, sb, ref fallos, ref avisos);
                }
                catch (System.Exception e)
                {
                    sb.AppendLine("FALLO: excepción " + e);
                    fallos++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(zona);
                }
            }
            sb.AppendLine();
            sb.AppendLine("Resumen: " + fallos + " fallos, " + avisos + " avisos.");
            Escribir(RutaInformeValidar, sb);
        }

        private static void ValidarZona(Ctx c, StringBuilder sb, ref int fallos, ref int avisos)
        {
            Cfg f = c.F;
            Transform marca = c.Nuevo.Find(NombreCamara);
            if (marca == null || marca.Find(NombreMira) == null)
            {
                sb.AppendLine("FALLO: falta " + NombreCamara + "/" + NombreMira + " (Fotos no sabría dónde tomar).");
                fallos++;
            }
            else
            {
                Vector3 p = marca.position;
                if (c.Fisica.OverlapSphere(p, 1.5f, bufCol, ~0, QueryTriggerInteraction.Ignore) > 0)
                {
                    sb.AppendLine("FALLO: la cámara aérea está dentro de algo en " + p.ToString("F1") + ".");
                    fallos++;
                }
                else
                {
                    sb.AppendLine("Cámara aérea " + p.ToString("F1") + " -> " + marca.Find(NombreMira).position.ToString("F1") + ": libre.");
                }
            }
            // Colliders nuevos sin malla visible (muros invisibles).
            foreach (Collider col in c.Nuevo.GetComponentsInChildren<Collider>(true))
            {
                if (col.GetComponentInChildren<Renderer>(true) == null && col.GetComponentInParent<Renderer>() == null)
                {
                    sb.AppendLine("FALLO: collider sin malla visible en " + col.transform.name + ".");
                    fallos++;
                }
            }
            // Faroles
            Transform faroles = c.Nuevo.Find("Faroles");
            int nFaroles = 0;
            if (faroles != null)
            {
                foreach (Transform poste in faroles)
                {
                    if (!poste.name.StartsWith("Poste_"))
                    {
                        continue;
                    }
                    nFaroles++;
                    RaycastHit h;
                    float bajo;
                    if (!Terreno(c, poste.position.x, poste.position.z, out h, out bajo))
                    {
                        sb.AppendLine("FALLO: " + poste.name + " sin piso debajo en " + poste.position.ToString("F1") + ".");
                        fallos++;
                        continue;
                    }
                    if (Mathf.Abs(poste.position.y - h.point.y) > 0.15f)
                    {
                        sb.AppendLine("FALLO: " + poste.name + " flota o está enterrado " + (poste.position.y - h.point.y).ToString("F2") + " m.");
                        fallos++;
                    }
                    if (EsCarril(RutaDe(c, h.collider.transform)))
                    {
                        sb.AppendLine("FALLO: " + poste.name + " está en el carril (" + RutaDe(c, h.collider.transform) + ").");
                        fallos++;
                    }
                    Bounds tronco = new Bounds(poste.position + Vector3.up * 3.5f, new Vector3(0.9f, 6.9f, 0.9f));
                    if (Choca(c, tronco, null))
                    {
                        sb.AppendLine("FALLO: " + poste.name + " se encima con algo en " + poste.position.ToString("F1") + ".");
                        fallos++;
                    }
                }
            }
            sb.AppendLine("Faroles: " + nFaroles + " (tope " + f.TopeFaroles + ").");
            if (nFaroles > f.TopeFaroles)
            {
                sb.AppendLine("FALLO: más faroles que el tope.");
                fallos++;
            }
            // Azoteas, cosas de azotea
            Transform losas = c.Nuevo.Find("Azoteas");
            if (losas != null)
            {
                foreach (Transform t in losas)
                {
                    Renderer r = t.GetComponent<Renderer>();
                    RaycastHit h;
                    float bajo;
                    if (r == null || !Terreno(c, r.bounds.center.x, r.bounds.center.z, out h, out bajo))
                    {
                        sb.AppendLine("FALLO: losa " + t.name + " sin azotea debajo.");
                        fallos++;
                        continue;
                    }
                    float hueco = r.bounds.min.y - h.point.y;
                    if (hueco < -0.03f || hueco > 0.08f)
                    {
                        sb.AppendLine("FALLO: losa " + t.name + " con hueco " + hueco.ToString("F3") + " m sobre su azotea.");
                        fallos++;
                    }
                }
                sb.AppendLine("Losas de color: " + losas.childCount + ".");
            }
            Transform props = c.Nuevo.Find("Props");
            if (props != null)
            {
                foreach (Transform t in props)
                {
                    Bounds b;
                    if (!BoundsDe(t, out b))
                    {
                        sb.AppendLine("FALLO: " + t.name + " sin malla.");
                        fallos++;
                        continue;
                    }
                    RaycastHit h;
                    float bajo;
                    if (!Terreno(c, b.center.x, b.center.z, out h, out bajo))
                    {
                        sb.AppendLine("FALLO: " + t.name + " sin piso debajo.");
                        fallos++;
                        continue;
                    }
                    float hueco = b.min.y - h.point.y;
                    if (hueco < -0.1f || hueco > 0.12f)
                    {
                        sb.AppendLine("FALLO: " + t.name + " con hueco " + hueco.ToString("F3") + " m sobre su azotea.");
                        fallos++;
                    }
                    Bounds encogida = b;
                    encogida.Expand(-0.2f);
                    if (Choca(c, encogida, h.collider.transform))
                    {
                        sb.AppendLine("FALLO: " + t.name + " se encima con algo que ya estaba en " + b.center.ToString("F1") + ".");
                        fallos++;
                    }
                }
                sb.AppendLine("Cosas de azotea: " + props.childCount + " (tope " + TopeProps + ").");
                if (props.childCount > TopeProps)
                {
                    sb.AppendLine("FALLO: más cosas de azotea que el tope.");
                    fallos++;
                }
            }
            // Ventanas y neones: pegados a una pared (rayo hacia atrás contra el collider del Cuerpo).
            int ventanas = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Ventana" && t.name != "Neon")
                {
                    continue;
                }
                if (t.name == "Ventana" && t.parent != null && t.parent.name.StartsWith("Cuarto"))
                {
                    continue;
                }
                }
                ventanas += t.name == "Ventana" ? 1 : 0;
                Vector3 dir = t.name == "Ventana" ? -t.forward : t.forward;
                RaycastHit h;
                if (!c.Fisica.Raycast(t.position + (t.name == "Ventana" ? -dir : -dir * 0f) * 0.3f, dir, out h, t.name == "Ventana" ? 0.6f : 1.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    sb.AppendLine("AVISO: " + t.name + " en " + t.position.ToString("F1") + " no toca ninguna pared (si es de un cuartito de azotea es normal: sus paredes no tienen collider).");
                    avisos++;
                }
            }
            sb.AppendLine("Ventanas: " + ventanas + " (tope " + TopeVentanas + ").");
            int luces = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
            int objetos = c.Nuevo.GetComponentsInChildren<Transform>(true).Length;
            sb.AppendLine("Luces nuevas: " + luces + " (tope " + TopeLucesNuevas + "). Objetos nuevos: " + objetos + " (tope " + TopeObjetosNuevos + ").");
            if (luces > TopeLucesNuevas)
            {
                sb.AppendLine("FALLO: demasiadas luces nuevas.");
                fallos++;
            }
            if (objetos > TopeObjetosNuevos)
            {
                sb.AppendLine("FALLO: demasiados objetos nuevos.");
                fallos++;
            }
            foreach (Light l in c.Nuevo.GetComponentsInChildren<Light>(true))
            {
                if (l.shadows != LightShadows.None || l.GetComponent<LuzNocturna>() == null)
                {
                    sb.AppendLine("FALLO: la luz " + l.transform.parent.name + " tiene sombras o no tiene LuzNocturna.");
                    fallos++;
                }
            }
            double mb = new FileInfo(f.Ruta).Length / 1048576.0;
            sb.AppendLine("Tamaño del prefab: " + mb.ToString("0.0") + " MB (tope 60).");
            if (new FileInfo(f.Ruta).Length > TopeBytesPrefab)
            {
                sb.AppendLine("FALLO: prefab de más de 60 MB.");
                fallos++;
            }
        }

        private static bool BoundsDe(Transform t, out Bounds b)
        {
            b = default(Bounds);
            bool primero = true;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (primero)
                {
                    b = r.bounds;
                    primero = false;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return !primero;
        }

        // ================================================================ Fotos

        /// <summary>
        /// Una foto por zona desde la cámara aérea que dejó Aplicar (T1414_&lt;Zona&gt;/CamaraA): con niebla aérea ligera (docs/capturas/t-1414/&lt;zona&gt;-a1x.png) y con la niebla del juego
        /// (&lt;zona&gt;-a1x-niebla.png), cada una con la referencia a la izquierda. No guarda nada; corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1414/Aereas: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            Directory.CreateDirectory("Logs");
            Physics.SyncTransforms();
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Cfg f in Zonas)
            {
                Vector3 desde = f.Desde0, hacia = f.Hacia0;
                Transform raiz = todos.FirstOrDefault(t => t.name == f.Raiz);
                Transform marca = raiz != null ? raiz.Find(NombreCamara) : null;
                if (marca != null && marca.Find(NombreMira) != null)
                {
                    desde = marca.position;
                    hacia = marca.Find(NombreMira).position;
                }
                else
                {
                    Debug.LogWarning("[AereasT1414] " + f.Nombre + ": no hay " + f.Raiz + "/" + NombreCamara + " (corre Aplicar); se usa la cámara anterior.");
                }
                string juego = "Logs/t1414-" + f.Foto + "-juego.png";
                string juegoNiebla = "Logs/t1414-" + f.Foto + "-niebla-juego.png";
                FotoAerea(desde, hacia, juego, f.Campo, DensidadNieblaAerea);
                FotoAerea(desde, hacia, juegoNiebla, f.Campo, -1f);
                ColocarS2.LadoALado("imageassets/referencias/look/" + f.Referencia + ".png", juego, CarpetaFotos + "/" + f.Foto + ".png");
                ColocarS2.LadoALado("imageassets/referencias/look/" + f.Referencia + ".png", juegoNiebla, CarpetaFotos + "/" + f.Foto + "-niebla.png");
            }
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
            GameObject go = new GameObject("CamaraT1414");
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

        // ================================================================ Diagnóstico de las torres en la bruma

        /// <summary>
        /// Abre la escena y escribe Logs/t1414-torres.txt: la altura de cada plano de bruma de T-1399 (T1399_Bruma/Bruma_*) y, de cada cosa alta (más de 40 m) a menos de 600 m de
        /// las cámaras aéreas, su base, su cima y cuánto la separa del piso que tiene debajo. Con "FLOTA" si la base está a más de 1 m sobre el piso (le falta base);
        /// con "cortada por la bruma" si la base queda bajo el plano de bruma de su zona (es la bruma, no la torre). No guarda nada.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1414/Aereas: diagnostico de torres y bruma")]
        public static void Diagnostico()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("Logs");
            Physics.SyncTransforms();
            StringBuilder sb = new StringBuilder("T-1414: torres y plano de bruma\n");
            GameObject bruma = GameObject.Find("T1399_Bruma");
            List<float> alturasBruma = new List<float>();
            if (bruma == null)
            {
                sb.AppendLine("No hay T1399_Bruma en la escena.");
            }
            else
            {
                foreach (Transform t in bruma.transform)
                {
                    if (t.name.StartsWith("Bruma_"))
                    {
                        alturasBruma.Add(t.position.y);
                        sb.AppendLine(t.name + ": plano a y = " + t.position.y.ToString("F1") + ", " + t.localScale.x.ToString("F0") + " x " + t.localScale.z.ToString("F0") + " m, centro " + t.position.ToString("F0") + ".");
                    }
                }
            }
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<Vector3> camaras = new List<Vector3>();
            foreach (Cfg f in Zonas)
            {
                Transform raiz = todos.FirstOrDefault(t => t.name == f.Raiz);
                Transform marca = raiz != null ? raiz.Find(NombreCamara) : null;
                camaras.Add(marca != null ? marca.position : f.Desde0);
            }
            int n = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OrderBy(rr => rr.bounds.min.y))
            {
                Bounds b = r.bounds;
                if (b.size.y < 40f || b.size.x > 400f || b.size.z > 400f || !camaras.Any(p => new Vector2(p.x - b.center.x, p.z - b.center.z).magnitude < 600f))
                {
                    continue;
                }
                RaycastHit[] hits = Physics.RaycastAll(new Vector3(b.center.x, b.min.y + 0.5f, b.center.z), Vector3.down, 400f, ~0, QueryTriggerInteraction.Ignore);
                float piso = float.MinValue;
                foreach (RaycastHit h in hits)
                {
                    if (!h.collider.transform.IsChildOf(r.transform) && h.collider.transform != r.transform)
                    {
                        piso = Mathf.Max(piso, h.point.y);
                    }
                }
                string ruta = r.transform.name;
                for (Transform p = r.transform.parent; p != null && ruta.Length < 90; p = p.parent)
                {
                    ruta = p.name + "/" + ruta;
                }
                string estado = piso == float.MinValue ? "sin piso medible debajo" : (b.min.y - piso > 1f ? "FLOTA " + (b.min.y - piso).ToString("F1") + " m sobre el piso" : "asentada");
                if (alturasBruma.Any(y => b.min.y < y && b.max.y > y))
                {
                    estado += "; cortada por la bruma (base bajo el plano, cima sobre él)";
                }
                sb.AppendLine(ruta + ": base y=" + b.min.y.ToString("F1") + ", cima y=" + b.max.y.ToString("F1") + ", tamaño " + b.size.ToString("F0") + ", " + estado + ".");
                if (++n >= 80)
                {
                    sb.AppendLine("(se corta en 80)");
                    break;
                }
            }
            sb.AppendLine(n + " cosas altas cerca de las cámaras.");
            Escribir(RutaInformeTorres, sb);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }

        // ================================================================ Ayudas

        private static Transform Hijo(Transform padre, string nombre)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(padre, false);
            return g.transform;
        }

        private static void Log(string s)
        {
            if (informe != null)
            {
                informe.AppendLine(s);
            }
        }

        private static string Megas(string ruta)
        {
            return (new FileInfo(ruta).Length / 1048576.0).ToString("0.0");
        }

        private static void Escribir(string ruta, StringBuilder sb)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ruta, sb.ToString());
            Debug.Log("[AereasT1414] " + ruta + ":\n" + sb);
        }
    }
}
