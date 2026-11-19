using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1402 (Puerto, ronda 3): corre DESPUÉS de ZonaPuertoT1396.Aplicar. Todo lo nuevo va bajo el hijo raíz "T1402_Puerto" del prefab T1357_Puerto, que Aplicar borra y rehace.
    /// 1) Pilas de PU08 giradas 90 grados (cara larga con grafiti hacia el pasillo de x = 160) con su cara oeste en x = 166.5 (fuera del arroyo), como K15.
    /// 2) Postes de madera con lámpara y muchos cables sobre esa orilla (a las bodegas de enfrente y a los postes que ya había).
    /// 3) Tarimas, tambos, huacales y montacargas en el pasillo (en los huecos entre pilas y junto a las bodegas).
    /// 4) El arco claro del cielo es el skyline SK01_Este del Malecón (T-1393): Fotos lo registra y toma una foto con el arco apagado para confirmarlo.
    /// Métodos: Aplicar (Logs/t1402-aplicar.txt), Validar (Logs/t1402-validar.txt), Fotos (corrida aparte, docs/capturas/t-1402/).
    /// </summary>
    public static class ZonaPuertoT1402
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        public const string Raiz = "T1402_Puerto";
        private const float YPatio = 0f;
        /// <summary>Cara oeste de las pilas giradas: la calle (x = 160, 12 m) llega a x = 166; con 0.5 m de margen nada de lo nuevo entra al arroyo.</summary>
        private const float XCaraOeste = 166.5f;
        private const float XPoste = 167.1f;
        private const string PrefijoViejo = "T1402_Reemplazado_";
        private const int TopeLuces = 10;
        private const int TopeObjetos = 1500;
        private static readonly string[] Variantes = { "_a", "_b" };
        private static readonly StringBuilder informe = new StringBuilder();

        // Pilas: (z del extremo norte, niveles de cada uno de los dos contenedores seguidos).
        private static readonly (float z, int[] niveles)[] Pilas = { (-89.5f, new[] { 3, 3 }), (-119.5f, new[] { 2, 3 }) };
        private static readonly float[] ZPostes = { -115.0f, -157.5f };

        private static Rect Huella(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);

        private static Rect Expandir(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        private static Bounds Limites(Transform t) => ZonaPuertoT1387.Limites(t);

        private static GameObject BuscarPrefab(string codigo)
        {
            return AssetDatabase.FindAssets(codigo + " t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(r => Path.GetFileNameWithoutExtension(r) == codigo)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
        }

        private static void Nota(string t)
        {
            informe.AppendLine(t);
        }

        private static List<Rect> Obstaculos(Transform raiz)
        {
            List<Rect> r = new List<Rect>();
            foreach (Transform g in raiz)
            {
                if (g.name != "Bodegas" && g.name != "Entrada" && g.name != "Postes" && g.name != "Patio" && g.name != "Muelle")
                {
                    continue;
                }
                foreach (Transform h in g)
                {
                    if (!h.gameObject.activeInHierarchy || h.GetComponent<Light>() != null)
                    {
                        continue;
                    }
                    Bounds b = Limites(h);
                    if (b.size.sqrMagnitude > 0.01f && b.size.y > 0.3f)
                    {
                        r.Add(Huella(b));
                    }
                }
            }
            return r;
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1402/Puerto ronda 3 (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            Nota("T-1402 Puerto (ronda 3)");
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                if (raiz.transform.Find("T1396_Puerto") == null)
                {
                    Nota("FALTA T1396_Puerto: corre primero ZonaPuertoT1396.Aplicar. No se cambió nada.");
                }
                else
                {
                    Transform viejo = raiz.transform.Find(Raiz);
                    if (viejo != null)
                    {
                        Object.DestroyImmediate(viejo.gameObject);
                    }
                    int lucesAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                    Transform nuevo = new GameObject(Raiz).transform;
                    nuevo.SetParent(raiz.transform, false);
                    KitS2c.Obra o = new KitS2c.Obra { Raiz = nuevo, Azar = new System.Random(14020), Nombre = Raiz };
                    RestaurarContenedores(raiz.transform);
                    List<Rect> ocupados = Obstaculos(raiz.transform);
                    List<Rect> mios = new List<Rect>();

                    PonerPilas(raiz.transform, nuevo, ocupados, mios);
                    PostesYCables(raiz.transform, nuevo, o, ocupados, mios);
                    PisoDetalle(raiz.transform, nuevo, ocupados, mios);

                    KitS2c.Terminar(o);
                    foreach (Transform t in nuevo.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.GetComponent<Light>() == null)
                        {
                            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                        }
                    }
                    int lucesDespues = raiz.GetComponentsInChildren<Light>(true).Length;
                    Nota("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (tope de nuevas " + TopeLuces + "). Objetos nuevos: " + nuevo.GetComponentsInChildren<Transform>(true).Length + " (tope " + TopeObjetos + ").");
                    Nota("ARCO DEL CIELO: no se toca el Malecón. Si Fotos confirma que puerto-k15-sinarco ya no tiene la banda, hay que recortar SK01_Este del Malecón a 300 grados (lo hace el orquestador).");
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1402-aplicar.txt", informe.ToString());
            Debug.Log("[ZonaPuertoT1402] " + informe);
        }

        private static int apagados;

        /// <summary>Devuelve a su sitio los contenedores que una corrida anterior apagó.</summary>
        private static void RestaurarContenedores(Transform raiz)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith(PrefijoViejo)).ToList())
            {
                t.name = t.name.Substring(PrefijoViejo.Length);
                t.gameObject.SetActive(true);
            }
        }

        /// <summary>Apaga (y renombra, para poder restaurarlos) los contenedores viejos de los bloques cuya huella toque el rectángulo. Devuelve cuántos apagó.</summary>
        private static int Despejar(Transform raiz, Rect zona)
        {
            Transform cont = raiz.Find("Contenedores");
            int n = 0;
            if (cont == null)
            {
                return 0;
            }
            foreach (Transform bloque in cont.Cast<Transform>().Where(t => t.name.StartsWith("Bloque_")))
            {
                foreach (Transform c in bloque.Cast<Transform>().Where(t => t.gameObject.activeSelf && (t.name.StartsWith("PT01_Contenedor") || t.name.StartsWith("T1396_Grafiti_"))).ToList())
                {
                    if (Huella(Limites(c)).Overlaps(Expandir(zona, -0.05f)))
                    {
                        c.name = PrefijoViejo + c.name;
                        c.gameObject.SetActive(false);
                        n++;
                    }
                }
            }
            apagados += n;
            return n;
        }

        // ---------------------------------------------------------------- 1. Pilas PU08 giradas

        /// <summary>Lee la cara con grafiti de PU08 con el método de T-1396 (es privado: reflexión). Si falla, se supone la cara -z.</summary>
        private static void CaraDe(GameObject prefab, Transform padre, out Vector3 cara, out bool largoEnX)
        {
            cara = Vector3.back;
            largoEnX = true;
            MethodInfo mi = typeof(ZonaPuertoT1396).GetMethod("CaraGrafiti", BindingFlags.NonPublic | BindingFlags.Static);
            if (mi != null)
            {
                object[] args = { prefab, padre, null, null, null };
                mi.Invoke(null, args);
                cara = (Vector3)args[2];
                largoEnX = (bool)args[3];
                Nota("  " + prefab.name + ": " + (string)args[4]);
            }
            else
            {
                Nota("  AVISO: no encontré CaraGrafiti de T-1396; se supone el grafiti en " + cara);
            }
        }

        private static float YawCaraAlOeste(Vector3 cara, bool largoEnX)
        {
            for (float yaw = 0f; yaw < 360f; yaw += 90f)
            {
                Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                Vector3 largo = q * (largoEnX ? Vector3.right : Vector3.forward);
                if (Mathf.Abs(largo.z) > 0.9f && Vector3.Dot(q * cara, Vector3.left) > 0.9f)
                {
                    return yaw;
                }
            }
            return 90f;
        }

        private static void PonerPilas(Transform raiz, Transform nuevo, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("PilasGrafiti").transform;
            padre.SetParent(nuevo, false);
            List<(GameObject prefab, float yaw)> modelos = new List<(GameObject, float)>();
            foreach (string v in Variantes)
            {
                GameObject p = BuscarPrefab("PU08_ContenedorGraffiti" + v);
                if (p == null)
                {
                    Nota("FALTA PU08_ContenedorGraffiti" + v + ": se omite.");
                    continue;
                }
                CaraDe(p, padre, out Vector3 cara, out bool largoEnX);
                modelos.Add((p, YawCaraAlOeste(cara, largoEnX)));
            }
            if (modelos.Count == 0)
            {
                return;
            }
            int puestos = 0, omitidos = 0;
            apagados = 0;
            foreach ((float zNorte, int[] niveles) in Pilas)
            {
                for (int k = 0; k < niveles.Length; k++)
                {
                    float zc = zNorte - ColocarT1357Puerto.LargoCont * (k + 0.5f);
                    float yBase = YPatio;
                    for (int n = 0; n < niveles[k]; n++)
                    {
                        (GameObject prefab, float yaw) = modelos[(puestos + n) % modelos.Count];
                        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                        g.name = "PU08_Pila_" + puestos + "_" + n;
                        g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
                        g.transform.localScale = Vector3.one;
                        Bounds b0 = Limites(g.transform);
                        float sx = ColocarT1357Puerto.AnchoCont / b0.size.x, sy = ColocarT1357Puerto.AltoCont / b0.size.y, sz = ColocarT1357Puerto.LargoCont / b0.size.z;
                        Vector3 e = (Mathf.Min(sx, sy, sz) < 0.8f || Mathf.Max(sx, sy, sz) > 1.25f) ? Vector3.one * Mathf.Min(sx, sy, sz) : new Vector3(sx, sy, sz);
                        g.transform.localScale = e;
                        Bounds b = Limites(g.transform);
                        g.transform.position += new Vector3(XCaraOeste - b.min.x, yBase - b.min.y, zc - b.center.z);
                        Bounds fin = Limites(g.transform);
                        Rect h = Huella(fin);
                        if (n == 0 && ocupados.Any(r => r.Overlaps(Expandir(h, -0.05f))))
                        {
                            Object.DestroyImmediate(g);
                            omitidos++;
                            Nota("Pila z " + zc.ToString("F1") + ": hay una bodega u otra cosa en la huella; se omite ese contenedor y su altura.");
                            break;
                        }
                        if (n == 0)
                        {
                            Despejar(raiz, h);
                        }
                        yBase = fin.max.y;
                        if (n == 0)
                        {
                            mios.Add(h);
                        }
                        puestos++;
                    }
                }
            }
            Renderer[] rend = padre.GetComponentsInChildren<Renderer>(false);
            if (rend.Length > 0)
            {
                LODGroup lg = padre.gameObject.AddComponent<LODGroup>();
                lg.SetLODs(new[] { new LOD(0.01f, rend) });
                lg.RecalculateBounds();
            }
            Nota("Pilas PU08 giradas 90 grados (cara larga con grafiti al oeste, hacia el pasillo): " + puestos + " contenedores en " + Pilas.Length + " pilas con la cara oeste en x " + XCaraOeste.ToString("F1") + " (fuera del arroyo, que llega a x 166); contenedores viejos apagados para dejarles sitio: " + apagados + "; " + omitidos + " omitidos. Los colliders de PU08 quedan encendidos.");
        }

        // ---------------------------------------------------------------- 2. Postes y cables

        private static void PostesYCables(Transform raiz, Transform nuevo, KitS2c.Obra o, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("PostesCables").transform;
            padre.SetParent(nuevo, false);
            List<Vector3> puntas = new List<Vector3>();
            foreach (float z in ZPostes)
            {
                Vector3 pie = new Vector3(XPoste, YPatio, z);
                Rect h = Rect.MinMaxRect(pie.x - 0.4f, pie.z - 0.4f, pie.x + 0.4f, pie.z + 0.4f);
                if (ocupados.Any(r => r.Overlaps(h)) || mios.Any(r => r.Overlaps(h)))
                {
                    Nota("Poste z " + z + ": hay algo; no se pone.");
                    continue;
                }
                Despejar(raiz, h);
                puntas.Add(KitS2c.Poste(o, padre, pie, Vector3.left, YPatio, true));
                mios.Add(h);
            }
            // Postes que ya había (T-1357/T-1396) cerca: se enlazan también.
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
                    foreach (float dy in new[] { 0f, -0.6f })
                    {
                        KitS2c.Cable(o, punta + Vector3.up * dy, q + Vector3.up * dy, 1.1f);
                        cables++;
                    }
                }
                // Cruzan el pasillo hasta la fachada de la bodega de enfrente.
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
            Nota("Postes de madera con lámpara y transformador: " + puntas.Count + " en x " + XPoste.ToString("F1") + ", fuera del arroyo, en los huecos entre pilas, " + cables + " cables (5 entre postes, 2 a cada poste vecino a menos de 26 m, 3 a la bodega de enfrente).");
        }

        // ---------------------------------------------------------------- 3. Detalles de piso

        private static void PisoDetalle(Transform raiz, Transform nuevo, List<Rect> ocupados, List<Rect> mios)
        {
            Transform padre = new GameObject("PisoDetalle").transform;
            padre.SetParent(nuevo, false);
            // (código, alto, x, z, giro)
            (string codigo, float alto, float x, float z, float giro)[] lista =
            {
                ("PU05_Montacargas", 2.6f, 168.4f, -118.0f, 90f), ("PU06_Pallets", 1.2f, 168.3f, -115.6f, 12f), ("C16_Tambo", 1.0f, 167.5f, -113.9f, 0f),
                ("C16_Tambo", 1.0f, 168.6f, -113.9f, 40f), ("C17_Huacales", 1.0f, 167.6f, -119.0f, 80f), ("PU06_Pallets", 1.2f, 168.2f, -120.2f, 5f),
                ("C16_Tambo", 1.0f, 167.4f, -156.9f, 0f),
            };
            int puestos = 0;
            foreach ((string codigo, float alto, float x, float z, float giro) in lista)
            {
                GameObject prefab = BuscarPrefab(codigo);
                if (prefab == null)
                {
                    Nota("FALTA " + codigo + ": se omite.");
                    continue;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                go.name = codigo + "_" + puestos;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, giro, 0f));
                go.transform.localScale = Vector3.one;
                Bounds b = Limites(go.transform);
                go.transform.localScale = Vector3.one * (alto / Mathf.Max(0.01f, b.size.y));
                b = Limites(go.transform);
                go.transform.position += new Vector3(x - b.center.x, YPatio - b.min.y, z - b.center.z);
                Rect h = Huella(Limites(go.transform));
                if (h.xMin < XCaraOeste - 0.05f || ocupados.Any(r => r.Overlaps(Expandir(h, -0.05f))) || mios.Any(r => r.Overlaps(Expandir(h, -0.05f))))
                {
                    Object.DestroyImmediate(go);
                    Nota(codigo + " en (" + x + ", " + z + "): choca con algo; se omite.");
                    continue;
                }
                Despejar(raiz, h);
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
                mios.Add(h);
                puestos++;
            }
            Nota("Detalles de piso: " + puestos + "/" + lista.Length + " (tarimas, tambos, huacales y 2 montacargas, sin collider, en los huecos entre pilas, fuera del arroyo (x >= 166.5)).");
        }

        // ---------------------------------------------------------------- Validar

        [MenuItem("Mecha Cyberpunk/T-1402/Puerto ronda 3 (validar)")]
        public static void Validar()
        {
            StringBuilder v = new StringBuilder("T-1402 validar Puerto\n");
            int fallos = 0;
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
                    v.AppendLine("Objetos " + objetos + " (tope " + TopeObjetos + "), luces nuevas " + luces + " (tope " + TopeLuces + ")");
                    fallos += (objetos > TopeObjetos ? 1 : 0) + (luces > TopeLuces ? 1 : 0);
                    Transform pilas = mia.Find("PilasGrafiti");
                    int pu08 = pilas == null ? 0 : pilas.childCount;
                    v.AppendLine("Contenedores PU08 girados: " + pu08);
                    if (pu08 < 6)
                    {
                        v.AppendLine("FALLO: faltan pilas de PU08");
                        fallos++;
                    }
                    if (pilas != null)
                    {
                        List<Rect> otros = Obstaculos(raiz.transform);
                        List<Bounds> cajas = pilas.Cast<Transform>().Select(Limites).ToList();
                        for (int i = 0; i < cajas.Count; i++)
                        {
                            Bounds b = cajas[i];
                            string nombre = pilas.GetChild(i).name;
                            if (b.size.z < b.size.x)
                            {
                                v.AppendLine("FALLO " + nombre + ": no está girado (eje largo en x).");
                                fallos++;
                            }
                            if (b.min.x < XCaraOeste - 0.05f)
                            {
                                v.AppendLine("CARRIL " + nombre + " invade el arroyo (x mín " + b.min.x.ToString("F2") + ").");
                                fallos++;
                            }
                            bool apoyado = Mathf.Abs(b.min.y - YPatio) < 0.1f || cajas.Where((c, j) => j != i).Any(c => Mathf.Abs(c.max.y - b.min.y) < 0.1f && Mathf.Abs(c.center.z - b.center.z) < 1f);
                            if (!apoyado)
                            {
                                v.AppendLine("ALTURA " + nombre + ": base en y=" + b.min.y.ToString("F2") + " (flota o está enterrado).");
                                fallos++;
                            }
                            if (otros.Any(r => r.Overlaps(Expandir(Huella(b), -0.1f))))
                            {
                                v.AppendLine("CHOQUE " + nombre + " con un obstáculo de la zona.");
                                fallos++;
                            }
                        }
                    }
                    int postes = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Poste_"));
                    bool hayCables = mia.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.name == "Cables");
                    v.AppendLine("Postes de madera: " + postes + "; cables: " + (hayCables ? "sí" : "NO"));
                    if (postes < 2 || !hayCables)
                    {
                        v.AppendLine("FALLO: faltan postes o cables");
                        fallos++;
                    }
                    Transform piso = mia.Find("PisoDetalle");
                    int detalles = piso == null ? 0 : piso.childCount;
                    v.AppendLine("Detalles de piso: " + detalles);
                    if (detalles < 6)
                    {
                        v.AppendLine("FALLO: pocos detalles de piso");
                        fallos++;
                    }
                    if (piso != null)
                    {
                        foreach (Transform d in piso)
                        {
                            Bounds b = Limites(d);
                            if (Mathf.Abs(b.min.y - YPatio) > 0.1f)
                            {
                                v.AppendLine("ALTURA " + d.name + ": base en y=" + b.min.y.ToString("F2"));
                                fallos++;
                            }
                        }
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            v.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1402-validar.txt", v.ToString());
            Debug.Log("[ZonaPuertoT1402] " + v);
        }

        // ---------------------------------------------------------------- Fotos

        private static void PonerDomoEn(Vector3 desde)
        {
            GameObject marca = new GameObject("MarcaDomoT1402");
            Camera cam = marca.AddComponent<Camera>();
            cam.enabled = false;
            marca.transform.position = desde;
            foreach (CieloNubes domo in Object.FindObjectsByType<CieloNubes>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                domo.Colocar(cam, ColocarS2cLook.HoraFoto);
            }
            Object.DestroyImmediate(marca);
        }

        /// <summary>Fotos de K15 (más una variante con otra mira y otra con el arco SK01_Este apagado) y A15. No guarda nada; corrida de Unity aparte de Aplicar.</summary>
        [MenuItem("Mecha Cyberpunk/T-1402/Puerto ronda 3 (fotos)")]
        public static void Fotos()
        {
            Directory.CreateDirectory("docs/capturas/t-1402");
            Directory.CreateDirectory("Logs");
            ColocarS2.Abrir();
            StringBuilder s = new StringBuilder("T-1402 skyline en la escena\n");
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.name.StartsWith("SK01") || (r.transform.parent != null && r.transform.parent.name.StartsWith("SK01")))
                {
                    s.AppendLine(r.name + " centro " + r.bounds.center.ToString("F0") + " tamaño " + r.bounds.size.ToString("F0"));
                }
            }
            File.WriteAllText("Logs/t1402-cielo.txt", s.ToString());
            Vector3 cam = ZonaPuertoT1396.CamaraK15;
            PonerDomoEn(cam);
            Tomar("K15_Puerto", "puerto-k15", cam, ZonaPuertoT1396.MiraK15, 55f);
            Tomar("K15_Puerto", "puerto-k15b", cam, new Vector3(150f, 20f, -255f), 58f);
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(g => g.name == "SK01_Este").ToList())
            {
                go.SetActive(false);
            }
            Tomar("K15_Puerto", "puerto-k15-sinarco", cam, ZonaPuertoT1396.MiraK15, 55f);
            PonerDomoEn(ZonaPuertoT1396.CamaraA15);
            Tomar("A15_Puerto_Aerea", "puerto-a15", ZonaPuertoT1396.CamaraA15, ZonaPuertoT1396.MiraA15, 62f);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }

        private static void Tomar(string referencia, string nombre, Vector3 desde, Vector3 hacia, float campo)
        {
            string juego = "Logs/t1402-" + nombre + "-juego.png";
            ColocarS2cLook.Foto(desde, hacia, juego, campo);
            ColocarS2.LadoALado("imageassets/referencias/look/" + referencia + ".png", juego, "docs/capturas/t-1402/" + nombre + ".png");
        }
    }
}
