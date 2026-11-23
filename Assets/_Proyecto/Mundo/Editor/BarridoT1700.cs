using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1700 (T69): barrido de flotantes a cualquier altura y de primitivos de Unity en Ciudad_Prologo.
    /// Unidad = el ancestro más alto de un MeshRenderer cuyo conjunto mide como mucho <see cref="LadoUnidad"/> m de lado (así un tinaco
    /// sobre una azotea se evalúa solo y no como parte del edificio). Flotante = su base no toca nada en <see cref="TolerarHueco"/> m hacia
    /// abajo: se prueban 5 puntos de la base con raycast contra colliders (sin los propios) y, si no hay collider, contra los bounds de
    /// los demás renderers. Se excluyen cosas que deben flotar (nombres, luces, partículas, vehículos, cuerpos dinámicos) y lo pegado a
    /// una pared (raycast horizontal corto hacia 4 lados o bounds de otro renderer pegados).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.BarridoT1700.Listar (lista-antes) / Aplicar (arregla y lista-despues) /
    /// ListarDespues (solo lista-despues).
    /// </summary>
    public static class BarridoT1700
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string CarpetaSalida = "docs/capturas/_tareas/T69/";
        public const float TolerarHueco = 0.3f;
        public const float LadoUnidad = 4f;
        public const float AltoUnidad = 6f;
        private const float DistanciaPared = 0.35f;

        private static readonly string[] IgnoradosExtra =
        {
            "ave", "pajaro", "pájaro", "avion", "avión", "aeronave", "holograma", "particula", "partícula", "trafico", "tráfico", "carro",
            "moto", "vehiculo", "vehículo", "patrulla", "paloma", "bandera", "globo", "grua", "grúa", "colgante", "cuerda", "hilo", "alambre",
            "cadena", "puente_", "tren", "camion", "camión", "bus", "helicop", "zeppelin", "dirigible", "balcon", "balcón", "poste", "wire",
            "hover", "volante", "dron", "banner", "lampar", "foco", "reflector", "tubo", "tuberia", "tubería", "escalera", "ladder",
            "cornisa", "manta", "franja", "puerta", "pandillero", "bando", "farol", "tablero", "medidores", "fondo", "huacales", "acoso"
        };

        public static readonly List<string> Descartes = new List<string>();
        public static int NUnidades, NIgn, NPlano, NNombrePiso, NSoportadas, NPared;

        public class Flotante
        {
            public Transform Unidad;
            public Bounds Caja;
            public float Hueco;
            public string Clase;
            public string Accion = "";
            public string Detalle = "";
        }

        public class Primitivo
        {
            public Renderer Renderer;
            public string Malla;
            public string Ruta;
            public Vector3 Pos;
            public string Motivo;
            /// <summary>Null = se apaga; si no, la razón justificada de dejarlo.</summary>
            public string Excepcion;
            public string Accion = "";
        }

        // ---- puntos de entrada ----

        public static void Listar()
        {
            Scene escena = Abrir();
            Escribir("lista-antes.txt", escena, Flotantes(escena), Primitivos(escena), null);
        }

        public static void ListarDespues()
        {
            Scene escena = Abrir();
            Escribir("lista-despues.txt", escena, Flotantes(escena), Primitivos(escena), null);
        }

        public static void Nombres()
        {
            Scene escena = Abrir();
            Physics.SyncTransforms();
            List<MeshRenderer> todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)).Where(r => r.enabled).ToList();
            Bounds[] cajas = todos.Select(r => r.bounds).ToArray();
            StringBuilder sb = new StringBuilder();
            foreach (MeshRenderer r in todos)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                string mesh = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "";
                string mat = r.sharedMaterial != null ? r.sharedMaterial.name : "";
                string txt = (Ruta(r.transform) + " " + mesh + " " + mat).ToLowerInvariant();
                if (System.Text.RegularExpressions.Regex.IsMatch(txt, "tinaco|tanque|cisterna|antena|parabol|aire|_ac|clima"))
                {
                    Bounds b = r.bounds;
                    float h = HuecoPunto(new Vector3(b.center.x, b.min.y, b.center.z), r.transform, cajas, todos, out string sop);
                    sb.AppendLine(b.center.ToString("F1") + "	" + b.size.ToString("F1") + "	hueco=" + h.ToString("F2") + "	" + mesh + "	" + mat + "	" + Ruta(r.transform) + "	soporte=" + sop);
                }
            }
            File.WriteAllText(Salida("nombres.txt"), sb.ToString());
        }

        public static void Aplicar()
        {
            Scene escena = Abrir();
            Physics.SyncTransforms();
            StringBuilder acciones = new StringBuilder();
            List<Flotante> fl = Flotantes(escena);
            foreach (Flotante f in fl)
            {
                ArreglarFlotante(f);
                acciones.AppendLine("FLOTANTE\t" + f.Clase + "\t" + f.Accion + "\t" + Ruta(f.Unidad));
            }
            Physics.SyncTransforms();
            List<Primitivo> pr = Primitivos(escena);
            foreach (Primitivo p in pr)
            {
                if (p.Renderer == null || p.Excepcion != null)
                {
                    continue;
                }
                Undo.RecordObject(p.Renderer.gameObject, "T-1700");
                p.Renderer.gameObject.SetActive(false);
                EditorUtility.SetDirty(p.Renderer.gameObject);
                p.Accion = "apagado";
                acciones.AppendLine("PRIMITIVO\t" + p.Malla + "\tapagado\t" + p.Ruta);
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Physics.SyncTransforms();
            File.WriteAllText(Salida("acciones.txt"), acciones.ToString());
            Escribir("lista-despues.txt", escena, Flotantes(escena), Primitivos(escena), null);
            Debug.Log("[T-1700] Aplicar: " + fl.Count + " flotantes tratados, " + pr.Count + " primitivos apagados");
        }

        private static Scene Abrir()
        {
            Scene activa = SceneManager.GetActiveScene();
            if (activa.path == RutaEscena && activa.isLoaded)
            {
                return activa;
            }
            return EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        }

        private static string Salida(string archivo)
        {
            Directory.CreateDirectory(CarpetaSalida);
            return CarpetaSalida + archivo;
        }

        // ---- flotantes ----

        private static bool Ignorado(Transform t)
        {
            if (BarridoCiudadT1501.EsIgnorado(t))
            {
                return true;
            }
            for (Transform a = t; a != null; a = a.parent)
            {
                string n = a.name.ToLowerInvariant();
                foreach (string frag in IgnoradosExtra)
                {
                    if (frag.Length <= 3 ? (n == frag || n.StartsWith(frag + "_") || n.EndsWith("_" + frag)) : n.Contains(frag))
                    {
                        return true;
                    }
                }
                if (a.Find("Rueda_DI") != null || a.GetComponent<Light>() != null || a.GetComponent<ParticleSystem>() != null)
                {
                    return true;
                }
                Rigidbody rb = a.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<Flotante> Flotantes(Scene escena)
        {
            Physics.SyncTransforms();
            Descartes.Clear();
            NUnidades = NIgn = NPlano = NNombrePiso = NSoportadas = NPared = 0;
            List<MeshRenderer> todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false))
                .Where(r => r.enabled && r.sharedMaterials.Length > 0 && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null).ToList();
            Bounds[] cajas = todos.Select(r => r.bounds).ToArray();
            Dictionary<Transform, Bounds?> cache = new Dictionary<Transform, Bounds?>();
            HashSet<Transform> unidades = new HashSet<Transform>();
            foreach (MeshRenderer r in todos)
            {
                Transform t = r.transform;
                while (t.parent != null)
                {
                    Bounds? bp = Limites(t.parent, cache);
                    if (!bp.HasValue || bp.Value.size.x > LadoUnidad || bp.Value.size.z > LadoUnidad || bp.Value.size.y > AltoUnidad)
                    {
                        break;
                    }
                    t = t.parent;
                }
                unidades.Add(t);
            }
            // Si una unidad es hija de otra unidad ya incluida, solo cuenta la más alta.
            List<Flotante> res = new List<Flotante>();
            foreach (Transform u in unidades)
            {
                if (u == null)
                {
                    continue;
                }
                NUnidades++;
                bool interesa = Clase(u) != "otro";
                if (Ignorado(u))
                {
                    if (interesa)
                    {
                        Descartes.Add("IGNORADO por nombre/componente	" + Ruta(u));
                    }
                    NIgn++;
                    continue;
                }
                Bounds? bn = Limites(u, cache);
                if (!bn.HasValue)
                {
                    continue;
                }
                Bounds b = bn.Value;
                if (b.size.y < 0.15f && Mathf.Max(b.size.x, b.size.z) > 1.5f)
                {
                    continue; // láminas planas: marcas, charcos, calcomanías
                }
                string nombreAll = u.name.ToLowerInvariant();
                if (System.Text.RegularExpressions.Regex.IsMatch(nombreAll, "piso|calle|asfalto|banqueta|acera|losa|suelo|pasto|terreno|techo|azotea"))
                {
                    continue;
                }
                float hueco;
                if (interesa)
                {
                    // Tinacos, aires, antenas y cajas: manda la malla real (los bounds de un modelo grande dan falsos soportes).
                    hueco = HuecoPreciso(u, b, cajas, todos);
                }
                else
                {
                    hueco = HuecoMinimo(u, b, cajas, todos);
                }
                string detalle = interesa ? UltimoDetalle : "";
                if (hueco <= TolerarHueco)
                {
                    NSoportadas++;
                    continue;
                }
                if (PegadoAPared(u, b, cajas, todos))
                {
                    if (interesa)
                    {
                        Descartes.Add("PEGADO a pared hueco=" + hueco.ToString("F2") + "	" + Ruta(u));
                    }
                    NPared++;
                    continue;
                }
                res.Add(new Flotante { Unidad = u, Caja = b, Hueco = hueco, Clase = Clase(u), Detalle = detalle });
            }
            LimpiarTemporales();
            return res.OrderBy(f => f.Clase).ThenBy(f => Ruta(f.Unidad)).ToList();
        }

        private static readonly Dictionary<MeshRenderer, MeshCollider> temporales = new Dictionary<MeshRenderer, MeshCollider>();

        private static string UltimoDetalle = "";

        private static void LimpiarTemporales()
        {
            foreach (MeshCollider mc in temporales.Values)
            {
                if (mc != null)
                {
                    UnityEngine.Object.DestroyImmediate(mc.gameObject);
                }
            }
            temporales.Clear();
        }

        /// <summary>Como <see cref="HuecoMinimo"/> pero contra la malla real de los renderers de abajo (collider temporal por cada uno), no sus bounds.</summary>
        private static float HuecoPreciso(Transform u, Bounds b, Bounds[] cajas, List<MeshRenderer> renderers)
        {
            float min = float.PositiveInfinity;
            UltimoDetalle = "";
            foreach (Vector3 p in Puntos(b))
            {
                float antes = min;
                for (int i = 0; i < cajas.Length; i++)
                {
                    Bounds c = cajas[i];
                    if (p.x < c.min.x || p.x > c.max.x || p.z < c.min.z || p.z > c.max.z || c.min.y > p.y + 1f)
                    {
                        continue;
                    }
                    MeshRenderer r = renderers[i];
                    if (r.transform == u || r.transform.IsChildOf(u) || temporales.ContainsKey(r))
                    {
                        continue;
                    }
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    GameObject go = new GameObject("T1700_Temp") { hideFlags = HideFlags.HideAndDontSave };
                    go.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
                    go.transform.localScale = r.transform.lossyScale;
                    MeshCollider mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    temporales[r] = mc;
                }
                Physics.SyncTransforms();
                RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 1f, Vector3.down, 300f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit h in hits)
                {
                    if (h.collider.gameObject.name != "T1700_Temp")
                    {
                        continue;
                    }
                    min = Mathf.Min(min, p.y - h.point.y);
                    UltimoDetalle += (p.y - h.point.y).ToString("F2") + "@" + Ruta(h.collider.transform).Length + " ";
                    break;
                }
            }
            return min;
        }

        private static string Clase(Transform u)
        {
            string n = u.name.ToLowerInvariant();
            foreach (MeshRenderer r in u.GetComponentsInChildren<MeshRenderer>(false))
            {
                n += " " + r.name.ToLowerInvariant() + " " + (r.sharedMaterial != null ? r.sharedMaterial.name.ToLowerInvariant() : "");
            }
            if (n.Contains("tinaco") || n.Contains("cisterna"))
            {
                return "tinaco";
            }
            if (n.Contains("antena") || n.Contains("parabol"))
            {
                return "antena";
            }
            if (n.Contains("aire") || n.Contains("clima") || n.Contains("ac_") || n.Contains("hvac") || n.Contains("ventilador"))
            {
                return "aire";
            }
            if (n.Contains("caja") || n.Contains("crate") || n.Contains("box") || n.Contains("contenedor") || n.Contains("barril") || n.Contains("tambo"))
            {
                return "caja";
            }
            return "otro";
        }

        private static Bounds? Limites(Transform t, Dictionary<Transform, Bounds?> cache)
        {
            if (cache.TryGetValue(t, out Bounds? ya))
            {
                return ya;
            }
            Bounds? total = null;
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!r.enabled || (r.transform != t && Ignorado(r.transform)))
                {
                    continue;
                }
                if (total.HasValue)
                {
                    Bounds x = total.Value;
                    x.Encapsulate(r.bounds);
                    total = x;
                }
                else
                {
                    total = r.bounds;
                }
            }
            cache[t] = total;
            return total;
        }

        private static Vector3[] Puntos(Bounds b)
        {
            float ix = b.extents.x * 0.8f;
            float iz = b.extents.z * 0.8f;
            Vector3 c = b.center;
            float y = b.min.y;
            return new[]
            {
                new Vector3(c.x, y, c.z), new Vector3(c.x + ix, y, c.z + iz), new Vector3(c.x - ix, y, c.z + iz),
                new Vector3(c.x + ix, y, c.z - iz), new Vector3(c.x - ix, y, c.z - iz)
            };
        }

        /// <summary>Distancia mínima de la base de la unidad a lo que tiene debajo (collider o bounds de otro renderer); infinito si no hay nada.</summary>
        private static float HuecoMinimo(Transform u, Bounds b, Bounds[] cajas, List<MeshRenderer> renderers)
        {
            float min = float.PositiveInfinity;
            foreach (Vector3 p in Puntos(b))
            {
                float g = HuecoPunto(p, u, cajas, renderers, out _);
                min = Mathf.Min(min, g);
            }
            return min;
        }

        public static float HuecoPunto(Vector3 p, Transform unidad, Bounds[] cajas, List<MeshRenderer> renderers, out string soporte)
        {
            soporte = "";
            float mejor = float.PositiveInfinity;
            RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 0.15f, Vector3.down, 300f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit h in hits)
            {
                if (h.collider.transform == unidad || h.collider.transform.IsChildOf(unidad))
                {
                    continue;
                }
                // Solo cuenta un collider que tiene un renderer visible (ajeno a la unidad) justo ahí; un collider invisible no sostiene nada.
                bool visible = false;
                for (int i = 0; i < cajas.Length && !visible; i++)
                {
                    Bounds c = cajas[i];
                    c.Expand(0.3f);
                    visible = c.Contains(h.point) && !(renderers[i].transform == unidad || renderers[i].transform.IsChildOf(unidad));
                }
                if (!visible)
                {
                    continue;
                }
                mejor = p.y - h.point.y;
                soporte = Ruta(h.collider.transform);
                break;
            }
            // Renderers sin collider: la cara de arriba de sus bounds que queda bajo el punto.
            for (int i = 0; i < cajas.Length; i++)
            {
                Bounds c = cajas[i];
                if (p.x < c.min.x || p.x > c.max.x || p.z < c.min.z || p.z > c.max.z || c.max.y > p.y + 0.15f)
                {
                    continue;
                }
                if (renderers[i].transform == unidad || renderers[i].transform.IsChildOf(unidad) || c.size.y < 0.02f)
                {
                    continue;
                }
                float g = p.y - c.max.y;
                if (g < mejor)
                {
                    mejor = g;
                    soporte = Ruta(renderers[i].transform);
                }
            }
            return mejor;
        }

        private static bool PegadoAPared(Transform u, Bounds b, Bounds[] cajas, List<MeshRenderer> renderers)
        {
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (Vector3 d in dirs)
            {
                float largo = (Mathf.Abs(d.x) > 0 ? b.extents.x : b.extents.z) + DistanciaPared;
                foreach (float fy in new[] { 0.5f, 0.85f })
                {
                    Vector3 o = new Vector3(b.center.x, b.min.y + b.size.y * fy, b.center.z);
                    foreach (RaycastHit h in Physics.RaycastAll(o, d, largo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (!(h.collider.transform == u || h.collider.transform.IsChildOf(u)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void ArreglarFlotante(Flotante f)
        {
            Transform u = f.Unidad;
            Bounds b = f.Caja;
            bool pequeno = Mathf.Max(b.size.x, b.size.z) <= LadoUnidad && b.size.y <= AltoUnidad;
            if (!pequeno)
            {
                f.Accion = "revisar (grande)";
                return;
            }
            if (f.Clase != "otro")
            {
                if (float.IsInfinity(f.Hueco) || f.Hueco > 25f)
                {
                    Undo.RecordObject(u.gameObject, "T-1700");
                    u.gameObject.SetActive(false);
                    EditorUtility.SetDirty(u.gameObject);
                    f.Accion = "apagado (sin superficie debajo)";
                    return;
                }
                Undo.RecordObject(u, "T-1700");
                u.position += Vector3.down * f.Hueco;
                EditorUtility.SetDirty(u);
                f.Accion = "bajado " + f.Hueco.ToString("F2") + " m (malla real)";
                return;
            }
            List<MeshRenderer> todos = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)).Where(r => r.enabled).ToList();
            Bounds[] cajas = todos.Select(r => r.bounds).ToArray();
            float min = float.PositiveInfinity;
            string soporte = "";
            foreach (Vector3 p in Puntos(b))
            {
                float g = HuecoPunto(p, u, cajas, todos, out string s);
                if (g < min)
                {
                    min = g;
                    soporte = s;
                }
            }
            if (float.IsInfinity(min) || min > 1.5f)
            {
                f.Accion = "revisar (hueco " + min.ToString("F1") + " m, no se mueve solo)";
                return;
            }
            Undo.RecordObject(u, "T-1700");
            u.position += Vector3.down * min;
            EditorUtility.SetDirty(u);
            f.Accion = "bajado " + min.ToString("F2") + " m sobre " + soporte;
        }

        // ---- primitivos ----

        private static bool Incorporada(string ruta)
        {
            return !string.IsNullOrEmpty(ruta) && (ruta.StartsWith("Library/") || ruta.Contains("unity default resources") || ruta.Contains("unity_builtin_extra"));
        }

        private static string Excepcion(MeshRenderer mr, string ruta, string mat)
        {
            string n = ruta + " " + mat.ToLowerInvariant();
            if (System.Text.RegularExpressions.Regex.IsMatch(n, "cristal|persiana|cielo|sky|vidrio|parabrisas|ventanilla|domo|nube"))
            {
                return "cristal, cielo o domo (visual a conservar)";
            }
            if (System.Text.RegularExpressions.Regex.IsMatch(n, "linea|charco|cebra|grieta|marca|raya"))
            {
                return "calcomania plana de calle";
            }
            if (System.Text.RegularExpressions.Regex.IsMatch(n, "neon|foco|luz|lampara|pantalla|led|aviso|cable|serie|semaforo|emis"))
            {
                return "luz o pieza emisiva";
            }
            if (System.Text.RegularExpressions.Regex.IsMatch(n, "edificio|fachada|puente|interior|revestimiento|muro|piso|techo|puerta|lugar_|tragamonedas|casino|cuerpojugador|barandal|acero|relleno|cornisa|asfalto|ventana"))
            {
                return "estructura/arquitectura";
            }
            Material m = mr.sharedMaterial;
            if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
            {
                return "tiene textura propia";
            }
            return null;
        }

        public static List<Primitivo> Primitivos(Scene escena)
        {
            List<Primitivo> res = new List<Primitivo>();
            foreach (MeshRenderer mr in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)))
            {
                if (!mr.enabled)
                {
                    continue;
                }
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }
                string ruta = AssetDatabase.GetAssetPath(mf.sharedMesh);
                string nm = mf.sharedMesh.name;
                bool incorporada = Incorporada(ruta) && (nm == "Cube" || nm == "Plane" || nm == "Quad" || nm == "Cylinder" || nm == "Sphere" || nm == "Capsule");
                if (!incorporada)
                {
                    continue;
                }
                string r = Ruta(mr.transform);
                string rl = r.ToLowerInvariant();
                string mat = mr.sharedMaterial != null ? mr.sharedMaterial.name : "";
                if (mat.Contains("Banqueta") || rl.Contains("banqueta") || rl.Contains("acera") || rl.Contains("s2c_"))
                {
                    continue;
                }
                res.Add(new Primitivo { Renderer = mr, Malla = nm, Ruta = r, Pos = mr.bounds.center, Motivo = mat, Excepcion = Excepcion(mr, rl, mat) });
            }
            return res;
        }

        // ---- salida ----

        public static string Ruta(Transform t)
        {
            string s = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                s = p.name + "/" + s;
            }
            return s;
        }

        private static void Escribir(string archivo, Scene escena, List<Flotante> fl, List<Primitivo> pr, string extra)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1700 " + archivo + " de " + RutaEscena);
            sb.AppendLine("Flotantes (cualquier altura): " + fl.Count + " | " + string.Join(", ", fl.GroupBy(f => f.Clase).Select(g => g.Key + "=" + g.Count())));
            sb.AppendLine("Unidades=" + NUnidades + " ignoradas=" + NIgn + " soportadas=" + NSoportadas + " pegadas a pared=" + NPared);
            sb.AppendLine("Primitivos visibles: " + pr.Count + " | " + string.Join(", ", pr.GroupBy(p => p.Malla).Select(g => g.Key + "=" + g.Count())));
            sb.AppendLine();
            sb.AppendLine("== FLOTANTES (clase, hueco m, posición, tamaño, ruta) ==");
            foreach (Flotante f in fl)
            {
                sb.AppendLine(f.Clase + "\t" + f.Hueco.ToString("F2") + "\t" + f.Caja.center.ToString("F1") + "\t" + f.Caja.size.ToString("F1") + "\t" + Ruta(f.Unidad) + "	" + f.Detalle);
            }
            sb.AppendLine();
            sb.AppendLine("Sin excepcion (a apagar): " + pr.Count(p => p.Excepcion == null) + " | con excepcion: " + string.Join(", ", pr.Where(p => p.Excepcion != null).GroupBy(p => p.Excepcion).Select(g => g.Key + "=" + g.Count())));
            sb.AppendLine("== PRIMITIVOS (malla, posicion, material, excepcion, ruta) ==");
            foreach (Primitivo p in pr.OrderBy(q => q.Excepcion != null))
            {
                sb.AppendLine(p.Malla + "	" + p.Pos.ToString("F1") + "	" + p.Motivo + "	" + (p.Excepcion ?? "APAGAR") + "	" + p.Ruta);
            }
            sb.AppendLine();
            sb.AppendLine("== DESCARTADOS tinaco/antena/aire/caja (motivo, ruta) ==");
            foreach (string d in Descartes)
            {
                sb.AppendLine(d);
            }
            File.WriteAllText(Salida(archivo), sb.ToString());
            Debug.Log("[T-1700] " + archivo + ": " + fl.Count + " flotantes, " + pr.Count + " primitivos");
        }
    }
}
