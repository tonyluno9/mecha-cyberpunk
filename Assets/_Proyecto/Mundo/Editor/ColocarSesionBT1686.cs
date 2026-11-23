using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1686: lo que pidió la sesión B en docs/para-sesion-A.md (ronda D): el marcador Historia_EsperaSkulls a media calle frente a la casa,
    /// una máquina CY06_Expendedora entre el CIC y el Tianguis, y fuera los PuntoTrepar que suben a azoteas (T39).
    /// Batchmode (Unity cerrado), cada paso en su corrida:
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.ColocarSesionBT1686.Diagnostico -> Logs/t1686-diagnostico.txt (no cambia nada)
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.ColocarSesionBT1686.Aplicar     -> cambia, guarda y escribe Logs/t1686-aplicar.txt
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.ColocarSesionBT1686.Fotos       -> docs/capturas/_tareas/T1686/*.png
    /// </summary>
    public static class ColocarSesionBT1686
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaPrefabEspera = "Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_EsperaSkulls.prefab";
        public const string NombreEspera = "Historia_EsperaSkulls";
        public const string RutaPrefabMaquina = "Assets/Arte/Calle/Prefabs/CY06_Expendedora.prefab";
        public const string NombreMaquina = "CY06_Expendedora_T1686";
        public const string NombreInicio = "Inicio_Calle";
        public const string CarpetaFotos = "docs/capturas/_tareas/T1686";
        /// <summary>Un PuntoTrepar cuya salida queda a más de esto sobre la calle lleva a un techo.</summary>
        public const float AlturaAzotea = 2.5f;
        private const string Etiqueta = "[T-1686] ";

        // ================================================================== diagnostico

        [MenuItem("Mecha Cyberpunk/T-1686/Diagnostico")]
        public static void Diagnostico()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var sb = new StringBuilder();
            sb.AppendLine("T-1686 diagnostico " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            Transform inicio = Buscar(NombreInicio);
            if (inicio != null)
            {
                sb.AppendLine("Inicio_Calle " + inicio.position.ToString("F2") + " fwd " + inicio.forward.ToString("F2") + " (" + Ruta(inicio) + ")");
                foreach (Vector3 dir in new[] { inicio.forward, Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    Vector3 d = new Vector3(dir.x, 0f, dir.z).normalized;
                    sb.AppendLine(" rayos hacia " + d.ToString("F2") + ":");
                    for (float m = 0f; m <= 16f; m += 1f)
                    {
                        Vector3 p = inicio.position + d * m;
                        sb.AppendLine("   " + m.ToString("00") + " m " + Piso(p));
                    }
                }
            }
            foreach (string n in new[] { "Historia_CallejonSkulls", NombreEspera, "Lugar_CIC", "CIC", "T503_Tianguis", "Tianguis", "ArcoTianguis", "Vendedores_Tianguis", NombreMaquina })
            {
                foreach (Transform t in TodosConNombre(n))
                {
                    Bounds? b = Caja(t);
                    sb.AppendLine(n + " " + t.position.ToString("F1") + " activo=" + t.gameObject.activeInHierarchy + (b.HasValue ? " bounds " + b.Value.min.ToString("F1") + ".." + b.Value.max.ToString("F1") : "") + " (" + Ruta(t) + ")");
                }
            }
            sb.AppendLine();
            sb.AppendLine("=== CY06_Expendedora* ===");
            foreach (Transform t in Todos().Where(t => t.name.StartsWith("CY06_Expendedora") && (t.parent == null || !t.parent.name.StartsWith("CY06_Expendedora"))))
            {
                sb.AppendLine("  " + t.position.ToString("F1") + " rotY " + t.eulerAngles.y.ToString("0") + " activo=" + t.gameObject.activeInHierarchy + " " + Ruta(t));
            }
            sb.AppendLine();
            sb.AppendLine("=== PuntoTrepar ===");
            List<PuntoTrepar> puntos = Object.FindObjectsByType<PuntoTrepar>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(p => p.transform.position.x).ToList();
            foreach (PuntoTrepar p in puntos)
            {
                sb.AppendLine("  " + (p.gameObject.activeInHierarchy ? "ACT " : "off ") + (EsAzotea(p) ? "AZOTEA " : "baja   ") + p.Tipo + " pie " + p.PieAbajo.ToString("F1")
                    + " alto " + p.AlturaSubida.ToString("F1") + " salida " + p.Salida.ToString("F1") + " sobreCalle " + SobreCalle(p).ToString("F1") + " " + Ruta(p.transform));
            }
            sb.AppendLine("total " + puntos.Count + ", activos " + puntos.Count(p => p.gameObject.activeInHierarchy) + ", activos a azotea " + puntos.Count(p => p.gameObject.activeInHierarchy && EsAzotea(p)));
            sb.AppendLine();
            sb.AppendLine("=== GenteDeBanqueta con clip NPC_Platicar_1 (pregonar) ===");
            int nPreg = 0;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "GenteDeBanqueta")
                {
                    continue;
                }
                var so = new SerializedObject(mb);
                Object clip = so.FindProperty("clipPlatica").objectReferenceValue;
                if (clip != null && clip.name == "NPC_Platicar_1" && mb.name.StartsWith("Vendedor_"))
                {
                    nPreg++;
                    sb.AppendLine("  " + mb.transform.position.ToString("F1") + " modo " + so.FindProperty("modo").enumNames[so.FindProperty("modo").enumValueIndex] + " " + Ruta(mb.transform));
                }
            }
            sb.AppendLine("vendedores que pregonan: " + nPreg);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1686-diagnostico.txt", sb.ToString());
            Debug.Log(Etiqueta + "diagnostico escrito");
        }

        // ================================================================== aplicar

        /// <summary>Raíz de la sesión B donde ya vive Historia_CallejonSkulls (T-1606).</summary>
        public const string RaizSesionB = "T1606_SesionB";
        public const string RaizMaquina = "T1686_SesionA";
        /// <summary>El CIC (Lugar_CIC) y la entrada poniente del Tianguis, en x: la máquina va en medio, en la calle z≈0.</summary>
        public const float XCic = -73.3f;
        public const float XTianguis = -36f;
        public const float MinEspera = 6f;
        public const float MaxEspera = 10f;
        private const float XIdealEspera = 6.5f;
        private const float FondoMaquina = 0.98f;
        private const float AnchoMaquina = 1.64f;
        private static readonly System.Text.RegularExpressions.Regex NombrePuerta = new System.Text.RegularExpressions.Regex(
            "puerta|door|entrada|cortina|acceso|zaguan|porton|jamba|reja", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        [MenuItem("Mecha Cyberpunk/T-1686/Aplicar")]
        public static void Aplicar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var sb = new StringBuilder();
            sb.AppendLine("T-1686 aplicar " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            // Lo de una corrida anterior.
            foreach (Transform t in TodosConNombre(NombreEspera).ToList())
            {
                Object.DestroyImmediate(t.gameObject);
            }
            foreach (Transform t in TodosConNombre(RaizMaquina).ToList())
            {
                Object.DestroyImmediate(t.gameObject);
            }
            Physics.SyncTransforms();

            ColocarEspera(escena, sb);
            ColocarMaquina(escena, sb);
            QuitarTrepar(sb);
            CalmarVendedores(sb);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1686-aplicar.txt", sb.ToString());
            Debug.Log(Etiqueta + sb);
        }

        private static void ColocarEspera(Scene escena, StringBuilder sb)
        {
            Transform inicio = Buscar(NombreInicio);
            if (inicio == null)
            {
                sb.AppendLine("ESPERA: NO QUEDÓ, no hay Inicio_Calle");
                return;
            }
            Vector3 o = inicio.position;
            Vector3 adelante = new Vector3(inicio.forward.x, 0f, inicio.forward.z).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            // Bordes de la banqueta a los dos lados de la calle, sobre la línea del inicio.
            float bordeCasa = BordeBanqueta(o, -adelante);
            float bordeEnfrente = BordeBanqueta(o, adelante);
            sb.AppendLine("ESPERA: banqueta de la casa a " + bordeCasa.ToString("F2") + " m detrás del inicio, la de enfrente a " + bordeEnfrente.ToString("F2") + " m delante");
            Vector3 elegido = Vector3.zero;
            float mejor = float.MaxValue;
            bool hallado = false;
            for (float m = MinEspera; m <= MaxEspera + 0.01f; m += 0.25f)
            {
                for (float l = -2f; l <= 2.01f; l += 0.5f)
                {
                    Vector3 p = o + adelante * m + lado * l;
                    if (!Physics.Raycast(new Vector3(p.x, 4f, p.z), Vector3.down, out RaycastHit piso, 6f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    if (!piso.collider.name.StartsWith("Asfalto") || piso.point.y < -0.3f || piso.point.y > 0.5f)
                    {
                        continue;
                    }
                    Vector3 q = piso.point;
                    if (Physics.CheckCapsule(q + Vector3.up * 0.75f, q + Vector3.up * 1.7f, 0.6f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    if (bordeEnfrente - m < 1.5f)
                    {
                        continue;
                    }
                    float costo = Mathf.Abs(m - XIdealEspera) + Mathf.Abs(l) * 1.5f;
                    if (costo < mejor)
                    {
                        mejor = costo;
                        elegido = q;
                        hallado = true;
                    }
                }
            }
            if (!hallado)
            {
                sb.AppendLine("ESPERA: NO QUEDÓ, ningún punto de asfalto libre a 6-10 m");
                return;
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEspera);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            Transform padre = Buscar(RaizSesionB);
            if (padre != null)
            {
                go.transform.SetParent(padre, true);
            }
            // Mira hacia la casa: ahí está el jugador.
            go.transform.SetPositionAndRotation(elegido, Quaternion.LookRotation(-adelante, Vector3.up));
            Vector3 d = elegido - o;
            d.y = 0f;
            sb.AppendLine("ESPERA: " + NombreEspera + " en " + elegido.ToString("F2") + " (" + d.magnitude.ToString("F2") + " m del Inicio_Calle, "
                + (bordeEnfrente - Vector3.Dot(d, adelante)).ToString("F2") + " m de la banqueta de enfrente, "
                + (Vector3.Dot(d, adelante) + bordeCasa).ToString("F2") + " m de la de la casa), padre " + (padre != null ? padre.name : "(raíz)"));
        }

        /// <summary>Metros desde <paramref name="o"/> en <paramref name="dir"/> hasta donde el piso deja de ser asfalto (banqueta).</summary>
        private static float BordeBanqueta(Vector3 o, Vector3 dir)
        {
            for (float m = 0f; m < 30f; m += 0.1f)
            {
                Vector3 p = o + dir * m;
                if (Physics.Raycast(new Vector3(p.x, 4f, p.z), Vector3.down, out RaycastHit h, 6f, ~0, QueryTriggerInteraction.Ignore) && !h.collider.name.StartsWith("Asfalto"))
                {
                    return m;
                }
            }
            return 30f;
        }

        private static void ColocarMaquina(Scene escena, StringBuilder sb)
        {
            // Visto en las fotos de fachadas (FotosFachadas): la banqueta norte entre el CIC y la calle de la casa tiene una cortina metálica en x≈-60..-59
            // (con dos NPC parados enfrente) y una puerta en x≈-63..-62; el tramo de pared lisa con medidores entre la ventana y esa puerta es x≈-65.5..-63.5.
            const float xPreferida = -64.3f;
            List<Transform> maquinas = Todos().Where(t => t.name.StartsWith("CY06_Expendedora") && t.gameObject.activeInHierarchy).ToList();
            List<Vector3> gente = Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(a => a.transform.position).ToList();
            List<Transform> puertas = Todos().Where(t => t.gameObject.activeInHierarchy && NombrePuerta.IsMatch(t.name)).ToList();
            bool hallado = false;
            float mejor = float.MaxValue;
            Vector3 elegido = Vector3.zero;
            float giro = 0f;
            string nota = "";
            // Banqueta norte (la del CIC, la máquina mira a -Z, giro 0) y sur (mira a +Z, giro 180).
            foreach (float signo in new[] { 1f, -1f })
            {
                Vector3 atras = new Vector3(0f, 0f, signo);
                for (float x = XCic + 6f; x <= XTianguis - 4f; x += 0.5f)
                {
                    // Pared detrás: rayo desde la banqueta hacia afuera de la calle.
                    Vector3 desde = new Vector3(x, 1.2f, signo * 6.5f);
                    if (!Physics.Raycast(desde, atras, out RaycastHit pared, 4f, ~0, QueryTriggerInteraction.Ignore) || pared.normal.z * signo > -0.8f)
                    {
                        continue;
                    }
                    Vector3 centro = new Vector3(x, 0f, pared.point.z - signo * (FondoMaquina * 0.5f + 0.03f));
                    if (!Physics.Raycast(new Vector3(x, 3f, centro.z), Vector3.down, out RaycastHit piso, 4f, ~0, QueryTriggerInteraction.Ignore)
                        || !piso.collider.name.StartsWith("Banqueta") || piso.normal.y < 0.9f)
                    {
                        continue;
                    }
                    centro.y = piso.point.y;
                    // La pared sigue a todo lo ancho de la máquina.
                    bool paredEntera = true;
                    foreach (float dx in new[] { -AnchoMaquina * 0.5f, AnchoMaquina * 0.5f })
                    {
                        if (!Physics.Raycast(new Vector3(x + dx, 1.2f, centro.z), atras, out RaycastHit p2, 1f, ~0, QueryTriggerInteraction.Ignore) || p2.normal.z * signo > -0.8f)
                        {
                            paredEntera = false;
                        }
                    }
                    if (!paredEntera)
                    {
                        continue;
                    }
                    Vector3 mitad = new Vector3(AnchoMaquina * 0.5f + 0.1f, 0.9f, FondoMaquina * 0.5f);
                    if (Physics.CheckBox(centro + Vector3.up * 1.05f, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    if (puertas.Any(t => Plano(t.position, centro) < 3f) || maquinas.Any(t => Plano(t.position, centro) < 5.5f)
                        || gente.Any(g => Plano(g, centro) < 1.8f))
                    {
                        continue;
                    }
                    float costo = Mathf.Abs(x - xPreferida) + (signo > 0f ? 0f : 2f);
                    if (costo < mejor)
                    {
                        mejor = costo;
                        elegido = centro;
                        giro = signo > 0f ? 0f : 180f;
                        hallado = true;
                        nota = "pared " + pared.collider.name;
                    }
                }
            }
            if (!hallado)
            {
                sb.AppendLine("MAQUINA: NO QUEDÓ, no hay banqueta con pared libre entre el CIC y el Tianguis");
                return;
            }
            var raiz = new GameObject(RaizMaquina);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabMaquina), escena);
            go.name = NombreMaquina;
            go.transform.SetParent(raiz.transform, false);
            go.transform.SetPositionAndRotation(elegido, Quaternion.Euler(0f, giro, 0f));
            sb.AppendLine("MAQUINA: " + NombreMaquina + " en " + elegido.ToString("F2") + " giro " + giro + " (" + nota + "), a " + (elegido.x - XCic).ToString("F1")
                + " m del CIC y " + (XTianguis - elegido.x).ToString("F1") + " m del Tianguis");
        }

        private static float Plano(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private static void QuitarTrepar(StringBuilder sb)
        {
            List<PuntoTrepar> puntos = Object.FindObjectsByType<PuntoTrepar>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            int antes = puntos.Count(p => p.gameObject.activeInHierarchy);
            int quitados = 0;
            foreach (PuntoTrepar p in puntos)
            {
                if (p.gameObject.activeInHierarchy && EsAzotea(p))
                {
                    sb.AppendLine("TREPAR: fuera " + Ruta(p.transform) + " pie " + p.PieAbajo.ToString("F1") + " salida a " + SobreCalle(p).ToString("F1") + " m");
                    p.gameObject.SetActive(false);
                    quitados++;
                }
            }
            int despues = puntos.Count(p => p.gameObject.activeInHierarchy);
            sb.AppendLine("TREPAR: total " + puntos.Count + ", activos antes " + antes + ", quitados " + quitados + ", activos después " + despues
                + " (bajos que se quedan: " + puntos.Count(p => p.gameObject.activeInHierarchy && !EsAzotea(p)) + ")");
        }

        private static void CalmarVendedores(StringBuilder sb)
        {
            List<MonoBehaviour> gente = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != null && m.GetType().Name == "GenteDeBanqueta").ToList();
            Object esperar = gente.Select(m => new SerializedObject(m).FindProperty("clipPlatica").objectReferenceValue).FirstOrDefault(c => c != null && c.name == "NPC_Esperar");
            if (esperar == null)
            {
                sb.AppendLine("VENDEDORES: no hay clip NPC_Esperar en la escena, no se cambian");
                return;
            }
            int n = 0;
            foreach (MonoBehaviour m in gente)
            {
                var so = new SerializedObject(m);
                SerializedProperty clip = so.FindProperty("clipPlatica");
                if (m.name.StartsWith("Vendedor_") && clip.objectReferenceValue != null && clip.objectReferenceValue.name == "NPC_Platicar_1")
                {
                    clip.objectReferenceValue = esperar;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    n++;
                    sb.AppendLine("VENDEDORES: " + Ruta(m.transform) + " pregonar -> esperar");
                }
            }
            sb.AppendLine("VENDEDORES: " + n + " cambiados");
        }

        // ================================================================== fotos

        [MenuItem("Mecha Cyberpunk/T-1686/Fotos")]
        public static void Fotos()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            Directory.CreateDirectory(CarpetaFotos);
            var sb = new StringBuilder();
            Transform espera = Buscar(NombreEspera);
            Transform inicio = Buscar(NombreInicio);
            if (espera != null && inicio != null)
            {
                // Una marca roja temporal (no se guarda) para ver el punto en la foto.
                GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                marca.transform.position = espera.position + Vector3.up;
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetColor("_BaseColor", Color.red);
                marca.GetComponent<Renderer>().sharedMaterial = mat;
                Vector3 puerta = inicio.position - inicio.forward * 2.5f + inicio.right * 1.8f + Vector3.up * 1.7f;
                Foto("01_espera_desde_puerta", puerta, espera.position + Vector3.up, 70f);
                Foto("01_espera_aerea", espera.position + new Vector3(-8f, 20f, -10f), espera.position, 60f);
                Object.DestroyImmediate(marca);
                Object.DestroyImmediate(mat);
                sb.AppendLine("espera " + espera.position.ToString("F2") + ", foto desde " + puerta.ToString("F1"));
            }
            Transform maquina = Buscar(NombreMaquina);
            if (maquina != null)
            {
                Vector3 frente = -maquina.forward;
                Foto("02_expendedora", maquina.position + frente * 5f + maquina.right * 1.5f + Vector3.up * 1.7f, maquina.position + Vector3.up * 1.2f, 70f);
                Foto("02_expendedora_lejos", maquina.position + frente * 7f + Vector3.right * 9f + Vector3.up * 1.7f, maquina.position + Vector3.up * 1.2f, 70f);
                sb.AppendLine("maquina " + maquina.position.ToString("F2"));
            }
            PuntoTrepar trepar = Object.FindObjectsByType<PuntoTrepar>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.name == "Acceso_S2c_Suburbios_01");
            if (trepar != null)
            {
                Vector3 pie = trepar.PieAbajo;
                Foto("03_trepar_quitado", pie - trepar.Frente * 5f + Vector3.up * 1.7f, pie + Vector3.up * 2.5f, 70f);
                sb.AppendLine("trepar " + Ruta(trepar.transform) + " activo=" + trepar.gameObject.activeInHierarchy);
            }
            File.WriteAllText("Logs/t1686-fotos.txt", sb.ToString());
        }

        /// <summary>Fachadas de las dos banquetas entre el CIC y el Tianguis, para elegir una pared sin puertas.</summary>
        public static void FotosFachadas()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Directory.CreateDirectory(CarpetaFotos + "/fachadas");
            for (float x = -66f; x <= -42f; x += 6f)
            {
                Foto("fachadas/norte_x" + (-x).ToString("0"), new Vector3(x, 1.7f, -1f), new Vector3(x, 1.5f, 10f), 60f);
                Foto("fachadas/sur_x" + (-x).ToString("0"), new Vector3(x, 1.7f, 1f), new Vector3(x, 1.5f, -10f), 60f);
            }
        }

        public static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            var go = new GameObject("CamFotosT1686");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.farClipPlane = 1500f;
            cam.nearClipPlane = 0.05f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log(Etiqueta + "foto " + nombre);
        }

        // ================================================================== consultas (las usa la prueba)

        /// <summary>Altura de la salida del punto sobre el piso al pie de la subida.</summary>
        public static float SobreCalle(PuntoTrepar p)
        {
            Vector3 pie = p.PieAbajo;
            float suelo = pie.y;
            if (Physics.Raycast(pie + Vector3.up * 1.5f, Vector3.down, out RaycastHit h, 4f, ~0, QueryTriggerInteraction.Ignore))
            {
                suelo = h.point.y;
            }
            return Mathf.Max(p.Salida.y, p.Cima.y) - Mathf.Min(suelo, 0.2f);
        }

        public static bool EsAzotea(PuntoTrepar p) => SobreCalle(p) > AlturaAzotea;

        public static List<PuntoTrepar> ActivosAAzotea() => Object.FindObjectsByType<PuntoTrepar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(p => p.gameObject.activeInHierarchy && EsAzotea(p)).ToList();

        // ================================================================== utilidades

        public static string Piso(Vector3 p)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(p.x, p.y + 6f, p.z), Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
            if (hits.Length == 0)
            {
                return p.ToString("F1") + " sin piso";
            }
            return "(" + p.x.ToString("F1") + "," + p.z.ToString("F1") + ") " + string.Join(" | ", hits.Take(3).Select(h => h.collider.name + " y" + h.point.y.ToString("F2")));
        }

        public static IEnumerable<Transform> Todos()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (GameObject raiz in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                    {
                        yield return t;
                    }
                }
            }
        }

        public static IEnumerable<Transform> TodosConNombre(string n) => Todos().Where(t => t.name == n);

        public static Transform Buscar(string n) => TodosConNombre(n).FirstOrDefault(t => t.gameObject.activeInHierarchy) ?? TodosConNombre(n).FirstOrDefault();

        private static Bounds? Caja(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 0)
            {
                return null;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        public static string Ruta(Transform t)
        {
            string r = t.name;
            int n = 0;
            for (Transform p = t.parent; p != null && n < 5; p = p.parent, n++)
            {
                r = p.name + "/" + r;
            }
            return r;
        }
    }
}
