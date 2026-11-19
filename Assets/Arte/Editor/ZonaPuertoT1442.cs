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
        public static readonly Vector3 CamaraK15 = new Vector3(160f, 1.7f, -150f);
        public static readonly Vector3 MiraK15 = new Vector3(150f, 12f, -260f);
        private const float PatioZSur = -237.0f, PatioZNorte = -205.0f, PatioXAtras = 118.0f, PatioXPilas = 150.3f;

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

        private static bool EnCalle(Rect h, float media = MediaCalle)
        {
            Vector2[] puntos = { h.center, new Vector2(h.xMin, h.yMin), new Vector2(h.xMax, h.yMin), new Vector2(h.xMin, h.yMax), new Vector2(h.xMax, h.yMax) };
            foreach (Vector3[] calle in ColocarT1357Puerto.Calles())
            {
                foreach (Vector2 p in puntos)
                {
                    if (ColocarT1357Puerto.DistanciaACalle(new Vector3(p.x, 0f, p.y), calle) < media)
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
                RestaurarApartados(raiz.transform);
                int lucesAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                Transform nuevo = new GameObject(Raiz).transform;
                nuevo.SetParent(raiz.transform, false);
                KitS2c.Obra o = new KitS2c.Obra { Raiz = nuevo, Azar = new System.Random(14420), Nombre = Raiz };
                List<Rect> ocupados = Ocupados(raiz.transform, nuevo);
                List<Rect> mios = new List<Rect>();
                Nota("Huellas ya ocupadas en el prefab (T1357..T1443): " + ocupados.Count);

                // Ronda 5: el cuadro fijo de K15 en la avenida (cruce x 160, z -150, mirando al sur). Se quitan el patio oeste y la busqueda de camara.
                Vector3 cam = CamaraK15, mira = MiraK15;
                Transform marca = new GameObject("CamaraK15_T1442").transform;
                marca.SetParent(nuevo, false);
                marca.position = cam;
                Transform marcaMira = new GameObject("Mira").transform;
                marcaMira.SetParent(marca, true);
                marcaMira.position = mira;
                Escaleras(nuevo, ocupados, mios, "Contenedores", XPared, -86f, -195f, 3, new[] { new[] { 1, 2, 3 }, new[] { 1, 2 } }, -300f);
                PilaEsquina(nuevo, ocupados, mios);
                Bodegas(raiz.transform, nuevo, o);
                PostesAvenida(raiz.transform, nuevo, o, ocupados, mios);
                Charcos(nuevo, o, ocupados, mios, "CharcosCalle", Rect.MinMaxRect(155f, -215f, 165f, -152f), 20, new Vector2(4f, 9f), new Vector2(2f, 4f), false, new List<float> { 155.2f, 156.5f, 164.5f }, false);
                CharcosLuz(nuevo, o);
                Props(nuevo, ocupados, mios, cam);
                PrimerPlanoDerecho(raiz.transform, nuevo, ocupados, mios, cam);
                Gente(nuevo, "Gente", 7, ocupados, mios, new System.Random(14422), false);
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

        private static void Escaleras(Transform nuevo, List<Rect> ocupados, List<Rect> mios, string grupo, float xMin, float zInicio, float zFin, int maxUnidades, int[][] patrones, float zMinimo)
        {
            Transform padre = new GameObject(grupo).transform;
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
            float zn = zInicio;
            while (zn > zFin && unidades < maxUnidades)
            {
                bool puesta = false;
                foreach (int[] alturas in patrones)
                {
                    List<Rect> rects = new List<Rect>();
                    for (int i = 0; i < alturas.Length; i++)
                    {
                        float zNorte = zn - i * (largo + Hueco);
                        rects.Add(Rect.MinMaxRect(xMin, zNorte - largo, xMin + ancho, zNorte));
                    }
                    Rect escalera = Rect.MinMaxRect(xMin, zn, xMin + ancho, zn + 0.95f);
                    if (rects.Any(r => !Libre(r, ocupados, mios) || EnCalle(r) || r.yMin < zMinimo) || !Libre(escalera, ocupados, mios))
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
                            GameObject g = PonerCont(padre, prefabs[color % prefabs.Count], "Cont_" + unidades + "_" + i + "_" + n, xMin, zc, yBase);
                            color++;
                            contenedores++;
                            yBase = Limites(g.transform).max.y;
                            hechos.Add(g);
                        }
                        mios.Add(rects[i]);
                    }
                    mios.Add(escalera);
                    float xc = xMin + ancho * 0.5f;
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

        // ---------------------------------------------------------------- Ronda 5: el cuadro fijo de K15 en la avenida (x = 160, mirando al sur)

        // ---------------------------------------------------------------- Ronda 6

        /// <summary>Puntos (x, 0, z) de las lámparas, focos, neones y postes del cuadro de K15: los charcos con reflejo van debajo y delante de ellos.</summary>
        private static readonly List<Vector3> puntosLuz = new List<Vector3>();

        /// <summary>
        /// Material propio de los charcos de T-1442: el mismo shader MechaCyberpunk/Mundo/Charco y las mismas máscaras que los S2c_Charco_0..3 compartidos (que no se editan),
        /// pero con el reflejo al máximo (_Intensidad 2, _FresnelMin 0.85, _Opacidad 1, _ReflejoBorde 0.5) para que reflejen las lámparas y los neones.
        /// </summary>
        private static Material MatCharco(int forma)
        {
            return Mat("T1442_Charco_" + forma, () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/Charco"));
                m.SetTexture("_Mascara", KitS2c.TexCharco(forma));
                m.SetFloat("_Intensidad", 2f);
                m.SetFloat("_FresnelMin", 0.85f);
                m.SetFloat("_Opacidad", 1f);
                m.SetFloat("_ReflejoBorde", 0.5f);
                return m;
            });
        }

        private static void CharcosLuz(Transform nuevo, KitS2c.Obra o)
        {
            Transform padre = new GameObject("CharcosLuz").transform;
            padre.SetParent(nuevo, false);
            Mesh quad = new Mesh { name = Raiz + "_CharcosLuz" };
            quad.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) });
            quad.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            quad.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            quad.RecalculateBounds();
            o.Mallas.Add(quad);
            int puestos = 0;
            foreach (Vector3 p in puntosLuz)
            {
                float hacia = p.x < 160f ? 1f : -1f; // hacia el centro de la calle: el reflejo cae debajo y delante de la luz
                foreach ((float dx, float largo, float ancho) in new[] { (2.0f, 4.5f, 2.6f), (4.6f, 6.5f, 3.2f) })
                {
                    Vector3 c = new Vector3(p.x + hacia * dx, 0f, p.z + o.Rango(-0.6f, 0.6f));
                    if (c.x < 154.2f || c.x > 165.8f)
                    {
                        continue;
                    }
                    GameObject q = new GameObject("CharcoLuz");
                    q.layer = ReflejoCharcos.CapaCharcos;
                    q.transform.SetParent(padre, false);
                    q.transform.position = c + Vector3.up * 0.012f;
                    q.transform.rotation = Quaternion.Euler(90f, o.Rango(-12f, 12f), 0f);
                    q.transform.localScale = new Vector3(ancho, largo, 1f);
                    q.AddComponent<MeshFilter>().sharedMesh = quad;
                    MeshRenderer mr = q.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = MatCharco(o.Azar.Next(4));
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    q.AddComponent<CharcoVisible>();
                    puestos++;
                }
            }
            Nota("CharcosLuz: " + puestos + " charcos con reflejo debajo y delante de " + puntosLuz.Count + " lámparas, focos, neones y postes del cuadro (material T1442_Charco_0..3: shader de charco del proyecto con el reflejo al máximo).");
        }

        /// <summary>Letrero de neón de bandera grande (1.9 m en su lado mayor), de cara a la avenida (girado 31 grados hacia la cámara, al norte) y solo con emisión (sin luz).</summary>
        private static void NeonGrande(KitS2c.Obra o, Transform padre, Vector3 pared, Vector3 normal, Color color, float lado)
        {
            o.Neones++;
            string codigo = color == KitS2c.Magenta ? KitS2c.NeonMagenta : KitS2c.NeonTurquesa;
            KitS2c.Medida m = KitS2c.Medir(codigo);
            float escala = lado / Mathf.Max(0.01f, Mathf.Max(m.Caja.size.x, m.Caja.size.y));
            Vector3 hacia = (normal + Vector3.forward * 0.6f).normalized;
            Quaternion giro = Quaternion.LookRotation(-hacia);
            Vector3 centro = pared + normal * 0.6f;
            GameObject raiz = new GameObject("Neon");
            raiz.transform.SetParent(padre, false);
            raiz.transform.SetPositionAndRotation(centro, giro);
            KitS2c.Pieza(raiz.transform, codigo, centro - Vector3.up * (m.Caja.size.y * escala * 0.5f), giro, Vector3.one * escala, false).name = "Letrero";
        }

        /// <summary>Tarima de madera apoyada contra la fachada (inclinada 70 grados, con la punta alta tocando la pared), con collider.</summary>
        private static GameObject PonerTarima(Transform padre, float xPared, float z, List<Rect> ocupados, List<Rect> mios)
        {
            GameObject prefab = BuscarPrefab("PU06_Pallets");
            if (prefab == null)
            {
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = "Tarima_" + padre.childCount;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 0f, -70f));
            go.transform.localScale = Vector3.one;
            Bounds b = Limites(go.transform);
            go.transform.localScale = Vector3.one * (1.6f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.y, b.size.z)));
            b = Limites(go.transform);
            go.transform.position += new Vector3(xPared + 0.03f - b.min.x, -b.min.y, z - b.center.z);
            Rect h = Huella(Limites(go.transform));
            if (EnCalle(h) || !Libre(h, ocupados, mios))
            {
                Object.DestroyImmediate(go);
                return null;
            }
            BugsZonasT1410.AsegurarColliderYBarril(go);
            mios.Add(h);
            return go;
        }

        /// <summary>Dos pilas de pallets una sobre otra (la de arriba apoyada en la de abajo, mismo centro).</summary>
        private static bool PonerApilado(Transform padre, Vector2 xz, int numero, List<Rect> ocupados, List<Rect> mios)
        {
            GameObject baseP = Poner(padre, "PU06_Pallets", 1.2f, xz, 20f * numero, ocupados, mios, true);
            if (baseP == null)
            {
                return false;
            }
            baseP.name = "Apil_base_" + numero;
            GameObject prefab = BuscarPrefab("PU06_Pallets");
            GameObject top = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            top.name = "Apil_top_" + numero;
            top.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 20f * numero + 35f, 0f));
            top.transform.localScale = Vector3.one;
            Bounds b = Limites(top.transform);
            top.transform.localScale = Vector3.one * (1.0f / Mathf.Max(0.01f, b.size.y));
            b = Limites(top.transform);
            Bounds bb = Limites(baseP.transform);
            top.transform.position += new Vector3(bb.center.x - b.center.x, bb.max.y - b.min.y, bb.center.z - b.center.z);
            BugsZonasT1410.AsegurarColliderYBarril(top);
            return true;
        }

        /// <summary>
        /// Primer plano derecho de la referencia: tarima de madera inclinada apoyada contra la bodega más cercana, huacales y tambos junto a ella y pallets apilados,
        /// a 5-12 m del lente. Va en un grupo propio (PrimerPlano) que Validar revisa como Props.
        /// </summary>
        private static void PrimerPlanoDerecho(Transform raiz, Transform nuevo, List<Rect> ocupados, List<Rect> mios, Vector3 cam)
        {
            Transform padre = new GameObject("PrimerPlano").transform;
            padre.SetParent(nuevo, false);
            Transform grupo = raiz.Find("Bodegas");
            Transform cercana = grupo == null ? null : grupo.Cast<Transform>()
                .Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")))
                .Where(t => { Bounds b = Limites(t); return b.max.x > 148f && b.max.x < 158f && b.max.z > -215f && b.min.z < -150f; })
                .OrderBy(t => Vector2.Distance(new Vector2(Limites(t).max.x, Limites(t).center.z), new Vector2(cam.x, cam.z))).FirstOrDefault();
            if (cercana == null)
            {
                Nota("PrimerPlano: no hay bodega PU03/PU04 cerca del cruce; se omite.");
                return;
            }
            float xw = Limites(cercana).max.x;
            System.Random azar = new System.Random(14425);
            float zTarima = float.NaN;
            foreach (float z in new[] { -155f, -156.5f, -158f, -153.8f, -160f, -161.5f })
            {
                if (PonerTarima(padre, xw, z, ocupados, mios) != null)
                {
                    zTarima = z;
                    break;
                }
            }
            int huacales = 0, tambos = 0, apilados = 0;
            float zBase = float.IsNaN(zTarima) ? -157f : zTarima;
            foreach (float dz in new[] { 1.5f, -1.5f, 3f, -3f })
            {
                if (huacales < 3 && Poner(padre, "C17_Huacales", 1.0f, new Vector2(xw + 1.3f, zBase + dz), (float)azar.NextDouble() * 360f, ocupados, mios, true) != null)
                {
                    huacales++;
                }
            }
            foreach (float dz in new[] { 4.2f, -4.2f, 5.5f, -5.5f })
            {
                if (tambos < 2 && Poner(padre, "C16_Tambo", 1.0f, new Vector2(xw + 1.2f, zBase + dz), (float)azar.NextDouble() * 360f, ocupados, mios, true) != null)
                {
                    tambos++;
                }
            }
            foreach (float z in new[] { -153.2f, -163.5f, -165f, -167f, -169f })
            {
                if (apilados < 2 && PonerApilado(padre, new Vector2(xw + 1.6f, z), apilados, ocupados, mios))
                {
                    apilados++;
                }
            }
            Nota("PrimerPlano (junto a " + cercana.name + ", pared en x " + xw.ToString("F1") + "): tarima apoyada " + (float.IsNaN(zTarima) ? "NO cupo" : "en z " + zTarima.ToString("F1")) + ", " + huacales + " huacales, " + tambos + " tambos y " + apilados + " pilas de pallets apilados.");
        }

        private static bool FueraDeAsfalto(Rect h)
        {
            Vector2[] puntos = { h.center, new Vector2(h.xMin, h.yMin), new Vector2(h.xMax, h.yMin), new Vector2(h.xMin, h.yMax), new Vector2(h.xMax, h.yMax) };
            foreach (Vector3[] calle in ColocarT1357Puerto.Calles())
            {
                foreach (Vector2 p in puntos)
                {
                    if (ColocarT1357Puerto.DistanciaACalle(new Vector3(p.x, 0f, p.y), calle) < ColocarT1357Puerto.AnchoCalle * 0.5f + 0.05f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Pila escalonada (1-2-3 de alto) en la esquina sureste del cruce de la cámara: contenedores PU08 con grafiti y PT01 de colores con el eje largo en x, la cara con grafiti al norte
        /// (hacia la cámara), pegada al borde sur de la calle z = -150 (fuera de sus 12 m de asfalto). Escalera al suelo por el norte y una escalera por escalón.
        /// </summary>
        private static void PilaEsquina(Transform nuevo, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("PilaEsquina").transform;
            padre.SetParent(nuevo, false);
            string[] codigos = { "PU08_ContenedorGraffiti_a", "PU08_ContenedorGraffiti_b", "PT01_Contenedor40_Naranja", "PU08_ContenedorGraffiti_a", "PT01_Contenedor40_Azul", "PU08_ContenedorGraffiti_b" };
            List<GameObject> prefabs = codigos.Select(BuscarPrefab).Where(p => p != null).ToList();
            if (prefabs.Count == 0)
            {
                Nota("PilaEsquina: faltan los prefabs de contenedor.");
                return;
            }
            float largo = ColocarT1357Puerto.LargoCont, alto = ColocarT1357Puerto.AltoCont, ancho = ColocarT1357Puerto.AnchoCont;
            int[][] patrones = { new[] { 1, 2, 3 }, new[] { 1, 2 }, new[] { 1 } };
            foreach (float zN in new[] { -156.3f, -156.6f, -157.0f })
            {
                foreach (float x0 in new[] { 166.5f, 167.2f, 168f })
                {
                    foreach (int[] alturas in patrones)
                    {
                        List<Rect> rects = new List<Rect>();
                        for (int i = 0; i < alturas.Length; i++)
                        {
                            float xa = x0 + i * (largo + 0.04f);
                            rects.Add(Rect.MinMaxRect(xa, zN - ancho, xa + largo, zN));
                        }
                        Rect escalera = Rect.MinMaxRect(x0, zN, x0 + largo, zN + 0.95f);
                        if (rects.Any(r => !Libre(r, ocupados, mios) || !FueraDeAsfalto(r)) || !Libre(escalera, ocupados, mios))
                        {
                            continue;
                        }
                        float zc = zN - ancho * 0.5f;
                        int color = 0, total = 0;
                        for (int i = 0; i < alturas.Length; i++)
                        {
                            float yBase = 0f;
                            for (int n = 0; n < alturas[i]; n++)
                            {
                                GameObject g = PonerContX(padre, prefabs[color % prefabs.Count], "Cont_e_" + i + "_" + n, rects[i].center.x, zc, yBase);
                                color++;
                                total++;
                                yBase = Limites(g.transform).max.y;
                            }
                            mios.Add(rects[i]);
                        }
                        mios.Add(escalera);
                        Escalera(padre, "Escalera_Suelo_e", new Vector3(x0 + largo * 0.5f, 0f, zN + 0.3f), alto, Vector3.back);
                        for (int i = 0; i + 1 < alturas.Length; i++)
                        {
                            Escalera(padre, "Escalera_Fila_e_" + i, new Vector3(rects[i].xMax - 0.25f, alturas[i] * alto, zc), alto, Vector3.right);
                        }
                        Nota("PilaEsquina: alturas " + string.Join("-", alturas) + " desde x " + x0 + ", z norte " + zN + " (" + total + " contenedores, grafiti al norte; escaleras al suelo y por escalón).");
                        return;
                    }
                }
            }
            Nota("PilaEsquina: no hay hueco en la esquina sureste del cruce (z -156..-159 junto a los bloques); se omite.");
        }

        /// <summary>Postes de madera con lámpara de sodio en el borde este (a la izquierda de la cámara) entre los que ya hay, con racimos de cables a los vecinos y a las bodegas de enfrente.</summary>
        private static void PostesAvenida(Transform raiz, Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("PostesAvenida").transform;
            padre.SetParent(nuevo, false);
            List<Vector3> puntas = new List<Vector3>();
            foreach (float z0 in new[] { -160f, -181f, -201f })
            {
                foreach (float dz in new[] { 0f, 2f, -2f, 4f, -4f, 6f, -6f })
                {
                    Vector3 pie = new Vector3(165.4f, 0f, z0 + dz);
                    Rect h = Rect.MinMaxRect(pie.x - 0.45f, pie.z - 0.45f, pie.x + 0.45f, pie.z + 0.45f);
                    if (!Libre(h, ocupados, mios))
                    {
                        continue;
                    }
                    puntas.Add(KitS2c.Poste(o, padre, pie, Vector3.left, 0f, true));
                    puntosLuz.Add(pie);
                    mios.Add(h);
                    break;
                }
            }
            List<Vector3> existentes = new List<Vector3>();
            Transform postes = raiz.Find("Postes");
            if (postes != null)
            {
                existentes = postes.Cast<Transform>().Where(t => t.name.StartsWith("Poste_") && t.gameObject.activeInHierarchy).Select(t => t.position + Vector3.up * 6.75f).ToList();
            }
            Transform grupoBodegas = raiz.Find("Bodegas");
            List<Transform> bodegas = grupoBodegas == null ? new List<Transform>()
                : grupoBodegas.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_"))).ToList();
            int cables = 0;
            for (int k = 0; k < puntas.Count; k++)
            {
                if (k + 1 < puntas.Count)
                {
                    foreach (float dy in new[] { 0f, -0.5f, -1f, 0.4f, 0.8f })
                    {
                        KitS2c.Cable(o, puntas[k] + Vector3.up * dy, puntas[k + 1] + Vector3.up * dy, 1.0f);
                        cables++;
                    }
                }
                Vector3 punta = puntas[k];
                foreach (Vector3 q in existentes.Where(p => new Vector2(p.x - punta.x, p.z - punta.z).magnitude < 26f && new Vector2(p.x - punta.x, p.z - punta.z).magnitude > 1f).Take(2))
                {
                    foreach (float dy in new[] { 0f, -0.6f, 0.5f })
                    {
                        KitS2c.Cable(o, punta + Vector3.up * dy, q + Vector3.up * dy, 1.1f);
                        cables++;
                    }
                }
                foreach (Transform b in bodegas)
                {
                    Bounds c = Limites(b);
                    if (c.center.x < 158f && punta.z > c.min.z + 1f && punta.z < c.max.z - 1f && c.max.y > 4.5f)
                    {
                        float yFin = Mathf.Min(6.2f, c.max.y - 0.6f);
                        foreach (float dz in new[] { -0.5f, 0f, 0.6f })
                        {
                            KitS2c.Cable(o, punta + Vector3.forward * dz, new Vector3(c.max.x + 0.08f, yFin, punta.z + dz), 1.1f);
                            cables++;
                        }
                        break;
                    }
                }
            }
            Nota("PostesAvenida: " + puntas.Count + " postes en x 165.4 (borde este) con " + cables + " cables (entre ellos, a los postes que ya había y a las bodegas de enfrente).");
        }

        private static void Bodegas(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            puntosLuz.Clear();
            Transform padre = new GameObject("Bodegas").transform;
            padre.SetParent(nuevo, false);
            Transform grupo = raiz.Find("Bodegas");
            List<Transform> bodegas = grupo == null ? new List<Transform>()
                : grupo.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")))
                    .Where(t => { Bounds b = Limites(t); return b.max.x > 148f && b.max.x < 158f && b.center.z < -84f && b.center.z > -215f; })
                    .OrderByDescending(t => Limites(t).center.z).Take(6).ToList();
            Material toldo = Mat("T1442_Toldo", () => KitS2c.Lit(new Color(0.78f, 0.4f, 0.14f), 0.25f, KitS2c.TexMuro()));
            Material lampara = Emisivo("T1442_LamparaNaranja", Naranja, 4f);
            int focos = 0;
            for (int i = 0; i < bodegas.Count; i++)
            {
                Bounds b = Limites(bodegas[i]);
                float xw = b.max.x, zc = b.center.z;
                bool enVista = zc < -140f && zc > -215f; // las que caen en el cuadro de K15 (cámara en z = -150 mirando al sur)
                float yToldo = Mathf.Clamp(b.size.y * 0.62f, 3.7f, 4.4f);
                KitS2c.Losa(o, padre, "Toldo_" + i, new Vector3(xw + 0.75f, yToldo, zc), Quaternion.Euler(0f, 0f, -11f), new Vector3(1.6f, 0.07f, Mathf.Min(9f, b.size.z * 0.6f)), toldo, 1.5f, false);
                KitS2c.Losa(o, padre, "Lampara_" + i + "_a", new Vector3(xw + 0.3f, yToldo - 0.3f, zc - 3.3f), Quaternion.identity, new Vector3(0.5f, 0.14f, 0.7f), lampara, 1f, false);
                KitS2c.Losa(o, padre, "Lampara_" + i + "_b", new Vector3(xw + 0.3f, yToldo - 0.3f, zc + 3.3f), Quaternion.identity, new Vector3(0.5f, 0.14f, 0.7f), lampara, 1f, false);
                if (enVista)
                {
                    puntosLuz.Add(new Vector3(xw, 0f, zc - 3.3f));
                    puntosLuz.Add(new Vector3(xw, 0f, zc + 3.3f));
                }
                if (enVista && focos < 2)
                {
                    puntosLuz.Add(new Vector3(xw, 0f, zc + 6.5f));
                    KitS2c.FocoPared(o, padre, new Vector3(xw + 0.1f, yToldo - 0.4f, zc + 6.5f), Quaternion.LookRotation(Vector3.left));
                    Light l = UltimaLuz(padre.GetChild(padre.childCount - 1));
                    if (l != null)
                    {
                        l.color = Naranja;
                    }
                    focos++;
                }
                int neones = enVista ? 2 : 1;
                for (int k = 0; k < neones; k++)
                {
                    Color color = (i + k) % 2 == 0 ? KitS2c.Turquesa : KitS2c.Magenta;
                    float zN = k == 0 ? zc + 5.2f : zc - 6.8f;
                    NeonGrande(o, padre, new Vector3(xw, k == 0 ? 3.4f : 3.9f, zN), Vector3.right, color, 1.9f);
                    if (enVista)
                    {
                        puntosLuz.Add(new Vector3(xw, 0f, zN));
                    }
                    foreach (Light ln in padre.GetChild(padre.childCount - 1).GetComponentsInChildren<Light>(true).ToList())
                    {
                        Object.DestroyImmediate(ln.gameObject); // el letrero queda solo con su emisión (el tope de luces es de 8)
                    }
                }
            }
            Nota("Bodegas del lado oeste con detalle: " + bodegas.Count + " (toldo, 2 lámparas emisivas, 1-2 neones emisivos; " + focos + " focos de pared con luz en las del cuadro).");
        }

        // ---------------------------------------------------------------- 3. Charcos con reflejo

        private static void Charcos(Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios, string grupo, Rect region, int cuantos, Vector2 largoRango, Vector2 anchoRango, bool largoEnX, List<float> xSesgo, bool sesgoEnZ = false)
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
                    x = sesgoEnZ ? x : Mathf.Clamp(o.Uno(xSesgo) + o.Rango(-2f, 2f), region.xMin, region.xMax); // donde pega la luz de lámparas y neones
                }
                float zCharco = o.Rango(region.yMin, region.yMax);
                if (sesgoEnZ && xSesgo != null && xSesgo.Count > 0 && puestos % 2 == 0)
                {
                    zCharco = Mathf.Clamp(o.Uno(xSesgo) + o.Rango(-2f, 2f), region.yMin, region.yMax);
                }
                Vector3 p = new Vector3(x, 0f, zCharco);
                float largo = o.Rango(largoRango.x, largoRango.y), anchoC = o.Rango(anchoRango.x, anchoRango.y);
                float giro = largoEnX ? 90f + o.Rango(-15f, 15f) : o.Rango(-15f, 15f);
                float semiX = largoEnX ? largo * 0.5f : anchoC * 0.5f, semiZ = largoEnX ? anchoC * 0.5f : largo * 0.5f;
                Rect h = Rect.MinMaxRect(p.x - semiX, p.z - semiZ, p.x + semiX, p.z + semiZ);
                if (grupo != "CharcosCalle" && !Libre(Expandir(h, -0.4f), ocupados, mios))
                {
                    continue;
                }
                int forma = o.Azar.Next(4);
                Material mat = MatCharco(forma);
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

        private static GameObject Poner(Transform padre, string codigo, float alto, Vector2 xz, float giro, List<Rect> ocupados, List<Rect> mios, bool collider, float mediaCalle = MediaCalle)
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
            if (EnCalle(h, mediaCalle) || !Libre(h, ocupados, mios))
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

        private static void Props(Transform nuevo, List<Rect> ocupados, List<Rect> mios, Vector3 cam)
        {
            Transform padre = new GameObject("Props").transform;
            padre.SetParent(nuevo, false);
            System.Random azar = new System.Random(14421);
            // (código, alto, cuántos, medio ancho de carril libre): los montacargas pueden acercarse un poco más al centro.
            (string codigo, float alto, int cuantos, float media)[] lista =
            {
                ("PU05_Montacargas", 2.6f, 4, MediaCalle), ("PU06_Pallets", 1.2f, 9, MediaCalle), ("C16_Tambo", 1.0f, 9, MediaCalle), ("C17_Huacales", 1.0f, 7, MediaCalle),
            };
            int total = 0;
            List<Vector3> montacargas = new List<Vector3>();
            foreach ((string codigo, float alto, int cuantos, float media) in lista)
            {
                int puestos = 0;
                for (int intento = 0; intento < 120 && puestos < cuantos; intento++)
                {
                    bool oeste = azar.NextDouble() < 0.65; // primer plano derecho, como la referencia
                    float x = oeste ? 154.4f + (float)azar.NextDouble() * 1.8f : 164.0f + (float)azar.NextDouble() * 1.8f;
                    float z = -153f - Mathf.Pow((float)azar.NextDouble(), 1.5f) * 62f; // entre 4 y 70 m de la cámara, más cerca que lejos
                    if (Vector2.Distance(new Vector2(x, z), new Vector2(cam.x, cam.z)) < 4f)
                    {
                        continue;
                    }
                    GameObject g = Poner(padre, codigo, alto, new Vector2(x, z), (float)azar.NextDouble() * 360f, ocupados, mios, true, media);
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
            // Reflector de sodio sobre los dos montacargas más cercanos a la cámara.
            foreach (Vector3 m in montacargas.OrderByDescending(v => v.z).Take(2))
            {
                KitS2c.Luz(padre, m + new Vector3(0f, 6.5f, 0f), LightType.Spot, Naranja, 420f, 18f, 100f, Vector3.down);
            }
            Nota("Props de piso en el cuadro de K15 (montacargas " + montacargas.Count + ", más pallets, tambos y huacales): " + total + " en total, en las orillas de la avenida entre z -153 y -215.");
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
                        x = 126f + (float)azar.NextDouble() * 23f;
                        z = -207f - (float)azar.NextDouble() * 28f;
                    }
                    else
                    {
                        bool oeste = azar.Next(2) == 0;
                        x = oeste ? 154.7f + (float)azar.NextDouble() * 1.2f : 164.2f + (float)azar.NextDouble() * 1.4f;
                        z = -154f - (float)azar.NextDouble() * 56f;
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

        private const string PrefijoApartado = "T1442_Apartado_";

        /// <summary>Vuelve a encender y renombrar lo que una corrida anterior apartó.</summary>
        private static void RestaurarApartados(Transform raiz)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith(PrefijoApartado)).ToList())
            {
                t.name = t.name.Substring(PrefijoApartado.Length);
                t.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Aparta (apaga y renombra con el prefijo T1442_Apartado_, reversible: Aplicar los vuelve a encender antes de rehacer) los props sueltos de T1357/T1387/T1396/T1402
        /// que estorban dentro de la zona del patio. Solo hijos directos de esos grupos: no toca grupos enteros (más de 30 m), contenedores de bloques, el muelle ni nada de T1443.
        /// </summary>
        private static void ApartarEstorbos(Transform raiz, Rect zona)
        {
            int apartados = 0;
            foreach (string g in new[] { "Patio", "Postes", "Entrada", "T1387_Puerto", "T1396_Puerto", "T1402_Puerto" })
            {
                Transform grupo = raiz.Find(g);
                if (grupo == null)
                {
                    continue;
                }
                foreach (Transform h in grupo.Cast<Transform>().ToList())
                {
                    string n = h.name;
                    if (!h.gameObject.activeSelf || n.Contains("Charco") || n == "Cables" || n.StartsWith("Halo") || n.StartsWith("Linea") || h.GetComponent<Light>() != null)
                    {
                        continue;
                    }
                    Bounds b = Limites(h);
                    if (b.size.y < 0.3f || b.size.sqrMagnitude < 0.04f || b.size.x > 30f || b.size.z > 30f || !Huella(b).Overlaps(zona))
                    {
                        continue;
                    }
                    h.name = PrefijoApartado + n;
                    h.gameObject.SetActive(false);
                    apartados++;
                    Nota("Apartado " + g + "/" + n + " en " + b.center.ToString("F1") + " (tamaño " + b.size.ToString("F1") + ").");
                }
            }
            Nota("Props apartados del patio de K15 (reversible, prefijo " + PrefijoApartado + "): " + apartados);
        }

        private static GameObject BodegaEn(Transform padre, GameObject prefab, string nombre, float zCentro, float anchoMax, List<Rect> ocupados, List<Rect> mios, out string bloqueo)
        {
            bloqueo = null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f)); // frente hacia +x (al patio), como la hilera oeste de T1357
            go.transform.localScale = Vector3.one;
            Bounds b0 = Limites(go.transform);
            go.transform.localScale = Vector3.one * Mathf.Min(anchoMax / b0.size.z, 5.5f / b0.size.x);
            Bounds b = Limites(go.transform);
            go.transform.position += new Vector3(PatioXAtras - b.min.x, -b.min.y, zCentro - b.center.z);
            b = Limites(go.transform);
            Rect h = Huella(b);
            Rect dentro = Expandir(h, -0.05f);
            foreach (Rect r in ocupados.Concat(mios))
            {
                if (r.Overlaps(dentro))
                {
                    bloqueo = "z " + zCentro.ToString("F1") + " choca con una huella de centro " + r.center.ToString("F1") + " tamaño " + r.size.ToString("F1");
                    Object.DestroyImmediate(go);
                    return null;
                }
            }
            mios.Add(h);
            return go;
        }

        private static void PatioBodegas(Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios, List<float> zLuces)
        {
            Transform padre = new GameObject("PatioBodegas").transform;
            padre.SetParent(nuevo, false);
            string[] codigos = { "PU03_Bodega", "PU04_Bodega", "PU03_Bodega" };
            GameObject[] prefabs = codigos.Select(BuscarPrefab).ToArray();
            Material toldo = Mat("T1442_Toldo", () => KitS2c.Lit(new Color(0.78f, 0.4f, 0.14f), 0.25f, KitS2c.TexMuro()));
            Material lampara = Emisivo("T1442_LamparaNaranja", Naranja, 4f);
            Material cortina = Mat("T1442_Cortina", () => KitS2c.Lit(new Color(0.42f, 0.46f, 0.5f), 0.45f, KitS2c.TexMuro()));
            int colocadas = 0;
            foreach (float ancho in new[] { 10f, 9f, 8f })
            {
                float zr = PatioZNorte;
                string primerBloqueo = null;
                while (colocadas < 3 && zr - ancho >= PatioZSur)
                {
                    GameObject prefab = prefabs[colocadas % prefabs.Length];
                    if (prefab == null)
                    {
                        Nota("FALTA " + codigos[colocadas % codigos.Length] + ": se omite.");
                        break;
                    }
                    GameObject go = BodegaEn(padre, prefab, "Bodega_" + colocadas, zr - ancho * 0.5f, ancho, ocupados, mios, out string bloqueo);
                    if (go == null)
                    {
                        primerBloqueo = primerBloqueo ?? bloqueo;
                        zr -= 1.5f;
                        continue;
                    }
                    Bounds b = Limites(go.transform);
                    float xf = b.max.x, zc = b.center.z;
                    float yToldo = Mathf.Clamp(b.size.y * 0.62f, 3.7f, 4.4f);
                    KitS2c.Losa(o, padre, "Toldo_" + colocadas, new Vector3(xf + 0.75f, yToldo, zc), Quaternion.Euler(0f, 0f, -11f), new Vector3(1.6f, 0.07f, Mathf.Min(8f, b.size.z * 0.8f)), toldo, 1.5f, false);
                    KitS2c.Losa(o, padre, "Lampara_" + colocadas, new Vector3(xf + 0.35f, yToldo - 0.3f, zc - 2.8f), Quaternion.identity, new Vector3(0.5f, 0.14f, 0.7f), lampara, 1f, false);
                    KitS2c.FocoPared(o, padre, new Vector3(xf + 0.1f, yToldo - 0.4f, zc + 2.8f), Quaternion.LookRotation(Vector3.left));
                    Light l = UltimaLuz(padre.GetChild(padre.childCount - 1));
                    if (l != null)
                    {
                        l.color = Naranja;
                    }
                    float zNeon = zc + (colocadas % 2 == 0 ? 1.2f : -1.2f);
                    KitS2c.Neon(o, padre, new Vector3(xf, 3.4f, zNeon), Vector3.right, Vector3.Cross(Vector3.up, Vector3.right), colocadas % 2 == 0 ? KitS2c.Magenta : KitS2c.Turquesa, true);
                    foreach (Light ln in padre.GetChild(padre.childCount - 1).GetComponentsInChildren<Light>(true).ToList())
                    {
                        Object.DestroyImmediate(ln.gameObject); // neón solo con emisión
                    }
                    // Cortina metálica bajo el neón (lámina corrugada pegada a la fachada).
                    KitS2c.Losa(o, padre, "Cortina_" + colocadas, new Vector3(xf + 0.06f, 1.35f, zNeon), Quaternion.identity, new Vector3(0.1f, 2.7f, 3.2f), cortina, 1f, false);
                    zLuces.Add(zc - 2.8f);
                    zLuces.Add(zc + 2.8f);
                    zLuces.Add(zNeon);
                    colocadas++;
                    zr -= ancho + 0.4f;
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
            Nota("Patio oeste: " + colocadas + " bodegas PU03/PU04 seguidas contra el borde oeste (fondo 5.5 m, frente al este) con toldo, lámpara naranja, foco, cortina y neón.");
        }

        private static void PatioPostes(Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios, Vector3 cam, Vector3 mira)
        {
            Transform padre = new GameObject("PatioPostes").transform;
            padre.SetParent(nuevo, false);
            Vector3 f = mira - cam;
            f.y = 0f;
            f.Normalize();
            List<Vector3> puntas = new List<Vector3>();
            // Uno junto a las pilas (este) y otro frente a las bodegas (oeste): nunca a menos de 10 m del lente ni detrás de él.
            (float xPoste, Vector3 brazo)[] sitios = { (PatioXPilas - 1.1f, Vector3.left), (PatioXAtras + 7.2f, Vector3.right) };
            foreach ((float xPoste, Vector3 brazo) in sitios)
            {
                List<float> zs = new List<float>();
                for (float z = PatioZNorte - 2f; z > PatioZSur + 1.5f; z -= 1.5f)
                {
                    Vector3 pie = new Vector3(xPoste, 0f, z);
                    Vector3 dv = pie - cam;
                    dv.y = 0f;
                    if (dv.magnitude >= 10f && Vector3.Dot(dv, f) > 6f)
                    {
                        zs.Add(z);
                    }
                }
                foreach (float z in zs.OrderBy(zz => Vector2.Distance(new Vector2(xPoste, zz), new Vector2(cam.x, cam.z))))
                {
                    Vector3 pie = new Vector3(xPoste, 0f, z);
                    Rect h = Rect.MinMaxRect(pie.x - 0.45f, pie.z - 0.45f, pie.x + 0.45f, pie.z + 0.45f);
                    if (!Libre(h, ocupados, mios))
                    {
                        continue;
                    }
                    puntas.Add(KitS2c.Poste(o, padre, pie, brazo, 0f, true));
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
                        if (Mathf.Abs(b.center.z - p.z) < 16f)
                        {
                            KitS2c.Cable(o, p, new Vector3(b.max.x + 0.05f, Mathf.Min(6.0f, b.max.y - 0.5f), b.center.z), 1.1f);
                            cables++;
                        }
                    }
                }
            }
            Nota("Postes del patio: " + puntas.Count + " (a 10 m o más de la cámara), " + cables + " cables.");
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
        /// Busca la cámara de K15 en el patio oeste (x 117..153, z -203..-237): contenedores en el tercio izquierdo (sin llenar media foto), bodegas a 3-14 m en el tercio derecho,
        /// barco y grúas (enteras, a 40 m o más) en la mitad superior, el pasillo al centro abajo y nada a menos de 2.5 m del lente. Cualquier pieza grande a menos de 15 m que cubra la
        /// mitad superior de la foto (patas de grúa, muros) se penaliza fuerte. Ojos a 1.7 m. Devuelve la cámara y el punto de mira.
        /// </summary>
        private static void BuscarCamara(Transform raiz, Transform nuevo, List<Rect> ocupados, out Vector3 mejorCam, out Vector3 mejorMira)
        {
            mejorCam = CamaraK15;
            mejorMira = MiraK15;
            List<Bounds> conts = new List<Bounds>(), bods = new List<Bounds>(), barcos = new List<Bounds>(), gruas = new List<Bounds>(), grandes = new List<Bounds>();
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
            // Piezas grandes de todo el prefab (patas de grúa, muros, bodegas viejas) cerca del patio, para penalizar las que tapen la foto.
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(false))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null || mf.transform.IsChildOf(nuevo))
                {
                    continue;
                }
                Bounds b = CajaMalla(mf);
                if (b.size.y > 3f && b.size.sqrMagnitude < 90000f && b.max.x > 90f && b.min.x < 200f && b.max.z > -275f && b.min.z < -190f)
                {
                    grandes.Add(b);
                }
            }
            grandes.AddRange(conts);
            grandes.AddRange(bods);
            GameObject go = new GameObject("TmpCamaraT1442");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.aspect = 1376f / 768f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            float mejor = float.MinValue;
            try
            {
                foreach (float x in new[] { 124f, 127f, 130f, 133f, 136f, 140f })
                {
                    foreach (float z in new[] { -203.5f, -205.5f, -207.5f, -209.5f, -212f })
                    {
                        Vector3 pos = new Vector3(x, 1.7f, z);
                        if (!Libre(Rect.MinMaxRect(x - 0.5f, z - 0.5f, x + 0.5f, z + 0.5f), ocupados, new List<Rect>()))
                        {
                            continue;
                        }
                        float cerca = 0f;
                        foreach (Bounds b in grandes)
                        {
                            if (b.SqrDistance(pos) < 6.25f)
                            {
                                cerca -= 100f;
                            }
                        }
                        foreach (float xt in new[] { 128f, 140f, 152f, 164f, 176f, 190f })
                        {
                            foreach (float zt in new[] { -245f, -260f, -280f })
                            {
                                foreach (float yt in new[] { 6f, 16f, 28f })
                                {
                                    Vector3 mira = new Vector3(xt, yt, zt);
                                    cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira - pos));
                                    float s = cerca;
                                    foreach (Bounds b in grandes)
                                    {
                                        if (b.SqrDistance(pos) < 225f && AreaSuperior(cam, b) > 0.03f)
                                        {
                                            s -= 60f; // pata de grúa o muro cerca tapando la mitad de arriba
                                        }
                                    }
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
                                        s += bien ? 8f : 0f;
                                    }
                                    foreach (Bounds b in barcos)
                                    {
                                        s += 0.8f * Corners(b).Count(c => { Vector3 v = cam.WorldToViewportPoint(c); return v.z > 0f && v.x > 0f && v.x < 0.65f && v.y > 0.35f && v.y < 1f; });
                                    }
                                    foreach (Bounds b in gruas)
                                    {
                                        int dentro = Corners(b).Count(c => { Vector3 v = cam.WorldToViewportPoint(c); return v.z > 0f && v.x > 0.05f && v.x < 0.95f && v.y > 0.25f && v.y < 1f; });
                                        s += 0.6f * dentro + (dentro == 8 ? 6f : 0f);
                                        if (dentro > 0 && Vector3.Distance(b.center, pos) < 40f)
                                        {
                                            s -= 80f; // la grúa debe verse entera al fondo, a 40 m o más
                                        }
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
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Nota("Cámara K15 buscada: " + mejorCam.ToString("F1") + " mira " + mejorMira.ToString("F1") + " con puntaje " + mejor.ToString("F1") + " (contenedores " + conts.Count + ", bodegas " + bods.Count + ", barcos " + barcos.Count + ", grúas " + gruas.Count + ", piezas grandes " + grandes.Count + ").");
        }

        /// <summary>Fracción de la pantalla que cubre la caja proyectada en la mitad superior del cuadro (y de 0.5 a 1); 0 si queda detrás de la cámara.</summary>
        private static float AreaSuperior(Camera cam, Bounds b)
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
            float w = Mathf.Clamp01(xMax) - Mathf.Clamp01(xMin), h = Mathf.Clamp01(yMax) - Mathf.Max(0.5f, Mathf.Clamp01(yMin));
            return Mathf.Max(0f, w) * Mathf.Max(0f, h);
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
            float xFrente = PatioXAtras + 5.5f;
            int total = 0, montacargas = 0;
            // Primer plano derecho (frente a las bodegas), a 3.4 m o más del lente: pallets, huacales y tambos como en la referencia.
            (string codigo, float alto, float d)[] primerPlano =
            {
                ("PU06_Pallets", 1.2f, 3.4f), ("C17_Huacales", 1.0f, 4.2f), ("PU06_Pallets", 1.2f, 5.0f), ("C16_Tambo", 1.0f, 5.9f),
                ("C17_Huacales", 1.0f, 6.6f), ("C16_Tambo", 1.0f, 7.4f), ("PU06_Pallets", 1.2f, 8.4f), ("C16_Tambo", 1.0f, 9.2f), ("PU06_Pallets", 1.2f, 10.5f),
            };
            if (Mathf.Abs(der.x) > 0.3f)
            {
                foreach ((string codigo, float alto, float d) in primerPlano)
                {
                    foreach (float extra in new[] { 0f, 0.4f, 0.8f, 1.2f })
                    {
                        float lat = (xFrente + 0.9f + extra - cam.x) / der.x;
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
                foreach (float lat in new[] { 0.4f, -0.4f, 0.9f, -0.9f, 1.8f, -1.8f })
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
                    float x = xFrente + 1f + (float)azar.NextDouble() * (PatioXPilas - xFrente - 2.5f);
                    float z = PatioZNorte - 2f - (float)azar.NextDouble() * (PatioZNorte - PatioZSur - 4f);
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
                foreach (Vector3 v in Muestra(casco, 3f, 60))
                {
                    KitS2c.Losa(o, padre, "VentanaBarco", v + haciaCam * 0.06f, Quaternion.LookRotation(haciaCam), new Vector3(2.4f, 0.9f, 0.12f), ventana, 1f, false);
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
                foreach (Vector3 v in Muestra(vs.Where(p => p.y > b.max.y - 1.5f), 4f, 10))
                {
                    KitS2c.Losa(o, padre, "BalizaGrua", v + Vector3.up * 1.0f, Quaternion.identity, new Vector3(2.4f, 2.4f, 2.4f), roja, 1f, false);
                    balizasGruas++;
                }
                float yAlta = b.min.y + b.size.y * 0.8f;
                foreach (Vector3 v in Muestra(vs.Where(p => Mathf.Abs(p.y - yAlta) < 1.2f), 6f, 8))
                {
                    KitS2c.Losa(o, padre, "BalizaGruaAlta", v + Vector3.up * 0.9f, Quaternion.identity, new Vector3(2.0f, 2.0f, 2.0f), roja, 1f, false);
                    balizasGruas++;
                }
                float yMedia = b.min.y + b.size.y * 0.55f;
                foreach (Vector3 v in Muestra(vs.Where(p => Mathf.Abs(p.y - yMedia) < 1.2f), 5f, 8))
                {
                    KitS2c.Losa(o, padre, "BalizaGruaMedia", v + Vector3.up * 0.8f, Quaternion.identity, new Vector3(1.7f, 1.7f, 1.7f), turquesa, 1f, false);
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
                    foreach (string g in new[] { "Contenedores", "Props", "PrimerPlano", "PilaEsquina", "PatioContenedores", "PatioProps", "PatioBodegas" })
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
                        bool esCont = suelo[i].name.StartsWith("Cont_") || suelo[i].name.StartsWith("Apil_");
                        bool apoyado = Mathf.Abs(b.min.y) < 0.05f;
                        if (!apoyado && esCont)
                        {
                            apoyado = cajas.Where((c, j) => j != i && (suelo[j].name.StartsWith("Cont_") || suelo[j].name.StartsWith("Apil_"))).Any(c => Mathf.Abs(c.max.y - b.min.y) < 0.05f && Mathf.Abs(c.center.x - b.center.x) < 0.3f && Mathf.Abs(c.center.z - b.center.z) < 0.3f);
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
