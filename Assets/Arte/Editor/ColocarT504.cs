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
    /// T-504 (paso A4 del plan de ciudad): viste la entrada de El Mercado de Ciudad_Prologo (callejón x -25 a -19, z 10 a 37, portón E02 en el
    /// muro oeste a z 26) como entrada-mercado-noche: luz naranja que sale por el portón, papel picado de colores colgando en hileras de lado a
    /// lado, barreras C18 con franja turquesa, cámaras CY08, servidor CY05, puesto de implantes CY09, plataforma de dron CY07a y charcos de color.
    /// Todo cuelga de la raíz <see cref="NombreRaiz"/> y se rehace al correrlo otra vez (idempotente). El E02 y las primeras CY08 vienen de T-419
    /// y no se tocan. Quedan libres el punto de la puerta (<see cref="Entrada"/>), el carril desde la boca del callejón y los puntos de personajes.
    /// </summary>
    public static class ColocarT504
    {
        public const string NombreRaiz = "T504_Mercado";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Mercado";
        private const string CarpetaMateriales = Carpeta + "/Materiales";
        private const string CarpetaTexturas = Carpeta + "/Texturas";
        private const string CarpetaSuburbios = "Assets/Arte/Suburbios/Texturas";

        /// <summary>Callejón: caras de los muros y tramo con piezas (m).</summary>
        public const float MuroOeste = -25f;
        public const float MuroEste = -19f;
        public const float ZMin = 11f;
        public const float ZMax = 37.5f;
        /// <summary>Carril desde la boca del callejón hasta el portón (x, z mínimos y máximos) que ninguna pieza de T504 toca.</summary>
        public static readonly Rect CarrilBoca = new Rect(-24.2f, 9f, 2.6f, 12.5f);

        private static readonly StringBuilder Informe = new StringBuilder();
        private static Transform raiz;
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly Color Naranja = new Color(1f, 0.5f, 0.12f);
        private static readonly Color Magenta = new Color(1f, 0.15f, 0.8f);
        private static readonly Color Turquesa = new Color(0.1f, 0.95f, 0.85f);

        public static IReadOnlyList<string> Codigos
        {
            get { return new[] { "C18_BarreraCalle", "CY05_ServidorCallejero", "CY08_CamaraCorporativa", "CY09_PuestoImplantes", "CY07a_PlataformaDron", "C16_Tambo" }; }
        }

        /// <summary>El punto de entrada de El Mercado (donde el jugador abre la tienda con E).</summary>
        public static Vector3 Entrada
        {
            get
            {
                Transform t = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(x => x.name == "Entrada" && x.parent != null && x.parent.name == "Lugar_MercadoNegro");
                return t != null ? t.position : new Vector3(-23.5f, 0.2f, 26f);
            }
        }

        [MenuItem("Mecha Cyberpunk/T-504/Colocar Entrada de El Mercado")]
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
            Directory.CreateDirectory(CarpetaTexturas);
            ColocarNoche.ReunirObstaculos();
            Vector3 entrada = Entrada;
            ColocarNoche.obstaculos.Add(new Bounds(new Vector3(entrada.x, 1.5f, entrada.z), new Vector3(5f, 3f, 5f)));
            ColocarNoche.obstaculos.Add(new Bounds(new Vector3(CarrilBoca.center.x, 1.5f, CarrilBoca.center.y), new Vector3(CarrilBoca.width, 3f, CarrilBoca.height)));
            raiz = new GameObject(NombreRaiz).transform;

            Transform piezas = Grupo("Piezas");
            PonerBarreras(piezas);
            PonerServidor(piezas);
            PonerImplantes(piezas);
            PonerDron(piezas);
            PonerCamaras(piezas);
            PonerBasura(piezas);
            PonerLuces();
            // T-803: sin papel picado (Tony nunca lo pidió; playtest del 6-nov).
            CrearCharcos();

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t504-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-504 colocado:\n" + Informe);
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
            return ArteNoche.Buscar(codigo).RutaPrefab;
        }

        private static Vector3 Tam(string codigo)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo)).GetComponent<BoxCollider>().size;
        }

        /// <summary>x de la cara del muro del lado indicado (-1 oeste, 1 este) a esa z; si no hay, el valor por defecto.</summary>
        private static float XMuro(float z, int lado)
        {
            Vector3 desde = new Vector3(-22f, 1.5f, z);
            if (Physics.Raycast(desde, new Vector3(lado, 0f, 0f), out RaycastHit golpe, 4f, ~0, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(golpe.normal.x) > 0.85f)
            {
                return golpe.point.x;
            }
            return lado < 0 ? MuroOeste : MuroEste;
        }

        private static GameObject Instanciar(string codigo, Transform padre, Vector3 pos, float giro, bool sinCollider)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = codigo + "_T504";
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
            if (sinCollider)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>())
                {
                    c.enabled = false;
                }
            }
            return go;
        }

        /// <summary>
        /// Pieza junto al muro del lado indicado, de frente al callejón. Busca desde la z deseada hacia los lados la primera posición libre.
        /// separacion: hueco entre el muro y la pieza; altura &gt; 0: colgada del muro a esa altura (sin collider); giroExtra: gira la pieza sobre su sitio.
        /// </summary>
        private static GameObject PonerPegada(Transform padre, string codigo, float zDeseada, int lado, float separacion, float altura = 0f, float giroExtra = 0f)
        {
            Vector3 tam = Tam(codigo);
            Vector3 normal = new Vector3(-lado, 0f, 0f);
            float giro = ColocarNoche.GiroHacia(normal) + giroExtra;
            HashSet<string> motivos = new HashSet<string>();
            foreach (float desplazo in Enumerable.Range(0, 24).SelectMany(i => new[] { i * 0.5f, -i * 0.5f }).Distinct())
            {
                float z = zDeseada + desplazo;
                if (z < ZMin || z > ZMax)
                {
                    continue;
                }
                float x = XMuro(z, lado) + normal.x * (tam.z * 0.5f + separacion);
                float suelo = altura > 0f ? altura : ColocarNoche.Suelo(new Vector3(x, 0f, z));
                if (float.IsNaN(suelo))
                {
                    continue;
                }
                Vector3 pivote = new Vector3(x, suelo, z);
                Bounds caja = ColocarNoche.CajaGirada(tam, pivote, giro);
                if (!ColocarNoche.Libre(caja))
                {
                    motivos.Add(ColocarNoche.bloqueo);
                    continue;
                }
                ColocarNoche.obstaculos.Add(caja);
                return Instanciar(codigo, padre, pivote, giro, altura > 0f);
            }
            Informe.AppendLine("No cupo: " + codigo + " cerca de z " + zDeseada + " lado " + lado + " (bloqueos: " + string.Join(" | ", motivos) + ")");
            return null;
        }

        /// <summary>Pieza suelta: busca alrededor de (xDeseada, zDeseada) la primera posición libre (z primero, luego x hacia el centro del callejón).</summary>
        private static GameObject PonerSuelta(Transform padre, string codigo, float xDeseada, float zDeseada, Vector3 normal, float pasoX)
        {
            Vector3 tam = Tam(codigo);
            float giro = ColocarNoche.GiroHacia(normal);
            HashSet<string> motivos = new HashSet<string>();
            for (int ix = 0; ix < 8; ix++)
            {
                foreach (float desplazo in Enumerable.Range(0, 16).SelectMany(i => new[] { i * 0.4f, -i * 0.4f }).Distinct())
                {
                    float x = xDeseada + ix * pasoX;
                    float z = zDeseada + desplazo;
                    float suelo = ColocarNoche.Suelo(new Vector3(x, 0f, z));
                    if (float.IsNaN(suelo))
                    {
                        continue;
                    }
                    Vector3 pivote = new Vector3(x, suelo, z);
                    Bounds caja = ColocarNoche.CajaGirada(tam, pivote, giro);
                    if (!ColocarNoche.Libre(caja))
                    {
                        motivos.Add(ColocarNoche.bloqueo);
                        continue;
                    }
                    ColocarNoche.obstaculos.Add(caja);
                    return Instanciar(codigo, padre, pivote, giro, false);
                }
            }
            Informe.AppendLine("No cupo: " + codigo + " cerca de x " + xDeseada + " z " + zDeseada + " (bloqueos: " + string.Join(" | ", motivos.Take(6)) + ")");
            return null;
        }

        // ------------------------------------------------------------------ piezas

        private static void PonerBarreras(Transform padre)
        {
            Material franja = Material("Franja_Turquesa", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Turquesa * 2.2f));
            Material aviso = Material("Aviso_Naranja", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Naranja * 2.6f));
            Vector3 tam = Tam("C18_BarreraCalle");
            int n = 0;
            // Dos al pie del muro oeste a los lados del portón y dos del lado este, escalonadas para dejar el centro libre.
            foreach (var (z, lado, separacion) in new[] { (21.6f, -1, 0.6f), (30.6f, -1, 0.6f), (23.8f, 1, 0.7f), (29.4f, 1, 0.7f) })
            {
                GameObject b = PonerPegada(padre, "C18_BarreraCalle", z, lado, separacion);
                if (b == null)
                {
                    continue;
                }
                foreach (float cara in new[] { -1f, 1f })
                {
                    GameObject tira = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(tira.GetComponent<Collider>());
                    tira.name = "Franja_Turquesa";
                    tira.transform.SetParent(b.transform, false);
                    tira.transform.localPosition = new Vector3(0f, tam.y * 0.78f, cara * (tam.z * 0.5f + 0.012f));
                    tira.transform.localScale = new Vector3(tam.x * 0.82f, 0.05f, 0.03f);
                    Apagar(tira, franja);
                }
                GameObject foco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(foco.GetComponent<Collider>());
                foco.name = "Aviso_Naranja";
                foco.transform.SetParent(b.transform, false);
                foco.transform.localPosition = new Vector3(tam.x * 0.42f, tam.y + 0.06f, 0f);
                foco.transform.localScale = Vector3.one * 0.14f;
                Apagar(foco, aviso);
                n++;
            }
            Informe.AppendLine("Barreras C18 con franja turquesa y aviso naranja: " + n);
        }

        private static void Apagar(GameObject go, Material mat)
        {
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static void PonerServidor(Transform padre)
        {
            GameObject s = PonerSuelta(padre, "CY05_ServidorCallejero", -24f, 33.8f, new Vector3(1f, 0f, 0f), 0.3f);
            if (s != null)
            {
                Luz(s.transform, "Luz", s.transform.position + Vector3.up * 1.2f + new Vector3(1.1f, 0f, 0f), Turquesa, 2.6f, 4.5f);
            }
            Informe.AppendLine("Servidor CY05 con luz turquesa: " + (s != null ? 1 : 0));
        }

        private static void PonerImplantes(Transform padre)
        {
            GameObject p = PonerSuelta(padre, "CY09_PuestoImplantes", -20.6f, 31.5f, new Vector3(-1f, 0f, 0f), -0.3f);
            if (p != null)
            {
                Luz(p.transform, "Luz", p.transform.position + Vector3.up * 2.3f + new Vector3(-0.9f, 0f, 0f), Magenta, 2.8f, 5f);
            }
            Informe.AppendLine("Puesto de implantes CY09 con luz magenta: " + (p != null ? 1 : 0));
        }

        private static void PonerDron(Transform padre)
        {
            GameObject d = PonerPegada(padre, "CY07a_PlataformaDron", 26f, 1, 0.4f, 3.9f);
            if (d != null)
            {
                Luz(d.transform, "Luz", d.transform.position + Vector3.up * 1.4f + new Vector3(-0.8f, 0f, 0f), Turquesa, 2.2f, 4f);
            }
            Informe.AppendLine("Plataforma de dron CY07a con luz turquesa: " + (d != null ? 1 : 0));
        }

        private static void PonerCamaras(Transform padre)
        {
            int n = 0;
            foreach (var (z, lado) in new[] { (21.5f, 1), (31f, 1), (34.5f, -1) })
            {
                if (PonerPegada(padre, "CY08_CamaraCorporativa", z, lado, 0.4f, 4.6f) != null)
                {
                    n++;
                }
            }
            Informe.AppendLine("Cámaras CY08 hacia el portón: " + n);
        }

        private static void PonerBasura(Transform padre)
        {
            int n = 0;
            foreach (var (codigo, z, separacion) in new[] { ("C16_Tambo", 20.2f, 0.5f) })
            {
                if (PonerPegada(padre, codigo, z, -1, separacion) != null)
                {
                    n++;
                }
            }
            Informe.AppendLine("Tambos C16: " + n);
        }

        // ------------------------------------------------------------------ luces

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

        /// <summary>La luz naranja que sale del portón: una adentro (se ve por la puerta) y otra afuera que pinta el piso del callejón.</summary>
        private static void PonerLuces()
        {
            Transform g = Grupo("LucesPuerta");
            Vector3 e = Entrada;
            Luz(g, "Luz_Adentro", new Vector3(MuroOeste - 1.6f, 1.9f, e.z), Naranja, 6f, 7f);
            Luz(g, "Luz_Derrame", new Vector3(MuroOeste + 0.9f, 1.4f, e.z), Naranja, 5f, 8f);
            Informe.AppendLine("Luces naranjas del portón: 2");
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

        // ------------------------------------------------------------------ charcos

        private static void CrearCharcos()
        {
            Transform g = Grupo("Charcos");
            Color[] tintes = { Magenta, Turquesa, Naranja, Magenta, Turquesa };
            float[] xs = { -22.4f, -21.4f, -23.4f, -22.0f, -20.8f };
            float[] zs = { 19.4f, 26.4f, 31.2f, 14.6f, 34.5f };
            int n = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                Color tinte = tintes[i];
                Material mat = Material("Charco_Mercado_" + i, "Universal Render Pipeline/Unlit", m =>
                {
                    m.SetTexture("_BaseMap", Tex("Charco_Redondo"));
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
                quad.transform.rotation = Quaternion.Euler(90f, i * 53f, 0f);
                float tamano = 1.5f + (i % 3) * 0.5f;
                quad.transform.localScale = new Vector3(tamano * 1.4f, tamano, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Charcos de color: " + n);
        }
    }
}
