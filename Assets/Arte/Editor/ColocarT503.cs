using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-503 (paso A3 del plan de ciudad): viste El Tianguis de Ciudad_Prologo (calle principal, x de -37 a 37, z = 0) como tianguis-noche:
    /// puestos con lona N03a/N03b en hileras a las orillas, series de focos cálidos en zigzag sobre la calle, vapor en los puestos de comida,
    /// dos expendedoras CY06, proyectores holo CY01 con hologramas de comida y charcos de color. Todo cuelga de la raíz <see cref="NombreRaiz"/>:
    /// al correrlo otra vez se borra y se rehace (idempotente). Deja libre el carril central (<see cref="Carril"/>), las bocacalles y los puntos
    /// de los personajes y de la pelea; la pelea con los SKULLS (x -37, z 77 a 110) queda fuera de la zona de trabajo.
    /// </summary>
    public static class ColocarT503
    {
        public const string NombreRaiz = "T503_Tianguis";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Tianguis";
        private const string CarpetaMateriales = Carpeta + "/Materiales";
        private const string CarpetaTexturas = Carpeta + "/Texturas";
        private const string CarpetaSuburbios = "Assets/Arte/Suburbios/Texturas";
        private const string RutaVapor = "Assets/Arte/Callejon/Texturas/Vapor_Suave.png";

        public const float XMin = -37f;
        public const float XMax = 37f;
        public const float MitadCarril = 3.4f;
        private const float AltoCarril = 3f;
        private const float ZPared = 9.6f;

        private static readonly StringBuilder Informe = new StringBuilder();
        private static Transform raiz;
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly Color Calido = new Color(1f, 0.62f, 0.25f);
        private static readonly Color Magenta = new Color(1f, 0.15f, 0.8f);
        private static readonly Color Turquesa = new Color(0.1f, 0.95f, 0.85f);
        private static readonly List<Vector3> PuntosVapor = new List<Vector3>();

        /// <summary>Franja central de la calle por donde pasa el jugador, los carros y la pandilla: nada de la raíz T503 entra aquí.</summary>
        public static Bounds Carril
        {
            get { return new Bounds(new Vector3(0f, AltoCarril * 0.5f, 0f), new Vector3(XMax - XMin + 20f, AltoCarril, MitadCarril * 2f)); }
        }

        public static IReadOnlyList<string> Codigos
        {
            get { return new[] { "N03a", "N03b", "CY06_Expendedora", "CY01_ProyectorHolo" }; }
        }

        [MenuItem("Mecha Cyberpunk/T-503/Colocar El Tianguis")]
        public static void Colocar()
        {
            Informe.Clear();
            Cache.Clear();
            PuntosVapor.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Physics.SyncTransforms();
            Directory.CreateDirectory(CarpetaMateriales);
            Directory.CreateDirectory(CarpetaTexturas);
            ColocarNoche.ReunirObstaculos();
            ColocarNoche.obstaculos.Add(Carril);
            ReservarBocacalles();
            raiz = new GameObject(NombreRaiz).transform;

            Transform puestos = Grupo("Puestos");
            PonerExpendedoras(puestos);
            PonerProyectores(puestos);
            PonerPuestos(puestos);
            CrearVapor();
            CrearFocos();
            CrearCharcos();

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t503-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-503 colocado:\n" + Informe);
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

        /// <summary>Las bocacalles de las dos orillas (huecos entre fachadas) quedan reservadas con 3.5 m a cada lado.</summary>
        private static void ReservarBocacalles()
        {
            int n = 0;
            for (float x = XMin - 4f; x <= XMax + 4f; x += 1f)
            {
                foreach (int lado in new[] { 1, -1 })
                {
                    Vector3 c = new Vector3(x, 1.4f, lado * (ZPared + 1.6f));
                    if (!Physics.CheckBox(c, new Vector3(0.45f, 1f, 0.45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    {
                        ColocarNoche.obstaculos.Add(new Bounds(new Vector3(x, 1.5f, lado * 7.5f), new Vector3(7f, 3f, 6f)));
                        n++;
                    }
                }
            }
            Informe.AppendLine("Muestras de bocacalle reservadas: " + n);
        }

        /// <summary>z de la fachada del lado indicado (rayo desde adentro de la calle hacia afuera); si no hay, el valor por defecto.</summary>
        private static float ZMuro(float x, int lado)
        {
            Vector3 desde = new Vector3(x, 1.5f, lado * (ZPared - 0.7f));
            if (Physics.Raycast(desde, new Vector3(0f, 0f, lado), out RaycastHit golpe, 3f, ~0, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(golpe.normal.z) > 0.85f)
            {
                return golpe.point.z;
            }
            return lado * ZPared;
        }

        private static GameObject Instanciar(string codigo, Transform padre, Vector3 pos, float giro)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = codigo + "_T503";
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
            return go;
        }

        // ------------------------------------------------------------------ piezas

        /// <summary>Pieza contra la fachada (espalda al muro, frente a la calle). Busca desde x deseada hacia los lados la primera posición libre.</summary>
        private static GameObject PonerPegada(Transform padre, string codigo, float xDeseada, int lado)
        {
            Vector3 tam = Tam(codigo);
            Vector3 normal = new Vector3(0f, 0f, -lado);
            float giro = ColocarNoche.GiroHacia(normal);
            foreach (float desplazo in Enumerable.Range(0, 30).SelectMany(i => new[] { i * 0.7f, -i * 0.7f }).Distinct())
            {
                float x = Mathf.Clamp(xDeseada + desplazo, XMin, XMax);
                float z = ZMuro(x, lado) + normal.z * (tam.z * 0.5f + 0.35f);
                float suelo = ColocarNoche.Suelo(new Vector3(x, 0f, z));
                if (float.IsNaN(suelo))
                {
                    continue;
                }
                Vector3 pivote = new Vector3(x, suelo, z);
                Bounds caja = ColocarNoche.CajaGirada(tam, pivote, giro);
                if (!ColocarNoche.Libre(caja))
                {
                    continue;
                }
                ColocarNoche.obstaculos.Add(caja);
                return Instanciar(codigo, padre, pivote, giro);
            }
            Informe.AppendLine("No cupo: " + codigo + " en x " + xDeseada + " lado " + lado);
            return null;
        }

        private static void PonerExpendedoras(Transform padre)
        {
            int n = 0;
            foreach (var (x, lado) in new[] { (-13f, 1), (24f, -1) })
            {
                GameObject go = PonerPegada(padre, "CY06_Expendedora", x, lado);
                if (go == null)
                {
                    continue;
                }
                Vector3 frente = new Vector3(0f, 0f, -lado);
                Luz(go.transform, "Luz", go.transform.position + Vector3.up * 1.4f + frente * 1.1f, n == 0 ? Magenta : Turquesa, 3.2f, 5.5f);
                n++;
            }
            Informe.AppendLine("Expendedoras CY06 con luz de color: " + n);
        }

        private static void PonerProyectores(Transform padre)
        {
            Transform hologramas = Grupo("Hologramas");
            string[] comidas = { "Holo_Taco", "Holo_Tazon", "Holo_Refresco" };
            Color[] tintes = { Calido, Turquesa, Magenta };
            int n = 0;
            int hechos = 0;
            foreach (var (x, lado) in new[] { (-29f, -1), (7f, 1), (31f, 1) })
            {
                GameObject go = PonerPegada(padre, "CY01_ProyectorHolo", x, lado);
                if (go == null)
                {
                    continue;
                }
                Vector3 normal = new Vector3(0f, 0f, -lado);
                CrearHolograma(hologramas, "Holograma_" + n, go.transform.position + Vector3.up * 3.6f + normal * 0.9f, normal, comidas[n % 3], tintes[n % 3]);
                hechos++;
                n++;
            }
            // Hologramas de comida sobre los puestos más cercanos al carril, mirando a la calle.
            Informe.AppendLine("Proyectores CY01: " + n + ", hologramas de comida: " + hechos);
        }

        private static void PonerPuestos(Transform padre)
        {
            string[] codigos = { "N03a", "N03b" };
            float[] zs = { 8.4f, 7.8f, 7.2f, 6.6f, 6.0f, 5.5f, 5.0f };
            int n = 0;
            int comida = 0;
            int fallos = 0;
            Transform hologramas = Grupo("HologramasPuestos");
            string[] comidas = { "Holo_Taco", "Holo_Tazon", "Holo_Refresco" };
            Color[] tintes = { Calido, Turquesa, Magenta };
            foreach (int lado in new[] { 1, -1 })
            {
                float ultimoX = XMin - 10f;
                for (float x = XMin; x <= XMax; x += 0.5f)
                {
                    if (x - ultimoX < 5f)
                    {
                        continue;
                    }
                    string codigo = codigos[(n + (lado > 0 ? 0 : 1)) % 2];
                    Vector3 tam = Tam(codigo);
                    Vector3 normal = new Vector3(0f, 0f, -lado);
                    float giro = ColocarNoche.GiroHacia(normal);
                    bool puesto = false;
                    foreach (float zb in zs)
                    {
                        float z = lado * zb;
                        float suelo = ColocarNoche.Suelo(new Vector3(x, 0f, z));
                        if (float.IsNaN(suelo))
                        {
                            continue;
                        }
                        Vector3 pivote = new Vector3(x, suelo, z);
                        Bounds caja = ColocarNoche.CajaGirada(tam, pivote, giro);
                        if (!ColocarNoche.Libre(caja))
                        {
                            continue;
                        }
                        ColocarNoche.obstaculos.Add(caja);
                        GameObject go = Instanciar(codigo, padre, pivote, giro);
                        if (n % 2 == 0)
                        {
                            PuntosVapor.Add(pivote + Vector3.up * 1.5f);
                            CrearHolograma(hologramas, "HoloPuesto_" + comida, pivote + Vector3.up * 3.1f + normal * (tam.z * 0.5f + 0.2f), normal, comidas[comida % 3], tintes[comida % 3], 0.9f);
                            comida++;
                        }
                        if (n % 3 == 0)
                        {
                            Luz(go.transform, "Luz", pivote + Vector3.up * 2.3f + normal * 0.6f, Calido, 3.5f, 6f);
                        }
                        ultimoX = x;
                        n++;
                        puesto = true;
                        break;
                    }
                    if (!puesto)
                    {
                        fallos++;
                    }
                }
            }
            Informe.AppendLine("Puestos N03a/N03b en hileras: " + n + ", con vapor y holograma de comida: " + comida + " (posiciones sin cupo: " + fallos + ")");
        }

        // ------------------------------------------------------------------ materiales y texturas

        private static Texture2D Tex(string nombre)
        {
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaSuburbios + "/" + nombre + ".png");
            if (t == null)
            {
                throw new InvalidOperationException("Falta " + CarpetaSuburbios + "/" + nombre + ".png: corre antes Mecha Cyberpunk/T-501/Colocar Suburbios");
            }
            return t;
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

        private static Texture2D GuardarTextura(string nombre, Texture2D t)
        {
            string ruta = CarpetaTexturas + "/" + nombre + ".png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        /// <summary>Icono de comida de 128x128 (taco, tazón con vapor o refresco) en trazo luminoso, fondo transparente.</summary>
        private static Texture2D TexturaComida(string tipo)
        {
            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (n - 1f) * 2f - 1f;
                    float v = y / (n - 1f) * 2f - 1f;
                    float d = DistanciaForma(tipo, u, v);
                    float borde = Mathf.Clamp01(1f - Mathf.Abs(d) / 0.07f);
                    float relleno = d < 0f ? 0.28f : 0f;
                    float halo = Mathf.Clamp01(1f - Mathf.Abs(d) / 0.22f) * 0.18f;
                    float a = Mathf.Clamp01(Mathf.Max(borde, relleno) + halo);
                    float marco = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) - 0.96f) / 0.03f) * 0.7f;
                    a = Mathf.Max(a, marco);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            return GuardarTextura(tipo, t);
        }

        /// <summary>Distancia aproximada con signo al contorno del icono (negativa adentro).</summary>
        private static float DistanciaForma(string tipo, float u, float v)
        {
            switch (tipo)
            {
                case "Holo_Taco":
                {
                    // Media luna: medio círculo (arriba) con base recta y un doblez interior.
                    float circulo = Mathf.Sqrt(u * u + (v + 0.35f) * (v + 0.35f)) - 0.62f;
                    float corte = -(v + 0.35f);
                    float d = Mathf.Max(circulo, corte);
                    float interior = Mathf.Abs(Mathf.Sqrt(u * u + (v + 0.35f) * (v + 0.35f)) - 0.38f);
                    return v + 0.35f > 0f ? Mathf.Min(d, interior < 0.03f ? -0.001f : d) : d;
                }
                case "Holo_Tazon":
                {
                    // Tazón: medio círculo hacia abajo + tres hilos de vapor.
                    float circulo = Mathf.Sqrt(u * u + (v - 0.05f) * (v - 0.05f)) - 0.6f;
                    float corte = v - 0.05f;
                    float tazon = Mathf.Max(circulo, corte);
                    float vapor = 9f;
                    foreach (float cx in new[] { -0.3f, 0f, 0.3f })
                    {
                        if (v > 0.2f && v < 0.85f)
                        {
                            float onda = cx + Mathf.Sin(v * 9f) * 0.07f;
                            vapor = Mathf.Min(vapor, Mathf.Abs(u - onda) - 0.025f);
                        }
                    }
                    return Mathf.Min(tazon, vapor);
                }
                default:
                {
                    // Vaso de refresco: trapecio con popote.
                    float ancho = Mathf.Lerp(0.5f, 0.34f, (v + 0.7f) / 1.4f);
                    float vaso = Mathf.Max(Mathf.Abs(u) - ancho, Mathf.Abs(v + 0.05f) - 0.65f);
                    float popote = Mathf.Max(Mathf.Abs(u - 0.18f - (v - 0.6f) * 0.25f) - 0.04f, Mathf.Abs(v - 0.78f) - 0.2f);
                    return Mathf.Min(vaso, popote);
                }
            }
        }

        // ------------------------------------------------------------------ hologramas, luces

        private static void CrearHolograma(Transform padre, string nombre, Vector3 pos, Vector3 normal, string tipo, Color tinte, float escala = 1.3f)
        {
            Material mat = Material(tipo + "_Mat_" + ColorUtility.ToHtmlStringRGB(tinte), "Universal Render Pipeline/Unlit", m =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTexturas + "/" + tipo + ".png") ?? TexturaComida(tipo);
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", new Color(tinte.r * 1.6f, tinte.g * 1.6f, tinte.b * 1.6f, 0.95f));
                Transparente(m, true);
            });
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = nombre;
            quad.transform.SetParent(padre, false);
            quad.transform.position = pos;
            quad.transform.rotation = Quaternion.LookRotation(-normal);
            quad.transform.localScale = new Vector3(escala, escala, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            quad.GetComponent<MeshRenderer>().receiveShadows = false;
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

        // ------------------------------------------------------------------ vapor

        private static void CrearVapor()
        {
            Transform g = Grupo("Vapor");
            // Puestos de comida que ya estaban (C12 tacos de la calle principal).
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name.StartsWith("Puesto_Tacos") && Mathf.Abs(t.position.z) < 12f && t.position.x > XMin - 4f && t.position.x < XMax + 4f)
                {
                    PuntosVapor.Add(t.position + Vector3.up * 1.4f);
                }
            }
            Material mat = Material("Vapor_Tianguis", "Universal Render Pipeline/Particles/Unlit", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaVapor));
                m.SetColor("_BaseColor", Color.white);
                Transparente(m, false);
                m.SetFloat("_Surface", 1f);
            });
            int i = 0;
            foreach (Vector3 p in PuntosVapor)
            {
                GameObject go = new GameObject("Vapor_" + i);
                go.transform.SetParent(g, false);
                go.transform.position = p;
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                ParticleSystem ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.prewarm = true;
                main.duration = 5f;
                main.startLifetime = 3f;
                main.startSpeed = 0.5f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
                main.startColor = new Color(1f, 0.85f, 0.75f, 0.4f);
                main.maxParticles = 14;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                ParticleSystem.EmissionModule em = ps.emission;
                em.rateOverTime = 4.5f;
                ParticleSystem.ShapeModule forma = ps.shape;
                forma.shapeType = ParticleSystemShapeType.Cone;
                forma.angle = 14f;
                forma.radius = 0.15f;
                ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
                tamano.enabled = true;
                tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.8f));
                ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
                color.enabled = true;
                Gradient gradiente = new Gradient();
                gradiente.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
                color.color = gradiente;
                ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                ps.Play();
                i++;
            }
            Informe.AppendLine("Vapor en puestos de comida (máx. 14 partículas cada uno): " + i);
        }

        // ------------------------------------------------------------------ focos en zigzag

        private static void CrearFocos()
        {
            Transform g = Grupo("Focos");
            Material cable = Material("Cable_Focos", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.06f)));
            Material foco = Material("Foco_Calido", "Universal Render Pipeline/Lit", m =>
            {
                m.SetColor("_BaseColor", new Color(0.6f, 0.35f, 0.1f));
                m.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.12f) * 2.2f);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            // Zigzag: cada serie cruza la calle de una fachada a la otra y la siguiente vuelve; dos filas desfasadas.
            float[] altos = { 5.6f, 5.0f };
            int series = 0;
            int focos = 0;
            int luces = 0;
            for (int fila = 0; fila < 2; fila++)
            {
                float x0 = XMin + 2f + fila * 6f;
                int lado = fila == 0 ? 1 : -1;
                for (float x = x0; x + 12f <= XMax + 2f; x += 12f)
                {
                    Vector3 a = new Vector3(x, altos[fila], ZMuro(x, lado) - lado * 0.15f);
                    Vector3 b = new Vector3(x + 12f, altos[fila] - 0.4f, ZMuro(x + 12f, -lado) + lado * 0.15f);
                    focos += CrearSerie(g, "Serie_" + series, a, b, cable, foco);
                    if (series % 2 == 0)
                    {
                        Vector3 medio = Vector3.Lerp(a, b, 0.5f) + Vector3.down * 0.9f;
                        Luz(g, "Luz_Serie_" + series, medio, Calido, 4.5f, 8f);
                        luces++;
                    }
                    series++;
                }
            }
            Informe.AppendLine("Series de focos en zigzag: " + series + ", focos emisivos: " + focos + ", luces cálidas sin sombra: " + luces);
        }

        private static int CrearSerie(Transform padre, string nombre, Vector3 a, Vector3 b, Material cable, Material foco)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            const int tramos = 24;
            const float caida = 0.9f;
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = tramos + 1;
            lr.widthMultiplier = 0.02f;
            lr.sharedMaterial = cable;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            for (int i = 0; i <= tramos; i++)
            {
                float t = i / (float)tramos;
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y -= caida * 4f * t * (1f - t);
                lr.SetPosition(i, p);
            }
            int n = 0;
            float largo = Vector3.Distance(a, b);
            int cantidad = Mathf.Max(6, Mathf.RoundToInt(largo / 1.7f));
            for (int i = 1; i < cantidad; i++)
            {
                float t = i / (float)cantidad;
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y -= caida * 4f * t * (1f - t) + 0.12f;
                GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(esfera.GetComponent<Collider>());
                esfera.name = "Foco_" + i;
                esfera.transform.SetParent(go.transform, false);
                esfera.transform.position = p;
                esfera.transform.localScale = Vector3.one * 0.13f;
                MeshRenderer mr = esfera.GetComponent<MeshRenderer>();
                mr.sharedMaterial = foco;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                n++;
            }
            return n;
        }

        // ------------------------------------------------------------------ charcos

        private static void CrearCharcos()
        {
            Transform g = Grupo("Charcos");
            Color[] tintes = { Magenta, Turquesa, Calido, Magenta, Turquesa, Calido, Magenta, Turquesa };
            float[] xs = { -31f, -22f, -12f, -3f, 8f, 17f, 27f, 34f };
            float[] zs = { 4.2f, -4.4f, 4.3f, -4.2f, 4.6f, -4.5f, 4.2f, -4.3f };
            int n = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                Color tinte = tintes[i];
                Material mat = Material("Charco_Tianguis_" + i, "Universal Render Pipeline/Unlit", m =>
                {
                    m.SetTexture("_BaseMap", Tex("Charco_Redondo"));
                    m.SetColor("_BaseColor", new Color(tinte.r * 0.6f, tinte.g * 0.6f, tinte.b * 0.6f, 0.8f));
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
                float tamano = 1.6f + (i % 3) * 0.6f;
                quad.transform.localScale = new Vector3(tamano * 1.5f, tamano, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Charcos de color: " + n);
        }
    }
}
