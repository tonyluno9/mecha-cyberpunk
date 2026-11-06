using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del playtest del 5-nov (docs/playtest-05nov.md): mundo y arte de Ciudad_Prologo. Cada paso es idempotente y guarda la escena.
    /// Zona 1 (<see cref="Limpieza"/>): sin lluvia, sin letreros de primitivos, sin focos ni arco en el tianguis, sin textos flotantes y la luz
    /// de cada poste sale de su lámpara. No regenera Barrio.prefab: lo que vive en él se quita editando el prefab en su lugar.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2.Limpieza
    /// </summary>
    public static class ColocarS2
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        /// <summary>Capa de render (bit) de los postes de luz: su propia lámpara no la ilumina; el resto de las luces sí.</summary>
        public const int CapaPostes = 5;
        public static readonly StringBuilder Informe = new StringBuilder();

        // Raíces de escena que solo son letreros hechos con cubos y planos con texto.
        private static readonly string[] RaicesLetreros = { "T501_Suburbios/Letreros", "T502_Callejon/Letrero" };

        // Textos flotantes y carteles de primitivos sueltos.
        private static readonly string[] Sueltos =
        {
            "GTA_T407/Tianguis/ArcoTianguis",   // cartel y marco del tianguis
            "GTA_T414/LetreroElFox",
            "GTA_T407/Mercado/Vendedor/Nombre",
            "GTA_T407/Mercado/LetreroArmas",
            "AMB_Noche/Focos",                  // series de focos sobre el tianguis
            "T503_Tianguis/Focos",
            "AMB_Noche/Salpicaduras",           // salpicaduras de la lluvia
        };

        [MenuItem("Mecha Cyberpunk/S2/1 Limpieza (lluvia, letreros, focos, textos, postes)")]
        public static void Limpieza()
        {
            Informe.Clear();
            Abrir();
            QuitarDelBarrio(new[] { "Lluvia" });
            HashSet<Object> referidos = Referidos();
            int n = 0;
            foreach (string ruta in Sueltos.Concat(RaicesLetreros))
            {
                Transform t = Buscar(ruta);
                if (t != null)
                {
                    n += Quitar(t.gameObject, referidos);
                }
            }
            // Letreros de T-508 (CAFÉ, INTERNET, CANTINA, ABARROTES...): cubos, tubos y planos con texto.
            foreach (Transform t in Todos().Where(t => t.name.StartsWith("Letrero_") && t.root.name == "T508_CallesLlenas").ToList())
            {
                n += Quitar(t.gameObject, referidos);
            }
            // Textos 3D sueltos (I+D+I dentro del consultorio, EL MERCADO, ENCARGOS).
            foreach (TextMesh tm in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
            {
                if (tm.transform.root.name != "MechaEstacionado")
                {
                    n += Quitar(tm.gameObject, referidos);
                }
            }
            // Halos de niebla y luces de los letreros de neón planos que ya no se ven (Barrio/LetrerosNeon está apagado).
            Transform neones = Buscar("Barrio/LetrerosNeon");
            foreach (string grupo in new[] { "AMB_Noche/NieblaNeon", "AMB_Noche/LucesLetreros" })
            {
                Transform g = Buscar(grupo);
                if (g == null || neones == null || neones.gameObject.activeSelf)
                {
                    continue;
                }
                foreach (Transform h in g.Cast<Transform>().ToList())
                {
                    if (h.name.Contains("LetreroNeon_"))
                    {
                        n += Quitar(h.gameObject, referidos);
                    }
                }
            }
            Informe.AppendLine("Quitados o apagados: " + n);
            Informe.AppendLine("Postes: " + LucesDePostes());
            Guardar("s2-limpieza");
        }

        // ---------------------------------------------------------------- Zona 2: escala

        /// <summary>
        /// Altura medida (m) de cada Visual_* de pie en Idle, con la malla animada horneada (Play, 5-nov), y la del jugador con el mismo método.
        /// Los avatares de Tripo tienen piernas cortas y la animación los deja más bajos que su pose de reposo (1.80 en el editor).
        /// </summary>
        public const float AlturaJugador = 1.40f;
        public static readonly (string prefab, float medida, float relativa)[] Personajes =
        {
            ("Visual_Doctor", 1.17f, 1.00f),
            ("Visual_Inventor", 1.18f, 0.98f),
            ("Visual_Hacker", 1.43f, 1.00f),
            ("Visual_man", 1.30f, 1.00f),
            ("Visual_RobotHombre", 1.28f, 1.00f),
            ("Visual_SkullHombre", 1.33f, 1.00f),
            ("Visual_GuardiaNova", 1.49f, 1.25f),   // los robots de Nova son más altos que una persona (T-530)
            ("Visual_woman", 1.40f, 1.00f),
            ("Visual_woman2", 1.12f, 0.97f),
        };
        private const string RutaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";

        [MenuItem("Mecha Cyberpunk/S2/2 Escala de personajes y props")]
        public static void Escala()
        {
            Informe.Clear();
            foreach ((string prefab, float medida, float relativa) in Personajes)
            {
                string ruta = RutaVisuales + prefab + ".prefab";
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                Transform modelo = raiz.transform.Find("Giro/Modelo");
                float factor = AlturaJugador * relativa / medida;
                modelo.localScale = Vector3.one * factor;
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                PrefabUtility.UnloadPrefabContents(raiz);
                Informe.AppendLine(prefab + ": " + medida.ToString("F2") + " m -> " + (AlturaJugador * relativa).ToString("F2") + " m (escala " + factor.ToString("F3") + ")");
            }
            Abrir();
            // Los guardias de T-510 se habían agrandado 1.19 en la escena: con el prefab ya a la medida, van a 1.
            Transform guardias = Buscar("T510_Guardias");
            if (guardias != null)
            {
                foreach (Transform g in guardias)
                {
                    if (g.localScale.x > 1.05f)
                    {
                        Informe.AppendLine("  guardia " + g.name + " escala " + g.localScale.x.ToString("F2") + " -> 1");
                        g.localScale = Vector3.one;
                    }
                    // El Modelo toma la escala del prefab Visual_GuardiaNova: sin override en la escena.
                    foreach (Animator an in g.GetComponentsInChildren<Animator>(true))
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(an.transform))
                        {
                            SerializedObject so = new SerializedObject(an.transform);
                            PrefabUtility.RevertPropertyOverride(so.FindProperty("m_LocalScale"), InteractionMode.AutomatedAction);
                        }
                    }
                }
            }
            EscalaProps();
            Guardar("s2-escala");
        }

        /// <summary>Props fuera de escala contra el jugador (se llena con lo que salga en las capturas).</summary>
        public static void EscalaProps()
        {
        }

        // ---------------------------------------------------------------- Postes

        /// <summary>Lleva la luz de cada farola a la cabeza de su lámpara (punta del brazo), apuntando hacia abajo. Devuelve cuántas movió.</summary>
        public static int LucesDePostes()
        {
            int movidas = 0;
            foreach (Transform farola in Todos().Where(t => t.name.StartsWith("Farola_") && t.parent != null && t.parent.name == "Farolas"))
            {
                Transform luz = farola.Find("Luz");
                MeshFilter mf = farola.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null && m.GetComponent<Renderer>().enabled && m.gameObject.activeInHierarchy);
                if (luz == null || mf == null)
                {
                    continue;
                }
                if (CabezaLampara(mf, out Vector3 cabeza))
                {
                    luz.position = cabeza;
                    luz.rotation = Quaternion.Euler(90f, 0f, 0f);
                    Light l = luz.GetComponent<Light>();
                    if (l != null && l.type == LightType.Spot)
                    {
                        l.spotAngle = Mathf.Min(l.spotAngle, 120f);
                        // La lámpara no ilumina su propio poste (si no, el tubo se ve como la fuente de la luz).
                        UniversalAdditionalLightData d = l.GetUniversalAdditionalLightData();
                        d.renderingLayers = d.renderingLayers & ~(1u << CapaPostes);
                        EditorUtility.SetDirty(d);
                    }
                    foreach (Renderer rp in farola.GetComponentsInChildren<Renderer>(true))
                    {
                        rp.renderingLayerMask = 1u << CapaPostes;
                        EditorUtility.SetDirty(rp);
                    }
                    EditorUtility.SetDirty(luz);
                    movidas++;
                }
            }
            return movidas;
        }

        /// <summary>Punto bajo la cabeza de la lámpara: los vértices de arriba más lejos del eje del tubo, 8 cm por debajo de ellos.</summary>
        public static bool CabezaLampara(MeshFilter mf, out Vector3 cabeza)
        {
            cabeza = Vector3.zero;
            Vector3[] vs = mf.sharedMesh.vertices.Select(v => mf.transform.TransformPoint(v)).ToArray();
            if (vs.Length < 10)
            {
                return false;
            }
            float minY = vs.Min(v => v.y);
            float maxY = vs.Max(v => v.y);
            Vector3[] baseV = vs.Where(v => v.y < minY + 0.5f).ToArray();
            Vector3 eje = new Vector3(baseV.Average(v => v.x), 0f, baseV.Average(v => v.z));
            float Horiz(Vector3 v) => new Vector2(v.x - eje.x, v.z - eje.z).magnitude;
            Vector3[] arriba = vs.Where(v => v.y > maxY - 1.6f).ToArray();
            Vector3 extremo = arriba.OrderByDescending(Horiz).First();
            if (Horiz(extremo) < 0.6f)
            {
                return false; // sin brazo: la lámpara está sobre el tubo
            }
            Vector3[] cab = arriba.Where(v => new Vector2(v.x - extremo.x, v.z - extremo.z).magnitude < 0.55f).ToArray();
            Vector3 c = new Vector3(cab.Average(v => v.x), cab.Min(v => v.y) - 0.08f, cab.Average(v => v.z));
            // Un poco hacia el tubo: el extremo es la orilla de la pantalla, no su centro.
            Vector3 haciaEje = new Vector3(eje.x - c.x, 0f, eje.z - c.z).normalized;
            cabeza = c + haciaEje * 0.15f;
            return true;
        }

        // ---------------------------------------------------------------- Capturas

        /// <summary>Captura con posproceso desde 'desde' mirando a 'hacia' (ruta relativa al proyecto).</summary>
        public static void Foto(Vector3 desde, Vector3 hacia, string ruta, float campo = 60f)
        {
            GameObject go = new GameObject("CamaraS2");
            Camera cam = go.AddComponent<Camera>();
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            cam.fieldOfView = campo;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }

        /// <summary>Referencia a la izquierda y juego a la derecha, en una tira de 1920x540.</summary>
        public static void LadoALado(string referencia, string juego, string salida)
        {
            CapturaT550.Componer(referencia, juego, salida);
        }

        // ---------------------------------------------------------------- Ayudas comunes

        public static void Abrir()
        {
            if (EditorSceneManager.GetActiveScene().path != RutaEscena)
            {
                EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }
        }

        public static void Guardar(string informe)
        {
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/" + informe + ".txt", Informe.ToString());
            Debug.Log("[Arte] " + informe + ":\n" + Informe);
        }

        public static IEnumerable<Transform> Todos()
        {
            return EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));
        }

        public static Transform Buscar(string ruta)
        {
            int i = ruta.IndexOf('/');
            string raiz = i < 0 ? ruta : ruta.Substring(0, i);
            GameObject r = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == raiz);
            return r == null ? null : i < 0 ? r.transform : r.transform.Find(ruta.Substring(i + 1));
        }

        /// <summary>Todo lo que algún script de la escena referencia (objetos, componentes): eso se apaga en lugar de borrarse.</summary>
        public static HashSet<Object> Referidos()
        {
            HashSet<Object> set = new HashSet<Object>();
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null)
                {
                    continue;
                }
                SerializedProperty it = new SerializedObject(mb).GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                    {
                        set.Add(it.objectReferenceValue);
                    }
                }
            }
            return set;
        }

        /// <summary>Borra el objeto (o lo apaga si un script lo usa o si es parte de un prefab). Devuelve 1 si hizo algo.</summary>
        public static int Quitar(GameObject go, HashSet<Object> referidos)
        {
            if (go == null)
            {
                return 0;
            }
            bool usado = go.GetComponentsInChildren<Transform>(true).Any(t => referidos.Contains(t.gameObject) || t.GetComponents<Component>().Any(referidos.Contains));
            if (usado || PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsOutermostPrefabInstanceRoot(go))
            {
                if (!go.activeSelf)
                {
                    return 0;
                }
                go.SetActive(false);
                Informe.AppendLine("  apagado " + Ruta(go.transform));
                return 1;
            }
            Informe.AppendLine("  borrado " + Ruta(go.transform));
            Object.DestroyImmediate(go);
            return 1;
        }

        /// <summary>Quita hijos directos de Barrio.prefab editando el prefab (no lo regenera).</summary>
        public static void QuitarDelBarrio(string[] hijos)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaBarrio);
            bool cambio = false;
            foreach (string h in hijos)
            {
                Transform t = raiz.transform.Find(h);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                    Informe.AppendLine("  Barrio.prefab: borrado " + h);
                    cambio = true;
                }
            }
            if (cambio)
            {
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaBarrio);
            }
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        public static string Ruta(Transform t)
        {
            string p = t.name;
            for (Transform q = t.parent; q != null; q = q.parent)
            {
                p = q.name + "/" + p;
            }
            return p;
        }
    }
}
