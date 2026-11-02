using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-506 (pulido visual, punto 1): quita el greybox visible de Ciudad_Prologo. Toda primitiva de Unity (cubo, cilindro...) con material de color liso
    /// recibe un material con textura puesta en metros del mundo (shader MuroMundo: revoque con ventanas, losa, madera, metal pintado, lona, lámina);
    /// los colliders y las posiciones no se tocan. Además cambia los puestos de cubos de T-407 (mesa, cajas, poste, lona verde, faldón) por mesa de madera
    /// con huacales C17 y tambos C16. Todo lo creado cuelga de <see cref="NombreRaiz"/> y se rehace al correrlo otra vez (idempotente: lo ya retexturizado
    /// no vuelve a contar como greybox).
    /// </summary>
    public static class ColocarT506
    {
        public const string NombreRaiz = "T506_SinGreybox";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Exteriores";
        private const string CarpetaTexturas = Carpeta + "/Texturas";
        private const string CarpetaMateriales = Carpeta + "/Materiales";
        private const string RutaShader = "MechaCyberpunk/MuroMundo";

        /// <summary>Raíces que no son zonas de la demo (arena, creador de personaje, negocios y guarida descartados): quedan fuera del inventario.</summary>
        public static readonly string[] RaicesExcluidas = { "JaulaClandestino", "Greybox_Apariencia", "GTA_T429", "GTA_T448" };
        /// <summary>Charcos, gente de bulto de la arena y carteles con textura; además se salta todo lo más delgado que <see cref="GrosorDecal"/> (rayas, cebras, grietas, marcas: pintura plana).</summary>
        public const float GrosorDecal = 0.12f;
        public static readonly string[] MaterialesDecal = { "Charco", "Publico", "Grafiti" };

        private static readonly string[] MallasPrimitivas = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };
        private static readonly StringBuilder Informe = new StringBuilder();
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        private enum Clase { Ventanas, Revoque, Losa, Madera, Lona, Lamina, Metal }

        // ------------------------------------------------------------------ detección (misma regla que la prueba EditMode)

        public static bool TieneTextura(Material m)
        {
            if (m == null) { return false; }
            Shader s = m.shader;
            for (int i = 0; i < s.GetPropertyCount(); i++)
            {
                if (s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture && m.GetTexture(s.GetPropertyName(i)) != null) { return true; }
            }
            return false;
        }

        public static bool EsEfecto(Material m)
        {
            if (m == null) { return false; }
            if (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.3f) { return true; }
            if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) { return true; }
            string sh = m.shader.name;
            return sh.Contains("Unlit") || sh.Contains("Particles") || sh.Contains("Sprites") || sh.StartsWith("MechaCyberpunk/Torre") || sh.Contains("TextMeshPro") || sh.Contains("UI/");
        }

        private static string RutaDe(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) { r = p.name + "/" + r; }
            return r;
        }

        public static bool EsGreybox(Renderer r, out Material material)
        {
            material = null;
            if (!(r is MeshRenderer) || !r.enabled || !r.gameObject.activeInHierarchy) { return false; }
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !MallasPrimitivas.Contains(mf.sharedMesh.name)) { return false; }
            Vector3 tam = r.bounds.size;
            if (Mathf.Min(tam.x, Mathf.Min(tam.y, tam.z)) < GrosorDecal) { return false; }
            Transform raiz = r.transform;
            while (raiz.parent != null) { raiz = raiz.parent; }
            if (RaicesExcluidas.Contains(raiz.name)) { return false; }
            for (Transform p = r.transform; p != null; p = p.parent)
            {
                if (p.name == "VistaPreviaApariencia") { return false; } // maniquí del mostrador de la clínica, 99 m bajo el suelo
            }
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) { return true; }
                if (MaterialesDecal.Any(d => m.name.Contains(d))) { continue; }
                if (!TieneTextura(m) && !EsEfecto(m)) { material = m; return true; }
            }
            return false;
        }

        // ------------------------------------------------------------------ colocación

        [MenuItem("Mecha Cyberpunk/T-506/Quitar greybox")]
        public static void Colocar()
        {
            Informe.Clear();
            Cache.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null) { Object.DestroyImmediate(vieja); }
            Directory.CreateDirectory(CarpetaTexturas);
            Directory.CreateDirectory(CarpetaMateriales);
            Transform raiz = new GameObject(NombreRaiz).transform;

            PonerPuestos(raiz);
            RetexturarMantas();
            int cambiados = Retexturar();
            Informe.AppendLine("Renderers retexturizados: " + cambiados);
            List<string> quedan = Pendientes();
            Informe.AppendLine("Greybox que queda: " + quedan.Count);
            foreach (string q in quedan.Take(40)) { Informe.AppendLine("  " + q); }

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t506-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-506 listo:\n" + Informe);
        }

        /// <summary>Lo que queda como greybox visible (para el informe y la prueba).</summary>
        public static List<string> Pendientes()
        {
            var lista = new List<string>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (EsGreybox(r, out Material m)) { lista.Add(RutaDe(r.transform) + " | " + (m != null ? m.name : "sin material")); }
            }
            return lista;
        }

        private static int Retexturar()
        {
            int n = 0;
            var porMaterial = new Dictionary<string, int>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!EsGreybox(r, out Material original) || original == null) { continue; }
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || TieneTextura(m) || EsEfecto(m) || MaterialesDecal.Any(d => m.name.Contains(d))) { continue; }
                    mats[i] = MaterialConArte(m);
                    if (!porMaterial.ContainsKey(m.name)) { porMaterial[m.name] = 0; }
                    porMaterial[m.name]++;
                }
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                n++;
            }
            foreach (KeyValuePair<string, int> kv in porMaterial.OrderByDescending(k => k.Value))
            {
                Informe.AppendLine("  " + kv.Key + " -> " + kv.Value);
            }
            return n;
        }

        // ------------------------------------------------------------------ puestos y mantas de T-407

        private static void PonerPuestos(Transform raiz)
        {
            Transform grupo = new GameObject("Puestos").transform;
            grupo.SetParent(raiz, false);
            GameObject huacales = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab("C17_Huacales"));
            GameObject tambo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab("C16_Tambo"));
            string[] ocultar = { "Caja1", "Caja2", "Caja3", "Poste", "Lona", "Faldon", "TuboNeon" };
            int n = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("Puesto_") || t.parent == null || t.parent.name != "Puestos" || t.parent.parent == null || t.parent.parent.name != "Tianguis") { continue; }
                Transform mesa = t.Find("Mesa");
                if (mesa == null) { continue; }
                Transform caja3 = t.Find("Caja3");
                Vector3 posCaja3 = caja3 != null ? caja3.position : mesa.position + mesa.rotation * new Vector3(1.9f, -0.2f, -0.3f);
                Quaternion giroCaja3 = caja3 != null ? caja3.rotation : mesa.rotation;
                foreach (string nombre in ocultar)
                {
                    Transform c = t.Find(nombre);
                    if (c != null && c.TryGetComponent(out Renderer rr)) { rr.enabled = false; }
                }
                // Huacales sobre la mesa y junto a ella, y un tambo donde estaba la caja grande; sin colliders: la mesa conserva el suyo.
                if (huacales != null)
                {
                    Prop(huacales, grupo, mesa.position + mesa.rotation * new Vector3(-0.5f, 0.45f, 0f), mesa.rotation, 0.5f);
                    Prop(huacales, grupo, posCaja3 + Vector3.down * 0.3f, giroCaja3, 0.6f);
                }
                if (tambo != null)
                {
                    Prop(tambo, grupo, posCaja3 + giroCaja3 * new Vector3(0.7f, -0.3f, 0f), giroCaja3, 0.85f);
                }
                n++;
            }
            Informe.AppendLine("Puestos de cubos de T-407 cambiados por mesa con huacales y tambo: " + n);
        }

        private static string RutaPrefab(string codigo)
        {
            string n = ImportarT500.RutasPrefab.FirstOrDefault(r => Path.GetFileNameWithoutExtension(r) == codigo);
            return n ?? ArteNoche.Buscar(codigo).RutaPrefab;
        }

        private static GameObject Prop(GameObject prefab, Transform padre, Vector3 pos, Quaternion giro, float altoDeseado)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(pos, giro);
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (Renderer rr in go.GetComponentsInChildren<Renderer>()) { b.Encapsulate(rr.bounds); }
            if (b.size.y > 0.01f) { go.transform.localScale = Vector3.one * Mathf.Clamp(altoDeseado / b.size.y, 0.3f, 1.5f); }
            foreach (Collider c in go.GetComponentsInChildren<Collider>()) { Object.DestroyImmediate(c); }
            return go;
        }

        private static void RetexturarMantas()
        {
            Material manta = MaterialManta();
            int n = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.name == "Manta" && r.sharedMaterial != null && r.sharedMaterial.name == "T407_Manta")
                {
                    r.sharedMaterial = manta;
                    EditorUtility.SetDirty(r);
                    n++;
                }
            }
            Informe.AppendLine("Mantas con tela y calavera: " + n);
        }

        // ------------------------------------------------------------------ materiales

        private static Clase Clasificar(string s)
        {
            if (s.Contains("Edif_") || s.Contains("Pobre_") || s == "Barrio_Casa" || s == "Barrio_Bar" || s == "Barrio_Casino" || s == "Barrio_Clandestino") { return Clase.Ventanas; }
            if (s.Contains("Lamina")) { return Clase.Lamina; }
            if (s.Contains("Banqueta") || s.Contains("Tierra") || s.Contains("Asfalto") || s.Contains("ConcretoOscuro") || s.Contains("Final_Concreto")) { return Clase.Losa; }
            if (s.Contains("Concreto") || s.Contains("Cornisa")) { return Clase.Revoque; }
            if (s.Contains("Madera") || s.Contains("Mesa") || s.Contains("Caja")) { return Clase.Madera; }
            if (s.Contains("Lona") || s.Contains("Toldo")) { return Clase.Lona; }
            return Clase.Metal;
        }

        /// <summary>T-509: material con textura para el original liso (el mismo que usa Colocar).</summary>
        public static Material ArteDe(Material original) { return MaterialConArte(original); }

        private static Material MaterialConArte(Material original)
        {
            string nombre = "T506_" + original.name;
            if (Cache.TryGetValue(nombre, out Material hecho)) { return hecho; }
            Clase clase = Clasificar(original.name);
            Color tinte = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : (original.HasProperty("_Color") ? original.GetColor("_Color") : Color.gray);
            float brillo = original.HasProperty("_Smoothness") ? original.GetFloat("_Smoothness") : 0.2f;
            Texture2D lados;
            Texture2D arriba;
            Vector2 escala;
            float metal = 0f;
            switch (clase)
            {
                case Clase.Ventanas: lados = Tex("T506_MuroVentanas"); arriba = Tex("T506_Revoque"); escala = new Vector2(4f, 3.2f); brillo = 0.15f; break;
                case Clase.Revoque: lados = Tex("T506_Revoque"); arriba = lados; escala = new Vector2(3f, 3f); brillo = 0.15f; break;
                case Clase.Losa: lados = Tex("T506_Losa"); arriba = lados; escala = new Vector2(3f, 3f); brillo = 0.2f; break;
                case Clase.Madera: lados = Tex("T506_Madera"); arriba = lados; escala = new Vector2(1.2f, 1.2f); brillo = 0.15f; break;
                case Clase.Lona: lados = Tex("T506_Lona"); arriba = lados; escala = new Vector2(1.2f, 1.2f); brillo = 0.1f; break;
                case Clase.Lamina: lados = Tex("T506_Lamina"); arriba = lados; escala = new Vector2(2f, 2f); brillo = 0.3f; metal = 0.4f; break;
                default: lados = Tex("T506_Metal"); arriba = lados; escala = new Vector2(2f, 2f); brillo = Mathf.Max(0.3f, brillo); metal = 0.5f; break;
            }
            Material m = CrearMaterial(nombre, tinte, lados, arriba, escala, brillo, metal);
            Cache[nombre] = m;
            return m;
        }

        private static Material CrearMaterial(string nombre, Color tinte, Texture2D lados, Texture2D arriba, Vector2 escala, float brillo, float metal)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find(RutaShader));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.shader = Shader.Find(RutaShader);
            tinte.a = 1f;
            m.SetColor("_BaseColor", tinte);
            m.SetTexture("_BaseMap", lados);
            m.SetTexture("_TopMap", arriba);
            m.SetVector("_Escala", new Vector4(escala.x, escala.y, 0f, 0f));
            m.SetFloat("_Smoothness", brillo);
            m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material MaterialManta()
        {
            string ruta = CarpetaMateriales + "/T506_Manta.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            Texture2D tex = Tex("T506_Manta");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_EmissionMap", tex);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.6f, 0.6f, 0.6f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ texturas generadas (tonos casi blancos: el tinte del material pone el color)

        private static Texture2D Tex(string nombre)
        {
            string ruta = CarpetaTexturas + "/" + nombre + ".png";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (t != null) { return t; }
            const int N = 256;
            Color[] px = new Color[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float g = Pixel(nombre, x / (float)N, y / (float)N);
                    px[y * N + x] = new Color(g, g, g, 1f);
                }
            }
            if (nombre == "T506_Manta") { ColorearManta(px, N); }
            Texture2D bmp = new Texture2D(N, N, TextureFormat.RGB24, false);
            bmp.SetPixels(px);
            File.WriteAllBytes(ruta, bmp.EncodeToPNG());
            Object.DestroyImmediate(bmp);
            AssetDatabase.ImportAsset(ruta);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        /// <summary>Ruido de valor que se repite cada 'p' celdas (la textura se puede mosaiquear).</summary>
        private static float Ruido(float u, float v, int p, int semilla)
        {
            float x = u * p;
            float y = v * p;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(Mod(x0, p), Mod(y0, p), semilla);
            float b = Hash(Mod(x0 + 1, p), Mod(y0, p), semilla);
            float c = Hash(Mod(x0, p), Mod(y0 + 1, p), semilla);
            float d = Hash(Mod(x0 + 1, p), Mod(y0 + 1, p), semilla);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static int Mod(int a, int p)
        {
            return ((a % p) + p) % p;
        }

        private static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        private static float Fractal(float u, float v, int semilla)
        {
            return Ruido(u, v, 4, semilla) * 0.5f + Ruido(u, v, 8, semilla + 1) * 0.3f + Ruido(u, v, 32, semilla + 2) * 0.2f;
        }

        private static float Pixel(string nombre, float u, float v)
        {
            switch (nombre)
            {
                case "T506_Revoque":
                {
                    float g = 0.86f + (Fractal(u, v, 3) - 0.5f) * 0.28f;
                    if (Ruido(u, v, 64, 9) > 0.93f) { g -= 0.1f; }
                    if (Mathf.Abs(v - 0.5f) < 0.006f) { g -= 0.12f; }
                    return Mathf.Clamp01(g);
                }
                case "T506_MuroVentanas":
                {
                    float g = 0.86f + (Fractal(u, v, 5) - 0.5f) * 0.26f;
                    g -= Mathf.SmoothStep(0.25f, 0f, v) * 0.12f;
                    if (v < 0.012f || v > 0.988f) { g -= 0.14f; }
                    // Ventana con marco, reja y alféizar (1.6 m x 1.4 m en la celda de 4 m x 3.2 m).
                    bool marco = u > 0.29f && u < 0.71f && v > 0.34f && v < 0.80f;
                    bool vidrio = u > 0.31f && u < 0.69f && v > 0.37f && v < 0.77f;
                    if (marco) { g = 0.95f; }
                    if (vidrio)
                    {
                        g = 0.16f + Fractal(u, v, 11) * 0.06f;
                        float barra = Mathf.Abs(Mathf.Repeat(u * 12f, 1f) - 0.5f);
                        if (barra > 0.4f) { g = 0.72f; }
                        if (Mathf.Abs(v - 0.57f) < 0.012f) { g = 0.72f; }
                    }
                    if (u > 0.27f && u < 0.73f && v > 0.30f && v < 0.34f) { g = 0.6f; }
                    return Mathf.Clamp01(g);
                }
                case "T506_Losa":
                {
                    float g = 0.82f + (Fractal(u, v, 21) - 0.5f) * 0.3f;
                    float junta = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    if (junta < 0.012f || Mathf.Abs(u - 0.5f) < 0.006f || Mathf.Abs(v - 0.5f) < 0.006f) { g -= 0.28f; }
                    return Mathf.Clamp01(g);
                }
                case "T506_Madera":
                {
                    float tabla = Mathf.Floor(u * 10f);
                    float local = Mathf.Repeat(u * 10f, 1f);
                    float tono = 0.78f + Hash((int)tabla, 0, 5) * 0.2f;
                    float veta = Ruido(Mathf.Repeat(local * 0.3f + tabla * 0.1f, 1f), v, 8, 31);
                    float g = tono + (veta - 0.5f) * 0.16f + (Ruido(u, v, 16, 33) - 0.5f) * 0.08f;
                    if (local < 0.05f || local > 0.95f) { g -= 0.32f; }
                    if ((v < 0.06f || v > 0.94f) && Mathf.Abs(local - 0.5f) < 0.12f) { g -= 0.3f; }
                    return Mathf.Clamp01(g);
                }
                case "T506_Lona":
                {
                    float franja = Mathf.Floor(u * 6f) % 2f < 1f ? 1f : 0.62f;
                    float trama = Mathf.Sin(u * 256f * 1.6f) * Mathf.Sin(v * 256f * 1.6f) * 0.04f;
                    return Mathf.Clamp01(franja * (0.95f + trama) + (Fractal(u, v, 41) - 0.5f) * 0.1f);
                }
                case "T506_Lamina":
                {
                    float onda = 0.5f + 0.5f * Mathf.Sin(u * Mathf.PI * 2f * 8f);
                    float g = 0.62f + onda * 0.32f + (Fractal(u, v, 51) - 0.5f) * 0.2f;
                    if (Ruido(u, v, 16, 55) > 0.82f) { g *= 0.78f; }
                    return Mathf.Clamp01(g);
                }
                case "T506_Manta":
                {
                    float trama = 0.9f + Mathf.Sin(u * 256f * 1.3f) * Mathf.Sin(v * 256f * 1.3f) * 0.05f;
                    return Mathf.Clamp01(trama + (Fractal(u, v, 61) - 0.5f) * 0.1f);
                }
                default:
                {
                    // Metal pintado: paneles de 1 m con costuras y remaches, rayones y óxido en las orillas.
                    float g = 0.82f + (Fractal(u, v, 71) - 0.5f) * 0.22f;
                    float cx = Mathf.Repeat(u * 2f, 1f);
                    float cy = Mathf.Repeat(v * 2f, 1f);
                    float borde = Mathf.Min(Mathf.Min(cx, 1f - cx), Mathf.Min(cy, 1f - cy));
                    if (borde < 0.015f) { g -= 0.32f; }
                    float rx = Mathf.Abs(cx - 0.5f);
                    float ry = Mathf.Abs(cy - 0.5f);
                    if (Mathf.Abs(rx - 0.4f) < 0.022f && Mathf.Abs(ry - 0.4f) < 0.022f) { g += 0.12f; }
                    if (Ruido(u, v, 32, 77) > 0.86f) { g -= 0.14f; }
                    if (borde < 0.06f) { g -= (Ruido(u, v, 32, 79) - 0.3f) * 0.2f; }
                    return Mathf.Clamp01(g);
                }
            }
        }

        /// <summary>La manta lleva calavera magenta sobre tela oscura.</summary>
        private static void ColorearManta(Color[] px, int n)
        {
            Color fondo = new Color(0.55f, 0.06f, 0.42f);
            Color figura = new Color(1f, 0.55f, 0.9f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n - 0.5f;
                    float v = y / (float)n - 0.5f;
                    float g = px[y * n + x].r;
                    Color c = fondo * g;
                    bool craneo = (u * u) / 0.07f + ((v - 0.1f) * (v - 0.1f)) / 0.09f < 1f;
                    bool mandibula = Mathf.Abs(u) < 0.14f && v < -0.12f && v > -0.28f;
                    bool ojo = ((Mathf.Abs(u) - 0.12f) * (Mathf.Abs(u) - 0.12f)) / 0.0025f + ((v - 0.1f) * (v - 0.1f)) / 0.0036f < 1f;
                    bool nariz = Mathf.Abs(u) < 0.03f && v < 0.0f && v > -0.08f;
                    bool dientes = mandibula && Mathf.Repeat((u + 0.5f) * 14f, 1f) < 0.12f;
                    if ((craneo || mandibula) && !ojo && !nariz && !dientes) { c = figura * g; }
                    px[y * n + x] = new Color(c.r, c.g, c.b, 1f);
                }
            }
        }
    }
}
