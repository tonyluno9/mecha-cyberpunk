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
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-501 (paso A1 del plan de ciudad): viste Suburbios de Ciudad_Prologo como las referencias (calle angosta y encerrada, cables en
    /// catenaria, lámparas N01 con luz cálida, letreros de neón, charcos, niebla azul, orillas con tambos/huacales/basura, tinacos en las
    /// azoteas y 48 torres de horizonte). Todo cuelga de la raíz <see cref="NombreRaiz"/> de la escena: al correrlo otra vez se borra y se
    /// rehace (idempotente). No toca Barrio.prefab ni los lugares de la historia: los obstáculos, puertas y personajes salen de ColocarNoche.
    /// </summary>
    public static class ColocarT501
    {
        public const string NombreRaiz = "T501_Suburbios";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Suburbios";
        private const string CarpetaTexturas = Carpeta + "/Texturas";
        private const string CarpetaMateriales = Carpeta + "/Materiales";
        private const string RutaPerfil = Carpeta + "/PerfilSuburbios.asset";
        private const string RutaShaderTorre = "MechaCyberpunk/TorreHorizonte";

        private const float XMin = -131f;
        private const float XMax = -57f;
        private const float XCruzada = -47f;
        private const float ZCruzadaMin = 12f;
        private const float ZCruzadaMax = 80f;
        public const int TorresMinimas = 30;
        public const int TorresMaximas = 60;
        private const int CantidadTorres = 48;
        private const int CablesPorCuadra = 8;
        private const float AnchoBanqueta = 2.56f;

        private static readonly StringBuilder Informe = new StringBuilder();
        private static Transform raiz;
        private static Random Azar = new Random(501);
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        private static readonly Color Magenta = new Color(1f, 0.15f, 0.8f);
        private static readonly Color Turquesa = new Color(0.1f, 0.95f, 0.85f);
        private static readonly Color Calido = new Color(1f, 0.6f, 0.25f);

        /// <summary>Un lado de calle: se camina a lo largo de la tangente y se lanza un rayo hacia el muro de las fachadas.</summary>
        private sealed class Lado
        {
            public string Nombre;
            public Vector3 Origen;      // punto en el centro de la calle donde empieza el recorrido
            public Vector3 Tangente;
            public Vector3 HaciaMuro;
            public float Largo;
        }

        private static List<Lado> Lados()
        {
            List<Lado> l = new List<Lado>
            {
                new Lado { Nombre = "PrincipalNorte", Origen = new Vector3(XMin, 0f, 0f), Tangente = Vector3.right, HaciaMuro = Vector3.forward, Largo = XMax - XMin },
                new Lado { Nombre = "PrincipalSur", Origen = new Vector3(XMin, 0f, 0f), Tangente = Vector3.right, HaciaMuro = Vector3.back, Largo = XMax - XMin },
                new Lado { Nombre = "CruzadaNorte", Origen = new Vector3(XCruzada, 0f, ZCruzadaMin), Tangente = Vector3.forward, HaciaMuro = Vector3.left, Largo = ZCruzadaMax - ZCruzadaMin },
                new Lado { Nombre = "CruzadaSur", Origen = new Vector3(XCruzada, 0f, -ZCruzadaMax), Tangente = Vector3.forward, HaciaMuro = Vector3.left, Largo = ZCruzadaMax - ZCruzadaMin },
            };
            return l;
        }

        [MenuItem("Mecha Cyberpunk/T-501/Colocar Suburbios")]
        public static void Colocar()
        {
            Informe.Clear();
            cables = 0;
            Azar = new Random(501);
            lamparas = 0;
            lucesLamparas = 0;
            Cache.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Physics.SyncTransforms();
            CrearRecursos();
            ColocarNoche.ReunirObstaculos();
            raiz = new GameObject(NombreRaiz).transform;

            List<Lado> lados = Lados();
            CrearCablesCruzados(lados);
            CrearCablesDePostes();
            CrearLamparas(lados);
            CrearLetreros(lados);
            CrearToldos(lados);
            CrearOrillas(lados);
            CrearCharcos();
            CrearNiebla();
            CrearTinacos();
            CrearHorizonte();
            CrearVolumen();

            Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t501-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-501 colocado:\n" + Informe);
        }

        // ------------------------------------------------------------------ recursos

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

        private static GameObject Instanciar(string codigo, Transform padre, Vector3 pos, float giro, float escala, bool sinCollider)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = codigo + "_T501";
            go.transform.localScale = Vector3.one * escala;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
            if (sinCollider)
            {
                Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            }
            return go;
        }

        private static void CrearRecursos()
        {
            Directory.CreateDirectory(CarpetaTexturas);
            Directory.CreateDirectory(CarpetaMateriales);
            GuardarTextura("Letrero_Magenta", TexturaLetrero(Magenta, 1), false);
            GuardarTextura("Letrero_Turquesa", TexturaLetrero(Turquesa, 2), false);
            GuardarTextura("Letrero_Verde", TexturaLetrero(new Color(0.5f, 1f, 0.3f), 3), false);
            GuardarTextura("Niebla_Degradado", TexturaDegradado(), true);
            GuardarTextura("Charco_Redondo", TexturaCharco(false), true);
            GuardarTextura("Charco_Emision", TexturaCharco(true), false);
            AssetDatabase.Refresh();
        }

        private static void GuardarTextura(string nombre, Texture2D tex, bool alfa)
        {
            string ruta = CarpetaTexturas + "/" + nombre + ".png";
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.alphaIsTransparency = alfa;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }

        private static Texture2D TexturaLetrero(Color neon, int variante)
        {
            const int w = 256, h = 128;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color fondo = new Color(0.02f, 0.02f, 0.04f, 1f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool borde = x < 8 || x >= w - 8 || y < 8 || y >= h - 8;
                    bool barra = false;
                    int cx = x - w / 2;
                    int cy = y - h / 2;
                    switch (variante)
                    {
                        case 1: barra = (Mathf.Abs(cy) < 6 && Mathf.Abs(cx) < 80) || (Mathf.Abs(cx - 40) < 6 && Mathf.Abs(cy) < 36); break;
                        case 2: barra = (Mathf.Abs(cx) < 6 && Mathf.Abs(cy) < 40) || (Mathf.Abs(cy - 24) < 6 && Mathf.Abs(cx) < 60) || (Mathf.Abs(cy + 24) < 6 && Mathf.Abs(cx) < 60); break;
                        default: barra = Mathf.Abs(Mathf.Abs(cx) + Mathf.Abs(cy) * 2 - 70) < 6; break;
                    }
                    t.SetPixel(x, y, borde || barra ? neon : fondo);
                }
            }
            t.Apply();
            return t;
        }

        private static Texture2D TexturaDegradado()
        {
            const int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Min(x, n - 1 - x) / (n * 0.35f));
                    float v = Mathf.SmoothStep(0f, 1f, Mathf.Min(y, n - 1 - y) / (n * 0.45f));
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(u * v)));
                }
            }
            t.Apply();
            return t;
        }

        private static Texture2D TexturaCharco(bool premultiplicada)
        {
            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - n / 2f) / (n / 2f);
                    float dy = (y - n / 2f) / (n / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy * 1.3f);
                    float a = Mathf.Clamp01((1f - r) * 4f);
                    float c = premultiplicada ? a * a : 1f;
                    t.SetPixel(x, y, new Color(c, c, c, a));
                }
            }
            t.Apply();
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

        private static Texture2D Tex(string nombre)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTexturas + "/" + nombre + ".png");
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

        private static Material MatCable()
        {
            return Material("Cable", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", new Color(0.015f, 0.015f, 0.02f)));
        }

        private static Material MatLetrero(string tex, Color color)
        {
            return Material("Letrero_" + tex, "Universal Render Pipeline/Lit", m =>
            {
                m.SetTexture("_BaseMap", Tex(tex));
                m.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
                m.SetTexture("_EmissionMap", Tex(tex));
                m.SetColor("_EmissionColor", color * 3.5f);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetFloat("_Cull", 0f);
            });
        }

        // ------------------------------------------------------------------ muros

        private static bool Muro(Vector3 centroCalle, Vector3 dir, float altura, out RaycastHit golpe)
        {
            Vector3 desde = new Vector3(centroCalle.x, altura, centroCalle.z);
            if (Physics.Raycast(desde, dir, out golpe, 22f, ~0, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(golpe.normal, -dir) > 0.85f
                && Mathf.Max(golpe.collider.bounds.size.x, golpe.collider.bounds.size.z) > 5f)
            {
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ cables

        private static void Catenaria(Transform padre, string nombre, Vector3 a, Vector3 b, float caida, float grosor)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            const int puntos = 14;
            lr.positionCount = puntos;
            lr.useWorldSpace = true;
            for (int i = 0; i < puntos; i++)
            {
                float t = i / (puntos - 1f);
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y -= caida * 4f * t * (1f - t);
                lr.SetPosition(i, p);
            }
            lr.startWidth = grosor;
            lr.endWidth = grosor;
            lr.sharedMaterial = MatCable();
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 2;
        }

        private static int cables;

        private static void CrearCablesCruzados(List<Lado> lados)
        {
            Transform g = Grupo("Cables");
            // Calle principal: 8 por cuadra de 80 m, de fachada a fachada.
            float paso = (XMax - XMin) / CablesPorCuadra;
            for (int i = 0; i < CablesPorCuadra; i++)
            {
                float x = XMin + paso * (i + 0.5f);
                float altura = 6.2f + (float)Azar.NextDouble() * 3f;
                Vector3 centro = new Vector3(x, 0f, 0f);
                if (Muro(centro, Vector3.forward, altura, out RaycastHit n) && Muro(centro, Vector3.back, altura, out RaycastHit s))
                {
                    float desnivel = (float)(Azar.NextDouble() - 0.5) * 1.2f;
                    Catenaria(g, "CablePrincipal_" + i, n.point + n.normal * 0.15f, s.point + s.normal * 0.15f + Vector3.up * desnivel, 0.9f + (float)Azar.NextDouble() * 0.7f, 0.05f);
                    cables++;
                    // Un segundo cable más delgado casi paralelo, como los mazos de la referencia.
                    Catenaria(g, "CablePrincipalB_" + i, n.point + n.normal * 0.15f + Vector3.up * 0.35f, s.point + s.normal * 0.15f + Vector3.up * (desnivel - 0.3f), 1.2f, 0.035f);
                    cables++;
                }
            }
            // Calle cruzada (x = -47): 8 por cuadra.
            foreach (float z0 in new[] { ZCruzadaMin, -ZCruzadaMax })
            {
                float pasoZ = (ZCruzadaMax - ZCruzadaMin) / CablesPorCuadra;
                for (int i = 0; i < CablesPorCuadra; i++)
                {
                    float z = z0 + pasoZ * (i + 0.5f);
                    float altura = 6.2f + (float)Azar.NextDouble() * 3f;
                    Vector3 centro = new Vector3(XCruzada, 0f, z);
                    if (Muro(centro, Vector3.left, altura, out RaycastHit o) && Muro(centro, Vector3.right, altura, out RaycastHit e))
                    {
                        Catenaria(g, "CableCruzada_" + z0 + "_" + i, o.point + o.normal * 0.15f, e.point + e.normal * 0.15f, 0.9f + (float)Azar.NextDouble() * 0.6f, 0.05f);
                        cables++;
                    }
                }
            }
            Informe.AppendLine("Cables cruzados: " + cables);
        }

        private static void CrearCablesDePostes()
        {
            Transform g = Grupo("CablesPostes");
            Transform farolas = GameObject.Find("Barrio").transform.Find("Farolas");
            List<Vector3> postes = new List<Vector3>();
            foreach (Transform t in farolas)
            {
                if (t.position.x < XCruzada + 10f && t.name.StartsWith("Farola"))
                {
                    postes.Add(t.position);
                }
            }
            int n = 0;
            foreach (var grupo in new[] { postes.Where(p => p.z > 0f && Mathf.Abs(p.z) < 10f).OrderBy(p => p.x).ToList(), postes.Where(p => p.z < 0f && Mathf.Abs(p.z) < 10f).OrderBy(p => p.x).ToList(),
                postes.Where(p => p.x > -58f && p.z > 0f && Mathf.Abs(p.z) >= 10f).OrderBy(p => p.z).ToList(), postes.Where(p => p.x > -58f && p.z < 0f && Mathf.Abs(p.z) >= 10f).OrderBy(p => p.z).ToList() })
            {
                for (int i = 0; i + 1 < grupo.Count; i++)
                {
                    foreach (float h in new[] { 6.4f, 6.0f, 5.6f })
                    {
                        Vector3 a = grupo[i] + Vector3.up * h;
                        Vector3 b = grupo[i + 1] + Vector3.up * (h + (float)(Azar.NextDouble() - 0.5) * 0.3f);
                        Catenaria(g, "CablePoste_" + n, a, b, Vector3.Distance(a, b) * 0.05f + 0.3f, 0.04f);
                        n++;
                        cables++;
                    }
                }
            }
            Informe.AppendLine("Cables entre postes: " + n);
        }

        // ------------------------------------------------------------------ lámparas, letreros, toldos

        private static float GiroDe(Vector3 normal)
        {
            return ColocarNoche.GiroHacia(normal);
        }

        private static int lamparas;
        private static int lucesLamparas;

        private static void CrearLamparas(List<Lado> lados)
        {
            Transform g = Grupo("Lamparas");
            Vector3 tam = Tam("N01");
            const float separacion = 9f;
            int turno = 0;
            foreach (Lado lado in lados)
            {
                for (float s = 3f; s < lado.Largo - 2f; s += separacion)
                {
                    Vector3 centro = lado.Origen + lado.Tangente * s;
                    if (!Muro(centro, lado.HaciaMuro, 2.3f, out RaycastHit muro))
                    {
                        continue;
                    }
                    PonerLampara(g, tam, muro, (turno++ % 3) == 0);
                }
            }
            // Una al lado de cada puerta de la zona (la tangente de la pared más cercana).
            foreach (Vector3 p in ColocarNoche.puertas.Where(p => p.x < XCruzada + 8f))
            {
                foreach (Vector3 dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    if (Muro(new Vector3(p.x, 0f, p.z) - dir * 1.2f, dir, 2.3f, out RaycastHit muro) && muro.distance < 2.5f)
                    {
                        Vector3 lateral = Vector3.Cross(Vector3.up, muro.normal) * 1.3f;
                        muro.point += lateral;
                        PonerLampara(g, tam, muro, true);
                        break;
                    }
                }
            }
            Informe.AppendLine("Lámparas N01: " + lamparas + " (" + lucesLamparas + " con luz)");
        }

        private static void PonerLampara(Transform padre, Vector3 tam, RaycastHit muro, bool conLuz)
        {
            Vector3 pos = muro.point + muro.normal * (tam.z * 0.5f) + Vector3.up * -0.1f;
            GameObject go = Instanciar("N01", padre, pos, GiroDe(muro.normal), 1f, true);
            go.name = "N01_T501";
            lamparas++;
            if (conLuz)
            {
                GameObject luz = new GameObject("Luz");
                luz.transform.SetParent(go.transform, false);
                luz.transform.position = pos + muro.normal * 0.5f + Vector3.up * 0.3f;
                Light l = luz.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = Calido;
                l.range = 9f;
                l.intensity = 8f;
                l.shadows = LightShadows.None;
                lucesLamparas++;
            }
        }

        private static void CrearLetreros(List<Lado> lados)
        {
            Transform g = Grupo("Letreros");
            int n = 0;
            string[] texturas = { "Letrero_Magenta", "Letrero_Turquesa", "Letrero_Magenta", "Letrero_Verde" };
            Color[] colores = { Magenta, Turquesa, Magenta, new Color(0.5f, 1f, 0.3f) };
            foreach (Lado lado in lados)
            {
                for (float s = 7f; s < lado.Largo - 4f; s += 8f)
                {
                    float altura = 3.5f + (float)Azar.NextDouble() * 1.6f;
                    Vector3 centro = lado.Origen + lado.Tangente * s;
                    if (!Muro(centro, lado.HaciaMuro, altura, out RaycastHit muro))
                    {
                        continue;
                    }
                    int k = n % 4;
                    GameObject plano = new GameObject("Letrero_" + n);
                    plano.transform.SetParent(g, false);
                    plano.transform.position = muro.point + muro.normal * 0.7f;
                    // Bandera: el plano es perpendicular al muro y mira a lo largo de la calle (dos caras).
                    Vector3 mira = lado.Tangente;
                    foreach (float sentido in new[] { 1f, -1f })
                    {
                        GameObject cara = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        Object.DestroyImmediate(cara.GetComponent<Collider>());
                        cara.name = "Cara";
                        cara.transform.SetParent(plano.transform, false);
                        cara.transform.rotation = Quaternion.LookRotation(-mira * sentido, Vector3.up);
                        cara.transform.localScale = new Vector3(1.6f, 0.8f, 1f);
                        cara.GetComponent<MeshRenderer>().sharedMaterial = MatLetrero(texturas[k], colores[k]);
                        cara.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    }
                    GameObject soporte = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(soporte.GetComponent<Collider>());
                    soporte.name = "Soporte";
                    soporte.transform.SetParent(plano.transform, false);
                    soporte.transform.position = muro.point + muro.normal * 0.35f;
                    soporte.transform.rotation = Quaternion.LookRotation(muro.normal);
                    soporte.transform.localScale = new Vector3(0.05f, 0.05f, 0.7f);
                    soporte.GetComponent<MeshRenderer>().sharedMaterial = MatCable();
                    n++;
                }
            }
            Informe.AppendLine("Letreros de neón: " + n);
        }

        private static void CrearToldos(List<Lado> lados)
        {
            Transform g = Grupo("Toldos");
            Material mat = Material("Toldo", "Universal Render Pipeline/Lit", m =>
            {
                m.SetColor("_BaseColor", new Color(0.8f, 0.55f, 0.12f));
                m.SetFloat("_Smoothness", 0.25f);
            });
            int n = 0;
            foreach (Lado lado in lados)
            {
                for (float s = 8.5f; s < lado.Largo - 3f; s += 14f)
                {
                    Vector3 centro = lado.Origen + lado.Tangente * s;
                    if (!Muro(centro, lado.HaciaMuro, 3.2f, out RaycastHit muro))
                    {
                        continue;
                    }
                    GameObject toldo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(toldo.GetComponent<Collider>());
                    toldo.name = "Toldo_" + n;
                    toldo.transform.SetParent(g, false);
                    toldo.transform.position = muro.point + muro.normal * 0.75f + Vector3.up * 0.15f;
                    toldo.transform.rotation = Quaternion.LookRotation(muro.normal) * Quaternion.Euler(12f, 0f, 0f);
                    toldo.transform.localScale = new Vector3(2.6f, 0.06f, 1.5f);
                    toldo.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    toldo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    n++;
                }
            }
            Informe.AppendLine("Toldos: " + n);
        }

        // ------------------------------------------------------------------ orillas

        private static void CrearOrillas(List<Lado> lados)
        {
            Transform g = Grupo("Orillas");
            string[] ciclo = { "N06", "C16_Tambo", "N06", "C17_Huacales", "C16_Tambo" };
            int n = 0;
            int i = 0;
            foreach (Lado lado in lados)
            {
                for (float s = 2f; s < lado.Largo - 1f; s += 5.5f)
                {
                    string codigo = ciclo[i % ciclo.Length];
                    float escala = codigo == "N06" ? 0.7f : 1f;
                    Vector3 tam = Tam(codigo) * escala;
                    Vector3 centroCalle = lado.Origen + lado.Tangente * s;
                    if (!Muro(centroCalle, lado.HaciaMuro, 1.2f, out RaycastHit muro) || AnchoBanqueta < 1.05f + tam.z + 0.3f)
                    {
                        s += 1f;
                        continue;
                    }
                    Vector3 centro = muro.point + muro.normal * (AnchoBanqueta - 0.3f - tam.z * 0.5f);
                    float suelo = ColocarNoche.Suelo(centro);
                    if (float.IsNaN(suelo))
                    {
                        continue;
                    }
                    float giro = GiroDe(muro.normal) + (float)(Azar.NextDouble() - 0.5) * 30f;
                    Bounds caja = ColocarNoche.CajaGirada(tam, new Vector3(centro.x, suelo, centro.z), giro);
                    if (!ColocarNoche.Libre(caja))
                    {
                        continue;
                    }
                    Instanciar(codigo, g, new Vector3(centro.x, suelo, centro.z), giro, escala, codigo == "N06");
                    ColocarNoche.obstaculos.Add(caja);
                    n++;
                    i++;
                }
            }
            Informe.AppendLine("Orillas (tambos, huacales, basura N06): " + n);
        }

        // ------------------------------------------------------------------ charcos y niebla

        private static void CrearCharcos()
        {
            Transform g = Grupo("Charcos");
            Color[] tintes = { Magenta, Turquesa, Calido, Color.black };
            int n = 0;
            for (int i = 0; i < 18; i++)
            {
                float x = XMin + 4f + (XMax - XMin - 8f) * (i / 17f) + (float)(Azar.NextDouble() - 0.5) * 3f;
                float z = ((float)Azar.NextDouble() - 0.5f) * 10f;
                float suelo = ColocarNoche.Suelo(new Vector3(x, 0f, z));
                if (float.IsNaN(suelo))
                {
                    continue;
                }
                Color tinte = tintes[i % tintes.Length];
                bool conTinte = tinte != Color.black;
                Material mat = Material("Charco_" + (i % tintes.Length), "Universal Render Pipeline/Lit", m =>
                {
                    m.SetTexture("_BaseMap", Tex("Charco_Redondo"));
                    m.SetColor("_BaseColor", new Color(0.02f, 0.04f, 0.08f, 0.85f));
                    m.SetFloat("_Smoothness", 1f);
                    m.SetFloat("_Metallic", 0.2f);
                    if (conTinte)
                    {
                        m.SetTexture("_EmissionMap", Tex("Charco_Emision"));
                        m.SetColor("_EmissionColor", tinte * 0.4f);
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        m.EnableKeyword("_EMISSION");
                    }
                    Transparente(m, false);
                });
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Charco_" + i;
                quad.transform.SetParent(g, false);
                quad.transform.position = new Vector3(x, suelo + 0.025f, z);
                quad.transform.rotation = Quaternion.Euler(90f, (float)Azar.NextDouble() * 360f, 0f);
                float tamano = 2f + (float)Azar.NextDouble() * 3f;
                quad.transform.localScale = new Vector3(tamano * 1.5f, tamano, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Charcos: " + n);
        }

        private static void CrearNiebla()
        {
            Transform g = Grupo("Niebla");
            Material mat = Material("Niebla_Azul", "Universal Render Pipeline/Unlit", m =>
            {
                m.SetTexture("_BaseMap", Tex("Niebla_Degradado"));
                m.SetColor("_BaseColor", new Color(0.12f, 0.3f, 0.6f, 0.3f));
                Transparente(m, true);
            });
            int n = 0;
            for (float x = -66f; x >= -132f; x -= 8f)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Niebla_" + n;
                quad.transform.SetParent(g, false);
                quad.transform.position = new Vector3(x, 5.5f, 0f);
                quad.transform.rotation = Quaternion.LookRotation(Vector3.right);
                quad.transform.localScale = new Vector3(20f, 13f, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                n++;
            }
            Informe.AppendLine("Capas de niebla azul: " + n);
        }

        // ------------------------------------------------------------------ azoteas

        private static void CrearTinacos()
        {
            Transform g = Grupo("Tinacos");
            Transform barrio = GameObject.Find("Barrio").transform;
            Vector3 tam = Tam("C09_Tinaco");
            const float escala = 1.35f;
            int n = 0;
            int sinHueco = 0;
            foreach (string manzana in new[] { "Manzana_1", "Manzana_4" })
            {
                Transform edificios = barrio.Find(manzana + "/Edificios");
                foreach (Transform edificio in edificios)
                {
                    Bounds b = ArteT250.Limites(edificio.gameObject);
                    bool puesto = false;
                    foreach (Vector2 f in new[] { new Vector2(0.3f, 0.3f), new Vector2(-0.3f, 0.3f), new Vector2(0.3f, -0.3f), new Vector2(-0.3f, -0.3f), new Vector2(0f, 0f), new Vector2(0.15f, -0.4f), new Vector2(-0.4f, 0.15f) })
                    {
                        Vector3 arriba = new Vector3(b.center.x + b.extents.x * f.x, b.max.y + 5f, b.center.z + b.extents.z * f.y);
                        if (!Physics.Raycast(arriba, Vector3.down, out RaycastHit techo, 60f, ~0, QueryTriggerInteraction.Ignore) || techo.point.y < 6f || !techo.collider.transform.IsChildOf(edificio))
                        {
                            continue;
                        }
                        Bounds caja = ColocarNoche.CajaGirada(tam, techo.point + Vector3.up * 0.3f, 0f, Vector3.one * escala);
                        if (!ColocarNoche.Libre(caja))
                        {
                            continue;
                        }
                        Instanciar("C09_Tinaco", g, techo.point, n * 47f, escala, true);
                        ColocarNoche.obstaculos.Add(caja);
                        n++;
                        puesto = true;
                        break;
                    }
                    if (!puesto)
                    {
                        sinHueco++;
                    }
                }
            }
            Informe.AppendLine("Tinacos C09: " + n + " (azoteas sin hueco libre: " + sinHueco + ")");
        }

        // ------------------------------------------------------------------ horizonte

        private static Material MatTorre(string nombre, float semilla, float niebla, float probLuz, Action<Material> extra = null)
        {
            return Material("Torre_" + nombre, RutaShaderTorre, m =>
            {
                m.SetColor("_ColorBase", new Color(0.015f, 0.02f, 0.06f));
                m.SetFloat("_Semilla", semilla);
                m.SetFloat("_Niebla", niebla);
                m.SetFloat("_ProbLuz", probLuz);
                m.SetFloat("_Brillo", 1.7f);
                m.SetColor("_ColorNiebla", new Color(0.07f, 0.1f, 0.3f));
                extra?.Invoke(m);
            });
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
            GameObjectUtility.SetStaticEditorFlags(c, StaticEditorFlags.BatchingStatic);
            return c;
        }

        private static void CrearHorizonte()
        {
            Transform g = Grupo("Horizonte");
            Material[] cuerpos =
            {
                MatTorre("Cerca_A", 1f, 0.5f, 0.35f), MatTorre("Cerca_B", 7f, 0.5f, 0.3f),
                MatTorre("Media_A", 3f, 0.62f, 0.35f), MatTorre("Media_B", 9f, 0.62f, 0.4f),
                MatTorre("Lejos_A", 5f, 0.74f, 0.4f), MatTorre("Lejos_B", 11f, 0.74f, 0.3f),
            };
            Material rojo = MatTorre("LuzRoja", 0f, 0.1f, 0f, m => m.SetColor("_ColorBase", new Color(6f, 0.15f, 0.12f)));
            Material verde = MatTorre("PantallaVerde", 0f, 0.15f, 0f, m => m.SetColor("_ColorBase", new Color(0.5f, 3.2f, 0.7f)));
            Material magenta = MatTorre("PantallaMagenta", 0f, 0.15f, 0f, m => m.SetColor("_ColorBase", new Color(3.2f, 0.4f, 2.2f)));
            Texture texE03 = TexturaDe("E03_PantallaGigante");
            Texture texCY02 = TexturaDe("CY02_PantallaVertical");
            Material pantallaE03 = MatTorre("PantallaE03", 0f, 0.15f, 0f, m => { m.SetFloat("_UsarTextura", 1f); m.SetTexture("_MainTex", texE03); m.SetFloat("_Brillo", 0.9f); });
            Material pantallaCY02 = MatTorre("PantallaCY02", 0f, 0.15f, 0f, m => { m.SetFloat("_UsarTextura", 1f); m.SetTexture("_MainTex", texCY02); m.SetFloat("_Brillo", 0.9f); });

            int n = 0;
            int pantallas = 0;
            int antenas = 0;
            int intentos = 0;
            Vector3 jaula = new Vector3(400f, 0f, 0f);
            while (n < CantidadTorres && intentos++ < 1000)
            {
                float ang = (float)Azar.NextDouble() * Mathf.PI * 2f;
                float radio = 340f + (float)Azar.NextDouble() * 480f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * radio, 0f, Mathf.Sin(ang) * radio);
                if (Vector3.Distance(pos, jaula) < 130f || Vector3.Distance(pos, new Vector3(200f, 0f, 0f)) < 70f)
                {
                    continue;
                }
                int banda = radio < 500f ? 0 : (radio < 680f ? 1 : 2);
                Material cuerpo = cuerpos[banda * 2 + (n % 2)];
                float ancho = 24f + (float)Azar.NextDouble() * 26f;
                float fondo = 24f + (float)Azar.NextDouble() * 26f;
                float alto = 90f + (float)Azar.NextDouble() * 190f;
                Transform torre = new GameObject("Torre_" + n).transform;
                torre.SetParent(g, false);
                torre.position = pos;
                Cubo(torre, "Cuerpo", pos + Vector3.up * alto * 0.5f, new Vector3(ancho, alto, fondo), cuerpo);
                float topeY = alto;
                if (n % 3 == 0)
                {
                    float alto2 = alto * 0.35f;
                    Cubo(torre, "Remate", pos + Vector3.up * (alto + alto2 * 0.5f), new Vector3(ancho * 0.6f, alto2, fondo * 0.6f), cuerpo);
                    topeY += alto2;
                }
                if (n % 2 == 0)
                {
                    float largoAntena = 14f + (float)Azar.NextDouble() * 14f;
                    Cubo(torre, "Antena", pos + Vector3.up * (topeY + largoAntena * 0.5f), new Vector3(1.4f, largoAntena, 1.4f), cuerpo);
                    Cubo(torre, "LuzRoja", pos + Vector3.up * (topeY + largoAntena + 1f), new Vector3(2.6f, 2.6f, 2.6f), rojo);
                    antenas++;
                }
                // Pantalla en la cara que mira al centro de la ciudad (verde, magenta, E03 o CY02).
                if (banda < 2 && n % 2 == 1 || n % 5 == 0)
                {
                    Vector3 haciaCentro = new Vector3(-pos.x, 0f, -pos.z).normalized;
                    bool ejeX = Mathf.Abs(haciaCentro.x) > Mathf.Abs(haciaCentro.z);
                    Vector3 normal = ejeX ? new Vector3(Mathf.Sign(haciaCentro.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(haciaCentro.z));
                    float mitad = (ejeX ? ancho : fondo) * 0.5f;
                    float ladoPantalla = (ejeX ? fondo : ancho) * 0.65f;
                    float altoPantalla = Mathf.Min(alto * 0.4f, ladoPantalla * 1.7f);
                    int tipo = pantallas % 4;
                    Material mp = tipo == 0 ? verde : (tipo == 1 ? magenta : (tipo == 2 ? pantallaE03 : pantallaCY02));
                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.DestroyImmediate(quad.GetComponent<Collider>());
                    quad.name = "Pantalla";
                    quad.transform.SetParent(torre, false);
                    quad.transform.position = pos + normal * (((ejeX ? ancho : fondo) * 0.5f) + 0.3f) + Vector3.up * (alto * 0.62f);
                    quad.transform.rotation = Quaternion.LookRotation(-normal);
                    quad.transform.localScale = new Vector3(ladoPantalla, altoPantalla, 1f);
                    quad.GetComponent<MeshRenderer>().sharedMaterial = mp;
                    quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    pantallas++;
                    _ = mitad;
                }
                n++;
            }
            Informe.AppendLine("Torres de horizonte: " + n + " (pantallas " + pantallas + ", antenas con luz roja " + antenas + ")");
        }

        // ------------------------------------------------------------------ posproceso local

        /// <summary>Crea el perfil si no existe, o lo rehace en su lugar (mismo GUID: la escena sigue apuntando a él). Los overrides van como sub-assets.</summary>
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
            ca.postExposure.Override(-0.25f);
            ca.saturation.Override(4f);
            ca.colorFilter.Override(new Color(0.85f, 0.95f, 1.1f));
            Vignette vi = perfil.Add<Vignette>(true);
            vi.intensity.Override(0.22f);
            AssetDatabase.AddObjectToAsset(ca, perfil);
            AssetDatabase.AddObjectToAsset(vi, perfil);
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        /// <summary>Solo regenera el perfil de Suburbios (sin tocar la escena).</summary>
        [MenuItem("Mecha Cyberpunk/T-501/Regenerar perfil de Suburbios")]
        public static void RegenerarPerfil()
        {
            VolumeProfile p = CrearPerfil();
            Debug.Log("[Arte] PerfilSuburbios: " + p.components.Count + " componentes");
        }

        private static void CrearVolumen()
        {
            VolumeProfile perfil = CrearPerfil();

            GameObject go = new GameObject("VolumenSuburbios");
            go.transform.SetParent(raiz, false);
            go.transform.position = new Vector3(-92f, 15f, 0f);
            BoxCollider caja = go.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(96f, 40f, 190f);
            Volume v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = 12f;
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
            Informe.AppendLine("Volumen de posproceso local de Suburbios (solo de noche): " + (tipo != null));
        }
    }
}
