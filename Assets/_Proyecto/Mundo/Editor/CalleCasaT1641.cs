using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1641 (T21 puntos 1 y 2): calle de la casa del jugador. Diagnostico escribe Logs/t1641.txt (todo lo que hay cerca del inicio del jugador).
    /// En batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CalleCasaT1641.Diagnostico | Aplicar | Fotos (Fotos en corrida aparte).
    /// </summary>
    public static class CalleCasaT1641
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaLog = "Logs/t1641.txt";
        public const string NombreRaiz = "T1641_CalleCasa";
        public const string RutaMatAsfalto = "Assets/_Proyecto/Mundo/Materiales/CalleCasa_Asfalto.mat";
        private const string RutaMatBase = "Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat";
        private const string RutaMatLinea = "Assets/Arte/Calle/Lineas/M01_LineasAmarillas.mat";
        private const string RutaMatBordillo = "Assets/Arte/Calle/Banqueta/T506_Barrio_Banqueta_T01_32x32.mat";

        // Callejon de la vuelta de la casa (Barrio/Manzana_4/Callejon): entre Edificio_42/33 (z -63.9) y Edificio_34 (z -54.1), de la calle (x -54) al portón del fondo (x -86).
        public const float XEste = -54.1f;
        public const float XOeste = -86.0f;
        public const float ZCentro = -59f;
        public const float AnchoAsfalto = 5.6f;
        public const float YPiso = 0.2f;
        public const float Grosor = 0.06f;
        public const float MetrosPorRepeticion = 8f; // como Barrio_Asfalto: 272 m / 34 repeticiones

        /// <summary>Doña Lupe (GTA_T406/DonaLupe) estaba parada en medio de la calle a 10 m de la puerta; pasa a la banqueta de enfrente.</summary>
        public static readonly Vector3 PosLupeNueva = new Vector3(-40.6f, YPiso, -73.5f);

        [MenuItem("Mecha Cyberpunk/T-1641/Aplicar")]
        public static void Aplicar()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var sb = new StringBuilder();

            // 1) Doña Lupe fuera de la calle.
            GameObject lupe = GameObject.Find("GTA_T406/DonaLupe");
            if (lupe != null)
            {
                sb.AppendLine("DonaLupe: " + lupe.transform.position.ToString("0.00") + " -> " + PosLupeNueva);
                lupe.transform.position = PosLupeNueva;
                lupe.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            }
            else
            {
                sb.AppendLine("DonaLupe: NO encontrada");
            }

            // 2) Asfalto, bordillos y raya del callejon.
            GameObject viejo = GameObject.Find(NombreRaiz);
            while (viejo != null)
            {
                Object.DestroyImmediate(viejo);
                viejo = GameObject.Find(NombreRaiz);
            }
            Material baseMat = AssetDatabase.LoadAssetAtPath<Material>(RutaMatBase);
            Material linea = AssetDatabase.LoadAssetAtPath<Material>(RutaMatLinea);
            Material bordillo = AssetDatabase.LoadAssetAtPath<Material>(RutaMatBordillo);
            float largo = XEste - XOeste;
            float centroX = (XEste + XOeste) * 0.5f;
            Material asfalto = AssetDatabase.LoadAssetAtPath<Material>(RutaMatAsfalto);
            if (asfalto == null)
            {
                asfalto = new Material(baseMat);
                AssetDatabase.CreateAsset(asfalto, RutaMatAsfalto);
            }
            var tiling = new Vector2(largo / MetrosPorRepeticion, AnchoAsfalto / MetrosPorRepeticion);
            asfalto.mainTextureScale = tiling;
            asfalto.SetTextureScale("_BaseMap", tiling);
            asfalto.SetTextureScale("_BumpMap", tiling);
            EditorUtility.SetDirty(asfalto);
            AssetDatabase.SaveAssets();

            var raiz = new GameObject(NombreRaiz);
            // Losa de asfalto (quad acostado: +X largo, +Z ancho).
            GameObject losa = Plano("Asfalto", raiz.transform, new Vector3(centroX, YPiso + Grosor, ZCentro), Quaternion.Euler(90f, 0f, 0f), new Vector3(largo, AnchoAsfalto, 1f), asfalto);
            sb.AppendLine("Asfalto: x " + XOeste + ".." + XEste + " z " + (ZCentro - AnchoAsfalto / 2) + ".." + (ZCentro + AnchoAsfalto / 2) + " bounds=" + losa.GetComponent<Renderer>().bounds.size.ToString("0.00"));
            // Raya central amarilla, en 3 tramos (la textura se repite 4 veces por 11.7 m en la calle principal).
            const int Tramos = 3;
            float largoTramo = largo / Tramos;
            for (int i = 0; i < Tramos; i++)
            {
                float cx = XOeste + largoTramo * (i + 0.5f);
                GameObject r = Plano("Raya_" + i, raiz.transform, new Vector3(cx, YPiso + Grosor + 0.005f, ZCentro), Quaternion.Euler(90f, 90f, 0f), new Vector3(1.58f, largoTramo, 1f), linea);
                var ts = new Vector2(1f, largoTramo / 11.7f); // el material ya trae tiling y = 4
                var mr = r.GetComponent<Renderer>();
                AjustarUV(r.GetComponent<MeshFilter>(), ts);
                if (i == 0) { sb.AppendLine("Raya bounds=" + mr.bounds.size.ToString("0.00")); }
            }
            // Bordillos de concreto a los dos lados del asfalto.
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = lado < 0 ? "Bordillo_Sur" : "Bordillo_Norte";
                b.transform.SetParent(raiz.transform, false);
                b.transform.position = new Vector3(centroX, YPiso + 0.06f, ZCentro + lado * (AnchoAsfalto * 0.5f + 0.08f));
                b.transform.localScale = new Vector3(largo, 0.12f, 0.16f);
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.GetComponent<Renderer>().sharedMaterial = bordillo;
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog.Replace("t1641", "t1641-aplicar"), sb.ToString());
            Debug.Log("[T-1641] " + sb);
        }

        private static GameObject Plano(string nombre, Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = escala;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var mr = go.GetComponent<Renderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Copia la malla del quad con UV multiplicadas (para repetir la textura sin crear otro material).</summary>
        private static void AjustarUV(MeshFilter mf, Vector2 mult)
        {
            Mesh m = Object.Instantiate(mf.sharedMesh);
            m.name = "QuadRaya";
            Vector2[] uv = m.uv;
            for (int i = 0; i < uv.Length; i++) { uv[i] = new Vector2(uv[i].x * mult.x, uv[i].y * mult.y); }
            m.uv = uv;
            mf.sharedMesh = m;
        }

        public static readonly Vector3 Inicio = new Vector3(-55.5f, 0.2f, -77f);

        [MenuItem("Mecha Cyberpunk/T-1641/Diagnostico")]
        public static void Diagnostico()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var sb = new StringBuilder();
            sb.AppendLine("T-1641 Diagnostico cerca de " + Inicio);
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in r.GetComponentsInChildren<Transform>(false))
                {
                    Vector3 p = t.position;
                    float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(Inicio.x, Inicio.z));
                    if (d > 22f)
                    {
                        continue;
                    }
                    Renderer rd = t.GetComponent<Renderer>();
                    var mbs = t.GetComponents<MonoBehaviour>().Where(m => m != null).Select(m => m.GetType().Name).ToArray();
                    if (rd == null && mbs.Length == 0)
                    {
                        continue;
                    }
                    string mats = rd != null ? string.Join(",", rd.sharedMaterials.Select(m => m != null ? m.name : "null")) : "";
                    string tam = rd != null ? " tam=" + rd.bounds.size.ToString("0.0") : "";
                    sb.AppendLine(d.ToString("0.0") + "m " + Ruta(t) + " pos=" + p.ToString("0.0") + tam + (mats.Length > 0 ? " mat=" + mats : "") + (mbs.Length > 0 ? " [" + string.Join(",", mbs) + "]" : ""));
                }
            }
            sb.AppendLine("--- suelos grandes (planos, sin altura) que cubren o rodean el punto, radio 60 m ---");
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer rd in r.GetComponentsInChildren<Renderer>(false))
                {
                    Bounds b = rd.bounds;
                    if (b.size.y > 1.0f || b.size.x * b.size.z < 12f || b.min.y > 1f)
                    {
                        continue;
                    }
                    float dx = Mathf.Max(0f, Mathf.Max(b.min.x - Inicio.x, Inicio.x - b.max.x));
                    float dz = Mathf.Max(0f, Mathf.Max(b.min.z - Inicio.z, Inicio.z - b.max.z));
                    if (Mathf.Sqrt(dx * dx + dz * dz) > 60f)
                    {
                        continue;
                    }
                    sb.AppendLine(Ruta(rd.transform) + " centro=" + b.center.ToString("0.0") + " tam=" + b.size.ToString("0.0") + " mat=" + string.Join(",", rd.sharedMaterials.Select(m => m != null ? m.name : "null")));
                }
            }
            sb.AppendLine("--- superficies planas (alto < 0.6) a menos de 90 m con material SIN textura base ---");
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer rd in r.GetComponentsInChildren<Renderer>(false))
                {
                    Bounds b = rd.bounds;
                    if (b.size.y > 0.6f || b.size.x * b.size.z < 4f || b.min.y > 1f || b.max.y < -0.6f)
                    {
                        continue;
                    }
                    float dx = Mathf.Max(0f, Mathf.Max(b.min.x - Inicio.x, Inicio.x - b.max.x));
                    float dz = Mathf.Max(0f, Mathf.Max(b.min.z - Inicio.z, Inicio.z - b.max.z));
                    if (Mathf.Sqrt(dx * dx + dz * dz) > 90f)
                    {
                        continue;
                    }
                    foreach (Material m in rd.sharedMaterials)
                    {
                        if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null)
                        {
                            sb.AppendLine(Ruta(rd.transform) + " centro=" + b.center.ToString("0.0") + " tam=" + b.size.ToString("0.0") + " mat=" + m.name + " color=" + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "?"));
                        }
                    }
                }
            }
            sb.AppendLine("--- hijos de Barrio/Calles y objetos inactivos / renderers apagados a menos de 70 m ---");
            GameObject calles = GameObject.Find("Barrio/Calles");
            if (calles != null)
            {
                foreach (Transform h in calles.transform)
                {
                    Renderer[] rs = h.GetComponentsInChildren<Renderer>(true);
                    sb.AppendLine("Calles/" + h.name + " activo=" + h.gameObject.activeSelf + " hijos=" + h.childCount + " renderers=" + rs.Length);
                }
            }
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                {
                    Renderer rd = t.GetComponent<Renderer>();
                    bool apagado = !t.gameObject.activeInHierarchy || (rd != null && !rd.enabled);
                    if (!apagado || Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(Inicio.x, Inicio.z)) > 70f)
                    {
                        continue;
                    }
                    if (rd != null || t.name.Contains("Calle") || t.name.Contains("Asfalto") || t.name.Contains("Piso") || t.name.Contains("Arroyo"))
                    {
                        sb.AppendLine("APAGADO " + Ruta(t) + " pos=" + t.position.ToString("0.0") + (rd != null ? " tam=" + rd.bounds.size.ToString("0.0") : ""));
                    }
                }
            }
            GameObject la = GameObject.Find("S2n_Lineas/Amarillas/LineaAmarilla");
            if (la != null)
            {
                MeshFilter mf = la.GetComponent<MeshFilter>();
                sb.AppendLine("LineaAmarilla modelo: mesh=" + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "?") + " rot=" + la.transform.eulerAngles + " escala=" + la.transform.lossyScale + " pos=" + la.transform.position + " collider=" + (la.GetComponent<Collider>() != null) + " capa=" + la.layer);
            }
            GameObject asf = GameObject.Find("Barrio/Calles/Asfalto");
            if (asf != null)
            {
                sb.AppendLine("Asfalto: capa=" + asf.layer + " collider=" + (asf.GetComponent<Collider>() != null) + " tag=" + asf.tag);
            }
            GameObject ban = GameObject.Find("Barrio/Manzana_4/Banqueta");
            if (ban != null)
            {
                sb.AppendLine("Banqueta Manzana_4: capa=" + ban.layer + " collider=" + (ban.GetComponent<Collider>() != null) + " tag=" + ban.tag + " pos=" + ban.transform.position + " esc=" + ban.transform.lossyScale);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, sb.ToString());
            Debug.Log("[T-1641] diagnostico en " + RutaLog);
        }

        /// <summary>Capturas en docs/capturas/_tareas/T21/ con el sufijo SUF (por defecto "antes"; SUF=despues en la variable de entorno T1641_SUF). Corrida aparte de Aplicar.</summary>
        [MenuItem("Mecha Cyberpunk/T-1641/Fotos")]
        public static void Fotos()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            string suf = System.Environment.GetEnvironmentVariable("T1641_SUF");
            if (string.IsNullOrEmpty(suf)) { suf = "antes"; }
            Directory.CreateDirectory("docs/capturas/_tareas/T21");
            Foto("inicio_" + suf, new Vector3(-55.5f, 1.7f, -77f), new Vector3(-40f, 1.2f, -77f), 70f);
            Foto("inicio_lateral_" + suf, new Vector3(-55.5f, 1.7f, -77f), new Vector3(-45f, 1.2f, -92f), 70f);
            Foto("aerea_" + suf, new Vector3(-50f, 70f, -100f), new Vector3(-50f, 0f, -77f), 60f);
            Foto("aerea_cerca_" + suf, new Vector3(-52f, 28f, -88f), new Vector3(-52f, 0f, -80f), 60f);
            Foto("calle_lado_" + suf, new Vector3(-48f, 6f, -96f), new Vector3(-50f, 0f, -82f), 65f);
            Foto("norte_" + suf, new Vector3(-55.5f, 1.7f, -77f), new Vector3(-55.5f, 1.2f, -50f), 70f);
            Foto("sur_" + suf, new Vector3(-55.5f, 1.7f, -77f), new Vector3(-55.5f, 1.2f, -100f), 70f);
            Foto("aerea_oeste_" + suf, new Vector3(-80f, 90f, -100f), new Vector3(-80f, 0f, -62f), 60f);
            Foto("callejon_" + suf, new Vector3(-55f, 1.7f, -60f), new Vector3(-75f, 1.2f, -60f), 70f);
            Foto("callejon_aerea_" + suf, new Vector3(-68f, 35f, -75f), new Vector3(-68f, 0f, -59f), 60f);
            Foto("callejon_oeste_" + suf, new Vector3(-80f, 1.7f, -59f), new Vector3(-56f, 1.2f, -59f), 70f);
            Foto("dorada_" + suf, new Vector3(-56f, 8f, -43f), new Vector3(-90f, 0f, -43f), 70f);
            Foto("dorada_aerea_" + suf, new Vector3(-80f, 45f, -60f), new Vector3(-80f, 0f, -43f), 60f);
            Foto("aerea_norte_" + suf, new Vector3(-60f, 90f, -80f), new Vector3(-60f, 0f, -45f), 60f);
        }

        private static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            var go = new GameObject("CamFotosT1641");
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
            File.WriteAllBytes("docs/capturas/_tareas/T21/" + nombre + ".png", tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }

        public const string RutaPrefabSkulls = "Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_CallejonSkulls.prefab";
        public const string NombreSkulls = "Historia_CallejonSkulls";
        private static readonly Vector3 OjoInicio = new Vector3(-55.5f, 1.7f, -77f);

        /// <summary>
        /// Ronda C de la sesión B: pone el marcador Historia_CallejonSkulls en el callejón de T-1641 (x -86..-54, z≈-59), sobre el piso, en un hueco
        /// libre, fuera de la vista del punto donde despierta el jugador y con línea recta libre hacia la calle de la casa. Idempotente: borra el anterior.
        /// Escribe Logs/t1641-skulls.txt y la foto docs/capturas/_tareas/T21/callejon_skulls.png.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1641/Colocar callejon SKULLS")]
        public static void ColocarCallejonSkulls()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var sb = new StringBuilder();
            Transform padre = null;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "MarcadorHistoria") { continue; }
                if (mb.gameObject.name == NombreSkulls) { Object.DestroyImmediate(mb.gameObject); continue; }
                if (padre == null) { padre = mb.transform.parent; }
            }
            Physics.SyncTransforms();

            Vector3 elegido = Vector3.zero;
            bool hallado = false;
            float mejor = float.MaxValue;
            for (float x = -60f; x >= -84f; x -= 0.5f)
            {
                for (float dz = -1.5f; dz <= 1.51f; dz += 0.75f)
                {
                    float z = ZCentro + dz;
                    if (!Physics.Raycast(new Vector3(x, 4f, z), Vector3.down, out RaycastHit piso, 6f, ~0, QueryTriggerInteraction.Ignore)) { continue; }
                    if (piso.point.y < -0.5f || piso.point.y > 0.8f) { continue; }
                    Vector3 p = piso.point;
                    // Hueco libre para una persona.
                    if (Physics.CheckCapsule(p + Vector3.up * 0.5f, p + Vector3.up * 1.7f, 0.45f, ~0, QueryTriggerInteraction.Ignore)) { continue; }
                    Vector3 pecho = p + Vector3.up * 1.2f;
                    // Fuera de la vista de donde despierta el jugador.
                    if (!Physics.Linecast(OjoInicio, pecho, ~0, QueryTriggerInteraction.Ignore)) { continue; }
                    if (!Physics.Linecast(OjoInicio, p + Vector3.up * 1.8f, ~0, QueryTriggerInteraction.Ignore)) { continue; }
                    // Línea recta libre hasta la calle de la casa (frente a la boca del callejón).
                    Vector3 calle = new Vector3(-49f, p.y + 1.2f, ZCentro);
                    if (Physics.Linecast(pecho, calle, ~0, QueryTriggerInteraction.Ignore)) { continue; }
                    // Lo más cerca posible de la boca, sin pegarse a ella, y centrado.
                    float costo = Mathf.Abs(x - (-64f)) + Mathf.Abs(dz) * 2f;
                    if (costo < mejor) { mejor = costo; elegido = p; hallado = true; }
                }
            }
            if (!hallado)
            {
                sb.AppendLine("SIN HUECO: ningún punto del callejón cumple (piso, libre, oculto, línea a la calle).");
                File.WriteAllText("Logs/t1641-skulls.txt", sb.ToString());
                Debug.LogError("[Mundo] " + sb);
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSkulls);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            if (padre != null) { go.transform.SetParent(padre, true); }
            // Mira hacia la calle de la casa (+X).
            go.transform.SetPositionAndRotation(elegido, Quaternion.LookRotation(Vector3.right, Vector3.up));
            sb.AppendLine("Historia_CallejonSkulls en " + elegido.ToString("0.00") + " mirando +X, padre=" + (padre != null ? Ruta(padre) : "(raíz)"));
            sb.AppendLine("Oculto desde " + OjoInicio + ": si. Linea libre a (-49, y+1.2, " + ZCentro + "): si. Distancia al inicio: " + Vector3.Distance(OjoInicio, elegido).ToString("0.0") + " m");
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1641-skulls.txt", sb.ToString());
            Debug.Log("[Mundo] " + sb);

            Directory.CreateDirectory("docs/capturas/_tareas/T21");
            var marca = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marca.transform.position = elegido + Vector3.up;
            var mr = marca.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", Color.red);
            mr.sharedMaterial = mat;
            Foto("callejon_skulls", new Vector3(-50f, 1.7f, ZCentro), elegido + Vector3.up, 60f);
            Foto("callejon_skulls_aerea", new Vector3(elegido.x + 6f, 25f, elegido.z - 14f), elegido, 60f);
            Object.DestroyImmediate(marca);
            Object.DestroyImmediate(mat);
        }

        public static string Ruta(Transform t)
        {
            string r = t.name;
            int n = 0;
            for (Transform p = t.parent; p != null && n < 4; p = p.parent, n++)
            {
                r = p.name + "/" + r;
            }
            return r;
        }
    }
}
