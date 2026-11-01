using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-505 (paso A5 del plan de ciudad): viste Neón Alto por fuera (la calle cruzada x 37 a 57 entre Manzana_2 y Manzana_3, con la entrada del casino
    /// en el muro este a z 47) como neon-alto-noche: dos tramos de autopista elevada N02 que cruzan la calle por arriba (sin collider: la cubierta
    /// queda a más de 7 m), vallas N07 en las orillas, anuncios verticales N08 y CY02 y pantallas gigantes E03 pegados a las fachadas (con brillo
    /// propio), la entrada E01 del casino con foquitos, tubos de neón y alfombra, cargadores CY10 y una moto, torres de fondo verdosas, niebla verde,
    /// charcos y un posproceso local verde solo de noche. Todo cuelga de <see cref="NombreRaiz"/> y se rehace al correrlo otra vez (idempotente).
    /// No toca el casino (Lugar_Casino), su puerta ni su horario: deja libre el corredor <see cref="CorredorCasino"/> hasta la puerta.
    /// </summary>
    public static class ColocarT505
    {
        public const string NombreRaiz = "T505_NeonAlto";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Exteriores";
        private const string CarpetaMateriales = Carpeta + "/Materiales";
        private const string RutaPerfil = Carpeta + "/PerfilNeonAlto.asset";
        private const string CarpetaSuburbios = "Assets/Arte/Suburbios/Texturas";
        private const string RutaShaderTorre = "MechaCyberpunk/TorreHorizonte";

        /// <summary>Caras de las fachadas oeste y este de la calle, y tramo con piezas (m).</summary>
        public const float XOeste = 37f;
        public const float XEste = 57f;
        public const float ZMin = 8f;
        public const float ZMax = 86f;
        /// <summary>z del centro de cada tramo de autopista y escala en X (pilares dentro de los edificios).</summary>
        public static readonly float[] ZPuentes = { 38f, 72f };
        public const float EscalaPuente = 2f;
        /// <summary>Corredor de la calle a la puerta del casino (x, z mínimos y tamaño) que ninguna pieza con collider de T505 toca.</summary>
        public static readonly Rect CorredorCasino = new Rect(37f, 44.5f, 20.5f, 5f);
        public const int TorresMinimas = 10;

        private static readonly StringBuilder Informe = new StringBuilder();
        private static Transform raiz;
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly Color Magenta = new Color(1f, 0.15f, 0.8f);
        private static readonly Color Verde = new Color(0.45f, 1f, 0.2f);
        private static readonly Color Turquesa = new Color(0.1f, 0.95f, 0.85f);
        private static readonly Color Calido = new Color(1f, 0.62f, 0.2f);

        /// <summary>El punto de entrada del casino (donde el jugador abre la puerta).</summary>
        public static Vector3 Entrada
        {
            get
            {
                Transform t = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(x => x.name == "Entrada" && x.parent != null && x.parent.name == "Lugar_Casino");
                return t != null ? t.position : new Vector3(55.5f, 0.2f, 47f);
            }
        }

        [MenuItem("Mecha Cyberpunk/T-505/Colocar Neón Alto")]
        public static void Colocar()
        {
            Informe.Clear();
            Cache.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Physics.SyncTransforms();
            Directory.CreateDirectory(CarpetaMateriales);
            ColocarNoche.ReunirObstaculos();
            Vector3 entrada = Entrada;
            ColocarNoche.obstaculos.Add(new Bounds(new Vector3(CorredorCasino.center.x, 1.5f, CorredorCasino.center.y), new Vector3(CorredorCasino.width, 3f, CorredorCasino.height)));
            ColocarNoche.obstaculos.Add(new Bounds(new Vector3(entrada.x, 1.5f, entrada.z), new Vector3(5f, 3f, 5f)));
            raiz = new GameObject(NombreRaiz).transform;

            Transform piezas = Grupo("Piezas");
            PonerPuentes(piezas);
            PonerPantallasGigantes(piezas);
            PonerAnuncios(piezas);
            PonerVallas(piezas);
            PonerEntradaCasino(piezas);
            PonerLuces();
            CrearCharcos();
            CrearNiebla();
            CrearTorres();
            CrearVolumen();

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t505-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-505 colocado:\n" + Informe);
        }

        // ------------------------------------------------------------------ utilidades

        private static Transform Grupo(string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(raiz, false);
            return t;
        }

        private static string RutaPrefab(string codigo)
        {
            string n = ImportarT500.RutasPrefab.FirstOrDefault(r => Path.GetFileNameWithoutExtension(r) == codigo);
            return n ?? ArteNoche.Buscar(codigo).RutaPrefab;
        }

        private static Vector3 Tam(string codigo)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo)).GetComponent<BoxCollider>().size;
        }

        private static GameObject Instanciar(string codigo, Transform padre, Vector3 pos, float giro, Vector3 escala, bool sinCollider)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = codigo + "_T505";
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
            go.transform.localScale = escala;
            if (sinCollider)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>())
                {
                    c.enabled = false;
                }
            }
            return go;
        }

        private static Texture TexturaDe(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                    {
                        return m.GetTexture("_BaseMap");
                    }
                }
            }
            return null;
        }

        private static Material Material(string nombre, string shader, Action<Material> ajustar)
        {
            if (Cache.TryGetValue(nombre, out Material m))
            {
                return m;
            }
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            AssetDatabase.DeleteAsset(ruta);
            m = new Material(Shader.Find(shader));
            ajustar(m);
            AssetDatabase.CreateAsset(m, ruta);
            Cache[nombre] = m;
            return m;
        }

        private static void Transparente(Material m, bool ladoDoble)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            if (ladoDoble)
            {
                m.SetFloat("_Cull", 0f);
            }
        }

        private static Texture2D TexSuburbios(string nombre)
        {
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaSuburbios + "/" + nombre + ".png");
            if (t == null)
            {
                throw new InvalidOperationException("Falta " + CarpetaSuburbios + "/" + nombre + ".png: corre antes Mecha Cyberpunk/T-501/Colocar Suburbios");
            }
            return t;
        }

        /// <summary>URP Lit con la textura de la pieza también como emisión: la pantalla y los marcos brillan de noche.</summary>
        private static Material Brillante(string codigo, float fuerza)
        {
            Texture tex = TexturaDe(codigo);
            return Material("Brillo_" + codigo, "Universal Render Pipeline/Lit", m =>
            {
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.35f);
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", Color.white * fuerza);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
        }

        private static void AplicarMaterial(GameObject go, Material mat)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < ms.Length; i++)
                {
                    ms[i] = mat;
                }
                r.sharedMaterials = ms;
            }
        }

        private static GameObject Cubo(Transform padre, string nombre, Vector3 centro, Vector3 tam, Material mat)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.name = nombre;
            c.transform.SetParent(padre, false);
            c.transform.position = centro;
            c.transform.localScale = tam;
            MeshRenderer mr = c.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return c;
        }

        private static Light Luz(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float rango)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = rango;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>Pieza pegada a la fachada del lado indicado (-1 oeste, 1 este) a esa altura, de frente a la calle. Busca a los lados de la z deseada.</summary>
        private static GameObject PonerEnFachada(Transform padre, string codigo, float zDeseada, int lado, float altura, float escalaZ, float escala = 1f)
        {
            Vector3 tam = Tam(codigo);
            Vector3 e = new Vector3(escala, escala, Mathf.Min(escala, escalaZ));
            Vector3 normal = new Vector3(-lado, 0f, 0f);
            float giro = ColocarNoche.GiroHacia(normal);
            Vector3 entrada = Entrada;
            HashSet<string> motivos = new HashSet<string>();
            foreach (float desplazo in Enumerable.Range(0, 26).SelectMany(i => new[] { i * 0.5f, -i * 0.5f }).Distinct())
            {
                float z = zDeseada + desplazo;
                if (z < ZMin || z > ZMax || (lado > 0 && Mathf.Abs(z - entrada.z) < 4.5f))
                {
                    continue;
                }
                if (!Physics.Raycast(new Vector3(47f, altura + tam.y * e.y * 0.5f, z), new Vector3(lado, 0f, 0f), out RaycastHit golpe, 14f, ~0, QueryTriggerInteraction.Ignore)
                    || golpe.normal.x * -lado < 0.85f)
                {
                    continue;
                }
                float x = golpe.point.x + normal.x * (tam.z * e.z * 0.5f + 0.3f);
                Vector3 pivote = new Vector3(x, altura, z);
                Bounds caja = ColocarNoche.CajaGirada(tam, pivote, giro, e);
                if (!LibreFachada(caja))
                {
                    motivos.Add(ColocarNoche.bloqueo);
                    continue;
                }
                ColocarNoche.obstaculos.Add(caja);
                return Instanciar(codigo, padre, pivote, giro, e, true);
            }
            Informe.AppendLine("No cupo: " + codigo + " cerca de z " + zDeseada + " lado " + lado + " (bloqueos: " + string.Join(" | ", motivos.Take(4)) + ")");
            return null;
        }

        /// <summary>Como Libre, pero ignora los azulejos planos de las fachadas (delgados en x y largos en z): un anuncio va delante de ellos.</summary>
        private static bool LibreFachada(Bounds caja)
        {
            Bounds holgada = caja;
            holgada.Expand(0.2f);
            foreach (Bounds b in ColocarNoche.obstaculos)
            {
                if (b.Intersects(holgada) && !(b.size.x < 1f && b.size.z > 2.5f) && b.size.x < 2f)
                {
                    ColocarNoche.bloqueo = "caja " + b.center + " " + b.size;
                    return false;
                }
            }
            Vector3 c = new Vector3(caja.center.x, 0f, caja.center.z);
            foreach (Vector3 p in ColocarNoche.puertas.Concat(ColocarNoche.personajes))
            {
                if (Vector3.Distance(c, new Vector3(p.x, 0f, p.z)) < 2.5f + caja.extents.z)
                {
                    ColocarNoche.bloqueo = "puerta o personaje " + p;
                    return false;
                }
            }
            return true;
        }

        /// <summary>Pieza en el piso: busca alrededor de (x, z) la primera posición libre (z primero, luego x).</summary>
        private static GameObject PonerEnPiso(Transform padre, string codigo, float xDeseada, float zDeseada, Vector3 normal, Vector3 escala, float giroExtra, bool sinCollider)
        {
            Vector3 tam = Tam(codigo);
            float giro = ColocarNoche.GiroHacia(normal) + giroExtra;
            HashSet<string> motivos = new HashSet<string>();
            foreach (float dx in new[] { 0f, -0.4f, 0.4f, -0.8f, 0.8f })
            {
                foreach (float desplazo in Enumerable.Range(0, 16).SelectMany(i => new[] { i * 0.4f, -i * 0.4f }).Distinct())
                {
                    float x = xDeseada + dx;
                    float z = zDeseada + desplazo;
                    float suelo = ColocarNoche.Suelo(new Vector3(x, 0f, z));
                    if (float.IsNaN(suelo))
                    {
                        continue;
                    }
                    Vector3 pivote = new Vector3(x, suelo, z);
                    Bounds caja = ColocarNoche.CajaGirada(tam, pivote, giro, escala);
                    if (!ColocarNoche.Libre(caja))
                    {
                        motivos.Add(ColocarNoche.bloqueo);
                        continue;
                    }
                    ColocarNoche.obstaculos.Add(caja);
                    return Instanciar(codigo, padre, pivote, giro, escala, sinCollider);
                }
            }
            Informe.AppendLine("No cupo: " + codigo + " cerca de x " + xDeseada + " z " + zDeseada + " (bloqueos: " + string.Join(" | ", motivos.Take(4)) + ")");
            return null;
        }

        // ------------------------------------------------------------------ piezas

        /// <summary>N02: dos tramos de autopista que cruzan la calle por arriba; alargados para que los pilares queden dentro de los edificios. Sin collider.</summary>
        private static void PonerPuentes(Transform padre)
        {
            Material verde = Material("Luz_Verde", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Verde * 2f));
            Material magenta = Material("Luz_Magenta", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Magenta * 2f));
            int n = 0;
            foreach (float z in ZPuentes)
            {
                GameObject p = Instanciar("N02", padre, new Vector3(47f, 0f, z), 0f, new Vector3(EscalaPuente, 1f, 1f), true);
                // Tira de neón bajo la cubierta (verde en un tramo, magenta en el otro).
                Cubo(p.transform, "Tira_Neon", new Vector3(47f, 7.35f, z - 5.06f), new Vector3(38f, 0.07f, 0.07f), n % 2 == 0 ? verde : magenta);
                Cubo(p.transform, "Tira_Neon", new Vector3(47f, 7.35f, z + 5.06f), new Vector3(38f, 0.07f, 0.07f), n % 2 == 0 ? verde : magenta);
                n++;
            }
            Informe.AppendLine("Autopista elevada N02 (sin collider, escala x " + EscalaPuente + "): " + n);
        }

        private static void PonerPantallasGigantes(Transform padre)
        {
            Material brillo = Brillante("E03_PantallaGigante", 1.1f);
            int n = 0;
            foreach (var (z, lado) in new[] { (60f, -1), (23f, 1) })
            {
                GameObject g = PonerEnFachada(padre, "E03_PantallaGigante", z, lado, 12f, 0.1f);
                if (g != null)
                {
                    AplicarMaterial(g, brillo);
                    n++;
                }
            }
            Informe.AppendLine("Pantallas gigantes E03: " + n);
        }

        private static void PonerAnuncios(Transform padre)
        {
            Material cy02 = Brillante("CY02_PantallaVertical", 2f);
            Material n08 = Brillante("N08", 2f);
            int a = 0;
            int b = 0;
            foreach (var (z, lado, altura) in new[] { (16f, -1, 3.6f), (30f, -1, 3.6f), (52f, -1, 3.6f), (66f, -1, 3.6f), (19f, 1, 3.6f), (31f, 1, 3.6f), (62f, 1, 3.6f), (77f, 1, 3.6f) })
            {
                GameObject g = PonerEnFachada(padre, "CY02_PantallaVertical", z, lado, altura, 0.45);
                if (g != null)
                {
                    AplicarMaterial(g, cy02);
                    a++;
                }
            }
            foreach (var (z, lado, altura) in new[] { (22f, -1, 4.8f), (41f, -1, 4.8f), (58f, -1, 4.8f), (13f, 1, 4.8f), (26f, 1, 4.8f), (68f, 1, 4.8f), (81f, 1, 4.8f) })
            {
                GameObject g = PonerEnFachada(padre, "N08", z, lado, altura, 0.5f);
                if (g != null)
                {
                    AplicarMaterial(g, n08);
                    b++;
                }
            }
            Informe.AppendLine("Anuncios verticales CY02: " + a + ", N08: " + b);
        }

        private static GameObject PonerEnFachada(Transform padre, string codigo, float z, int lado, float altura, double escalaZ)
        {
            return PonerEnFachada(padre, codigo, z, lado, altura, (float)escalaZ);
        }

        /// <summary>N07: tramos de valla a escala 0.3 (1.5 m de alto) a las orillas de la calle; cada tramo deja huecos de cruce y nunca llega al corredor del casino.</summary>
        private static void PonerVallas(Transform padre)
        {
            const float escala = 0.3f;
            float largo = Tam("N07").x * escala;
            int n = 0;
            foreach (var (xCentro, zInicio, piezas) in new[] { (39.3f, 15.5f, 3), (39.3f, 56f, 3), (39.3f, 70f, 3), (55.2f, 18f, 3), (55.2f, 56f, 3), (55.2f, 70f, 3) })
            {
                for (int i = 0; i < piezas; i++)
                {
                    GameObject g = PonerEnPiso(padre, "N07", xCentro, zInicio + i * largo, new Vector3(1f, 0f, 0f), Vector3.one * escala, 0f, false);
                    if (g != null)
                    {
                        // El prefab corre su largo en X: se gira para que corra en Z.
                        n++;
                    }
                }
            }
            Informe.AppendLine("Vallas N07: " + n);
        }

        private static void PonerEntradaCasino(Transform padre)
        {
            Vector3 e = Entrada;
            Transform e01 = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.StartsWith("E01_EntradaCasino") && !t.IsChildOf(raiz));
            Transform g = Grupo("Casino");
            g.SetParent(padre, false);
            Material foco = Material("Foco_Casino", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Calido * 2.4f));
            Material tuboMagenta = Material("Tubo_Magenta", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Magenta * 2.4f));
            Material tuboTurquesa = Material("Tubo_Turquesa", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Turquesa * 2.2f));
            Material alfombra = Material("Alfombra_Roja", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", new Color(0.55f, 0.04f, 0.08f)));
            int focos = 0;
            if (e01 != null)
            {
                Bounds b = new Bounds(e01.position, Vector3.zero);
                foreach (Renderer r in e01.GetComponentsInChildren<Renderer>())
                {
                    b.Encapsulate(r.bounds);
                }
                float xFrente = b.min.x - 0.04f;
                float mitad = Mathf.Min(b.extents.z - 0.2f, 3.1f);
                for (int i = 0; i < 15; i++)
                {
                    float z = e.z - mitad + i * (mitad * 2f / 14f);
                    GameObject f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.DestroyImmediate(f.GetComponent<Collider>());
                    f.name = "Foco_" + i;
                    f.transform.SetParent(g, false);
                    f.transform.position = new Vector3(xFrente, 3.0f, z);
                    f.transform.localScale = Vector3.one * 0.12f;
                    f.GetComponent<MeshRenderer>().sharedMaterial = foco;
                    f.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    focos++;
                }
                Cubo(g, "Tubo_Magenta", new Vector3(xFrente, 3.3f, e.z), new Vector3(0.05f, 0.06f, mitad * 2f), tuboMagenta);
                Cubo(g, "Tubo_Turquesa", new Vector3(xFrente, 3.46f, e.z), new Vector3(0.05f, 0.05f, mitad * 2f), tuboTurquesa);
                Cubo(g, "Tubo_Magenta_Alto", new Vector3(xFrente, 3.62f, e.z), new Vector3(0.05f, 0.06f, mitad * 2f), tuboMagenta);
            }
            GameObject tapete = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(tapete.GetComponent<Collider>());
            tapete.name = "Alfombra";
            tapete.transform.SetParent(g, false);
            tapete.transform.position = new Vector3(e.x + 0.5f, 0.035f, e.z);
            tapete.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
            tapete.transform.localScale = new Vector3(2.4f, 1.6f, 1f);
            tapete.GetComponent<MeshRenderer>().sharedMaterial = alfombra;
            tapete.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            int cargadores = 0;
            foreach (float z in new[] { 51.6f, 53.0f })
            {
                if (PonerEnPiso(g, "CY10_CargadorCalle", 56.1f, z, new Vector3(-1f, 0f, 0f), Vector3.one, 0f, false) != null)
                {
                    cargadores++;
                }
            }
            bool moto = PonerEnPiso(g, "C19_Moto", 52.6f, 41.5f, new Vector3(0f, 0f, -1f), Vector3.one, 0f, false) != null;
            Informe.AppendLine("Casino E01: " + (e01 != null) + ", focos " + focos + ", cargadores CY10 " + cargadores + ", moto " + moto);
        }

        // ------------------------------------------------------------------ luces

        private static void PonerLuces()
        {
            Transform g = Grupo("Luces");
            Vector3 e = Entrada;
            Luz(g, "Luz_Puente_Verde", new Vector3(47f, 6.4f, ZPuentes[0] - 1f), Verde, 4.5f, 13f);
            Luz(g, "Luz_Puente_Magenta", new Vector3(47f, 6.4f, ZPuentes[1] - 1f), Magenta, 7f, 15f);
            Luz(g, "Luz_Casino_Calida", new Vector3(e.x, 3.2f, e.z), Calido, 3.5f, 7f);
            Luz(g, "Luz_Casino_Magenta", new Vector3(e.x - 1.2f, 4.2f, e.z + 3f), Magenta, 4f, 9f);
            Luz(g, "Luz_Pantalla_Verde", new Vector3(43f, 10f, 60f), Verde, 4f, 14f);
            Luz(g, "Luz_Pantalla_Magenta", new Vector3(51f, 10f, 23f), Magenta, 8f, 18f);
            Informe.AppendLine("Luces de color sin sombra: 6");
        }

        // ------------------------------------------------------------------ charcos y niebla

        private static void CrearCharcos()
        {
            Transform g = Grupo("Charcos");
            Color[] tintes = { Verde, Magenta, Verde, Magenta, Turquesa, Verde, Magenta, Turquesa };
            float[] xs = { 44.2f, 49.6f, 46.4f, 43.5f, 50.8f, 47.5f, 45.2f, 49.0f };
            float[] zs = { 14f, 20f, 29f, 41f, 55f, 63f, 76f, 82f };
            int n = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                Color tinte = tintes[i];
                Material mat = Material("Charco_Neon_" + i, "Universal Render Pipeline/Unlit", m =>
                {
                    m.SetTexture("_BaseMap", TexSuburbios("Charco_Redondo"));
                    m.SetColor("_BaseColor", new Color(tinte.r * 0.3f, tinte.g * 0.3f, tinte.b * 0.3f, 0.7f));
                    Transparente(m, false);
                });
                float suelo = ColocarNoche.Suelo(new Vector3(xs[i], 0f, zs[i]));
                if (float.IsNaN(suelo))
                {
                    continue;
                }
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Charco_" + i;
                quad.transform.SetParent(g, false);
                quad.transform.position = new Vector3(xs[i], suelo + 0.03f, zs[i]);
                quad.transform.rotation = Quaternion.Euler(90f, i * 47f, 0f);
                float tamano = 2f + (i % 3) * 0.6f;
                quad.transform.localScale = new Vector3(tamano * 1.5f, tamano, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Charcos de color: " + n);
        }

        private static void CrearNiebla()
        {
            Transform g = Grupo("Niebla");
            Material mat = Material("Niebla_Verde", "Universal Render Pipeline/Unlit", m =>
            {
                m.SetTexture("_BaseMap", TexSuburbios("Niebla_Degradado"));
                m.SetColor("_BaseColor", new Color(0.1f, 0.5f, 0.3f, 0.16f));
                Transparente(m, true);
            });
            int n = 0;
            for (float z = 24f; z <= 84f; z += 10f)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Niebla_" + n;
                quad.transform.SetParent(g, false);
                quad.transform.position = new Vector3(47f, 6f, z);
                quad.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                quad.transform.localScale = new Vector3(20f, 14f, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Capas de niebla verde: " + n);
        }

        // ------------------------------------------------------------------ torres de fondo

        private static Material MatTorre(string nombre, float semilla, float niebla, float probLuz, Action<Material> extra = null)
        {
            return Material("TorreNeon_" + nombre, RutaShaderTorre, m =>
            {
                m.SetColor("_ColorBase", new Color(0.01f, 0.035f, 0.04f));
                m.SetFloat("_Semilla", semilla);
                m.SetFloat("_Niebla", niebla);
                m.SetFloat("_ProbLuz", probLuz);
                m.SetFloat("_Brillo", 1.7f);
                m.SetColor("_ColorNiebla", new Color(0.04f, 0.2f, 0.12f));
                extra?.Invoke(m);
            });
        }

        /// <summary>Torres altas del distrito rico al norte y al este de Neón Alto (más cerca que el horizonte de T-501), con ventanas, pantallas verde y magenta y antenas con luz roja.</summary>
        private static void CrearTorres()
        {
            Transform g = Grupo("Torres");
            Random azar = new Random(505);
            Material[] cuerpos = { MatTorre("A", 2f, 0.4f, 0.4f), MatTorre("B", 8f, 0.5f, 0.35f), MatTorre("C", 4f, 0.6f, 0.4f) };
            Material rojo = MatTorre("LuzRoja", 0f, 0.1f, 0f, m => m.SetColor("_ColorBase", new Color(6f, 0.15f, 0.12f)));
            Material verde = MatTorre("PantallaVerde", 0f, 0.15f, 0f, m => m.SetColor("_ColorBase", new Color(0.5f, 3.2f, 0.7f)));
            Material magenta = MatTorre("PantallaMagenta", 0f, 0.15f, 0f, m => m.SetColor("_ColorBase", new Color(3.2f, 0.4f, 2.2f)));
            Texture texE03 = TexturaDe("E03_PantallaGigante");
            Material pantallaE03 = MatTorre("PantallaE03", 0f, 0.15f, 0f, m => { m.SetFloat("_UsarTextura", 1f); m.SetTexture("_MainTex", texE03); m.SetFloat("_Brillo", 0.9f); });
            int n = 0;
            int pantallas = 0;
            int antenas = 0;
            // 10 al norte (detrás de la autopista del final, z 130 a 300) y 4 al este.
            for (int i = 0; i < 14; i++)
            {
                Vector3 pos = i < 10
                    ? new Vector3(-10f + (i % 5) * 36f + (float)azar.NextDouble() * 14f, 0f, 135f + (i / 5) * 70f + (float)azar.NextDouble() * 40f)
                    : new Vector3(170f + (i % 2) * 70f + (float)azar.NextDouble() * 20f, 0f, 10f + (i - 10) * 26f);
                float ancho = 22f + (float)azar.NextDouble() * 18f;
                float fondo = 22f + (float)azar.NextDouble() * 18f;
                float alto = 110f + (float)azar.NextDouble() * 120f;
                Material cuerpo = cuerpos[i % cuerpos.Length];
                Transform torre = new GameObject("Torre_" + i).transform;
                torre.SetParent(g, false);
                torre.position = pos;
                Cubo(torre, "Cuerpo", pos + Vector3.up * alto * 0.5f, new Vector3(ancho, alto, fondo), cuerpo);
                float tope = alto;
                if (i % 3 == 0)
                {
                    float alto2 = alto * 0.3f;
                    Cubo(torre, "Remate", pos + Vector3.up * (alto + alto2 * 0.5f), new Vector3(ancho * 0.6f, alto2, fondo * 0.6f), cuerpo);
                    tope += alto2;
                }
                if (i % 2 == 0)
                {
                    float largo = 14f + (float)azar.NextDouble() * 12f;
                    Cubo(torre, "Antena", pos + Vector3.up * (tope + largo * 0.5f), new Vector3(1.4f, largo, 1.4f), cuerpo);
                    Cubo(torre, "LuzRoja", pos + Vector3.up * (tope + largo + 1f), new Vector3(2.6f, 2.6f, 2.6f), rojo);
                    antenas++;
                }
                // Pantalla en la cara que mira a la calle de Neón Alto.
                Vector3 haciaCentro = new Vector3(47f - pos.x, 0f, 48f - pos.z).normalized;
                bool ejeX = Mathf.Abs(haciaCentro.x) > Mathf.Abs(haciaCentro.z);
                Vector3 normal = ejeX ? new Vector3(Mathf.Sign(haciaCentro.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(haciaCentro.z));
                float lado = (ejeX ? fondo : ancho) * 0.7f;
                Material mp = pantallas % 3 == 0 ? verde : (pantallas % 3 == 1 ? magenta : pantallaE03);
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Pantalla";
                quad.transform.SetParent(torre, false);
                quad.transform.position = pos + normal * (((ejeX ? ancho : fondo) * 0.5f) + 0.3f) + Vector3.up * (alto * 0.55f);
                quad.transform.rotation = Quaternion.LookRotation(-normal);
                quad.transform.localScale = new Vector3(lado, Mathf.Min(alto * 0.35f, lado * 1.5f), 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mp;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                pantallas++;
                n++;
            }
            Informe.AppendLine("Torres de fondo: " + n + " (pantallas " + pantallas + ", antenas con luz roja " + antenas + ")");
        }

        // ------------------------------------------------------------------ posproceso local

        /// <summary>Crea el perfil verde si no existe, o lo rehace en su lugar (mismo GUID). Los overrides van como sub-assets.</summary>
        internal static VolumeProfile CrearPerfil()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfil);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, RutaPerfil);
            }
            foreach (Object viejo in AssetDatabase.LoadAllAssetsAtPath(RutaPerfil))
            {
                if (viejo is VolumeComponent)
                {
                    Object.DestroyImmediate(viejo, true);
                }
            }
            perfil.components.Clear();
            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.postExposure.Override(-0.2f);
            ca.saturation.Override(8f);
            ca.colorFilter.Override(new Color(0.92f, 1.08f, 0.96f));
            Vignette vi = perfil.Add<Vignette>(true);
            vi.intensity.Override(0.24f);
            AssetDatabase.AddObjectToAsset(ca, perfil);
            AssetDatabase.AddObjectToAsset(vi, perfil);
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        private static void CrearVolumen()
        {
            VolumeProfile perfil = CrearPerfil();
            GameObject go = new GameObject("VolumenNeonAlto");
            go.transform.SetParent(raiz, false);
            go.transform.position = new Vector3(47f, 15f, 48f);
            BoxCollider caja = go.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(26f, 40f, 90f);
            Volume v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = 10f;
            v.priority = 5f;
            v.sharedProfile = perfil;
            // Solo de noche: el peso sigue al del volumen global de noche (PosprocesoNoche).
            Type tipo = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Ambiente.VolumenSigueNoche")).FirstOrDefault(t => t != null);
            if (tipo != null)
            {
                Component c = go.AddComponent(tipo);
                SerializedObject so = new SerializedObject(c);
                GameObject noche = GameObject.Find("PosprocesoNoche");
                so.FindProperty("origen").objectReferenceValue = noche != null ? noche.GetComponent<Volume>() : null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Informe.AppendLine("Volumen de posproceso local verde de Neón Alto (solo de noche): " + (tipo != null));
        }
    }
}
