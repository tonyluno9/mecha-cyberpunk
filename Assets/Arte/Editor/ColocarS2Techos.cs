using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// S2 techos B (14-nov): se puede subir a las azoteas. Junta techos vecinos en grupos (hueco horizontal menor o igual a 3,2 m y
    /// diferencia de alto menor o igual a 1,8 m), asegura que cada techo se camina, y pone accesos: escaleras de servicio y tubos de
    /// desagüe (greybox + <see cref="PuntoTrepar"/>), rutas de parkour (contenedor L01 y barda MT01 hasta una escalera, o contenedor solo
    /// en un techo bajo), pretiles C05 en los bordes que dan a la calle y unos tablones entre grupos. Idempotente: rehace su raíz
    /// <see cref="Raiz"/> y las mallas de <see cref="RutaMallas"/>. Todo sale de sondeos de física sobre la escena, sin coordenadas a mano.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2Techos.Colocar
    /// </summary>
    public static class ColocarS2Techos
    {
        public const string Raiz = "S2_Techos";
        public const string RutaCarpeta = "Assets/Arte/Mundo/S2b/";
        public const string RutaMallas = RutaCarpeta + "Mallas";
        private const string RutaContenedor = "Assets/Arte/Callejon/Prefabs/L01_Contenedor.prefab";
        private const string RutaBarda = "Assets/Arte/Calle/Prefabs/MT01_MuroTrasero.prefab";
        private const string RutaPretil = "Assets/Arte/Calle/Prefabs/C05_PretilAzotea.prefab";

        // Reglas de agrupación y de movimiento del jugador (TreparAPie, ConfigTrepar_Base).
        private const float HuecoMax = 3.2f;
        private const float DifAltoMax = 1.8f;
        private const float TolTecho = 0.15f;
        private const float AgarreMin = 0.5f;
        private const float AgarreMax = 2.3f;
        private const float AlturaContenedor = 1.61f;
        private const float AlturaBarda = 3.5f;
        private const float AlturaBardaPrefab = 6f;
        private const float LargoBarda = 4.79f;
        private const float FondoBarda = 1.4f;
        private const float LargoContenedor = 2f;
        private const float FondoContenedor = 1.25f;
        private const float LargoPretil = 1.88f;
        private const float FondoPretil = 0.35f;
        private const float LibreSalida = 2f;
        private const float SaltoNaturalMin = 0.3f;
        private const float TablonMin = 3.2f;
        private const float TablonMax = 6f;
        private const float TablonAncho = 0.5f;
        private const float TablonGrueso = 0.1f;
        private const float PasoEscalera = 0.3f;
        private const float AlturaMinAcceso = 2.6f;
        private const float SueloMaxY = 1.2f;

        private static readonly Vector3[] Dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        private static readonly string[] ZonasCasas = { "S2c_Suburbios", "S2c_CalleDorada", "S2c_Mercado" };
        private static readonly string[] NombresProps = { "BaseTinaco", "C09_Tinaco", "C10_AireAcondicionado", "CY04_AntenaAzotea" };

        private sealed class Techo
        {
            public Transform casa;
            public BoxCollider col;
            public Bounds b;
            public float top;
            public string zona;
            public int grupo = -1;
            public bool descartado;
            public int indice;
            public string Nombre => casa.parent.name + "/" + casa.name;
        }

        private sealed class Acceso
        {
            public Techo techo;
            public Vector3 n;
            public Vector3 w;      // punto del muro, y = suelo
            public float baseY;    // dónde empieza la subida (suelo o parte de arriba de la barda)
            public TipoTrepar tipo = TipoTrepar.Escalera;
            public string zona;
            public string ruta;    // nombre de la ruta de parkour si el acceso cierra una
        }

        private static List<Techo> techos;
        private static List<Acceso> accesos;
        private static List<Vector3> salidas;
        private static List<Bounds> obstaculos;
        private static Transform raiz;
        private static Material matEscalera;
        private static Material matTubo;
        private static Material matTablon;
        private static int contadorRutas;
        private static int totalRutasB;
        private static int totalRutasA;

        [MenuItem("Mecha Cyberpunk/S2/Techos B (accesos, parkour, pretiles)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Preparar();
            RecogerTechos();
            DescartarContenidos();
            AsegurarCaminables();
            Agrupar();
            PonerPropsSolidos();
            Physics.SyncTransforms();
            ConstruirObstaculos();
            ParkourContenedorSolo();
            ParkourBarda();
            ElegirAccesos();
            CrearAccesos();
            Physics.SyncTransforms();
            ActivarPretilesExistentes();
            PonerPretiles();
            Tablones();
            SaltosNaturales();
            Resumen();
            ColocarS2.Guardar("s2-techos");
        }

        /// <summary>Solo analiza (sin tocar nada fuera de memoria) y devuelve cuántos candidatos hay. Para depurar con eval.</summary>
        public static string Analizar()
        {
            ColocarS2.Informe.Clear();
            File.WriteAllText("Logs/techos-progreso.txt", "inicio\n");
            ColocarS2.Abrir();
            Preparar();
            File.AppendAllText("Logs/techos-progreso.txt", "preparado\n");
            RecogerTechos();
            File.AppendAllText("Logs/techos-progreso.txt", "recogidos " + techos.Count + "\n");
            DescartarContenidos();
            Agrupar();
            File.AppendAllText("Logs/techos-progreso.txt", "agrupados\n");
            Physics.SyncTransforms();
            ConstruirObstaculos();
            File.AppendAllText("Logs/techos-progreso.txt", "obstaculos " + obstaculos.Count + "\n");
            Resumen();
            return ColocarS2.Informe.ToString();
        }

        // ------------------------------------------------------------------ preparación

        private static void Preparar()
        {
            Object.DestroyImmediate(GameObject.Find(Raiz));
            foreach (GameObject g in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == Raiz).ToList())
            {
                Object.DestroyImmediate(g);
            }
            raiz = new GameObject(Raiz).transform;
            Directory.CreateDirectory(RutaCarpeta);
            AssetDatabase.Refresh();
            if (AssetDatabase.IsValidFolder(RutaMallas))
            {
                AssetDatabase.DeleteAsset(RutaMallas);
            }
            AssetDatabase.CreateFolder(RutaCarpeta.TrimEnd('/'), "Mallas");
            // Textura de metal oxidado de la escalera de emergencia L03 (el test de SinGreybox no admite material liso).
            matEscalera = Material("S2b_EscaleraGreybox", new Color(0.55f, 0.55f, 0.58f), 0.6f, 0.35f, TexturaMetal);
            matTubo = Material("S2b_TuboGreybox", new Color(0.75f, 0.75f, 0.78f), 0.6f, 0.35f, TexturaTubo);
            matTablon = Material("S2b_TablonGreybox", new Color(0.32f, 0.22f, 0.14f), 0f, 0.2f);
            techos = new List<Techo>();
            accesos = new List<Acceso>();
            salidas = new List<Vector3>();
            contadorRutas = 0;
            totalRutasA = 0;
            totalRutasB = 0;
            Physics.SyncTransforms();
        }

        /// <summary>Solo actualiza los materiales de escalera y tubo (textura de metal) sin volver a colocar nada.</summary>
        public static void ActualizarMateriales()
        {
            Material("S2b_EscaleraGreybox", new Color(0.55f, 0.55f, 0.58f), 0.6f, 0.35f, TexturaMetal);
            Material("S2b_TuboGreybox", new Color(0.75f, 0.75f, 0.78f), 0.6f, 0.35f, TexturaTubo);
            AssetDatabase.SaveAssets();
        }

        private const string TexturaMetal = "Assets/Arte/Callejon/L03_EscaleraEmergencia/L03_EscaleraEmergencia_basecolor.jpg";
        private const string TexturaTubo = "Assets/Arte/Callejon/L03_EscaleraEmergencia/L03_EscaleraEmergencia_basecolor.jpg";

        private static Material Material(string nombre, Color color, float metal, float suavidad, string textura = null)
        {
            string ruta = RutaCarpeta + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", suavidad);
            if (textura != null)
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textura));
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Transform GrupoParkour()
        {
            Transform t = raiz.Find("Parkour");
            return t != null ? t : Grupo("Parkour");
        }

        private static Transform Grupo(string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(raiz, false);
            return t;
        }

        // ------------------------------------------------------------------ techos

        private static void RecogerTechos()
        {
            foreach (GameObject g in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                bool zonaCasas = ZonasCasas.Contains(g.name);
                if (!zonaCasas && g.name != "Barrio")
                {
                    continue;
                }
                foreach (BoxCollider c in g.GetComponentsInChildren<BoxCollider>(false))
                {
                    if (!c.enabled || c.isTrigger || c.transform.parent == null || c.transform.parent == g.transform)
                    {
                        continue;
                    }
                    Bounds b = c.bounds;
                    bool vale;
                    if (zonaCasas)
                    {
                        vale = c.name == "Cuerpo" && c.transform.parent.name.StartsWith("Casa_");
                    }
                    else
                    {
                        vale = c.name.StartsWith("Cuerpo") && c.transform.parent.name.StartsWith("Edificio_")
                            && b.max.y <= 11.01f && b.max.y >= 3f && b.size.x >= 3f && b.size.z >= 3f;
                    }
                    if (!vale)
                    {
                        continue;
                    }
                    Renderer r = c.GetComponent<Renderer>();
                    float top = r != null ? r.bounds.max.y : b.max.y;
                    techos.Add(new Techo { casa = c.transform.parent, col = c, b = b, top = top, zona = g.name, indice = techos.Count });
                }
            }
        }

        private static float Area(Bounds b)
        {
            return b.size.x * b.size.z;
        }

        private static float Solape(Bounds a, Bounds b)
        {
            float dx = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            float dz = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
            return dx > 0f && dz > 0f ? dx * dz : 0f;
        }

        /// <summary>Techos duplicados (la misma casa dos veces): se queda el más alto.</summary>
        private static void DescartarContenidos()
        {
            foreach (Techo a in techos)
            {
                foreach (Techo o in techos)
                {
                    if (o == a || o.descartado)
                    {
                        continue;
                    }
                    bool contenido = Solape(a.b, o.b) >= 0.7f * Area(a.b);
                    bool oMejor = o.top > a.top + 0.01f || (Mathf.Abs(o.top - a.top) <= 0.01f && o.indice < a.indice);
                    if (contenido && oMejor)
                    {
                        a.descartado = true;
                        break;
                    }
                }
            }
            techos = techos.Where(t => !t.descartado).ToList();
            for (int i = 0; i < techos.Count; i++)
            {
                techos[i].indice = i;
            }
        }

        /// <summary>La cara de arriba del collider debe coincidir con el techo visible; si no, se agrega una caja TechoCaminable.</summary>
        private static void AsegurarCaminables()
        {
            Transform padre = Grupo("TechosCaminables");
            int agregados = 0;
            foreach (Techo t in techos)
            {
                int bien = 0;
                for (int i = 0; i < 5; i++)
                {
                    Vector3 p = t.b.center + new Vector3(i == 1 ? -t.b.extents.x * 0.5f : i == 2 ? t.b.extents.x * 0.5f : 0f, 0f, i == 3 ? -t.b.extents.z * 0.5f : i == 4 ? t.b.extents.z * 0.5f : 0f);
                    foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(p.x, t.top + 6f, p.z), Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (Mathf.Abs(hit.point.y - t.top) <= TolTecho)
                        {
                            bien++;
                            break;
                        }
                    }
                }
                if (bien >= 3)
                {
                    continue;
                }
                GameObject go = new GameObject("TechoCaminable_" + t.casa.parent.name + "_" + t.casa.name);
                go.transform.SetParent(padre, false);
                BoxCollider bc = go.AddComponent<BoxCollider>();
                go.transform.position = new Vector3(t.b.center.x, t.top - 0.15f, t.b.center.z);
                bc.size = new Vector3(t.b.size.x, 0.3f, t.b.size.z);
                agregados++;
                ColocarS2.Informe.AppendLine("  TechoCaminable en " + t.Nombre + " (visible " + t.top.ToString("F2") + ", sondeo " + bien + "/5)");
            }
            ColocarS2.Informe.AppendLine("Techos que necesitaron TechoCaminable: " + agregados + " de " + techos.Count);
        }

        private static void Agrupar()
        {
            int g = 0;
            foreach (Techo t in techos)
            {
                if (t.grupo >= 0)
                {
                    continue;
                }
                Queue<Techo> cola = new Queue<Techo>();
                t.grupo = g;
                cola.Enqueue(t);
                while (cola.Count > 0)
                {
                    Techo a = cola.Dequeue();
                    foreach (Techo o in techos)
                    {
                        if (o.grupo < 0 && o.zona == a.zona && Vecinos(a, o))
                        {
                            o.grupo = g;
                            cola.Enqueue(o);
                        }
                    }
                }
                g++;
            }
        }

        private static float Hueco(Bounds a, Bounds b)
        {
            float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
            float dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool Vecinos(Techo a, Techo o)
        {
            return Hueco(a.b, o.b) <= HuecoMax && Mathf.Abs(a.top - o.top) <= DifAltoMax;
        }

        private static IEnumerable<IGrouping<int, Techo>> Grupos()
        {
            return techos.GroupBy(t => t.grupo);
        }

        private static void PonerPropsSolidos()
        {
            int n = 0;
            foreach (Techo t in techos)
            {
                foreach (Transform hijo in t.casa.GetComponentsInChildren<Transform>(true))
                {
                    if (!NombresProps.Contains(hijo.name) || hijo.position.y < t.top - 0.3f)
                    {
                        continue;
                    }
                    foreach (Collider c in hijo.GetComponents<Collider>())
                    {
                        if (!c.enabled || c.isTrigger)
                        {
                            c.enabled = true;
                            c.isTrigger = false;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                            n++;
                        }
                    }
                }
            }
            ColocarS2.Informe.AppendLine("Colliders sólidos activados en tinacos, bases, aires y antenas: " + n);
        }

        private static void ConstruirObstaculos()
        {
            obstaculos = new List<Bounds>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || !r.enabled || r.transform.IsChildOf(raiz))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.y < 0.5f || b.size.x > 40f || b.size.z > 40f || b.max.y < 0.3f)
                {
                    continue;
                }
                obstaculos.Add(b);
            }
        }

        // ------------------------------------------------------------------ geometría de muros

        private static float Extension(Bounds b, Vector3 dir)
        {
            return Mathf.Abs(dir.x) * b.extents.x + Mathf.Abs(dir.z) * b.extents.z;
        }

        private static void Muro(Techo t, int lado, out Vector3 n, out Vector3 u, out float a0, out float a1)
        {
            n = Dirs[lado];
            u = Vector3.Cross(Vector3.up, n);
            float c = Vector3.Dot(t.b.center, u);
            float e = Extension(t.b, u);
            a0 = c - e;
            a1 = c + e;
        }

        private static Vector3 PuntoMuro(Techo t, Vector3 n, Vector3 u, float a)
        {
            float cn = Vector3.Dot(t.b.center, n) + Extension(t.b, n);
            return u * a + n * cn;
        }

        private static Vector3 Mitad(Vector3 n, float largoU, float alto, float profundo)
        {
            return Mathf.Abs(n.x) > 0.5f ? new Vector3(profundo, alto, largoU) : new Vector3(largoU, alto, profundo);
        }

        private static bool ChocaRender(Vector3 centro, Vector3 mitad)
        {
            Bounds caja = new Bounds(centro, mitad * 2f);
            foreach (Bounds o in obstaculos)
            {
                if (o.Intersects(caja))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Caja vacía: sin colliders sólidos ni modelos visibles. Profundidad desde el muro (d0 a d1), a lo largo del muro largoU/2 a cada lado, altura de y0+h0 a y0+h1.</summary>
        private static bool Libre(Vector3 w, Vector3 n, float largoMitad, float d0, float d1, float y0, float h0, float h1)
        {
            Vector3 centro = w + n * ((d0 + d1) * 0.5f);
            centro.y = y0 + (h0 + h1) * 0.5f;
            Vector3 mitad = Mitad(n, largoMitad, (h1 - h0) * 0.5f, (d1 - d0) * 0.5f);
            return !Physics.CheckBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore) && !ChocaRender(centro, mitad);
        }

        private static bool Suelo(Vector3 p, out float y)
        {
            y = 0f;
            if (!Physics.Raycast(new Vector3(p.x, 2.5f, p.z), Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            y = hit.point.y;
            return hit.normal.y >= 0.8f && hit.point.y <= SueloMaxY;
        }

        /// <summary>Muro plano en todo el ancho (muestras a lo largo) y suelo parejo delante. Devuelve el suelo en y0.</summary>
        private static bool ParedPlana(Techo t, Vector3 n, Vector3 u, Vector3 w, float mitadAncho, float profSuelo, out float y0)
        {
            y0 = 0f;
            if (!Suelo(w + n * 0.4f, out y0))
            {
                return false;
            }
            float alto = t.top - y0;
            if (alto < AlturaMinAcceso)
            {
                return false;
            }
            int muestras = mitadAncho > 1f ? 5 : 3;
            for (int i = 0; i < muestras; i++)
            {
                float o = muestras == 1 ? 0f : Mathf.Lerp(-mitadAncho, mitadAncho, i / (float)(muestras - 1));
                Vector3 p = w + u * o;
                for (float d = 0.4f; d <= profSuelo; d += 0.8f)
                {
                    if (!Suelo(p + n * d, out float ys) || Mathf.Abs(ys - y0) > 0.12f)
                    {
                        return false;
                    }
                }
                foreach (float h in new[] { 0.4f, alto * 0.5f, alto - 0.4f })
                {
                    Vector3 origen = p + n * 0.5f + Vector3.up * (y0 + h);
                    if (!Physics.Raycast(origen, -n, out RaycastHit hit, 0.9f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.distance - 0.5f) > 0.1f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Hay un techo caminable de pie justo detrás del muro, donde sale la escalera.</summary>
        private static bool SalidaBuena(Techo t, Vector3 n, Vector3 w)
        {
            Vector3 s = w - n * 0.8f;
            if (!Physics.Raycast(new Vector3(s.x, t.top + 1f, s.z), Vector3.down, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(hit.point.y - t.top) > 0.08f)
            {
                return false;
            }
            return !Physics.CheckCapsule(new Vector3(s.x, t.top + 0.4f, s.z), new Vector3(s.x, t.top + 1.6f, s.z), 0.3f, ~0, QueryTriggerInteraction.Ignore);
        }

        private static bool CercaDeSalida(Vector3 p, float radio)
        {
            foreach (Vector3 s in salidas)
            {
                Vector2 d = new Vector2(p.x - s.x, p.z - s.z);
                if (d.magnitude < radio)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ elegir y crear accesos

        private static void ElegirAccesos()
        {
            // Cuántos accesos por grupo: uno por cada ~30 m de largo. Un techo suelto solo si es grande.
            Dictionary<string, int> porZona = ZonasCasas.Concat(new[] { "Barrio" }).ToDictionary(z => z, z => accesos.Count(a => a.zona == z));
            List<(IGrouping<int, Techo> grupo, int meta, float largo)> lista = new List<(IGrouping<int, Techo>, int, float)>();
            foreach (IGrouping<int, Techo> g in Grupos())
            {
                Bounds u = g.First().b;
                foreach (Techo t in g)
                {
                    u.Encapsulate(t.b);
                }
                float largo = Mathf.Max(u.size.x, u.size.z);
                int meta = g.Count() >= 2 ? Mathf.Clamp(Mathf.CeilToInt(largo / 30f), 1, 5) : (Area(g.First().b) >= 45f ? 1 : 0);
                lista.Add((g, meta, largo));
            }
            foreach (var e in lista)
            {
                Rellenar(e.grupo.ToList(), e.meta, 10f);
            }
            // Cada zona de casas debe tener al menos 3: se suman donde más largo queda por cubrir.
            foreach (string zona in ZonasCasas)
            {
                int seguridad = 0;
                while (accesos.Count(a => a.zona == zona) < 3 && seguridad++ < 6)
                {
                    var mejor = lista.Where(e => e.grupo.First().zona == zona).OrderByDescending(e => e.largo / (accesos.Count(a => a.techo.grupo == e.grupo.Key) + 1)).FirstOrDefault();
                    if (mejor.grupo == null || !Rellenar(mejor.grupo.ToList(), accesos.Count(a => a.techo.grupo == mejor.grupo.Key) + 1, 7f))
                    {
                        break;
                    }
                }
            }
            // Tubos: el segundo acceso de cada zona (y el sexto si hay muchos) es un tubo de desagüe.
            foreach (string zona in ZonasCasas.Concat(new[] { "Barrio" }))
            {
                List<Acceso> deZona = accesos.Where(a => a.zona == zona && a.ruta == null).ToList();
                for (int i = 0; i < deZona.Count; i++)
                {
                    if (i == 1 || i == 5)
                    {
                        deZona[i].tipo = TipoTrepar.Tubo;
                    }
                }
            }
        }

        /// <summary>Agrega accesos al grupo hasta tener meta, separados al menos separacion m. Devuelve si agregó alguno.</summary>
        private static bool Rellenar(List<Techo> grupo, int meta, float separacion)
        {
            bool agrego = false;
            Bounds u = grupo[0].b;
            foreach (Techo t in grupo)
            {
                u.Encapsulate(t.b);
            }
            List<Acceso> propios = accesos.Where(a => a.techo.grupo == grupo[0].grupo).ToList();
            int intentos = 0;
            while (propios.Count < meta && intentos++ < 8)
            {
                Acceso mejor = null;
                float mejorPuntaje = -1f;
                foreach (Techo t in grupo)
                {
                    for (int lado = 0; lado < 4; lado++)
                    {
                        Muro(t, lado, out Vector3 n, out Vector3 u2, out float a0, out float a1);
                        for (float a = a0 + 1.0f; a <= a1 - 1.0f; a += 0.75f)
                        {
                            Vector3 w = PuntoMuro(t, n, u2, a);
                            if (CercaDeSalida(w - n * 0.8f, separacion) && separacion > 0f && propios.Any(p => Vector2.Distance(new Vector2(p.w.x, p.w.z), new Vector2(w.x, w.z)) < separacion))
                            {
                                continue;
                            }
                            float minimo = propios.Count == 0 ? 100f - Vector2.Distance(new Vector2(w.x, w.z), new Vector2(u.center.x, u.center.z)) : propios.Min(p => Vector2.Distance(new Vector2(p.w.x, p.w.z), new Vector2(w.x, w.z)));
                            if (minimo < separacion || minimo <= mejorPuntaje)
                            {
                                continue;
                            }
                            if (!ParedPlana(t, n, u2, w, 0.4f, 1.6f, out float y0) || !SalidaBuena(t, n, w + Vector3.up * y0))
                            {
                                continue;
                            }
                            if (!Libre(w, n, 0.5f, 0.12f, 1.6f, y0, 0.25f, 2.5f))
                            {
                                continue;
                            }
                            mejorPuntaje = minimo;
                            mejor = new Acceso { techo = t, n = n, w = new Vector3(w.x, y0, w.z), baseY = y0, zona = t.zona };
                        }
                    }
                }
                if (mejor == null)
                {
                    break;
                }
                accesos.Add(mejor);
                propios.Add(mejor);
                salidas.Add(mejor.w - mejor.n * 0.8f);
                agrego = true;
            }
            return agrego;
        }

        private static void CrearAccesos()
        {
            Transform padre = Grupo("Accesos");
            Dictionary<string, int> n = new Dictionary<string, int>();
            foreach (Acceso a in accesos.Where(a => a.ruta == null))
            {
                n[a.zona] = n.TryGetValue(a.zona, out int k) ? k + 1 : 1;
                CrearPuntoTrepar(padre, "Acceso_" + a.zona + "_" + n[a.zona].ToString("00"), a);
            }
        }

        private static GameObject CrearPuntoTrepar(Transform padre, string nombre, Acceso a)
        {
            float alt = a.techo.top - a.baseY;
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(new Vector3(a.w.x, a.baseY, a.w.z), Quaternion.LookRotation(-a.n));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)a.tipo;
            so.FindProperty("alturaSubida").floatValue = alt;
            so.FindProperty("salida").vector3Value = new Vector3(0f, alt, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
            GameObject visual = a.tipo == TipoTrepar.Tubo ? MallaTubo(go.transform, alt) : MallaEscalera(go.transform, alt);
            visual.name = a.tipo == TipoTrepar.Tubo ? "TuboGreybox" : "EscaleraGreybox";
            return go;
        }

        // ------------------------------------------------------------------ greybox de escalera y tubo

        private static GameObject MallaEscalera(Transform padre, float alt)
        {
            Mesh cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            List<CombineInstance> partes = new List<CombineInstance>();
            const float distancia = 0.13f;
            float alto = alt + 0.9f;
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                Agregar(partes, cubo, new Vector3(x, alto * 0.5f, -distancia), new Vector3(0.06f, alto, 0.05f));
                for (float y = 0.4f; y < alto; y += 1.5f)
                {
                    Agregar(partes, cubo, new Vector3(x, y, -distancia * 0.5f), new Vector3(0.04f, 0.04f, distancia));
                }
            }
            for (float y = PasoEscalera; y < alto; y += PasoEscalera)
            {
                Agregar(partes, cubo, new Vector3(0f, y, -distancia), new Vector3(0.6f, 0.03f, 0.03f));
            }
            return ObjetoMalla(padre, "escalera", partes, matEscalera);
        }

        private static GameObject MallaTubo(Transform padre, float alt)
        {
            Mesh cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Mesh cilindro = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            List<CombineInstance> partes = new List<CombineInstance>();
            float alto = alt + 0.3f;
            const float radio = 0.06f;
            Agregar(partes, cilindro, new Vector3(0f, alto * 0.5f, -radio - 0.02f), new Vector3(radio * 2f, alto * 0.5f, radio * 2f));
            for (float y = 0.5f; y < alto; y += 1.2f)
            {
                Agregar(partes, cilindro, new Vector3(0f, y, -radio - 0.02f), new Vector3(0.17f, 0.03f, 0.17f));
                Agregar(partes, cubo, new Vector3(0f, y, -0.015f), new Vector3(0.03f, 0.03f, 0.04f));
            }
            return ObjetoMalla(padre, "tubo", partes, matTubo);
        }

        private static void Agregar(List<CombineInstance> partes, Mesh malla, Vector3 centro, Vector3 escala)
        {
            partes.Add(new CombineInstance { mesh = malla, transform = Matrix4x4.TRS(centro, Quaternion.identity, escala) });
        }

        private static int contadorMallas;

        private static GameObject ObjetoMalla(Transform padre, string prefijo, List<CombineInstance> partes, Material mat)
        {
            Mesh m = new Mesh();
            m.CombineMeshes(partes.ToArray(), true, true);
            m.name = prefijo + "_" + (++contadorMallas).ToString("000");
            AssetDatabase.CreateAsset(m, RutaMallas + "/" + m.name + ".asset");
            GameObject go = new GameObject("Visual");
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        // ------------------------------------------------------------------ parkour

        private static GameObject Prefab(Transform padre, string ruta, Vector3 pos, Quaternion rot, Vector3 escala)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                ColocarS2.Informe.AppendLine("  FALTA " + ruta);
                return null;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.transform.SetPositionAndRotation(pos, rot);
            g.transform.localScale = escala;
            foreach (Transform t in g.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }
            return g;
        }

        /// <summary>Ruta A: techo bajo (como Barrio/Edificio_7, 3,8 m): un contenedor L01 pegado al muro y de ahí se agarra el borde.</summary>
        private static void ParkourContenedorSolo()
        {
            Transform padre = GrupoParkour();
            Dictionary<string, int> motivos = new Dictionary<string, int>();
            foreach (Techo t in techos.Where(t => t.zona == "Barrio" && t.top <= 4.6f))
            {
                for (int lado = 0; lado < 4; lado++)
                {
                    Muro(t, lado, out Vector3 n, out Vector3 u, out float a0, out float a1);
                    bool puesto = false;
                    for (float a = a0 + 1.2f; a <= a1 - 1.2f && !puesto; a += 0.5f)
                    {
                        Vector3 w = PuntoMuro(t, n, u, a);
                        if (!Suelo(w + n * 0.4f, out float y0))
                        {
                            continue;
                        }
                        float subida = t.top - (y0 + AlturaContenedor);
                        if (subida < AgarreMin || subida > AgarreMax - 0.05f)
                        {
                            continue;
                        }
                        string motivo = !MuroPlano(t, n, u, w, y0, 1.0f) ? "muro" : !Libre(w, n, 1.3f, 0.1f, FondoContenedor + 0.15f + 1.4f, y0, 0.1f, 2.6f) ? "libre" : !SalidaBuena(t, n, new Vector3(w.x, y0, w.z)) ? "salida" : "";
                        if (motivo == "")
                        {
                            motivo = !Suelo(w + n * (FondoContenedor + 0.5f), out float yc) || Mathf.Abs(yc - y0) > 0.12f ? "suelo" : "";
                        }
                        if (motivo != "")
                        {
                            motivos[motivo + lado] = motivos.TryGetValue(motivo + lado, out int mc) ? mc + 1 : 1;
                            continue;
                        }
                        contadorRutas++;
                        totalRutasA++;
                        Transform ruta = new GameObject("Ruta_" + t.zona + "_" + contadorRutas.ToString("00") + "_ContenedorSolo").transform;
                        ruta.SetParent(padre, false);
                        Prefab(ruta, RutaContenedor, new Vector3(w.x, y0, w.z) + n * (FondoContenedor * 0.5f + 0.03f), Quaternion.LookRotation(n), Vector3.one);
                        salidas.Add(w - n * 0.8f);
                        ColocarS2.Informe.AppendLine("Ruta A (contenedor solo) en " + t.Nombre + " lado " + n + " a " + a.ToString("F1") + ": techo " + t.top.ToString("F2") + " sobre suelo " + y0.ToString("F2"));
                        puesto = true;
                    }
                    if (puesto)
                    {
                        break;
                    }
                }
                ColocarS2.Informe.AppendLine("  Ruta A " + t.Nombre + " rechazos: " + string.Join(", ", motivos.Select(k => k.Key + "=" + k.Value)));
            }
        }

        private static bool MuroPlano(Techo t, Vector3 n, Vector3 u, Vector3 w, float y0, float mitadAncho)
        {
            float alto = t.top - y0;
            for (int i = 0; i < 5; i++)
            {
                float o = Mathf.Lerp(-mitadAncho, mitadAncho, i / 4f);
                foreach (float h in new[] { 0.4f, alto * 0.5f, alto - 0.4f })
                {
                    Vector3 origen = w + u * o + n * 0.5f + Vector3.up * (y0 + h);
                    if (!Physics.Raycast(origen, -n, out RaycastHit hit, 0.9f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.distance - 0.5f) > 0.1f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Ruta B: contenedor L01 (1,61 m) delante de una barda MT01 escalada a 3,5 m pegada a la pared de una casa; arriba de la barda,
        /// una escalera de servicio corta hasta el techo. Cada salto queda entre 0,5 y 2,3 m.
        /// </summary>
        private static void ParkourBarda()
        {
            Transform padre = GrupoParkour();
            Dictionary<string, int> metas = new Dictionary<string, int> { { "S2c_Suburbios", 3 }, { "S2c_CalleDorada", 2 }, { "S2c_Mercado", 2 } };
            List<Vector3> hechas = new List<Vector3>();
            foreach (KeyValuePair<string, int> kv in metas)
            {
                int ya = 0;
                foreach (Techo t in techos.Where(t => t.zona == kv.Key && t.top >= 5.2f && t.top <= 7.2f))
                {
                    if (ya >= kv.Value)
                    {
                        break;
                    }
                    for (int lado = 0; lado < 4 && ya < kv.Value; lado++)
                    {
                        Muro(t, lado, out Vector3 n, out Vector3 u, out float a0, out float a1);
                        for (float a = a0 + 0.8f; a <= a1 - 0.8f; a += 0.5f)
                        {
                            Vector3 w = PuntoMuro(t, n, u, a);
                            if (hechas.Any(h => Vector2.Distance(new Vector2(h.x, h.z), new Vector2(w.x, w.z)) < 16f))
                            {
                                break;
                            }
                            if (!ParedPlana(t, n, u, w, 2.3f, 0.4f, out float y0))
                            {
                                continue;
                            }
                            float subidaBarda = AlturaBarda - AlturaContenedor;
                            if (subidaBarda < AgarreMin || subidaBarda > AgarreMax - 0.1f || t.top - y0 < AlturaBarda + 1f)
                            {
                                continue;
                            }
                            float fondoTotal = FondoBarda + 0.04f + FondoContenedor;
                            if (!Libre(w, n, 2.6f, 0.1f, fondoTotal + 0.2f + 1.4f, y0, 0.1f, AlturaBarda + 0.1f))
                            {
                                continue;
                            }
                            if (!ComprobarSuelos(w, n, u, y0, fondoTotal + 1.4f) || !SalidaBuena(t, n, new Vector3(w.x, y0, w.z)))
                            {
                                continue;
                            }
                            contadorRutas++;
                            totalRutasB++;
                            ya++;
                            hechas.Add(w);
                            Transform ruta = new GameObject("Ruta_" + t.zona + "_" + contadorRutas.ToString("00") + "_BardaYEscalera").transform;
                            ruta.SetParent(padre, false);
                            Vector3 pie = new Vector3(w.x, y0, w.z);
                            Quaternion rot = Quaternion.LookRotation(n);
                            GameObject barda = Prefab(ruta, RutaBarda, pie + n * (FondoBarda * 0.5f + 0.02f), rot, new Vector3(1f, AlturaBarda / AlturaBardaPrefab, 1f));
                            if (barda != null)
                            {
                                barda.name = "Barda";
                            }
                            GameObject cont = Prefab(ruta, RutaContenedor, pie + n * (FondoBarda + 0.04f + FondoContenedor * 0.5f), rot, Vector3.one);
                            if (cont != null)
                            {
                                cont.name = "Contenedor";
                            }
                            Acceso final = new Acceso { techo = t, n = n, w = new Vector3(w.x, y0 + AlturaBarda, w.z), baseY = y0 + AlturaBarda, zona = t.zona, ruta = ruta.name };
                            accesos.Add(final);
                            salidas.Add(w - n * 0.8f);
                            GameObject punto = CrearPuntoTrepar(ruta, "EscaleraDeLaBarda", final);
                            ColocarS2.Informe.AppendLine("Ruta B " + ruta.name + " en " + t.Nombre + ": suelo " + y0.ToString("F2") + ", techo " + t.top.ToString("F2"));
                            break;
                        }
                    }
                }
            }
        }

        private static bool ComprobarSuelos(Vector3 w, Vector3 n, Vector3 u, float y0, float fondo)
        {
            for (float o = -2.4f; o <= 2.41f; o += 1.2f)
            {
                for (float d = 0.4f; d <= fondo; d += 0.9f)
                {
                    if (!Suelo(w + u * o + n * d, out float ys) || Mathf.Abs(ys - y0) > 0.12f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // ------------------------------------------------------------------ pretiles

        private static bool Exposido(Techo t, Vector3 puntoBorde, Vector3 n, float mitadAncho)
        {
            // Región justo afuera del borde: si algún techo vecino del grupo la toca, ahí es un salto y no va pretil.
            Vector3 centro = puntoBorde + n * (0.05f + HuecoMax * 0.5f);
            Vector3 mitad = Mitad(n, mitadAncho, 1f, HuecoMax * 0.5f - 0.025f);
            Bounds region = new Bounds(centro, mitad * 2f);
            foreach (Techo o in techos)
            {
                if (o != t && o.grupo == t.grupo && Mathf.Abs(o.top - t.top) <= DifAltoMax)
                {
                    Bounds ob = o.b;
                    Vector3 c = ob.center;
                    c.y = 0f;
                    ob.center = c;
                    Bounds r = region;
                    r.center = new Vector3(r.center.x, 0f, r.center.z);
                    if (ob.Intersects(r))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static int pretilesNuevos;
        private static int pretilesExistentes;

        private static void ActivarPretilesExistentes()
        {
            foreach (Techo t in techos.Where(t => ConAcceso(t.grupo)))
            {
                foreach (Transform hijo in t.casa.GetComponentsInChildren<Transform>(true))
                {
                    if (hijo.name != "C05_PretilAzotea" || hijo.position.y < t.top - 0.3f)
                    {
                        continue;
                    }
                    Collider c = hijo.GetComponent<Collider>();
                    if (c == null || c.enabled)
                    {
                        continue;
                    }
                    // Lado más cercano.
                    int mejor = 0;
                    float dMin = float.MaxValue;
                    for (int lado = 0; lado < 4; lado++)
                    {
                        float d = Mathf.Abs(Vector3.Dot(hijo.position - t.b.center, Dirs[lado]) - Extension(t.b, Dirs[lado]));
                        if (d < dMin)
                        {
                            dMin = d;
                            mejor = lado;
                        }
                    }
                    if (dMin > 1.2f || CercaDeSalida(hijo.position, LibreSalida + 0.9f) || !Exposido(t, hijo.position, Dirs[mejor], 0.9f))
                    {
                        continue;
                    }
                    c.enabled = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                    pretilesExistentes++;
                }
            }
            Physics.SyncTransforms();
        }

        private static bool ConAcceso(int grupo)
        {
            return accesos.Any(a => a.techo.grupo == grupo) || salidas.Any(s => techos.Any(t => t.grupo == grupo && s.x >= t.b.min.x - 1f && s.x <= t.b.max.x + 1f && s.z >= t.b.min.z - 1f && s.z <= t.b.max.z + 1f));
        }

        private static void PonerPretiles()
        {
            Transform padre = Grupo("Pretiles");
            foreach (Techo t in techos.Where(t => ConAcceso(t.grupo)))
            {
                for (int lado = 0; lado < 4; lado++)
                {
                    Muro(t, lado, out Vector3 n, out Vector3 u, out float a0, out float a1);
                    float largo = a1 - a0;
                    int k = Mathf.Max(1, Mathf.RoundToInt(largo / LargoPretil));
                    float w = largo / k;
                    for (int i = 0; i < k; i++)
                    {
                        float a = a0 + (i + 0.5f) * w;
                        Vector3 borde = PuntoMuro(t, n, u, a);
                        borde.y = t.top;
                        if (!Exposido(t, borde, n, w * 0.5f))
                        {
                            continue;
                        }
                        Vector3 pos = borde - n * (FondoPretil * 0.5f + 0.02f);
                        if (CercaDeSalida(pos, LibreSalida))
                        {
                            continue;
                        }
                        Vector3 centroCaja = pos + Vector3.up * 0.55f;
                        Vector3 mitad = Mitad(n, w * 0.45f, 0.45f, FondoPretil * 0.45f);
                        if (Physics.CheckBox(centroCaja, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        GameObject p = Prefab(padre, RutaPretil, pos, Quaternion.LookRotation(n), new Vector3(w / LargoPretil, 1f, 1f));
                        if (p != null)
                        {
                            Physics.SyncTransforms();
                            pretilesNuevos++;
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ tablones y saltos

        private static void Tablones()
        {
            Transform padre = Grupo("Tablones");
            int hechos = 0;
            HashSet<string> zonasUsadas = new HashSet<string>();
            List<(Techo a, Techo b, float hueco)> pares = new List<(Techo, Techo, float)>();
            foreach (Techo a in techos)
            {
                foreach (Techo b in techos)
                {
                    if (b.indice <= a.indice || a.grupo == b.grupo || a.zona != b.zona || Mathf.Abs(a.top - b.top) > 0.3f)
                    {
                        continue;
                    }
                    float h = Hueco(a.b, b.b);
                    if (h >= TablonMin + 0.1f && h <= TablonMax && ConAcceso(a.grupo) && ConAcceso(b.grupo))
                    {
                        pares.Add((a, b, h));
                    }
                }
            }
            foreach (var par in pares.OrderBy(p => p.hueco))
            {
                if (hechos >= 3)
                {
                    break;
                }
                Techo a = par.a;
                Techo b = par.b;
                float dx = Mathf.Max(a.b.min.x - b.b.max.x, b.b.min.x - a.b.max.x);
                float dz = Mathf.Max(a.b.min.z - b.b.max.z, b.b.min.z - a.b.max.z);
                bool enX = dx > 0f && dz <= 0f; // se separan a lo largo de X
                bool enZ = dz > 0f && dx <= 0f;
                if (!enX && !enZ)
                {
                    continue;
                }
                float solapeMin = enX ? Mathf.Max(a.b.min.z, b.b.min.z) : Mathf.Max(a.b.min.x, b.b.min.x);
                float solapeMax = enX ? Mathf.Min(a.b.max.z, b.b.max.z) : Mathf.Min(a.b.max.x, b.b.max.x);
                if (solapeMax - solapeMin < 2f || zonasUsadas.Contains(a.zona + a.grupo))
                {
                    continue;
                }
                float c = (solapeMin + solapeMax) * 0.5f;
                Techo bajo = a.b.center.x + a.b.center.z < b.b.center.x + b.b.center.z ? a : b;
                Techo alto = bajo == a ? b : a;
                float y = Mathf.Max(a.top, b.top);
                // Del centro de un techo, 0,5 m adentro de su borde, al otro.
                Vector3 ini, fin;
                if (enX)
                {
                    float x0 = a.b.center.x < b.b.center.x ? a.b.max.x - 0.5f : a.b.min.x + 0.5f;
                    float x1 = a.b.center.x < b.b.center.x ? b.b.min.x + 0.5f : b.b.max.x - 0.5f;
                    ini = new Vector3(x0, y, c);
                    fin = new Vector3(x1, y, c);
                }
                else
                {
                    float z0 = a.b.center.z < b.b.center.z ? a.b.max.z - 0.5f : a.b.min.z + 0.5f;
                    float z1 = a.b.center.z < b.b.center.z ? b.b.min.z + 0.5f : b.b.max.z - 0.5f;
                    ini = new Vector3(c, y, z0);
                    fin = new Vector3(c, y, z1);
                }
                Vector3 mid = (ini + fin) * 0.5f;
                float largo = Vector3.Distance(ini, fin);
                Vector3 dir = (fin - ini).normalized;
                // Espacio libre sobre el hueco y sobre los apoyos.
                Vector3 mitadCaja = new Vector3(enX ? (par.hueco * 0.5f) : 0.4f, 0.8f, enX ? 0.4f : (par.hueco * 0.5f));
                if (Physics.CheckBox(mid + Vector3.up * 1.0f, mitadCaja, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                GameObject tab = new GameObject("Tablon_" + a.zona + "_" + (hechos + 1).ToString("00"));
                tab.transform.SetParent(padre, false);
                tab.transform.SetPositionAndRotation(new Vector3(mid.x, y, mid.z), Quaternion.LookRotation(dir));
                tab.transform.localScale = new Vector3(TablonAncho, TablonGrueso, largo);
                GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(cubo.GetComponent<BoxCollider>());
                cubo.transform.SetParent(tab.transform, false);
                cubo.name = "Visual";
                cubo.GetComponent<MeshRenderer>().sharedMaterial = matTablon;
                tab.AddComponent<BoxCollider>();
                zonasUsadas.Add(a.zona + a.grupo);
                hechos++;
                ColocarS2.Informe.AppendLine("Tablón " + tab.name + " entre " + a.Nombre + " (" + a.top.ToString("F2") + ") y " + b.Nombre + " (" + b.top.ToString("F2") + "), hueco " + par.hueco.ToString("F1") + " m, largo " + largo.ToString("F1") + " m");
            }
            ColocarS2.Informe.AppendLine("Tablones: " + hechos);
        }

        private static void SaltosNaturales()
        {
            int total = 0;
            foreach (IGrouping<int, Techo> g in Grupos().Where(g => g.Count() >= 2))
            {
                List<Techo> l = g.ToList();
                for (int i = 0; i < l.Count; i++)
                {
                    for (int j = i + 1; j < l.Count; j++)
                    {
                        float h = Hueco(l[i].b, l[j].b);
                        if (h > SaltoNaturalMin && h <= HuecoMax)
                        {
                            total++;
                            ColocarS2.Informe.AppendLine("  Salto natural: " + l[i].Nombre + " -> " + l[j].Nombre + " hueco " + h.ToString("F1") + " m, desnivel " + Mathf.Abs(l[i].top - l[j].top).ToString("F2") + " m");
                        }
                    }
                }
            }
            ColocarS2.Informe.AppendLine("Saltos naturales (hueco mayor a " + SaltoNaturalMin + " m y hasta " + HuecoMax + " m dentro de un grupo): " + total);
        }

        private static void Resumen()
        {
            foreach (Techo t in techos.Where(t => t.zona == "Barrio"))
            {
                ColocarS2.Informe.AppendLine("  Barrio " + t.Nombre + " techo " + t.top.ToString("F2") + " collider " + t.b.max.y.ToString("F2") + " centro " + t.b.center + " tam " + t.b.size);
            }
            ColocarS2.Informe.AppendLine("Techos: " + techos.Count + " en " + Grupos().Count() + " grupos");
            foreach (string z in ZonasCasas.Concat(new[] { "Barrio" }))
            {
                IEnumerable<Techo> deZona = techos.Where(t => t.zona == z);
                ColocarS2.Informe.AppendLine("  " + z + ": " + deZona.Count() + " techos, " + deZona.Select(t => t.grupo).Distinct().Count() + " grupos, accesos " + accesos.Count(a => a.zona == z && a.ruta == null)
                    + " (tubos " + accesos.Count(a => a.zona == z && a.ruta == null && a.tipo == TipoTrepar.Tubo) + "), escaleras de ruta " + accesos.Count(a => a.zona == z && a.ruta != null));
            }
            ColocarS2.Informe.AppendLine("Rutas de parkour: " + (totalRutasA + totalRutasB) + " (contenedor solo " + totalRutasA + ", barda y escalera " + totalRutasB + ")");
            ColocarS2.Informe.AppendLine("Pretiles existentes activados: " + pretilesExistentes + ", nuevos: " + pretilesNuevos);
            ColocarS2.Informe.AppendLine("Accesos en total (PuntoTrepar): " + accesos.Count);
        }
    }
}
