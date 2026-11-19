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
        /// Cámara propia de K15 (T-1442): ojos a 1.7 m en la calle de x = 160, junto a la pared de contenedores, mirando por el patio hacia el barco y las grúas del muelle.
        /// La de T1396 estaba a 2.3 m y 84 m de la esquina; el muelle queda a ~140 m: las grúas (40 m) y el barco se ven a esa distancia sin mover nada del prefab.
        /// </summary>
        public static readonly Vector3 CamaraK15 = new Vector3(158.6f, 1.7f, -100f);
        public static readonly Vector3 MiraK15 = new Vector3(168f, 13f, -255f);

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

                Escaleras(nuevo, ocupados, mios);
                Bodegas(raiz.transform, nuevo, o);
                Charcos(nuevo, o, ocupados, mios);
                Props(nuevo, ocupados, mios);
                Gente(nuevo, ocupados, mios);
                Muelle(raiz.transform, nuevo, o);

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

        private static void Escalera(Transform padre, string nombre, Vector3 pie, float alto)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(pie, Quaternion.LookRotation(Vector3.back));
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
                if (luces < 4)
                {
                    KitS2c.FocoPared(o, padre, new Vector3(xw + 0.1f, 3.5f, zc + 3.3f), Quaternion.LookRotation(Vector3.left));
                    Light l = UltimaLuz(padre.GetChild(padre.childCount - 1));
                    if (l != null)
                    {
                        l.color = Naranja;
                    }
                    luces++;
                }
                Color neon = i % 2 == 0 ? KitS2c.Turquesa : KitS2c.Magenta;
                KitS2c.Neon(o, padre, new Vector3(xw, 3.2f, zc + (i % 2 == 0 ? 5.2f : -5.2f)), Vector3.right, Vector3.back, neon, true);
                Transform neonObj = padre.GetChild(padre.childCount - 1);
                foreach (Light l in neonObj.GetComponentsInChildren<Light>(true).ToList())
                {
                    Object.DestroyImmediate(l.gameObject); // el letrero queda solo con su emisión (el tope de luces es de 8)
                }
            }
            Nota("Bodegas del lado oeste con detalle: " + bodegas.Count + " (toldo de lámina, lámpara emisiva, neón emisivo sin luz; " + luces + " focos de pared con luz).");
        }

        // ---------------------------------------------------------------- 3. Charcos con reflejo

        private static void Charcos(Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("Charcos").transform;
            padre.SetParent(nuevo, false);
            Mesh quad = new Mesh { name = Raiz + "_Charco" };
            quad.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) });
            quad.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            quad.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            quad.RecalculateBounds();
            o.Mallas.Add(quad);
            int puestos = 0;
            for (int i = 0; i < 60 && puestos < 26; i++)
            {
                Vector3 p = new Vector3(o.Rango(154.6f, 165.4f), 0f, o.Rango(-88f, -205f));
                float largo = o.Rango(1.8f, 4.8f), anchoC = o.Rango(1.2f, 3.0f), giro = o.Rango(0f, 180f);
                Rect h = Rect.MinMaxRect(p.x - anchoC * 0.5f, p.z - largo * 0.5f, p.x + anchoC * 0.5f, p.z + largo * 0.5f);
                if (!Libre(h, ocupados, mios))
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
            Nota("Charcos con reflejo (shader MechaCyberpunk/Mundo/Charco y materiales S2c_Charco_0..3 del proyecto, sin editarlos): " + puestos);
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
            // Dos reflectores de sodio sobre los montacargas más cercanos a la cámara (luces reales, sin sombras).
            foreach (Vector3 m in montacargas.OrderByDescending(v => v.z).Take(2))
            {
                KitS2c.Luz(padre, m + new Vector3(0f, 6.5f, 0f), LightType.Spot, Naranja, 420f, 18f, 100f, Vector3.down);
            }
            Nota("Props de piso (montacargas, pallets, tambos, huacales) al borde de la calle (fuera de los 8.4 m centrales): " + total + "; reflectores de sodio: " + Mathf.Min(2, montacargas.Count));
        }

        private static void Gente(Transform nuevo, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("Gente").transform;
            padre.SetParent(nuevo, false);
            System.Random azar = new System.Random(14422);
            string[] modelos =
            {
                "NP11_Estibador/NP11_Estibador_a", "NP11_Estibador/NP11_Estibador_b", "NP11_Estibador/NP11_Estibador_a", "NP11_Estibador/NP11_Estibador_b",
                "NP11_Estibador/NP11_Estibador_a", "NP11_Estibador/NP11_Estibador_b", "NP13_Repartidor/NP13_Repartidor_a", "NP07_Pescador/NP07_Pescador_b",
            };
            int puestos = 0;
            foreach (string m in modelos)
            {
                string ruta = "Assets/Arte/Personajes/" + m;
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta + ".fbx");
                RuntimeAnimatorController ctl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ruta + ".controller");
                if (fbx == null)
                {
                    Nota("FALTA " + ruta + ".fbx: se omite.");
                    continue;
                }
                for (int intento = 0; intento < 40; intento++)
                {
                    bool oeste = azar.Next(2) == 0;
                    float x = oeste ? 154.7f + (float)azar.NextDouble() * 1.2f : 164.2f + (float)azar.NextDouble() * 1.4f;
                    float z = -88f - (float)azar.NextDouble() * 110f;
                    Rect h = Rect.MinMaxRect(x - 0.4f, z - 0.4f, x + 0.4f, z + 0.4f);
                    if (EnCalle(h) || !Libre(h, ocupados, mios))
                    {
                        continue;
                    }
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, padre);
                    go.name = Path.GetFileName(ruta) + "_" + puestos;
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
                    else
                    {
                        Nota("AVISO: sin controller para " + ruta);
                    }
                    mios.Add(h);
                    puestos++;
                    break;
                }
            }
            Nota("Gente (sin collider, con su Animator, pivote en los pies a y = 0): " + puestos + " de " + modelos.Length);
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

        private static void Muelle(Transform raiz, Transform nuevo, KitS2c.Obra o)
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
                Vector3 haciaCam = CamaraK15 - b.center;
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
            foreach (Transform g in muelle.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && t.name.StartsWith("PU01_Grua")))
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
                    foreach (string g in new[] { "Contenedores", "Props" })
                    {
                        Transform grupo = mia.Find(g);
                        if (grupo != null)
                        {
                            suelo.AddRange(grupo.Cast<Transform>().Where(t => t.GetComponent<PuntoTrepar>() == null));
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
            PonerDomoEn(CamaraK15);
            const string juego = "Logs/t1442-puerto-k15-juego.png";
            ColocarS2cLook.Foto(CamaraK15, MiraK15, juego, 55f);
            ColocarS2.LadoALado("imageassets/referencias/look/K15_Puerto.png", juego, "docs/capturas/_vista19b/puerto-k15-t1442.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }
    }
}
