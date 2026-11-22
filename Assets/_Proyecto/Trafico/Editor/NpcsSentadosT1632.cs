using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Trafico.Peatones;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1632 (T18): pone civiles sentados (NpcSentado, clip PJ01_Sentado) en las bancas de Ciudad_Prologo (las Banca_Sentable del parque, las PZ01_Banca sueltas) y en
    /// taburetes y sillas con altura de asiento razonable, hasta <see cref="Maximo"/>. Aplicar es idempotente: borra la raíz NpcsSentados_T1632 y la rehace.
    /// Diagnostico escribe Logs/t1632.txt (en Play incluye la diferencia cadera-asiento de cada NPC).
    /// </summary>
    public static class NpcsSentadosT1632
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaiz = "NpcsSentados_T1632";
        public const string RutaLog = "Logs/t1632.txt";
        public const string CarpetaClips = "Assets/Arte/Personajes/PJ01/Clips";
        public const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs";
        public const int Maximo = 16;
        private const float SeparacionEnBanca = 0.4f;

        private static readonly string[] Visuales =
        {
            "NP07_Pescador_a", "NP14_Oficinista_a", "NP08_Turista_b", "NP12_Abuela_a", "NP11_Estibador_b", "NP13_Repartidor_a", "NP10_Elotero_a", "NP14_Oficinista_b",
            "NP09_Corredor_a", "NP08_Turista_a", "NP13_Repartidor_b", "NP11_Estibador_a",
        };

        private struct Lugar
        {
            public string nombre;
            public Transform ancla;
            public float lateral;
            public float altura;
            public bool frenteMenosZ;
            public GameObject asiento;
            public int banca; // misma banca = compañeros
            public bool esBanca;
        }

        [MenuItem("Mecha Cyberpunk/T-1632/Aplicar NPCs sentados")]
        public static void Aplicar()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject viejo = GameObject.Find(NombreRaiz);
            while (viejo != null)
            {
                Object.DestroyImmediate(viejo);
                viejo = GameObject.Find(NombreRaiz);
            }
            AnimationClip sentado = CargarClip("PJ01_Sentado");
            AnimationClip susto = CargarClip("NPC_Asustarse");
            AnimationClip huir = CargarClip("NPC_Huir_Corriendo");
            AnimationClip levantar = ExisteClip("PJ01_Sentado_Levantarse") ? CargarClip("PJ01_Sentado_Levantarse") : (ExisteClip("NPC_Sentado_Levantarse") ? CargarClip("NPC_Sentado_Levantarse") : null);
            if (sentado == null)
            {
                Debug.LogError("[Trafico] T-1632: no encuentro el clip PJ01_Sentado en " + CarpetaClips);
                return;
            }
            var prefabs = Visuales.Select(v => AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + "/Visual_" + v + ".prefab")).Where(p => p != null).ToList();
            if (prefabs.Count == 0)
            {
                Debug.LogError("[Trafico] T-1632: no hay visuales Visual_NP* para sentar");
                return;
            }

            var raiz = new GameObject(NombreRaiz);
            var sb = new StringBuilder();
            sb.AppendLine("T-1632 Aplicar: clips sentado=" + sentado.name + " susto=" + (susto != null) + " huir=" + (huir != null));
            List<Lugar> lugares = ElegirLugares(raiz.transform, sb);
            var creados = new List<NpcSentado>();
            var porBanca = new Dictionary<int, List<NpcSentado>>();
            for (int i = 0; i < lugares.Count; i++)
            {
                Lugar l = lugares[i];
                var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i % prefabs.Count], raiz.transform);
                npc.name = "Sentado_" + (i + 1) + "_" + l.nombre;
                var sentadoNpc = npc.AddComponent<NpcSentado>();
                float desfase = Mathf.Repeat(i * 0.37f, 1f);
                float rapidez = 0.92f + (i % 5) * 0.04f;
                float giro = ((i * 7) % 11 - 5) * 1.2f;
                sentadoNpc.Configurar(l.ancla, l.lateral, l.frenteMenosZ, l.asiento, sentado, susto, huir, desfase, rapidez, giro);
                sentadoNpc.FijarAlturaAsiento(l.altura);
                sentadoNpc.FijarClipLevantarse(levantar); // T-1653: null si todavía no existe el clip
                // Colocación aproximada para el editor (el clip se alinea solo al jugar: asiento a 0.46 m sobre el origen).
                Vector3 asientoPos = sentadoNpc.PosicionAsiento;
                Vector3 frente = sentadoNpc.FrenteAsiento;
                npc.transform.SetPositionAndRotation(asientoPos - Vector3.up * 0.46f, Quaternion.LookRotation(frente, Vector3.up));
                creados.Add(sentadoNpc);
                if (l.esBanca)
                {
                    if (!porBanca.TryGetValue(l.banca, out List<NpcSentado> lista))
                    {
                        porBanca[l.banca] = lista = new List<NpcSentado>();
                    }
                    lista.Add(sentadoNpc);
                }
                sb.AppendLine("  " + npc.name + " visual=" + prefabs[i % prefabs.Count].name + " altura=" + l.altura.ToString("0.00") + " en " + asientoPos);
            }
            foreach (List<NpcSentado> par in porBanca.Values.Where(p => p.Count == 2))
            {
                par[0].Acompanar(par[1]);
                par[1].Acompanar(par[0]);
            }
            sb.AppendLine("NPCs sentados: " + creados.Count + " (tope " + Maximo + ")");
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Escribir(sb.ToString());
            Debug.Log("[Trafico] T-1632: " + creados.Count + " NPCs sentados; ver " + RutaLog);
        }

        /// <summary>
        /// T-1640 (T29): quita TODOS los civiles sentados de Ciudad_Prologo (la raíz NpcsSentados_T1632 y cualquier otro NPC con NpcSentado o NpcSentadoBanca).
        /// El código de sentarse y las bancas se quedan. Escribe Logs/t1640.txt.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1640/Quitar NPCs sentados")]
        public static void Quitar()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var sb = new StringBuilder();
            int quitados = 0;
            GameObject viejo = GameObject.Find(NombreRaiz);
            while (viejo != null)
            {
                quitados += viejo.GetComponentsInChildren<NpcSentado>(true).Length;
                sb.AppendLine("quita raiz " + viejo.name + " con " + viejo.transform.childCount + " hijos");
                Object.DestroyImmediate(viejo);
                viejo = GameObject.Find(NombreRaiz);
            }
            foreach (GameObject r in escena.GetRootGameObjects())
            {
                foreach (MonoBehaviour mb in r.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb != null && (mb is NpcSentado || mb.GetType().Name == "NpcSentadoBanca"))
                    {
                        sb.AppendLine("quita " + Ruta(mb.transform) + " en " + mb.transform.position);
                        Object.DestroyImmediate(mb.gameObject);
                        quitados++;
                    }
                }
            }
            int quedan = Object.FindObjectsByType<NpcSentado>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length
                + Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(m => m != null && m.GetType().Name == "NpcSentadoBanca");
            sb.AppendLine("quitados: " + quitados + ", quedan sentados: " + quedan);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1640.txt", sb.ToString());
            Debug.Log("[Trafico] T-1640: " + sb.ToString());
        }

        [MenuItem("Mecha Cyberpunk/T-1640/Fotos antes")]
        public static void FotosAntes() { Fotos("antes"); }

        [MenuItem("Mecha Cyberpunk/T-1640/Fotos despues")]
        public static void FotosDespues() { Fotos("despues"); }

        /// <summary>Capturas de las bancas (con o sin sentados) en docs/capturas/_tareas/T29/. Va en una corrida aparte de Quitar.</summary>
        private static void Fotos(string sufijo)
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Directory.CreateDirectory("docs/capturas/_tareas/T29");
            var bancas = new List<Transform>();
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                bancas.AddRange(r.GetComponentsInChildren<Transform>(false).Where(t => t.name.StartsWith("Banca_Sentable")));
            }
            bancas = bancas.OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
            Debug.Log("[Trafico] T-1640 fotos " + sufijo + ": bancas " + bancas.Count);
            for (int i = 0; i < Mathf.Min(4, bancas.Count); i++)
            {
                Vector3 c = bancas[i].position;
                FotoCamara("banca" + i + "_" + sufijo, c + new Vector3(0f, 3f, -4.5f), c + Vector3.up * 0.7f, 60f);
            }
            if (bancas.Count > 0)
            {
                Vector3 c = bancas[0].position;
                FotoCamara("parque_aerea_" + sufijo, c + new Vector3(0f, 45f, -30f), c, 50f);
            }
        }

        private static void FotoCamara(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            var go = new GameObject("CamFotosT1640");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.farClipPlane = 800f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes("docs/capturas/_tareas/T29/" + nombre + ".png", tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }

        [MenuItem("Mecha Cyberpunk/T-1632/Diagnostico")]
        public static void Diagnostico()
        {
            var sb = new StringBuilder();
            sb.AppendLine("T-1632 Diagnostico (" + (Application.isPlaying ? "Play" : "Edit") + ")");
            NpcSentado[] todos = Object.FindObjectsByType<NpcSentado>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(n => n.name).ToArray();
            sb.AppendLine("NpcSentado en la escena: " + todos.Length);
            float peor = 0f;
            foreach (NpcSentado n in todos)
            {
                Vector3? cadera = n.PosicionCadera;
                float? pie = n.AlturaPieMasBajo;
                string linea = "  " + n.name + " fase=" + n.FaseActual + " asiento=" + n.PosicionAsiento;
                if (Application.isPlaying && cadera.HasValue)
                {
                    float dif = n.DiferenciaCadera;
                    peor = Mathf.Max(peor, dif);
                    linea += " cadera=" + cadera.Value + " dif_cadera_asiento(m)=" + dif.ToString("0.000") + " pie_mas_bajo_sobre_piso=" + (pie.HasValue ? pie.Value.ToString("0.000") : "?")
                        + " variacion_clip=" + n.VariacionCaderaClip.ToString("0.000") + " compañero=" + (n.Companero != null);
                }
                else
                {
                    linea += " (sin medir: solo en Play)";
                }
                sb.AppendLine(linea);
            }
            if (Application.isPlaying)
            {
                sb.AppendLine("Peor diferencia cadera-asiento: " + peor.ToString("0.000") + " m (objetivo < 0.10)");
            }
            var temp = new GameObject("t1632_tmp");
            List<Lugar> lugares = ElegirLugares(temp.transform, sb);
            sb.AppendLine("Lugares que Aplicar usaría ahora: " + lugares.Count);
            Object.DestroyImmediate(temp);
            Escribir(sb.ToString());
            Debug.Log("[Trafico] T-1632 diagnóstico escrito en " + RutaLog);
        }

        // ------------------------------------------------------------------ lugares

        private static List<Lugar> ElegirLugares(Transform raiz, StringBuilder log)
        {
            var lugares = new List<Lugar>();
            var todos = new List<Transform>();
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r.name == NombreRaiz || r.name == "t1632_tmp")
                {
                    continue;
                }
                todos.AddRange(r.GetComponentsInChildren<Transform>(false));
            }

            // 1) Banca_Sentable (parque): sus dos asientos son hijos Asiento_Izquierdo y Asiento_Derecho; se llenan 2, 1, 2, 1...
            List<Transform> sentables = todos.Where(t => t.name.StartsWith("Banca_Sentable")).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
            log.AppendLine("Banca_Sentable en la escena: " + sentables.Count);
            for (int b = 0; b < sentables.Count; b++)
            {
                Transform izq = sentables[b].Cast<Transform>().FirstOrDefault(h => h.name == "Asiento_Izquierdo");
                Transform der = sentables[b].Cast<Transform>().FirstOrDefault(h => h.name == "Asiento_Derecho");
                var asientos = new List<Transform>();
                if (izq != null) { asientos.Add(izq); }
                if (der != null && b % 2 == 0) { asientos.Add(der); }
                foreach (Transform a in asientos)
                {
                    lugares.Add(new Lugar { nombre = "BancaSentable" + b, ancla = a, lateral = 0f, altura = NpcSentado.AlturaAsientoBanca, frenteMenosZ = true, asiento = a.gameObject, banca = 1000 + b, esBanca = true });
                }
            }

            // 2) PZ01_Banca sueltas (no las de dentro de una Banca_Sentable): 2, 1, 2, 1...
            List<Transform> viejas = todos.Where(t => t.name.StartsWith("PZ01_Banca") && !t.name.Contains("Sentable") && !TieneAncestro(t, "Banca_Sentable") && !TieneAncestro(t, "PZ01_Banca"))
                .OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
            log.AppendLine("PZ01_Banca sueltas: " + viejas.Count);
            for (int b = 0; b < viejas.Count; b++)
            {
                lugares.Add(new Lugar { nombre = "PZ01_" + b + "_i", ancla = viejas[b], lateral = -SeparacionEnBanca, altura = NpcSentado.AlturaAsientoBanca, frenteMenosZ = true, banca = b, esBanca = true });
                if (b % 2 == 0)
                {
                    lugares.Add(new Lugar { nombre = "PZ01_" + b + "_d", ancla = viejas[b], lateral = SeparacionEnBanca, altura = NpcSentado.AlturaAsientoBanca, frenteMenosZ = true, banca = b, esBanca = true });
                }
            }

            // 3) Taburetes y sillas sueltas con superficie de asiento entre 0.3 y 0.65 m (se descartan las demás y se anota por qué).
            List<Transform> sillas = todos.Where(t => EsSilla(t.name) && !TieneAncestro(t, "Banca_Sentable") && !TieneAncestro(t, "PZ01_Banca") && !t.name.StartsWith("S2n_")
                && !EsSilla(t.parent != null ? t.parent.name : "") && !TieneAncestro(t, "S2n_")).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
            log.AppendLine("Candidatos taburete/silla/banco: " + sillas.Count);
            foreach (Transform s in sillas)
            {
                Renderer[] rs = s.GetComponentsInChildren<Renderer>(false);
                if (rs.Length == 0)
                {
                    log.AppendLine("  descarta " + Ruta(s) + ": sin malla");
                    continue;
                }
                Bounds caja = rs[0].bounds;
                foreach (Renderer r in rs) { caja.Encapsulate(r.bounds); }
                Renderer asiento = rs.FirstOrDefault(r => r.name.Contains("Asiento"));
                float tope = asiento != null ? asiento.bounds.max.y : caja.max.y;
                float altura = tope - caja.min.y;
                if (altura < 0.3f || altura > 0.65f || caja.size.x > 1.2f || caja.size.z > 1.2f)
                {
                    log.AppendLine("  descarta " + Ruta(s) + ": altura de asiento " + altura.ToString("0.00") + ", tamaño " + caja.size);
                    continue;
                }
                var ancla = new GameObject("Ancla_" + s.name + "_" + lugares.Count).transform;
                ancla.SetParent(raiz, false);
                Vector3 hacia = DireccionAMesa(s, todos);
                ancla.SetPositionAndRotation(new Vector3(caja.center.x, caja.min.y, caja.center.z), Quaternion.LookRotation(-hacia, Vector3.up)); // frente = -forward
                lugares.Add(new Lugar { nombre = s.name, ancla = ancla, lateral = 0f, altura = altura, frenteMenosZ = true, banca = -1, esBanca = false });
                log.AppendLine("  usa " + Ruta(s) + ": altura de asiento " + altura.ToString("0.00"));
            }
            if (lugares.Count > Maximo)
            {
                log.AppendLine("Se recortan " + (lugares.Count - Maximo) + " lugares por el tope de " + Maximo);
                lugares = lugares.Take(Maximo).ToList();
            }
            return lugares;
        }

        private static bool EsSilla(string nombre)
        {
            foreach (string p in new[] { "Taburete", "Silla", "Banco" })
            {
                if (nombre == p || nombre.StartsWith(p + " ("))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Horizontal hacia la mesa más cercana (a menos de 3 m); sin mesa, el forward de la silla.</summary>
        private static Vector3 DireccionAMesa(Transform silla, List<Transform> todos)
        {
            Transform mejor = null;
            float mejorD = 3f;
            foreach (Transform t in todos)
            {
                if (t.name == "Mesa" || t.name.StartsWith("Mesa ("))
                {
                    float d = Vector3.ProjectOnPlane(t.position - silla.position, Vector3.up).magnitude;
                    if (d < mejorD)
                    {
                        mejorD = d;
                        mejor = t;
                    }
                }
            }
            Vector3 v = mejor != null ? Vector3.ProjectOnPlane(mejor.position - silla.position, Vector3.up) : Vector3.ProjectOnPlane(silla.forward, Vector3.up);
            return v.sqrMagnitude < 1e-4f ? Vector3.forward : v.normalized;
        }

        private static bool TieneAncestro(Transform t, string prefijo)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (p.name.StartsWith(prefijo))
                {
                    return true;
                }
            }
            return false;
        }

        private static string Ruta(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null && r.Length < 120; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }

        /// <summary>Clip (del FBX de PJ01/Clips, Humanoid) por nombre.</summary>
        private static bool ExisteClip(string nombre) => File.Exists(CarpetaClips + "/" + nombre + ".fbx");

        public static AnimationClip CargarClip(string nombre)
        {
            string ruta = CarpetaClips + "/" + nombre + ".fbx";
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => c.name == nombre)
                ?? AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        private static void Escribir(string texto)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, texto);
        }
    }
}
