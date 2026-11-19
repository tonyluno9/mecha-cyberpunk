using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1383: el look de noche de las zonas nuevas (Malecón, Cerro, Autopista y Puerto) contra las referencias K11 a K18.
    /// Aplicar(): arregla el asfalto de escarcha (S2c_Asfalto: albedo HDR 2.4 y normal map importado como textura de color), pone el piso mojado
    /// y los charcos, las sondas de reflejo, los faroles de sodio, una luz chica por neón magenta o turquesa, la niebla azul-morada y el posproceso.
    /// Trabaja sobre los prefabs de zona (LoadPrefabContents / SaveAsPrefabAsset) y solo lo global en la escena. Idempotente.
    /// Batchmode (en la carpeta principal): -executeMethod MechaCyberpunk.Arte.Editor.LookNocheT1383.Aplicar, y después .Fotos.
    /// Informe en Logs/t1383-look.txt.
    /// </summary>
    public static class LookNocheT1383
    {
        private const string CarpetaZonas = "Assets/Arte/Mundo/Zonas/";
        private const string CarpetaLook = "Assets/Arte/Mundo/Look";
        private const string RutaCharco = CarpetaLook + "/Charco.prefab";
        private const string RutaMaterialCharco = CarpetaLook + "/Charco_T1383.mat";
        private const string RutaAsfaltoT1382 = "Assets/Arte/Materiales/Calle18nov/M_T07_AsfaltoMojado.mat";
        private const string RutaAsfaltoPropio = CarpetaLook + "/ZM_AsfaltoMojado_T1383.mat";
        private const string RutaBanqueta = CarpetaLook + "/ZM_BanquetaMojada_T1383.mat";
        private const string RutaNormalAsfaltoOrigen = "Assets/Arte/Exteriores/Texturas/T535_Asfalto_Normal.png";
        private const string RutaNormalAsfalto = CarpetaLook + "/Texturas/T1383_AsfaltoNormal.png";
        private const string RutaBanquetaAlbedo = "Assets/Arte/Exteriores/Texturas/T506_Losa.png";
        private const string RutaInforme = "Logs/t1383-look.txt";

        private static readonly string[] Zonas = { "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto" };
        private static readonly string[] PrefijosZona = { "T13", "ZM_", "Cerro_", "Malecon_", "Puerto_", "Autopista_", "M_T07" };

        // Materiales compartidos que se cambian solo dentro de los prefabs de zona (los .mat de los barrios viejos no se tocan).
        private const string AsfaltoViejo = "S2c_Asfalto";
        private static readonly string[] BanquetasViejas = { "S2c_Banqueta", "T1354_Banqueta" };

        // Piso mojado
        private static readonly Color ColorAsfaltoPropio = new Color(0.08f, 0.08f, 0.1f);
        private const float BrilloAsfaltoPropio = 0.85f;
        private static readonly Color ColorBanqueta = new Color(0.3f, 0.3f, 0.33f);
        private const float BrilloBanqueta = 0.6f;
        private static readonly Color ColorCharco = new Color(0.015f, 0.025f, 0.05f);
        private const float BrilloCharco = 0.97f;
        private const float AltoCharco = 0.025f;
        private const float CharcoMin = 1.5f;
        private const float CharcoMax = 4f;
        private const int CharcosMin = 20;
        private const int CharcosMax = 40;
        private const float MetrosPorCharco = 25f;
        private const float MargenCharcoBanqueta = 1.2f;
        private const float DistanciaCharcoViejo = 3f;
        private const int SemillaCharcos = 1383;

        // Sondas de reflejo
        private const float MetrosPorSonda = 40f;
        private const int SondasMaximo = 14;
        private const float AltoSonda = 6f;
        private static readonly Vector3 TamanoSonda = new Vector3(50f, 24f, 50f);
        private const int ResolucionSonda = 128;

        // Faroles de sodio (K11/K13: ~2000 K, charco de luz de ~10 m)
        private static readonly Color Sodio = new Color(1f, 0.55f, 0.2f);
        private const float IntensidadSodio = 170f;
        private const float AlcanceSodio = 16f;
        private const float AnguloSodio = 100f;
        private const float AnguloInteriorSodio = 40f;

        // Neones
        private const float IntensidadNeon = 3f;
        private const float AlcanceNeon = 5f;
        private const int NeonesPorZona = 60;
        private const float CeldaNeon = 4f;
        private const float DistanciaLuzVieja = 3f;
        private const float TamanoMaximoNeon = 15f;
        private const float DistanciaMaximaCalle = 30f;
        private const float EmpujeNeon = 0.8f;
        private const int TopeLucesEscena = 1400;

        // Niebla y volumen (global)
        private static readonly Color ColorNieblaNoche = new Color(0.102f, 0.086f, 0.188f); // #1a1630
        private const float DensidadNiebla = 0.012f; // exponencial: ~250 m al 5 %
        private const float UmbralBloom = 1f;
        private const float IntensidadBloom = 1.2f;
        private const float ContrasteNoche = 16f;
        private const float TemperaturaNoche = 14f;
        private const float VinetaNoche = 0.24f;

        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================ Aplicar

        public static void Aplicar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1383: look de noche de las zonas");
            ColocarS2.Abrir();
            int lucesEscenaAntes = ContarLucesEscena();
            Informe.AppendLine("Luces de la escena antes: " + lucesEscenaAntes + " (tope seguro " + TopeLucesEscena + ")");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Directory.CreateDirectory(CarpetaLook + "/Texturas");
            ExplicarEscarcha();
            Material asfalto = MaterialAsfalto();
            Material banqueta = MaterialBanqueta();
            GameObject charco = PrefabCharco();
            int presupuesto = Mathf.Max(0, TopeLucesEscena - lucesEscenaAntes);
            int neonesPorZona = Mathf.Min(NeonesPorZona, presupuesto / Zonas.Length);
            Informe.AppendLine("Luces nuevas permitidas por zona: " + neonesPorZona + " de neón (más cero de farolas: solo se ajustan)");

            int agregadas = 0;
            for (int i = 0; i < Zonas.Length; i++)
            {
                agregadas += Zona(Zonas[i], i, asfalto, banqueta, charco, neonesPorZona);
            }
            AssetDatabase.SaveAssets();

            ColocarS2.Abrir();
            GlobalEscena();
            int lucesEscenaDespues = ContarLucesEscena();
            Informe.AppendLine("Luces de la escena después: " + lucesEscenaDespues + " (nuevas " + (lucesEscenaDespues - lucesEscenaAntes) + ", sumadas por zona " + agregadas + ")");
            if (lucesEscenaDespues > TopeLucesEscena)
            {
                Informe.AppendLine("AVISO: pasa del tope de " + TopeLucesEscena + " luces; correr con menos neones (bajar NeonesPorZona).");
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, Informe.ToString());
            Debug.Log("[Arte] " + Informe);
        }

        private static int ContarLucesEscena()
        {
            return Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        // ================================================================ Causa de la escarcha (evidencia)

        private static void ExplicarEscarcha()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Causa de la escarcha");
            Material viejo = AssetDatabase.LoadAssetAtPath<Material>(KitS2c.Carpeta + "/" + AsfaltoViejo + ".mat");
            if (viejo == null)
            {
                Informe.AppendLine("No existe " + AsfaltoViejo + ".mat");
                return;
            }
            Color c = viejo.GetColor("_BaseColor");
            Texture normal = viejo.GetTexture("_BumpMap");
            string tipo = "sin normal map";
            if (normal != null)
            {
                TextureImporter ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal)) as TextureImporter;
                tipo = AssetDatabase.GetAssetPath(normal) + " importada como " + (ti != null ? ti.textureType + ", sRGB=" + ti.sRGBTexture : "?");
            }
            Informe.AppendLine(AsfaltoViejo + ".mat (el arroyo de las calles de Cerro, Malecón, Autopista y Puerto): _BaseColor = " + c
                + ", _Smoothness = " + viejo.GetFloat("_Smoothness") + ", _BumpMap = " + tipo);
            Informe.AppendLine("1) _BaseColor con canales de 2.4 (T-880 lo subió para los barrios con neones): el albedo pasa de 1, así que las luces de sodio lo queman a blanco.");
            Informe.AppendLine("2) _BumpMap = T535_Asfalto_Normal.png está importada como Default con sRGB y no como NormalMap: el plano (0.5, 0.5, 1) pasa a (0.21, 0.21, 1) y se lee como una normal torcida en toda la calle; el grano cambia por pixel y brilla con la luz: la escarcha.");
            Informe.AppendLine("Arreglo: materiales propios de zona (los .mat de S2c_ y los barrios viejos no se tocan), con la normal copiada como NormalMap y albedo menor que 1.");
        }

        // ================================================================ Materiales

        private static bool EsDeZona(string nombre)
        {
            return PrefijosZona.Any(p => nombre.StartsWith(p));
        }

        private static Material Lit(string ruta, Color color, float brillo)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", brillo);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D NormalDeZonas()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(RutaNormalAsfalto) == null)
            {
                if (!AssetDatabase.CopyAsset(RutaNormalAsfaltoOrigen, RutaNormalAsfalto))
                {
                    Informe.AppendLine("No se pudo copiar " + RutaNormalAsfaltoOrigen + " (¿LFS sin bajar?)");
                    return null;
                }
            }
            TextureImporter ti = AssetImporter.GetAtPath(RutaNormalAsfalto) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(RutaNormalAsfalto);
        }

        /// <summary>El asfalto mojado de T-1382 si existe; si no, uno propio oscuro con la normal bien importada.</summary>
        private static Material MaterialAsfalto()
        {
            Material t1382 = AssetDatabase.LoadAssetAtPath<Material>(RutaAsfaltoT1382);
            if (t1382 != null)
            {
                Informe.AppendLine("Asfalto: " + RutaAsfaltoT1382 + " (T-1382)");
                return t1382;
            }
            Material m = Lit(RutaAsfaltoPropio, ColorAsfaltoPropio, BrilloAsfaltoPropio);
            Texture2D normal = NormalDeZonas();
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 0.6f);
            if (normal != null)
            {
                m.EnableKeyword("_NORMALMAP");
            }
            Informe.AppendLine("Asfalto: " + RutaAsfaltoPropio + " (no está el de T-1382; propio, brillo " + BrilloAsfaltoPropio + ")");
            return m;
        }

        private static Material MaterialBanqueta()
        {
            Material m = Lit(RutaBanqueta, ColorBanqueta, BrilloBanqueta);
            // La banqueta vieja usaba T506_Losa.png (gris, no es una normal) como _BumpMap: aquí no lleva normal.
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arte/Calle/Banqueta/T01_Banqueta.png");
            m.SetTexture("_BaseMap", albedo);
            m.SetTextureScale("_BaseMap", Vector2.one);
            Informe.AppendLine("Banqueta: " + RutaBanqueta + " (sin normal; la de S2c_Banqueta era un gris T506_Losa leído como normal)");
            return m;
        }

        /// <summary>
        /// Revisa los materiales de zona que usan los renderers: albedo mayor que 1 (se baja a 1) y _BumpMap que no está importado como NormalMap (se quita).
        /// Lo que arregla y lo que solo avisa va al informe.
        /// </summary>
        private static void RevisarMateriales(string zona, GameObject raiz)
        {
            HashSet<Material> vistos = new HashSet<Material>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null || !vistos.Add(m) || !EsDeZona(m.name))
                    {
                        continue;
                    }
                    bool cambio = false;
                    if (m.HasProperty("_BaseColor"))
                    {
                        Color c = m.GetColor("_BaseColor");
                        if (Mathf.Max(c.r, c.g, c.b) > 1.001f)
                        {
                            Color limitado = new Color(Mathf.Min(c.r, 1f), Mathf.Min(c.g, 1f), Mathf.Min(c.b, 1f), c.a);
                            m.SetColor("_BaseColor", limitado);
                            Informe.AppendLine("  [" + zona + "] " + m.name + ": _BaseColor " + c + " -> " + limitado);
                            cambio = true;
                        }
                    }
                    if (m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null)
                    {
                        string ruta = AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap"));
                        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
                        if (ti != null && ti.textureType != TextureImporterType.NormalMap)
                        {
                            m.SetTexture("_BumpMap", null);
                            m.DisableKeyword("_NORMALMAP");
                            Informe.AppendLine("  [" + zona + "] " + m.name + ": _BumpMap " + ruta + " no es NormalMap: se quita");
                            cambio = true;
                        }
                    }
                    if (m.HasProperty("_BaseMap"))
                    {
                        Vector2 e = m.GetTextureScale("_BaseMap");
                        if (Mathf.Max(e.x, e.y) >= 20f)
                        {
                            Informe.AppendLine("  [" + zona + "] AVISO " + m.name + ": tiling " + e + " (muy grande)");
                        }
                    }
                    if (cambio)
                    {
                        EditorUtility.SetDirty(m);
                    }
                }
            }
        }

        private static int CambiarPiso(GameObject raiz, Material asfalto, Material banqueta)
        {
            int cambios = 0;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                bool toco = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null)
                    {
                        continue;
                    }
                    if (ms[i].name == AsfaltoViejo)
                    {
                        ms[i] = asfalto;
                        toco = true;
                    }
                    else if (BanquetasViejas.Contains(ms[i].name))
                    {
                        ms[i] = banqueta;
                        toco = true;
                    }
                }
                if (toco)
                {
                    r.sharedMaterials = ms;
                    cambios++;
                }
            }
            return cambios;
        }

        // ================================================================ Charco (prefab)

        private static GameObject PrefabCharco()
        {
            Material m = Lit(RutaMaterialCharco, ColorCharco, BrilloCharco);
            GameObject raiz = new GameObject("Charco");
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Agua";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(raiz.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // el quad mira a -Z: así su cara mira hacia arriba
            MeshRenderer mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaCharco);
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("Charco: " + RutaCharco + " (quad sin collider ni sombras, brillo " + BrilloCharco + ", metálico 0)");
            return prefab;
        }

        // ================================================================ Una zona

        private static int Zona(string nombre, int indice, Material asfalto, Material banqueta, GameObject charco, int neonesMax)
        {
            string ruta = CarpetaZonas + nombre + ".prefab";
            Informe.AppendLine();
            Informe.AppendLine("== " + nombre);
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                int antes = raiz.GetComponentsInChildren<Light>(true).Length;
                BorrarGrupo(raiz.transform, "T1383_LucesNeon");
                BorrarGrupo(raiz.transform, "T1383_Charcos");
                BorrarGrupo(raiz.transform, "T1383_Sondas");

                int piso = CambiarPiso(raiz, asfalto, banqueta);
                RevisarMateriales(nombre, raiz);
                Informe.AppendLine("Renderers con piso mojado: " + piso);

                List<Vector3> calle = PuntosDeCalle(raiz);
                float largo = Muestrear(raiz, 1f, new System.Random(1), 0f).Count; // una muestra por metro de calle
                Informe.AppendLine("Calles de zona (CalleZona): " + raiz.GetComponentsInChildren<CalleZona>(true).Length + ", puntos " + calle.Count + ", " + largo.ToString("F0") + " m");

                int sodio = AjustarSodio(raiz);
                int neones = PonerNeones(raiz, calle, neonesMax);
                int charcos = PonerCharcos(raiz, indice, charco);
                int sondas = PonerSondas(raiz);

                int despues = raiz.GetComponentsInChildren<Light>(true).Length;
                Informe.AppendLine("Faroles de sodio ajustados: " + sodio + "; luces de neón nuevas: " + neones + "; charcos nuevos: " + charcos + "; sondas: " + sondas);
                Informe.AppendLine("Luces (Light) en el prefab: antes " + antes + ", después " + despues);
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                return despues - antes;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void BorrarGrupo(Transform raiz, string nombre)
        {
            Transform g = raiz.Find(nombre);
            if (g != null)
            {
                Object.DestroyImmediate(g.gameObject);
            }
        }

        private static Transform Grupo(Transform raiz, string nombre)
        {
            Transform g = new GameObject(nombre).transform;
            g.SetParent(raiz, false);
            return g;
        }

        // ---------------------------------------------------------------- Calles

        /// <summary>Puntos de las calles de zona (las que usan el tráfico y la policía), en el espacio de la raíz del prefab.</summary>
        private static List<Vector3> PuntosDeCalle(GameObject raiz)
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (CalleZona cz in raiz.GetComponentsInChildren<CalleZona>(true))
            {
                puntos.AddRange(cz.Puntos().Select(p => raiz.transform.InverseTransformPoint(p)));
            }
            return puntos;
        }

        private static float DistanciaACalle(List<Vector3> pts, Vector3 p)
        {
            float mejor = float.MaxValue;
            foreach (Vector3 q in pts)
            {
                float dx = q.x - p.x, dz = q.z - p.z;
                mejor = Mathf.Min(mejor, Mathf.Sqrt(dx * dx + dz * dz));
            }
            return mejor;
        }

        // ---------------------------------------------------------------- Faroles de sodio

        private static bool EsSodio(Light l)
        {
            Color c = l.color;
            bool naranja = c.r >= 0.85f && c.g >= 0.35f && c.g <= 0.8f && c.b <= 0.5f;
            string padre = l.transform.parent != null ? l.transform.parent.name : "";
            bool deFarol = padre.StartsWith("Poste") || padre.Contains("Farol");
            return deFarol && (l.type == LightType.Spot || l.type == LightType.Point) && (naranja || l.range >= 10f);
        }

        private static int AjustarSodio(GameObject raiz)
        {
            int n = 0;
            foreach (Light l in raiz.GetComponentsInChildren<Light>(true))
            {
                if (!EsSodio(l))
                {
                    continue;
                }
                l.color = Sodio;
                l.range = AlcanceSodio;
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

        // ---------------------------------------------------------------- Neones con luz

        private static bool EsNeon(Material m, out Color color)
        {
            color = Color.black;
            if (m == null || !m.HasProperty("_EmissionColor"))
            {
                return false;
            }
            bool emite = m.IsKeywordEnabled("_EMISSION") || m.name.Contains("Neon") || m.name.Contains("Pantalla");
            Color e = m.GetColor("_EmissionColor");
            float max = Mathf.Max(e.r, e.g, e.b);
            if (!emite || max < 0.8f)
            {
                return false;
            }
            Color.RGBToHSV(new Color(e.r / max, e.g / max, e.b / max), out float h, out float s, out float _);
            bool magenta = h >= 0.78f && h <= 0.97f;
            bool turquesa = h >= 0.44f && h <= 0.56f;
            if (s < 0.5f || !(magenta || turquesa))
            {
                return false;
            }
            color = Color.HSVToRGB(h, 1f, 1f);
            return true;
        }

        private static int PonerNeones(GameObject raiz, List<Vector3> calle, int maximo)
        {
            if (maximo <= 0)
            {
                return 0;
            }
            List<Vector3> existentes = raiz.GetComponentsInChildren<Light>(true).Select(l => l.transform.position).ToList();
            HashSet<Vector2Int> celdas = new HashSet<Vector2Int>();
            HashSet<Material> conBrillo = new HashSet<Material>();
            List<(Renderer r, Color color, float dist, Vector3 centro)> candidatos = new List<(Renderer, Color, float, Vector3)>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponentInParent<Light>() != null)
                {
                    continue;
                }
                foreach (Material m in r.sharedMaterials)
                {
                    if (!EsNeon(m, out Color color))
                    {
                        continue;
                    }
                    // RealtimeEmissive: con None se pierde _EMISSION al reimportar. Solo en materiales de zona (los de los barrios no se tocan).
                    if (EsDeZona(m.name) && conBrillo.Add(m))
                    {
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        EditorUtility.SetDirty(m);
                    }
                    Bounds b = r.bounds;
                    float dist = calle.Count > 0 ? DistanciaACalle(calle, b.center) : 0f;
                    if (b.size.magnitude <= TamanoMaximoNeon && dist <= DistanciaMaximaCalle)
                    {
                        candidatos.Add((r, color, dist, b.center));
                    }
                    break;
                }
            }
            Transform grupo = Grupo(raiz.transform, "T1383_LucesNeon");
            int puestas = 0;
            foreach (var c in candidatos.OrderBy(x => x.dist))
            {
                if (puestas >= maximo)
                {
                    break;
                }
                Vector2Int celda = new Vector2Int(Mathf.FloorToInt(c.centro.x / CeldaNeon), Mathf.FloorToInt(c.centro.z / CeldaNeon));
                if (celdas.Contains(celda) || existentes.Any(p => (p - c.centro).sqrMagnitude < DistanciaLuzVieja * DistanciaLuzVieja))
                {
                    continue;
                }
                celdas.Add(celda);
                Vector3 posicion = c.centro + HaciaCalle(calle, c.centro) * EmpujeNeon;
                Light l = KitS2c.Luz(grupo, posicion, LightType.Point, c.color, IntensidadNeon, AlcanceNeon, 0f);
                l.name = "LuzNeon";
                l.shadows = LightShadows.None;
                puestas++;
            }
            return puestas;
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

        // ---------------------------------------------------------------- Charcos y sondas

        private struct Muestra
        {
            public Vector3 Punto;
            public Vector3 Tangente;
            public float Ancho;
        }

        /// <summary>Puntos repartidos cada <paramref name="paso"/> m de arco sobre todas las calles de zona, con su tangente y ancho.</summary>
        private static List<Muestra> Muestrear(GameObject raiz, float paso, System.Random azar, float desfase)
        {
            List<Muestra> salida = new List<Muestra>();
            foreach (CalleZona cz in raiz.GetComponentsInChildren<CalleZona>(true))
            {
                List<Vector3> p = cz.Puntos().Select(q => raiz.transform.InverseTransformPoint(q)).ToList();
                float siguiente = desfase > 0f ? (float)azar.NextDouble() * paso : paso * 0.5f;
                float acumulado = 0f;
                for (int i = 0; i + 1 < p.Count; i++)
                {
                    float seg = Vector3.Distance(p[i], p[i + 1]);
                    while (siguiente <= acumulado + seg && seg > 0.001f)
                    {
                        float t = (siguiente - acumulado) / seg;
                        salida.Add(new Muestra { Punto = Vector3.Lerp(p[i], p[i + 1], t), Tangente = (p[i + 1] - p[i]).normalized, Ancho = cz.Ancho });
                        siguiente += paso * (desfase > 0f ? 0.6f + 0.8f * (float)azar.NextDouble() : 1f);
                    }
                    acumulado += seg;
                }
            }
            return salida;
        }

        private static int PonerCharcos(GameObject raiz, int indice, GameObject charco)
        {
            System.Random azar = new System.Random(SemillaCharcos + indice);
            List<Vector3> viejos = raiz.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("S2c_Charco"))
                .Select(r => r.bounds.center).ToList();
            List<Muestra> todas = Muestrear(raiz, 1f, azar, 0f);
            float largo = todas.Count; // con paso de 1 m, cada muestra es ~1 m de calle
            int objetivo = Mathf.Clamp(Mathf.RoundToInt(largo / MetrosPorCharco), CharcosMin, CharcosMax);
            if (todas.Count == 0)
            {
                Informe.AppendLine("Sin CalleZona: no hay dónde poner charcos.");
                return 0;
            }
            Transform grupo = Grupo(raiz.transform, "T1383_Charcos");
            int puestos = 0;
            for (int k = 0; k < objetivo * 3 && puestos < objetivo; k++)
            {
                // Un tramo distinto de la calle para cada charco (reparto parejo y con semilla).
                int baseMuestra = Mathf.Min(todas.Count - 1, Mathf.FloorToInt((puestos + (float)azar.NextDouble()) * todas.Count / objetivo));
                Muestra m = todas[baseMuestra];
                float tamano = Mathf.Lerp(CharcoMin, CharcoMax, (float)azar.NextDouble());
                float lateralMax = Mathf.Max(0f, m.Ancho * 0.5f - MargenCharcoBanqueta - tamano * 0.5f);
                Vector3 plano = new Vector3(m.Tangente.x, 0f, m.Tangente.z);
                if (plano.sqrMagnitude < 0.0001f)
                {
                    continue;
                }
                plano.Normalize();
                Vector3 lado = Vector3.Cross(Vector3.up, plano);
                Vector3 normal = Vector3.Cross(m.Tangente, lado).normalized;
                Vector3 posicion = m.Punto + lado * (((float)azar.NextDouble() * 2f - 1f) * lateralMax) + normal * AltoCharco;
                if (viejos.Any(v => (v - posicion).sqrMagnitude < DistanciaCharcoViejo * DistanciaCharcoViejo))
                {
                    continue;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(charco, grupo);
                go.transform.localPosition = posicion;
                go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, (float)azar.NextDouble() * 360f, 0f);
                go.transform.localScale = new Vector3(tamano, 1f, tamano * Mathf.Lerp(0.6f, 1f, (float)azar.NextDouble()));
                viejos.Add(posicion);
                puestos++;
            }
            return puestos;
        }

        private static int PonerSondas(GameObject raiz)
        {
            System.Random azar = new System.Random(SemillaCharcos);
            List<Muestra> todas = Muestrear(raiz, MetrosPorSonda, azar, 0f);
            Transform grupo = Grupo(raiz.transform, "T1383_Sondas");
            int n = 0;
            int salto = Mathf.Max(1, Mathf.CeilToInt(todas.Count / (float)SondasMaximo));
            for (int i = 0; i < todas.Count && n < SondasMaximo; i += salto)
            {
                GameObject go = new GameObject("SondaReflejo_T1383");
                go.transform.SetParent(grupo, false);
                go.transform.localPosition = todas[i].Punto + Vector3.up * AltoSonda;
                ReflectionProbe sonda = go.AddComponent<ReflectionProbe>();
                sonda.mode = ReflectionProbeMode.Realtime;
                sonda.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                sonda.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                sonda.resolution = ResolucionSonda;
                sonda.size = TamanoSonda;
                sonda.boxProjection = true;
                sonda.blendDistance = 8f;
                sonda.importance = 1;
                n++;
            }
            return n;
        }

        // ================================================================ Global: niebla y posproceso

        private static void GlobalEscena()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Global");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = ColorNieblaNoche;
            RenderSettings.fogDensity = DensidadNiebla;
            Informe.AppendLine("RenderSettings de la escena: niebla exponencial #1a1630, densidad " + DensidadNiebla + " (~250 m al 5 %).");
            NieblaDelCiclo();
            PosprocesoNoche();
            InformeUrp();
        }

        /// <summary>
        /// CicloDiaNoche pisa RenderSettings.fog en cada Aplicar con ConfigCicloDia: la niebla que se ve en el juego sale de ese asset (dato, no código).
        /// Se llevan al azul-morado #1a1630 las claves de noche del gradiente (antes del 28 % y después del 80 % del día).
        /// </summary>
        private static void NieblaDelCiclo()
        {
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            FieldInfo campo = typeof(ConfigCicloDia).GetField("colorNiebla", BindingFlags.NonPublic | BindingFlags.Instance);
            if (config == null || campo == null)
            {
                Informe.AppendLine("AVISO: no se pudo leer ConfigCicloDia_Mundo (niebla del ciclo sin cambiar).");
                return;
            }
            Gradient g = (Gradient)campo.GetValue(config);
            GradientColorKey[] claves = g.colorKeys;
            int cambiadas = 0;
            for (int i = 0; i < claves.Length; i++)
            {
                if (claves[i].time <= 0.28f || claves[i].time >= 0.8f)
                {
                    claves[i].color = ColorNieblaNoche;
                    cambiadas++;
                }
            }
            g.SetKeys(claves, g.alphaKeys);
            campo.SetValue(config, g);
            EditorUtility.SetDirty(config);
            Informe.AppendLine("ConfigCicloDia_Mundo: " + cambiadas + " claves de noche de colorNiebla a #1a1630. Densidad del ciclo (exp. al cuadrado) sin tocar.");
        }

        private static T Componente<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            if (!perfil.TryGet(out T c))
            {
                c = perfil.Add<T>(true);
                AssetDatabase.AddObjectToAsset(c, perfil);
            }
            c.active = true;
            return c;
        }

        /// <summary>Bloom, ACES, contraste, balance cálido y viñeta leve en el volumen global de noche (el que CicloDiaNoche pesa por la hora).</summary>
        private static void PosprocesoNoche()
        {
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ColocarS2cLook.RutaPerfilNoche);
            if (noche == null)
            {
                Informe.AppendLine("AVISO: no existe " + ColocarS2cLook.RutaPerfilNoche);
                return;
            }
            Componente<Tonemapping>(noche).mode.Override(TonemappingMode.ACES);
            Bloom bloom = Componente<Bloom>(noche);
            bloom.threshold.Override(UmbralBloom);
            bloom.intensity.Override(IntensidadBloom);
            bloom.tint.Override(Color.white);
            ColorAdjustments ca = Componente<ColorAdjustments>(noche);
            ca.contrast.Override(ContrasteNoche);
            WhiteBalance wb = Componente<WhiteBalance>(noche);
            wb.temperature.Override(TemperaturaNoche);
            Vignette vi = Componente<Vignette>(noche);
            vi.intensity.Override(VinetaNoche);
            EditorUtility.SetDirty(noche);
            Informe.AppendLine("PerfilNoche: ACES, bloom umbral " + UmbralBloom + " intensidad " + IntensidadBloom + ", contraste " + ContrasteNoche + ", temperatura " + TemperaturaNoche + ", viñeta " + VinetaNoche
                + ". El volumen local del Malecón (T-1354, prioridad 10, sodio) queda encima: no se tocó.");
        }

        private static void InformeUrp()
        {
            foreach (string ruta in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Medio_RPAsset.asset" })
            {
                Object urp = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                if (urp == null)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(urp);
                Informe.AppendLine(ruta + ": HDR " + Valor(so, "m_SupportsHDR") + ", luces adicionales (modo) " + Valor(so, "m_AdditionalLightsRenderingMode")
                    + " (límite por objeto " + Valor(so, "m_AdditionalLightsPerObjectLimit") + "), mezcla de sondas " + Valor(so, "m_ReflectionProbeBlending")
                    + ", proyección de caja " + Valor(so, "m_ReflectionProbeBoxProjection") + ", atlas de sondas " + Valor(so, "m_ReflectionProbeAtlas"));
            }
            Informe.AppendLine("SSR: URP 17 no trae reflejos en pantalla (SSR es de HDRP): solo sondas de caja + brillo 0.85 a 0.97 + el ReflejoCharcos que ya usa la ciudad.");
        }

        private static string Valor(SerializedObject so, string campo)
        {
            SerializedProperty p = so.FindProperty(campo);
            return p == null ? "?" : p.propertyType == SerializedPropertyType.Boolean ? p.boolValue.ToString() : p.intValue.ToString();
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T1383/Fotos del look de noche")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            const string salida = "docs/capturas/t-1383/";
            Directory.CreateDirectory(salida);
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                ("malecon-k11", "K11_Malecon.png", new Vector3(40f, 1.9f, -235.6f), new Vector3(-59f, 1.9f, -221.7f), 60f),
                ("malecon-k12", "K12_Malecon_Muelle.png", new Vector3(70f, 2.2f, -236f), new Vector3(149f, 2.2f, -250f), 75f),
                ("cerro-k13", "K13_ColoniaCerro.png", ColocarT1355Cerro.K13Desde, ColocarT1355Cerro.K13Hacia, 66f),
                ("puerto-k15", "K15_Puerto.png", new Vector3(157f, 1.7f, -98f), new Vector3(172f, 22f, -255f), 55f),
                ("autopista-k18", "K18_AutopistaElevada.png", new Vector3(-228f, 2.2f, 226f), new Vector3(-330f, 11f, 248f), 72f),
            };
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
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        /// <summary>Sube la cámara de a 10 m (hasta 6 veces) si quedó dentro de un collider.</summary>
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
