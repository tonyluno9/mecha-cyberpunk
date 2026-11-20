using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-1520: el cuarto se quita de verdad (T-1496 solo lo había apagado y la casa de Alan seguía con su sala, cama y espejo).
    /// Ciudad_Prologo: borra T550_Cuartos y S2t_Espejo (con Cuarto_Habitacion y todo lo que cuelga), apaga Lugar_Casa/Interior/Sala y Lugar_Casa/Prologo
    /// (instancias del Barrio: solo se pueden apagar), crea la aparición "Inicio_Calle" en la calle cerca de la puerta de la casa (la escoge
    /// buscando suelo libre, sin techo y sin cuerpos físicos cerca) y vuelve a poner la cama (dormir) y el espejo como instancias propias de la
    /// escena, sin modelo, a los lados de Casa_Puerta. Deja DirectorPandillas apuntando a Inicio_Calle.
    /// Menu_Calles: borra T550_Cuartos si lo trae. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.SinCuartoT1520.Aplicar y luego ...Diagnostico.
    /// </summary>
    public static class SinCuartoT1520
    {
        public const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaMenu = "Assets/_Proyecto/Escenas/Menu_Calles.unity";
        public static readonly string[] EscenasDeBuild =
        {
            "Assets/_Proyecto/Escenas/Inicio.unity",
            "Assets/_Proyecto/Escenas/Menu_Calles.unity",
            "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity",
            "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
            "Assets/_Proyecto/Escenas/Flashback.unity",
        };
        public const string RaizCuartos = "T550_Cuartos";
        public const string NombreCuarto = "Cuarto_Habitacion";
        public const string IdInicio = "Inicio_Calle";
        public const string IdPuerta = "Casa_Puerta";
        public const string RaizInteracciones = "Casa_PuertaInteracciones";
        public const string PrefabCama = "Assets/_Proyecto/Progresion/Prologo/Prefabs/CamaJugador.prefab";
        public const string PrefabEspejo = "Assets/_Proyecto/Progresion/Prologo/Prefabs/EspejoJugador.prefab";
        public const float SeparacionLateral = 1.3f;
        /// <summary>Punto preferido (el borde del asfalto frente a la puerta de la casa, sobre la calle del oeste: puerta en x -55.5, banqueta hasta -54).</summary>
        public static readonly Vector3 Preferido = new Vector3(-53f, 0f, -77f);

        [MenuItem("Mecha Cyberpunk/T-1520/Aplicar sin cuarto")]
        public static void Aplicar()
        {
            AplicarCiudad();
            AplicarMenu();
        }

        public static void AplicarCiudad()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaCiudad, OpenSceneMode.Single);
            int borrados = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects().Where(g => g.name == RaizCuartos).ToArray())
            {
                int cantidad = raiz.GetComponentsInChildren<Transform>(true).Length;
                Object.DestroyImmediate(raiz);
                borrados += cantidad;
            }
            // El espejo de reflejo del cuarto (raíz aparte, dentro de la casa).
            foreach (GameObject raiz in escena.GetRootGameObjects().Where(g => g.name == "S2t_Espejo").ToArray())
            {
                borrados += raiz.GetComponentsInChildren<Transform>(true).Length;
                Object.DestroyImmediate(raiz);
            }
            // Por si Cuarto_Habitacion quedó suelto en otra raíz.
            foreach (Transform t in Todas(escena).Where(t => t != null && t.name == NombreCuarto).ToArray())
            {
                borrados += t.GetComponentsInChildren<Transform>(true).Length;
                Object.DestroyImmediate(t.gameObject);
            }

            int apagados = 0;
            foreach (Transform t in Todas(escena).ToArray())
            {
                if (t == null || !t.gameObject.activeSelf)
                {
                    continue;
                }
                string ruta = Ruta(t);
                if (ruta.EndsWith("Lugar_Casa/Interior/Sala") || ruta.EndsWith("Lugar_Casa/Prologo"))
                {
                    t.gameObject.SetActive(false);
                    apagados++;
                }
            }

            Transform puerta = Todas(escena).FirstOrDefault(t => t != null && t.name == IdPuerta);
            Physics.SyncTransforms();
            Vector3 punto = BuscarSueloLibre(out bool encontrado);
            Transform inicio = CrearInicio(escena, punto);
            int interacciones = PonerCamaYEspejo(escena, puerta);
            bool director = ApuntarDirector(escena);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[T-1520] Ciudad_Prologo: objetos del cuarto borrados " + borrados + "; apagados (sala y cama/espejo viejos) " + apagados
                + "; " + IdInicio + " en " + inicio.position + (encontrado ? " (suelo libre encontrado)" : " (SIN suelo libre: revisar a mano)")
                + "; cama/espejo nuevos junto a la puerta " + interacciones + "; DirectorPandillas apunta a " + IdInicio + ": " + director);
        }

        public static void AplicarMenu()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Single);
            int borrados = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects().Where(g => g.name == RaizCuartos).ToArray())
            {
                borrados += raiz.GetComponentsInChildren<Transform>(true).Length;
                Object.DestroyImmediate(raiz);
            }
            foreach (Transform t in Todas(escena).Where(t => t != null && t.name == NombreCuarto).ToArray())
            {
                borrados += t.GetComponentsInChildren<Transform>(true).Length;
                Object.DestroyImmediate(t.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[T-1520] Menu_Calles: objetos del cuarto borrados " + borrados);
        }

        /// <summary>Lista lo que aún menciona al cuarto (por nombre) en cada escena de build, sin cambiar nada.</summary>
        [MenuItem("Mecha Cyberpunk/T-1520/Diagnóstico del cuarto")]
        public static void Diagnostico()
        {
            StringBuilder sb = new StringBuilder("[T-1520] Diagnóstico del cuarto en las escenas de build\n");
            foreach (string ruta in EscenasDeBuild)
            {
                Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                List<string> hallazgos = new List<string>();
                foreach (Transform t in Todas(escena))
                {
                    if (t == null)
                    {
                        continue;
                    }
                    string n = t.name.ToLowerInvariant();
                    bool nombre = n.Contains("cuarto") || n.Contains("habitacion") || n.Contains("recamara") || n.Contains("prop_cama") || n.Contains("prop_espejo")
                        || n == "sala" || n == "casa_cama";
                    bool componente = t.GetComponents<Component>().Any(c => c != null && (c.GetType().Name == "CuartoInterior" || c.GetType().Name == "PuertaCuarto"
                        || c.GetType().Name == "CamaJugador" || c.GetType().Name == "EspejoJugador"));
                    if (nombre || componente)
                    {
                        hallazgos.Add("  " + (t.gameObject.activeInHierarchy ? "[ACTIVO] " : "[apagado] ") + Ruta(t)
                            + (componente ? " <" + string.Join(",", t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name)
                                .Where(x => x == "CuartoInterior" || x == "PuertaCuarto" || x == "CamaJugador" || x == "EspejoJugador")) + ">" : "")
                            + " pos " + t.position);
                    }
                }
                sb.AppendLine(ruta + ": " + hallazgos.Count + " menciones");
                foreach (string h in hallazgos.Take(60))
                {
                    sb.AppendLine(h);
                }
                if (ruta == RutaCiudad)
                {
                    bool hayInicio = Todas(escena).Any(t => t != null && t.name == IdInicio);
                    sb.AppendLine("  aparición " + IdInicio + ": " + (hayInicio ? "existe" : "NO existe"));
                }
            }
            Debug.Log(sb.ToString());
        }

        // ---------- piezas ----------

        // Busca cerca de Preferido un punto con suelo a ~0 o ~0.2 m, sin techo, sin colisionadores a la altura del cuerpo y sin cuerpos físicos
        // (carros, props sueltos) a 3.5 m. No hay datos del tráfico en el editor: por eso se pide el borde del asfalto y no el centro del carril.
        private static Vector3 BuscarSueloLibre(out bool encontrado)
        {
            List<Vector3> candidatos = new List<Vector3>();
            for (float x = -54.5f; x <= -50.5f; x += 0.75f)
            {
                for (float z = -87f; z <= -67f; z += 1.5f)
                {
                    candidatos.Add(new Vector3(x, 0f, z));
                }
            }
            foreach (Vector3 c in candidatos.OrderBy(c => (c - Preferido).sqrMagnitude))
            {
                if (!Physics.Raycast(new Vector3(c.x, 8f, c.z), Vector3.down, out RaycastHit suelo, 16f, ~0, QueryTriggerInteraction.Ignore)
                    || suelo.point.y < -0.1f || suelo.point.y > 0.4f)
                {
                    continue;
                }
                Vector3 p = new Vector3(c.x, suelo.point.y, c.z);
                if (Physics.CheckCapsule(p + Vector3.up * 0.5f, p + Vector3.up * 1.6f, 0.45f, ~0, QueryTriggerInteraction.Ignore)
                    || Physics.Raycast(p + Vector3.up * 1.8f, Vector3.up, 10f, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                bool cuerpoCerca = Physics.OverlapSphere(p + Vector3.up, 3.5f, ~0, QueryTriggerInteraction.Ignore)
                    .Any(col => col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic);
                if (cuerpoCerca)
                {
                    continue;
                }
                encontrado = true;
                return p;
            }
            encontrado = false;
            return new Vector3(Preferido.x, 0f, Preferido.z);
        }

        private static Transform CrearInicio(Scene escena, Vector3 posicion)
        {
            Transform existente = Todas(escena).FirstOrDefault(t => t != null && t.name == IdInicio);
            Quaternion giro = Quaternion.Euler(0f, 90f, 0f); // mira hacia la calle (+X)
            if (existente != null)
            {
                existente.SetPositionAndRotation(posicion, giro);
                return existente;
            }
            // Se clona la aparición Clinica_Camilla (un PuntoAparicion suelto de la escena) para no referenciar el ensamblado de Mundo.
            Transform molde = Todas(escena).FirstOrDefault(t => t != null && t.name == "Clinica_Camilla" && t.gameObject.scene == escena && !EsDeInstancia(t));
            if (molde == null)
            {
                Debug.LogError("[T-1520] No hay Clinica_Camilla para clonar; crea " + IdInicio + " a mano.");
                GameObject vacio = new GameObject(IdInicio);
                vacio.transform.SetPositionAndRotation(posicion, giro);
                return vacio.transform;
            }
            GameObject copia = Object.Instantiate(molde.gameObject, molde.parent);
            copia.name = IdInicio;
            copia.SetActive(true);
            foreach (MonoBehaviour m in copia.GetComponents<MonoBehaviour>())
            {
                if (m != null && m.GetType().Name == "PuntoAparicion")
                {
                    SerializedObject so = new SerializedObject(m);
                    so.FindProperty("id").stringValue = IdInicio;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            copia.transform.SetPositionAndRotation(posicion, giro);
            return copia.transform;
        }

        private static bool EsDeInstancia(Transform t) => PrefabUtility.IsPartOfPrefabInstance(t);

        // Cama (dormir y guardar) y espejo (editar personaje) como instancias de la escena, sin modelo, a los lados de la puerta.
        private static int PonerCamaYEspejo(Scene escena, Transform puerta)
        {
            if (puerta == null)
            {
                Debug.LogError("[T-1520] No encontré " + IdPuerta + "; la cama y el espejo no se movieron.");
                return 0;
            }
            Transform raiz = escena.GetRootGameObjects().Select(g => g.transform).FirstOrDefault(t => t.name == RaizInteracciones);
            if (raiz == null)
            {
                raiz = new GameObject(RaizInteracciones).transform;
            }
            raiz.SetPositionAndRotation(puerta.position, puerta.rotation);
            int creados = 0;
            creados += Asegurar<CamaJugador>(raiz, PrefabCama, -SeparacionLateral);
            creados += Asegurar<EspejoJugador>(raiz, PrefabEspejo, SeparacionLateral);
            return raiz.childCount;
        }

        private static int Asegurar<T>(Transform raiz, string rutaPrefab, float lateral) where T : Component
        {
            T actual = raiz.GetComponentInChildren<T>(true);
            GameObject objeto;
            int creado = 0;
            if (actual != null)
            {
                objeto = actual.gameObject;
                while (objeto.transform.parent != raiz && objeto.transform.parent != null)
                {
                    objeto = objeto.transform.parent.gameObject;
                }
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
                if (prefab == null)
                {
                    Debug.LogError("[T-1520] No existe " + rutaPrefab);
                    return 0;
                }
                objeto = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
                creado = 1;
            }
            objeto.transform.SetPositionAndRotation(raiz.position + raiz.right * lateral, raiz.rotation);
            objeto.SetActive(true);
            foreach (MeshRenderer r in objeto.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponentInParent<Canvas>() == null)
                {
                    r.enabled = false;
                }
            }
            foreach (Collider c in objeto.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }
            return creado;
        }

        private static bool ApuntarDirector(Scene escena)
        {
            DirectorPandillas director = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DirectorPandillas>(true)).FirstOrDefault();
            if (director == null)
            {
                Debug.LogError("[T-1520] No encontré DirectorPandillas en Ciudad_Prologo.");
                return false;
            }
            SerializedObject so = new SerializedObject(director);
            SerializedProperty p = so.FindProperty("idInicio");
            if (p == null)
            {
                return false;
            }
            p.stringValue = IdInicio;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static IEnumerable<Transform> Todas(Scene escena)
        {
            return escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));
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
    }
}
