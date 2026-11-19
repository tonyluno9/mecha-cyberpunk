using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1392: look de noche, ronda 2. Corrige lo que T-1383 dejó oscuro y azul marino contra K11, K13, K15 y K18.
    /// Aplicar(): (1) charcos con el material y el reflejo plano de los barrios (S2c_Charco_n + ReflejoCharcos, capa Water), forma irregular por la máscara;
    /// (2) ambiente, cielo y niebla de noche de ConfigCicloDia_Mundo más claros y azul grisáceo (moderado, los barrios lo comparten) y un Volume local
    /// por zona (exposición, bloom) que solo se nota de noche; (3) faroles de sodio con más intensidad y alcance; (4) neones: enciende la emisión de los
    /// materiales NE01/NE01B/NE02 (no tenían la keyword _EMISSION) y pone o refuerza una luz por letrero; (5) el skyline SK01 con un shader aditivo sin niebla.
    /// DEBE correr DESPUÉS de las zonas de la ronda 2 (para encontrar sus letreros y su geometría). Idempotente.
    /// Batchmode (carpeta principal): -executeMethod MechaCyberpunk.Arte.Editor.LookNocheT1392.Aplicar, y en otra corrida .Fotos.
    /// Informe en Logs/t1392-look.txt.
    /// </summary>
    public static class LookNocheT1392
    {
        private const string CarpetaZonas = "Assets/Arte/Mundo/Zonas/";
        private const string CarpetaLook = "Assets/Arte/Mundo/Look";
        private const string RutaMallaCharco = CarpetaLook + "/Mallas/T1392_CharcoPlano.asset";
        private const string RutaPerfilZonas = CarpetaLook + "/PerfilZonasNoche_T1392.asset";
        private const string RutaShaderSkyline = "MechaCyberpunk/Fondos/SkylineSinNiebla";
        private const string RutaInforme = "Logs/t1392-look.txt";
        private const string RaizVolumenes = "T1392_Volumenes";

        private static readonly string[] Zonas = { "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto", "T1388_Uniones" };
        private static readonly string[] MaterialesSkyline = { "Assets/Arte/Fondos/M_SK01_Skyline.mat", "Assets/Arte/Fondos/M_SK01_Skyline_b.mat" };
        private static readonly string[] GruposPropios = { "T1383_Charcos", "T1383_LucesNeon", "T1392_Charcos", "T1392_LucesNeon" };

        // Charcos (más grandes y con el material de los barrios)
        private const float AltoCharco = 0.02f;
        private const float CharcoMin = 2.5f;
        private const float CharcoMax = 6.5f;
        private const int CharcosMin = 30;
        private const int CharcosMax = 70;
        private const float MetrosPorCharco = 15f;
        private const float MargenCharcoBanqueta = 1.2f;
        private const float DistanciaCharcoViejo = 4f;
        private const float PisoMinimoCharco = -0.3f; // ConfigCharcos_Mundo: pisoMinimo -0.5 y pisoMaximo 0.6 (el plano del reflejo solo sigue pisos en ese rango)
        private const float PisoMaximoCharco = 0.55f;
        private const float PendienteMaxima = 0.08f;
        private const int SemillaCharcos = 1392;
        private const int FormasCharco = 4;

        // Faroles de sodio (antes de T-1392: intensidad 170, alcance 16, cono 100/40)
        private static readonly Color Sodio = new Color(1f, 0.55f, 0.2f);
        public const float IntensidadSodio = 400f;
        public const float AlcanceSodio = 22f;
        private const float AnguloSodio = 110f;
        private const float AnguloInteriorSodio = 45f;

        // Neones (las luces de KitS2c.Neon eran 0.35 / 3.5 m; las de T-1383, 3 / 5 m)
        private const float IntensidadNeon = 8f;
        private const float AlcanceNeon = 8f;
        private const int NeonesNuevosPorZona = 60;
        private const float CeldaNeon = 4f;
        private const float DistanciaLuzNeon = 2.5f;
        private const float TamanoMaximoNeon = 20f;
        private const float DistanciaMaximaCalle = 45f;
        private const float EmpujeNeon = 0.8f;
        private const int TopeLucesEscena = 1400;

        // Ambiente, cielo y niebla de noche (claves del gradiente con tiempo <= 0.28 o >= 0.8; orden por tiempo)
        private static readonly Color[] AmbienteNoche = { new Color(0.46f, 0.52f, 0.66f), new Color(0.44f, 0.46f, 0.64f), new Color(0.34f, 0.36f, 0.56f), new Color(0.46f, 0.52f, 0.66f) };
        private static readonly Color NieblaNoche = new Color(0.15f, 0.21f, 0.32f);
        private static readonly Color CieloNoche = new Color(0.07f, 0.11f, 0.22f); // solo las claves de tiempo 0 y >= 0.8 (noche cerrada)

        // Volumen local por zona
        private const float ExposicionZona = 0.7f;
        private const float ContrasteZona = 10f;
        private const float SaturacionZona = 6f;
        private const float UmbralBloomZona = 0.8f;
        private const float IntensidadBloomZona = 1.8f;
        private const float DifusionBloomZona = 0.7f;
        private const float MezclaVolumen = 25f;
        private const float PrioridadVolumen = 11f; // por encima del volumen de sodio del Malecón (10): ese solo define el color
        private const float MargenVolumen = 60f;

        private const float LejosFotos = 1100f;

        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1392/Aplicar look ronda 2")]
        public static void Aplicar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1392: look de noche, ronda 2 (correr al final de las zonas)");
            ColocarS2.Abrir();
            int lucesAntes = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Informe.AppendLine("Luces de la escena antes: " + lucesAntes + " (tope seguro " + TopeLucesEscena + ")");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Directory.CreateDirectory(CarpetaLook + "/Mallas");
            Mesh malla = MallaCharco();
            int presupuesto = Mathf.Max(0, TopeLucesEscena - lucesAntes);
            int nuevasPorZona = Mathf.Min(NeonesNuevosPorZona, presupuesto / Zonas.Length);
            Informe.AppendLine("Luces de neón nuevas permitidas por zona: " + nuevasPorZona);

            HashSet<Material> encendidos = new HashSet<Material>();
            for (int i = 0; i < Zonas.Length; i++)
            {
                Zona(Zonas[i], i, malla, nuevasPorZona, encendidos);
            }
            Skyline();
            AssetDatabase.SaveAssets();

            ColocarS2.Abrir();
            Global();
            VolumenesDeZona();
            int lucesDespues = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Informe.AppendLine("Luces de la escena después: " + lucesDespues + (lucesDespues > TopeLucesEscena ? " AVISO: pasa del tope " + TopeLucesEscena : ""));
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, Informe.ToString());
            Debug.Log("[Arte] " + Informe);
        }

        // ================================================================ Una zona

        private static void Zona(string nombre, int indice, Mesh malla, int neonesMax, HashSet<Material> encendidos)
        {
            string ruta = CarpetaZonas + nombre + ".prefab";
            Informe.AppendLine();
            Informe.AppendLine("== " + nombre);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
            {
                Informe.AppendLine("No existe " + ruta + ": se salta.");
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                int antes = raiz.GetComponentsInChildren<Light>(true).Length;
                foreach (string g in GruposPropios)
                {
                    Transform t = raiz.transform.Find(g);
                    if (t != null)
                    {
                        Object.DestroyImmediate(t.gameObject);
                    }
                }
                List<Vector3> calle = PuntosDeCalle(raiz);
                int sodio = AjustarSodio(raiz, nombre == "T1355_Cerro" ? RendimientoCerroT1365.AlcanceMaximoLuz : AlcanceSodio); // el Cerro mantiene el tope de rendimiento de T-1365 (18 m)
                int charcos = PonerCharcos(raiz, indice, malla);
                int reforzadas;
                int nuevas = PonerNeones(raiz, calle, neonesMax, encendidos, out reforzadas);
                int despues = raiz.GetComponentsInChildren<Light>(true).Length;
                Informe.AppendLine("Faroles de sodio ajustados: " + sodio + " (intensidad " + IntensidadSodio + ", alcance " + AlcanceSodio + ", cono " + AnguloSodio + "/" + AnguloInteriorSodio + ")");
                Informe.AppendLine("Charcos nuevos (material S2c_Charco_n, capa Water): " + charcos);
                Informe.AppendLine("Luces de neón reforzadas: " + reforzadas + "; nuevas: " + nuevas);
                Informe.AppendLine("Luces (Light) en el prefab: antes " + antes + ", después " + despues);
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static List<Vector3> PuntosDeCalle(GameObject raiz)
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (CalleZona cz in raiz.GetComponentsInChildren<CalleZona>(true))
            {
                puntos.AddRange(cz.Puntos().Select(p => raiz.transform.InverseTransformPoint(p)));
            }
            return puntos;
        }

        // ---------------------------------------------------------------- Faroles de sodio

        internal static bool EsSodio(Light l)
        {
            Color c = l.color;
            bool naranja = c.r >= 0.85f && c.g >= 0.35f && c.g <= 0.8f && c.b <= 0.5f;
            string padre = l.transform.parent != null ? l.transform.parent.name : "";
            bool deFarol = padre.StartsWith("Poste") || padre.Contains("Farol");
            return deFarol && (l.type == LightType.Spot || l.type == LightType.Point) && (naranja || l.range >= 10f);
        }

        private static int AjustarSodio(GameObject raiz, float alcance)
        {
            int n = 0;
            foreach (Light l in raiz.GetComponentsInChildren<Light>(true))
            {
                if (!EsSodio(l))
                {
                    continue;
                }
                l.color = Sodio;
                l.range = alcance;
                l.intensity = IntensidadSodio;
                if (l.type == LightType.Spot)
                {
                    l.spotAngle = AnguloSodio;
                    l.innerSpotAngle = AnguloInteriorSodio;
                }
                l.shadows = LightShadows.None;
                n++;
            }
            return n;
        }

        // ---------------------------------------------------------------- Charcos

        private static Mesh MallaCharco()
        {
            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMallaCharco);
            if (m == null)
            {
                m = new Mesh { name = "T1392_CharcoPlano" };
                AssetDatabase.CreateAsset(m, RutaMallaCharco);
            }
            m.Clear();
            m.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.RecalculateBounds();
            EditorUtility.SetDirty(m);
            return m;
        }

        private struct Muestra
        {
            public Vector3 Punto;
            public Vector3 Tangente;
            public float Ancho;
        }

        private static List<Muestra> Muestrear(GameObject raiz)
        {
            List<Muestra> salida = new List<Muestra>();
            foreach (CalleZona cz in raiz.GetComponentsInChildren<CalleZona>(true))
            {
                List<Vector3> p = cz.Puntos().Select(q => raiz.transform.InverseTransformPoint(q)).ToList();
                float siguiente = 0.5f;
                float acumulado = 0f;
                for (int i = 0; i + 1 < p.Count; i++)
                {
                    float seg = Vector3.Distance(p[i], p[i + 1]);
                    while (siguiente <= acumulado + seg && seg > 0.001f)
                    {
                        float t = (siguiente - acumulado) / seg;
                        salida.Add(new Muestra { Punto = Vector3.Lerp(p[i], p[i + 1], t), Tangente = (p[i + 1] - p[i]).normalized, Ancho = cz.Ancho });
                        siguiente += 1f;
                    }
                    acumulado += seg;
                }
            }
            return salida;
        }

        private static int PonerCharcos(GameObject raiz, int indice, Mesh malla)
        {
            List<Muestra> todas = Muestrear(raiz);
            if (todas.Count == 0)
            {
                Informe.AppendLine("Sin CalleZona: no hay dónde poner charcos.");
                return 0;
            }
            // Solo en tramos llanos y a la altura de piso que sigue ReflejoCharcos: en rampas o pisos elevados el reflejo saldría mal.
            List<Muestra> validas = todas.Where(m => Mathf.Abs(m.Tangente.y) <= PendienteMaxima && m.Punto.y >= PisoMinimoCharco && m.Punto.y <= PisoMaximoCharco).ToList();
            Informe.AppendLine("Muestras de calle: " + todas.Count + " m, llanas y a nivel de piso: " + validas.Count + " m");
            if (validas.Count == 0)
            {
                return 0;
            }
            System.Random azar = new System.Random(SemillaCharcos + indice);
            List<Vector3> ocupados = raiz.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("S2c_Charco"))
                .Select(r => r.bounds.center).ToList();
            Material[] materiales = new Material[FormasCharco];
            for (int f = 0; f < FormasCharco; f++)
            {
                int forma = f;
                materiales[f] = KitS2c.Mat("S2c_Charco_" + forma, () =>
                {
                    Material mat = new Material(Shader.Find("MechaCyberpunk/Mundo/Charco"));
                    mat.SetTexture("_Mascara", KitS2c.TexCharco(forma));
                    return mat;
                });
            }
            int objetivo = Mathf.Clamp(Mathf.RoundToInt(validas.Count / MetrosPorCharco), CharcosMin, CharcosMax);
            Transform grupo = new GameObject("T1392_Charcos").transform;
            grupo.SetParent(raiz.transform, false);
            int puestos = 0;
            for (int k = 0; k < objetivo * 4 && puestos < objetivo; k++)
            {
                Muestra m = validas[Mathf.Min(validas.Count - 1, Mathf.FloorToInt((puestos + (float)azar.NextDouble()) * validas.Count / objetivo))];
                float tamano = Mathf.Lerp(CharcoMin, CharcoMax, (float)azar.NextDouble());
                tamano = Mathf.Min(tamano, Mathf.Max(2f, m.Ancho - 2f * MargenCharcoBanqueta));
                float lateralMax = Mathf.Max(0f, m.Ancho * 0.5f - MargenCharcoBanqueta - tamano * 0.5f);
                Vector3 plano = new Vector3(m.Tangente.x, 0f, m.Tangente.z);
                if (plano.sqrMagnitude < 0.0001f)
                {
                    continue;
                }
                plano.Normalize();
                Vector3 lado = Vector3.Cross(Vector3.up, plano);
                Vector3 posicion = m.Punto + lado * (((float)azar.NextDouble() * 2f - 1f) * lateralMax);
                posicion.y = m.Punto.y + AltoCharco;
                if (ocupados.Any(v => (v - posicion).sqrMagnitude < DistanciaCharcoViejo * DistanciaCharcoViejo))
                {
                    continue;
                }
                GameObject go = new GameObject("Charco");
                go.layer = ReflejoCharcos.CapaCharcos;
                go.transform.SetParent(grupo, false);
                // Alineado con la calle (±35°) para no salirse del arroyo; la forma irregular viene de la máscara S2c_Charco_n.
                go.transform.localRotation = Quaternion.LookRotation(plano, Vector3.up) * Quaternion.Euler(0f, ((float)azar.NextDouble() * 2f - 1f) * 35f, 0f);
                go.transform.localPosition = posicion;
                go.transform.localScale = new Vector3(tamano * Mathf.Lerp(0.6f, 1f, (float)azar.NextDouble()), 1f, tamano);
                go.AddComponent<MeshFilter>().sharedMesh = malla;
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = materiales[azar.Next(FormasCharco)];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                go.AddComponent<CharcoVisible>();
                ocupados.Add(posicion);
                puestos++;
            }
            return puestos;
        }

        // ---------------------------------------------------------------- Neones

        private static readonly Dictionary<Material, Color?> ColoresNeon = new Dictionary<Material, Color?>();

        /// <summary>El color del letrero: promedio de hue de los píxeles brillantes del mapa de emisión (se lee el archivo, no hace falta que sea legible) o, si no, _EmissionColor.</summary>
        internal static Color? ColorDeNeon(Material m)
        {
            if (m == null || !m.HasProperty("_EmissionColor") || m.name.Contains("Ventana"))
            {
                return null;
            }
            if (ColoresNeon.TryGetValue(m, out Color? ya))
            {
                return ya;
            }
            Color? resultado = null;
            Color e = m.GetColor("_EmissionColor");
            float max = Mathf.Max(e.r, e.g, e.b);
            Texture tex = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
            bool nombre = m.name.StartsWith("NE0") || m.name.Contains("Neon") || m.name.Contains("Pantalla");
            if (tex != null && (max >= 0.5f || nombre))
            {
                string ruta = AssetDatabase.GetAssetPath(tex);
                if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta))
                {
                    Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(t, File.ReadAllBytes(ruta)))
                    {
                        Color32[] px = t.GetPixels32();
                        int paso = Mathf.Max(1, px.Length / 4096);
                        float sx = 0f, sy = 0f, total = 0f;
                        for (int i = 0; i < px.Length; i += paso)
                        {
                            Color.RGBToHSV(px[i], out float h, out float s, out float v);
                            if (s < 0.4f || v < 0.5f)
                            {
                                continue;
                            }
                            float w = s * v;
                            sx += Mathf.Cos(h * 2f * Mathf.PI) * w;
                            sy += Mathf.Sin(h * 2f * Mathf.PI) * w;
                            total += w;
                        }
                        if (total > 0.001f)
                        {
                            float hue = Mathf.Atan2(sy, sx) / (2f * Mathf.PI);
                            hue = hue < 0f ? hue + 1f : hue;
                            resultado = Color.HSVToRGB(hue, 1f, 1f);
                        }
                    }
                    Object.DestroyImmediate(t);
                }
            }
            if (resultado == null && max >= 0.8f)
            {
                Color.RGBToHSV(new Color(e.r / max, e.g / max, e.b / max), out float h, out float s, out float _);
                if (s >= 0.5f)
                {
                    resultado = Color.HSVToRGB(h, 1f, 1f);
                }
            }
            ColoresNeon[m] = resultado;
            return resultado;
        }

        internal static bool Excluido(Renderer r)
        {
            for (Transform t = r.transform; t != null; t = t.parent)
            {
                string n = t.name;
                if (n.StartsWith("T1392_") || n.Contains("Carro") || n.Contains("Dron") || n.Contains("Patrulla") || n.Contains("Moto"))
                {
                    return true;
                }
            }
            return false;
        }

        private static int PonerNeones(GameObject raiz, List<Vector3> calle, int maximo, HashSet<Material> encendidos, out int reforzadas)
        {
            reforzadas = 0;
            Light[] luces = raiz.GetComponentsInChildren<Light>(true);
            HashSet<Light> tocadas = new HashSet<Light>();
            HashSet<Vector2Int> celdas = new HashSet<Vector2Int>();
            List<(Renderer r, Color color, float dist, Vector3 centro)> sinLuz = new List<(Renderer, Color, float, Vector3)>();
            int sinGlow = 0;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (Excluido(r) || r.GetComponentInParent<Light>() != null)
                {
                    continue;
                }
                Color? color = null;
                foreach (Material m in r.sharedMaterials)
                {
                    color = ColorDeNeon(m);
                    if (color.HasValue)
                    {
                        // Los NE01/NE01B/NE02 no tenían la keyword _EMISSION ni flags de emisión en tiempo real: sin eso la emisión no se pinta.
                        if (encendidos.Add(m))
                        {
                            if (!m.IsKeywordEnabled("_EMISSION"))
                            {
                                m.EnableKeyword("_EMISSION");
                                sinGlow++;
                                Informe.AppendLine("  Emisión encendida (faltaba _EMISSION): " + AssetDatabase.GetAssetPath(m));
                            }
                            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                            EditorUtility.SetDirty(m);
                        }
                        break;
                    }
                }
                if (!color.HasValue)
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.magnitude > TamanoMaximoNeon)
                {
                    continue;
                }
                // Si el letrero ya trae su luz (grupo "Neon" de KitS2c.Neon o una luz chica a menos de 2.5 m), se refuerza en lugar de agregar otra.
                Light propia = null;
                Transform grupoNeon = r.transform;
                while (grupoNeon != null && grupoNeon.name != "Neon" && grupoNeon != raiz.transform)
                {
                    grupoNeon = grupoNeon.parent;
                }
                if (grupoNeon != null && grupoNeon.name == "Neon")
                {
                    propia = grupoNeon.GetComponentInChildren<Light>(true);
                }
                if (propia == null)
                {
                    propia = luces.FirstOrDefault(l => l != null && l.type == LightType.Point && l.range <= 10f && (l.transform.position - b.center).sqrMagnitude < DistanciaLuzNeon * DistanciaLuzNeon);
                }
                if (propia != null)
                {
                    if (tocadas.Add(propia))
                    {
                        propia.intensity = IntensidadNeon;
                        propia.range = AlcanceNeon;
                        propia.shadows = LightShadows.None;
                        reforzadas++;
                    }
                    continue;
                }
                float dist = calle.Count > 0 ? DistanciaACalle(calle, b.center) : 0f;
                if (dist <= DistanciaMaximaCalle)
                {
                    sinLuz.Add((r, color.Value, dist, b.center));
                }
            }
            Informe.AppendLine("Materiales de neón con la emisión recién encendida en esta zona: " + sinGlow);
            if (maximo <= 0 || sinLuz.Count == 0)
            {
                return 0;
            }
            Transform grupo = new GameObject("T1392_LucesNeon").transform;
            grupo.SetParent(raiz.transform, false);
            int puestas = 0;
            foreach (var c in sinLuz.OrderBy(x => x.dist))
            {
                if (puestas >= maximo)
                {
                    break;
                }
                Vector2Int celda = new Vector2Int(Mathf.FloorToInt(c.centro.x / CeldaNeon), Mathf.FloorToInt(c.centro.z / CeldaNeon));
                if (!celdas.Add(celda))
                {
                    continue;
                }
                Vector3 posicion = c.centro + HaciaCalle(calle, c.centro) * EmpujeNeon;
                Light l = KitS2c.Luz(grupo, posicion, LightType.Point, c.color, IntensidadNeon, AlcanceNeon, 0f);
                l.name = "LuzNeon";
                l.shadows = LightShadows.None;
                puestas++;
            }
            return puestas;
        }

        internal static float DistanciaACalle(List<Vector3> pts, Vector3 p)
        {
            float mejor = float.MaxValue;
            foreach (Vector3 q in pts)
            {
                mejor = Mathf.Min(mejor, (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z));
            }
            return Mathf.Sqrt(mejor);
        }

        private static Vector3 HaciaCalle(List<Vector3> calle, Vector3 p)
        {
            if (calle.Count == 0)
            {
                return Vector3.zero;
            }
            Vector3 mejor = calle[0];
            float d = float.MaxValue;
            foreach (Vector3 q in calle)
            {
                float dd = (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z);
                if (dd < d)
                {
                    d = dd;
                    mejor = q;
                }
            }
            Vector3 v = new Vector3(mejor.x - p.x, 0f, mejor.z - p.z);
            return v.sqrMagnitude < 0.0001f ? Vector3.zero : v.normalized;
        }

        // ---------------------------------------------------------------- Skyline sin niebla

        private static void Skyline()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Skyline SK01");
            Shader shader = Shader.Find(RutaShaderSkyline);
            if (shader == null)
            {
                Informe.AppendLine("AVISO: no se encontró el shader " + RutaShaderSkyline + " (¿no compiló?). Skyline sin cambiar.");
                return;
            }
            foreach (string ruta in MaterialesSkyline)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null)
                {
                    Informe.AppendLine("No existe " + ruta);
                    continue;
                }
                string antes = m.shader != null ? m.shader.name : "?";
                Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                Color color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : new Color(1.6f, 1.6f, 1.6f, 1f);
                m.shader = shader;
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", color);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                EditorUtility.SetDirty(m);
                Informe.AppendLine(ruta + ": shader " + antes + " -> " + shader.name + " (aditivo, sin niebla, de día al 15 %)");
            }
            Informe.AppendLine("Far clip del jugador en juego: 1000 m (MechaJugador, JugadorAPie y Ciudad_Prologo); el arco de SK01 es de radio 900 m: queda dentro. Fotos: " + LejosFotos + " m (antes 600).");
        }

        // ================================================================ Global: ambiente, cielo y niebla

        private static void Global()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Global (ConfigCicloDia_Mundo: lo comparten los barrios viejos, cambio moderado)");
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            if (config == null)
            {
                Informe.AppendLine("AVISO: no se pudo cargar ConfigCicloDia_Mundo.");
                return;
            }
            Informe.AppendLine("Valores del asset en la rama antes de T-1392: colorAmbiente noche (0.4,0.41,0.47) / (0.38,0.33,0.52) / dusk (0.24,0.24,0.42); colorNiebla noche #1a1630 (0.102,0.086,0.188); colorCielo noche (0.035,0.055,0.13); densidad de niebla 0.006 a 0.009 (exp. al cuadrado, sin tocar).");
            Gradiente(config, "colorAmbiente", AmbienteNoche, false);
            Gradiente(config, "colorNiebla", new[] { NieblaNoche, NieblaNoche, NieblaNoche, NieblaNoche }, false);
            Gradiente(config, "colorCielo", new[] { CieloNoche, null as Color?, null as Color?, CieloNoche }.Select(c => c ?? Color.clear).ToArray(), true);
            EditorUtility.SetDirty(config);
            RenderSettings.fog = true;
            RenderSettings.fogColor = NieblaNoche;
            Informe.AppendLine("RenderSettings de la escena: fogColor " + NieblaNoche + " (en juego manda el ciclo).");
        }

        /// <summary>Pone los colores de noche del gradiente: las claves con tiempo &lt;= 0.28 o &gt;= 0.8, en orden. Con <paramref name="omitirClear"/>, las de alfa 0 (Color.clear) no se tocan.</summary>
        private static void Gradiente(ConfigCicloDia config, string campoNombre, Color[] objetivos, bool omitirClear)
        {
            FieldInfo campo = typeof(ConfigCicloDia).GetField(campoNombre, BindingFlags.NonPublic | BindingFlags.Instance);
            if (campo == null)
            {
                Informe.AppendLine("AVISO: no existe el campo " + campoNombre);
                return;
            }
            Gradient g = (Gradient)campo.GetValue(config);
            GradientColorKey[] claves = g.colorKeys;
            int n = 0;
            for (int i = 0; i < claves.Length; i++)
            {
                if (claves[i].time > 0.28f && claves[i].time < 0.8f)
                {
                    continue;
                }
                Color objetivo = objetivos[Mathf.Min(n, objetivos.Length - 1)];
                n++;
                if (omitirClear && objetivo == Color.clear)
                {
                    continue;
                }
                Informe.AppendLine("  " + campoNombre + " t=" + claves[i].time.ToString("F2") + ": " + claves[i].color + " -> " + objetivo);
                claves[i].color = objetivo;
            }
            g.SetKeys(claves, g.alphaKeys);
            campo.SetValue(config, g);
        }

        // ================================================================ Volúmenes locales por zona (en la escena)

        private static void VolumenesDeZona()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Volúmenes locales por zona (escena Ciudad_Prologo, bajo " + RaizVolumenes + ")");
            GameObject vieja = GameObject.Find(RaizVolumenes);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            if (ciclo == null || ciclo.VolumeNoche == null)
            {
                Informe.AppendLine("AVISO: no hay CicloDiaNoche con VolumeNoche en la escena: sin volúmenes locales.");
                return;
            }
            VolumeProfile perfil = PerfilZonas();
            Transform raiz = new GameObject(RaizVolumenes).transform;
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (string zona in Zonas)
            {
                Transform instancia = todos.FirstOrDefault(t => t.name == zona);
                if (instancia == null)
                {
                    Informe.AppendLine(zona + ": no hay instancia en la escena; sin volumen.");
                    continue;
                }
                Bounds? caja = CajaDeZona(instancia, zona);
                if (!caja.HasValue)
                {
                    Informe.AppendLine(zona + ": sin calles ni renderers medibles; sin volumen.");
                    continue;
                }
                Bounds c = caja.Value;
                c.Expand(new Vector3(MargenVolumen * 2f, 0f, MargenVolumen * 2f));
                Vector3 min = new Vector3(c.min.x, c.min.y - 12f, c.min.z);
                Vector3 max = new Vector3(c.max.x, c.max.y + 45f, c.max.z);
                GameObject go = new GameObject("VolumenNoche_T1392_" + zona);
                go.transform.SetParent(raiz, false);
                go.transform.position = (min + max) * 0.5f;
                BoxCollider col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = max - min;
                Volume v = go.AddComponent<Volume>();
                v.isGlobal = false;
                v.blendDistance = MezclaVolumen;
                v.priority = PrioridadVolumen;
                v.weight = 1f;
                v.sharedProfile = perfil;
                VolumenSigueNoche sigue = go.AddComponent<VolumenSigueNoche>();
                SerializedObject so = new SerializedObject(sigue);
                so.FindProperty("origen").objectReferenceValue = ciclo.VolumeNoche;
                so.ApplyModifiedPropertiesWithoutUndo();
                Informe.AppendLine(zona + ": caja " + min + " a " + max + ", mezcla " + MezclaVolumen + " m, prioridad " + PrioridadVolumen + ", sigue a la noche");
            }
        }

        private static Bounds? CajaDeZona(Transform instancia, string zona)
        {
            Bounds? caja = null;
            foreach (CalleZona cz in instancia.GetComponentsInChildren<CalleZona>(true))
            {
                foreach (Vector3 p in cz.Puntos())
                {
                    if (caja == null)
                    {
                        caja = new Bounds(p, Vector3.zero);
                    }
                    else
                    {
                        Bounds b = caja.Value;
                        b.Encapsulate(p);
                        caja = b;
                    }
                }
            }
            if (caja == null)
            {
                foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.bounds.size.magnitude > 300f)
                    {
                        continue;
                    }
                    if (caja == null)
                    {
                        caja = r.bounds;
                    }
                    else
                    {
                        Bounds b = caja.Value;
                        b.Encapsulate(r.bounds);
                        caja = b;
                    }
                }
            }
            if (caja != null && zona == "T1354_Malecon")
            {
                Bounds b = caja.Value;
                b.Encapsulate(ColocarT1354Curva.CajaVolumen());
                caja = b;
            }
            return caja;
        }

        private static VolumeProfile PerfilZonas()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilZonas);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, RutaPerfilZonas);
            }
            foreach (Object viejo in AssetDatabase.LoadAllAssetsAtPath(RutaPerfilZonas))
            {
                if (viejo is VolumeComponent)
                {
                    Object.DestroyImmediate(viejo, true);
                }
            }
            perfil.components.Clear();
            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.postExposure.Override(ExposicionZona);
            ca.contrast.Override(ContrasteZona);
            ca.saturation.Override(SaturacionZona);
            Bloom bloom = perfil.Add<Bloom>(true);
            bloom.threshold.Override(UmbralBloomZona);
            bloom.intensity.Override(IntensidadBloomZona);
            bloom.scatter.Override(DifusionBloomZona);
            foreach (VolumeComponent c in new VolumeComponent[] { ca, bloom })
            {
                AssetDatabase.AddObjectToAsset(c, perfil);
            }
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            Informe.AppendLine("Perfil " + RutaPerfilZonas + ": exposición +" + ExposicionZona + ", contraste " + ContrasteZona + ", saturación " + SaturacionZona + ", bloom umbral " + UmbralBloomZona + " intensidad " + IntensidadBloomZona
                + " (sin color de filtro ni balance: el sodio del Malecón conserva el suyo). El PerfilNoche global no se tocó.");
            return perfil;
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T1392/Fotos del look ronda 2")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            const string salida = "docs/capturas/t-1392/";
            Directory.CreateDirectory(salida);
            float lejosAntes = ColocarS2cLook.LejosFoto;
            ColocarS2cLook.LejosFoto = LejosFotos;
            Shader.SetGlobalFloat("_T501Noche", 1f); // en el editor VolumenSigueNoche no corre: sin esto el skyline sale de "día"
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                ("malecon-k11", "K11_Malecon.png", new Vector3(40f, 1.9f, -235.6f), new Vector3(-59f, 1.9f, -221.7f), 60f),
                ("malecon-k12", "K12_Malecon_Muelle.png", new Vector3(70f, 2.2f, -236f), new Vector3(149f, 2.2f, -250f), 75f),
                ("cerro-k13", "K13_ColoniaCerro.png", ColocarT1355Cerro.K13Desde, ColocarT1355Cerro.K13Hacia, 66f),
                ("puerto-k15", "K15_Puerto.png", new Vector3(157f, 1.7f, -98f), new Vector3(172f, 22f, -255f), 55f),
                ("autopista-k18", "K18_AutopistaElevada.png", new Vector3(-228f, 2.2f, 226f), new Vector3(-330f, 11f, 248f), 72f),
            };
            try
            {
                foreach (var t in tomas)
                {
                    string juego = salida + t.nombre + ".png";
                    ColocarS2cLook.Foto(DejarLibre(t.desde), t.hacia, juego, t.campo);
                    if (File.Exists(referencias + t.referencia))
                    {
                        ColocarS2.LadoALado(referencias + t.referencia, juego, salida + t.nombre + "-lado.png");
                    }
                    else
                    {
                        Debug.LogWarning("[Arte] Falta la referencia " + referencias + t.referencia + " (solo hay referencias en la carpeta principal)");
                    }
                }
            }
            finally
            {
                ColocarS2cLook.LejosFoto = lejosAntes;
            }
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        private static Vector3 DejarLibre(Vector3 desde)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < 6 && Physics.CheckSphere(desde, 1.5f, ~0, QueryTriggerInteraction.Ignore); i++)
            {
                desde += Vector3.up * 10f;
            }
            return desde;
        }
    }
}
