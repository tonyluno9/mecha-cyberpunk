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

        public static void Taller()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (string r in new[] { "GTA_T417/Apoyo_CANEPA", "S2n_TallerMecanico/Fachada", "GTA_T417/Apoyo_TINO" })
            {
                Transform t = ColocarS2.Buscar(r);
                sb.AppendLine("== " + r + (t == null ? " FALTA" : " p" + t.position.ToString("F2") + " r" + t.eulerAngles.ToString("F0")));
                if (t == null) continue;
                foreach (Transform h in t.GetComponentsInChildren<Transform>(true).Take(40))
                {
                    Renderer rd = h.GetComponent<Renderer>();
                    sb.AppendLine("  " + Ruta(h).Substring(r.Length) + (h.gameObject.activeInHierarchy ? "" : " [apagado]") + " p" + h.position.ToString("F2") + " s" + h.lossyScale.ToString("F2")
                        + (rd != null ? " b" + rd.bounds.size.ToString("F2") : "") + " " + Fuente(h.gameObject) + " " + string.Join(",", h.GetComponents<MonoBehaviour>().Where(m => m != null).Select(m => m.GetType().Name)));
                }
            }
            sb.AppendLine("== Letreros cerca del taller (x -40..-24, z -80..-60)");
            foreach (Transform t in ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && (t.name.Contains("Letrero") || t.name.StartsWith("C15") || t.name.StartsWith("L1") || t.GetComponent<Canvas>() != null)
                && t.position.x > -40f && t.position.x < -24f && t.position.z > -80f && t.position.z < -60f))
            {
                sb.AppendLine("  " + Ruta(t) + " p" + t.position.ToString("F2") + " r" + t.eulerAngles.ToString("F0") + " " + Fuente(t.gameObject));
            }
            File.WriteAllText("Logs/s2t-diag-taller.txt", sb.ToString());
        }

        public static void Nombres()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (var g in ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && t.GetComponent<Renderer>() != null && t.GetComponent<Renderer>().enabled)
                .GroupBy(t => string.Join("/", Ruta(t).Split('/').Take(2)) + " | " + (t.GetComponent<Renderer>().sharedMaterial != null ? t.GetComponent<Renderer>().sharedMaterial.name : "-"))
                .Where(g => g.Key.ToLowerInvariant().Contains("tej") || g.Key.ToLowerInvariant().Contains("techo") || g.Key.ToLowerInvariant().Contains("azotea") || g.Key.ToLowerInvariant().Contains("teja") || g.Key.ToLowerInvariant().Contains("lamina") || g.Key.ToLowerInvariant().Contains("losa")))
            {
                Transform e = g.First();
                sb.AppendLine(g.Count() + "x " + g.Key + " ej " + Ruta(e) + " c" + e.GetComponent<Renderer>().bounds.center.ToString("F0") + " s" + e.GetComponent<Renderer>().bounds.size.ToString("F1"));
            }
            File.WriteAllText("Logs/s2t-diag-nombres.txt", sb.ToString());
            ColocarS2t.FotoHora(13f, new Vector3(-80f, 45f, -110f), new Vector3(-50f, 0f, -75f), ColocarS2t.CarpetaCapturas + "s2t-recorrido-alto-casa.png", 70f);
            ColocarS2t.FotoHora(13f, new Vector3(-20f, 45f, -40f), new Vector3(-50f, 0f, -90f), ColocarS2t.CarpetaCapturas + "s2t-recorrido-alto-casa2.png", 70f);
        }

        public static void FotosSur()
        {
            ColocarS2.Abrir();
            var tomas = new[] {
                ("sur-1", new Vector3(-47f, 1.7f, -88f), new Vector3(-30f, 4f, -98f)),
                ("sur-2", new Vector3(-20f, 1.7f, -85f), new Vector3(-30f, 5f, -98f)),
                ("sur-3", new Vector3(-47f, 1.7f, -100f), new Vector3(-65f, 4f, -100f)),
                ("sur-4", new Vector3(-30f, 25f, -70f), new Vector3(-30f, 0f, -100f)) };
            foreach (var (n, d, h) in tomas)
            {
                ColocarS2t.FotoHora(13f, d, h, ColocarS2t.CarpetaCapturas + "s2t-recorrido-" + n + ".png", 70f);
            }
            File.WriteAllText("Logs/s2t-diag-sur.txt", "== cerca de (-30,-98)" + System.Environment.NewLine + string.Join(System.Environment.NewLine, ColocarS2.Todos().Where(t => t.parent != null && t.parent.parent == null && t.gameObject.activeInHierarchy).SelectMany(r => r.Cast<Transform>()).Where(t => new Vector2(t.position.x + 35f, t.position.z + 98f).magnitude < 20f).Select(t => Ruta(t) + " p" + t.position.ToString("F0"))));
        }

        public static void Elevados()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (Rigidbody rb in Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.position.y > 4f))
            {
                sb.AppendLine("cuerpo alto: " + Ruta(rb.transform) + " p" + rb.position.ToString("F1"));
            }
            foreach (Transform t in ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("N02") || t.name.Contains("Autopista"))))
            {
                Renderer r = t.GetComponentInChildren<Renderer>();
                sb.AppendLine(Ruta(t) + " p" + t.position.ToString("F1") + (r != null ? " b" + r.bounds.center.ToString("F1") + " s" + r.bounds.size.ToString("F1") : ""));
            }
            File.WriteAllText("Logs/s2t-diag-elevados.txt", sb.ToString());
        }

        public static void FotoL03()
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Callejon/Prefabs/L03_EscaleraEmergencia.prefab");
            GameObject i = (GameObject)PrefabUtility.InstantiatePrefab(p);
            Bounds b = ArteT250.Limites(i);
            File.WriteAllText("Logs/s2t-diag-l03.txt", "L03 limites " + b.center.ToString("F2") + " " + b.size.ToString("F2") + " colliders " + string.Join(",", i.GetComponentsInChildren<Collider>().Select(c => c.GetType().Name + " " + c.bounds.size.ToString("F2"))));
            Object.DestroyImmediate(i);
            ColocarS2t.FotoPrefabs(new[] { "Assets/Arte/Callejon/Prefabs/L03_EscaleraEmergencia.prefab", "Assets/Arte/Calle/Prefabs/N02.prefab" }, ColocarS2t.CarpetaCapturas + "s2t-l03-n02.png");
        }

        public static void AlturasL03()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject i = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Callejon/Prefabs/L03_EscaleraEmergencia.prefab"));
            foreach (Collider c in i.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (MeshFilter mf in i.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            Physics.SyncTransforms();
            Bounds b = ArteT250.Limites(i);
            StringBuilder sb = new StringBuilder("L03 " + b.min.ToString("F2") + " .. " + b.max.ToString("F2") + "  filas z de max a min, columnas x de min a max (alto en dm)" + System.Environment.NewLine);
            for (float z = b.max.z; z >= b.min.z; z -= 0.25f)
            {
                sb.Append(z.ToString("F2").PadLeft(6) + " ");
                for (float x = b.min.x; x <= b.max.x; x += 0.25f)
                {
                    sb.Append(Physics.Raycast(new Vector3(x, b.max.y + 1f, z), Vector3.down, out RaycastHit h, 10f) ? ((int)(h.point.y * 10f)).ToString().PadLeft(3) : "  .");
                }
                sb.AppendLine();
            }
            File.WriteAllText("Logs/s2t-diag-l03-alturas.txt", sb.ToString());
        }

        public static void FotosRamales()
        {
            ColocarS2.Abrir();
            var tomas = new[] {
                ("callejon-fondo", new Vector3(-60f, 1.7f, -60f), new Vector3(-90f, 2f, -60f)),
                ("callejon-alto", new Vector3(-60f, 30f, -45f), new Vector3(-80f, 0f, -62f)),
                ("ramal-x25", new Vector3(-25f, 1.7f, 6f), new Vector3(-25f, 2f, 45f)),
                ("ramal-x25-alto", new Vector3(-5f, 30f, 20f), new Vector3(-25f, 0f, 35f)) };
            foreach (var (n, d, h) in tomas)
            {
                ColocarS2t.FotoHora(13f, d, h, ColocarS2t.CarpetaCapturas + "s2t-" + n + ".png", 70f);
            }
        }

        public static void Pasillos()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            StringBuilder sb = new StringBuilder();
            foreach (var (n, caja) in new[] {
                ("callejon -> oeste", new Bounds(new Vector3(-110f, 2f, -60f), new Vector3(50f, 3f, 5f))),
                ("ramal x-25 -> norte", new Bounds(new Vector3(-25f, 2f, 65f), new Vector3(5f, 3f, 40f))),
                ("ramal x-25 -> oeste", new Bounds(new Vector3(-45f, 2f, 40f), new Vector3(40f, 3f, 5f))) })
            {
                sb.AppendLine("== " + n + " " + caja.min.ToString("F0") + ".." + caja.max.ToString("F0"));
                foreach (Collider c in Physics.OverlapBox(caja.center, caja.extents, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .GroupBy(c => c.transform).Select(g => g.First()))
                {
                    sb.AppendLine("  " + Ruta(c.transform) + " b" + c.bounds.min.ToString("F0") + ".." + c.bounds.max.ToString("F0"));
                }
            }
            File.WriteAllText("Logs/s2t-diag-pasillos.txt", sb.ToString());
        }

        public static void Edificios()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (string r in new[] { "Barrio/Manzana_2/Edificios/Edificio_13", "Barrio/Manzana_2/Edificios/Edificio_14", "Barrio/Manzana_4/Edificios/Edificio_39", "Barrio/Manzana_4/Edificios/Edificio_40" })
            {
                Transform t = ColocarS2.Buscar(r);
                sb.AppendLine("== " + r + " hijos " + (t == null ? "FALTA" : t.childCount.ToString()));
                if (t == null) continue;
                foreach (var g in t.GetComponentsInChildren<Transform>(true).Where(h => h != t).GroupBy(h => h.parent == t ? h.name.Split('_')[0] : "(nieto)"))
                    sb.AppendLine("   " + g.Count() + "x " + g.Key);
            }
            // Lo que vive fuera del edificio pero sobre su huella (fachadas del kit de T-501/T-530, pisos de ArteNoche, letreros...).
            foreach (var (n, caja) in new[] { ("huella 13+14", new Bounds(new Vector3(-22f, 10f, 64.5f), new Vector3(6.4f, 30f, 39.5f))) })
            {
                sb.AppendLine("== " + n);
                foreach (var g in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.enabled && caja.Contains(r.bounds.center))
                    .GroupBy(r => string.Join("/", Ruta(r.transform).Split('/').Take(3))))
                    sb.AppendLine("   " + g.Count() + "x " + g.Key);
            }
            File.WriteAllText("Logs/s2t-diag-edificios.txt", sb.ToString());
        }

        public static void Brumas()
        {
            ColocarS2.Abrir();
            StringBuilder sb = new StringBuilder();
            foreach (UnityEngine.Rendering.Volume v in Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Collider c = v.GetComponent<Collider>();
                sb.AppendLine("volumen " + Ruta(v.transform) + " global " + v.isGlobal + " peso " + v.weight + " perfil " + (v.sharedProfile != null ? v.sharedProfile.name + " [" + string.Join(",", v.sharedProfile.components.Select(x => x.GetType().Name)) + "]" : "-") + (c != null ? " caja " + c.bounds.center.ToString("F0") + " " + c.bounds.size.ToString("F0") : ""));
            }
            foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(p => new Vector2(p.transform.position.x + 85f, p.transform.position.z + 60f).magnitude < 40f))
            {
                sb.AppendLine("particulas " + Ruta(ps.transform) + " p" + ps.transform.position.ToString("F0"));
            }
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.enabled && r.sharedMaterial != null && (r.sharedMaterial.name.ToLowerInvariant().Contains("niebla") || r.sharedMaterial.name.ToLowerInvariant().Contains("bruma") || r.sharedMaterial.name.ToLowerInvariant().Contains("vapor") || r.sharedMaterial.name.ToLowerInvariant().Contains("humo"))))
            {
                sb.AppendLine("malla de bruma " + Ruta(r.transform) + " c" + r.bounds.center.ToString("F0") + " s" + r.bounds.size.ToString("F0") + " " + r.sharedMaterial.name);
            }
            File.WriteAllText("Logs/s2t-diag-brumas.txt", sb.ToString());
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
