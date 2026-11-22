using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1606: coloca en Ciudad_Prologo lo que pidio la sesion B (docs/para-sesion-A.md). Los tipos de Personaje, IA y Progresion no se
    /// referencian: los prefabs se instancian por ruta y sus campos se escriben con SerializedObject.
    /// Orden en batchmode (Unity cerrado), cada uno en su corrida:
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.ColocarSesionBT1606.Diagnostico  (no guarda, escribe Logs/t1606-colocar.txt)
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.ColocarSesionBT1606.Aplicar      (guarda la escena, escribe Logs/t1606-colocar.txt)
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.ColocarSesionBT1606.Fotos        (corrida aparte: docs/capturas/_tareas/para-sesion-A/)
    /// </summary>
    public static class ColocarSesionBT1606
    {
        public const string RutaLog = "Logs/t1606-colocar.txt";
        public const string CarpetaFotos = "docs/capturas/_tareas/para-sesion-A";
        public const string NombreRaiz = "T1606_SesionB";
        public const string PrefijoReemplazo = "Reemplaza:";
        public const string Etiqueta = "[T-1606] ";
        public const string RutaBanca = "Assets/_Proyecto/Personaje/Interaccion/Prefabs/Banca_Sentable.prefab";
        public const string RutaMolestia = "Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_Molestia.prefab";
        public const string RutaRanura = "Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_Ranura.prefab";
        public const string RutaTorre = "Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_Torre.prefab";
        public const string RutaMarcador = "Assets/_Proyecto/IA/Prefabs/MarcadorEscenaBarrio.prefab";
        public const string IdCamilla = "Clinica_Camilla";
        public const string IdInicioCalle = "Inicio_Calle";
        public const int MaxBancas = 20;

        private static readonly string[] IdsEscena =
        {
            "bully_piso_tianguis", "vendedor_tacos", "vendedor_elotes", "bully_snakes_senora", "bully_comerciante_cortina",
            "borracho", "celular", "pareja_discutiendo", "vecinos_muro", "vecinos_nova",
        };

        [MenuItem("Mecha Cyberpunk/T-1606 Colocar sesion B: diagnostico")]
        public static void Diagnostico()
        {
            Abrir();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1606 DIAGNOSTICO de " + CreadorPrologoCiudad.RutaEscena);
            List<Transform> bancas = BancasViejas();
            sb.AppendLine("Bancas PZ01 activas (candidatas a reemplazar): " + bancas.Count);
            foreach (Transform b in bancas)
            {
                sb.AppendLine("  " + Ruta(b) + " en " + V(b.position) + " yaw " + b.eulerAngles.y.ToString("0"));
            }
            sb.AppendLine("Banca_Sentable ya en la escena: " + Contar("Banca_Sentable"));
            ListarNombres(sb, "Clinica / Doc / Camilla / Cama", "Clinica", "Doc", "Camilla", "Cama");
            PuntoAparicion camilla = BuscarPunto(IdCamilla);
            sb.AppendLine("Punto " + IdCamilla + ": " + (camilla != null ? "existe en " + V(camilla.transform.position) : "NO existe (Aplicar lo crea sobre la camilla)"));
            PuntoAparicion inicio = BuscarPunto(IdInicioCalle);
            sb.AppendLine("Punto " + IdInicioCalle + ": " + (inicio != null ? V(inicio.transform.position) + " mira " + V(inicio.transform.forward) : "NO existe"));
            MuroPerimetroT1601.Resultado muro = MuroPerimetroT1601.Analizar();
            sb.AppendLine("Piezas del muro: " + muro.Piezas.Count);
            if (inicio != null)
            {
                foreach (MuroPerimetroT1601.Pieza p in muro.Piezas.Where(x => x.Tipo == MuroPerimetroT1601.Tipo.Tramo)
                    .OrderBy(x => Plano(x.T.position, inicio.transform.position)).Take(4))
                {
                    sb.AppendLine("  tramo " + p.T.name + " lado " + muro.Lados[p.Lado].Nombre + " en " + V(p.T.position) + " a " + Plano(p.T.position, inicio.transform.position).ToString("0.0") + " m de la casa");
                }
            }
            ListarNombres(sb, "Torre / Nova / Recinto", "RecintoJefe", "Recinto_JefeFinal", "Torre_Nova", "Frente_Nova");
            ListarNombres(sb, "Tianguis / Puesto", "Tianguis", "Puesto");
            ListarNombres(sb, "Neon Alto", "NeonAlto", "Neon");
            ListarNombres(sb, "Cortina / Persiana", "Cortina", "Persiana");
            Escribir(sb.ToString());
            Debug.Log(Etiqueta + "diagnostico escrito en " + RutaLog);
        }

        [MenuItem("Mecha Cyberpunk/T-1606 Colocar sesion B: aplicar")]
        public static void Aplicar()
        {
            Scene escena = Abrir();
            StringBuilder log = new StringBuilder();
            log.AppendLine("T-1606 APLICAR");

            // Idempotencia: devolver las bancas viejas y borrar la raiz anterior.
            DeshacerReemplazos(escena, log);
            GameObject viejo = GameObject.Find(NombreRaiz);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo);
            }
            Physics.SyncTransforms();
            GameObject raiz = new GameObject(NombreRaiz);
            SceneManager.MoveGameObjectToScene(raiz, escena);

            // 1. Bancas.
            GameObject prefabBanca = Cargar(RutaBanca, log);
            if (prefabBanca != null)
            {
                int n = 0;
                foreach (Transform vieja in BancasViejas().Take(MaxBancas))
                {
                    Vector3 pos = vieja.position;
                    Quaternion rot = vieja.rotation;
                    string registro = PrefijoReemplazo + IndiceRuta(vieja) + "|" + vieja.name;
                    vieja.gameObject.SetActive(false);
                    GameObject reg = new GameObject(registro);
                    reg.transform.SetParent(raiz.transform, false);
                    GameObject nueva = (GameObject)PrefabUtility.InstantiatePrefab(prefabBanca, raiz.transform);
                    nueva.transform.SetPositionAndRotation(pos, rot);
                    log.AppendLine("Banca " + n + ": reemplaza " + registro + " en " + V(pos));
                    n++;
                }
                log.AppendLine("Bancas sentables colocadas: " + n + " (NPC sentado en plazas: saltado, requiere un visual de NPC civil con NpcSentadoBanca; npcInicial queda vacio)");
            }

            // 2. Camilla del Doc.
            PuntoAparicion existente = BuscarPunto(IdCamilla);
            if (existente != null)
            {
                log.AppendLine(IdCamilla + ": ya existe en " + V(existente.transform.position) + ", no se duplica.");
            }
            else
            {
                Transform camilla = BuscarPorNombre("Camilla") ?? BuscarPorNombre("Cama");
                if (camilla == null)
                {
                    log.AppendLine(IdCamilla + ": NO se encontro la camilla de la clinica (nombres Camilla/Cama).");
                }
                else
                {
                    Bounds b = CajaDe(camilla);
                    GameObject go = new GameObject(IdCamilla);
                    go.transform.SetParent(raiz.transform, false);
                    go.transform.SetPositionAndRotation(new Vector3(b.center.x, b.max.y, b.center.z), Quaternion.Euler(0f, camilla.eulerAngles.y, 0f));
                    PuntoAparicion pa = go.AddComponent<PuntoAparicion>();
                    SerializedObject so = new SerializedObject(pa);
                    so.FindProperty("id").stringValue = IdCamilla;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    log.AppendLine(IdCamilla + ": creado sobre " + Ruta(camilla) + " en " + V(go.transform.position));
                }
            }

            // 3. Historia.
            PuntoAparicion inicio = BuscarPunto(IdInicioCalle);
            if (inicio == null)
            {
                log.AppendLine("Historia_Molestia y Historia_Ranura: NO hay Inicio_Calle, saltados.");
            }
            else
            {
                Vector3 adelante = Vector3.ProjectOnPlane(inicio.transform.forward, Vector3.up).normalized;
                if (adelante.sqrMagnitude < 0.5f)
                {
                    adelante = Vector3.right;
                }
                GameObject molestia = Cargar(RutaMolestia, log);
                if (molestia != null)
                {
                    Vector3 pos;
                    // La calle de Suburbios corre norte-sur (asfalto x -51 a -42): Inicio_Calle mira +X hacia la casa de enfrente,
                    // asi que "20 m por donde sale" es 20 m a lo largo de la calle (+Z), en el centro del asfalto.
                    Vector3 sobreCalle = new Vector3(-46.5f, inicio.transform.position.y, inicio.transform.position.z + 20f);
                    if (BuscarLibre(sobreCalle, inicio.transform.position.y, raiz.transform, out pos))
                    {
                        Poner(molestia, raiz.transform, pos, Quaternion.LookRotation(Vector3.forward, Vector3.up), log, "Historia_Molestia");
                    }
                    else
                    {
                        log.AppendLine("Historia_Molestia: NO hubo punto libre a ~20 m de Inicio_Calle.");
                    }
                }

                GameObject ranura = Cargar(RutaRanura, log);
                if (ranura != null)
                {
                    MuroPerimetroT1601.Resultado muro = MuroPerimetroT1601.Analizar();
                    MuroPerimetroT1601.Pieza mejor = null;
                    float dm = float.MaxValue;
                    foreach (MuroPerimetroT1601.Pieza p in muro.Piezas.Where(x => x.Tipo == MuroPerimetroT1601.Tipo.Tramo))
                    {
                        float d = Plano(p.T.position, inicio.transform.position);
                        if (d < dm)
                        {
                            dm = d;
                            mejor = p;
                        }
                    }
                    if (mejor == null)
                    {
                        log.AppendLine("Historia_Ranura: NO hay tramos de muro.");
                    }
                    else
                    {
                        Vector3 fuera = muro.Lados[mejor.Lado].Fuera;
                        fuera.y = 0f;
                        fuera.Normalize();
                        Bounds cb = CajaDe(mejor.T);
                        Vector3 pos = new Vector3(cb.center.x, mejor.BaseY, cb.center.z) - fuera * (mejor.Grosor * 0.5f);
                        Poner(ranura, raiz.transform, pos, Quaternion.LookRotation(fuera, Vector3.up), log,
                            "Historia_Ranura (tramo " + mejor.T.name + ", lado " + muro.Lados[mejor.Lado].Nombre + ", a " + dm.ToString("0.0") + " m de la casa, +Z hacia afuera)");
                    }
                }
            }

            Transform recinto = BuscarPorNombre("Recinto_JefeFinal") ?? BuscarPorNombre("RecintoJefe");
            GameObject torre = Cargar(RutaTorre, log);
            if (torre != null)
            {
                if (recinto == null)
                {
                    log.AppendLine("Historia_Torre: NO se encontro Recinto_JefeFinal/RecintoJefe; el director usa su respaldo.");
                }
                else
                {
                    Vector3 pos = recinto.position;
                    Vector3 piso;
                    if (Piso(pos, pos.y, raiz.transform, 3f, out piso))
                    {
                        pos = piso;
                    }
                    Vector3 haciaCiudad = Vector3.ProjectOnPlane(-pos, Vector3.up);
                    Quaternion rot = haciaCiudad.sqrMagnitude > 1f ? Quaternion.LookRotation(haciaCiudad.normalized, Vector3.up) : recinto.rotation;
                    Poner(torre, raiz.transform, pos, rot, log, "Historia_Torre (sobre " + Ruta(recinto) + ")");
                }
            }

            // 4. Marcadores de escenas del barrio.
            GameObject prefabMarcador = Cargar(RutaMarcador, log);
            if (prefabMarcador != null)
            {
                Vector3 tianguis = CentroDe("Calle_tianguis", CentroDe("Tianguis", Vector3.zero));
                log.AppendLine("Ref tianguis " + V(tianguis));
                Vector3 neon = CentroDe("Calle_neon", new Vector3(90f, 0f, 40f));
                log.AppendLine("Ref neon " + V(neon));
                Transform cortina = BuscarPorNombre("Cortina") ?? BuscarPorNombre("Persiana");
                Vector3 cortinaPos = cortina != null ? CajaDe(cortina).center : tianguis + new Vector3(0f, 0f, 14f);
                Vector3 inicioPos = inicio != null ? inicio.transform.position : new Vector3(-53f, 0f, -76.5f);
                Vector3 hacia = inicio != null ? Vector3.ProjectOnPlane(inicio.transform.forward, Vector3.up).normalized : Vector3.right;
                Vector3 recintoPos = recinto != null ? recinto.position : new Vector3(90f, 0f, -45f);
                Vector3 haciaCentro = Vector3.ProjectOnPlane(-recintoPos, Vector3.up).normalized;
                Dictionary<string, Vector3> deseado = new Dictionary<string, Vector3>
                {
                    // El tianguis es una calle E-O de 18 m (banquetas en z = +-9): los puntos van sobre las banquetas, junto a los puestos.
                    { "bully_piso_tianguis", tianguis + new Vector3(0f, 0f, 8.5f) },
                    { "vendedor_tacos", tianguis + new Vector3(10f, 0f, -8.5f) },
                    { "vendedor_elotes", tianguis + new Vector3(-10f, 0f, 8.5f) },
                    // Neon Alto: calle E-O con banquetas en z = +-9 alrededor de la referencia.
                    { "bully_snakes_senora", neon + new Vector3(0f, 0f, 3.5f) },
                    { "bully_comerciante_cortina", cortinaPos },
                    { "borracho", tianguis + new Vector3(-22f, 0f, -8.5f) },
                    { "celular", tianguis + new Vector3(18f, 0f, 8.5f) },
                    // Calle de Suburbios (N-S, banqueta oeste en x -53.5): la casa de la izquierda tiene interior en x < -57.
                    { "pareja_discutiendo", new Vector3(inicioPos.x + 0.5f, 0f, inicioPos.z + 14f) },
                    { "vecinos_muro", new Vector3(inicioPos.x + 0.5f, 0f, inicioPos.z - 4f) },
                    { "vecinos_nova", recintoPos + haciaCentro * 20f },
                };
                foreach (string id in IdsEscena)
                {
                    Vector3 pos;
                    if (!BuscarLibre(deseado[id], 0f, raiz.transform, out pos))
                    {
                        log.AppendLine("Marcador " + id + ": NO hubo punto libre cerca de " + V(deseado[id]));
                        continue;
                    }
                    GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(prefabMarcador, raiz.transform);
                    m.name = "MarcadorEscenaBarrio_" + id;
                    m.transform.position = pos;
                    foreach (MonoBehaviour mb in m.GetComponents<MonoBehaviour>())
                    {
                        if (mb != null && mb.GetType().Name == "MarcadorEscenaBarrio")
                        {
                            SerializedObject so = new SerializedObject(mb);
                            so.FindProperty("idEscena").stringValue = id;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                    log.AppendLine("Marcador " + id + ": en " + V(pos) + " (deseado " + V(deseado[id]) + ")");
                }
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Escribir(log.ToString());
            Debug.Log(Etiqueta + "aplicado; ver " + RutaLog);
        }

        [MenuItem("Mecha Cyberpunk/T-1606 Colocar sesion B: fotos")]
        public static void Fotos()
        {
            Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            GameObject go = GameObject.Find(NombreRaiz);
            Transform raiz = go != null ? go.transform : null;
            Transform banca = Hijo(raiz, "Banca_Sentable");
            if (banca != null)
            {
                Foto("banca", banca.position - banca.forward * 4f + Vector3.up * 1.8f, banca.position + Vector3.up * 0.5f);
            }
            PuntoAparicion camilla = BuscarPunto(IdCamilla);
            if (camilla != null)
            {
                Vector3 p = camilla.transform.position;
                Foto("camilla", p + new Vector3(3f, 2.5f, 3f), p);
            }
            Transform ranura = Hijo(raiz, "Historia_Ranura");
            if (ranura != null)
            {
                Foto("ranura", ranura.position - ranura.forward * 4f + Vector3.up * 1.8f, ranura.position + Vector3.up * 1.2f);
            }
            Transform molestia = Hijo(raiz, "Historia_Molestia");
            if (molestia != null)
            {
                Foto("molestia", molestia.position - molestia.forward * 4f + Vector3.up * 1.8f, molestia.position + Vector3.up * 1f);
            }
            Transform torre = Hijo(raiz, "Historia_Torre");
            if (torre != null)
            {
                Foto("torre", torre.position - torre.forward * 12f + Vector3.up * 6f, torre.position + Vector3.up * 8f);
            }
        }

        // ------------------------------------------------------------------ busqueda

        internal static Scene Abrir()
        {
            Scene activa = SceneManager.GetActiveScene();
            if (activa.path == CreadorPrologoCiudad.RutaEscena && activa.isLoaded)
            {
                return activa;
            }
            return EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
        }

        private static List<Transform> TodosLosTransforms()
        {
            List<Transform> lista = new List<Transform>();
            foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                lista.AddRange(r.GetComponentsInChildren<Transform>(true));
            }
            return lista;
        }

        /// <summary>Bancas PZ01 activas que no son las nuevas: el transform mas alto cuyo nombre trae PZ01_Banca.</summary>
        private static List<Transform> BancasViejas()
        {
            List<Transform> lista = new List<Transform>();
            foreach (Transform t in TodosLosTransforms())
            {
                if (!t.gameObject.activeInHierarchy || t.name.Contains("Sentable") || !t.name.Contains("PZ01_Banca") || EsDeLaRaiz(t))
                {
                    continue;
                }
                bool padreTambien = false;
                for (Transform p = t.parent; p != null; p = p.parent)
                {
                    if (p.name.Contains("PZ01_Banca"))
                    {
                        padreTambien = true;
                        break;
                    }
                }
                if (!padreTambien)
                {
                    lista.Add(t);
                }
            }
            return lista;
        }

        private static bool EsDeLaRaiz(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name == NombreRaiz)
                {
                    return true;
                }
            }
            return false;
        }

        private static void DeshacerReemplazos(Scene escena, StringBuilder log)
        {
            GameObject raiz = GameObject.Find(NombreRaiz);
            if (raiz == null)
            {
                return;
            }
            GameObject[] roots = escena.GetRootGameObjects();
            foreach (Transform hijo in raiz.transform)
            {
                if (!hijo.name.StartsWith(PrefijoReemplazo))
                {
                    continue;
                }
                string[] partes = hijo.name.Substring(PrefijoReemplazo.Length).Split('|')[0].Split('/');
                Transform t = null;
                bool ok = true;
                for (int i = 0; i < partes.Length && ok; i++)
                {
                    int idx;
                    if (!int.TryParse(partes[i], out idx))
                    {
                        ok = false;
                    }
                    else if (i == 0)
                    {
                        ok = idx >= 0 && idx < roots.Length;
                        t = ok ? roots[idx].transform : null;
                    }
                    else
                    {
                        ok = t != null && idx >= 0 && idx < t.childCount;
                        t = ok ? t.GetChild(idx) : null;
                    }
                }
                if (ok && t != null)
                {
                    t.gameObject.SetActive(true);
                    log.AppendLine("Restaurada " + hijo.name);
                }
                else
                {
                    log.AppendLine("No se pudo restaurar " + hijo.name);
                }
            }
        }

        /// <summary>Camino de indices desde la raiz de la escena: "raiz/hijo/hijo".</summary>
        private static string IndiceRuta(Transform t)
        {
            List<int> idx = new List<int>();
            Transform c = t;
            while (c.parent != null)
            {
                idx.Add(c.GetSiblingIndex());
                c = c.parent;
            }
            idx.Add(c.GetSiblingIndex());
            idx.Reverse();
            return string.Join("/", idx.Select(i => i.ToString()).ToArray());
        }

        private static PuntoAparicion BuscarPunto(string id)
        {
            foreach (PuntoAparicion p in Object.FindObjectsByType<PuntoAparicion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
            return null;
        }

        private static Transform BuscarPorNombre(string parte)
        {
            return TodosLosTransforms().FirstOrDefault(t => t.name.Contains(parte) && t.gameObject.activeInHierarchy && !EsDeLaRaiz(t));
        }

        private static int Contar(string parte)
        {
            return TodosLosTransforms().Count(t => t.name.Contains(parte));
        }

        private static void ListarNombres(StringBuilder sb, string titulo, params string[] partes)
        {
            List<Transform> lista = TodosLosTransforms().Where(t => !EsDeLaRaiz(t) && partes.Any(p => t.name.Contains(p))).ToList();
            sb.AppendLine(titulo + ": " + lista.Count + " objetos");
            foreach (Transform t in lista.Take(12))
            {
                sb.AppendLine("  " + Ruta(t) + " en " + V(t.position) + (t.gameObject.activeInHierarchy ? "" : " (apagado)"));
            }
        }

        private static Vector3 CentroDe(string nombre, Vector3 respaldo)
        {
            Transform t = BuscarPorNombre(nombre);
            if (t == null)
            {
                return respaldo;
            }
            return t.GetComponentsInChildren<Renderer>().Length > 0 ? CajaDe(t).center : t.position;
        }

        private static Bounds CajaDe(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return new Bounds(t.position, Vector3.one);
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ------------------------------------------------------------------ colocacion

        private static GameObject Cargar(string ruta, StringBuilder log)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (g == null)
            {
                log.AppendLine("NO existe el prefab " + ruta);
            }
            return g;
        }

        private static void Poner(GameObject prefab, Transform padre, Vector3 pos, Quaternion rot, StringBuilder log, string texto)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(pos, rot);
            log.AppendLine(texto + ": " + prefab.name + " en " + V(pos));
        }

        /// <summary>Primer piso (no trigger, no de la raiz nueva, normal hacia arriba) bajo el punto, dentro de una tolerancia de altura.</summary>
        private static bool Piso(Vector3 xz, float yRef, Transform ignorar, float tolerancia, out Vector3 punto)
        {
            Physics.SyncTransforms();
            RaycastHit[] golpes = Physics.RaycastAll(new Vector3(xz.x, yRef + 3f, xz.z), Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(golpes, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit h in golpes)
            {
                if (ignorar != null && h.collider.transform.IsChildOf(ignorar))
                {
                    continue;
                }
                if (h.normal.y < 0.7f || Mathf.Abs(h.point.y - yRef) > tolerancia)
                {
                    continue;
                }
                punto = h.point;
                return true;
            }
            punto = xz;
            return false;
        }

        private static bool Libre(Vector3 p, Transform ignorar)
        {
            Collider[] cs = Physics.OverlapCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.7f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider c in cs)
            {
                if (ignorar == null || !c.transform.IsChildOf(ignorar))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Busca en anillos de 2 m (hasta 16 m) el punto con piso y sin nada en una capsula de persona.</summary>
        private static bool BuscarLibre(Vector3 deseado, float yRef, Transform ignorar, out Vector3 pos)
        {
            for (float r = 0f; r <= 16f; r += 2f)
            {
                int pasos = r == 0f ? 1 : 8;
                for (int i = 0; i < pasos; i++)
                {
                    float a = i * Mathf.PI * 2f / pasos;
                    Vector3 c = deseado + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    Vector3 piso;
                    if (Piso(c, yRef, ignorar, 1.5f, out piso) && Libre(piso, ignorar))
                    {
                        pos = piso;
                        return true;
                    }
                }
            }
            pos = deseado;
            return false;
        }

        private static Transform Hijo(Transform raiz, string parte)
        {
            if (raiz == null)
            {
                return null;
            }
            foreach (Transform t in raiz)
            {
                if (t.name.Contains(parte))
                {
                    return t;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ utilidades

        private static float Plano(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static string V(Vector3 v)
        {
            return "(" + v.x.ToString("0.0") + ", " + v.y.ToString("0.0") + ", " + v.z.ToString("0.0") + ")";
        }

        private static string Ruta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        private static void Escribir(string texto)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, texto);
        }

        private static void Foto(string nombre, Vector3 pos, Vector3 mira)
        {
            const int Ancho = 1280;
            const int Alto = 720;
            GameObject go = new GameObject("CamFotosT1606");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 1500f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log(Etiqueta + "foto " + nombre);
        }
    }
}
