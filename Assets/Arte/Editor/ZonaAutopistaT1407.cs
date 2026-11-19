using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1407 (Autopista elevada, ronda 5, K18). Orden de corrida (cada una en su propia corrida de Unity):
    /// 1) BuscarCamara (con la escena Ciudad_Prologo, ANTES de Aplicar): prueba una rejilla de cámaras sobre piso caminable, puntúa cada una proyectando la boca del túnel,
    ///    una columna del tablero, la rampa norte a media subida y la torre de reflectores, y escribe Logs/t1407-camara.txt (mejor, 5 siguientes y el sitio de la torre).
    /// 2) Aplicar: lee ese archivo; mueve y agranda la torre de reflectores dentro del cuadro de la mejor cámara (150 a 300 m), le pone cresta emisiva y tres haces nuevos
    ///    (HaloLuz más brillante), y cuelga del tablero placas emisivas de sodio con su halo y unas pocas luces. Todo bajo "T1407_Autopista" (se borra y rehace).
    /// 3) Validar (Logs/t1407-validar.txt). 4) Fotos (corrida aparte): docs/capturas/t-1407/k18.png (mejor cámara) y k18b.png (la segunda).
    /// Curva del tablero: NO se hace. El tablero principal es recto por z = 250 (x -330 a 240) y entra al túnel por una boca fija en (-330, 10, 250); curvarlo movería columnas,
    /// guardarraíles, luminarias, tráfico y el trazado que miden AutopistaT1356Pruebas y las pruebas de tráfico, y la rampa norte ya se une a él en x = -300. Se compensa con la cámara.
    /// Si se vuelve a correr BuscarCamara después de Aplicar, hay que volver a correr Aplicar.
    /// </summary>
    public static class ZonaAutopistaT1407
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab";
        public const string NombreRaiz = "T1407_Autopista";
        public const string RutaCamara = "Logs/t1407-camara.txt";

        private const float Campo = 70f;
        private const float Aspecto = 1376f / 768f;
        private const int MaxLucesNuevas = 4;
        private const float AnchoBaseTorre = 13f;
        private const float ZPlacas = 243.6f;
        private static readonly Vector3 Boca = new Vector3(-329f, 13.5f, 250f);
        private static readonly StringBuilder informe = new StringBuilder();
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private sealed class Candidato
        {
            public Vector3 Pos;
            public Vector3 Adelante;
            public float Puntaje;
            public string Detalle;
            public Vector3 Mira => Pos + Adelante * 30f;
        }

        // ---------------------------------------------------------------- Utilidades

        private static string F(float v) => v.ToString("F2", Inv);

        private static string V3(Vector3 v) => F(v.x) + ";" + F(v.y) + ";" + F(v.z);

        private static Vector3 LeerV3(string s)
        {
            string[] p = s.Split(';');
            return new Vector3(float.Parse(p[0], Inv), float.Parse(p[1], Inv), float.Parse(p[2], Inv));
        }

        private static float Puntua(float v, float lo, float hi, float tol)
        {
            if (v >= lo && v <= hi)
            {
                return 1f;
            }
            float d = v < lo ? lo - v : v - hi;
            return Mathf.Max(0f, 1f - d / tol);
        }

        private static bool EsSueloBajo(Collider c) => c.bounds.max.y < 0.6f;

        private static bool EsTablero(Transform t)
        {
            for (Transform a = t; a != null; a = a.parent)
            {
                if (a.name == "Tablero")
                {
                    return true;
                }
            }
            return false;
        }

        private static Bounds? Caja(GameObject go, bool soloVisible)
        {
            Bounds? caja = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (soloVisible && (!r.enabled || r.name.StartsWith("Halo")))
                {
                    continue;
                }
                if (caja.HasValue)
                {
                    Bounds w = caja.Value;
                    w.Encapsulate(r.bounds);
                    caja = w;
                }
                else
                {
                    caja = r.bounds;
                }
            }
            return caja;
        }

        private static float DistanciaPlanta(Vector3 p, List<Vector3> puntos)
        {
            float m = float.MaxValue;
            foreach (Vector3 q in puntos)
            {
                m = Mathf.Min(m, new Vector2(p.x - q.x, p.z - q.z).magnitude);
            }
            return m;
        }

        /// <summary>Cajas de lo visible del prefab (sin lo de T1407, el tablero, pisos grandes, fondo, cerro, halos, cables, charcos y líneas).</summary>
        private static List<Bounds> Ocupados(Transform raiz, Transform mia)
        {
            Transform acantilado = raiz.Find("Acantilado");
            Transform fondo = raiz.Find("Fondo");
            Transform tablero = raiz.Find("Tablero");
            List<Bounds> lista = new List<Bounds>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || (mia != null && r.transform.IsChildOf(mia)))
                {
                    continue;
                }
                if (r.name == "Cables" || r.name.StartsWith("Halo") || r.name.StartsWith("Haz_") || r.name.StartsWith("Charco") || r.name.StartsWith("Linea"))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.x > 60f || b.size.z > 60f || b.size.y < 0.4f || (acantilado != null && r.transform.IsChildOf(acantilado))
                    || (fondo != null && r.transform.IsChildOf(fondo)) || (tablero != null && r.transform.IsChildOf(tablero)))
                {
                    continue;
                }
                lista.Add(b);
            }
            return lista;
        }

        /// <summary>Base de la losa del tablero principal en (x, z): el mínimo y de los renderers de Tablero/Principal que cubren ese punto y no son columnas (min y &gt; 3).</summary>
        private static float BajoLosa(Transform raiz, float x, float z)
        {
            Transform tab = raiz.Find("Tablero/Principal");
            float m = float.MaxValue;
            if (tab != null)
            {
                foreach (Renderer r in tab.GetComponentsInChildren<Renderer>(false))
                {
                    Bounds b = r.bounds;
                    if (b.min.y > 3f && x >= b.min.x - 0.5f && x <= b.max.x + 0.5f && z >= b.min.z - 0.5f && z <= b.max.z + 0.5f)
                    {
                        m = Mathf.Min(m, b.min.y);
                    }
                }
            }
            return m == float.MaxValue ? -1f : m;
        }

        // ---------------------------------------------------------------- Búsqueda de cámara

        /// <summary>Piso caminable: el hueco más bajo está a ras (y &lt; 0.6, casi plano) y lo único que hay encima es el tablero o la rampa.</summary>
        private static bool Piso(float x, float z, out float y)
        {
            y = 0f;
            RaycastHit[] hs = Physics.RaycastAll(new Vector3(x, 40f, z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            if (hs.Length == 0)
            {
                return false;
            }
            RaycastHit bajo = hs.OrderBy(h => h.point.y).First();
            if (bajo.point.y > 0.6f || bajo.normal.y < 0.7f)
            {
                return false;
            }
            foreach (RaycastHit h in hs)
            {
                if (h.point.y > 0.6f && !EsTablero(h.collider.transform))
                {
                    return false;
                }
            }
            y = bajo.point.y;
            return true;
        }

        private static bool Ocluido(Vector3 desde, Vector3 objetivo, float tolerancia)
        {
            if (Physics.Linecast(desde, objetivo, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                return Vector3.Distance(hit.point, objetivo) > tolerancia;
            }
            return false;
        }

        [MenuItem("Mecha Cyberpunk/T-1407/Autopista ronda 5 (buscar camara)")]
        public static void BuscarCamara()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            StringBuilder s = new StringBuilder("T-1407 BuscarCamara: rejilla x -280..-120 cada 4 m, z 205..245 cada 3 m, alturas 2, 4 y 6 m, campo 70, aspecto 1376/768\n");
            List<Vector3> columnas = T.Columnas(T.Principal()).Where(c => c.x > -330f && c.x < -100f).Select(c => new Vector3(c.x, 5f, c.z)).ToList();
            List<Vector3> rampa = T.Resamplear(T.RampaNorte(), 5f).Where(p => p.y >= 3f && p.y <= 7f).Select(p => new Vector3(p.x, p.y + 0.3f, p.z)).ToList();
            Transform torre = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "TorreReflectores");
            Vector3? puntoTorre = null;
            if (torre != null)
            {
                Bounds? bt = Caja(torre.gameObject, false);
                if (bt.HasValue)
                {
                    puntoTorre = new Vector3(bt.Value.center.x, bt.Value.max.y - 2f, bt.Value.center.z);
                }
            }
            s.AppendLine("Columnas candidatas (tablero principal, x -330..-100): " + columnas.Count + "; puntos de la rampa a media subida: " + rampa.Count
                + "; torre actual: " + (puntoTorre.HasValue ? puntoTorre.Value.ToString("F1") : "no encontrada"));

            List<Candidato> todos = new List<Candidato>();
            int sinPiso = 0, chocaCerca = 0, bocaTapada = 0, rampaTapada = 0, probadas = 0;
            GameObject go = new GameObject("T1407_CamTemp") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = Campo;
                cam.aspect = Aspecto;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 2000f;
                for (float x = -280f; x <= -120f + 0.01f; x += 4f)
                {
                    for (float z = 205f; z <= 245f + 0.01f; z += 3f)
                    {
                        if (!Piso(x, z, out float suelo))
                        {
                            sinPiso++;
                            continue;
                        }
                        foreach (float altura in new[] { 2f, 4f, 6f })
                        {
                            Vector3 pos = new Vector3(x, suelo + altura, z);
                            if (Physics.OverlapSphere(pos, 1f, ~0, QueryTriggerInteraction.Ignore).Any(c => !EsSueloBajo(c)))
                            {
                                chocaCerca++;
                                continue;
                            }
                            if (Ocluido(pos, Boca, 3f))
                            {
                                bocaTapada++;
                                continue;
                            }
                            List<Vector3> rampaLibre = rampa.Where(p => !Ocluido(pos, p, 3f)).ToList();
                            if (rampaLibre.Count == 0)
                            {
                                rampaTapada++;
                                continue;
                            }
                            List<Vector3> columnasLibres = columnas.Where(c => new Vector2(c.x - pos.x, c.z - pos.z).magnitude < 25f && !Ocluido(pos, c, 3f)).ToList();
                            probadas++;
                            Vector3 haciaBoca = Boca - pos;
                            float yaw0 = Mathf.Atan2(haciaBoca.x, haciaBoca.z) * Mathf.Rad2Deg;
                            float pitch0 = -Mathf.Atan2(haciaBoca.y, new Vector2(haciaBoca.x, haciaBoca.z).magnitude) * Mathf.Rad2Deg;
                            Candidato mejor = null;
                            for (float izq = 6f; izq <= 30f; izq += 4f)
                            {
                                foreach (float dp in new[] { -6f, -3f, 0f, 3f, 6f })
                                {
                                    Quaternion rot = Quaternion.Euler(pitch0 + dp, yaw0 - izq, 0f);
                                    Vector3 adelante = rot * Vector3.forward;
                                    if (Physics.Raycast(pos, adelante, out RaycastHit golpe, 3f, ~0, QueryTriggerInteraction.Ignore) && !EsSueloBajo(golpe.collider))
                                    {
                                        continue;
                                    }
                                    cam.transform.SetPositionAndRotation(pos, rot);
                                    Vector3 vb = cam.WorldToViewportPoint(Boca);
                                    float tBoca = vb.z > 0f ? Mathf.Min(Puntua(vb.x, 0.55f, 0.8f, 0.25f), Puntua(vb.y, 0.45f, 0.6f, 0.25f)) : 0f;
                                    if (tBoca <= 0.3f)
                                    {
                                        continue;
                                    }
                                    float tCol = 0f, tRampa = 0f, tTorre = 0f;
                                    Vector3 vc = Vector3.zero, vr = Vector3.zero, vt = Vector3.zero;
                                    foreach (Vector3 c in columnasLibres)
                                    {
                                        Vector3 v = cam.WorldToViewportPoint(c);
                                        float t = v.z > 0f && v.y > 0.05f && v.y < 0.95f ? Puntua(v.x, 0.45f, 0.65f, 0.25f) : 0f;
                                        if (t > tCol)
                                        {
                                            tCol = t;
                                            vc = v;
                                        }
                                    }
                                    foreach (Vector3 r in rampaLibre)
                                    {
                                        Vector3 v = cam.WorldToViewportPoint(r);
                                        float t = v.z > 0f && v.y > 0.05f && v.y < 0.8f ? Puntua(v.x, 0.3f, 0.5f, 0.25f) : 0f;
                                        if (t > tRampa)
                                        {
                                            tRampa = t;
                                            vr = v;
                                        }
                                    }
                                    if (puntoTorre.HasValue)
                                    {
                                        vt = cam.WorldToViewportPoint(puntoTorre.Value);
                                        tTorre = vt.z > 0f ? Mathf.Min(Puntua(vt.x, 0.1f, 0.35f, 0.25f), Puntua(vt.y, 0.6f, 0.95f, 0.3f)) : 0f;
                                    }
                                    float total = 3f * tBoca + 3f * tCol + 2f * tRampa + 1f * tTorre;
                                    if (mejor == null || total > mejor.Puntaje)
                                    {
                                        mejor = new Candidato
                                        {
                                            Pos = pos,
                                            Adelante = adelante,
                                            Puntaje = total,
                                            Detalle = "boca=(" + F(vb.x) + "," + F(vb.y) + ") columna=(" + F(vc.x) + "," + F(vc.y) + ") rampa=(" + F(vr.x) + "," + F(vr.y) + ") torre=(" + F(vt.x) + "," + F(vt.y) + ")",
                                        };
                                    }
                                }
                            }
                            if (mejor != null)
                            {
                                todos.Add(mejor);
                            }
                        }
                    }
                }
                s.AppendLine("Rechazadas: sin piso caminable " + sinPiso + " (columnas de la rejilla), collider a menos de 1 m " + chocaCerca + ", boca tapada " + bocaTapada + ", rampa tapada " + rampaTapada
                    + ". Posiciones puntuadas: " + probadas + ", con puntaje: " + todos.Count + ".");
                if (todos.Count == 0)
                {
                    s.AppendLine("SIN CAMARA: ninguna posición puntúa. Revisar las rechazadas.");
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(RutaCamara, s.ToString());
                    Debug.LogError("[Arte] T-1407 BuscarCamara sin resultado:\n" + s);
                    return;
                }
                todos = todos.OrderByDescending(c => c.Puntaje).ToList();
                Candidato mejorCam = todos[0];
                bool hayTorre = TorreSitio(cam, mejorCam, out Vector3 sitio, out float alto, out float ancho, out string textoTorre);
                s.AppendLine("Puntaje máximo posible: 9 (boca 3, columna 3, rampa 2, torre 1; la torre cuenta donde está ahora, Aplicar la mueve).");
                List<Candidato> elegidas = new List<Candidato> { mejorCam };
                // La segunda debe ser otra posición (a 8 m o más) y, si hay sitio de torre, ver la torre movida dentro del cuadro.
                foreach (Candidato c in todos.Skip(1))
                {
                    if (elegidas.Count >= 6)
                    {
                        break;
                    }
                    if (elegidas.Any(e => Vector3.Distance(e.Pos, c.Pos) < 8f))
                    {
                        continue;
                    }
                    if (elegidas.Count == 1 && hayTorre)
                    {
                        cam.transform.SetPositionAndRotation(c.Pos, Quaternion.LookRotation(c.Adelante));
                        Vector3 vt = cam.WorldToViewportPoint(new Vector3(sitio.x, alto, sitio.z));
                        if (vt.z <= 0f || vt.x < 0.02f || vt.x > 0.55f || vt.y < 0.4f)
                        {
                            continue;
                        }
                    }
                    elegidas.Add(c);
                }
                for (int i = 0; i < elegidas.Count; i++)
                {
                    Candidato c = elegidas[i];
                    s.AppendLine((i == 0 ? "CAM0 (elegida)" : "CAM" + i) + " desde=" + V3(c.Pos) + " hacia=" + V3(c.Mira) + " campo=" + F(Campo) + " puntaje=" + F(c.Puntaje) + " " + c.Detalle);
                }
                s.AppendLine(hayTorre ? "TORRE sitio=" + V3(new Vector3(sitio.x, 0f, sitio.z)) + " altura=" + F(alto) + " ancho=" + F(ancho) + " " + textoTorre
                    : "TORRE sin sitio libre: " + textoTorre);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaCamara, s.ToString());
            Debug.Log("[Arte] T-1407 BuscarCamara:\n" + s);
        }

        /// <summary>
        /// Sitio de la torre dentro del cuadro de la cámara: de 300 a 150 m, con la base a la izquierda (x de pantalla 0.22 y variantes), donde no haya ningún collider en una caja
        /// de su ancho (sin contar el piso a ras) ni la autopista, la rampa o el túnel a menos de 14 m. La altura deja su punta en y = 0.85 de la pantalla.
        /// </summary>
        private static bool TorreSitio(Camera cam, Candidato c, out Vector3 sitio, out float alto, out float ancho, out string texto)
        {
            sitio = Vector3.zero;
            alto = 0f;
            ancho = 0f;
            texto = "";
            Vector3 adelanteP = new Vector3(c.Adelante.x, 0f, c.Adelante.z).normalized;
            float tanMedio = Mathf.Tan(Campo * 0.5f * Mathf.Deg2Rad) * Aspecto;
            float cabeceoArriba = Mathf.Asin(Mathf.Clamp(c.Adelante.y, -1f, 1f)) * Mathf.Rad2Deg;
            float elevacion = cabeceoArriba + Mathf.Atan(0.7f * Mathf.Tan(Campo * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
            List<Vector3> lineas = T.Resamplear(T.Principal(), 5f);
            lineas.AddRange(T.Resamplear(T.RampaNorte(), 5f));
            lineas.AddRange(T.Resamplear(T.Tunel(), 5f));
            for (float d = 300f; d >= 150f; d -= 10f)
            {
                foreach (float vx in new[] { 0.22f, 0.18f, 0.26f, 0.14f, 0.3f })
                {
                    float giro = Mathf.Atan((2f * vx - 1f) * tanMedio) * Mathf.Rad2Deg;
                    Vector3 dir = Quaternion.AngleAxis(giro, Vector3.up) * adelanteP;
                    Vector3 p = c.Pos + dir * d;
                    float h = Mathf.Clamp(c.Pos.y + d * Mathf.Tan(elevacion * Mathf.Deg2Rad), 60f, 168f);
                    float w = Mathf.Max(8f, h / 5f);
                    Vector3 centro = new Vector3(p.x, 45f, p.z);
                    bool choca = Physics.OverlapBox(centro, new Vector3(w * 0.5f + 3f, 45f, w * 0.5f + 3f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(col => !EsSueloBajo(col));
                    if (choca || DistanciaPlanta(p, lineas) <= 14f + w * 0.5f || p.z >= 292f)
                    {
                        continue;
                    }
                    sitio = new Vector3(p.x, 0f, p.z);
                    alto = h;
                    ancho = w;
                    texto = "(a " + F(d) + " m de la cámara, x de pantalla pedido " + F(vx) + ")";
                    return true;
                }
            }
            texto = "no hay sitio libre entre 150 y 300 m dentro del cuadro; la torre se deja donde está.";
            return false;
        }

        // ---------------------------------------------------------------- Lectura de la cámara

        private static List<(Vector3 desde, Vector3 hacia, float puntaje)> LeerCamaras(out Vector3? sitio, out float alto, out float ancho)
        {
            sitio = null;
            alto = 0f;
            ancho = 0f;
            List<(Vector3, Vector3, float)> lista = new List<(Vector3, Vector3, float)>();
            if (!File.Exists(RutaCamara))
            {
                return lista;
            }
            foreach (string linea in File.ReadAllLines(RutaCamara))
            {
                Dictionary<string, string> kv = linea.Split(' ').Where(t => t.Contains("=")).GroupBy(t => t.Substring(0, t.IndexOf('='))).ToDictionary(g => g.Key, g => g.First().Substring(g.Key.Length + 1));
                if (linea.StartsWith("CAM") && kv.ContainsKey("desde") && kv.ContainsKey("hacia"))
                {
                    lista.Add((LeerV3(kv["desde"]), LeerV3(kv["hacia"]), kv.ContainsKey("puntaje") ? float.Parse(kv["puntaje"], Inv) : 0f));
                }
                else if (linea.StartsWith("TORRE sitio=") && kv.ContainsKey("sitio"))
                {
                    sitio = LeerV3(kv["sitio"]);
                    alto = float.Parse(kv["altura"], Inv);
                    ancho = float.Parse(kv["ancho"], Inv);
                }
            }
            return lista;
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1407/Autopista ronda 5 (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            informe.AppendLine("T-1407 Autopista (ronda 5)");
            informe.AppendLine("Curva del tablero: NO se hizo. El tablero principal es recto (z = 250, x -330 a 240) y la boca del túnel está fija en (-330, 10, 250); curvarlo movería columnas, guardarraíles, luminarias, tráfico y el trazado que miden las pruebas (AutopistaT1356Pruebas, tráfico). La columna y la rampa se leen con la cámara elegida.");
            List<(Vector3 desde, Vector3 hacia, float puntaje)> camaras = LeerCamaras(out Vector3? sitio, out float alto, out float ancho);
            informe.AppendLine("Cámaras leídas de " + RutaCamara + ": " + camaras.Count + (sitio.HasValue ? "; sitio de torre " + sitio.Value.ToString("F1") : "; sin sitio de torre"));
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform vieja = raiz.Find(NombreRaiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                int lucesAntes = contenido.GetComponentsInChildren<Light>(true).Length;
                KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1407, raiz);
                Vector3 adelante = camaras.Count > 0 ? camaras[0].hacia - camaras[0].desde : Vector3.left;
                PonerTorre(o, raiz, sitio, alto, ancho, adelante);
                PonerPlacas(o, raiz);
                KitS2c.Terminar(o);
                int lucesDespues = contenido.GetComponentsInChildren<Light>(true).Length;
                informe.AppendLine("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (nuevas " + (lucesDespues - lucesAntes) + ", tope " + MaxLucesNuevas + ").");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1407-autopista-aplicar.txt", informe.ToString());
            Debug.Log("[Arte] T-1407 autopista aplicar:\n" + informe);
        }

        // ---------------------------------------------------------------- Torre

        private static Material MatHaz()
        {
            return KitS2c.Mat("T1407_HazTorre", () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/HaloLuz"));
                m.SetColor("_Color", new Color(0.78f, 0.9f, 1f));
                m.SetFloat("_Intensidad", 1.7f);
                m.SetFloat("_Borde", 2.2f);
                m.SetFloat("_Caida", 1.1f);
                m.SetTexture("_Ruido", KitS2c.TexMuro());
                m.SetTextureScale("_Ruido", new Vector2(3f, 1f));
                return m;
            });
        }

        private static Material MatEmisiva(string nombre, Color noche)
        {
            return KitS2c.Mat(nombre, () =>
            {
                Material m = KitS2c.Emision(noche, KitS2c.TexMuro());
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
        }

        private static void Estatico(GameObject g)
        {
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private static void PonerTorre(KitS2c.Obra o, Transform raiz, Vector3? sitio, float alto, float ancho, Vector3 adelante)
        {
            Transform torre = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "TorreReflectores");
            if (torre == null)
            {
                informe.AppendLine("No existe TorreReflectores en el prefab: no hay torre que mover.");
                return;
            }
            Transform pa = torre.parent;
            torre.localScale = Vector3.one;
            Bounds? original = Caja(torre.gameObject, false);
            float alto0 = original.HasValue && original.Value.size.y > 1f ? original.Value.size.y : 168f;
            Vector3 antes = torre.position;
            float sy, sxz;
            if (sitio.HasValue)
            {
                torre.position = new Vector3(sitio.Value.x, antes.y, sitio.Value.z);
                sy = Mathf.Clamp(alto / alto0, 0.2f, 1f);
                sxz = Mathf.Clamp(ancho / AnchoBaseTorre, 0.6f, 2.4f);
                torre.localScale = new Vector3(sxz, sy, sxz);
                informe.AppendLine("TORRE movida de " + antes.ToString("F1") + " a " + torre.position.ToString("F1") + ", alto " + (alto0 * sy).ToString("F0") + " m, ancho " + (AnchoBaseTorre * sxz).ToString("F1") + " m (escala " + torre.localScale.ToString("F2") + ").");
            }
            else
            {
                sy = torre.localScale.y;
                sxz = 1f;
                torre.localScale = new Vector3(0.6f, 1f, 0.6f);
                sxz = 0.6f;
                sy = 1f;
                informe.AppendLine("TORRE: sin sitio en " + RutaCamara + "; se deja en " + antes.ToString("F1") + " con escala (0.6, 1, 0.6).");
            }
            float h = alto0 * sy;
            float w = AnchoBaseTorre * sxz;
            Vector3 cima = new Vector3(torre.position.x, torre.position.y + h, torre.position.z);
            // Los tres haces viejos (T1386/T1402, tenues) se apagan por nombre; los nuevos son más largos y brillantes.
            for (int i = 0; i < 3; i++)
            {
                Transform viejo = pa == null ? null : pa.Find("Haz_" + i);
                if (viejo != null)
                {
                    viejo.gameObject.SetActive(false);
                    informe.AppendLine("Haz viejo apagado: " + viejo.name);
                }
            }
            Transform grupo = new GameObject("Torre").transform;
            grupo.SetParent(o.Raiz, false);
            Vector3 af = new Vector3(adelante.x, 0f, adelante.z);
            af = af.sqrMagnitude < 0.01f ? Vector3.left : af.normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, af).normalized;
            Vector3[] direcciones = { (Vector3.up - derecha * 0.34f).normalized, Vector3.up, (Vector3.up + derecha * 0.34f).normalized };
            Material haz = MatHaz();
            for (int i = 0; i < direcciones.Length; i++)
            {
                float largo = (i == 1 ? 1.25f : 0.95f) * Mathf.Max(h, 60f);
                int antesHijos = grupo.childCount;
                KitS2c.Halo(o, grupo, cima + Vector3.up * 10f, cima.y, KitS2c.Fria);
                if (grupo.childCount == antesHijos)
                {
                    informe.AppendLine("Haz " + i + ": KitS2c.Halo no creó nada.");
                    continue;
                }
                Transform hz = grupo.GetChild(grupo.childCount - 1);
                hz.name = "HazNuevo_" + i;
                hz.GetComponent<MeshRenderer>().sharedMaterial = haz;
                hz.rotation = Quaternion.FromToRotation(Vector3.up, -direcciones[i]);
                hz.position = cima + direcciones[i] * largo;
                hz.localScale = new Vector3(largo * (i == 1 ? 0.12f : 0.16f), largo, largo * (i == 1 ? 0.12f : 0.16f));
            }
            // Cresta emisiva: una placa por cara en lo alto y una tapa, con el mismo material de emisión de noche (azul frío, brillante).
            Material cresta = MatEmisiva("T1407_CrestaTorre", new Color(1.6f, 2.4f, 3.6f));
            float alta = Mathf.Max(4f, h * 0.1f);
            Vector3[] caras = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            for (int i = 0; i < caras.Length; i++)
            {
                Vector3 centro = cima - Vector3.up * (alta * 0.7f) + caras[i] * (w * 0.5f + 0.25f);
                GameObject placa = KitS2c.Losa(o, grupo, "Cresta_" + i, centro, Quaternion.LookRotation(caras[i]), new Vector3(w * 0.8f, alta, 0.5f), cresta, 4f, false);
                Estatico(placa);
            }
            Estatico(KitS2c.Losa(o, grupo, "Cresta_Tapa", cima + Vector3.up * 0.4f, Quaternion.identity, new Vector3(w * 0.9f, 0.8f, w * 0.9f), cresta, 4f, false));
            informe.AppendLine("Torre: 3 haces nuevos (HaloLuz, intensidad 1.7), 4 placas de cresta emisivas de " + (w * 0.8f).ToString("F1") + " x " + alta.ToString("F1") + " m y una tapa, en la punta (y " + cima.y.ToString("F0") + ").");
        }

        // ---------------------------------------------------------------- Placas y luz de sodio bajo el tablero

        private static void PonerPlacas(KitS2c.Obra o, Transform raiz)
        {
            Transform grupo = new GameObject("Placas").transform;
            grupo.SetParent(o.Raiz, false);
            List<Vector3> columnas = T.Columnas(T.Principal());
            List<Bounds> ocupados = Ocupados(raiz, o.Raiz);
            Material placaMat = MatEmisiva("T1407_PlacaSodio", new Color(3.4f, 1.9f, 0.7f));
            Material halo = KitS2c.Mat("T1407_HaloSodio", () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/HaloLuz"));
                m.SetColor("_Color", KitS2c.Sodio);
                m.SetFloat("_Intensidad", 0.6f);
                m.SetFloat("_Borde", 3f);
                m.SetFloat("_Caida", 1.6f);
                m.SetTexture("_Ruido", KitS2c.TexMuro());
                m.SetTextureScale("_Ruido", new Vector2(3f, 1f));
                return m;
            });
            int n = 0, luces = 0;
            for (float x = -318f; x <= -100f; x += 18f)
            {
                if (columnas.Any(q => Mathf.Abs(q.x - x) < 3.5f))
                {
                    informe.AppendLine("Placa en x " + x + ": junto a una columna, se omite.");
                    continue;
                }
                float y = BajoLosa(raiz, x, ZPlacas);
                if (y < 0f)
                {
                    informe.AppendLine("Placa en x " + x + ": no hay losa del tablero encima, se omite.");
                    continue;
                }
                Vector3 centro = new Vector3(x, y - 0.06f, ZPlacas);
                Bounds caja = new Bounds(centro - Vector3.up * 0.3f, new Vector3(3f, 0.8f, 1.5f));
                if (ocupados.Any(b => b.Intersects(caja)))
                {
                    informe.AppendLine("Placa en x " + x + ": choca con algo visible, se omite.");
                    continue;
                }
                GameObject placa = KitS2c.Losa(o, grupo, "PlacaSodio_" + n, centro, Quaternion.identity, new Vector3(2.6f, 0.12f, 0.9f), placaMat, 2f, false);
                Estatico(placa);
                int antesHijos = grupo.childCount;
                KitS2c.Halo(o, grupo, centro - Vector3.up * 0.2f, 0f, KitS2c.Sodio);
                if (grupo.childCount > antesHijos)
                {
                    Transform h = grupo.GetChild(grupo.childCount - 1);
                    h.name = "HaloSodio_" + n;
                    h.GetComponent<MeshRenderer>().sharedMaterial = halo;
                }
                if (n % 3 == 1 && luces < MaxLucesNuevas)
                {
                    Light l = KitS2c.Luz(grupo, centro - Vector3.up * 0.5f, LightType.Spot, KitS2c.Sodio, KitS2c.IntensidadPoste * 0.9f, 14f, 105f, Vector3.down);
                    l.gameObject.name = "LuzSodioPlaca_" + n;
                    luces++;
                }
                informe.AppendLine("Placa de sodio " + n + " en " + centro.ToString("F1") + " (losa en y " + y.ToString("F1") + ").");
                n++;
            }
            informe.AppendLine("Placas emisivas bajo el tablero: " + n + ", con " + luces + " luces de sodio (sin sombras).");
        }

        // ---------------------------------------------------------------- Validar

        [MenuItem("Mecha Cyberpunk/T-1407/Autopista ronda 5 (validar)")]
        public static void Validar()
        {
            StringBuilder s = new StringBuilder("T-1407 validar Autopista\n");
            int fallos = 0;
            List<(Vector3 desde, Vector3 hacia, float puntaje)> camaras = LeerCamaras(out Vector3? sitio, out float alto, out float ancho);
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mia = raiz.Find(NombreRaiz);
                if (mia == null)
                {
                    s.AppendLine("FALLO: no existe " + NombreRaiz);
                    fallos++;
                }
                else
                {
                    Light[] luces = mia.GetComponentsInChildren<Light>(true);
                    s.AppendLine("Luces nuevas: " + luces.Length + " (tope " + MaxLucesNuevas + ")");
                    fallos += luces.Length > MaxLucesNuevas ? 1 : 0;
                    foreach (Light l in luces)
                    {
                        if (l.shadows != LightShadows.None || l.type != LightType.Spot)
                        {
                            s.AppendLine("FALLO: luz " + l.name + " con sombras o que no es spot.");
                            fallos++;
                        }
                    }
                    int haces = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("HazNuevo_"));
                    int crestas = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Cresta_"));
                    s.AppendLine("Haces nuevos: " + haces + " (3 esperados); piezas de cresta: " + crestas + " (5 esperadas).");
                    if (haces != 3 || crestas != 5)
                    {
                        s.AppendLine("FALLO: faltan haces o cresta de la torre.");
                        fallos++;
                    }
                    List<Bounds> ocupados = Ocupados(raiz, mia);
                    Transform torre = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "TorreReflectores");
                    if (torre != null)
                    {
                        Bounds? bt = Caja(torre.gameObject, false);
                        s.AppendLine("Torre en " + torre.position.ToString("F1") + ", escala " + torre.localScale.ToString("F2") + (sitio.HasValue ? ", pedida en " + sitio.Value.ToString("F1") : ""));
                        if (bt.HasValue)
                        {
                            Bounds huella = new Bounds(new Vector3(bt.Value.center.x, 40f, bt.Value.center.z), new Vector3(bt.Value.size.x, 80f, bt.Value.size.z));
                            if (ocupados.Any(b => b.Intersects(huella)))
                            {
                                s.AppendLine("CHOQUE: la torre se encima con algo visible del prefab.");
                                fallos++;
                            }
                            Transform acantilado = raiz.Find("Acantilado");
                            if (acantilado != null && acantilado.GetComponentsInChildren<Renderer>(true).Any(r => r.bounds.Intersects(huella)))
                            {
                                s.AppendLine("AVISO: la caja de la torre toca la caja del cerro (puede ser solo la caja; revisar en la foto).");
                            }
                        }
                    }
                    foreach (Renderer r in mia.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("PlacaSodio_")))
                    {
                        float bajo = BajoLosa(raiz, r.bounds.center.x, r.bounds.center.z);
                        if (bajo < 0f || Mathf.Abs(r.bounds.max.y - bajo) > 0.3f)
                        {
                            s.AppendLine("FLOTA o enterrada: " + r.name + " (tope " + r.bounds.max.y.ToString("F2") + ", losa " + bajo.ToString("F2") + ")");
                            fallos++;
                        }
                    }
                    if (camaras.Count > 0 && torre != null)
                    {
                        GameObject go = new GameObject("T1407_CamValidar") { hideFlags = HideFlags.HideAndDontSave };
                        try
                        {
                            Camera cam = go.AddComponent<Camera>();
                            cam.enabled = false;
                            cam.fieldOfView = Campo;
                            cam.aspect = Aspecto;
                            Bounds? bt = Caja(torre.gameObject, false);
                            for (int i = 0; i < Mathf.Min(2, camaras.Count); i++)
                            {
                                cam.transform.SetPositionAndRotation(camaras[i].desde, Quaternion.LookRotation(camaras[i].hacia - camaras[i].desde));
                                if (bt.HasValue)
                                {
                                    Vector3 v = cam.WorldToViewportPoint(new Vector3(bt.Value.center.x, bt.Value.max.y, bt.Value.center.z));
                                    s.AppendLine("CAM" + i + ": punta de la torre en pantalla (" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + "), a " + Vector3.Distance(camaras[i].desde, bt.Value.center).ToString("F0") + " m"
                                        + (v.z > 0f && v.x > 0.02f && v.x < 0.6f ? "" : " (fuera del cuadro previsto)"));
                                }
                                if (ocupados.Any(b => b.Contains(camaras[i].desde)))
                                {
                                    s.AppendLine("FALLO: la cámara " + i + " está dentro de algo visible del prefab.");
                                    fallos++;
                                }
                            }
                        }
                        finally
                        {
                            Object.DestroyImmediate(go);
                        }
                    }
                    else
                    {
                        s.AppendLine("AVISO: sin " + RutaCamara + " o sin torre; no se revisó el cuadro.");
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            s.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1407-validar.txt", s.ToString());
            Debug.Log("[Arte] T-1407 autopista validar:\n" + s);
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>k18 con la mejor cámara de Logs/t1407-camara.txt y k18b con la segunda. No guarda nada; corrida de Unity aparte de Aplicar.</summary>
        [MenuItem("Mecha Cyberpunk/T-1407/Autopista ronda 5 (fotos)")]
        public static void Fotos()
        {
            List<(Vector3 desde, Vector3 hacia, float puntaje)> camaras = LeerCamaras(out _, out _, out _);
            if (camaras.Count == 0)
            {
                Debug.LogError("[Arte] T-1407 fotos: no hay cámaras en " + RutaCamara + ". Corre BuscarCamara y Aplicar antes.");
                return;
            }
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1407");
            Directory.CreateDirectory("Logs");
            string[] nombres = { "k18", "k18b" };
            for (int i = 0; i < Mathf.Min(2, camaras.Count); i++)
            {
                string juego = "Logs/t1407-" + nombres[i] + "-juego.png";
                ColocarS2cLook.Foto(camaras[i].desde, camaras[i].hacia, juego, Campo);
                ColocarS2.LadoALado("imageassets/referencias/look/K18_AutopistaElevada.png", juego, "docs/capturas/t-1407/" + nombres[i] + ".png");
            }
        }
    }
}
