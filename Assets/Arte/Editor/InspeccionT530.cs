using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-530 (plan de la noche 3-nov, paso 1): volcado de Ciudad_Prologo para el barrido de assets rotos, flotantes y encimados.
    /// Una línea por objeto: ruta, activo, posición, tamaño de sus renderers (bounds del mundo), altura del piso debajo y componentes.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.InspeccionT530.Volcar
    /// </summary>
    public static class InspeccionT530
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        public static void Volcar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            StringBuilder sb = new StringBuilder();
            foreach (GameObject raiz in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                Recorrer(raiz.transform, raiz.name, sb, 0);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t530-escena.txt", sb.ToString());
            Debug.Log("[T-530] volcado: " + sb.Length + " caracteres");
        }

        private static void Recorrer(Transform t, string ruta, StringBuilder sb, int nivel)
        {
            Vector3 p = t.position;
            string comps = string.Join(",", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
            Renderer r = t.GetComponent<Renderer>();
            string tam = r != null ? " tam=" + V(r.bounds.size) + " min_y=" + r.bounds.min.y.ToString("F2") : "";
            MeshFilter mf = t.GetComponent<MeshFilter>();
            string malla = mf != null && mf.sharedMesh != null ? " malla=" + mf.sharedMesh.name : "";
            string mat = r != null && r.sharedMaterial != null ? " mat=" + r.sharedMaterial.name : "";
            sb.Append(ruta).Append(t.gameObject.activeInHierarchy ? "" : " [OFF]").Append(" pos=").Append(V(p))
                .Append(" esc=").Append(V(t.lossyScale)).Append(tam).Append(malla).Append(mat).Append(" {").Append(comps).Append("}\n");
            if (nivel > 12)
            {
                return;
            }
            foreach (Transform h in t)
            {
                Recorrer(h, ruta + "/" + h.name, sb, nivel + 1);
            }
        }

        /// <summary>Renderers grandes (más de 'minAlto' m de alto y 'minAncho' m de ancho) sin ningún collider sólido que cubra su caja.</summary>
        public static string SinCollider(float minAlto, float minAncho)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            Physics.SyncTransforms();
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Bounds b = r.bounds;
                if (b.size.y < minAlto || Mathf.Max(b.size.x, b.size.z) < minAncho)
                {
                    continue;
                }
                if (Cubierto(b))
                {
                    continue;
                }
                n++;
                sb.Append(Ruta(r.transform)).Append(" tam=").Append(V(b.size)).Append(" centro=").Append(V(b.center)).Append('\n');
            }
            File.WriteAllText("Logs/t530-sincollider.txt", sb.ToString());
            return n + " sin collider";
        }

        /// <summary>
        /// Mallas sólidas que se pueden atravesar: renderers de más de 1.5 m de alto y 0.6 m de grueso cuya caja, a 1 m del piso, tiene
        /// puntos (cada 'paso' m) sin ningún collider sólido cerca (0.25 m). Escribe Logs/t530-atravesables.txt ordenado por puntos libres.
        /// </summary>
        public static string Atravesables(float paso)
        {
            Physics.SyncTransforms();
            System.Collections.Generic.List<(string, int, int, string)> lista = new System.Collections.Generic.List<(string, int, int, string)>();
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Bounds b = r.bounds;
                if (b.size.y < 1.5f || Mathf.Min(b.size.x, b.size.z) < 0.6f || Mathf.Max(b.size.x, b.size.z) > 60f || b.min.y > 0.6f || b.min.y < -1f)
                {
                    continue;
                }
                if (r.GetComponentInParent<UnityEngine.UI.Graphic>() != null || r.GetComponentInParent<ParticleSystem>() != null)
                {
                    continue;
                }
                float y = b.min.y + 1f;
                int libres = 0;
                int total = 0;
                for (float x = b.min.x + paso * 0.5f; x < b.max.x; x += paso)
                {
                    for (float z = b.min.z + paso * 0.5f; z < b.max.z; z += paso)
                    {
                        Vector3 pto = new Vector3(x, y, z);
                        // Solo cuenta si de verdad hay malla ahí: un rayo hacia abajo desde arriba de la caja choca con la malla (con collider temporal no: se usa la caja).
                        total++;
                        if (!Physics.CheckSphere(pto, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            libres++;
                        }
                    }
                }
                if (total > 0 && libres * 2 > total)
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    lista.Add((Ruta(r.transform), libres, total, V(b.size) + " centro=" + V(b.center) + (mf != null && mf.sharedMesh != null ? " malla=" + mf.sharedMesh.name : "")));
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (var e in lista.OrderByDescending(e => e.Item2))
            {
                sb.Append(e.Item1).Append(" libres=").Append(e.Item2).Append('/').Append(e.Item3).Append(" tam=").Append(e.Item4).AppendLine();
            }
            File.WriteAllText("Logs/t530-atravesables.txt", sb.ToString());
            return lista.Count + " atravesables";
        }

        /// <summary>Hay colliders sólidos en la caja: se prueba con 5 puntos de su cara a media altura del jugador (1 m sobre su base).</summary>
        public static bool Cubierto(Bounds b)
        {
            float y = b.min.y + Mathf.Min(1f, b.size.y * 0.5f);
            Vector3 c = new Vector3(b.center.x, y, b.center.z);
            Vector3 ext = new Vector3(b.extents.x * 0.5f, 0.3f, b.extents.z * 0.5f);
            return Physics.CheckBox(c, ext, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        }

        public static string Ruta(Transform t)
        {
            return t.parent == null ? t.name : Ruta(t.parent) + "/" + t.name;
        }

        /// <summary>Foto en perspectiva desde 'desde' mirando a 'hacia' (1600x900), con la luz que tenga la escena; 'dia' agrega un sol plano.</summary>
        public static string Foto(float dx, float dy, float dz, float hx, float hy, float hz, float fov, bool dia, string ruta)
        {
            GameObject go = new GameObject("CamaraT530");
            Camera cam = go.AddComponent<Camera>();
            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData datos = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = fov;
            cam.transform.position = new Vector3(dx, dy, dz);
            cam.transform.LookAt(new Vector3(hx, hy, hz));
            GameObject sol = null;
            if (dia)
            {
                sol = new GameObject("SolT530");
                Light l = sol.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.5f;
                sol.transform.rotation = Quaternion.Euler(55f, 30f, 0f);
            }
            RenderTexture rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            foto.Apply();
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            if (sol != null) { Object.DestroyImmediate(sol); }
            return "guardado " + ruta;
        }

        /// <summary>En Play: pone al jugador en (x, z) mirando hacia (hx, hz) y deja que la cámara del juego lo siga.</summary>
        public static string PonerJugador(float x, float y, float z, float hx, float hz)
        {
            GameObject jugador = GameObject.Find("JugadorAPie");
            if (jugador == null)
            {
                return "sin jugador";
            }
            CharacterController cc = jugador.GetComponent<CharacterController>();
            if (cc != null) { cc.enabled = false; }
            jugador.transform.position = new Vector3(x, y, z);
            jugador.transform.rotation = Quaternion.LookRotation(new Vector3(hx - x, 0f, hz - z));
            if (cc != null) { cc.enabled = true; }
            foreach (MonoBehaviour m in jugador.GetComponentsInChildren<MonoBehaviour>())
            {
                // El giro de la cámara del hombro sigue al cuerpo (ControladorAPie.GiroCamara).
                System.Reflection.PropertyInfo p = m.GetType().GetProperty("GiroCamara");
                System.Reflection.FieldInfo f = m.GetType().GetField("giroCamara", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (p != null && f != null)
                {
                    f.SetValue(m, jugador.transform.eulerAngles.y);
                }
            }
            return "ok";
        }

        /// <summary>En Play: foto con la cámara del juego (Camera.main), 1600x900.</summary>
        public static string FotoJuego(string ruta)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return "sin cámara";
            }
            RenderTexture rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            RenderTexture antes = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = antes;
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            foto.Apply();
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            return "guardado " + ruta;
        }

        /// <summary>Foto ortográfica desde arriba centrada en (cx, cz) que cubre 'lado' m, a 'ruta' (png 2048x2048). Luz de día plana.</summary>
        public static string FotoArriba(float cx, float cz, float lado, string ruta)
        {
            GameObject go = new GameObject("CamaraT530");
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = lado * 0.5f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(cx, 300f, cz);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject sol = new GameObject("SolT530");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 2.5f;
            sol.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
            Color ambiente = RenderSettings.ambientLight;
            UnityEngine.Rendering.AmbientMode modo = RenderSettings.ambientMode;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
            bool niebla = RenderSettings.fog;
            RenderSettings.fog = false;
            GameObject fondo = GameObject.Find("T513_Fondo");
            bool fondoActivo = fondo != null && fondo.activeSelf;
            if (fondo != null) { fondo.SetActive(false); }
            const int Px = 2048;
            RenderTexture rt = new RenderTexture(Px, Px, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(Px, Px, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, Px, Px), 0, 0);
            foto.Apply();
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(sol);
            RenderSettings.ambientMode = modo;
            RenderSettings.ambientLight = ambiente;
            RenderSettings.fog = niebla;
            if (fondo != null) { fondo.SetActive(fondoActivo); }
            return "guardado " + ruta;
        }

        private static string V(Vector3 v)
        {
            return "(" + v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1") + ")";
        }
    }
}
