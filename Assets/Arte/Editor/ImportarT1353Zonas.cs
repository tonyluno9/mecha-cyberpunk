using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1353: prefabs de las piezas modulares de Blender de las zonas nuevas (Assets/Arte/ZonasModulos/Modelos/*.fbx).
    /// Cada prefab: raíz con hijo Visual (instancia del FBX: si el modelo cambia, el prefab sigue valiendo), pivote = origen del FBX
    /// (ya viene en la base al centro, o en el enganche del tramo), collider, URP Lit por nombre de material, LODGroup y flags estáticos.
    /// Idempotente: reescribe el prefab en su misma ruta (conserva el GUID).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarT1353Zonas.Importar
    /// </summary>
    public static class ImportarT1353Zonas
    {
        public const string Carpeta = "Assets/Arte/ZonasModulos/";
        public const string CarpetaModelos = Carpeta + "Modelos/";
        public const string CarpetaPrefabs = Carpeta + "Prefabs/";
        public const string CarpetaMateriales = Carpeta + "Materiales/";
        public const string RutaMalla = CarpetaModelos + "T_Malla.png";

        public enum TipoCollider
        {
            Caja,
            Malla,
            MallaConvexa,
            Rampa
        }

        public sealed class Pieza
        {
            public readonly string Nombre;
            public readonly TipoCollider Collider;
            /// <summary>Porcentaje de pantalla (0-1) bajo el cual se oculta.</summary>
            public readonly float Corte;
            public readonly bool Oclusor;
            /// <summary>Materiales en el orden de las ranuras del FBX (el importador T250 no trae los del archivo).</summary>
            public readonly string[] Ranuras;

            public Pieza(string nombre, TipoCollider collider, float corte, bool oclusor, params string[] ranuras)
            {
                Nombre = nombre;
                Collider = collider;
                Corte = corte;
                Oclusor = oclusor;
                Ranuras = ranuras;
            }

            public string RutaFbx => CarpetaModelos + Nombre + ".fbx";
            public string RutaPrefab => CarpetaPrefabs + Nombre + ".prefab";
        }

        private const float CorteChico = 0.03f;
        private const float CorteMediano = 0.02f;
        private const float CorteGrande = 0.01f;

        public static readonly Pieza[] Piezas =
        {
            new Pieza("AU01_AutopistaRecta", TipoCollider.Malla, CorteGrande, true, "Concreto", "Asfalto", "PinturaBlanca"),
            new Pieza("AU02_AutopistaCurva45", TipoCollider.Malla, CorteGrande, true, "Concreto", "Asfalto", "PinturaBlanca"),
            new Pieza("AU03_AutopistaRampa", TipoCollider.Malla, CorteGrande, true, "Concreto", "Asfalto", "PinturaBlanca"),
            new Pieza("AU04_ColumnaAutopista", TipoCollider.Caja, CorteGrande, true, "Concreto"),
            new Pieza("AU05_Guardarrail", TipoCollider.Caja, CorteChico, false, "Metal"),
            new Pieza("AU06_EntradaTunel", TipoCollider.Malla, CorteGrande, true, "Concreto", "Fondo", "Asfalto", "Luz"),
            new Pieza("CE01_EscaleraCerro", TipoCollider.Rampa, CorteMediano, false, "Concreto"),
            new Pieza("CE02_BarandalTubo", TipoCollider.Caja, CorteChico, false, "Metal"),
            new Pieza("CE03_BarandalTuboEscalera", TipoCollider.Caja, CorteChico, false, "Metal"),
            new Pieza("DS01_RejaPuas", TipoCollider.Caja, CorteMediano, false, "Metal", "Malla", "Hierro"),
            new Pieza("LM01_MuroLuz", TipoCollider.Caja, CorteMediano, true, "ConcretoClaro", "Luz"),
            new Pieza("ML01_BarandalMalecon", TipoCollider.Caja, CorteChico, false, "ConcretoClaro"),
            new Pieza("ML02_EscaleraPlaya", TipoCollider.Rampa, CorteMediano, false, "Concreto"),
            new Pieza("ML03_MuelleRecto", TipoCollider.Malla, CorteMediano, false, "Madera", "MaderaOscura"),
            new Pieza("ML04_MuelleEsquina", TipoCollider.Malla, CorteMediano, false, "Madera", "MaderaOscura"),
            new Pieza("ML05_Escollera", TipoCollider.MallaConvexa, CorteMediano, true, "Roca"),
            new Pieza("PT01_Contenedor40", TipoCollider.Caja, CorteMediano, true, "Contenedor"),
            new Pieza("PT02_BitaAmarre", TipoCollider.Caja, CorteChico, false, "Hierro"),
        };

        /// <summary>Grosor mínimo (m) de una caja de collider: los barandales miden 5 cm y se atravesarían.</summary>
        private const float GrosorMinimoCaja = 0.2f;
        private const float GrosorRampa = 0.3f;

        [MenuItem("Mecha Cyberpunk/T-1353/Importar piezas de zonas")]
        public static void Importar()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(CarpetaPrefabs);
            Directory.CreateDirectory(CarpetaMateriales);
            StringBuilder informe = new StringBuilder();
            foreach (Pieza pieza in Piezas)
            {
                informe.AppendLine(CrearPrefab(pieza));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1353-importar.txt", informe.ToString());
            Debug.Log("[Arte] T-1353 piezas de zonas:\n" + informe);
        }

        public static string CrearPrefab(Pieza pieza)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(pieza.RutaFbx);
            if (fbx == null)
            {
                return pieza.Nombre + "\tFALTA FBX " + pieza.RutaFbx;
            }

            GameObject raiz = new GameObject(pieza.Nombre);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                int n = Mathf.Max(1, r.sharedMaterials.Length);
                Material[] mats = new Material[n];
                for (int i = 0; i < n; i++)
                {
                    mats[i] = MaterialDe(i < pieza.Ranuras.Length ? pieza.Ranuras[i] : pieza.Ranuras[0]);
                }
                r.sharedMaterials = mats;
            }

            Bounds b = Limites(raiz);
            PonerCollider(pieza, raiz, visual, b);

            LODGroup lod = raiz.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(pieza.Corte, renderers) });
            lod.RecalculateBounds();

            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            if (pieza.Oclusor)
            {
                flags |= StaticEditorFlags.OccluderStatic;
            }
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(pieza.RutaPrefab));
            PrefabUtility.SaveAsPrefabAsset(raiz, pieza.RutaPrefab);
            Object.DestroyImmediate(raiz);
            return string.Format("{0}\tcollider {1}\tcaras {2}\tbounds {3:F2}x{4:F2}x{5:F2} min {6:F2},{7:F2},{8:F2}",
                pieza.Nombre, pieza.Collider, TriangulosDe(fbx), b.size.x, b.size.y, b.size.z, b.min.x, b.min.y, b.min.z);
        }

        private static int TriangulosDe(GameObject fbx)
        {
            return fbx.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => f.sharedMesh.triangles.Length / 3);
        }

        private static void PonerCollider(Pieza pieza, GameObject raiz, GameObject visual, Bounds b)
        {
            switch (pieza.Collider)
            {
                case TipoCollider.Caja:
                    BoxCollider caja = raiz.AddComponent<BoxCollider>();
                    caja.center = b.center;
                    caja.size = new Vector3(Mathf.Max(b.size.x, GrosorMinimoCaja), Mathf.Max(b.size.y, GrosorMinimoCaja), Mathf.Max(b.size.z, GrosorMinimoCaja));
                    break;
                case TipoCollider.Malla:
                case TipoCollider.MallaConvexa:
                    // Un MeshCollider por malla del FBX, en el hijo que la lleva: así la malla sigue siendo la del modelo.
                    foreach (MeshFilter f in visual.GetComponentsInChildren<MeshFilter>(true))
                    {
                        MeshCollider mc = f.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = f.sharedMesh;
                        mc.convex = pieza.Collider == TipoCollider.MallaConvexa;
                    }
                    break;
                case TipoCollider.Rampa:
                    PonerRampa(raiz, visual);
                    break;
            }
        }

        /// <summary>
        /// Escalera que se sube a pie: el CharacterController no sube peldaños de más de 0.35 m, así que (como T-996) lleva una rampa
        /// invisible de caja que va del pie al último peldaño. Se calcula con la malla: el eje de subida es el horizontal cuya posición
        /// más se correlaciona con la altura de los vértices.
        /// </summary>
        private static void PonerRampa(GameObject raiz, GameObject visual)
        {
            Vector3[] puntos = visual.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh != null)
                .SelectMany(f => f.sharedMesh.vertices.Select(v => raiz.transform.InverseTransformPoint(f.transform.TransformPoint(v))))
                .ToArray();
            Bounds b = Limites(raiz);
            float mediaX = puntos.Average(p => p.x);
            float mediaZ = puntos.Average(p => p.z);
            float mediaY = puntos.Average(p => p.y);
            float covX = puntos.Sum(p => (p.x - mediaX) * (p.y - mediaY));
            float covZ = puntos.Sum(p => (p.z - mediaZ) * (p.y - mediaY));
            bool porZ = Mathf.Abs(covZ) >= Mathf.Abs(covX);
            float signo = (porZ ? covZ : covX) >= 0f ? 1f : -1f;
            Vector3 eje = porZ ? Vector3.forward : Vector3.right;
            float largoEje = porZ ? b.size.z : b.size.x;
            float ancho = porZ ? b.size.x : b.size.z;
            float subida = b.max.y;
            Vector3 pie = Vector3.Scale(b.center, Vector3.one - AbsEje(eje)) - eje * signo * (largoEje * 0.5f);
            pie.y = 0f;
            Vector3 cima = pie + eje * signo * largoEje + Vector3.up * subida;
            Vector3 dir = (cima - pie).normalized;
            Quaternion giro = Quaternion.LookRotation(dir, Vector3.up);

            GameObject g = new GameObject("Rampa");
            g.transform.SetParent(raiz.transform, false);
            BoxCollider caja = g.AddComponent<BoxCollider>();
            caja.size = new Vector3(ancho, GrosorRampa, (cima - pie).magnitude);
            g.transform.localRotation = giro;
            g.transform.localPosition = (pie + cima) * 0.5f - g.transform.localRotation * Vector3.up * (GrosorRampa * 0.5f);
        }

        private static Vector3 AbsEje(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        // ------------------------------------------------------------------ materiales

        private static Material MaterialDe(string nombre)
        {
            string ruta = CarpetaMateriales + "ZM_" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            Color color;
            float metal = 0f;
            float suavidad = 0.2f;
            switch (nombre)
            {
                case "Asfalto": color = new Color(0.13f, 0.13f, 0.14f); suavidad = 0.15f; break;
                case "Concreto": color = new Color(0.42f, 0.42f, 0.43f); break;
                case "ConcretoClaro": color = new Color(0.65f, 0.64f, 0.62f); break;
                case "PinturaBlanca": color = new Color(0.85f, 0.85f, 0.82f); break;
                case "Metal": color = new Color(0.45f, 0.47f, 0.5f); metal = 0.6f; suavidad = 0.4f; break;
                case "Hierro": color = new Color(0.22f, 0.22f, 0.24f); metal = 0.5f; suavidad = 0.3f; break;
                case "Malla": color = new Color(0.67f, 0.69f, 0.71f); metal = 0.4f; break;
                case "Madera": color = new Color(0.42f, 0.29f, 0.18f); break;
                case "MaderaOscura": color = new Color(0.24f, 0.16f, 0.1f); break;
                case "Roca": color = new Color(0.34f, 0.33f, 0.31f); suavidad = 0.1f; break;
                case "Contenedor": color = new Color(0.55f, 0.2f, 0.15f); metal = 0.3f; break;
                case "Fondo": color = new Color(0.02f, 0.02f, 0.03f); suavidad = 0.05f; break;
                case "Luz": color = new Color(0.9f, 0.95f, 1f); break;
                default: color = new Color(0.5f, 0.5f, 0.5f); break;
            }
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", suavidad);
            if (nombre == "Luz")
            {
                // Luz emisiva (sin Light real). Sin RealtimeEmissive se pierde _EMISSION al reimportar.
                m.SetColor("_EmissionColor", new Color(0.55f, 0.95f, 1.4f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.EnableKeyword("_EMISSION");
            }
            if (nombre == "Malla")
            {
                // T_Malla.png: rombos de reja con huecos transparentes -> recorte alfa a dos caras.
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaMalla);
                m.SetTexture("_BaseMap", t);
                m.SetTexture("_MainTex", t);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = 2450;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Bounds Limites(GameObject objeto)
        {
            Renderer[] rs = objeto.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
