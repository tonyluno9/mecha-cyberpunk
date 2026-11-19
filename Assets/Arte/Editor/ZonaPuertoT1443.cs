using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Mundo.Ambiente;
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
    /// T-1443 (Puerto, vista aérea A15): el patio como en la referencia. Edita T1357_Puerto.prefab SIN rehacerlo con ColocarT1357Puerto.
    /// Todo lo nuevo vive bajo el hijo raíz "T1443_Patio" (Aplicar lo borra y lo rehace). Lo que cambia de lo viejo es solo de materiales y es reversible:
    /// los contenedores y las grúas reciben un material propio "T1443_*__&lt;original&gt;" (en Assets/Arte/Mundo/Puerto/) y Aplicar vuelve a poner el original antes de rehacer.
    /// 1) Contenedores a más de 85 m de las cámaras K15 con colores variados (rojo óxido, verde, azul, naranja, gris, turquesa).
    /// 2) Pintura del patio (malla combinada 2 cm sobre el piso): doble borde de manzana, flechas de carril; charcos que reflejan (materiales S2c_Charco_*, sin editarlos).
    /// 3) Postes de sodio en carriles y esquinas con cabeza emisiva, halo y charco de luz aditivo (máx. 8 Light).
    /// 4) Grúas PU01: material más claro, balizas rojas extra y verdes (emisivas); barco PU02: carga de contenedores de colores, ventanas, mástiles con lámparas.
    /// 5) Bodegas PU03/PU04 en la franja este del patio con ventiladores, neones, lámparas; pallets, cajas, barriles, montacargas, caseta con pluma y reja.
    /// Todo lo nuevo se revisa contra los renderers existentes (bounds) y se salta lo que estorba: puede correr antes o después de T-1442 (K15, raíz T1442_Calle).
    /// Orden: Aplicar, Validar; Fotos en una corrida de Unity aparte.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaPuertoT1443.Aplicar (y .Validar, .Fotos)
    /// </summary>
    public static class ZonaPuertoT1443
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        public const string Raiz = "T1443_Patio";
        public const string Carpeta = "Assets/Arte/Mundo/Puerto";
        public const string RutaMallas = Carpeta + "/T1443_Mallas.asset";
        public const string RutaInforme = "Logs/t1443-aplicar.txt";
        public const string RutaValidar = "Logs/t1443-validar.txt";
        public const string RutaFoto = "docs/capturas/_vista19b/puerto-a15-t1443.png";
        public const int Semilla = 14430;
        public const int TopeLuces = 8;
        public const int TopeObjetos = 3000;
        public const long TopeBytesPrefab = 60L * 1024 * 1024;

        /// <summary>Cámara A15 (esquina noreste del muelle, mirando al suroeste: agua y barco arriba a la izquierda, bodegas abajo a la derecha) y su mira.</summary>
        public static readonly Vector3 CamaraA15 = new Vector3(320f, 50f, -150f);
        public static readonly Vector3 MiraA15 = new Vector3(240f, 0f, -215f);
        public const float CampoA15 = 60f;

        private const float YPatio = 0f;
        private const float MediaCalle = 6f;
        private const float LargoCont = 12.19f, AltoCont = 2.59f, AnchoCont = 2.44f;
        /// <summary>Las cámaras K15 son de T-1442 (raíz T1442_Calle, hasta 80 m): aquí no se toca nada a menos de 85 m de ellas.</summary>
        private const float RadioK15 = 85f;
        private static readonly Vector2[] CamarasK15 = { new Vector2(157f, -98f), new Vector2(158.6f, -84f) };
        private const float MaxAnchoBodega = 15.5f, ParedEsteBodega = 288.5f;
        private const string Prefijo = "T1443_";

        private static readonly Color[] Paleta =
        {
            new Color(0.82f, 0.33f, 0.2f), new Color(0.3f, 0.64f, 0.46f), new Color(0.3f, 0.5f, 0.82f), new Color(0.95f, 0.55f, 0.18f),
            new Color(0.62f, 0.65f, 0.7f), new Color(0.2f, 0.65f, 0.68f), new Color(0.62f, 0.2f, 0.18f),
        };
        private static readonly float[] PesoPaleta = { 3f, 3f, 2f, 2f, 1f, 1f, 2f };

        // ---------------------------------------------------------------- Estado de una corrida

        private sealed class Lote
        {
            public string Nombre;
            public Material Mat;
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> T = new List<int>();
            public bool Sombras;
        }

        private static GameObject zona;
        private static Transform raiz;
        private static KitS2c.Obra obra;
        private static PhysicsScene fisica;
        private static readonly List<Bounds> ocupados = new List<Bounds>();
        private static readonly List<Bounds> tramos = new List<Bounds>();
        private static readonly Dictionary<string, Lote> lotes = new Dictionary<string, Lote>();
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Material> originales = new Dictionary<string, Material>();
        private static readonly List<string> faltan = new List<string>();
        private static readonly List<Vector3> pozas = new List<Vector3>();
        private static StringBuilder informe;
        private static int colocados;
        private static readonly List<Bounds> contBounds = new List<Bounds>();
        private static Vector3 camPos, camF, camR, camU;
        private static bool hayCamara;
        private const string SufijoApartado = "_T1443Apartado", SufijoColision = "_T1443Colision";
        private const float TanV = 0.5774f, Aspecto = 1376f / 768f;

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1443/Puerto: aplicar patio A15")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1443: patio del Puerto (A15)\n");
            faltan.Clear();
            prefabs.Clear();
            originales.Clear();
            lotes.Clear();
            ocupados.Clear();
            tramos.Clear();
            pozas.Clear();
            bodegasNuevas.Clear();
            sitiosBodega.Clear();
            razones.Clear();
            hayCamara = false;
            colocados = 0;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                Log("ERROR: no existe " + RutaPrefab);
                Escribir(RutaInforme);
                return;
            }
            Directory.CreateDirectory(Carpeta);
            AssetDatabase.Refresh();
            zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform vieja = zona.transform.Find(Raiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                    Log("Raíz anterior " + Raiz + " borrada (se rehace).");
                }
                RestaurarMateriales();
                BorrarMaterialesPropios();
                Physics.SyncTransforms();
                fisica = zona.scene.GetPhysicsScene();
                int lucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                int objetosAntes = zona.GetComponentsInChildren<Transform>(true).Length;
                Log("Antes: " + lucesAntes + " luces, " + objetosAntes + " objetos, prefab " + Megas(RutaPrefab) + " MB.");
                obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                raiz = obra.Raiz;
                RestaurarApartados();
                CargarOcupados();
                Paso("grúas", Gruas);
                Paso("cámara A15", BuscarCamara);
                Paso("bodegas y detalles", Bodegas);
                Paso("relleno de manzanas", Rellenar);
                Paso("contenedores de colores", Contenedores);
                Paso("caseta y reja", CasetaYReja);
                Paso("postes y luz", Postes);
                Paso("carriles estacionados", Carriles);
                Paso("barco", Barco);
                Paso("pintura", Pintura);
                Paso("charcos", Charcos);
                Paso("lotes", ConstruirLotes);
                MarcarEstatico();
                GuardarMallas();
                int luces = raiz.GetComponentsInChildren<Light>(true).Length;
                int objetos = raiz.GetComponentsInChildren<Transform>(true).Length;
                Log("Luces nuevas: " + luces + " (tope " + TopeLuces + "). Objetos nuevos: " + objetos + " (tope " + TopeObjetos + "). Luces en el prefab: " + zona.GetComponentsInChildren<Light>(true).Length + " (antes " + lucesAntes + ").");
                if (luces > TopeLuces || objetos > TopeObjetos)
                {
                    Log("AVISO: se pasó de un tope.");
                }
                if (faltan.Count > 0)
                {
                    Log("Assets que faltan: " + string.Join(", ", faltan.Distinct()));
                }
                Physics.SyncTransforms();
                bool ok;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(zona, RutaPrefab, out ok);
                Log(ok && guardado != null ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log("Prefab después: " + Megas(RutaPrefab) + " MB (tope 60).");
            }
            catch (System.Exception e)
            {
                Log("ERROR: " + e);
                Debug.LogError("[ZonaPuertoT1443] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
                zona = null;
                raiz = null;
            }
            Escribir(RutaInforme);
        }

        private static void Paso(string nombre, System.Action accion)
        {
            try
            {
                accion();
            }
            catch (System.Exception e)
            {
                Log("ERROR en '" + nombre + "': " + e);
                Debug.LogError("[ZonaPuertoT1443] " + nombre + ": " + e);
            }
        }

        // ================================================================ Materiales (reversibles)

        private static Material MatPropio(string nombre, System.Func<Material> crear)
        {
            string ruta = Carpeta + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null)
            {
                return m;
            }
            m = crear();
            m.name = nombre;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        private static Material Emisivo(string nombre, Color baseColor, Color emision)
        {
            return MatPropio(Prefijo + nombre, () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", baseColor);
                m.SetFloat("_Smoothness", 0.3f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emision);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
        }

        private static Material Opaco(string nombre, Color color, float suavidad, float metal)
        {
            return MatPropio(Prefijo + nombre, () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", suavidad);
                m.SetFloat("_Metallic", metal);
                return m;
            });
        }

        private static Material MatPoza()
        {
            return MatPropio(Prefijo + "PozaLuz", () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                m.SetTexture("_BaseMap", TexRadial());
                m.SetColor("_BaseColor", new Color(1f, 0.6f, 0.25f, 0.6f));
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", 5f);
                m.SetFloat("_DstBlend", 10f);
                m.SetFloat("_SrcBlendAlpha", 1f);
                m.SetFloat("_DstBlendAlpha", 10f);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetShaderPassEnabled("ShadowCaster", false);
                return m;
            });
        }

        private static Texture2D TexRadial()
        {
            string ruta = Carpeta + "/T1443_PozaLuzAlfa.png";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (t != null)
            {
                return t;
            }
            const int n = 64;
            Texture2D img = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float v = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    v = v * v * (3f - 2f * v);
                    img.SetPixel(x, y, new Color(1f, 1f, 1f, dx * dx + dy * dy >= 0.9f ? 0f : v * v));
                }
            }
            File.WriteAllBytes(ruta, img.EncodeToPNG());
            Object.DestroyImmediate(img);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (ti != null)
            {
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        /// <summary>Vuelve a poner el material original en cada renderer que tenga uno "T1443_*__&lt;original&gt;".</summary>
        private static void RestaurarMateriales()
        {
            int n = 0;
            foreach (Renderer r in zona.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material m = ms[i];
                    if (m == null || !m.name.StartsWith(Prefijo))
                    {
                        continue;
                    }
                    int k = m.name.IndexOf("__");
                    if (k < 0)
                    {
                        continue;
                    }
                    Material orig = BuscarMaterial(m.name.Substring(k + 2));
                    if (orig == null)
                    {
                        Log("AVISO: no encuentro el material original '" + m.name.Substring(k + 2) + "' de " + r.name);
                        continue;
                    }
                    ms[i] = orig;
                    cambio = true;
                    n++;
                }
                if (cambio)
                {
                    r.sharedMaterials = ms;
                }
            }
            Log("Materiales originales restaurados: " + n + ".");
        }

        private static Material BuscarMaterial(string nombre)
        {
            Material m;
            if (originales.TryGetValue(nombre, out m))
            {
                return m;
            }
            foreach (string g in AssetDatabase.FindAssets(nombre + " t:Material"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);
                if (ruta.StartsWith(Carpeta + "/"))
                {
                    continue;
                }
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                {
                    Material c = o as Material;
                    if (c != null && c.name == nombre)
                    {
                        originales[nombre] = c;
                        return c;
                    }
                }
            }
            originales[nombre] = null;
            return null;
        }

        private static void BorrarMaterialesPropios()
        {
            foreach (string g in AssetDatabase.FindAssets("t:Material", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileName(ruta).StartsWith(Prefijo))
                {
                    AssetDatabase.DeleteAsset(ruta);
                }
            }
        }

        /// <summary>Cambia el color base de cada material del objeto por uno propio (clon con el mismo mapa), nombrado "T1443_&lt;codigo&gt;_&lt;color&gt;__&lt;original&gt;".</summary>
        private static void Tintar(GameObject go, Color color)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material orig = ms[i];
                    if (orig == null || !orig.HasProperty("_BaseColor") || orig.name.StartsWith(Prefijo))
                    {
                        continue;
                    }
                    Color c = color;
                    string nombre = Prefijo + "T_" + ColorUtility.ToHtmlStringRGB(c) + "__" + orig.name;
                    ms[i] = MatPropio(nombre, () =>
                    {
                        Material m = new Material(orig);
                        m.SetColor("_BaseColor", c);
                        if (m.HasProperty("_Smoothness"))
                        {
                            m.SetFloat("_Smoothness", 0.35f);
                        }
                        if (m.HasProperty("_Metallic"))
                        {
                            m.SetFloat("_Metallic", 0.15f);
                        }
                        return m;
                    });
                }
                r.sharedMaterials = ms;
            }
        }

        private static void Aclarar(GameObject go)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material orig = ms[i];
                    if (orig == null || !orig.HasProperty("_BaseColor") || orig.name.StartsWith(Prefijo))
                    {
                        continue;
                    }
                    ms[i] = MatPropio(Prefijo + "G__" + orig.name, () =>
                    {
                        Material m = new Material(orig);
                        Color c = orig.GetColor("_BaseColor");
                        m.SetColor("_BaseColor", new Color(Mathf.Min(1f, c.r * 2f + 0.07f), Mathf.Min(1f, c.g * 2.1f + 0.09f), Mathf.Min(1f, c.b * 2.4f + 0.14f), c.a));
                        if (m.HasProperty("_Smoothness"))
                        {
                            m.SetFloat("_Smoothness", 0.4f);
                        }
                        return m;
                    });
                }
                r.sharedMaterials = ms;
            }
        }

        private static Color ColorAlAzar()
        {
            float total = PesoPaleta.Sum();
            float t = obra.Rango(0f, total);
            for (int i = 0; i < Paleta.Length; i++)
            {
                t -= PesoPaleta[i];
                if (t <= 0f)
                {
                    return Paleta[i];
                }
            }
            return Paleta[0];
        }

        // ================================================================ Lugar: ocupación, calles, suelo

        private static bool EsContenedorViejo(Transform t)
        {
            return Categoria(t) == 0;
        }

        /// <summary>0 contenedor, 1 grúa, 2 barco, 3 bodega, -1 otra cosa (por el nombre de algún ancestro).</summary>
        private static int Categoria(Transform t)
        {
            for (Transform p = t; p != null && p != zona.transform; p = p.parent)
            {
                string n = p.name;
                if (n.Contains("Reemplazado") || n.EndsWith(SufijoApartado))
                {
                    return -1;
                }
                if (n.StartsWith("PU01_Grua")) return 1;
                if (n.StartsWith("PU02_Barco")) return 2;
                if (n.StartsWith("PU03_") || n.StartsWith("PU04_")) return 3;
                if (n.StartsWith("PT01_Contenedor") || n.StartsWith("PU08")) return 0;
            }
            return -1;
        }

        private static bool LibreSinContenedores(Bounds b, float margen)
        {
            Bounds e = b;
            e.Expand(new Vector3(margen * 2f, 0f, margen * 2f));
            for (int i = 0; i < ocupados.Count; i++)
            {
                if (ocupados[i].Intersects(e) && !contBounds.Contains(ocupados[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Reversible: apaga (y renombra con sufijo) los contenedores, escaleras y grafitis dentro de la caja y los colliders de su manzana.</summary>
        private static void Apartar(Bounds b)
        {
            Bounds e = b;
            e.Expand(new Vector3(1f, 0f, 1f));
            int n = 0;
            foreach (Transform t in zona.GetComponentsInChildren<Transform>(false).ToList())
            {
                if (t.IsChildOf(raiz) || t.name.EndsWith(SufijoApartado))
                {
                    continue;
                }
                bool candidato = t.name.StartsWith("PT01_Contenedor") || t.name.StartsWith("PU08") || t.name.Contains("Grafiti") || t.name.StartsWith("Escalera_");
                if (!candidato || t.GetComponentInChildren<Renderer>() == null && !t.name.StartsWith("Escalera_"))
                {
                    continue;
                }
                Bounds bt = t.name.StartsWith("Escalera_") ? new Bounds(t.position, Vector3.one * 0.5f) : Medir(t.gameObject);
                if (!e.Intersects(bt))
                {
                    continue;
                }
                Transform bloque = t.parent;
                if (bloque != null && bloque.name.StartsWith("Bloque_") && !bloque.name.EndsWith(SufijoColision))
                {
                    foreach (BoxCollider bc in bloque.GetComponents<BoxCollider>())
                    {
                        bc.enabled = false;
                    }
                    bloque.name += SufijoColision;
                }
                t.name += SufijoApartado;
                t.gameObject.SetActive(false);
                n++;
            }
            ocupados.RemoveAll(o => contBounds.Contains(o) && o.Intersects(e));
            contBounds.RemoveAll(o => o.Intersects(e));
            Log("Apartadas " + n + " piezas de pilas (sufijo " + SufijoApartado + "; los colliders de su manzana se apagan y la manzana lleva " + SufijoColision + ").");
        }

        private static void RestaurarApartados()
        {
            int n = 0;
            foreach (Transform t in zona.GetComponentsInChildren<Transform>(true).ToList())
            {
                if (t.name.EndsWith(SufijoApartado))
                {
                    t.name = t.name.Substring(0, t.name.Length - SufijoApartado.Length);
                    t.gameObject.SetActive(true);
                    n++;
                }
                else if (t.name.EndsWith(SufijoColision))
                {
                    t.name = t.name.Substring(0, t.name.Length - SufijoColision.Length);
                    foreach (BoxCollider bc in t.GetComponents<BoxCollider>())
                    {
                        bc.enabled = true;
                    }
                    n++;
                }
            }
            Log("Pilas apartadas por una corrida anterior: " + n + " restauradas.");
        }

        // ---------------------------------------------------------------- Cámara
        private static readonly List<Vector3> sitiosBodega = new List<Vector3>();
        private const float SitioX = 25f, SitioZ = 24.4f, AltoSitio = 14f;

        private static Bounds CajaSitio(Vector3 c)
        {
            return new Bounds(new Vector3(c.x, AltoSitio * 0.5f, c.z), new Vector3(SitioX, AltoSitio, SitioZ));
        }

        /// <summary>Pares de manzanas contiguas (misma x, filas consecutivas) del este del patio y lejos de la ciudad (norte y oeste): los centros de los sitios posibles para una bodega, del más lejano al más cercano.</summary>
        private static List<Vector3> CandidatosSitios(bool todos = false)
        {
            List<ColocarT1357Puerto.Bloque> bl = ColocarT1357Puerto.Bloques();
            List<(Vector3 c, float lejos)> l = new List<(Vector3, float)>();
            foreach (ColocarT1357Puerto.Bloque a in bl)
            {
                foreach (ColocarT1357Puerto.Bloque b in bl)
                {
                    if (Mathf.Abs(a.Centro.x - b.Centro.x) > 0.1f || Mathf.Abs(a.Centro.y - b.Centro.y - (ColocarT1357Puerto.FondoBloque + ColocarT1357Puerto.PasilloZ)) > 0.1f)
                    {
                        continue;
                    }
                    Vector3 c = new Vector3(a.Centro.x, 0f, (a.Centro.y + b.Centro.y) * 0.5f);
                    Bounds caja = CajaSitio(c);
                    if ((!todos && c.x < 225f) || CercaK15(c) || EnCalle(caja, 0.2f) || !LibreSinContenedores(caja, 0.3f))
                    {
                        continue;
                    }
                    l.Add((c, Mathf.Min(-45f - c.z, c.x - 115f)));
                }
            }
            return l.OrderByDescending(t => t.lejos).Select(t => t.c).ToList();
        }

        private static bool Proyectar(Vector3 w, out float u, out float v, out float prof)
        {
            Vector3 d = w - camPos;
            prof = Vector3.Dot(d, camF);
            u = v = 0f;
            if (prof < 3f)
            {
                return false;
            }
            u = 0.5f + 0.5f * Vector3.Dot(d, camR) / (prof * TanV * Aspecto);
            v = 0.5f + 0.5f * Vector3.Dot(d, camU) / (prof * TanV);
            return true;
        }
        /// <summary>Cámara A15 fija de la ronda 5 (elegida a mano por el orquestador).</summary>
        public static readonly Vector3 CamaraFija = new Vector3(320f, 50f, -150f);
        public static readonly Vector3 MiraFija = new Vector3(240f, 0f, -215f);

        /// <summary>
        /// Ronda 5: cámara fija (CamaraFija a MiraFija, campo 60) guardada en T1443_Patio/CamaraA y CamaraA/Mira. Con su matriz proyecta los pares de manzanas contiguas del patio y se queda con los 2 a 3
        /// que caen abajo a la derecha del cuadro (u &gt; 0.55, v &lt; 0.45, a menos de 220 m), los más cercanos a (0.75, 0.25); esos sitios se apartan (reversible) para las bodegas.
        /// </summary>
        private static void BuscarCamara()
        {
            camPos = CamaraFija;
            camF = (MiraFija - CamaraFija).normalized;
            camR = Vector3.Cross(Vector3.up, camF).normalized;
            camU = Vector3.Cross(camF, camR);
            hayCamara = true;
            GameObject cam = new GameObject("CamaraA");
            cam.transform.SetParent(raiz, false);
            cam.transform.position = camPos;
            GameObject mira = new GameObject("Mira");
            mira.transform.SetParent(cam.transform, false);
            mira.transform.position = MiraFija;
            Log("CÁMARA A15 FIJA: " + camPos.ToString("F1") + " mirando a " + MiraFija.ToString("F1") + ", campo 60.");
            // Medida de una bodega (largo 22) sin girar.
            GameObject prueba = Colocar(Grupo("Bodegas"), "PU03_Bodega", new Vector3(0f, YPatio, 0f), 0f, 22f, 0f, false);
            if (prueba == null)
            {
                Log("Bodegas: falta PU03_Bodega.");
                return;
            }
            Vector3 medida = Medir(prueba).size;
            Object.DestroyImmediate(prueba);
            razones.Clear();
            int fueraPrefab = 0, enRegion = 0;
            List<(Vector3 c, Bounds caja, float nota)> sitios = new List<(Vector3, Bounds, float)>();
            // Todo el suelo cerca del puerto, cada 4 m: ¿cae abajo a la derecha del cuadro?
            for (float x = 40f; x <= 420f; x += 4f)
            {
                for (float z = -330f; z <= 40f; z += 4f)
                {
                    float u, v, prof;
                    if (!Proyectar(new Vector3(x, 0f, z), out u, out v, out prof) || prof > 220f || u < 0.55f || u > 0.95f || v < 0.05f || v > 0.45f)
                    {
                        continue;
                    }
                    enRegion++;
                    if (x < 118f || x > 314f || z < -236f || z > -50f)
                    {
                        fueraPrefab++;
                        Razon("el piso es de otra zona o agua (fuera del rectángulo del puerto x 118..314, z -236..-50)");
                        continue;
                    }
                    Vector3 haciaCam = camPos - new Vector3(x, 0f, z);
                    haciaCam.y = 0f;
                    haciaCam.Normalize();
                    int mq = 0;
                    float md = -2f;
                    for (int q = 0; q < 4; q++)
                    {
                        Vector3 puerta = q == 0 ? Vector3.forward : q == 1 ? Vector3.right : q == 2 ? Vector3.back : Vector3.left;
                        if (Vector3.Dot(puerta, haciaCam) > md)
                        {
                            md = Vector3.Dot(puerta, haciaCam);
                            mq = q;
                        }
                    }
                    Vector3 tam = mq % 2 == 1 ? new Vector3(medida.z, medida.y, medida.x) : medida;
                    Bounds b = new Bounds(new Vector3(x, tam.y * 0.5f, z), tam);
                    if (b.min.x < 118f || b.max.x > 314f || b.min.z < -236f || b.max.z > -50f)
                    {
                        Razon("la bodega se saldría del patio");
                        continue;
                    }
                    if (CercaK15(b.center))
                    {
                        Razon("a menos de 85 m de K15 (de T-1442)");
                        continue;
                    }
                    if (EnCalle(b, 0.3f))
                    {
                        Razon("pisa el carril de una calle");
                        continue;
                    }
                    if (!LibreSinContenedores(b, 0.4f))
                    {
                        Razon("choca con algo que no es contenedor (poste, montacargas, bodega, grúa...)");
                        continue;
                    }
                    sitios.Add((new Vector3(x, 0f, z), b, Vector2.Distance(new Vector2(u, v), new Vector2(0.75f, 0.25f))));
                }
            }
            Log("Piso de la región abajo a la derecha (u 0.55 a 0.95, v 0.05 a 0.45): " + enRegion + " puntos de 4 m; " + fueraPrefab + " fuera del puerto (otra zona o agua). Sitios válidos: " + sitios.Count + ". Descartes -> " + string.Join("; ", razones.Select(kv => kv.Key + ": " + kv.Value)));
            razones.Clear();
            sitiosBodega.Clear();
            List<Bounds> elegidas = new List<Bounds>();
            foreach (var s in sitios.OrderBy(t => t.nota))
            {
                if (sitiosBodega.Count >= 3)
                {
                    break;
                }
                Bounds grande = s.caja;
                grande.Expand(new Vector3(2f, 0f, 2f));
                if (elegidas.Any(o => o.Intersects(grande)))
                {
                    continue;
                }
                elegidas.Add(s.caja);
                sitiosBodega.Add(s.c);
            }
            foreach (Bounds cj in elegidas)
            {
                Log("Sitio de bodega " + cj.center.ToString("F1") + " (huella " + cj.size.x.ToString("F1") + " x " + cj.size.z.ToString("F1") + "); se apartan los contenedores que pisa.");
                Apartar(cj);
            }
        }


        private static void CargarOcupados()
        {
            contBounds.Clear();
            foreach (Renderer r in zona.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r.transform.IsChildOf(raiz))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (r.gameObject.name == "Tramo" && b.size.y < 0.6f)
                {
                    tramos.Add(b);
                }
                if (r.name == "Halo" || r.name.Contains("Cable") || r.name == "Linea" || r.name == "Charco" || b.size.y < 0.3f || b.size.x > 60f || b.size.z > 60f)
                {
                    continue;
                }
                ocupados.Add(b);
                if (EsContenedorViejo(r.transform)) { contBounds.Add(b); }
            }
            Log("Renderers que cuentan como obstáculo: " + ocupados.Count + "; tramos de asfalto: " + tramos.Count + ".");
        }

        private static bool CercaK15(Vector3 p)
        {
            return CamarasK15.Any(c => Vector2.Distance(c, new Vector2(p.x, p.z)) < RadioK15);
        }

        private static bool Libre(Bounds b, float margen)
        {
            Bounds e = b;
            e.Expand(new Vector3(margen * 2f, 0f, margen * 2f));
            for (int i = 0; i < ocupados.Count; i++)
            {
                if (ocupados[i].Intersects(e))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Si la caja pisa el arroyo de alguna calle interna (ancho 12 m) más el margen.</summary>
        private static bool EnCalle(Bounds b, float margen)
        {
            foreach (Vector3[] c in ColocarT1357Puerto.Calles())
            {
                for (int i = 1; i < c.Length; i++)
                {
                    float x0 = Mathf.Min(c[i - 1].x, c[i].x) - MediaCalle - margen, x1 = Mathf.Max(c[i - 1].x, c[i].x) + MediaCalle + margen;
                    float z0 = Mathf.Min(c[i - 1].z, c[i].z) - MediaCalle - margen, z1 = Mathf.Max(c[i - 1].z, c[i].z) + MediaCalle + margen;
                    if (b.max.x > x0 && b.min.x < x1 && b.max.z > z0 && b.min.z < z1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static float Suelo(float x, float z)
        {
            float y = YPatio;
            RaycastHit h;
            if (fisica.Raycast(new Vector3(x, 4f, z), Vector3.down, out h, 8f, ~0, QueryTriggerInteraction.Ignore) && h.point.y > -1.5f && h.point.y < 0.5f)
            {
                y = h.point.y;
            }
            foreach (Bounds t in tramos)
            {
                if (x >= t.min.x && x <= t.max.x && z >= t.min.z && z <= t.max.z)
                {
                    y = Mathf.Max(y, t.max.y);
                }
            }
            return y;
        }

        private static Transform Grupo(string nombre)
        {
            Transform t = raiz.Find(nombre);
            if (t != null)
            {
                return t;
            }
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(raiz, false);
            return g.transform;
        }

        // ================================================================ Prefabs

        private static GameObject Buscar(string codigo)
        {
            GameObject p;
            if (prefabs.TryGetValue(codigo, out p))
            {
                return p;
            }
            p = AssetDatabase.FindAssets(codigo + " t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(ruta => { string n = Path.GetFileNameWithoutExtension(ruta); return n == codigo || n.StartsWith(codigo + "_"); })
                .OrderBy(ruta => Path.GetFileNameWithoutExtension(ruta) == codigo ? 0 : 1)
                .ThenBy(ruta => ruta.StartsWith("Assets/Arte/Zonas/") == (codigo != "PT01_Contenedor40") ? 0 : 1)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
            prefabs[codigo] = p;
            if (p == null)
            {
                faltan.Add(codigo);
            }
            return p;
        }

        private static Bounds Medir(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.one);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>Pone la pieza con la base centrada en <paramref name="pie"/> (y = piso). <paramref name="largo"/> escala el lado más largo en planta; <paramref name="alto"/> el alto. Null si falta el modelo.</summary>
        private static GameObject Colocar(Transform padre, string codigo, Vector3 pie, float yaw, float largo, float alto, bool largoEnX)
        {
            GameObject p = Buscar(codigo);
            if (p == null)
            {
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(p, padre);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Bounds b = Medir(go);
            if (largoEnX && b.size.z > b.size.x)
            {
                yaw += 90f;
            }
            float f = 1f;
            if (largo > 0f)
            {
                f = largo / Mathf.Max(b.size.x, b.size.z);
            }
            else if (alto > 0f)
            {
                f = alto / Mathf.Max(0.01f, b.size.y);
            }
            go.transform.localScale = Vector3.one * f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            b = Medir(go);
            go.transform.position += new Vector3(pie.x - b.center.x, pie.y - b.min.y, pie.z - b.center.z);
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }
            colocados++;
            return go;
        }

        /// <summary>Como Colocar, pero sobre el piso real y solo si queda libre (sin pisar obstáculos ni el arroyo ni el área de K15); si no, lo borra y devuelve null.</summary>
        private static GameObject Poner(Transform padre, string codigo, Vector3 pie, float yaw, float largo, float alto)
        {
            pie.y = Suelo(pie.x, pie.z);
            GameObject go = Colocar(padre, codigo, pie, yaw, largo, alto, false);
            if (go == null)
            {
                return null;
            }
            Bounds b = Medir(go);
            if (CercaK15(b.center) || EnCalle(b, 0.2f) || !Libre(b, 0.15f))
            {
                Object.DestroyImmediate(go);
                return null;
            }
            ocupados.Add(b);
            return go;
        }

        /// <summary>Como Medir pero sin los conos de halo (son luz, no volumen).</summary>
        private static Bounds MedirSinHalo(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true).Where(r => r.name != "Halo").ToArray();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.one);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static List<Vector3> Vertices(Transform t)
        {
            List<Vector3> l = new List<Vector3>();
            foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(false))
            {
                Mesh m = mf.sharedMesh;
                Renderer r = mf.GetComponent<Renderer>();
                if (m == null || r == null || !r.enabled)
                {
                    continue;
                }
                Matrix4x4 mat = mf.transform.localToWorldMatrix;
                foreach (Vector3 v in m.vertices)
                {
                    l.Add(mat.MultiplyPoint3x4(v));
                }
            }
            return l;
        }

        // ================================================================ Lotes de mallas combinadas

        private static Lote LoteDe(string nombre, Material mat, bool sombras = false)
        {
            Lote l;
            if (!lotes.TryGetValue(nombre, out l))
            {
                l = new Lote { Nombre = nombre, Mat = mat, Sombras = sombras };
                lotes[nombre] = l;
            }
            return l;
        }

        private static void Cuad(Lote l, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int k = l.V.Count;
            l.V.Add(a); l.V.Add(b); l.V.Add(c); l.V.Add(d);
            for (int i = 0; i < 4; i++)
            {
                l.N.Add(normal);
            }
            l.UV.Add(new Vector2(0f, 0f)); l.UV.Add(new Vector2(1f, 0f)); l.UV.Add(new Vector2(1f, 1f)); l.UV.Add(new Vector2(0f, 1f));
            // Unity: el frente es el horario; cross(b - a, c - a) apunta hacia quien lo ve.
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
            {
                l.T.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            else
            {
                l.T.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            }
        }

        private static void Triangulo(Lote l, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            int k = l.V.Count;
            l.V.Add(a); l.V.Add(b); l.V.Add(c);
            for (int i = 0; i < 3; i++)
            {
                l.N.Add(normal);
            }
            l.UV.Add(new Vector2(0f, 0f)); l.UV.Add(new Vector2(1f, 0f)); l.UV.Add(new Vector2(0.5f, 1f));
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
            {
                l.T.AddRange(new[] { k, k + 1, k + 2 });
            }
            else
            {
                l.T.AddRange(new[] { k, k + 2, k + 1 });
            }
        }

        /// <summary>Caja alineada a los ejes con centro y tamaño en mundo.</summary>
        private static void Caja(Lote l, Vector3 c, Vector3 s)
        {
            Vector3[] ejes = { Vector3.right, Vector3.up, Vector3.forward };
            float[] m = { s.x * 0.5f, s.y * 0.5f, s.z * 0.5f };
            for (int i = 0; i < 3; i++)
            {
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    Vector3 n = ejes[i] * sg;
                    int j = (i + 1) % 3, k = (i + 2) % 3;
                    Vector3 o = c + n * m[i];
                    Vector3 u = ejes[j] * m[j], v = ejes[k] * m[k];
                    Cuad(l, o - u - v, o + u - v, o + u + v, o - u + v, n);
                }
            }
        }

        /// <summary>Tira plana horizontal de <paramref name="largo"/> a lo largo de <paramref name="dir"/> (en planta).</summary>
        private static void Tira(Lote l, Vector3 centro, Vector3 dir, float largo, float ancho, float y)
        {
            dir.y = 0f;
            dir.Normalize();
            Vector3 r = new Vector3(dir.z, 0f, -dir.x);
            Vector3 c = new Vector3(centro.x, y, centro.z);
            Cuad(l, c - dir * largo * 0.5f - r * ancho * 0.5f, c - dir * largo * 0.5f + r * ancho * 0.5f, c + dir * largo * 0.5f + r * ancho * 0.5f, c + dir * largo * 0.5f - r * ancho * 0.5f, Vector3.up);
        }

        private static void ConstruirLotes()
        {
            Transform padre = Grupo("Lotes");
            foreach (Lote l in lotes.Values)
            {
                if (l.V.Count == 0)
                {
                    continue;
                }
                Mesh m = new Mesh { name = "T1443_" + l.Nombre, indexFormat = IndexFormat.UInt32 };
                m.SetVertices(l.V.Select(v => raiz.InverseTransformPoint(v)).ToList());
                m.SetNormals(l.N);
                m.SetUVs(0, l.UV);
                m.SetTriangles(l.T, 0);
                m.RecalculateBounds();
                obra.Mallas.Add(m);
                GameObject go = new GameObject(l.Nombre);
                go.transform.SetParent(padre, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = l.Mat;
                mr.shadowCastingMode = l.Sombras ? ShadowCastingMode.On : ShadowCastingMode.Off;
                mr.receiveShadows = l.Sombras;
                Log("Lote " + l.Nombre + ": " + (l.V.Count / 4) + " caras aprox., " + (l.T.Count / 3) + " triángulos.");
            }
        }

        // ================================================================ 1. Contenedores de colores

        private static void Contenedores()
        {
            int n = 0, existentesPU08 = 0, saltados = 0;
            HashSet<Transform> vistos = new HashSet<Transform>();
            foreach (Transform t in zona.GetComponentsInChildren<Transform>(false))
            {
                if (t.IsChildOf(raiz))
                {
                    continue;
                }
                if (t.name.StartsWith("PU08") && t.GetComponentInChildren<Renderer>() != null)
                {
                    existentesPU08++;
                }
                if (!t.name.StartsWith("PT01_Contenedor") || t.name.Contains("Reemplazado") || EsDeGrafiti(t) || vistos.Any(v => t.IsChildOf(v)))
                {
                    continue;
                }
                vistos.Add(t);
                Renderer[] rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0)
                {
                    continue;
                }
                if (CercaK15(Medir(t.gameObject).center))
                {
                    saltados++;
                    continue;
                }
                Tintar(t.gameObject, ColorAlAzar());
                n++;
            }
            Log("Contenedores con color nuevo: " + n + " (" + saltados + " dentro de 85 m de las cámaras K15 se dejan a T-1442). PU08 con grafiti que ya había: " + existentesPU08 + ".");
        }

        private static bool EsDeGrafiti(Transform t)
        {
            for (Transform p = t; p != null && p != zona.transform; p = p.parent)
            {
                if (p.name.StartsWith("PU08") || p.name.Contains("Grafiti") || p.name.Contains("Reemplazado"))
                {
                    return true;
                }
            }
            return false;
        }

        // ================================================================ 5. Bodegas y detalles

        private static void Bodegas()
        {
            Transform g = Grupo("Bodegas");
            Transform d = Grupo("Detalles");
            if (!hayCamara || sitiosBodega.Count == 0)
            {
                Log("Bodegas: no hay cámara ni sitios elegidos; 0 bodegas nuevas.");
                return;
            }
            string[] codigos = { "PU03_Bodega", "PU04_Bodega" };
            Lote ventilador = LoteDe("Ventiladores", Opaco("Ventilador", new Color(0.3f, 0.32f, 0.36f), 0.4f, 0.5f));
            Lote tapa = LoteDe("VentiladoresTapa", Opaco("VentiladorTapa", new Color(0.5f, 0.52f, 0.55f), 0.35f, 0.5f));
            Lote magenta = LoteDe("NeonMagenta", Emisivo("NeonMagenta", new Color(0.3f, 0.02f, 0.2f), new Color(7f, 0.6f, 4.5f)));
            Lote turquesa = LoteDe("NeonTurquesa", Emisivo("NeonTurquesa", new Color(0.02f, 0.25f, 0.22f), new Color(0.5f, 6.5f, 6f)));
            Lote lampara = LoteDe("LamparasBodega", Emisivo("LamparaBodega", new Color(0.3f, 0.2f, 0.08f), new Color(9f, 4.5f, 1.4f)));
            int puestas = 0;
            foreach (Vector3 s in sitiosBodega)
            {
                Vector3 haciaCam = camPos - s;
                haciaCam.y = 0f;
                haciaCam.Normalize();
                int mejorQ = 0;
                float mejorD = -2f;
                for (int q = 0; q < 4; q++)
                {
                    Vector3 puerta = q == 0 ? Vector3.forward : q == 1 ? Vector3.right : q == 2 ? Vector3.back : Vector3.left;
                    float dp = Vector3.Dot(puerta, haciaCam);
                    if (dp > mejorD)
                    {
                        mejorD = dp;
                        mejorQ = q;
                    }
                }
                float yaw = mejorQ * 90f;
                string cod = codigos[puestas % 2];
                GameObject go = Colocar(g, cod, new Vector3(s.x, YPatio, s.z), yaw, 22f, 0f, false);
                if (go == null)
                {
                    Log("Bodegas: falta " + cod + ".");
                    continue;
                }
                Bounds b = Medir(go);
                if (b.size.x > SitioX - 1f || b.size.z > SitioZ - 1f)
                {
                    float f = Mathf.Min((SitioX - 1f) / b.size.x, (SitioZ - 1f) / b.size.z);
                    go.transform.localScale *= f;
                    b = Medir(go);
                    go.transform.position += new Vector3(s.x - b.center.x, YPatio - b.min.y, s.z - b.center.z);
                    b = Medir(go);
                }
                puestas++;
                go.name = cod + "_T1443_" + puestas;
                Log("Bodega " + go.name + " en el sitio " + s.ToString("F1") + ": x " + b.min.x.ToString("F1") + ".." + b.max.x.ToString("F1") + ", z " + b.min.z.ToString("F1") + ".." + b.max.z.ToString("F1") + ", alto " + b.size.y.ToString("F1") + " m, puerta mirando a la cámara (yaw " + yaw + ", coseno " + mejorD.ToString("F2") + ").");
                DetallesBodega(go, d, yaw, ventilador, tapa, magenta, turquesa, lampara);
                ocupados.Add(b);
                bodegasNuevas.Add(b);
            }
            Log("Bodegas nuevas: " + puestas + " (en manzanas apartadas, abajo a la derecha de la cámara A15).");
        }


        private static readonly List<Bounds> bodegasNuevas = new List<Bounds>();
        private static readonly Dictionary<string, int> razones = new Dictionary<string, int>();

        private static void Razon(string r)
        {
            int n;
            razones.TryGetValue(r, out n);
            razones[r] = n + 1;
        }

        private static void DetallesBodega(GameObject go, Transform d, float yaw, Lote ventilador, Lote tapa, Lote magenta, Lote turquesa, Lote lampara)
        {
            Bounds b = Medir(go);
            List<Vector3> vs = Vertices(go.transform);
            Quaternion giro = Quaternion.Euler(0f, yaw, 0f);
            Vector3 puerta = giro * Vector3.forward; // yaw 0: puerta a +z; 90: a +x (como en T-1357)
            Vector3 lado = Vector3.Cross(Vector3.up, puerta);
            bool ejeX = Mathf.Abs(puerta.x) > 0.5f;
            float pared = ejeX ? b.size.x * 0.5f : b.size.z * 0.5f;
            IEnumerable<Vector3> paredes = vs.Where(v => v.y > 0.5f && v.y < 3f);
            if (paredes.Any())
            {
                pared = paredes.Max(v => Vector3.Dot(v - b.center, puerta));
            }
            float mitadLargo = ejeX ? b.size.z * 0.5f : b.size.x * 0.5f;
            for (int k = -1; k <= 1; k++)
            {
                Vector3 p = b.center + lado * (k * mitadLargo * 0.5f);
                IEnumerable<Vector3> cerca = vs.Where(v => Mathf.Abs(v.x - p.x) < 2f && Mathf.Abs(v.z - p.z) < 2f);
                if (!cerca.Any())
                {
                    continue;
                }
                float yt = cerca.Max(v => v.y);
                Caja(ventilador, new Vector3(p.x, yt + 0.3f, p.z), new Vector3(1.3f, 0.9f, 1.3f));
                Caja(tapa, new Vector3(p.x, yt + 0.88f, p.z), new Vector3(1.8f, 0.18f, 1.8f));
            }
            Vector3 cara = b.center + puerta * (pared + 0.12f);
            Vector3 grosor = ejeX ? new Vector3(0.14f, 1f, 0.4f) : new Vector3(0.4f, 1f, 0.14f);
            Caja(magenta, new Vector3(cara.x, 4.6f, cara.z) + lado * (mitadLargo * 0.55f), new Vector3(grosor.x, 1.8f, grosor.z));
            Caja(turquesa, new Vector3(cara.x, 3.4f, cara.z) - lado * (mitadLargo * 0.6f), new Vector3(grosor.x, 1.3f, grosor.z));
            Vector3 gl = ejeX ? new Vector3(0.5f, 0.25f, 0.3f) : new Vector3(0.3f, 0.25f, 0.5f);
            Caja(lampara, new Vector3(cara.x, 5.6f, cara.z) + lado * 4f + puerta * 0.13f, gl);
            Caja(lampara, new Vector3(cara.x, 5.6f, cara.z) - lado * 4f + puerta * 0.13f, gl);
            Vector3 frente = b.center + puerta * (pared + 1.8f);
            float ry = Mathf.Atan2(puerta.x, puerta.z) * Mathf.Rad2Deg;
            Poner(d, "PU06_Pallets", frente - lado * 6f, ry + obra.Rango(-20f, 20f), 0f, 1.2f);
            Poner(d, "PU06_Pallets", frente + lado * 7f, ry + obra.Rango(-20f, 20f), 0f, 1.0f);
            Poner(d, "C17_Huacales", frente + lado * 1.5f, obra.Rango(0f, 360f), 0f, 1.0f);
            Poner(d, "C17_Huacales", frente - lado * 2.5f + puerta * 0.4f, obra.Rango(0f, 360f), 0f, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                Poner(d, "C16_Tambo", frente + lado * (9f + i) + puerta * (1.4f + i * 0.2f), obra.Rango(0f, 360f), 0f, 1.0f);
            }
            Poner(d, "PU05_Montacargas", frente - lado * 10f + puerta * 2.8f, ry, 0f, 2.6f);
        }

        /// <summary>
        /// Ronda 4: las manzanas visibles desde la cámara A15 se vuelven bloques compactos escalonados de 1, 2, 3 y 3 o 4 contenedores de alto (fila 0 a 1 para no tocar la escalera del suelo de T-1357):
        /// rellena cada celda (2 columnas por 4 filas) con PT01_Contenedor40 de la paleta, apaga (reversible, sufijo) las escaleras de fila viejas de la manzana y pone escaleras PuntoTrepar nuevas de 2.59 m
        /// (una por escalón de 1 contenedor, en la columna oeste, como T-1357) y colliders para la altura añadida. Cada manzana lleva su LODGroup.
        /// </summary>
        private static void Rellenar()
        {
            if (!hayCamara)
            {
                Log("Relleno: sin cámara; no se rellena.");
                return;
            }
            Transform g = Grupo("Relleno");
            int idx = 0, bloques = 0, anadidos = 0, escaleras = 0, ocultas = 0;
            foreach (ColocarT1357Puerto.Bloque bl in ColocarT1357Puerto.Bloques())
            {
                idx++;
                Vector3 cb = new Vector3(bl.Centro.x, 1f, bl.Centro.y);
                float u, v, prof;
                if (CercaK15(cb) || !Proyectar(cb, out u, out v, out prof) || u < -0.2f || u > 1.2f || v < -0.2f || v > 1.2f || prof > 240f)
                {
                    continue;
                }
                if (Libre(new Bounds(cb, new Vector3(4f, 0.5f, 4f)), 0f) || bodegasNuevas.Any(bb => new Bounds(cb, new Vector3(ColocarT1357Puerto.LargoBloque + 2f, 4f, ColocarT1357Puerto.FondoBloque + 2f)).Intersects(bb)))
                {
                    continue; // manzana apartada o con una bodega nueva encima
                }
                int[] meta = { 1, 2, 3, obra.Chance(0.7f) ? 4 : 3 };
                GameObject pila = new GameObject("Relleno_Bloque_" + idx);
                pila.transform.SetParent(g, false);
                Transform bloqueGo = zona.transform.Find("Contenedores/Bloque_" + idx);
                if (bloqueGo != null)
                {
                    foreach (Transform t in bloqueGo.Cast<Transform>().ToList())
                    {
                        if (t.name.StartsWith("Escalera_Fila") && t.gameObject.activeSelf)
                        {
                            t.name += SufijoApartado;
                            t.gameObject.SetActive(false);
                            ocultas++;
                        }
                    }
                }
                for (int f = 0; f < ColocarT1357Puerto.Filas; f++)
                {
                    float z = bl.ZFila(f);
                    for (int c = 0; c < 2; c++)
                    {
                        float x = bl.Centro.x + (c == 0 ? -1f : 1f) * LargoCont * 0.5f;
                        float top = 0f;
                        foreach (Bounds cbnd in contBounds)
                        {
                            if (Mathf.Abs(cbnd.center.x - x) < 3f && Mathf.Abs(cbnd.center.z - z) < 1.3f)
                            {
                                top = Mathf.Max(top, cbnd.max.y);
                            }
                        }
                        int tiers = Mathf.RoundToInt(top / AltoCont);
                        if (tiers == 0 && !Libre(new Bounds(new Vector3(x, AltoCont * 0.5f, z), new Vector3(LargoCont, AltoCont, AnchoCont)), 0.1f))
                        {
                            continue;
                        }
                        int objetivo = meta[f];
                        float baseY = tiers == 0 ? 0f : top;
                        for (int k = Mathf.Max(tiers, 0); k < objetivo; k++)
                        {
                            float y = baseY + (k - tiers) * AltoCont;
                            GameObject cont = Colocar(pila.transform, "PT01_Contenedor40", new Vector3(x, y, z), obra.Chance(0.15f) ? 180f : 0f, LargoCont, 0f, true);
                            if (cont == null)
                            {
                                return;
                            }
                            Tintar(cont, ColorAlAzar());
                            anadidos++;
                        }
                        if (objetivo > tiers)
                        {
                            float alto = (objetivo - tiers) * AltoCont;
                            BoxCollider bc = pila.AddComponent<BoxCollider>();
                            bc.center = new Vector3(x, baseY + alto * 0.5f, z);
                            bc.size = new Vector3(LargoCont, alto, AnchoCont);
                        }
                    }
                }
                for (int f = 0; f < ColocarT1357Puerto.Filas - 1; f++)
                {
                    if (meta[f + 1] > meta[f])
                    {
                        EscaleraNueva(pila.transform, "Escalera_Fila" + f + "_T1443", new Vector3(bl.Centro.x - LargoCont * 0.5f, meta[f] * AltoCont, bl.ZFila(f) + ColocarT1357Puerto.PasoFila * 0.5f - 0.1f), AltoCont);
                        escaleras++;
                    }
                }
                Renderer[] rs = pila.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    LODGroup lg = pila.AddComponent<LODGroup>();
                    lg.SetLODs(new[] { new LOD(0.012f, rs) });
                    lg.RecalculateBounds();
                    bloques++;
                }
            }
            Log("Relleno de manzanas: " + anadidos + " contenedores nuevos en " + bloques + " manzanas visibles (metas 1, 2, 3, 3 o 4 de alto); " + escaleras + " escaleras PuntoTrepar nuevas de 2.59 m y " + ocultas + " escaleras de fila viejas apagadas (sufijo " + SufijoApartado + ").");
        }

        private static void EscaleraNueva(Transform padre, string nombre, Vector3 pie, float alto)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(pie, Quaternion.LookRotation(Vector3.forward));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
            so.FindProperty("alturaSubida").floatValue = alto;
            so.FindProperty("salida").vector3Value = new Vector3(0f, alto, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
        }

        /// <summary>Montacargas, pallets y huacales estacionados en la orilla de las calles internas visibles (a 7.4 m del eje, fuera del asfalto).</summary>
        private static void Carriles()
        {
            if (!hayCamara)
            {
                return;
            }
            Transform d = Grupo("Detalles");
            string[] cods = { "PU05_Montacargas", "PU06_Pallets", "C17_Huacales", "PU06_Pallets", "C16_Tambo" };
            float[] altos = { 2.6f, 1.2f, 1.0f, 1.0f, 1.0f };
            int n = 0, puestos = 0;
            foreach (Vector3[] calle in ColocarT1357Puerto.Calles())
            {
                for (int i = 1; i < calle.Length; i++)
                {
                    Vector3 dir = calle[i] - calle[i - 1];
                    float largo = dir.magnitude;
                    dir /= largo;
                    Vector3 der = Vector3.Cross(Vector3.up, dir);
                    float ry = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    for (float t = 12f; t < largo - 12f; t += 9f)
                    {
                        n++;
                        Vector3 pie = calle[i - 1] + dir * t + der * (n % 2 == 0 ? 7.4f : -7.4f);
                        float u, v, prof;
                        if (CercaK15(pie) || !Proyectar(pie, out u, out v, out prof) || u < 0f || u > 1f || v < 0f || v > 1f || prof > 220f)
                        {
                            continue;
                        }
                        int k = n % cods.Length;
                        pie.y = Suelo(pie.x, pie.z);
                        GameObject go = Colocar(d, cods[k], pie, ry + (k == 0 ? 0f : obra.Rango(-25f, 25f)), 0f, altos[k], false);
                        if (go == null)
                        {
                            continue;
                        }
                        Bounds b = Medir(go);
                        float lateral = Mathf.Abs(Vector3.Dot(b.center - calle[i - 1], der));
                        if (!Libre(b, 0.15f) || EnCalle(b, 0.05f))
                        {
                            Object.DestroyImmediate(go);
                            continue;
                        }
                        ocupados.Add(b);
                        puestos++;
                    }
                }
            }
            Log("Carriles: " + puestos + " montacargas, pallets, huacales y tambos estacionados fuera del carril (a 7.4 m del eje).");
        }


        private const float LargoBloque2 = ColocarT1357Puerto.LargoBloque;

        private static void CasetaYReja()
        {
            Transform d = Grupo("Detalles");
            bool caseta = false;
            // Primero junto a la primera bodega nueva (en un anillo a su alrededor).
            if (bodegasNuevas.Count > 0)
            {
                Bounds bb = bodegasNuevas[0];
                for (int k = 0; k < 16 && !caseta; k++)
                {
                    float ang = k * 22.5f * Mathf.Deg2Rad;
                    Vector3 p = bb.center + new Vector3(Mathf.Sin(ang) * (bb.extents.x + 5f), 0f, Mathf.Cos(ang) * (bb.extents.z + 5f));
                    foreach (float yaw in new[] { 0f, 90f })
                    {
                        GameObject c = Poner(d, "PU07_PlumaCaseta_a", p, yaw, 0f, 3.2f);
                        if (c != null)
                        {
                            c.name = "PU07_PlumaCaseta_T1443";
                            caseta = true;
                            Log("Caseta PU07 con pluma junto a la bodega nueva, en " + p.ToString("F1") + ".");
                            break;
                        }
                    }
                }
            }
            for (float z = -112f; z >= -205f && !caseta; z -= 5f)
            {
                foreach (float yaw in new[] { 0f, 90f })
                {
                    GameObject c = Poner(d, "PU07_PlumaCaseta_a", new Vector3(311.5f, YPatio, z), yaw, 0f, 3.2f);
                    if (c == null)
                    {
                        continue;
                    }
                    Bounds b = Medir(c);
                    if (b.max.x > 317.5f)
                    {
                        ocupados.Remove(b);
                        Object.DestroyImmediate(c);
                        continue;
                    }
                    c.name = "PU07_PlumaCaseta_T1443";
                    caseta = true;
                    break;
                }
            }
            int piezas = 0;
            for (float z = -100f; z >= -210f && piezas < 14; z -= 4f)
            {
                GameObject r = Poner(d, "DS01_RejaPuas", new Vector3(316.8f, YPatio, z), 90f, 4f, 0f);
                if (r != null)
                {
                    piezas++;
                }
            }
            Log("Caseta con pluma: " + (caseta ? "sí" : "no (no cupo en x 306..318)") + ". Piezas de reja: " + piezas + ".");
        }

        // ================================================================ 3. Postes y luz

        private static void Postes()
        {
            Transform g = Grupo("Postes");
            List<Vector3[]> candidatos = new List<Vector3[]>(); // pie, haciaCalle
            List<Vector3[]> calles = ColocarT1357Puerto.Calles();
            int paso = 0;
            foreach (Vector3[] c in calles)
            {
                for (int i = 1; i < c.Length; i++)
                {
                    Vector3 dir = (c[i] - c[i - 1]);
                    float largo = dir.magnitude;
                    dir /= largo;
                    Vector3 der = Vector3.Cross(Vector3.up, dir);
                    for (float t = 16f; t < largo - 14f; t += 24f)
                    {
                        paso++;
                        float lado = paso % 2 == 0 ? 7f : -7f;
                        Vector3 pie = c[i - 1] + dir * t + der * lado;
                        candidatos.Add(new[] { pie, -der * Mathf.Sign(lado) });
                    }
                }
            }
            foreach (Vector3 esq in new[] { new Vector3(300f, 0f, -80f), new Vector3(300f, 0f, -150f), new Vector3(300f, 0f, -215f), new Vector3(300f, 0f, -232f) })
            {
                foreach (Vector2 s in new[] { new Vector2(7.5f, 7.5f), new Vector2(-7.5f, 7.5f), new Vector2(7.5f, -7.5f), new Vector2(-7.5f, -7.5f) })
                {
                    Vector3 pie = esq + new Vector3(s.x, 0f, s.y);
                    candidatos.Add(new[] { pie, new Vector3(-s.x, 0f, -s.y).normalized });
                }
            }
            List<Vector3[]> buenos = new List<Vector3[]>();
            foreach (Vector3[] c in candidatos)
            {
                Vector3 pie = c[0];
                Bounds b = new Bounds(new Vector3(pie.x, 3f, pie.z), new Vector3(1f, 6f, 1f));
                if (CercaK15(pie) || pie.x < 243f || pie.x > 316f || pie.z > -60f || pie.z < -236f || EnCalle(b, 0.4f) || !Libre(b, 0.5f))
                {
                    continue;
                }
                buenos.Add(c);
            }
            buenos = buenos.OrderBy(c => Vector2.Distance(new Vector2(c[0].x, c[0].z), new Vector2(MiraA15.x, MiraA15.z))).ToList();
            Lote cabezas = LoteDe("CabezasPoste", Emisivo("CabezaPoste", new Color(0.4f, 0.22f, 0.08f), new Color(12f, 5.5f, 1.4f)));
            Lote poza = LoteDe("PozasLuz", MatPoza());
            int conLuz = 0, sinLuz = 0;
            List<Vector3> ocupadosPostes = new List<Vector3>();
            Dictionary<string, int> usados = new Dictionary<string, int>();
            foreach (Vector3[] c in buenos)
            {
                if (conLuz + sinLuz >= 16)
                {
                    break;
                }
                Vector3 pie = c[0];
                pie.y = Suelo(pie.x, pie.z);
                Vector3 hacia = c[1];
                Bounds cuerpo = new Bounds(pie + Vector3.up * 3f, new Vector3(1f, 6f, 1f));
                if (!Libre(cuerpo, 0.5f) || ocupadosPostes.Any(q => Vector3.Distance(q, pie) < 9f))
                {
                    continue; // ya hay otro poste cerca (los dos de una esquina se encimaban)
                }
                ocupadosPostes.Add(pie);
                if (conLuz < TopeLuces)
                {
                    KitS2c.Poste(obra, g, pie, hacia, pie.y, false);
                    conLuz++;
                }
                else
                {
                    PosteSinLuz(g, pie, hacia, cabezas);
                    sinLuz++;
                }
                ocupados.Add(cuerpo);
                Vector3 centro = pie + new Vector3(hacia.x, 0f, hacia.z).normalized * 3.2f;
                float yp = Mathf.Max(pie.y, 0.08f) + 0.06f;
                Tira(poza, centro, Vector3.right, 9f, 9f, yp);
            }
            Log("Postes de sodio: " + conLuz + " con Light real (tope " + TopeLuces + ") y " + sinLuz + " sin luz real (cabeza emisiva + halo + charco de luz aditivo).");
        }

        private static void PosteSinLuz(Transform padre, Vector3 pie, Vector3 haciaCalle, Lote cabezas)
        {
            haciaCalle.y = 0f;
            haciaCalle.Normalize();
            obra.Postes++;
            GameObject poste = KitS2c.Pieza(padre, "C07_PosteLuz", pie, Quaternion.identity, Vector3.one);
            poste.name = "PosteEmisivo_" + obra.Postes;
            MeshFilter mf = poste.GetComponentsInChildren<MeshFilter>().OrderByDescending(m => m.sharedMesh.vertexCount).First();
            Vector3 cabeza;
            if (ColocarS2.CabezaLampara(mf, out cabeza))
            {
                Vector3 brazo = cabeza - pie;
                brazo.y = 0f;
                float giro = Vector3.SignedAngle(brazo, haciaCalle, Vector3.up);
                poste.transform.rotation = Quaternion.Euler(0f, giro, 0f);
                ColocarS2.CabezaLampara(mf, out cabeza);
            }
            else
            {
                cabeza = pie + Vector3.up * 6.6f + haciaCalle * 1.2f;
            }
            foreach (Renderer r in poste.GetComponentsInChildren<Renderer>())
            {
                r.renderingLayerMask = 1u << ColocarS2.CapaPostes;
            }
            Caja(cabezas, cabeza + Vector3.up * 0.06f, new Vector3(0.9f, 0.14f, 0.45f));
            KitS2c.Halo(obra, poste.transform, cabeza, pie.y, KitS2c.Sodio);
        }

        // ================================================================ 4. Grúas

        private static void Gruas()
        {
            Transform muelle = zona.transform.Find("Muelle");
            if (muelle == null)
            {
                Log("Grúas: no hay Muelle.");
                return;
            }
            Lote rojas = LoteDe("BalizasRojas", Emisivo("BalizaRoja", new Color(0.3f, 0.02f, 0.02f), new Color(14f, 0.4f, 0.4f)));
            Lote verdes = LoteDe("BalizasVerdes", Emisivo("BalizaVerde", new Color(0.02f, 0.3f, 0.12f), new Color(0.5f, 11f, 4f)));
            int n = 0, rojasN = 0, verdesN = 0;
            foreach (Transform g in muelle.Cast<Transform>().Where(t => t.name.StartsWith("PU01_Grua") && t.gameObject.activeInHierarchy).ToList())
            {
                n++;
                List<Vector3> vs = Vertices(g);
                if (vs.Count == 0)
                {
                    continue;
                }
                Aclarar(g.gameObject);
                float minY = vs.Min(v => v.y), maxY = vs.Max(v => v.y), alto = maxY - minY;
                // Patas: los vértices más bajos deben apoyar en el muelle (tolerancia 5 cm).
                List<Vector3> pies = vs.Where(v => v.y < minY + 0.3f).ToList();
                float peor = 0f;
                foreach (Vector3 p in pies.Take(40))
                {
                    peor = Mathf.Max(peor, Mathf.Abs(p.y - Suelo(p.x, p.z)));
                }
                // Muestras de las patas (una por cuadro de 0.5 m) y búsqueda del ajuste (dy, dz de hasta 3 m) con el peor error mínimo contra el piso.
                List<Vector3> muestras = pies.GroupBy(p => new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.z * 2f))).Select(gp => gp.First()).Take(40).ToList();
                float mejorCosto = float.MaxValue, mejorDy = 0f, mejorDz = 0f;
                for (float dz = -3f; dz <= 3.01f; dz += 0.5f)
                {
                    List<float> suelos = muestras.Select(m => Suelo(m.x, m.z + dz)).ToList();
                    foreach (float s0 in suelos.Distinct().ToList())
                    {
                        float dy = s0 - minY;
                        float costo = 0f;
                        for (int i = 0; i < muestras.Count; i++)
                        {
                            costo = Mathf.Max(costo, Mathf.Abs(muestras[i].y + dy - suelos[i]));
                        }
                        costo += 0.01f * (Mathf.Abs(dz) + Mathf.Abs(dy));
                        if (costo < mejorCosto)
                        {
                            mejorCosto = costo; mejorDy = dy; mejorDz = dz;
                        }
                    }
                }
                // Regla de la ronda 2: el piso del muelle es la mediana del piso bajo las patas; la base de la grúa sube o baja hasta ahí (dy), sin mover en z.
                List<float> pisos = muestras.Select(m => Suelo(m.x, m.z)).OrderBy(s => s).ToList();
                float mediana = pisos[pisos.Count / 2];
                Log("Grúa " + g.name + ": piso bajo las patas min " + pisos[0].ToString("F2") + " / mediana " + mediana.ToString("F2") + " / max " + pisos[pisos.Count - 1].ToString("F2") + "; base de la grúa y " + minY.ToString("F2") + ".");
                float dyMed = mediana - minY;
                if (Mathf.Abs(dyMed) > 0.05f && Mathf.Abs(dyMed) < 3f)
                {
                    mejorDy = dyMed;
                    mejorDz = 0f;
                    mejorCosto = -1f;
                }
                if (mejorCosto + 0.01f < peor && Mathf.Abs(mejorDy) < 3f)
                {
                    g.position += new Vector3(0f, mejorDy, mejorDz);
                    Physics.SyncTransforms();
                    vs = Vertices(g);
                    minY += mejorDy;
                    maxY += mejorDy;
                    Log("Grúa " + g.name + ": movida dy " + mejorDy.ToString("F2") + " m (a y = piso) y dz " + mejorDz.ToString("F1") + " m; peor apoyo " + peor.ToString("F2") + " -> " + mejorCosto.ToString("F2") + " m.");
                }
                else if (peor > 0.05f)
                {
                    Log("AVISO grúa " + g.name + ": peor apoyo " + peor.ToString("F2") + " m y ningún ajuste de ±3 m lo mejora (mejor " + mejorCosto.ToString("F2") + ").");
                }
                List<Vector3> puntos = new List<Vector3>();
                System.Action<Vector3, bool> agregar = (p, verde) =>
                {
                    if (puntos.All(q => Vector3.Distance(p, q) > 2.5f))
                    {
                        puntos.Add(p);
                        Caja(verde ? verdes : rojas, p + Vector3.up * 0.25f, new Vector3(0.55f, 0.55f, 0.55f));
                        if (verde) verdesN++; else rojasN++;
                    }
                };
                List<Vector3> altos = vs.Where(v => v.y > minY + alto * 0.7f).ToList();
                if (altos.Count == 0) altos = vs;
                foreach (Vector3 vv in altos.OrderByDescending(a => a.y).Take(600))
                {
                    if (puntos.Count >= 7) break;
                    agregar(vv, false);
                }
                agregar(altos.OrderBy(v => v.x).First(), false);
                agregar(altos.OrderByDescending(v => v.x).First(), false);
                agregar(altos.OrderBy(v => v.z).First(), false);
                agregar(altos.OrderByDescending(v => v.z).First(), false);
                List<Vector3> medios = vs.Where(v => v.y > minY + alto * 0.35f && v.y < minY + alto * 0.7f).ToList();
                if (medios.Count > 0)
                {
                    agregar(medios.OrderBy(v => v.x).First(), false);
                    agregar(medios.OrderByDescending(v => v.x).First(), false);
                    agregar(medios.OrderBy(v => v.z).First(), true);
                    agregar(medios.OrderByDescending(v => v.z).First(), true);
                    agregar(medios.OrderByDescending(v => v.y).First(), true);
                }
                agregar(altos.OrderBy(v => v.x + v.z).First(), true);
                agregar(altos.OrderByDescending(v => v.x + v.z).First(), true);
                Log("Grúa " + g.name + ": alto " + alto.ToString("F1") + " m, base y " + minY.ToString("F2") + ", peor apoyo " + peor.ToString("F2") + " m; balizas totales sumadas " + puntos.Count + ".");
            }
            Log("Grúas: " + n + "; balizas nuevas " + rojasN + " rojas y " + verdesN + " verdes (sobre vértices de la malla; las de T-1396 se quedan).");
        }

        // ================================================================ 4b. Barco

        private static void Barco()
        {
            Transform muelle = zona.transform.Find("Muelle");
            Transform barco = muelle == null ? null : muelle.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("PU02_Barco") && t.gameObject.activeInHierarchy);
            if (barco == null)
            {
                Log("Barco: no hay PU02_Barco activo en el Muelle.");
                return;
            }
            List<Vector3> vs = Vertices(barco);
            Bounds b = Medir(barco.gameObject);
            if (vs.Count < 50 || b.size.x < b.size.z)
            {
                Log("Barco: sin vértices legibles o eje largo en z; no se agrega carga.");
                return;
            }
            const float bin = 4f;
            int nb = Mathf.CeilToInt(b.size.x / bin);
            float[] maxY = Enumerable.Repeat(float.NegativeInfinity, nb).ToArray();
            foreach (Vector3 v in vs)
            {
                if (Mathf.Abs(v.z - b.center.z) < b.size.z * 0.3f)
                {
                    int i = Mathf.Clamp((int)((v.x - b.min.x) / bin), 0, nb - 1);
                    maxY[i] = Mathf.Max(maxY[i], v.y);
                }
            }
            List<float> validas = maxY.Where(y => !float.IsNegativeInfinity(y)).OrderBy(y => y).ToList();
            if (validas.Count < 4)
            {
                Log("Barco: muy pocos datos de cubierta.");
                return;
            }
            float cubierta = validas[(int)(validas.Count * 0.35f)];
            // La cubierta es el plano horizontal con más vértices (franjas de 0.25 m) en la franja central, entre el 10 % y el 75 % del alto del casco.
            Dictionary<int, int> histograma = new Dictionary<int, int>();
            foreach (Vector3 vh in vs)
            {
                if (Mathf.Abs(vh.z - b.center.z) < b.size.z * 0.35f && vh.y > b.min.y + b.size.y * 0.1f && vh.y < b.min.y + b.size.y * 0.75f)
                {
                    int cubo = Mathf.RoundToInt(vh.y * 4f);
                    int cuenta;
                    histograma.TryGetValue(cubo, out cuenta);
                    histograma[cubo] = cuenta + 1;
                }
            }
            if (histograma.Count > 0)
            {
                int mejorCubo = histograma.OrderByDescending(kv => kv.Value).First().Key;
                Log("Barco: cubierta por histograma y = " + (mejorCubo / 4f).ToString("F2") + " (percentil daba " + cubierta.ToString("F2") + ").");
                cubierta = mejorCubo / 4f;
            }
            // Tramo plano más largo de la cubierta.
            int mejorIni = -1, mejorLen = 0, ini = -1;
            for (int i = 0; i <= nb; i++)
            {
                bool plano = i < nb && !float.IsNegativeInfinity(maxY[i]) && Mathf.Abs(maxY[i] - cubierta) < 1.5f;
                if (plano && ini < 0) ini = i;
                if (!plano && ini >= 0)
                {
                    if (i - ini > mejorLen) { mejorLen = i - ini; mejorIni = ini; }
                    ini = -1;
                }
            }
            Log("Barco: perfil de la altura máxima por casilla de 4 m (de x " + b.min.x.ToString("F0") + "): " + string.Join(" ", maxY.Select(y => float.IsNegativeInfinity(y) ? "-" : y.ToString("F1"))));
            Log("Barco: x " + b.min.x.ToString("F0") + ".." + b.max.x.ToString("F0") + ", z " + b.min.z.ToString("F0") + ".." + b.max.z.ToString("F0") + ", y " + b.min.y.ToString("F1") + ".." + b.max.y.ToString("F1") + "; cubierta a y " + cubierta.ToString("F2") + ", tramo plano " + mejorLen + " casillas de 4 m.");
            Transform g = Grupo("Barco");
            if (mejorLen >= 2)
            {
                float x0 = b.min.x + mejorIni * bin + 1.5f, x1 = b.min.x + (mejorIni + mejorLen) * bin - 1.5f;
                IEnumerable<Vector3> borde = vs.Where(v => Mathf.Abs(v.y - cubierta) < 0.8f && v.x > x0 && v.x < x1);
                if (borde.Any())
                {
                    float zmin = borde.Min(v => v.z), zmax = borde.Max(v => v.z);
                    float ancho = zmax - zmin, zc = (zmin + zmax) * 0.5f;
                    int filas = Mathf.Clamp(Mathf.FloorToInt((ancho - 1.2f) / (AnchoCont + 0.12f)), 1, 6);
                    int cont = 0, graf = 0;
                    int col = 0;
                    for (float x = x0 + LargoCont * 0.5f; x + LargoCont * 0.5f <= x1; x += LargoCont + 0.2f)
                    {
                        col++;
                        GameObject pila = new GameObject("Pila_" + col);
                        pila.transform.SetParent(g, false);
                        for (int f = 0; f < filas; f++)
                        {
                            float z = zc + (f - (filas - 1) * 0.5f) * (AnchoCont + 0.12f);
                            bool centro = f > 0 && f < filas - 1;
                            int alto = centro ? obra.Azar.Next(2, 4) : obra.Azar.Next(1, 3);
                            for (int k = 0; k < alto; k++)
                            {
                                bool grafiti = obra.Chance(0.12f);
                                string cod = grafiti ? (obra.Chance(0.5f) ? "PU08_ContenedorGraffiti_a" : "PU08_ContenedorGraffiti_b") : "PT01_Contenedor40";
                                GameObject c = Colocar(pila.transform, cod, new Vector3(x, cubierta + k * AltoCont, z), obra.Chance(0.15f) ? 180f : 0f, LargoCont, 0f, true);
                                if (c == null)
                                {
                                    continue;
                                }
                                if (!grafiti)
                                {
                                    Tintar(c, ColorAlAzar());
                                }
                                else
                                {
                                    graf++;
                                }
                                cont++;
                            }
                        }
                        Renderer[] rs = pila.GetComponentsInChildren<Renderer>();
                        if (rs.Length > 0)
                        {
                            LODGroup lg = pila.AddComponent<LODGroup>();
                            lg.SetLODs(new[] { new LOD(0.01f, rs) });
                            lg.RecalculateBounds();
                        }
                    }
                    Log("Barco: " + cont + " contenedores en cubierta (" + col + " pilas de " + filas + " filas; " + graf + " con grafiti PU08).");
                    // Mástiles con lámpara en el borde norte de la cubierta.
                    Lote mastil = LoteDe("MastilesBarco", Opaco("MastilBarco", new Color(0.18f, 0.2f, 0.24f), 0.3f, 0.6f));
                    Lote lamp = LoteDe("LamparasBarco", Emisivo("LamparaBarco", new Color(0.4f, 0.3f, 0.12f), new Color(12f, 8f, 3.5f)));
                    int masts = 0;
                    for (float x = x0 + 4f; x < x1 - 2f; x += 16f)
                    {
                        Caja(mastil, new Vector3(x, cubierta + 5.2f, zmax - 0.4f), new Vector3(0.18f, 10.4f, 0.18f));
                        Caja(lamp, new Vector3(x, cubierta + 10.5f, zmax - 0.6f), new Vector3(1.4f, 0.25f, 0.7f));
                        masts++;
                    }
                    Log("Barco: " + masts + " mástiles con lámpara emisiva.");
                }
            }
            // Superestructura: la zona más alta; ventanas sobre su cara norte (+z) tomando el vértice real de cada fila.
            int sIni = -1, sLen = 0, si = -1;
            for (int i = 0; i <= nb; i++)
            {
                bool alta = i < nb && !float.IsNegativeInfinity(maxY[i]) && maxY[i] > cubierta + 3f;
                if (alta && si < 0) si = i;
                if (!alta && si >= 0)
                {
                    if (i - si > sLen) { sLen = i - si; sIni = si; }
                    si = -1;
                }
            }
            if (sLen >= 1)
            {
                float s0 = b.min.x + sIni * bin, s1 = s0 + sLen * bin;
                float techo = Enumerable.Range(sIni, sLen).Max(i => maxY[i]);
                Lote ambar = LoteDe("VentanasBarco", Emisivo("VentanaAmbar", new Color(0.35f, 0.25f, 0.08f), new Color(6f, 4f, 1.4f)));
                Lote celeste = LoteDe("VentanasBarcoFrias", Emisivo("VentanaFria", new Color(0.1f, 0.25f, 0.3f), new Color(1.5f, 4.5f, 5.5f)));
                int vn = 0;
                for (float y = cubierta + 1.8f; y < techo - 0.8f && vn < 90; y += 1.7f)
                {
                    for (float x = s0 + 1f; x < s1 - 0.8f && vn < 90; x += 1.7f)
                    {
                        IEnumerable<Vector3> cerca = vs.Where(v => Mathf.Abs(v.x - x) < 1f && Mathf.Abs(v.y - y) < 0.5f);
                        if (!cerca.Any() || obra.Chance(0.25f))
                        {
                            continue;
                        }
                        float zf = cerca.Max(v => v.z);
                        if (zf < b.center.z)
                        {
                            continue;
                        }
                        Caja(obra.Chance(0.75f) ? ambar : celeste, new Vector3(x, y, zf + 0.04f), new Vector3(0.95f, 0.55f, 0.08f));
                        vn++;
                    }
                }
                Log("Barco: superestructura x " + s0.ToString("F0") + ".." + s1.ToString("F0") + " hasta y " + techo.ToString("F1") + "; " + vn + " ventanas encendidas.");
            }
            else
            {
                Log("Barco: no se distingue superestructura sobre la cubierta (+3 m); sin ventanas.");
            }
        }

        // ================================================================ 2. Pintura y charcos

        private static void Pintura()
        {
            Material mat = MatPropio(Prefijo + "Pintura", () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", new Color(0.84f, 0.86f, 0.88f));
                m.SetFloat("_Smoothness", 0.3f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.16f, 0.16f, 0.17f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
            Lote l = LoteDe("Pintura", mat);
            int bordes = 0, flechas = 0, manzanas = 0;
            // Doble borde de manzana: segunda raya a 1.7 m del contenedor (la de T-1357 está a 0.9 m).
            foreach (ColocarT1357Puerto.Bloque bl in ColocarT1357Puerto.Bloques())
            {
                Vector3 c = new Vector3(bl.Centro.x, 1f, bl.Centro.y);
                if (CercaK15(c) || Libre(new Bounds(c, new Vector3(4f, 0.5f, 4f)), 0f))
                {
                    continue; // fuera del área de K15 o el bloque ya no está
                }
                manzanas++;
                float mx = ColocarT1357Puerto.LargoBloque * 0.5f + 1.7f, mz = ColocarT1357Puerto.FondoBloque * 0.5f + 1.7f;
                for (int lado = 0; lado < 4; lado++)
                {
                    bool horizontal = lado < 2;
                    float fijo = (lado % 2 == 0 ? -1f : 1f) * (horizontal ? mz : mx);
                    float largoTotal = horizontal ? mx * 2f : mz * 2f;
                    for (float t = 0f; t < largoTotal; t += 3f)
                    {
                        float paso = Mathf.Min(3f, largoTotal - t);
                        float cen = -largoTotal * 0.5f + t + paso * 0.5f;
                        Vector3 p = horizontal ? new Vector3(c.x + cen, 0f, c.z + fijo) : new Vector3(c.x + fijo, 0f, c.z + cen);
                        Vector3 dir = horizontal ? Vector3.right : Vector3.forward;
                        Bounds caja = new Bounds(new Vector3(p.x, 1f, p.z), horizontal ? new Vector3(paso, 1.2f, 0.3f) : new Vector3(0.3f, 1.2f, paso));
                        if (EnCalle(caja, 0.2f) || !Libre(caja, 0.05f))
                        {
                            continue;
                        }
                        Tira(l, p, dir, paso + 0.02f, 0.34f, Suelo(p.x, p.z) + 0.02f);
                        bordes++;
                    }
                }
            }
            // Flechas de carril en las calles internas (carril derecho hacia delante, izquierdo hacia atrás).
            foreach (Vector3[] c in ColocarT1357Puerto.Calles())
            {
                for (int i = 1; i < c.Length; i++)
                {
                    Vector3 dir = c[i] - c[i - 1];
                    float largo = dir.magnitude;
                    dir /= largo;
                    Vector3 der = Vector3.Cross(Vector3.up, dir);
                    for (float t = 14f; t < largo - 9f; t += 26f)
                    {
                        for (int carril = -1; carril <= 1; carril += 2)
                        {
                            Vector3 centro = c[i - 1] + dir * t + der * (3f * carril);
                            Vector3 d = dir * carril;
                            Bounds caja = new Bounds(new Vector3(centro.x, 1f, centro.z), new Vector3(3.6f, 1.2f, 3.6f));
                            if (CercaK15(centro) || !Libre(caja, 0.1f))
                            {
                                continue;
                            }
                            Flecha(l, centro, d, Suelo(centro.x, centro.z) + 0.02f);
                            flechas++;
                        }
                    }
                }
            }
            Log("Pintura: " + manzanas + " manzanas con doble borde (" + bordes + " tramos de 3 m), " + flechas + " flechas de carril; 2 cm sobre el piso (asfalto o losa).");
        }

        private static void Flecha(Lote l, Vector3 centro, Vector3 dir, float y)
        {
            dir.y = 0f;
            dir.Normalize();
            Vector3 r = new Vector3(dir.z, 0f, -dir.x);
            Vector3 cola = new Vector3(centro.x, y, centro.z) - dir * 1.65f;
            Tira(l, cola + dir * 1.1f, dir, 2.2f, 0.45f, y);
            Vector3 baseCabeza = cola + dir * 2.2f;
            Triangulo(l, baseCabeza - r * 0.85f, baseCabeza + r * 0.85f, cola + dir * 3.3f, Vector3.up);
        }

        private static void Charcos()
        {
            Transform g = Grupo("Charcos");
            Mesh quad = new Mesh { name = "T1443_CharcoQuad" };
            quad.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) });
            quad.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            quad.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            quad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            quad.RecalculateBounds();
            obra.Mallas.Add(quad);
            Material[] mats = new Material[4];
            for (int f = 0; f < 4; f++)
            {
                int forma = f;
                mats[f] = KitS2c.Mat("S2c_Charco_" + forma, () =>
                {
                    Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/Charco"));
                    m.SetTexture("_Mascara", KitS2c.TexCharco(forma));
                    return m;
                });
            }
            int n = 0, intentos = 0;
            while (n < 70 && intentos++ < 800)
            {
                Vector3 p = new Vector3(obra.Rango(243f, 316f), 0f, obra.Rango(-236f, -80f));
                float largo = obra.Rango(2f, 5f), ancho = obra.Rango(1.2f, 3f), giro = obra.Rango(0f, 180f);
                float radio = Mathf.Max(largo, ancho) * 0.5f;
                Bounds caja = new Bounds(new Vector3(p.x, 1f, p.z), new Vector3(radio * 2f, 1.5f, radio * 2f));
                if (CercaK15(p) || !Libre(caja, 0.1f))
                {
                    continue;
                }
                float y0 = Suelo(p.x, p.z);
                bool plano = true;
                foreach (Vector2 s in new[] { new Vector2(radio, radio), new Vector2(-radio, radio), new Vector2(radio, -radio), new Vector2(-radio, -radio) })
                {
                    if (Mathf.Abs(Suelo(p.x + s.x, p.z + s.y) - y0) > 0.03f)
                    {
                        plano = false;
                    }
                }
                if (!plano || p.z < -236f + radio)
                {
                    continue;
                }
                GameObject q = new GameObject("Charco");
                q.layer = ReflejoCharcos.CapaCharcos;
                q.transform.SetParent(g, false);
                q.transform.position = new Vector3(p.x, y0 + 0.025f, p.z);
                q.transform.rotation = Quaternion.Euler(0f, giro, 0f);
                q.transform.localScale = new Vector3(ancho, 1f, largo);
                q.AddComponent<MeshFilter>().sharedMesh = quad;
                MeshRenderer mr = q.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mats[obra.Azar.Next(4)];
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                q.AddComponent<CharcoVisible>();
                n++;
            }
            Log("Charcos: " + n + " (material S2c_Charco_0..3 existente, shader MechaCyberpunk/Mundo/Charco con reflejo por ReflejoCharcos; no se editó). El reflejo real se confirma en la foto.");
        }

        // ================================================================ Cierre

        private static void MarcarEstatico()
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<Light>() == null)
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                }
            }
        }

        private static void GuardarMallas()
        {
            AssetDatabase.DeleteAsset(RutaMallas);
            if (obra.Mallas.Count == 0)
            {
                return;
            }
            AssetDatabase.CreateAsset(obra.Mallas[0], RutaMallas);
            foreach (Mesh m in obra.Mallas.Skip(1))
            {
                AssetDatabase.AddObjectToAsset(m, RutaMallas);
            }
            AssetDatabase.SaveAssets();
            Log("Mallas guardadas en " + RutaMallas + ": " + obra.Mallas.Count + ".");
        }

        // ================================================================ Validar

        [MenuItem("Mecha Cyberpunk/T1443/Puerto: validar patio A15")]
        public static void Validar()
        {
            StringBuilder v = new StringBuilder("T-1443: validación del patio del Puerto\n");
            int fallos = 0;
            GameObject z = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                zona = z;
                Physics.SyncTransforms();
                fisica = z.scene.GetPhysicsScene();
                Transform r = z.transform.Find(Raiz);
                if (r == null)
                {
                    v.AppendLine("FALLA: no existe " + Raiz + " (corre Aplicar).");
                    fallos++;
                }
                else
                {
                    raiz = r;
                    ocupados.Clear();
                    tramos.Clear();
                    CargarOcupados();
                    int luces = r.GetComponentsInChildren<Light>(true).Length;
                    int objetos = r.GetComponentsInChildren<Transform>(true).Length;
                    float mb = new FileInfo(RutaPrefab).Length / 1048576f;
                    v.AppendLine("Luces nuevas: " + luces + " (tope " + TopeLuces + "). Objetos nuevos: " + objetos + " (tope " + TopeObjetos + "). Prefab: " + mb.ToString("F1") + " MB (tope 60). Luces totales del prefab: " + z.GetComponentsInChildren<Light>(true).Length + ".");
                    if (luces > TopeLuces) { v.AppendLine("FALLA: demasiadas luces."); fallos++; }
                    if (objetos > TopeObjetos) { v.AppendLine("FALLA: demasiados objetos."); fallos++; }
                    if (mb * 1048576f > TopeBytesPrefab) { v.AppendLine("FALLA: prefab muy grande."); fallos++; }
                    int flotan = 0, enCarril = 0, encimados = 0, choques = 0;
                    List<Transform> piezas = new List<Transform>();
                    foreach (string grupo in new[] { "Bodegas", "Detalles", "Postes" })
                    {
                        Transform gr = r.Find(grupo);
                        if (gr != null)
                        {
                            piezas.AddRange(gr.Cast<Transform>().Where(t => t.GetComponentInChildren<Renderer>() != null));
                        }
                    }
                    List<Bounds> cajas = new List<Bounds>();
                    foreach (Transform t in piezas)
                    {
                        Bounds b = MedirSinHalo(t.gameObject);
                        cajas.Add(b);
                        float suelo = Suelo(b.center.x, b.center.z);
                        if (Mathf.Abs(b.min.y - suelo) > 0.05f && !t.name.StartsWith("Poste"))
                        {
                            flotan++;
                            v.AppendLine("FALLA flota/enterrado: " + t.name + " base " + b.min.y.ToString("F2") + " vs piso " + suelo.ToString("F2"));
                        }
                        if (EnCalle(b, 0f) && !t.name.StartsWith("Poste"))
                        {
                            enCarril++;
                            v.AppendLine("FALLA en carril: " + t.name + " en " + b.center.ToString("F1"));
                        }
                        // Contra lo que ya había (sin la raíz propia): penetración mayor a 15 cm en los tres ejes.
                        Bounds ch = b;
                        ch.Expand(-0.3f);
                        if (ocupados.Any(o => o.Intersects(ch)))
                        {
                            choques++;
                            v.AppendLine("FALLA choca con algo existente: " + t.name + " en " + b.center.ToString("F1"));
                        }
                    }
                    for (int i = 0; i < cajas.Count; i++)
                    {
                        for (int j = i + 1; j < cajas.Count; j++)
                        {
                            Bounds a = cajas[i];
                            a.Expand(-0.3f);
                            if (a.Intersects(cajas[j]))
                            {
                                encimados++;
                                v.AppendLine("FALLA encimados: " + piezas[i].name + " con " + piezas[j].name);
                            }
                        }
                    }
                    v.AppendLine("Flotantes/enterrados: " + flotan + ". En carril: " + enCarril + ". Encimados entre sí: " + encimados + ". Chocan con lo existente: " + choques + ". (Piezas revisadas: " + piezas.Count + ".)");
                    fallos += flotan + enCarril + encimados + choques;
                    // Grúas sobre el muelle.
                    Transform muelle = z.transform.Find("Muelle");
                    if (muelle != null)
                    {
                        foreach (Transform g in muelle.Cast<Transform>().Where(t => t.name.StartsWith("PU01_Grua") && t.gameObject.activeInHierarchy))
                        {
                            List<Vector3> vs = Vertices(g);
                            if (vs.Count == 0) continue;
                            float minY = vs.Min(p => p.y);
                            List<float> pisosPata = vs.Where(q => q.y < minY + 0.3f).Take(60).Select(pp => Suelo(pp.x, pp.z)).OrderBy(s2 => s2).ToList();
                            float peorPata = pisosPata.Count == 0 ? 0f : Mathf.Abs(minY - pisosPata[pisosPata.Count / 2]); // contra la mediana del piso bajo las patas
                            bool bien = peorPata <= 0.05f;
                            v.AppendLine((bien ? "Grúa " : "FALLA grúa ") + g.name + ": pata baja y " + minY.ToString("F2") + ", peor error contra el piso " + peorPata.ToString("F2") + " m");
                            if (!bien) fallos++;
                        }
                    }
                    v.AppendLine("Materiales T1443 en el patio: " + z.GetComponentsInChildren<Renderer>(true).SelectMany(rr => rr.sharedMaterials).Where(m => m != null && m.name.StartsWith(Prefijo)).Select(m => m.name).Distinct().Count() + " distintos.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(z);
                zona = null;
                raiz = null;
            }
            v.AppendLine(fallos == 0 ? "RESULTADO: sin fallas." : "RESULTADO: " + fallos + " falla(s).");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaValidar, v.ToString());
            Debug.Log("[ZonaPuertoT1443] " + RutaValidar + ":\n" + v);
        }

        // ================================================================ Fotos

        /// <summary>
        /// Foto A15 desde CamaraA15 (esquina noreste del muelle, mirando al suroeste) con la niebla aérea ligera de T-1414 (docs/capturas/_vista19b/puerto-a15-t1443.png) y con la niebla
        /// del juego (…-niebla.png), referencia a la izquierda. No guarda nada; corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1443/Puerto: fotos A15")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("Logs");
            Directory.CreateDirectory(Path.GetDirectoryName(RutaFoto));
            Physics.SyncTransforms();
            const string referencia = "imageassets/referencias/look/A15_Puerto_Aerea.png";
            Vector3 desde = CamaraA15, mira = MiraA15;
            Transform patio = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == Raiz);
            Transform marca = patio != null ? patio.Find("CamaraA") : null;
            if (marca != null && marca.Find("Mira") != null)
            {
                desde = marca.position;
                mira = marca.Find("Mira").position;
                Debug.Log("[ZonaPuertoT1443] Cámara de la búsqueda: " + desde + " -> " + mira);
            }
            else
            {
                Debug.LogWarning("[ZonaPuertoT1443] No hay " + Raiz + "/CamaraA (corre Aplicar): se usa la cámara fija.");
            }
            FotoCamara(desde, mira, "Logs/t1443-a15-juego.png", CampoA15, AereasT1414.DensidadNieblaAerea);
            FotoCamara(desde, mira, "Logs/t1443-a15-niebla-juego.png", CampoA15, -1f);
            ColocarS2.LadoALado(referencia, "Logs/t1443-a15-juego.png", RutaFoto);
            ColocarS2.LadoALado(referencia, "Logs/t1443-a15-niebla-juego.png", RutaFoto.Replace(".png", "-niebla.png"));
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }

        private static void FotoCamara(Vector3 desde, Vector3 hacia, string ruta, float campo, float densidadNiebla)
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
            GameObject go = new GameObject("CamaraT1443");
            Camera cam = go.AddComponent<Camera>();
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = ColocarS2cLook.LejosFoto;
            cam.fieldOfView = campo;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            const int ancho = 1376, alto = 768;
            RenderTexture rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = (float)ancho / alto;
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

        // ================================================================ Ayudas

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

        private static void Escribir(string ruta)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ruta, informe.ToString());
            Debug.Log("[ZonaPuertoT1443] " + ruta + ":\n" + informe);
        }
    }
}
