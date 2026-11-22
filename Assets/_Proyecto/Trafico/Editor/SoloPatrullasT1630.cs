using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1630: los civiles ya no manejan. Sobre Ciudad_Prologo: apaga el tráfico civil en movimiento (TraficoCalle.circulanCiviles = false; el pool queda para refuerzos), pone carros civiles
    /// ESTACIONADOS junto a la banqueta (los Carro_V* robables de Vehiculos, quietos, alineados, sin pasar de los cruces ni encimarse) y pone semáforos en cada cruce de la red de calles
    /// (un poste ES01 por sentido que llega; los SemaforoCiclo viejos que ya hay en el cruce se adoptan y los maneja el mismo reloj). Las patrullas de ronda (PatrullaMundo) los obedecen.
    /// Todo lo nuevo cuelga de una raíz propia que se borra y se rehace: se puede repetir. Escribe Logs/t1630.txt.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.SoloPatrullasT1630.Aplicar (o .Diagnostico, que no cambia nada).
    /// </summary>
    public static class SoloPatrullasT1630
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaiz = "GTA_T1630_SoloPatrullas";
        public const string PrefijoEstacionado = "Estacionado_";
        public const string RutaInforme = "Logs/t1630.txt";
        public const int MaximoEstacionados = 60;

        private const string RutaPrefabSemaforo = "Assets/Arte/Calle/Prefabs/ES01_Semaforo.prefab";
        private const string RutaMaterialLuz = "Assets/Arte/Calle/ES01_Semaforo/ES01_Luz.mat";
        private const string PlantillaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_{0}.prefab";
        private const string TipoSemaforoViejo = "SemaforoCiclo";
        private static readonly string[] Modelos =
        {
            "V01a", "V01b", "V02", "V03", "V04", "V09", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20", "V21", "V22", "V23", "V24"
        };

        private const int Semilla = 1630;
        // Semáforos.
        private const float SegundosVerde = 8f;
        private const float SegundosAmbar = 2f;
        private const float SegundosDespeje = 1.5f;
        private const float DesfaseEntreCruces = 3.7f;
        private const float DistanciaPoste = 8.5f; // del centro del cruce, hacia atrás y a la derecha (la esquina de la banqueta)
        private const float RadioAdopcion = 5f;
        private const float RadioViejosDelCruce = 16f;
        private const float SeparacionEntrePostes = 3f;
        private const float AlturaBanquetaMinima = 0.08f;
        private const float AlturaBanquetaMaxima = 0.5f;
        private static readonly float[] AtrasPoste = { 8.5f, 9.5f, 7.5f, 10.5f };
        private static readonly float[] LadoPoste = { 8.5f, 9.5f, 10.5f, 7.5f, 11.5f };
        private static readonly Vector3 SemiAlturaPoste = new Vector3(0.45f, 1f, 0.45f);
        private const float CentroAlturaPoste = 1.6f;
        // Lentes del ES01 respecto al pivote del prefab (las mismas de ColocarT910): rojo, ámbar, verde.
        private static readonly Vector3[] PosicionLentes = { new Vector3(-1f, 3.74f, -0.215f), new Vector3(-1f, 3.36f, -0.215f), new Vector3(-1f, 2.98f, -0.215f) };
        private const float DiametroLente = 0.2f;
        private const float GrosorLente = 0.01f;
        // Estacionados.
        private const float MargenAlCruce = 20f;
        private const float LargoMinimoDeTramo = 30f;
        private const float MaximaDiagonal = 0.1f;
        private const float AlturaRayo = 1f;
        private const float AlcanceRayo = 2.5f;
        private const float AlturaRayoCalle = 1.5f;
        private const float AlcanceRayoCalle = 4f;
        private const float DesdeCentroDeCalle = 3f;
        private const float HastaCentroDeCalle = 11f;
        private const float PasoDeBordillo = 0.25f;
        private const float AlturaDeBordillo = 0.1f;
        private const float HolguraAlBordillo = 0.25f;
        private const float BordeInternoMinimo = 3.5f; // el carril de las patrullas llega a ~3.5 m del eje: el carro estacionado no lo pisa
        private const float DiferenciaDeSueloMaxima = 0.12f;
        private const float HolguraLargo = 0.7f;
        private const float HolguraAncho = 0.1f;
        private const float AlturaDeRevision = 0.95f;
        private const float SemiAlturaDeRevision = 0.55f;
        private const float AlturaDeSuelo = 0.35f;
        private const float HolguraSobreElSuelo = 0.02f;
        private const float PasoDeBusqueda = 3f;
        private const float SaltoAlAzar = 8f;
        private const float ProbabilidadDeSaltar = 0.25f;
        private const float SeparacionMaximaExtra = 6f;
        private const float SeparacionMinima = 1f;
        private const float LimiteEscenaMegas = 95f;

        [MenuItem("Mecha Cyberpunk/T-1630/Aplicar solo patrullas")]
        public static void Aplicar()
        {
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("T-1630 Aplicar (" + RutaEscena + ")");
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            BorrarRaiz();
            GameObject raiz = new GameObject(NombreRaiz);
            Physics.SyncTransforms();

            TraficoCalle trafico = Object.FindFirstObjectByType<TraficoCalle>(FindObjectsInactive.Include);
            if (trafico == null)
            {
                informe.AppendLine("ERROR: la escena no tiene TraficoCalle.");
            }
            else
            {
                SerializedObject so = new SerializedObject(trafico);
                so.FindProperty("circulanCiviles").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(trafico);
                informe.AppendLine("TraficoCalle.circulanCiviles = false (el pool de " + trafico.Carros.Count + " carros queda para refuerzos y asaltos).");
            }

            GrafoCalles calles = ArmarCalles(trafico, informe);
            if (calles != null)
            {
                List<int> cruces = Cruces(calles);
                informe.AppendLine("Cruces de la red (3 o más calles): " + cruces.Count + " de " + calles.Cantidad + " nodos.");
                PonerSemaforos(calles, cruces, raiz.transform, informe);
                PonerEstacionados(calles, cruces, raiz.transform, informe);
            }

            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            long bytes = new FileInfo(RutaEscena).Length;
            informe.AppendLine("Escena guardada: " + (bytes / 1048576f).ToString("0.0") + " MB.");
            if (bytes > LimiteEscenaMegas * 1048576f)
            {
                informe.AppendLine("ERROR: la escena pasa de " + LimiteEscenaMegas + " MB (GitHub rechaza más de 100).");
                Debug.LogError("[Trafico] T-1630: la escena pesa " + (bytes / 1048576f).ToString("0.0") + " MB.");
            }
            informe.AppendLine();
            Estado(informe);
            Escribir(informe);
        }

        [MenuItem("Mecha Cyberpunk/T-1630/Diagnóstico")]
        public static void Diagnostico()
        {
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("T-1630 Diagnóstico (" + RutaEscena + ")");
            if (SceneManager.GetActiveScene().path != RutaEscena)
            {
                EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }
            Estado(informe);
            Escribir(informe);
        }

        // ------------------------------------------------------------------ estado de la escena (lo usan Aplicar y Diagnostico)

        private static void Estado(StringBuilder informe)
        {
            TraficoCalle trafico = Object.FindFirstObjectByType<TraficoCalle>(FindObjectsInactive.Include);
            informe.AppendLine("== Tráfico civil ==");
            if (trafico == null)
            {
                informe.AppendLine("Sin TraficoCalle en la escena.");
            }
            else
            {
                informe.AppendLine("TraficoCalle.CirculanCiviles = " + trafico.CirculanCiviles + ", pool de " + trafico.Carros.Count + " carros.");
            }
            GrafoCalles calles = ArmarCalles(trafico, informe);
            List<int> cruces = calles != null ? Cruces(calles) : new List<int>();
            informe.AppendLine("== Cruces y semáforos ==");
            SemaforoCruce[] semaforos = Object.FindObjectsByType<SemaforoCruce>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Behaviour[] viejos = SemaforosViejos();
            informe.AppendLine("Cruces en la red: " + cruces.Count + ". SemaforoCruce (T-1630) en la escena: " + semaforos.Length + ". SemaforoCiclo viejos: " + viejos.Length
                + " (apagados por adopción: " + viejos.Count(v => !v.enabled) + "). Postes ES01 (por nombre): "
                + Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(t => t.name.StartsWith("ES01_Semaforo")) + ".");
            foreach (int i in cruces)
            {
                Vector3 c = calles.Nodo(i);
                SemaforoCruce s = semaforos.FirstOrDefault(x => Plano(x.transform.position, c) < 2f);
                int controlan = s != null ? s.Postes.Count(p => p != null && p.Controla) : 0;
                int visuales = s != null ? s.Postes.Count(p => p != null && !p.Controla) : 0;
                informe.AppendLine("Cruce (" + c.x.ToString("0.0") + ", " + c.z.ToString("0.0") + ") con " + calles.Vecinos(i).Count + " calles: "
                    + (s != null ? "semáforo con " + controlan + " postes que mandan y " + visuales + " viejos adoptados, desfase " + s.Desfase.ToString("0.0") + " s" : "SIN semáforo")
                    + "; SemaforoCiclo viejos a menos de " + RadioViejosDelCruce + " m: " + viejos.Count(v => Plano(v.transform.position, c) <= RadioViejosDelCruce) + ".");
            }
            foreach (Behaviour v in viejos)
            {
                float cerca = cruces.Count > 0 ? cruces.Min(i => Plano(calles.Nodo(i), v.transform.position)) : -1f;
                informe.AppendLine("SemaforoCiclo viejo en " + v.transform.position.ToString("F1") + " (" + (v.enabled ? "andando" : "apagado, lo pinta T-1630") + "), cruce de la red más cercano a " + cerca.ToString("0.0") + " m.");
            }
            informe.AppendLine("== Estacionados ==");
            GameObject raiz = GameObject.Find(NombreRaiz);
            Transform grupo = raiz != null ? raiz.transform.Find("Estacionados") : null;
            int cuenta = grupo != null ? grupo.childCount : 0;
            informe.AppendLine("Carros estacionados nuevos (" + NombreRaiz + "/Estacionados): " + cuenta + ".");
            if (grupo != null)
            {
                foreach (Transform t in grupo)
                {
                    informe.AppendLine("  " + t.name + " en (" + t.position.x.ToString("0.0") + ", " + t.position.y.ToString("0.00") + ", " + t.position.z.ToString("0.0") + ") giro " + t.eulerAngles.y.ToString("0"));
                }
            }
        }

        private static void Escribir(StringBuilder informe)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Trafico] T-1630:\n" + informe);
        }

        // ------------------------------------------------------------------ red de calles

        private static void BorrarRaiz()
        {
            foreach (GameObject objeto in SceneManager.GetActiveScene().GetRootGameObjects().Where(o => o.name == NombreRaiz).ToArray())
            {
                Object.DestroyImmediate(objeto);
            }
        }

        // Las mismas calles que usa TraficoCalle (las manzanas del barrio), recortadas al muro. Null si no se encuentran.
        private static GrafoCalles ArmarCalles(TraficoCalle trafico, StringBuilder informe)
        {
            Transform barrio = null;
            Rect area = new Rect();
            if (trafico != null)
            {
                SerializedObject so = new SerializedObject(trafico);
                barrio = so.FindProperty("barrio").objectReferenceValue as Transform;
                area = so.FindProperty("areaMuro").rectValue;
            }
            if (barrio == null)
            {
                GestorPersecucion gestor = Object.FindFirstObjectByType<GestorPersecucion>(FindObjectsInactive.Include);
                barrio = gestor != null ? gestor.transform.parent : null;
            }
            GrafoCalles calles = barrio != null ? GestorPersecucion.ArmarCallesDe(barrio) : null;
            if (calles == null || calles.Cantidad < 2)
            {
                informe.AppendLine("ERROR: no encontré las manzanas del barrio; no hay red de calles.");
                return null;
            }
            AreaMuro.Definir(area);
            AreaMuro.Recortar(calles, AreaMuro.MargenPorOmision);
            return calles;
        }

        private static List<int> Cruces(GrafoCalles calles)
        {
            List<int> cruces = new List<int>();
            for (int i = 0; i < calles.Cantidad; i++)
            {
                if (calles.Vecinos(i).Count >= 3 && calles.Dentro(i) && !calles.EsDeZona(i))
                {
                    cruces.Add(i);
                }
            }
            return cruces;
        }

        // ------------------------------------------------------------------ semáforos

        private static Behaviour[] SemaforosViejos()
        {
            GameObject raiz = GameObject.Find(NombreRaiz);
            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != null && m.GetType().Name == TipoSemaforoViejo && (raiz == null || !m.transform.IsChildOf(raiz.transform)))
                .Cast<Behaviour>().ToArray();
        }

        private static void PonerSemaforos(GrafoCalles calles, List<int> cruces, Transform raiz, StringBuilder informe)
        {
            Transform grupo = Hijo(raiz, "Semaforos");
            List<Behaviour> viejos = SemaforosViejos().ToList();
            HashSet<Behaviour> adoptados = new HashSet<Behaviour>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSemaforo);
            Material luz = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialLuz);
            if (prefab == null)
            {
                informe.AppendLine("FALTA " + RutaPrefabSemaforo + ": sin poste nuevo, los cruces sin poste viejo quedan con la línea de parada sola (sin modelo).");
            }
            List<Vector3> ocupados = viejos.Select(v => v.transform.position).ToList();
            int nuevos = 0;
            int adoptadosControl = 0;
            int soloControl = 0;
            int k = 0;
            foreach (int i in cruces)
            {
                Vector3 c = calles.Nodo(i);
                GameObject cruceObjeto = new GameObject("Cruce_" + c.x.ToString("0") + "_" + c.z.ToString("0"));
                cruceObjeto.transform.SetParent(grupo, false);
                cruceObjeto.transform.position = c;
                SemaforoCruce cruce = cruceObjeto.AddComponent<SemaforoCruce>();
                cruce.Configurar(SegundosVerde, SegundosAmbar, SegundosDespeje, k * DesfaseEntreCruces);
                k++;
                float alturaCalle = c.y;
                if (Physics.Raycast(new Vector3(c.x, c.y + AlturaRayoCalle, c.z), Vector3.down, out RaycastHit calle, AlcanceRayoCalle, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    alturaCalle = calle.point.y;
                }
                List<(Vector3 sentido, Vector3 ideal)> llegadas = new List<(Vector3, Vector3)>();
                foreach (int vecino in calles.Vecinos(i))
                {
                    Vector3 sentido = SentidoDeLlegada(c, calles.Nodo(vecino));
                    if (sentido == Vector3.zero || llegadas.Any(l => Vector3.Dot(l.sentido, sentido) > 0.9f))
                    {
                        continue;
                    }
                    llegadas.Add((sentido, c - sentido * DistanciaPoste + Vector3.Cross(Vector3.up, sentido) * DistanciaPoste));
                }
                StringBuilder linea = new StringBuilder();
                foreach (var llegada in llegadas)
                {
                    int eje = Mathf.Abs(llegada.sentido.x) > Mathf.Abs(llegada.sentido.z) ? 0 : 1;
                    Behaviour viejo = viejos.Where(v => !adoptados.Contains(v) && Plano(v.transform.position, llegada.ideal) <= RadioAdopcion).OrderBy(v => Plano(v.transform.position, llegada.ideal)).FirstOrDefault();
                    if (viejo != null)
                    {
                        adoptados.Add(viejo);
                        cruce.AgregarPoste(PosteViejo(viejo, eje, llegada.sentido, true));
                        adoptadosControl++;
                        linea.Append(" [" + Eje(eje) + " viejo]");
                        continue;
                    }
                    if (prefab != null && luz != null && BuscarLugarDePoste(c, llegada.sentido, alturaCalle, ocupados, out Vector3 pie))
                    {
                        cruce.AgregarPoste(PosteNuevo(prefab, luz, cruceObjeto.transform, pie, llegada.sentido, eje, "Poste_ES01_" + Eje(eje) + "_" + cruce.Postes.Count));
                        ocupados.Add(pie);
                        nuevos++;
                        linea.Append(" [" + Eje(eje) + " nuevo]");
                        continue;
                    }
                    GameObject control = new GameObject("Control_" + Eje(eje) + "_" + cruce.Postes.Count);
                    control.transform.SetParent(cruceObjeto.transform, false);
                    control.transform.position = new Vector3(llegada.ideal.x, alturaCalle, llegada.ideal.z);
                    cruce.AgregarPoste(new SemaforoCruce.Poste(control.transform, eje, llegada.sentido, true, null, null));
                    soloControl++;
                    linea.Append(" [" + Eje(eje) + " solo línea de parada: sin lugar libre en la banqueta]");
                }
                // Los SemaforoCiclo viejos que quedaron cerca del cruce sin poste nuevo propio se pintan con el reloj del cruce (no mandan a nadie).
                foreach (Behaviour viejo in viejos.Where(v => !adoptados.Contains(v) && Plano(v.transform.position, c) <= RadioViejosDelCruce).ToArray())
                {
                    if (llegadas.Count == 0)
                    {
                        break;
                    }
                    var masCercana = llegadas.OrderBy(l => Plano(viejo.transform.position, l.ideal)).First();
                    int eje = Mathf.Abs(masCercana.sentido.x) > Mathf.Abs(masCercana.sentido.z) ? 0 : 1;
                    adoptados.Add(viejo);
                    cruce.AgregarPoste(PosteViejo(viejo, eje, masCercana.sentido, false));
                    linea.Append(" [" + Eje(eje) + " viejo solo luz]");
                }
                EditorUtility.SetDirty(cruce);
                informe.AppendLine("Cruce (" + c.x.ToString("0.0") + ", " + c.z.ToString("0.0") + ") " + calles.Vecinos(i).Count + " calles:" + linea);
            }
            informe.AppendLine("Semáforos: " + cruces.Count + " cruces, " + nuevos + " postes ES01 nuevos, " + adoptadosControl + " viejos adoptados como poste, " + soloControl + " solo con línea de parada. Postes ocupados considerados: " + ocupados.Count + ".");
        }

        private static string Eje(int eje) => eje == 0 ? "X" : "Z";

        // Hacia dónde va un carro que llega al cruce desde el vecino (por el eje de la calle); cero si la calle va en diagonal.
        private static Vector3 SentidoDeLlegada(Vector3 cruce, Vector3 vecino)
        {
            Vector3 v = vecino - cruce;
            v.y = 0f;
            float mayor = Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.z));
            if (mayor < 1f || Mathf.Min(Mathf.Abs(v.x), Mathf.Abs(v.z)) / mayor > MaximaDiagonal * 3f)
            {
                return Vector3.zero;
            }
            return Mathf.Abs(v.x) > Mathf.Abs(v.z) ? new Vector3(-Mathf.Sign(v.x), 0f, 0f) : new Vector3(0f, 0f, -Mathf.Sign(v.z));
        }

        private static SemaforoCruce.Poste PosteViejo(Behaviour viejo, int eje, Vector3 sentido, bool controla)
        {
            SerializedObject so = new SerializedObject(viejo);
            SerializedProperty arreglo = so.FindProperty("lentes");
            Renderer[] lentes = new Renderer[arreglo.arraySize];
            for (int j = 0; j < lentes.Length; j++)
            {
                lentes[j] = arreglo.GetArrayElementAtIndex(j).objectReferenceValue as Renderer;
            }
            Light luz = so.FindProperty("luz").objectReferenceValue as Light;
            viejo.enabled = false; // el reloj del cruce lo pinta: así todos los postes del cruce coinciden con lo que obedecen las patrullas
            EditorUtility.SetDirty(viejo);
            return new SemaforoCruce.Poste(viejo.transform, eje, sentido, controla, lentes, luz);
        }

        // Un lugar libre de la banqueta, a la derecha de la llegada y cerca de la esquina del cruce.
        private static bool BuscarLugarDePoste(Vector3 cruce, Vector3 sentido, float alturaCalle, List<Vector3> ocupados, out Vector3 pie)
        {
            Vector3 derecha = Vector3.Cross(Vector3.up, sentido);
            foreach (float atras in AtrasPoste)
            {
                foreach (float lado in LadoPoste)
                {
                    Vector3 p = cruce - sentido * atras + derecha * lado;
                    if (!Physics.Raycast(new Vector3(p.x, alturaCalle + AlturaRayoCalle, p.z), Vector3.down, out RaycastHit suelo, AlcanceRayoCalle, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    float sobre = suelo.point.y - alturaCalle;
                    if (sobre < AlturaBanquetaMinima || sobre > AlturaBanquetaMaxima || suelo.normal.y < 0.9f)
                    {
                        continue;
                    }
                    Vector3 candidato = new Vector3(p.x, suelo.point.y, p.z);
                    if (ocupados.Any(o => Plano(o, candidato) < SeparacionEntrePostes))
                    {
                        continue;
                    }
                    if (Physics.CheckBox(candidato + Vector3.up * CentroAlturaPoste, SemiAlturaPoste, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    pie = candidato;
                    return true;
                }
            }
            pie = Vector3.zero;
            return false;
        }

        // El ES01 con la cara hacia los carros que llegan (hacia -sentido) y tres discos sobre sus lentes, como ColocarT910.Semaforo. El modelo trae las lentes en su cara -Z.
        private static SemaforoCruce.Poste PosteNuevo(GameObject prefab, Material luz, Transform padre, Vector3 pie, Vector3 sentido, int eje, string nombre)
        {
            GameObject poste = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            poste.name = nombre;
            poste.transform.SetPositionAndRotation(pie, Quaternion.LookRotation(sentido));
            Renderer[] discos = new Renderer[PosicionLentes.Length];
            string[] colores = { "Roja", "Ambar", "Verde" };
            for (int i = 0; i < discos.Length; i++)
            {
                GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(d.GetComponent<Collider>());
                d.name = "Lente_" + colores[i];
                d.transform.SetParent(poste.transform, false);
                d.transform.localPosition = PosicionLentes[i];
                d.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                d.transform.localScale = new Vector3(DiametroLente, GrosorLente, DiametroLente);
                MeshRenderer r = d.GetComponent<MeshRenderer>();
                r.sharedMaterial = luz;
                r.shadowCastingMode = ShadowCastingMode.Off;
                discos[i] = r;
            }
            return new SemaforoCruce.Poste(poste.transform, eje, sentido, true, discos, null);
        }

        // ------------------------------------------------------------------ carros estacionados

        private static void PonerEstacionados(GrafoCalles calles, List<int> cruces, Transform raiz, StringBuilder informe)
        {
            Transform grupo = Hijo(raiz, "Estacionados");
            List<GameObject> prefabs = new List<GameObject>();
            List<string> codigos = new List<string>();
            foreach (string codigo in Modelos)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(PlantillaCarro, codigo));
                if (p == null)
                {
                    informe.AppendLine("FALTA " + string.Format(PlantillaCarro, codigo));
                    continue;
                }
                prefabs.Add(p);
                codigos.Add(codigo);
            }
            if (prefabs.Count == 0)
            {
                informe.AppendLine("ERROR: sin prefabs de carro; no hay estacionados.");
                return;
            }
            System.Random azar = new System.Random(Semilla);
            List<Vector3> centrosDeCruce = cruces.Select(calles.Nodo).ToList();
            Dictionary<string, int> rechazos = new Dictionary<string, int>();
            int puestos = 0;
            int tramos = 0;
            int turno = 0;
            for (int i = 0; i < calles.Cantidad && puestos < MaximoEstacionados; i++)
            {
                foreach (int j in calles.Vecinos(i))
                {
                    if (j <= i || !calles.Dentro(i) || !calles.Dentro(j) || calles.EsDeZona(i) || calles.EsDeZona(j))
                    {
                        continue;
                    }
                    Vector3 a = calles.Nodo(i);
                    Vector3 d = calles.Nodo(j) - a;
                    d.y = 0f;
                    float largo = d.magnitude;
                    if (largo < LargoMinimoDeTramo)
                    {
                        continue;
                    }
                    d /= largo;
                    if (Mathf.Min(Mathf.Abs(d.x), Mathf.Abs(d.z)) > MaximaDiagonal)
                    {
                        Contar(rechazos, "tramo en diagonal");
                        continue;
                    }
                    d = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(d.z));
                    tramos++;
                    foreach (int lado in new[] { 1, -1 })
                    {
                        float s = 0f;
                        while (s < largo && puestos < MaximoEstacionados)
                        {
                            Vector3 centro = a + d * s;
                            if (centrosDeCruce.Any(c => Plano(c, centro) < MargenAlCruce))
                            {
                                Contar(rechazos, "junto a un cruce");
                                s += PasoDeBusqueda;
                                continue;
                            }
                            if (azar.NextDouble() < ProbabilidadDeSaltar)
                            {
                                s += SaltoAlAzar;
                                continue;
                            }
                            float usado = 0f;
                            string motivo = "";
                            for (int intento = 0; intento < 2 && usado <= 0f; intento++)
                            {
                                int modelo = (turno + intento) % prefabs.Count;
                                usado = Estacionar(prefabs[modelo], codigos[modelo], grupo, centro, d, lado, a.y, puestos + 1, out motivo);
                                if (usado <= 0f)
                                {
                                    Contar(rechazos, motivo);
                                }
                            }
                            if (usado > 0f)
                            {
                                puestos++;
                                turno++;
                                s += usado + SeparacionMinima + (float)azar.NextDouble() * SeparacionMaximaExtra;
                            }
                            else
                            {
                                s += PasoDeBusqueda;
                            }
                        }
                    }
                }
            }
            informe.AppendLine("Estacionados: " + puestos + " carros en " + tramos + " tramos (máximo " + MaximoEstacionados + "). Rechazos: "
                + (rechazos.Count == 0 ? "ninguno" : string.Join(", ", rechazos.Select(r => r.Key + " x" + r.Value))) + ".");
        }

        // Pone un carro pegado al bordillo del lado indicado; devuelve lo que ocupa a lo largo de la calle, o 0 si no cabe (y lo quita).
        private static float Estacionar(GameObject prefab, string codigo, Transform grupo, Vector3 centro, Vector3 eje, int lado, float alturaNodo, int numero, out string motivo)
        {
            motivo = "";
            if (!Physics.Raycast(new Vector3(centro.x, alturaNodo + AlturaRayoCalle, centro.z), Vector3.down, out RaycastHit calle, AlcanceRayoCalle, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                motivo = "sin calle debajo";
                return 0f;
            }
            float alturaCalle = calle.point.y;
            Vector3 haciaLaBanqueta = Vector3.Cross(Vector3.up, eje) * lado;
            float bordillo = -1f;
            for (float l = DesdeCentroDeCalle; l <= HastaCentroDeCalle; l += PasoDeBordillo)
            {
                Vector3 q = centro + haciaLaBanqueta * l;
                if (!Physics.Raycast(new Vector3(q.x, alturaCalle + AlturaRayo, q.z), Vector3.down, out RaycastHit suelo, AlcanceRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    || suelo.point.y > alturaCalle + AlturaDeBordillo || suelo.normal.y < 0.9f)
                {
                    bordillo = l;
                    break;
                }
            }
            if (bordillo < 0f)
            {
                motivo = "sin banqueta a la vista";
                return 0f;
            }
            GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
            carro.name = PrefijoEstacionado + numero + "_" + codigo;
            carro.transform.SetPositionAndRotation(centro, Quaternion.LookRotation(eje * lado));
            Physics.SyncTransforms();
            bool alLargoDeX = Mathf.Abs(eje.x) > 0.5f;
            Bounds b = Medidas(carro);
            float mitadAncho = alLargoDeX ? b.extents.z : b.extents.x;
            float mitadLargo = alLargoDeX ? b.extents.x : b.extents.z;
            if (b.size == Vector3.zero || bordillo - HolguraAlBordillo - 2f * mitadAncho < BordeInternoMinimo)
            {
                Object.DestroyImmediate(carro);
                motivo = "calle angosta para el carro";
                return 0f;
            }
            Vector3 destino = centro + haciaLaBanqueta * (bordillo - HolguraAlBordillo - mitadAncho);
            carro.transform.position += new Vector3(destino.x - b.center.x, 0f, destino.z - b.center.z);
            Physics.SyncTransforms();
            b = Medidas(carro);
            if (!Physics.Raycast(new Vector3(b.center.x, alturaCalle + AlturaRayo, b.center.z), Vector3.down, out RaycastHit bajoElCarro, AlcanceRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Object.DestroyImmediate(carro);
                motivo = "sin piso bajo el carro";
                return 0f;
            }
            float suelo2 = bajoElCarro.point.y;
            foreach (float cabecera in new[] { mitadLargo, -mitadLargo })
            {
                Vector3 punta = b.center + eje * cabecera;
                if (!Physics.Raycast(new Vector3(punta.x, alturaCalle + AlturaRayo, punta.z), Vector3.down, out RaycastHit hit, AlcanceRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(hit.point.y - suelo2) > DiferenciaDeSueloMaxima)
                {
                    Object.DestroyImmediate(carro);
                    motivo = "piso desnivelado";
                    return 0f;
                }
            }
            carro.transform.position += Vector3.up * (suelo2 + HolguraSobreElSuelo - b.min.y);
            Physics.SyncTransforms();
            b = Medidas(carro);
            Vector3 semi = new Vector3(b.extents.x + (alLargoDeX ? HolguraLargo : HolguraAncho), SemiAlturaDeRevision, b.extents.z + (alLargoDeX ? HolguraAncho : HolguraLargo));
            foreach (Collider otro in Physics.OverlapBox(new Vector3(b.center.x, suelo2 + AlturaDeRevision, b.center.z), semi, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (otro.transform.IsChildOf(carro.transform) || otro.bounds.max.y <= suelo2 + AlturaDeSuelo)
                {
                    continue;
                }
                Object.DestroyImmediate(carro);
                motivo = "choca con " + otro.name;
                return 0f;
            }
            return 2f * mitadLargo;
        }

        private static Bounds Medidas(GameObject objeto)
        {
            Renderer[] rs = objeto.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return new Bounds();
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static void Contar(Dictionary<string, int> cuentas, string clave)
        {
            // Los rechazos "choca con X" se agrupan por nombre de objeto para que el informe no crezca sin fin.
            cuentas[clave] = cuentas.TryGetValue(clave, out int n) ? n + 1 : 1;
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(padre, false);
            return g.transform;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
