using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-510: da vida a la gente de banqueta de T-508 (que estaba congelada) y pone a los guardias de Neón Alto. Sobre lo que ya hay en Ciudad_Prologo:
    /// 1 de cada 4 personas pasa a RobotHombre; las parejas que platican asienten o discuten (clip en bucle, cada pareja a su rapidez y fase) y 4 de cada
    /// 10 parejas, y la mitad de los solitarios, caminan por la banqueta de ida y vuelta (solo donde el recorrido está libre); los demás se quedan de pie
    /// y respiran. 3 GuardiaNova quedan parados en Neón Alto, lejos de las puertas, del Casino y de la ruta a EL HACKER. Se puede repetir; si se rehace
    /// T-508 (ColocarT508.Colocar), vuelve a correr Aplicar. Nada lleva collider ni sombra.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT510.Aplicar
    /// </summary>
    public static class ColocarT510
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaizGente = "T508_CallesLlenas";
        public const string NombreRaizGuardias = "T510_Guardias";
        public const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";

        /// <summary>Uno de cada cuántos de la gente son robots; fracción de las parejas que caminan; y de los solitarios.</summary>
        public const int CadaCuantosRobot = 4;
        public const float FraccionParejasQueCaminan = 0.4f;
        public const float FraccionSolosQueCaminan = 0.5f;
        public const float FraccionParejasEnojadas = 0.25f;
        public const int GuardiasNeon = 3;

        private const float RadioPersona = 0.4f;
        private const float AltoPersona = 1.8f;
        private const float PasoRecorrido = 0.5f;
        private const float MaximoPorLado = 5f;
        private const float RecorridoMinimo = 4f;
        private const float DiferenciaDeSueloMaxima = 0.12f;
        private const float DistanciaGuardiaARuta = 8f;
        private const float SeparacionGuardias = 12f;
        private const float DistanciaGuardiaAGente = 3.5f;
        private const float ZonaNeonX = 47f;
        private const float ZonaNeonZMin = 12f;
        private const float ZonaNeonZMax = 84f;

        /// <summary>Casino (57.5, 47) y EL HACKER (74.5, 47): el camino que no se tapa con guardias, saliendo de la calle de Neón Alto.</summary>
        private static readonly Vector3[] RutaHacker = { new Vector3(ZonaNeonX, 0f, 47f), new Vector3(57.5f, 0f, 47f), new Vector3(74.5f, 0f, 47f) };

        private static readonly StringBuilder Informe = new StringBuilder();
        private static List<Bounds> ocupado;

        [MenuItem("Mecha Cyberpunk/T-510/Gente con vida y guardias")]
        public static void Aplicar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject raiz = GameObject.Find(NombreRaizGente);
            if (raiz == null)
            {
                Debug.LogError("[Arte] T-510: falta " + NombreRaizGente + " (corre ColocarT508.Colocar).");
                return;
            }
            GameObject viejosGuardias = GameObject.Find(NombreRaizGuardias);
            if (viejosGuardias != null)
            {
                Object.DestroyImmediate(viejosGuardias);
            }

            List<Transform> gente = raiz.GetComponentsInChildren<VisualPersonaje>(true)
                .Select(v => v.transform).Where(t => t.name.StartsWith("Gente_")).OrderBy(t => t.name).ToList();
            int robots = Robots(gente);
            gente = raiz.GetComponentsInChildren<VisualPersonaje>(true)
                .Select(v => v.transform).Where(t => t.name.StartsWith("Gente_")).OrderBy(t => t.name).ToList();

            // Obstáculos sin la gente que se va a mover: sus cuerpos no cuentan.
            foreach (Transform t in gente) { t.gameObject.SetActive(false); }
            Physics.SyncTransforms();
            ColocarNoche.ReunirObstaculos();
            foreach (Transform t in gente) { t.gameObject.SetActive(true); }
            ocupado = new List<Bounds>();

            List<List<Transform>> grupos = Agrupar(gente);
            var conCaminata = new List<List<Transform>>();
            foreach (List<Transform> g in grupos)
            {
                if (g[0].GetComponent<GenteDeBanqueta>() != null)
                {
                    Informe.AppendLine("ya procesado: " + g[0].name);
                    ocupado.AddRange(g.Select(t => Caja(t.position)));
                    continue;
                }
                bool pareja = g.Count == 2;
                float fraccion = pareja ? FraccionParejasQueCaminan : FraccionSolosQueCaminan;
                if (EsDeCliente(g[0]) || Azar("camina" + Clave(g[0])) >= fraccion)
                {
                    conCaminata.Add(null);
                    PonerParado(g);
                }
                else
                {
                    conCaminata.Add(g);
                }
            }
            int caminan = 0;
            int quietos = 0;
            foreach (List<Transform> g in conCaminata.Where(c => c != null))
            {
                if (PonerCaminante(g))
                {
                    caminan += g.Count;
                }
                else
                {
                    PonerParado(g);
                }
            }
            foreach (List<Transform> g in grupos)
            {
                if (g[0].GetComponent<GenteDeBanqueta>() != null || g[0].GetComponentInChildren<GenteDeBanqueta>() != null)
                {
                    GenteDeBanqueta c = g[0].GetComponentInChildren<GenteDeBanqueta>();
                    if (c != null && c.Modo != ModoGente.Caminar) { quietos += g.Count; }
                }
            }
            int guardias = PonerGuardias();
            Informe.AppendLine("gente=" + gente.Count + " robots=" + robots + " caminan=" + caminan + " paradas=" + quietos + " guardias=" + guardias);

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t510-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-510:\n" + Informe);
        }

        // ------------------------------------------------------------------ robots

        private static bool EsRobot(Transform t)
        {
            Animator a = t.GetComponentInChildren<Animator>(true);
            return a != null && a.runtimeAnimatorController != null && AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("/RobotHombre/");
        }

        private static int Robots(List<Transform> gente)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + "Visual_RobotHombre.prefab");
            int total = 0;
            foreach (Transform t in gente)
            {
                if (t.GetComponentInChildren<GenteDeBanqueta>(true) != null)
                {
                    total += EsRobot(t) ? 1 : 0;
                    continue;
                }
                if (Hash(Clave(t)) % CadaCuantosRobot != 0)
                {
                    continue;
                }
                total++;
                if (EsRobot(t))
                {
                    continue;
                }
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t.parent);
                nuevo.name = t.name;
                nuevo.transform.SetPositionAndRotation(t.position, t.rotation);
                ConfigurarDeFondo(nuevo);
                Object.DestroyImmediate(t.gameObject);
            }
            return total;
        }

        /// <summary>Sin colliders ni sombra, el cuerpo se corta de lejos (LOD a 4.5 % de pantalla) y el Animator se apaga fuera de pantalla (60 FPS).</summary>
        public static void ConfigurarDeFondo(GameObject go)
        {
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            SkinnedMeshRenderer cuerpo = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (cuerpo != null)
            {
                cuerpo.shadowCastingMode = ShadowCastingMode.Off;
                cuerpo.updateWhenOffscreen = false;
                LODGroup grupo = go.GetComponent<LODGroup>();
                if (grupo == null)
                {
                    grupo = go.AddComponent<LODGroup>();
                }
                grupo.SetLODs(new[] { new LOD(0.045f, new Renderer[] { cuerpo }) });
                grupo.RecalculateBounds();
            }
            Animator anim = go.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.cullingMode = AnimatorCullingMode.CullCompletely;
            }
        }

        // ------------------------------------------------------------------ grupos y modos

        private const float DistanciaDePareja = 1.5f;

        /// <summary>
        /// Une cada "Gente_Platica_zona_n" con su "..._nb" más cercano (los nombres se repiten entre calles, por eso se usa la distancia); lo demás va solo.
        /// </summary>
        private static List<List<Transform>> Agrupar(List<Transform> gente)
        {
            var salida = new List<List<Transform>>();
            var usados = new HashSet<Transform>();
            foreach (Transform t in gente)
            {
                if (usados.Contains(t))
                {
                    continue;
                }
                usados.Add(t);
                var grupo = new List<Transform> { t };
                if (t.name.StartsWith("Gente_Platica_") && !t.name.EndsWith("b"))
                {
                    Transform pareja = gente.Where(o => !usados.Contains(o) && o.name == t.name + "b" && Vector3.Distance(o.position, t.position) < DistanciaDePareja)
                        .OrderBy(o => Vector3.Distance(o.position, t.position)).FirstOrDefault();
                    if (pareja != null)
                    {
                        usados.Add(pareja);
                        grupo.Add(pareja);
                    }
                }
                salida.Add(grupo);
            }
            return salida;
        }

        /// <summary>Clave estable de un grupo: nombre y lugar (los nombres se repiten).</summary>
        private static string Clave(Transform t) => t.name + "@" + Mathf.RoundToInt(t.position.x) + "," + Mathf.RoundToInt(t.position.z);

        private static bool EsDeCliente(Transform t) => t.name.StartsWith("Gente_Cliente_");

        private static Bounds Caja(Vector3 pos) => new Bounds(pos + Vector3.up * AltoPersona * 0.5f, new Vector3(RadioPersona * 2f, AltoPersona, RadioPersona * 2f));

        private static AnimationClip Clip(string modelo, string nombre)
        {
            Personaje.Visual.Editor.CreadorVisualTripo.ClipDeModelo(modelo, nombre, out AnimationClip clip);
            return clip;
        }

        /// <summary>De pie: las parejas platican (asienten o discuten) y los solos o clientes respiran.</summary>
        private static void PonerParado(List<Transform> grupo)
        {
            string clave = Clave(grupo[0]);
            bool pareja = grupo.Count == 2;
            bool enojados = pareja && Azar("enojo" + clave) < FraccionParejasEnojadas;
            AnimationClip clip = pareja ? (enojados ? Clip("man", Personaje.Visual.Editor.CreadorVisualTripo.ClipEnojo) : Clip("woman2", Personaje.Visual.Editor.CreadorVisualTripo.ClipAcuerdo)) : null;
            int i = 0;
            foreach (Transform t in grupo)
            {
                var c = t.gameObject.AddComponent<GenteDeBanqueta>();
                float fase = Azar("fase" + Clave(t));
                float ritmo = Mathf.Lerp(0.75f, 1.25f, Azar("ritmo" + Clave(t)));
                c.Configurar(clip != null ? ModoGente.Platicar : ModoGente.Quieto, fase, ritmo, clip, Vector3.zero, Vector3.zero, 1f, 0f, null);
                ocupado.Add(Caja(t.position));
                Informe.AppendLine((clip != null ? (enojados ? "discute " : "platica ") : "quieto ") + t.name);
                i++;
            }
        }

        private static bool PonerCaminante(List<Transform> grupo)
        {
            Vector3 centro = grupo.Aggregate(Vector3.zero, (s, t) => s + t.position) / grupo.Count;
            Vector3 tangente = grupo.Count == 2
                ? (grupo[1].position - grupo[0].position)
                : Vector3.Cross(Vector3.up, grupo[0].forward);
            tangente.y = 0f;
            if (tangente.sqrMagnitude < 0.01f)
            {
                return false;
            }
            tangente.Normalize();
            float mas = Alcance(grupo, centro, tangente, 1f);
            float menos = Alcance(grupo, centro, tangente, -1f);
            if (mas + menos < RecorridoMinimo)
            {
                Informe.AppendLine("no camina (poco sitio " + (mas + menos).ToString("0.0") + " m): " + grupo[0].name);
                return false;
            }
            float rapidez = Mathf.Lerp(0.7f, 1.2f, Azar("vel" + Clave(grupo[0])));
            float pausa = Mathf.Lerp(1.5f, 4.5f, Azar("pausa" + Clave(grupo[0])));
            float fase = Azar("fasec" + Clave(grupo[0]));
            foreach (Transform t in grupo)
            {
                Vector3 a = t.position - tangente * (menos - 0.1f);
                Vector3 b = t.position + tangente * (mas - 0.1f);
                a.y = ColocarNoche.Suelo(a);
                b.y = ColocarNoche.Suelo(b);
                // El visual cuelga de un objeto que es el que camina.
                GameObject envoltorio = new GameObject(t.name);
                envoltorio.transform.SetParent(t.parent, false);
                envoltorio.transform.SetPositionAndRotation(t.position, t.rotation);
                t.SetParent(envoltorio.transform, true);
                t.name = "Visual";
                var c = t.gameObject.AddComponent<GenteDeBanqueta>();
                c.Configurar(ModoGente.Caminar, fase, 1f, null, a, b, rapidez, pausa, envoltorio.transform);
                Informe.AppendLine("camina " + envoltorio.name + " " + (mas + menos).ToString("0.0") + " m a " + rapidez.ToString("0.00") + " m/s");
            }
            // El recorrido queda ocupado para los que vienen después (y para los guardias).
            Vector3 p0 = centro - tangente * menos;
            Vector3 p1 = centro + tangente * mas;
            Bounds recorrido = new Bounds(p0 + Vector3.up * AltoPersona * 0.5f, new Vector3(RadioPersona * 2f, AltoPersona, RadioPersona * 2f));
            recorrido.Encapsulate(p1 + Vector3.up * AltoPersona * 0.5f);
            ocupado.Add(recorrido);
            return true;
        }

        /// <summary>Cuántos metros se puede caminar en esa dirección sin chocar con nada, sin salirse de la banqueta ni cambiar de nivel.</summary>
        private static float Alcance(List<Transform> grupo, Vector3 centro, Vector3 tangente, float signo)
        {
            float libre = 0f;
            float sueloBase = ColocarNoche.Suelo(centro);
            for (float d = PasoRecorrido; d <= MaximoPorLado; d += PasoRecorrido)
            {
                foreach (Transform t in grupo)
                {
                    Vector3 p = t.position + tangente * signo * d;
                    float suelo = ColocarNoche.Suelo(p);
                    if (float.IsNaN(suelo) || float.IsNaN(sueloBase) || Mathf.Abs(suelo - sueloBase) > DiferenciaDeSueloMaxima)
                    {
                        return libre;
                    }
                    p.y = suelo;
                    Bounds caja = Caja(p);
                    if (!ColocarNoche.Libre(caja) || ocupado.Any(b => b.Intersects(caja)))
                    {
                        return libre;
                    }
                }
                libre = d;
            }
            return libre;
        }

        // ------------------------------------------------------------------ guardias

        private static int PonerGuardias()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + "Visual_GuardiaNova.prefab");
            Transform raiz = new GameObject(NombreRaizGuardias).transform;
            var candidatos = new List<(Vector3 pos, Vector3 mira, float distRuta)>();
            for (float z = ZonaNeonZMin; z <= ZonaNeonZMax; z += 2f)
            {
                foreach (float lado in new[] { 1f, -1f })
                {
                    Vector3 origen = new Vector3(ZonaNeonX, 1.4f, z);
                    if (!Physics.Raycast(origen, Vector3.right * lado, out RaycastHit golpe, 16f, ~0, QueryTriggerInteraction.Ignore)
                        || Vector3.Dot(golpe.normal, Vector3.left * lado) < 0.92f || golpe.collider.attachedRigidbody != null)
                    {
                        continue;
                    }
                    Vector3 pos = new Vector3(golpe.point.x - lado * 0.9f, 0f, z);
                    float suelo = ColocarNoche.Suelo(pos);
                    if (float.IsNaN(suelo))
                    {
                        continue;
                    }
                    pos.y = suelo;
                    Bounds caja = Caja(pos);
                    if (!ColocarNoche.Libre(caja) || ocupado.Any(b => b.Intersects(caja)))
                    {
                        continue;
                    }
                    float ruta = DistanciaARuta(pos);
                    if (ruta < DistanciaGuardiaARuta)
                    {
                        continue;
                    }
                    candidatos.Add((pos, new Vector3(-lado, 0f, 0f), ruta));
                }
            }
            // Los más cercanos a la ruta (los más visibles) y separados entre sí, lejos de la gente.
            var gente = Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(v => v.transform.position).ToList();
            var elegidos = new List<Vector3>();
            foreach (var c in candidatos.OrderBy(c => c.distRuta))
            {
                if (elegidos.Count >= GuardiasNeon)
                {
                    break;
                }
                if (elegidos.Any(e => Vector3.Distance(e, c.pos) < SeparacionGuardias) || gente.Any(g => Vector3.Distance(g, c.pos) < DistanciaGuardiaAGente))
                {
                    continue;
                }
                elegidos.Add(c.pos);
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
                go.name = "Guardia_" + elegidos.Count;
                go.transform.SetPositionAndRotation(c.pos, Quaternion.LookRotation(c.mira, Vector3.up));
                ConfigurarDeFondo(go);
                go.AddComponent<GenteDeBanqueta>().Configurar(ModoGente.Quieto, Azar("g" + go.name), 0.9f, null, Vector3.zero, Vector3.zero, 1f, 0f, null);
                Informe.AppendLine("guardia " + go.name + " " + c.pos + " a " + c.distRuta.ToString("0.0") + " m de la ruta del Hacker");
            }
            return elegidos.Count;
        }

        private static float DistanciaARuta(Vector3 p)
        {
            float mejor = float.MaxValue;
            for (int i = 0; i < RutaHacker.Length - 1; i++)
            {
                Vector3 a = RutaHacker[i];
                Vector3 b = RutaHacker[i + 1];
                Vector3 ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(new Vector3(p.x, 0f, p.z) - a, ab) / ab.sqrMagnitude);
                mejor = Mathf.Min(mejor, Vector3.Distance(new Vector3(p.x, 0f, p.z), a + ab * t));
            }
            return mejor;
        }

        // ------------------------------------------------------------------ azar estable

        private static int Hash(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s)
                {
                    h = (h ^ c) * 16777619;
                }
                return (int)(h & 0x7fffffff);
            }
        }

        private static float Azar(string clave) => (Hash(clave) % 10000) / 10000f;
    }
}
