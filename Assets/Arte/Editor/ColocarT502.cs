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
    /// T-502 (paso A2 del plan de ciudad): viste el callejón de la emboscada de Ciudad_Prologo (corre en X de -87 a -59, muros en z -54.1 y
    /// -63.9, ruta por z -59) como callejon-noche: tuberías N04, ventilas N05 con vapor, contenedor, llantas, tambos, escalera, neón magenta,
    /// pocos puntos de luz, niebla rosa y un posproceso local oscuro y magenta. Todo cuelga de la raíz <see cref="NombreRaiz"/>: al correrlo
    /// otra vez se borra y se rehace (idempotente). Deja libre el corredor de la ruta (<see cref="Corredor"/>) y no toca Barrio.prefab.
    /// </summary>
    public static class ColocarT502
    {
        public const string NombreRaiz = "T502_Callejon";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Callejon";
        private const string CarpetaMateriales = Carpeta + "/Materiales";
        private const string CarpetaSuburbios = "Assets/Arte/Suburbios/Texturas";
        private const string RutaPerfil = Carpeta + "/PerfilCallejon.asset";

        public const float XMin = -87f;
        public const float XMax = -59f;
        public const float ZRuta = -59f;
        public const float MitadCorredor = 2f;
        private const float AltoCorredor = 2.6f;

        private static readonly StringBuilder Informe = new StringBuilder();
        private static Transform raiz;
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly Color Magenta = new Color(1f, 0.15f, 0.8f);
        private static readonly Color Turquesa = new Color(0.1f, 0.95f, 0.85f);
        private static readonly Color Calido = new Color(1f, 0.6f, 0.25f);
        private static readonly Color Rosa = new Color(0.9f, 0.3f, 0.65f);

        /// <summary>Franja por donde pasa el jugador (y la pandilla de la emboscada): nada de la raíz T502 entra aquí.</summary>
        public static Bounds Corredor
        {
            get { return new Bounds(new Vector3((XMin + XMax) * 0.5f, AltoCorredor * 0.5f, ZRuta), new Vector3(XMax - XMin, AltoCorredor, MitadCorredor * 2f)); }
        }

        private static readonly float[] Desplazos = Enumerable.Range(0, 24).SelectMany(i => new[] { i * 0.7f, -i * 0.7f }).Distinct().ToArray();

        private struct Pieza
        {
            public string Codigo;
            public float X;
            public int Lado;       // +1 muro norte (z -54.1), -1 muro sur (z -63.9)
            public float Escala;
            public float Altura;   // 0 = en el piso; > 0 = colgado del muro a esa altura (sin collider)
            public string Etiqueta;
        }

        private static readonly Pieza[] Piezas =
        {
            new Pieza { Codigo = "L01_Contenedor", X = -67.5f, Lado = -1, Escala = 1f },
            new Pieza { Codigo = "L03_EscaleraEmergencia", X = -82.5f, Lado = 1, Escala = 0.5f },
            new Pieza { Codigo = "L04_Llantas", X = -62.5f, Lado = 1, Escala = 1f },
            new Pieza { Codigo = "L04_Llantas", X = -84f, Lado = -1, Escala = 0.9f },
            new Pieza { Codigo = "C16_Tambo", X = -66.5f, Lado = 1, Escala = 1f },
            new Pieza { Codigo = "C16_Tambo", X = -71.5f, Lado = -1, Escala = 1f },
            new Pieza { Codigo = "C16_Tambo", X = -61.5f, Lado = -1, Escala = 1f },
            new Pieza { Codigo = "C16_Tambo", X = -79.5f, Lado = 1, Escala = 1f },
            new Pieza { Codigo = "C17_Huacales", X = -60.8f, Lado = 1, Escala = 1f },
            new Pieza { Codigo = "N06", X = -72.2f, Lado = 1, Escala = 0.7f },
            new Pieza { Codigo = "N06", X = -64.5f, Lado = -1, Escala = 0.7f },
            new Pieza { Codigo = "N04", X = -62.5f, Lado = 1, Escala = 0.5f, Altura = 0.9f },
            new Pieza { Codigo = "N04", X = -79.8f, Lado = 1, Escala = 0.5f, Altura = 0.9f },
            new Pieza { Codigo = "N04", X = -80.5f, Lado = -1, Escala = 0.5f, Altura = 0.9f },
            new Pieza { Codigo = "N04", X = -69.5f, Lado = -1, Escala = 0.5f, Altura = 1.1f },
            new Pieza { Codigo = "N05", X = -63.5f, Lado = 1, Escala = 1f, Altura = 3.3f },
            new Pieza { Codigo = "N05", X = -72.5f, Lado = 1, Escala = 1f, Altura = 3.0f },
            new Pieza { Codigo = "N05", X = -72f, Lado = -1, Escala = 1f, Altura = 3.3f },
            new Pieza { Codigo = "N05", X = -83f, Lado = -1, Escala = 1f, Altura = 3.0f },
            new Pieza { Codigo = "N05", X = -62f, Lado = -1, Escala = 1f, Altura = 3.2f },
        };

        public static IReadOnlyList<string> Codigos
        {
            get { return Piezas.Select(p => p.Codigo).Distinct().ToList(); }
        }

        [MenuItem("Mecha Cyberpunk/T-502/Colocar Callejón")]
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
            ColocarNoche.obstaculos.Add(Corredor);
            raiz = new GameObject(NombreRaiz).transform;

            CrearPiezas();
            CrearLetrero();
            CrearLamparas();
            CrearLuces();
            CrearCharcos();
            CrearNiebla();
            CrearVolumen();

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t502-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-502 colocado:\n" + Informe);
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

        /// <summary>Punto del muro del lado indicado a esa x (rayo desde la ruta); si no hay, el valor de Barrio (-54.1 / -63.9).</summary>
        private static float ZMuro(float x, int lado)
        {
            Vector3 desde = new Vector3(x, 1.5f, ZRuta);
            if (Physics.Raycast(desde, new Vector3(0f, 0f, lado), out RaycastHit golpe, 8f, ~0, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(golpe.normal.z) > 0.85f)
            {
                return golpe.point.z;
            }
            return lado > 0 ? -54.1f : -63.9f;
        }

        private static GameObject Instanciar(string codigo, Transform padre, Vector3 pos, float giro, float escala, bool sinCollider)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = codigo + "_T502";
            go.transform.localScale = Vector3.one * escala;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
            if (sinCollider)
            {
                Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            }
            return go;
        }

        // ------------------------------------------------------------------ piezas

        private static void CrearPiezas()
        {
            Transform g = Grupo("Piezas");
            Transform vapor = Grupo("Vapor");
            Material matVapor = MatVapor();
            int puestas = 0;
            int ventilas = 0;
            foreach (Pieza p in Piezas)
            {
                Vector3 tam = Tam(p.Codigo) * p.Escala;
                Vector3 normal = new Vector3(0f, 0f, -p.Lado);
                float giro = ColocarNoche.GiroHacia(normal);
                bool colgada = p.Altura > 0f;
                bool relajada = p.Codigo == "L03_EscaleraEmergencia";
                bool puesta = false;
                foreach (float desplazo in Desplazos)
                {
                    float x = Mathf.Clamp(p.X + desplazo, XMin + 1f, XMax - 1f);
                    float zMuro = ZMuro(x, p.Lado);
                    float z = zMuro + normal.z * (tam.z * 0.5f + (colgada ? 0.08f : 0.4f));
                    float suelo = colgada ? p.Altura : ColocarNoche.Suelo(new Vector3(x, 0f, z));
                    if (float.IsNaN(suelo))
                    {
                        continue;
                    }
                    Vector3 pivote = new Vector3(x, suelo, z);
                    Bounds caja = ColocarNoche.CajaGirada(tam / p.Escala, pivote, giro, Vector3.one * p.Escala);
                    if (!colgada && !relajada && !ColocarNoche.Libre(caja))
                    {
                        continue;
                    }
                    if ((colgada || relajada) && caja.Intersects(Corredor))
                    {
                        continue;
                    }
                    GameObject go = Instanciar(p.Codigo, g, pivote, giro, p.Escala, colgada || p.Codigo == "N06");
                    if (!colgada)
                    {
                        ColocarNoche.obstaculos.Add(caja);
                    }
                    if (p.Codigo == "N05")
                    {
                        CrearVapor(vapor, matVapor, go.transform.position + Vector3.up * (tam.y * 0.5f) + normal * (tam.z * 0.5f + 0.05f), ventilas);
                        ventilas++;
                    }
                    puestas++;
                    puesta = true;
                    break;
                }
                if (!puesta)
                {
                    Informe.AppendLine("No cupo: " + p.Codigo + " en x " + p.X + " lado " + p.Lado + " bloqueo: " + typeof(ColocarNoche).GetField("bloqueo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null));
                }
            }
            Informe.AppendLine("Piezas (N04, N05, contenedor, llantas, tambos, escalera, huacales, basura): " + puestas + " de " + Piezas.Length + ", ventilas con vapor: " + ventilas);
        }

        private static void CrearVapor(Transform padre, Material mat, Vector3 pos, int indice)
        {
            GameObject go = new GameObject("Vapor_" + indice);
            go.transform.SetParent(padre, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 5f;
            main.startLifetime = 3.2f;
            main.startSpeed = 0.7f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startColor = new Color(0.9f, 0.5f, 0.8f, 0.5f);
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 5f;
            ParticleSystem.ShapeModule forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 12f;
            forma.radius = 0.12f;
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
        }

        // ------------------------------------------------------------------ materiales

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

        /// <summary>Disco suave (alfa que cae sin borde duro) para el vapor.</summary>
        private static Texture2D TexturaVapor()
        {
            const string ruta = Carpeta + "/Texturas/Vapor_Suave.png";
            Directory.CreateDirectory(Carpeta + "/Texturas");
            const int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt(Mathf.Pow((x - n / 2f) / (n / 2f), 2f) + Mathf.Pow((y - n / 2f) / (n / 2f), 2f));
                    float a = Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static Material MatVapor()
        {
            return Material("Vapor_Callejon", "Universal Render Pipeline/Particles/Unlit", m =>
            {
                m.SetTexture("_BaseMap", TexturaVapor());
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            });
        }

        // ------------------------------------------------------------------ neón, luces, charcos, niebla

        private static void CrearLetrero()
        {
            Transform g = Grupo("Letrero");
            Material mat = Material("Letrero_Magenta_Callejon", "Universal Render Pipeline/Lit", m =>
            {
                m.SetTexture("_BaseMap", Tex("Letrero_Magenta"));
                m.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
                m.SetTexture("_EmissionMap", Tex("Letrero_Magenta"));
                m.SetColor("_EmissionColor", Magenta * 3.2f);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetFloat("_Cull", 0f);
            });
            float zMuro = ZMuro(-78.5f, -1);
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Neon_Magenta";
            quad.transform.SetParent(g, false);
            quad.transform.position = new Vector3(-78.5f, 3.5f, zMuro + 0.8f);
            quad.transform.rotation = Quaternion.LookRotation(Vector3.back);
            quad.transform.localScale = new Vector3(2.4f, 1.2f, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Informe.AppendLine("Letrero de neón magenta: 1");
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

        private static void CrearLamparas()
        {
            Transform g = Grupo("Lamparas");
            Vector3 tam = Tam("N01");
            int n = 0;
            foreach (var (x, lado) in new[] { (-68.6f, 1), (-71.6f, -1) })
            {
                float z = ZMuro(x, lado) + (-lado) * (tam.z * 0.5f + 0.05f);
                GameObject go = Instanciar("N01", g, new Vector3(x, 2.1f, z), ColocarNoche.GiroHacia(new Vector3(0f, 0f, -lado)), 1f, true);
                go.name = "N01_T502";
                Luz(go.transform, "Luz", new Vector3(x, 2.4f, z - lado * 0.5f), Calido, 6f, 6f);
                n++;
            }
            Informe.AppendLine("Lámparas N01 con luz cálida: " + n);
        }

        private static void CrearLuces()
        {
            Transform g = Grupo("Luces");
            Luz(g, "Magenta_Letrero", new Vector3(-78.5f, 3.2f, -61.8f), Magenta, 5f, 7f);
            Luz(g, "Magenta_Entrada", new Vector3(-63.5f, 3.2f, -55.6f), Magenta, 3f, 6f);
            Luz(g, "Turquesa_Escalera", new Vector3(-81f, 2.2f, -56.4f), Turquesa, 3.5f, 5f);
            Informe.AppendLine("Luces de color (sin sombra): 3");
        }

        private static void CrearCharcos()
        {
            Transform g = Grupo("Charcos");
            Color[] tintes = { Magenta, Turquesa, Magenta, Rosa, Turquesa, Magenta };
            float[] xs = { -62f, -66.5f, -71f, -75.5f, -80f, -84f };
            float[] zs = { -58.2f, -60.6f, -58.4f, -60.2f, -58.6f, -59.8f };
            int n = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                Color tinte = tintes[i];
                Material mat = Material("Charco_Callejon_" + i, "Universal Render Pipeline/Unlit", m =>
                {
                    m.SetTexture("_BaseMap", Tex("Charco_Redondo"));
                    m.SetColor("_BaseColor", new Color(tinte.r * 0.55f, tinte.g * 0.55f, tinte.b * 0.55f, 0.8f));
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
                float tamano = 1.3f + (i % 3) * 0.5f;
                quad.transform.localScale = new Vector3(tamano * 1.5f, tamano, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Charcos magenta y turquesa: " + n);
        }

        private static void CrearNiebla()
        {
            Transform g = Grupo("Niebla");
            Material mat = Material("Niebla_Rosa", "Universal Render Pipeline/Unlit", m =>
            {
                m.SetTexture("_BaseMap", Tex("Niebla_Degradado"));
                m.SetColor("_BaseColor", new Color(0.75f, 0.22f, 0.55f, 0.11f));
                Transparente(m, true);
            });
            int n = 0;
            foreach (float x in new[] { -63f, -68f, -73f, -78f, -83f })
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Niebla_" + n;
                quad.transform.SetParent(g, false);
                quad.transform.position = new Vector3(x, 2.6f, ZRuta);
                quad.transform.rotation = Quaternion.LookRotation(Vector3.right);
                quad.transform.localScale = new Vector3(9.4f, 5.2f, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Capas de niebla rosa: " + n);
        }

        private static void CrearVolumen()
        {
            AssetDatabase.DeleteAsset(RutaPerfil);
            VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.postExposure.Override(-0.6f);
            ca.saturation.Override(6f);
            ca.colorFilter.Override(new Color(1.15f, 0.8f, 1.1f));
            Vignette vi = perfil.Add<Vignette>(true);
            vi.intensity.Override(0.3f);
            AssetDatabase.CreateAsset(perfil, RutaPerfil);
            AssetDatabase.AddObjectToAsset(ca, perfil);
            AssetDatabase.AddObjectToAsset(vi, perfil);
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();

            GameObject go = new GameObject("VolumenCallejon");
            go.transform.SetParent(raiz, false);
            go.transform.position = new Vector3(-73f, 3f, ZRuta);
            BoxCollider caja = go.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(30f, 10f, 11f);
            Volume v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = 3f;
            v.priority = 6f;
            v.sharedProfile = perfil;
            Type tipo = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Ambiente.VolumenSigueNoche")).FirstOrDefault(t => t != null);
            if (tipo != null)
            {
                Component c = go.AddComponent(tipo);
                SerializedObject so = new SerializedObject(c);
                GameObject noche = GameObject.Find("PosprocesoNoche");
                so.FindProperty("origen").objectReferenceValue = noche != null ? noche.GetComponent<Volume>() : null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Informe.AppendLine("Volumen local oscuro y magenta (solo de noche): " + (tipo != null));
        }
    }
}
