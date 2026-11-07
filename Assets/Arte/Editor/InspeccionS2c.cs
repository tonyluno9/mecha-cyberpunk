using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov (docs/plan-look-ciudad.md): vuelca a Logs/inspeccion-s2c.txt lo que hay en Ciudad_Prologo (raíces y sus
    /// límites, manzanas del barrio, volúmenes, luces y cámaras) para ubicar la calle de prueba y el mapa nuevo. Solo lee.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.InspeccionS2c.Volcar
    /// </summary>
    public static class InspeccionS2c
    {
        [MenuItem("Mecha Cyberpunk/S2c/Inspeccion (solo lee)")]
        public static void Volcar()
        {
            ColocarS2.Abrir();
            StringBuilder s = new StringBuilder();
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                s.AppendLine("ROOT " + r.name + " act=" + r.activeSelf + " hijos=" + r.transform.childCount + " pos=" + r.transform.position + " " + Limites(r));
                if (r.name == "Barrio" || r.name.StartsWith("GTA_") || r.name.StartsWith("T5") || r.name.StartsWith("S2"))
                {
                    foreach (Transform h in r.transform)
                    {
                        s.AppendLine("   " + h.name + " act=" + h.gameObject.activeSelf + " hijos=" + h.childCount + " " + Limites(h.gameObject));
                    }
                }
            }
            foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string comps = v.sharedProfile == null ? "-" : string.Join(",", v.sharedProfile.components.Select(c => c.GetType().Name + (c.active ? "" : "(off)")));
                s.AppendLine("VOLUME " + ColocarS2.Ruta(v.transform) + " global=" + v.isGlobal + " w=" + v.weight + " prio=" + v.priority + " perfil=" + AssetDatabase.GetAssetPath(v.sharedProfile) + " [" + comps + "]");
            }
            Light[] luces = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            s.AppendLine("LUCES " + luces.Length + " " + string.Join(" ", luces.GroupBy(l => l.type).Select(g => g.Key + "=" + g.Count())));
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                s.AppendLine("CAM " + ColocarS2.Ruta(c.transform) + " fov=" + c.fieldOfView + " far=" + c.farClipPlane + " pos=" + c.transform.position + " tag=" + c.tag);
            }
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(m => m != null && (m.GetType().Name.Contains("Camara") || m.GetType().Name.Contains("CicloDia") || m.GetType().Name.Contains("Mapa"))))
            {
                s.AppendLine("MB " + m.GetType().FullName + " en " + ColocarS2.Ruta(m.transform) + " pos=" + m.transform.position);
            }
            s.AppendLine("RP " + AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline) + " calidad=" + QualitySettings.names[QualitySettings.GetQualityLevel()]
                         + " rpCalidad=" + AssetDatabase.GetAssetPath(QualitySettings.renderPipeline));
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/inspeccion-s2c.txt", s.ToString());
            Debug.Log("[Arte] InspeccionS2c lista");
        }

        /// <summary>
        /// Fotos sueltas sin guardar la escena: -s2cFotos "nombre:px,py,pz:tx,ty,tz[:fov];..." → docs/capturas/nombre.png.
        /// Con -s2cJugador x,z,giro pone al jugador ahí (solo para la foto).
        /// </summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            string[] args = System.Environment.GetCommandLineArgs();
            string lista = Arg(args, "-s2cFotos");
            string jugador = Arg(args, "-s2cJugador");
            if (!string.IsNullOrEmpty(jugador))
            {
                float[] j = jugador.Split(',').Select(float.Parse).ToArray();
                GameObject ja = GameObject.Find("JugadorAPie");
                if (ja != null)
                {
                    ja.transform.SetPositionAndRotation(new Vector3(j[0], 0.25f, j[1]), Quaternion.Euler(0f, j[2], 0f));
                }
            }
            foreach (string foto in (lista ?? "").Split(';').Where(x => x.Length > 0))
            {
                string[] p = foto.Split(':');
                Vector3 desde = Vec(p[1]);
                Vector3 hacia = Vec(p[2]);
                float fov = p.Length > 3 ? float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture) : 55f;
                ColocarS2cLook.Foto(desde, hacia, "docs/capturas/" + p[0] + ".png", fov);
            }
            Debug.Log("[Arte] InspeccionS2c.Fotos listas");
        }

        /// <summary>Medidas de cada prefab de una carpeta y una foto de catálogo en una escena vacía: -s2cCarpeta Assets/Arte/Calle.</summary>
        public static void Catalogo()
        {
            string carpeta = Arg(System.Environment.GetCommandLineArgs(), "-s2cCarpeta") ?? "Assets/Arte/Calle";
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
            StringBuilder s = new StringBuilder();
            float x = 0f;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { carpeta });
            foreach (string g in guids)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                GameObject i = (GameObject)PrefabUtility.InstantiatePrefab(p);
                Renderer[] rs = i.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0)
                {
                    Object.DestroyImmediate(i);
                    continue;
                }
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                s.AppendLine(Path.GetFileNameWithoutExtension(ruta) + " tam=" + b.size.ToString("0.00") + " centro=" + b.center.ToString("0.00") + " mats=" +
                             string.Join(",", rs.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct()));
                i.transform.position = new Vector3(x - b.min.x, 0f, 0f);
                x += b.size.x + 1f;
            }
            File.WriteAllText("Logs/catalogo-s2c.txt", s.ToString());
            RenderSettings.ambientLight = Color.gray;
            int n = Mathf.CeilToInt(x / 25f);
            for (int k = 0; k < n; k++)
            {
                ColocarS2.Foto(new Vector3(k * 25f + 12.5f, 4f, -16f), new Vector3(k * 25f + 12.5f, 2.5f, 0f), "Logs/catalogo-s2c-" + k + ".png", 60f, 200f);
            }
            Debug.Log("[Arte] Catalogo " + guids.Length + " prefabs, " + n + " fotos");
        }

        /// <summary>Todo lo que tiene render dentro de una caja XZ: -s2cCaja xmin,xmax,zmin,zmax → Logs/zona-s2c.txt (agrupado por padre).</summary>
        public static void Zona()
        {
            ColocarS2.Abrir();
            float[] c = Arg(System.Environment.GetCommandLineArgs(), "-s2cCaja").Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            StringBuilder s = new StringBuilder();
            var grupos = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.bounds.center.x >= c[0] && r.bounds.center.x <= c[1] && r.bounds.center.z >= c[2] && r.bounds.center.z <= c[3])
                .GroupBy(r => Nivel(r.transform, 8));
            foreach (var g in grupos.OrderBy(g => g.Key))
            {
                Bounds b = g.First().bounds;
                foreach (Renderer r in g)
                {
                    b.Encapsulate(r.bounds);
                }
                s.AppendLine(g.Key + " n=" + g.Count() + " min=" + V(b.min) + " max=" + V(b.max) + " " + string.Join(",", g.Select(r => r.name).Distinct().Take(6)));
            }
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                         .Where(l => l.transform.position.x >= c[0] && l.transform.position.x <= c[1] && l.transform.position.z >= c[2] && l.transform.position.z <= c[3]))
            {
                s.AppendLine("LUZ " + ColocarS2.Ruta(l.transform) + " " + l.type + " pos=" + V(l.transform.position) + " int=" + l.intensity + " rango=" + l.range + " color=" + l.color);
            }
            File.WriteAllText("Logs/zona-s2c.txt", s.ToString());
            Debug.Log("[Arte] Zona lista");
        }

        /// <summary>Hijos directos de una ruta con sus límites: -s2cRuta Barrio/Manzana_4/Edificios → Logs/hijos-s2c.txt.</summary>
        public static void Hijos()
        {
            ColocarS2.Abrir();
            Transform t = ColocarS2.Buscar(Arg(System.Environment.GetCommandLineArgs(), "-s2cRuta"));
            StringBuilder s = new StringBuilder();
            foreach (Transform h in t)
            {
                s.AppendLine(h.name + " act=" + h.gameObject.activeSelf + " pos=" + V(h.position) + " " + Limites(h.gameObject));
            }
            File.WriteAllText("Logs/hijos-s2c.txt", s.ToString());
        }

        /// <summary>Mapas de profundidad de las piezas de fachada del kit (Logs/profundidad-*.png).</summary>
        public static void Profundidades()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            KitS2c.GuardarProfundidad = true;
            foreach (string c in new[] { "C01_FachadaLocal_cy", "C02_FachadaVivienda_cy", "C03_FachadaPisoAlto_cy", "C20a_PisoVentana_cy", "C20b_PisoDosVentanas_cy" })
            {
                KitS2c.Medir(c);
            }
            File.WriteAllText("Logs/medidas-s2c.txt", ColocarS2.Informe.ToString());
        }

        private static string Nivel(Transform t, int n)
        {
            string ruta = ColocarS2.Ruta(t);
            string[] partes = ruta.Split('/');
            return string.Join("/", partes.Take(Mathf.Min(n, partes.Length)));
        }

        private static string Arg(string[] args, string nombre)
        {
            int i = System.Array.IndexOf(args, nombre);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static Vector3 Vec(string s)
        {
            float[] v = s.Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(v[0], v[1], v[2]);
        }

        private static string Limites(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 0)
            {
                return "sin render";
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return "min=" + V(b.min) + " max=" + V(b.max) + " r=" + rs.Length;
        }

        private static string V(Vector3 v)
        {
            return "(" + v.x.ToString("0") + "," + v.y.ToString("0.00") + "," + v.z.ToString("0") + ")";
        }
    }
}
