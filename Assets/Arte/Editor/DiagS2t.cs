using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>Diagnósticos de la sesión 2 del 6-nov tarde (solo leen la escena). Salida en Logs/s2t-diag-*.txt.</summary>
    public static class DiagS2t
    {
        private static string Ruta(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }

        private static string Fuente(GameObject g)
        {
            GameObject f = PrefabUtility.GetCorrespondingObjectFromOriginalSource(g);
            return f != null ? AssetDatabase.GetAssetPath(f) : "-";
        }

        /// <summary>Todas las luces de la ciudad y los postes (por nombre o por prefab de origen).</summary>
        public static void Postes()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            Transform[] todos = ColocarS2.Todos().ToArray();
            sb.AppendLine("== Luces (" + todos.Count(t => t.GetComponent<Light>() != null) + ")");
            foreach (var g in todos.Where(t => t.GetComponent<Light>() != null).GroupBy(t => t.parent != null ? Ruta(t.parent).Split('/').Take(3).Aggregate((a, b) => a + "/" + b) : "-"))
            {
                Light l = g.First().GetComponent<Light>();
                sb.AppendLine("  " + g.Count() + "x " + g.Key + "  (" + l.type + " int " + l.intensity + " rango " + l.range + " ej " + g.First().name + ")");
            }
            sb.AppendLine("== Postes por nombre");
            foreach (var g in todos.Where(t => Regex(t.name)).GroupBy(t => Fuente(t.gameObject) + " | " + t.name.Split('_', ' ', '(')[0]))
            {
                sb.AppendLine("  " + g.Count() + "x " + g.Key + "  ej " + Ruta(g.First()));
            }
            sb.AppendLine("== Mallas de postes");
            foreach (var g in todos.Select(t => t.GetComponent<MeshFilter>()).Where(m => m != null && m.sharedMesh != null && Regex(m.sharedMesh.name + " " + AssetDatabase.GetAssetPath(m.sharedMesh)))
                .GroupBy(m => AssetDatabase.GetAssetPath(m.sharedMesh) + " :: " + m.sharedMesh.name))
            {
                sb.AppendLine("  " + g.Count() + "x " + g.Key + "  ej " + Ruta(g.First().transform) + " luz hija: " + (g.First().transform.root.GetComponentsInChildren<Light>().Length));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/s2t-diag-postes.txt", sb.ToString());
        }

        /// <summary>Primitivos de Unity (cubo, plano, cilindro...) que se ven en la ciudad, agrupados por los 3 primeros niveles de su ruta.</summary>
        public static void Primitivos()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            var lista = ColocarS2.Todos()
                .Select(t => (t, mf: t.GetComponent<MeshFilter>(), r: t.GetComponent<MeshRenderer>()))
                .Where(x => x.mf != null && x.r != null && x.r.enabled && x.t.gameObject.activeInHierarchy && x.mf.sharedMesh != null
                    && AssetDatabase.GetAssetPath(x.mf.sharedMesh).StartsWith("Library/unity default"))
                .ToArray();
            sb.AppendLine("== Primitivos visibles: " + lista.Length);
            foreach (var g in lista.GroupBy(x => string.Join("/", Ruta(x.t).Split('/').Take(3))).OrderByDescending(g => g.Count()))
            {
                var e = g.First();
                sb.AppendLine("  " + g.Count() + "x " + g.Key + "  (" + e.mf.sharedMesh.name + ", mat " + (e.r.sharedMaterial != null ? e.r.sharedMaterial.name : "-")
                    + ", ej " + e.t.name + " en " + e.t.position.ToString("F0") + ")");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/s2t-diag-primitivos.txt", sb.ToString());
        }

        /// <summary>Objetos con malla visibles dentro de un radio (en planta) de un punto, agrupados por su raíz con prefab de origen.</summary>
        public static string Cerca(Vector3 centro, float radio, float yMax = 30f)
        {
            StringBuilder sb = new StringBuilder();
            var lista = ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && t.GetComponent<Renderer>() != null && t.GetComponent<Renderer>().enabled)
                .Select(t => (t, b: t.GetComponent<Renderer>().bounds))
                .Where(x => new Vector2(x.b.center.x - centro.x, x.b.center.z - centro.z).magnitude < radio && x.b.center.y < yMax)
                .ToArray();
            foreach (var x in lista.OrderBy(x => Ruta(x.t)))
            {
                Transform raizPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(x.t.gameObject)?.transform ?? x.t;
                sb.AppendLine("  " + Ruta(x.t) + "  c" + x.b.center.ToString("F1") + " s" + x.b.size.ToString("F1") + "  " + Fuente(raizPrefab.gameObject));
            }
            return sb.ToString();
        }

        public static void Casa()
        {
            ColocarS2.Abrir();
            File.WriteAllText("Logs/s2t-diag-casa.txt", "== Fuera de la casa (frente x -57, z -84..-70)\n" + Cerca(new Vector3(-52f, 0f, -77f), 9f, 12f));
        }

        /// <summary>Hijos (con su posición y si están activos) de unas rutas de la escena, y lo que tiene renderer en el cuarto.</summary>
        public static void Cuarto()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (string r in new[] { "Barrio/Manzana_4/Lugar_Casa/Interior", "GTA_T426/TableroEncargos", "T550_Cuartos/Cuarto_Habitacion", "S2_Casa" })
            {
                Transform t = ColocarS2.Buscar(r);
                sb.AppendLine("== " + r + (t == null ? " FALTA" : ""));
                if (t == null) continue;
                foreach (Transform h in t.GetComponentsInChildren<Transform>(true).Where(h => h.parent == t || h.parent?.parent == t))
                {
                    Renderer rd = h.GetComponent<Renderer>();
                    sb.AppendLine("  " + Ruta(h).Substring(r.Length) + (h.gameObject.activeInHierarchy ? "" : " [apagado]") + " p" + h.position.ToString("F2")
                        + (rd != null ? " r:" + rd.enabled + " b" + rd.bounds.size.ToString("F2") + " mat " + (rd.sharedMaterial != null ? rd.sharedMaterial.name : "-") : "")
                        + " " + string.Join(",", h.GetComponents<MonoBehaviour>().Where(m => m != null).Select(m => m.GetType().Name)));
                }
            }
            File.WriteAllText("Logs/s2t-diag-cuarto.txt", sb.ToString());
        }

        /// <summary>Lo que se ve dentro de una caja del mundo (para buscar superficies encimadas).</summary>
        public static string EnCaja(Bounds caja)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.enabled && r.bounds.Intersects(caja)))
            {
                sb.AppendLine("  " + Ruta(r.transform) + " c" + r.bounds.center.ToString("F3") + " s" + r.bounds.size.ToString("F3") + " mat " + string.Join(",", r.sharedMaterials.Where(m => m != null).Select(m => m.name)));
            }
            return sb.ToString();
        }

        public static void PisoCuarto()
        {
            ColocarS2.Abrir();
            Bounds caja = new Bounds(new Vector3(-63f, 0.21f, -73.5f), new Vector3(2.8f, 0.06f, 3.8f));
            File.WriteAllText("Logs/s2t-diag-piso.txt", EnCaja(caja));
        }

        public static void Cables()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (Transform t in ColocarS2.Todos().Where(t => t.name.StartsWith("CY03") && t.gameObject.activeInHierarchy && new Vector2(t.position.x + 52f, t.position.z + 77f).magnitude < 25f))
            {
                sb.AppendLine(Ruta(t) + " p" + t.position.ToString("F1"));
            }
            File.WriteAllText("Logs/s2t-diag-cables.txt", sb.ToString());
        }

        public static void Espejo()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(m => m != null && m.GetType().Name == "EspejoJugador"))
            {
                sb.AppendLine("EspejoJugador: " + Ruta(m.transform) + " activo " + m.gameObject.activeInHierarchy + " p" + m.transform.position.ToString("F2") + " r" + m.transform.eulerAngles.ToString("F0"));
                foreach (Renderer r in m.GetComponentsInChildren<Renderer>(true))
                {
                    sb.AppendLine("   " + Ruta(r.transform) + " en " + r.enabled + " c" + r.bounds.center.ToString("F2") + " s" + r.bounds.size.ToString("F2") + " " + (r.sharedMaterial != null ? r.sharedMaterial.name : "-"));
                }
            }
            foreach (Transform t in ColocarS2.Todos().Where(t => t.name.Contains("H03") || t.name.Contains("Espejo")))
            {
                Renderer r = t.GetComponentInChildren<Renderer>();
                sb.AppendLine(Ruta(t) + " activo " + t.gameObject.activeInHierarchy + " p" + t.position.ToString("F2") + " r" + t.eulerAngles.ToString("F0") + (r != null ? " c" + r.bounds.center.ToString("F2") + " s" + r.bounds.size.ToString("F2") : ""));
            }
            File.WriteAllText("Logs/s2t-diag-espejo.txt", sb.ToString());
        }

        public static void LucesPostes()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (Transform p in PostesS2t.Postes().Where(p => p.name.StartsWith("N09")).Take(3).Concat(PostesS2t.Postes().Take(2)))
            {
                MeshFilter mf = PostesS2t.ModeloVisible(p);
                var cab = PostesS2t.Cabezas(mf.sharedMesh, mf.transform.localToWorldMatrix);
                sb.AppendLine(Ruta(p) + " modelo " + AssetDatabase.GetAssetPath(mf.sharedMesh) + " cabezas " + string.Join(" ", cab.Select(c => c.ToString("F2"))));
                foreach (Light l in p.GetComponentsInChildren<Light>(true))
                {
                    var d = l.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                    sb.AppendLine("   luz " + l.name + " en " + l.enabled + "/" + l.gameObject.activeInHierarchy + " " + l.type + " p" + l.transform.position.ToString("F2") + " r" + l.transform.eulerAngles.ToString("F0")
                        + " int " + l.intensity + " rango " + l.range + " ang " + l.spotAngle + " sombras " + l.shadows + " modo " + l.lightmapBakeType + " capas " + (d != null ? ((uint)d.renderingLayers).ToString("X") : "-")
                        + " culling " + l.cullingMask + " comps " + string.Join(",", l.GetComponents<MonoBehaviour>().Select(m => m.GetType().Name)));
                }
            }
            File.WriteAllText("Logs/s2t-diag-lucespostes.txt", sb.ToString());
        }

        public static void N09()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (Transform t in ColocarS2.Todos().Where(t => t.name.StartsWith("N09")))
            {
                MeshFilter mf = t.GetComponentInChildren<MeshFilter>(true);
                Renderer r = mf != null ? mf.GetComponent<Renderer>() : null;
                sb.AppendLine(Ruta(t) + " activo " + t.gameObject.activeInHierarchy + " malla " + (mf != null && mf.sharedMesh != null ? AssetDatabase.GetAssetPath(mf.sharedMesh) : "-") + " render " + (r != null && r.enabled));
            }
            File.WriteAllText("Logs/s2t-diag-n09.txt", sb.ToString());
        }

        public static void Todo()
        {
            Postes();
            Primitivos();
            Casa();
        }

        private static bool Regex(string s)
        {
            string n = s.ToLowerInvariant();
            return n.Contains("poste") || n.Contains("farol") || n.Contains("c07") || n.Contains("n09") || n.Contains("lampara") || n.Contains("luminaria");
        }
    }
}
