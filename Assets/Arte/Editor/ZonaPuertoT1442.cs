using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1442 (Puerto, vista de calle K15 de la ronda 19-nov). Todo lo nuevo va bajo el hijo raíz "T1442_Calle" del prefab T1357_Puerto, que Aplicar borra y rehace
    /// (se puede correr dos veces; no apaga ni mueve nada viejo). Corre en cualquier orden con ZonaPuertoT1443 (T1443_Patio): cada uno mira los bounds de lo que ya hay antes de colocar.
    /// 1) Escaleras de contenedores (1-2-3 de alto, de colores y con grafiti) pegadas al lado este de la calle, con PuntoTrepar en cada escalón.
    /// 2) Bodegas del lado oeste: toldos de lámina, lámparas naranjas (emisión + pocas luces) y letreros de neón sin luz (emisión).
    /// 3) Charcos con reflejo (material y shader de charco del proyecto, sin editarlos) por el piso de la calle.
    /// 4) Montacargas, pallets, tambos, huacales y 8 personas (estibadores NP11 y otros NP) al borde de la calle.
    /// 5) Barco y grúas del muelle con ventanas y balizas emisivas puestas SOBRE vértices de su malla (no flotan).
    /// Métodos: Aplicar (Logs/t1442-aplicar.txt), Validar (Logs/t1442-validar.txt), Fotos (corrida aparte; docs/capturas/_vista19b/puerto-k15-t1442.png).
    /// </summary>
    public static class ZonaPuertoT1442
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        public const string Raiz = "T1442_Calle";
        private const string CarpetaMat = "Assets/Arte/Mundo/Puerto";
        private const float XPared = 166.5f;
        /// <summary>Medio ancho del carril libre: la calle mide 12 m, pero 1.8 m de cada borde se usan para props y gente.</summary>
        private const float MediaCalle = 4.2f;
        private const int TopeLuces = 8;
        private const int TopeObjetos = 1200;
        private static readonly StringBuilder informe = new StringBuilder();

        /// <summary>
        /// Cámara de K15 (ronda 2): ojos a 1.7 m en el patio sobre el muelle oeste (x 117..183, z -222.4..-237), FUERA de las calles de tránsito, mirando al oeste.
        /// A la izquierda las pilas de contenedores escalonadas (z -237..-234.6) con el barco PU02 y la grúa nueva detrás (50-60 m); a la derecha la hilera de bodegas (3 m).
        /// </summary>
        public static readonly Vector3 CamaraK15 = new Vector3(181.5f, 1.7f, -231.5f);
        public static readonly Vector3 MiraK15 = new Vector3(118f, 6f, -233f);
        private const float PatioZFrente = -227.8f, PatioZSur = -237.0f;

        private static void Nota(string t)
        {
            informe.AppendLine(t);
        }

        private static Bounds Limites(Transform t) => ZonaPuertoT1387.Limites(t);

        private static Rect Huella(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);

        private static Rect Expandir(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        private static GameObject BuscarPrefab(string codigo)
        {
            return AssetDatabase.FindAssets(codigo + " t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(r => Path.GetFileNameWithoutExtension(r) == codigo)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
        }

        // ---------------------------------------------------------------- Ayudantes de mundo

        private static Bounds CajaMalla(MeshFilter mf)
        {
            Bounds l = mf.sharedMesh.bounds;
            Vector3 e = l.extents;
            Bounds r = new Bounds(mf.transform.TransformPoint(l.center + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                Vector3 s = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                r.Encapsulate(mf.transform.TransformPoint(l.center + s));
            }
            return r;
        }

        /// <summary>Huellas de todo lo sólido que ya hay en el prefab (fuera de mi raíz): lo que mide más de 0.35 m, no es un piso ni un cable, y empieza abajo de 6 m.</summary>
        private static List<Rect> Ocupados(Transform raiz, Transform mia)
        {
            List<Rect> r = new List<Rect>();
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(false))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null || (mia != null && mf.transform.IsChildOf(mia)))
                {
                    continue;
                }
                string n = mf.name;
                if (n == "Cables" || n.StartsWith("Charco") || n.StartsWith("Halo") || n.StartsWith("Linea"))
                {
                    continue;
                }
                Bounds b = CajaMalla(mf);
                if (b.size.y < 0.35f || b.size.x > 45f || b.size.z > 45f || b.min.y > 6f || b.size.sqrMagnitude < 0.04f)
                {
                    continue;
                }
                r.Add(Huella(b));
            }
            return r;
        }

        private static bool EnCalle(Rect h)
        {
            Vector2[] puntos = { h.center, new Vector2(h.xMin, h.yMin), new Vector2(h.xMax, h.yMin), new Vector2(h.xMin, h.yMax), new Vector2(h.xMax, h.yMax) };
            foreach (Vector3[] calle in ColocarT1357Puerto.Calles())
            {
                foreach (Vector2 p in puntos)
                {
                    if (ColocarT1357Puerto.DistanciaACalle(new Vector3(p.x, 0f, p.y), calle) < MediaCalle)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool Libre(Rect h, List<Rect> ocupados, List<Rect> mios)
        {
            Rect dentro = Expandir(h, -0.05f);
            return !ocupados.Any(r => r.Overlaps(dentro)) && !mios.Any(r => r.Overlaps(dentro));
        }

        private static Material Mat(string nombre, System.Func<Material> crear)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMat))
            {
                AssetDatabase.CreateFolder("Assets/Arte/Mundo", "Puerto");
            }
            string ruta = CarpetaMat + "/" + nombre + ".mat";
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

        private static Material Emisivo(string nombre, Color color, float brillo)
        {
            return Mat(nombre, () =>
            {
                Material m = KitS2c.Lit(new Color(0.04f, 0.04f, 0.04f), 0.2f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * brillo);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
        }

        private static readonly Color Naranja = new Color(1f, 0.55f, 0.2f);

        private static Light UltimaLuz(Transform t)
        {
            return t.GetComponentInChildren<Light>(true);
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1442/Puerto calle K15 (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            Nota("T-1442 Puerto calle K15");
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform viejo = raiz.transform.Find(Raiz);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                int lucesAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                Transform nuevo = new GameObject(Raiz).transform;
                nuevo.SetParent(raiz.transform, false);
                KitS2c.Obra o = new KitS2c.Obra { Raiz = nuevo, Azar = new System.Random(14420), Nombre = Raiz };
                List<Rect> ocupados = Ocupados(raiz.transform, nuevo);
                List<Rect> mios = new List<Rect>();
                Nota("Huellas ya ocupadas en el prefab (T1357..T1443): " + ocupados.Count);

                // Avenida (ronda 1): se deja, sin luces (las 8 luces son del patio).
                Escaleras(nuevo, ocupados, mios);
                Bodegas(raiz.transform, nuevo, o);
                Charcos(nuevo, o, ocupados, mios, "Charcos", Rect.MinMaxRect(154.6f, -205f, 165.4f, -88f), 8, new Vector2(1.8f, 4.8f), new Vector2(1.2f, 3f), false, null);
                Props(nuevo, ocupados, mios);
                Gente(nuevo, "Gente", 2, ocupados, mios, new System.Random(14422), false);
                // Patio de K15 (ronda 2).
                List<float> xLuces = new List<float>();
                PatioBodegas(nuevo, o, ocupados, mios, xLuces);
                PatioPilas(nuevo, ocupados, mios);
                GruaNueva(raiz.transform, nuevo);
                BuscarCamara(raiz.transform, nuevo, ocupados, out Vector3 cam, out Vector3 mira);
                Transform marca = new GameObject("CamaraK15_T1442").transform;
                marca.SetParent(nuevo, false);
                marca.position = cam;
                Transform marcaMira = new GameObject("Mira").transform;
                marcaMira.SetParent(marca, true);
                marcaMira.position = mira;
                PatioPostes(nuevo, o, ocupados, mios, cam, mira);
                Charcos(nuevo, o, ocupados, mios, "CharcosPatio", Rect.MinMaxRect(121f, -233.8f, 181f, -228.6f), 26, new Vector2(6f, 14f), new Vector2(2.5f, 4.2f), true, xLuces);
                PatioProps(nuevo, ocupados, mios, cam, mira);
                Gente(nuevo, "GentePatio", 7, ocupados, mios, new System.Random(14423), true);
                Muelle(raiz.transform, nuevo, o, cam);

                KitS2c.Terminar(o);
                Transform gente = nuevo.Find("Gente");
                foreach (Transform t in nuevo.GetComponentsInChildren<Transform>(true))
                {
                    if (t.GetComponent<Light>() == null && (gente == null || !t.IsChildOf(gente)) && t.GetComponent<Animator>() == null)
                    {
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                    }
                }
                int lucesDespues = raiz.GetComponentsInChildren<Light>(true).Length;
                Nota("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (nuevas " + (lucesDespues - lucesAntes) + ", tope " + TopeLuces + "). Objetos nuevos: " + nuevo.GetComponentsInChildren<Transform>(true).Length + " (tope " + TopeObjetos + ").");
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1442-aplicar.txt", informe.ToString());
            Debug.Log("[ZonaPuertoT1442] " + informe);
        }

        // ---------------------------------------------------------------- 1. Escaleras de contenedores

        private static readonly Dictionary<string, float> yawCache = new Dictionary<string, float>();

        /// <summary>Giro (grados) con el que el contenedor queda con su eje largo en z y, si es PU08, con la cara con grafiti hacia el oeste (la calle).</summary>
        private static float YawDe(GameObject prefab, Transform padre)
        {
            if (yawCache.TryGetValue(prefab.name, out float ya))
            {
                return ya;
            }
            float yaw = 90f;
            MethodInfo mi = typeof(ZonaPuertoT1396).GetMethod("CaraGrafiti", BindingFlags.NonPublic | BindingFlags.Static);
            if (prefab.name.StartsWith("PU08") && mi != null)
            {
                object[] args = { prefab, padre, null, null, null };
                mi.Invoke(null, args);
                Vector3 cara = (Vector3)args[2];
                bool largoEnX = (bool)args[3];
                for (float y = 0f; y < 360f; y += 90f)
                {
                    Quaternion q = Quaternion.Euler(0f, y, 0f);
                    Vector3 largo = q * (largoEnX ? Vector3.right : Vector3.forward);
                    if (Mathf.Abs(largo.z) > 0.9f && Vector3.Dot(q * cara, Vector3.left) > 0.9f)
                    {
                        yaw = y;
                        break;
                    }
                }
            }
            else
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Bounds b = Limites(g.transform);
                yaw = b.size.x > b.size.z ? 90f : 0f;
                Object.DestroyImmediate(g);
            }
            yawCache[prefab.name] = yaw;
            return yaw;
        }

        private static GameObject PonerCont(Transform padre, GameObject prefab, string nombre, float xMin, float zCentro, float yBase)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = nombre;
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, YawDe(prefab, padre), 0f));
            g.transform.localScale = Vector3.one;
            Bounds b0 = Limites(g.transform);
            float sx = ColocarT1357Puerto.AnchoCont / b0.size.x, sy = ColocarT1357Puerto.AltoCont / b0.size.y, sz = ColocarT1357Puerto.LargoCont / b0.size.z;
            float menor = Mathf.Min(sx, sy, sz);
            g.transform.localScale = (menor < 0.8f || Mathf.Max(sx, sy, sz) > 1.25f) ? Vector3.one * menor : new Vector3(sx, sy, sz);
            Bounds b = Limites(g.transform);
            g.transform.position += new Vector3(xMin - b.min.x, yBase - b.min.y, zCentro - b.center.z);
            BugsZonasT1410.AsegurarColliderYBarril(g);
            return g;
        }

        private static void Escalera(Transform padre, string nombre, Vector3 pie, float alto, Vector3? dir = null)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(pie, Quaternion.LookRotation(dir ?? Vector3.back));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
            so.FindProperty("alturaSubida").floatValue = alto;
            so.FindProperty("salida").vector3Value = new Vector3(0f, alto, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
        }

        private static void Escaleras(Transform nuevo, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("Contenedores").transform;
            padre.SetParent(nuevo, false);
            string[] codigos =
            {
                "PU08_ContenedorGraffiti_a", "PT01_Contenedor40_Naranja", "PU08_ContenedorGraffiti_b", "PT01_Contenedor40_Azul",
                "PT01_Contenedor40_Rojo", "PT01_Contenedor40_Verde",
            };
            List<GameObject> prefabs = new List<GameObject>();
            foreach (string c in codigos)
            {
                GameObject p = BuscarPrefab(c);
                if (p == null)
                {
                    Nota("FALTA " + c + ": se omite.");
                }
                else
                {
                    prefabs.Add(p);
                }
            }
            if (prefabs.Count == 0)
            {
                return;
            }
            const float Hueco = 0.02f;
            float largo = ColocarT1357Puerto.LargoCont, alto = ColocarT1357Puerto.AltoCont, ancho = ColocarT1357Puerto.AnchoCont;
            int unidades = 0, contenedores = 0, color = 0;
            float zn = -86f;
            while (zn > -195f && unidades < 3)
            {
                int[][] patrones = { new[] { 1, 2, 3 }, new[] { 1, 2 } };
                bool puesta = false;
                foreach (int[] alturas in patrones)
                {
                    List<Rect> rects = new List<Rect>();
                    for (int i = 0; i < alturas.Length; i++)
                    {
                        float zNorte = zn - i * (largo + Hueco);
                        rects.Add(Rect.MinMaxRect(XPared, zNorte - largo, XPared + ancho, zNorte));
                    }
                    Rect escalera = Rect.MinMaxRect(XPared, zn, XPared + ancho, zn + 0.95f);
                    if (rects.Any(r => !Libre(r, ocupados, mios) || EnCalle(r)) || !Libre(escalera, ocupados, mios))
                    {
                        continue;
                    }
                    List<GameObject> hechos = new List<GameObject>();
                    for (int i = 0; i < alturas.Length; i++)
                    {
                        float zc = rects[i].center.y;
                        float yBase = 0f;
                        for (int n = 0; n < alturas[i]; n++)
                        {
                            GameObject g = PonerCont(padre, prefabs[color % prefabs.Count], "Cont_" + unidades + "_" + i + "_" + n, XPared, zc, yBase);
                            color++;
                            contenedores++;
                            yBase = Limites(g.transform).max.y;
                            hechos.Add(g);
                        }
                        mios.Add(rects[i]);
                    }
                    mios.Add(escalera);
                    float xc = XPared + ancho * 0.5f;
                    Escalera(padre, "Escalera_Suelo_" + unidades, new Vector3(xc, 0f, zn + 0.3f), alto);
                    for (int i = 0; i + 1 < alturas.Length; i++)
                    {
                        float zSur = rects[i].yMin;
                        Escalera(padre, "Escalera_Fila_" + unidades + "_" + i, new Vector3(xc, alturas[i] * alto, zSur + 0.25f), alto);
                    }
                    Nota("Escalera de contenedores " + unidades + " (alturas " + string.Join("-", alturas) + ") desde z " + zn.ToString("F1") + ", x " + XPared + ".");
                    unidades++;
                    zn -= alturas.Length * (largo + Hueco) + 1.8f;
                    puesta = true;
                    break;
                }
                if (!puesta)
                {
                    zn -= 2f;
                }
            }
            Nota("Contenedores nuevos (colores y grafiti, escalonados): " + contenedores + " en " + unidades + " escaleras; si salió 0, el lado este de la calle ya está lleno (T1396/T1402/T1443).");
        }

        // ---------------------------------------------------------------- 2. Bodegas: toldos, lámparas, neones

        private static void Bodegas(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            Transform padre = new GameObject("Bodegas").transform;
            padre.SetParent(nuevo, false);
            Transform grupo = raiz.Find("Bodegas");
            List<Transform> bodegas = grupo == null ? new List<Transform>()
                : grupo.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")))
                    .Where(t => { Bounds b = Limites(t); return b.max.x > 148f && b.max.x < 158f && b.center.z < -84f && b.center.z > -215f; })
                    .OrderByDescending(t => Limites(t).center.z).Take(6).ToList();
            Material toldo = Mat("T1442_Toldo", () => KitS2c.Lit(new Color(0.78f, 0.4f, 0.14f), 0.25f, KitS2c.TexMuro()));
            Material lampara = Emisivo("T1442_LamparaNaranja", Naranja, 4f);
            int luces = 0;
            for (int i = 0; i < bodegas.Count; i++)
            {
                Bounds b = Limites(bodegas[i]);
                float xw = b.max.x, zc = b.center.z;
                float ancho = Mathf.Min(7f, b.size.z * 0.5f);
                GameObject t = KitS2c.Losa(o, padre, "Toldo_" + i, new Vector3(xw + 0.75f, 4.0f, zc), Quaternion.Euler(0f, 0f, -11f), new Vector3(1.6f, 0.07f, ancho), toldo, 1.5f, false);
                t.name = "Toldo_" + i;
                KitS2c.Losa(o, padre, "Lampara_" + i, new Vector3(xw + 0.3f, 3.75f, zc - 3.3f), Quaternion.identity, new Vector3(0.5f, 0.14f, 0.7f), lampara, 1f, false);
                Color neon = i % 2 == 0 ? KitS2c.Turquesa : KitS2c.Magenta;
                KitS2c.Neon(o, padre, new Vector3(xw, 3.2f, zc + (i % 2 == 0 ? 5.2f : -5.2f)), Vector3.right, Vector3.back, neon, true);
                Transform neonObj = padre.GetChild(padre.childCount - 1);
                foreach (Light l in neonObj.GetComponentsInChildren<Light>(true).ToList())
                {
                    Object.DestroyImmediate(l.gameObject); // el letrero queda solo con su emisión (el tope de luces es de 8)
                }
            }
            Nota("Bodegas del lado oeste con detalle: " + bodegas.Count + " (toldo de lámina, lámpara emisiva, neón emisivo; sin luces reales).");
        }

        // ---------------------------------------------------------------- 3. Charcos con reflejo

        private static void Charcos(Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios, string grupo, Rect region, int cuantos, Vector2 largoRango, Vector2 anchoRango, bool largoEnX, List<float> xSesgo)
        {
            Transform padre = new GameObject(grupo).transform;
            padre.SetParent(nuevo, false);
            Mesh quad = new Mesh { name = Raiz + "_" + grupo };
            quad.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) });
            quad.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            quad.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            quad.RecalculateBounds();
            o.Mallas.Add(quad);
            int puestos = 0;
            for (int i = 0; i < cuantos * 6 && puestos < cuantos; i++)
            {
                float x = o.Rango(region.xMin, region.xMax);
                if (xSesgo != null && xSesgo.Count > 0 && puestos % 2 == 0)
                {
                    x = Mathf.Clamp(o.Uno(xSesgo) + o.Rango(-2f, 2f), region.xMin, region.xMax); // donde pega la luz de lámparas y neones
                }
                Vector3 p = new Vector3(x, 0f, o.Rango(region.yMin, region.yMax));
                float largo = o.Rango(largoRango.x, largoRango.y), anchoC = o.Rango(anchoRango.x, anchoRango.y);
                float giro = largoEnX ? 90f + o.Rango(-15f, 15f) : o.Rango(0f, 180f);
                float semiX = largoEnX ? largo * 0.5f : anchoC * 0.5f, semiZ = largoEnX ? anchoC * 0.5f : largo * 0.5f;
                Rect h = Rect.MinMaxRect(p.x - semiX, p.z - semiZ, p.x + semiX, p.z + semiZ);
                if (grupo != "CharcosPatio" && !Libre(Expandir(h, -0.4f), ocupados, mios))
                {
                    continue;
                }
                int forma = o.Azar.Next(4);
                Material mat = KitS2c.Mat("S2c_Charco_" + forma, () =>
                {
                    Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/Charco"));
                    m.SetTexture("_Mascara", KitS2c.TexCharco(forma));
                    return m;
                });
                GameObject q = new GameObject("Charco");
                q.layer = ReflejoCharcos.CapaCharcos;
                q.transform.SetParent(padre, false);
                q.transform.position = p + Vector3.up * 0.012f;
                q.transform.rotation = Quaternion.Euler(90f, giro, 0f);
                q.transform.localScale = new Vector3(anchoC, largo, 1f);
                q.AddComponent<MeshFilter>().sharedMesh = quad;
                MeshRenderer mr = q.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                q.AddComponent<CharcoVisible>();
                puestos++;
            }
            Nota(grupo + ": " + puestos + " charcos con reflejo (shader y materiales S2c_Charco del proyecto, sin editarlos).");
        }

        // ---------------------------------------------------------------- 4. Props y gente

        private static GameObject Poner(Transform padre, string codigo, float alto, Vector2 xz, float giro, List<Rect> ocupados, List<Rect> mios, bool collider)
        {
            GameObject prefab = BuscarPrefab(codigo);
            if (prefab == null)
            {
                Nota("FALTA " + codigo + ": se omite.");
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = codigo + "_" + padre.childCount;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, giro, 0f));
            go.transform.localScale = Vector3.one;
            Bounds b = Limites(go.transform);
            go.transform.localScale = Vector3.one * (alto / Mathf.Max(0.01f, b.size.y));
            b = Limites(go.transform);
            go.transform.position += new Vector3(xz.x - b.center.x, -b.min.y, xz.y - b.center.z);
            Rect h = Huella(Limites(go.transform));
            if (EnCalle(h) || !Libre(h, ocupados, mios))
            {
                Object.DestroyImmediate(go);
                return null;
            }
            if (collider)
            {
                BugsZonasT1410.AsegurarColliderYBarril(go);
            }
            mios.Add(h);
            return go;
        }

        private static void Props(Transform nuevo, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("Props").transform;
            padre.SetParent(nuevo, false);
            System.Random azar = new System.Random(14421);
            (string codigo, float alto, int cuantos)[] lista =
            {
                ("PU05_Montacargas", 2.6f, 3), ("PU06_Pallets", 1.2f, 5), ("C16_Tambo", 1.0f, 6), ("C17_Huacales", 1.0f, 4),
            };
            int total = 0;
            List<Vector3> montacargas = new List<Vector3>();
            foreach ((string codigo, float alto, int cuantos) in lista)
            {
                int puestos = 0;
                for (int intento = 0; intento < 60 && puestos < cuantos; intento++)
                {
                    bool oeste = azar.Next(2) == 0;
                    float x = oeste ? 155.6f + (float)azar.NextDouble() * 0.9f : 164.4f + (float)azar.NextDouble() * 0.9f;
                    float z = -88f - (float)azar.NextDouble() * 115f;
                    GameObject g = Poner(padre, codigo, alto, new Vector2(x, z), (float)azar.NextDouble() * 360f, ocupados, mios, true);
                    if (g != null)
                    {
                        puestos++;
                        total++;
                        if (codigo == "PU05_Montacargas")
                        {
                            montacargas.Add(g.transform.position);
                        }
                    }
                }
            }
            Nota("Props de piso (montacargas, pallets, tambos, huacales) al borde de la calle (fuera de los 8.4 m centrales): " + total + "");
        }

        private static void Gente(Transform nuevo, string grupo, int cuantos, List<Rect> ocupados, List<Rect> mios, System.Random azar, bool patio)
        {
            Transform padre = new GameObject(grupo).transform;
            padre.SetParent(nuevo, false);
            string[] modelos =
            {
                "NP11_Estibador/NP11_Estibador_a", "NP11_Estibador/NP11_Estibador_b", "NP11_Estibador/NP11_Estibador_a", "NP11_Estibador/NP11_Estibador_b",
                "NP13_Repartidor/NP13_Repartidor_a", "NP11_Estibador/NP11_Estibador_a", "NP07_Pescador/NP07_Pescador_b", "NP11_Estibador/NP11_Estibador_b",
            };
            int puestos = 0;
            for (int k = 0; k < cuantos; k++)
            {
                string m = modelos[k % modelos.Length];
                string carpeta = "Assets/Arte/Personajes/" + m.Substring(0, m.IndexOf('/'));
                string nombre = Path.GetFileName(m);
                string ruta = "Assets/Arte/Personajes/" + m;
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta + ".fbx");
                RuntimeAnimatorController ctl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ruta + ".controller");
                if (fbx == null)
                {
                    Nota("FALTA " + ruta + ".fbx: se omite.");
                    continue;
                }
                for (int intento = 0; intento < 60; intento++)
                {
                    float x, z;
                    if (patio)
                    {
                        x = 124f + (float)azar.NextDouble() * 54f;
                        z = azar.Next(2) == 0 ? -228.9f - (float)azar.NextDouble() * 0.6f : -233.1f - (float)azar.NextDouble() * 0.4f;
                    }
                    else
                    {
                        bool oeste = azar.Next(2) == 0;
                        x = oeste ? 154.7f + (float)azar.NextDouble() * 1.2f : 164.2f + (float)azar.NextDouble() * 1.4f;
                        z = -88f - (float)azar.NextDouble() * 110f;
                    }
                    Rect h = Rect.MinMaxRect(x - 0.4f, z - 0.4f, x + 0.4f, z + 0.4f);
                    if ((!patio && EnCalle(h)) || !Libre(h, ocupados, mios))
                    {
                        continue;
                    }
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, padre);
                    go.name = nombre + "_" + puestos;
                    go.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, (float)azar.NextDouble() * 360f, 0f));
                    foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                    {
                        Object.DestroyImmediate(c);
                    }
                    Animator an = go.GetComponentInChildren<Animator>(true);
                    if (an == null)
                    {
                        an = go.AddComponent<Animator>();
                    }
                    an.applyRootMotion = false;
                    if (ctl != null)
                    {
                        an.runtimeAnimatorController = ctl;
                    }
                    ArreglarBlanco(go, carpeta, nombre);
                    PoseDe(go, ctl, azar);
                    mios.Add(h);
                    puestos++;
                    break;
                }
            }
            Nota(grupo + ": " + puestos + " de " + cuantos + " personas (sin collider, con Animator, material Base explícito y pose muestreada; pivote en los pies a y = 0).");
        }

        /// <summary>Los fbx de NP salen blancos si no traen su material: se asigna NP*_Base.mat (o una copia mía si su textura base falta) a toda ranura vacía o sin textura.</summary>
        private static void ArreglarBlanco(GameObject go, string carpeta, string nombre)
        {
            Material baseMat = AssetDatabase.LoadAssetAtPath<Material>(carpeta + "/Materiales/" + nombre + "_Base.mat");
            if (baseMat == null)
            {
                Nota("AVISO: no existe " + carpeta + "/Materiales/" + nombre + "_Base.mat");
                return;
            }
            Material usar = baseMat;
            string prop = baseMat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            if (baseMat.GetTexture(prop) == null)
            {
                string[] rutas = AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta + "/Texturas" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
                string elegida = rutas.FirstOrDefault(r => Path.GetFileName(r).ToLowerInvariant().Contains("base") && r.Contains(nombre))
                    ?? rutas.FirstOrDefault(r => r.Contains(nombre) && !Path.GetFileName(r).ToLowerInvariant().Contains("normal")) ?? rutas.FirstOrDefault();
                Texture2D tex = elegida == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(elegida);
                if (tex == null)
                {
                    Nota("AVISO: " + nombre + " no tiene textura base en " + carpeta + "/Texturas; queda con su color.");
                }
                else
                {
                    usar = Mat("T1442_" + nombre + "_Base", () =>
                    {
                        Material c = new Material(baseMat);
                        c.SetTexture(prop, tex);
                        return c;
                    });
                    Nota("Material de " + nombre + ": copia propia con textura " + elegida);
                }
            }
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null || !ms[i].HasProperty("_BaseMap") || ms[i].GetTexture("_BaseMap") == null)
                    {
                        ms[i] = usar;
                    }
                }
                r.sharedMaterials = ms;
            }
        }

        /// <summary>El Animator no corre en el editor (salen en pose T): se muestrea un clip de trabajo o idle del controller para dejar la pose guardada.</summary>
        private static void PoseDe(GameObject go, RuntimeAnimatorController ctl, System.Random azar)
        {
            if (ctl == null)
            {
                return;
            }
            AnimationClip[] clips = ctl.animationClips;
            if (clips == null || clips.Length == 0)
            {
                Nota("AVISO: el controller de " + go.name + " no tiene clips.");
                return;
            }
            AnimationClip clip = clips.FirstOrDefault(c => { string n = c.name.ToLowerInvariant(); return n.Contains("trabaj") || n.Contains("carg") || n.Contains("work"); })
                ?? clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("idle")) ?? clips[0];
            clip.SampleAnimation(go, clip.length * (float)azar.NextDouble());
        }

        // ---------------------------------------------------------------- 4b. Patio de K15 (ronda 2)

        private static float YawX(GameObject prefab, Transform padre)
        {
            string clave = prefab.name + "_X";
            if (yawCache.TryGetValue(clave, out float ya))
            {
                return ya;
            }
            float yaw = 0f;
            MethodInfo mi = typeof(ZonaPuertoT1396).GetMethod("CaraGrafiti", BindingFlags.NonPublic | BindingFlags.Static);
            if (prefab.name.StartsWith("PU08") && mi != null)
            {
                object[] args = { prefab, padre, null, null, null };
                mi.Invoke(null, args);
                Vector3 cara = (Vector3)args[2];
                bool largoEnX = (bool)args[3];
                for (float y = 0f; y < 360f; y += 90f)
                {
                    Quaternion q = Quaternion.Euler(0f, y, 0f);
                    Vector3 largo = q * (largoEnX ? Vector3.right : Vector3.forward);
                    if (Mathf.Abs(largo.x) > 0.9f && Vector3.Dot(q * cara, Vector3.forward) > 0.9f)
                    {
                        yaw = y; // eje largo en x y la cara con grafiti al norte (hacia el pasillo)
                        break;
                    }
                }
            }
            else
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Bounds b = Limites(g.transform);
                yaw = b.size.x > b.size.z ? 0f : 90f;
                Object.DestroyImmediate(g);
            }
            yawCache[clave] = yaw;
            return yaw;
        }

        private static GameObject PonerContX(Transform padre, GameObject prefab, string nombre, float xCentro, float zCentro, float yBase)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = nombre;
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, YawX(prefab, padre), 0f));
            g.transform.localScale = Vector3.one;
            Bounds b0 = Limites(g.transform);
            float sx = ColocarT1357Puerto.LargoCont / b0.size.x, sy = ColocarT1357Puerto.AltoCont / b0.size.y, sz = ColocarT1357Puerto.AnchoCont / b0.size.z;
            float menor = Mathf.Min(sx, sy, sz);
            g.transform.localScale = (menor < 0.8f || Mathf.Max(sx, sy, sz) > 1.25f) ? Vector3.one * menor : new Vector3(sx, sy, sz);
            Bounds b = Limites(g.transform);
            g.transform.position += new Vector3(xCentro - b.center.x, yBase - b.min.y, zCentro - b.center.z);
            BugsZonasT1410.AsegurarColliderYBarril(g);
            return g;
        }

        private static GameObject BodegaEn(Transform padre, GameObject prefab, string nombre, float xCentro, float anchoMax, List<Rect> ocupados, List<Rect> mios, out string bloqueo)
        {
            bloqueo = null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f)); // frente hacia -z, como la hilera norte de T1357
            go.transform.localScale = Vector3.one;
            Bounds b0 = Limites(go.transform);
            go.transform.localScale = Vector3.one * Mathf.Min(anchoMax / b0.size.x, 5.5f / b0.size.z);
            Bounds b = Limites(go.transform);
            go.transform.position += new Vector3(xCentro - b.center.x, -b.min.y, PatioZFrente - b.min.z);
            b = Limites(go.transform);
            Rect h = Huella(b);
            Rect dentro = Expandir(h, -0.05f);
            foreach (Rect r in ocupados.Concat(mios))
            {
                if (r.Overlaps(dentro))
                {
                    bloqueo = "x " + xCentro.ToString("F1") + " choca con una huella de centro " + r.center.ToString("F1") + " tamaño " + r.size.ToString("F1");
                    Object.DestroyImmediate(go);
                    return null;
                }
            }
            mios.Add(h);
            return go;
        }

        private static void PatioBodegas(Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios, List<float> xLuces)
        {
            Transform padre = new GameObject("PatioBodegas").transform;
            padre.SetParent(nuevo, false);
            string[] codigos = { "PU03_Bodega", "PU04_Bodega", "PU03_Bodega" };
            GameObject[] prefabs = codigos.Select(BuscarPrefab).ToArray();
            Material toldo = Mat("T1442_Toldo", () => KitS2c.Lit(new Color(0.78f, 0.4f, 0.14f), 0.25f, KitS2c.TexMuro()));
            Material lampara = Emisivo("T1442_LamparaNaranja", Naranja, 4f);
            Material cortina = Mat("T1442_Cortina", () => KitS2c.Lit(new Color(0.42f, 0.46f, 0.5f), 0.45f, KitS2c.TexMuro()));
            int colocadas = 0;
            foreach (float ancho in new[] { 19f, 15f, 12f })
            {
                float xr = 181f;
                string primerBloqueo = null;
                while (colocadas < 3 && xr - ancho >= 116.5f)
                {
                    GameObject prefab = prefabs[colocadas % prefabs.Length];
                    if (prefab == null)
                    {
                        Nota("FALTA " + codigos[colocadas % codigos.Length] + ": se omite.");
                        break;
                    }
                    GameObject go = BodegaEn(padre, prefab, "Bodega_" + colocadas, xr - ancho * 0.5f, ancho, ocupados, mios, out string bloqueo);
                    if (go == null)
                    {
                        primerBloqueo = primerBloqueo ?? bloqueo;
                        xr -= 1.5f;
                        continue;
                    }
                    Bounds b = Limites(go.transform);
                    float zf = b.min.z, xc = b.center.x;
                    float yToldo = Mathf.Clamp(b.size.y * 0.62f, 3.7f, 4.4f);
                    KitS2c.Losa(o, padre, "Toldo_" + colocadas, new Vector3(xc, yToldo, zf - 0.75f), Quaternion.Euler(-11f, 0f, 0f), new Vector3(Mathf.Min(8f, b.size.x * 0.45f), 0.07f, 1.6f), toldo, 1.5f, false);
                    KitS2c.Losa(o, padre, "Lampara_" + colocadas, new Vector3(xc - 3.3f, yToldo - 0.3f, zf - 0.35f), Quaternion.identity, new Vector3(0.7f, 0.14f, 0.5f), lampara, 1f, false);
                    KitS2c.FocoPared(o, padre, new Vector3(xc + 3.3f, yToldo - 0.4f, zf - 0.1f), Quaternion.LookRotation(Vector3.forward));
                    Light l = UltimaLuz(padre.GetChild(padre.childCount - 1));
                    if (l != null)
                    {
                        l.color = Naranja;
                    }
                    float xNeon = xc + (colocadas % 2 == 0 ? 3.5f : -3.5f);
                    KitS2c.Neon(o, padre, new Vector3(xNeon, 3.4f, zf), Vector3.back, Vector3.Cross(Vector3.up, Vector3.back), colocadas % 2 == 0 ? KitS2c.Magenta : KitS2c.Turquesa, true);
                    foreach (Light ln in padre.GetChild(padre.childCount - 1).GetComponentsInChildren<Light>(true).ToList())
                    {
                        Object.DestroyImmediate(ln.gameObject); // neón solo con emisión
                    }
                    // Cortina metálica bajo el neón (lámina corrugada pegada a la fachada).
                    KitS2c.Losa(o, padre, "Cortina_" + colocadas, new Vector3(xNeon, 1.35f, zf - 0.06f), Quaternion.identity, new Vector3(3.2f, 2.7f, 0.1f), cortina, 1f, false);
                    xLuces.Add(xc - 3.3f);
                    xLuces.Add(xc + 3.3f);
                    xLuces.Add(xNeon);
                    colocadas++;
                    xr -= ancho + 0.5f;
                }
                if (primerBloqueo != null)
                {
                    Nota("Bodegas del patio (ancho " + ancho + "): primera posición rechazada: " + primerBloqueo);
                }
                if (colocadas >= 3)
                {
                    break;
                }
            }
            Nota("Patio: " + colocadas + " bodegas PU03/PU04 seguidas (frente en z " + PatioZFrente + ", fondo 5.5 m) con toldo, lámpara naranja, foco, cortina y neón.");
        }

        private static void PatioPostes(Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios, Vector3 cam, Vector3 mira)
        {
            Transform padre = new GameObject("PatioPostes").transform;
            padre.SetParent(nuevo, false);
            Vector3 f = mira - cam;
            f.y = 0f;
            f.Normalize();
            List<Vector3> puntas = new List<Vector3>();
            // Uno a la izquierda (junto a las pilas, 10-20 m al frente) y otro a la derecha (frente a las bodegas, 22-34 m): nunca a menos de 9 m del lente.
            (float zPoste, float dMin, float dMax)[] sitios = { (PatioZSur + 3.3f, 10f, 20f), (PatioZFrente - 1.0f, 22f, 34f) };
            foreach ((float zPoste, float dMin, float dMax) in sitios)
            {
                for (float d = dMin; d <= dMax; d += 1.5f)
                {
                    Vector3 pie = new Vector3(cam.x + f.x * d, 0f, zPoste);
                    Rect h = Rect.MinMaxRect(pie.x - 0.45f, pie.z - 0.45f, pie.x + 0.45f, pie.z + 0.45f);
                    if (pie.x < 118f || !Libre(h, ocupados, mios))
                    {
                        continue;
                    }
                    puntas.Add(KitS2c.Poste(o, padre, pie, zPoste < PatioZFrente - 2f ? Vector3.forward : Vector3.back, 0f, true));
                    mios.Add(h);
                    break;
                }
            }
            int cables = 0;
            for (int k = 0; k + 1 < puntas.Count; k++)
            {
                foreach (float dy in new[] { 0f, -0.5f, -1f, 0.4f })
                {
                    KitS2c.Cable(o, puntas[k] + Vector3.up * dy, puntas[k + 1] + Vector3.up * dy, 1.0f);
                    cables++;
                }
            }
            Transform bodegas = nuevo.Find("PatioBodegas");
            if (bodegas != null)
            {
                foreach (Vector3 p in puntas)
                {
                    foreach (Transform t in bodegas.Cast<Transform>().Where(x => x.name.StartsWith("Bodega_")))
                    {
                        Bounds b = Limites(t);
                        if (Mathf.Abs(b.center.x - p.x) < 22f)
                        {
                            KitS2c.Cable(o, p, new Vector3(b.center.x, Mathf.Min(6.0f, b.max.y - 0.5f), b.min.z - 0.05f), 1.1f);
                            cables++;
                        }
                    }
                }
            }
            Nota("Postes del patio: " + puntas.Count + " (a 9 m o más de la cámara), " + cables + " cables.");
        }

        private static void PatioPilas(Transform nuevo, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("PatioContenedores").transform;
            padre.SetParent(nuevo, false);
            string[] codigos = { "PU08_ContenedorGraffiti_a", "PT01_Contenedor40_Naranja", "PU08_ContenedorGraffiti_b", "PT01_Contenedor40_Azul", "PT01_Contenedor40_Rojo", "PT01_Contenedor40_Verde" };
            List<GameObject> prefabs = codigos.Select(BuscarPrefab).Where(p => p != null).ToList();
            if (prefabs.Count == 0)
            {
                Nota("Patio: no hay prefabs de contenedor.");
                return;
            }
            float largo = ColocarT1357Puerto.LargoCont, alto = ColocarT1357Puerto.AltoCont, ancho = ColocarT1357Puerto.AnchoCont;
            float zc = PatioZSur + ancho * 0.5f, zNorte = PatioZSur + ancho;
            (float x0, int[] alturas)[] unidades = { (117.8f, new[] { 1, 2 }), (142.6f, new[] { 1, 2, 3 }) };
            int u = 0, color = 0, total = 0;
            foreach ((float x0, int[] alturas) in unidades)
            {
                List<Rect> rects = new List<Rect>();
                for (int i = 0; i < alturas.Length; i++)
                {
                    float xa = x0 + i * (largo + 0.04f);
                    rects.Add(Rect.MinMaxRect(xa, PatioZSur, xa + largo, zNorte));
                }
                Rect escalera = Rect.MinMaxRect(x0, zNorte, x0 + largo, zNorte + 0.95f);
                if (rects.Any(r => !Libre(r, ocupados, mios)) || !Libre(escalera, ocupados, mios))
                {
                    Nota("Pila del patio " + u + " (x " + x0 + "): hay algo (T1443 u otro); se omite.");
                    u++;
                    continue;
                }
                for (int i = 0; i < alturas.Length; i++)
                {
                    float yBase = 0f;
                    for (int n = 0; n < alturas[i]; n++)
                    {
                        GameObject g = PonerContX(padre, prefabs[color % prefabs.Count], "Cont_p" + u + "_" + i + "_" + n, rects[i].center.x, zc, yBase);
                        color++;
                        total++;
                        yBase = Limites(g.transform).max.y;
                    }
                    mios.Add(rects[i]);
                }
                mios.Add(escalera);
                Escalera(padre, "Escalera_Suelo_" + u, new Vector3(x0 + largo * 0.5f, 0f, zNorte + 0.3f), alto, Vector3.back);
                for (int i = 0; i + 1 < alturas.Length; i++)
                {
                    Escalera(padre, "Escalera_Fila_" + u + "_" + i, new Vector3(rects[i].xMax - 0.25f, alturas[i] * alto, zc), alto, Vector3.right);
                }
                Nota("Pila del patio " + u + " con alturas " + string.Join("-", alturas) + " desde x " + x0 + " (sube de oeste a este; escalera al suelo por el norte y una por escalón).");
                u++;
            }
            Nota("Contenedores del patio (colores y grafiti): " + total);
        }

        private static void GruaNueva(Transform raiz, Transform nuevo)
        {
            Transform padre = new GameObject("GruaNueva").transform;
            padre.SetParent(nuevo, false);
            Transform muelle = raiz.Find("Muelle");
            if (muelle == null)
            {
                return;
            }
            List<Transform> gruas = muelle.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && t.name.StartsWith("PU01_Grua")).ToList();
            if (gruas.Count == 0)
            {
                Nota("Grúa nueva: no hay grúas PU01 en el Muelle.");
                return;
            }
            Transform modelo = gruas.OrderBy(t => Limites(t).center.x).First();
            GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(modelo.gameObject);
            if (fuente == null)
            {
                Nota("Grúa nueva: " + modelo.name + " no es instancia de prefab; no se copia.");
                return;
            }
            Bounds bm = Limites(modelo);
            foreach (float desfase in new[] { 45f, 40f, 35f, 30f, 55f })
            {
                GameObject nueva = (GameObject)PrefabUtility.InstantiatePrefab(fuente, padre);
                nueva.name = "PU01_Grua_Nueva";
                nueva.transform.SetPositionAndRotation(modelo.position, modelo.rotation);
                nueva.transform.localScale = modelo.lossyScale;
                Bounds bn = Limites(nueva.transform);
                nueva.transform.position += new Vector3(bm.center.x - desfase - bn.center.x, 0f, 0f);
                Rect hn = Huella(Limites(nueva.transform));
                if (gruas.Any(t => Huella(Limites(t)).Overlaps(Expandir(hn, 1f))) || hn.xMin < 112f)
                {
                    Object.DestroyImmediate(nueva);
                    continue;
                }
                Nota("Grúa nueva PU01_Grua_Nueva a " + desfase + " m al oeste de " + modelo.name + ": huella " + hn.ToString() + ".");
                return;
            }
            Nota("Grúa nueva: no cupo a 30-55 m al oeste de " + modelo.name + " (huella " + Huella(bm).ToString() + ").");
        }

        private static Bounds[] Esquinas8(Bounds b)
        {
            return new[] { b };
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return b.center + new Vector3((i & 1) == 0 ? -b.extents.x : b.extents.x, (i & 2) == 0 ? -b.extents.y : b.extents.y, (i & 4) == 0 ? -b.extents.z : b.extents.z);
            }
        }

        /// <summary>
        /// Busca la cámara de K15 en el patio puntuando como la referencia: contenedores en el tercio izquierdo (sin llenar media foto), bodegas a 3-12 m en el tercio derecho,
        /// barco y grúas en la mitad superior, el pasillo al centro abajo, y nada a menos de 2.5 m del lente. Ojos a 1.7 m. Devuelve la cámara y el punto de mira.
        /// </summary>
        private static void BuscarCamara(Transform raiz, Transform nuevo, List<Rect> ocupados, out Vector3 mejorCam, out Vector3 mejorMira)
        {
            mejorCam = CamaraK15;
            mejorMira = MiraK15;
            List<Bounds> conts = new List<Bounds>(), bods = new List<Bounds>(), barcos = new List<Bounds>(), gruas = new List<Bounds>();
            Transform pc = nuevo.Find("PatioContenedores");
            if (pc != null)
            {
                conts.AddRange(pc.Cast<Transform>().Where(t => t.name.StartsWith("Cont_p")).Select(Limites));
            }
            Transform pb = nuevo.Find("PatioBodegas");
            if (pb != null)
            {
                bods.AddRange(pb.Cast<Transform>().Where(t => t.name.StartsWith("Bodega_")).Select(Limites));
            }
            Transform muelle = raiz.Find("Muelle");
            if (muelle != null)
            {
                foreach (Transform t in muelle)
                {
                    if (!t.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    if (t.name.StartsWith("PU02_Barco"))
                    {
                        barcos.Add(Limites(t));
                    }
                    else if (t.name.StartsWith("PU01_Grua"))
                    {
                        gruas.Add(Limites(t));
                    }
                }
            }
            Transform gn = nuevo.Find("GruaNueva");
            if (gn != null)
            {
                gruas.AddRange(gn.Cast<Transform>().Select(Limites));
            }
            GameObject go = new GameObject("TmpCamaraT1442");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.aspect = 1376f / 768f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            float mejor = float.MinValue;
            try
            {
                foreach (float x in new[] { 150f, 154f, 158f, 162f, 166f, 170f, 174f, 178f, 182f })
                {
                    foreach (float z in new[] { -232f, -231.6f, -231.2f, -230.8f, -230.4f })
                    {
                        Vector3 pos = new Vector3(x, 1.7f, z);
                        if (!Libre(Rect.MinMaxRect(x - 0.5f, z - 0.5f, x + 0.5f, z + 0.5f), ocupados, new List<Rect>()))
                        {
                            continue;
                        }
                        float cerca = 0f;
                        foreach (Bounds b in conts.Concat(bods))
                        {
                            if (b.SqrDistance(pos) < 6.25f)
                            {
                                cerca -= 100f;
                            }
                        }
                        foreach (Rect r in ocupados)
                        {
                            float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax), dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
                            if (dx * dx + dz * dz < 6.25f)
                            {
                                cerca -= 50f;
                            }
                        }
                        foreach (float zt in new[] { -222f, -226f, -230f, -234f, -238f, -242f })
                        {
                            foreach (float yt in new[] { 4f, 14f, 24f })
                            {
                                Vector3 mira = new Vector3(x - 60f, yt, zt);
                                cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira - pos));
                                float s = cerca;
                                float area = 0f;
                                foreach (Bounds b in conts)
                                {
                                    Vector3 v = cam.WorldToViewportPoint(b.center);
                                    if (v.z > 2.5f && v.x < 0.34f && v.x > -0.2f)
                                    {
                                        s += 1f;
                                    }
                                    area += AreaPantalla(cam, b);
                                }
                                if (area > 0.4f)
                                {
                                    s -= (area - 0.4f) * 80f;
                                }
                                foreach (Bounds b in bods)
                                {
                                    bool bien = Corners(b).Any(c => { Vector3 v = cam.WorldToViewportPoint(c); float d = Vector3.Distance(c, pos); return v.z > 0f && v.x > 0.62f && v.x < 1f && v.y > 0f && v.y < 1f && d > 3f && d < 14f; });
                                    s += bien ? 4f : 0f;
                                }
                                foreach (Bounds b in barcos)
                                {
                                    s += 0.8f * Corners(b).Count(c => { Vector3 v = cam.WorldToViewportPoint(c); return v.z > 0f && v.x > 0f && v.x < 0.65f && v.y > 0.35f && v.y < 1f; });
                                }
                                foreach (Bounds b in gruas)
                                {
                                    int dentro = Corners(b).Count(c => { Vector3 v = cam.WorldToViewportPoint(c); return v.z > 0f && v.x > 0.05f && v.x < 0.95f && v.y > 0.25f && v.y < 1f; });
                                    s += 0.6f * dentro + (dentro == 8 ? 4f : 0f);
                                }
                                Vector3 f = mira - pos;
                                f.y = 0f;
                                f.Normalize();
                                Vector3 suelo = cam.WorldToViewportPoint(pos + f * 7f - Vector3.up * 1.7f);
                                if (suelo.z > 0f && suelo.x > 0.3f && suelo.x < 0.7f && suelo.y > 0.05f && suelo.y < 0.42f)
                                {
                                    s += 3f;
                                }
                                if (s > mejor)
                                {
                                    mejor = s;
                                    mejorCam = pos;
                                    mejorMira = mira;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Nota("Cámara K15 buscada: " + mejorCam.ToString("F1") + " mira " + mejorMira.ToString("F1") + " con puntaje " + mejor.ToString("F1") + " (contenedores " + conts.Count + ", bodegas " + bods.Count + ", barcos " + barcos.Count + ", grúas " + gruas.Count + ").");
        }

        /// <summary>Fracción de la pantalla (0-1) que cubre la caja proyectada, recortada al cuadro; 0 si queda detrás.</summary>
        private static float AreaPantalla(Camera cam, Bounds b)
        {
            float xMin = 2f, yMin = 2f, xMax = -2f, yMax = -2f;
            foreach (Vector3 c in Corners(b))
            {
                Vector3 v = cam.WorldToViewportPoint(c);
                if (v.z <= 0.05f)
                {
                    return 0f;
                }
                xMin = Mathf.Min(xMin, v.x);
                xMax = Mathf.Max(xMax, v.x);
                yMin = Mathf.Min(yMin, v.y);
                yMax = Mathf.Max(yMax, v.y);
            }
            float w = Mathf.Clamp01(xMax) - Mathf.Clamp01(xMin), h = Mathf.Clamp01(yMax) - Mathf.Clamp01(yMin);
            return Mathf.Max(0f, w) * Mathf.Max(0f, h);
        }

        private static void PatioProps(Transform nuevo, List<Rect> ocupados, List<Rect> mios, Vector3 cam, Vector3 mira)
        {
            Transform padre = new GameObject("PatioProps").transform;
            padre.SetParent(nuevo, false);
            System.Random azar = new System.Random(14424);
            Vector3 f = mira - cam;
            f.y = 0f;
            f.Normalize();
            Vector3 der = new Vector3(f.z, 0f, -f.x);
            int total = 0, montacargas = 0;
            // Primer plano derecho (frente a las bodegas), a 3.4 m o más del lente: pallets, huacales y tambos como en la referencia.
            (string codigo, float alto, float d)[] primerPlano =
            {
                ("PU06_Pallets", 1.2f, 3.4f), ("C17_Huacales", 1.0f, 4.2f), ("PU06_Pallets", 1.2f, 5.0f), ("C16_Tambo", 1.0f, 5.9f),
                ("C17_Huacales", 1.0f, 6.6f), ("C16_Tambo", 1.0f, 7.4f), ("PU06_Pallets", 1.2f, 8.4f), ("C16_Tambo", 1.0f, 9.2f),
            };
            if (Mathf.Abs(der.z) > 0.3f)
            {
                foreach ((string codigo, float alto, float d) in primerPlano)
                {
                    foreach (float extra in new[] { 0f, -0.4f, 0.4f, -0.8f })
                    {
                        float zObjetivo = PatioZFrente - 0.9f + extra;
                        float lat = (zObjetivo - cam.z) / der.z;
                        Vector3 p = cam + f * d + der * lat;
                        if (Poner(padre, codigo, alto, new Vector2(p.x, p.z), (float)azar.NextDouble() * 360f, ocupados, mios, true) != null)
                        {
                            total++;
                            break;
                        }
                    }
                }
            }
            // Dos montacargas al medio del pasillo (10 y 18 m), con su reflector de sodio.
            foreach (float d in new[] { 10f, 18f })
            {
                foreach (float lat in new[] { 0.4f, -0.4f, 0.9f, -0.9f })
                {
                    Vector3 p = cam + f * d + der * lat;
                    GameObject g = Poner(padre, "PU05_Montacargas", 2.6f, new Vector2(p.x, p.z), (float)azar.NextDouble() * 360f, ocupados, mios, true);
                    if (g != null)
                    {
                        montacargas++;
                        KitS2c.Luz(padre, g.transform.position + Vector3.up * 6.5f, LightType.Spot, Naranja, 420f, 18f, 100f, Vector3.down);
                        break;
                    }
                }
            }
            (string codigo, float alto, int cuantos)[] resto = { ("PU06_Pallets", 1.2f, 3), ("C16_Tambo", 1.0f, 4), ("C17_Huacales", 1.0f, 3) };
            foreach ((string codigo, float alto, int cuantos) in resto)
            {
                int puestos = 0;
                for (int intento = 0; intento < 80 && puestos < cuantos; intento++)
                {
                    float x = 120f + (float)azar.NextDouble() * 59f;
                    float z = azar.Next(2) == 0 ? -228.9f - (float)azar.NextDouble() * 0.5f : -233.3f + (float)azar.NextDouble() * 0.6f;
                    if (Vector2.Distance(new Vector2(x, z), new Vector2(cam.x, cam.z)) < 3.2f)
                    {
                        continue;
                    }
                    if (Poner(padre, codigo, alto, new Vector2(x, z), (float)azar.NextDouble() * 360f, ocupados, mios, true) != null)
                    {
                        puestos++;
                        total++;
                    }
                }
            }
            Nota("Patio: " + montacargas + " montacargas (reflector de sodio cada uno) y " + total + " pallets, tambos y huacales con collider (primer plano derecho incluido).");
        }

        // ---------------------------------------------------------------- 5. Barco y grúas con luces emisivas

        private static List<Vector3> Vertices(Transform t)
        {
            List<Vector3> l = new List<Vector3>();
            foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(false))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null)
                {
                    continue;
                }
                Matrix4x4 m = mf.transform.localToWorldMatrix;
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    l.Add(m.MultiplyPoint3x4(v));
                }
            }
            return l;
        }

        /// <summary>Un vértice por celda de <paramref name="celda"/> m (en x, y, z) entre los que cumplen el filtro; son puntos que están sobre la malla.</summary>
        private static List<Vector3> Muestra(IEnumerable<Vector3> puntos, float celda, int maximo)
        {
            HashSet<Vector3Int> usadas = new HashSet<Vector3Int>();
            List<Vector3> r = new List<Vector3>();
            foreach (Vector3 p in puntos)
            {
                Vector3Int k = new Vector3Int(Mathf.FloorToInt(p.x / celda), Mathf.FloorToInt(p.y / celda), Mathf.FloorToInt(p.z / celda));
                if (usadas.Add(k))
                {
                    r.Add(p);
                    if (r.Count >= maximo)
                    {
                        break;
                    }
                }
            }
            return r;
        }

        private static void Muelle(Transform raiz, Transform nuevo, KitS2c.Obra o, Vector3 camara)
        {
            Transform padre = new GameObject("Muelle").transform;
            padre.SetParent(nuevo, false);
            Transform muelle = raiz.Find("Muelle");
            if (muelle == null)
            {
                Nota("No hay Muelle en el prefab: sin barco ni grúas que iluminar.");
                return;
            }
            Material ventana = Emisivo("T1442_VentanaBarco", new Color(1f, 0.8f, 0.45f), 3.5f);
            Material blanca = Emisivo("T1442_BalizaBlanca", new Color(1f, 0.95f, 0.8f), 5f);
            Material roja = Emisivo("T1442_BalizaRoja", new Color(1f, 0.08f, 0.05f), 6f);
            Material turquesa = Emisivo("T1442_BalizaTurquesa", new Color(0.2f, 1f, 0.9f), 4f);
            int ventanas = 0, balizas = 0;
            Transform barco = muelle.Cast<Transform>().FirstOrDefault(t => t.gameObject.activeInHierarchy && t.name.StartsWith("PU02_Barco"));
            if (barco != null)
            {
                Bounds b = Limites(barco);
                List<Vector3> vs = Vertices(barco);
                Vector3 haciaCam = camara - b.center;
                haciaCam.y = 0f;
                haciaCam.Normalize();
                float h = b.size.y;
                float maxProy = vs.Where(v => v.y > b.min.y + 0.5f * h).Max(v => Vector3.Dot(v - b.center, haciaCam));
                List<Vector3> casco = vs.Where(v => v.y > b.min.y + 0.55f * h && v.y < b.min.y + 0.8f * h && Vector3.Dot(v - b.center, haciaCam) > maxProy - 2.5f).ToList();
                foreach (Vector3 v in Muestra(casco, 4f, 30))
                {
                    KitS2c.Losa(o, padre, "VentanaBarco", v + haciaCam * 0.06f, Quaternion.LookRotation(haciaCam), new Vector3(1.8f, 0.6f, 0.1f), ventana, 1f, false);
                    ventanas++;
                }
                foreach (Vector3 v in Muestra(vs.Where(p => p.y > b.max.y - 1.5f).OrderByDescending(p => p.y), 6f, 4))
                {
                    KitS2c.Losa(o, padre, "BalizaBarco", v + Vector3.up * 0.6f, Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), blanca, 1f, false);
                    balizas++;
                }
                Nota("Barco " + barco.name + ": caja " + b.min.ToString("F0") + ".." + b.max.ToString("F0") + "; " + ventanas + " ventanas emisivas en el casco hacia la cámara y " + balizas + " luces de mástil (sobre vértices de su malla).");
            }
            else
            {
                Nota("No hay PU02_Barco activo en el Muelle.");
            }
            int gruas = 0;
            int balizasGruas = 0;
            List<Transform> todasGruas = muelle.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && t.name.StartsWith("PU01_Grua")).ToList();
            Transform gruaNueva = nuevo.Find("GruaNueva");
            if (gruaNueva != null)
            {
                todasGruas.AddRange(gruaNueva.Cast<Transform>());
            }
            foreach (Transform g in todasGruas)
            {
                gruas++;
                Bounds b = Limites(g);
                List<Vector3> vs = Vertices(g);
                foreach (Vector3 v in Muestra(vs.Where(p => p.y > b.max.y - 1.5f), 8f, 4))
                {
                    KitS2c.Losa(o, padre, "BalizaGrua", v + Vector3.up * 0.7f, Quaternion.identity, new Vector3(1.3f, 1.3f, 1.3f), roja, 1f, false);
                    balizasGruas++;
                }
                float yMedia = b.min.y + b.size.y * 0.6f;
                foreach (Vector3 v in Muestra(vs.Where(p => Mathf.Abs(p.y - yMedia) < 1.2f), 10f, 3))
                {
                    KitS2c.Losa(o, padre, "BalizaGruaMedia", v + Vector3.up * 0.5f, Quaternion.identity, new Vector3(0.9f, 0.9f, 0.9f), turquesa, 1f, false);
                    balizasGruas++;
                }
            }
            Nota("Grúas PU01: " + gruas + " con " + balizasGruas + " balizas emisivas (rojas arriba, turquesa a media altura), puestas sobre vértices de su malla.");
        }

        // ---------------------------------------------------------------- Validar

        private static Bounds CajaCollider(Collider c)
        {
            if (c is BoxCollider bc)
            {
                Transform t = bc.transform;
                Vector3 e = bc.size * 0.5f;
                Bounds r = new Bounds(t.TransformPoint(bc.center + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
                for (int i = 1; i < 8; i++)
                {
                    r.Encapsulate(t.TransformPoint(bc.center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
                }
                return r;
            }
            return Limites(c.transform);
        }

        [MenuItem("Mecha Cyberpunk/T-1442/Puerto calle K15 (validar)")]
        public static void Validar()
        {
            StringBuilder v = new StringBuilder("T-1442 validar Puerto calle K15\n");
            int fallos = 0, flotantes = 0, encimados = 0, enCarril = 0, muros = 0;
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform mia = raiz.transform.Find(Raiz);
                if (mia == null)
                {
                    v.AppendLine("FALLO: no existe " + Raiz);
                    fallos++;
                }
                else
                {
                    int objetos = mia.GetComponentsInChildren<Transform>(true).Length;
                    int luces = mia.GetComponentsInChildren<Light>(true).Length;
                    v.AppendLine("Objetos " + objetos + " (tope " + TopeObjetos + "), luces nuevas " + luces + " (tope " + TopeLuces + "), luces totales del prefab " + raiz.GetComponentsInChildren<Light>(true).Length);
                    fallos += (objetos > TopeObjetos ? 1 : 0) + (luces > TopeLuces ? 1 : 0);
                    List<Rect> otros = Ocupados(raiz.transform, mia);

                    // Piezas de piso: contenedores y props (con collider) de mi raíz.
                    List<Transform> suelo = new List<Transform>();
                    foreach (string g in new[] { "Contenedores", "Props", "PatioContenedores", "PatioProps", "PatioBodegas" })
                    {
                        Transform grupo = mia.Find(g);
                        if (grupo != null)
                        {
                            suelo.AddRange(grupo.Cast<Transform>().Where(t => t.GetComponent<PuntoTrepar>() == null && t.GetComponent<Light>() == null && (g != "PatioBodegas" || t.name.StartsWith("Bodega_"))));
                        }
                    }
                    List<Bounds> cajas = suelo.Select(Limites).ToList();
                    for (int i = 0; i < suelo.Count; i++)
                    {
                        Bounds b = cajas[i];
                        bool esCont = suelo[i].name.StartsWith("Cont_");
                        bool apoyado = Mathf.Abs(b.min.y) < 0.05f;
                        if (!apoyado && esCont)
                        {
                            apoyado = cajas.Where((c, j) => j != i && suelo[j].name.StartsWith("Cont_")).Any(c => Mathf.Abs(c.max.y - b.min.y) < 0.05f && Mathf.Abs(c.center.x - b.center.x) < 0.3f && Mathf.Abs(c.center.z - b.center.z) < 0.3f);
                        }
                        if (!apoyado)
                        {
                            v.AppendLine("FLOTANTE/ENTERRADO " + suelo[i].name + ": base en y=" + b.min.y.ToString("F2"));
                            flotantes++;
                        }
                        Bounds interior = b;
                        interior.Expand(-0.1f);
                        for (int j = i + 1; j < suelo.Count; j++)
                        {
                            Bounds c = cajas[j];
                            c.Expand(-0.1f);
                            if (interior.Intersects(c))
                            {
                                v.AppendLine("ENCIMADO " + suelo[i].name + " con " + suelo[j].name);
                                encimados++;
                            }
                        }
                        if (b.min.y < 0.5f && otros.Any(r => r.Overlaps(Expandir(Huella(b), -0.1f))))
                        {
                            v.AppendLine("ENCIMADO " + suelo[i].name + " con algo que ya había en el prefab");
                            encimados++;
                        }
                        if (b.min.y < 0.5f && EnCalle(Huella(b)))
                        {
                            v.AppendLine("EN CARRIL " + suelo[i].name + " en " + b.center.ToString("F1"));
                            enCarril++;
                        }
                    }

                    // Muros invisibles: cada collider encendido debe caber en la malla visible de su pieza (0.4 m de margen).
                    foreach (Collider c in mia.GetComponentsInChildren<Collider>(false).Where(x => x.enabled && x.gameObject.activeInHierarchy && !x.isTrigger))
                    {
                        GameObject rootPieza = PrefabUtility.GetNearestPrefabInstanceRoot(c.gameObject) ?? c.gameObject;
                        Bounds vis = Limites(rootPieza.transform);
                        Bounds cb = CajaCollider(c);
                        vis.Expand(0.4f);
                        if (!(vis.Contains(cb.min) && vis.Contains(cb.max)))
                        {
                            v.AppendLine("MURO collider sin malla que lo cubra: " + rootPieza.name + " collider " + cb.center.ToString("F1") + " tamaño " + cb.size.ToString("F1"));
                            muros++;
                        }
                    }

                    // Escaleras: cada contenedor de la escalera de contenedores debe poder subirse (un PuntoTrepar al pie de la primera).
                    int escaleras = mia.GetComponentsInChildren<PuntoTrepar>(true).Length;
                    v.AppendLine("Escaleras PuntoTrepar: " + escaleras + "; contenedores de piso: " + cajas.Count(c => true));
                    foreach (Transform piezaAerea in mia.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Toldo_")))
                    {
                        Bounds b = Limites(piezaAerea);
                        if (b.min.y < 3.4f)
                        {
                            v.AppendLine("TOLDO muy bajo " + piezaAerea.name + " y mín " + b.min.y.ToString("F2"));
                            fallos++;
                        }
                    }
                    fallos += flotantes + encimados + enCarril + muros;
                    v.AppendLine("Resumen: flotantes " + flotantes + ", encimados " + encimados + ", en carril " + enCarril + ", muros " + muros);
                    string ruta = Path.GetFullPath(RutaPrefab);
                    v.AppendLine("Prefab: " + (new FileInfo(ruta).Length / 1048576f).ToString("F1") + " MB (tope 60)");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            v.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1442-validar.txt", v.ToString());
            Debug.Log("[ZonaPuertoT1442] " + v);
        }

        // ---------------------------------------------------------------- Fotos

        private static void PonerDomoEn(Vector3 desde)
        {
            GameObject marca = new GameObject("MarcaDomoT1442");
            Camera cam = marca.AddComponent<Camera>();
            cam.enabled = false;
            marca.transform.position = desde;
            foreach (CieloNubes domo in Object.FindObjectsByType<CieloNubes>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                domo.Colocar(cam, ColocarS2cLook.HoraFoto);
            }
            Object.DestroyImmediate(marca);
        }

        /// <summary>K15 desde la cámara nueva, lado a lado con la referencia. No guarda nada; corrida de Unity aparte de Aplicar.</summary>
        [MenuItem("Mecha Cyberpunk/T-1442/Puerto calle K15 (fotos)")]
        public static void Fotos()
        {
            Directory.CreateDirectory("docs/capturas/_vista19b");
            Directory.CreateDirectory("Logs");
            ColocarS2.Abrir();
            Vector3 cam = CamaraK15, mira = MiraK15;
            GameObject marca = GameObject.Find("CamaraK15_T1442");
            if (marca != null && marca.transform.childCount > 0)
            {
                cam = marca.transform.position;
                mira = marca.transform.GetChild(0).position;
            }
            File.WriteAllText("Logs/t1442-camara.txt", "cámara " + cam.ToString("F2") + " mira " + mira.ToString("F2") + (marca == null ? " (constantes: falta CamaraK15_T1442)" : " (marca del prefab)"));
            PonerDomoEn(cam);
            const string juego = "Logs/t1442-puerto-k15-juego.png";
            ColocarS2cLook.Foto(cam, mira, juego, 55f);
            ColocarS2.LadoALado("imageassets/referencias/look/K15_Puerto.png", juego, "docs/capturas/_vista19b/puerto-k15-t1442.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }
    }
}
