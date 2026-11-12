using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-970: importa las piezas reales de Blender de la escopeta, el fusil y el francotirador, remapea sus materiales y las pone en los
    /// prefabs W0x en lugar de la malla vieja de una pieza. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.ArmasPiezasT970.Importar y luego .ColocarPrefabs
    /// </summary>
    public static class ArmasPiezasT970
    {
        private const string Raiz = "Assets/Arte/Armas/";
        private const string MaterialCorte = Raiz + "Armas_CorteInterior.mat";
        private const float MargenBoca = 0.005f;
        private static readonly Quaternion GiroModelo = Quaternion.Euler(90f, 0f, 180f);

        private sealed class Arma
        {
            public readonly string Carpeta;
            public readonly string Id;
            public Arma(string carpeta, string id) { Carpeta = carpeta; Id = id; }
            public string RutaFbx => Raiz + Carpeta + "/" + Id + "_Piezas.fbx";
            public string RutaPrefab => Raiz + Carpeta + "/" + Carpeta + ".prefab";
            public string RutaMat => Raiz + Carpeta + "/" + Carpeta + ".mat";
        }

        private static readonly Arma[] Armas =
        {
            new Arma("W02_Escopeta", "W02"), new Arma("W03_Fusil", "W03"), new Arma("W04_Francotirador", "W04")
        };

        /// <summary>Importa los tres FBX de piezas y remapea materiales.</summary>
        public static void Importar()
        {
            Material corte = AssetDatabase.LoadAssetAtPath<Material>(MaterialCorte);
            if (corte == null)
            {
                corte = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Armas_CorteInterior" };
                corte.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.06f, 1f));
                corte.SetFloat("_Metallic", 0.3f);
                corte.SetFloat("_Smoothness", 0.25f);
                AssetDatabase.CreateAsset(corte, MaterialCorte);
            }
            foreach (Arma a in Armas)
            {
                AssetDatabase.ImportAsset(a.RutaFbx, ImportAssetOptions.ForceUpdate);
                ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(a.RutaFbx);
                imp.animationType = ModelImporterAnimationType.None;
                imp.importAnimation = false;
                imp.importCameras = false;
                imp.importLights = false;
                imp.isReadable = false;
                imp.useFileScale = true;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                imp.SaveAndReimport();
                Material arma = AssetDatabase.LoadAssetAtPath<Material>(a.RutaMat);
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(a.RutaFbx))
                {
                    if (o is Material m)
                    {
                        Material destino = m.name.StartsWith("Corte_Interior") ? corte : arma;
                        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), destino);
                        Debug.Log("[T970] " + a.Id + " material " + m.name + " -> " + destino.name);
                    }
                }
                imp.SaveAndReimport();
                string todos = "";
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(a.RutaFbx))
                {
                    todos += o.GetType().Name + ":" + o.name + " ";
                }
                Debug.Log("[T970] " + a.Id + " sub-assets: " + todos);
            }
            AssetDatabase.SaveAssets();
        }

        // Límites en el espacio local de la raíz, con los vértices reales.
        private static bool Limites(Transform raiz, out Bounds b, System.Func<Renderer, bool> filtro)
        {
            b = new Bounds();
            bool primero = true;
            List<Vector3> v = new List<Vector3>();
            foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter f = r.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null || !filtro(r))
                {
                    continue;
                }
                Matrix4x4 m = raiz.worldToLocalMatrix * r.transform.localToWorldMatrix;
                v.Clear();
                f.sharedMesh.GetVertices(v);
                foreach (Vector3 p in v)
                {
                    Vector3 q = m.MultiplyPoint3x4(p);
                    if (primero)
                    {
                        b = new Bounds(q, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(q);
                    }
                }
            }
            return !primero;
        }

        /// <summary>Arma todos los prefabs W0x con sus piezas (la misma lógica que usa ColocarS1nArmas) y reporta límites.</summary>
        public static void ColocarPrefabs()
        {
            StringBuilder informe = new StringBuilder();
            foreach (Arma a in Armas)
            {
                ArmarPrefab(a.RutaPrefab, a.RutaFbx, informe);
            }
            Debug.Log("[T970]\n" + informe);
        }

        /// <summary>
        /// Quita el Modelo viejo del prefab (si lo hay), pone el FBX de piezas girado (cañón a +Z, arriba +Y, escala 1, empuñadura en el origen)
        /// y mueve la Boca a la punta de la pieza Canon. Idempotente: si ya está armado no cambia nada.
        /// </summary>
        public static void ArmarPrefab(string rutaPrefab, string rutaFbx, StringBuilder sb)
        {
            string id = System.IO.Path.GetFileNameWithoutExtension(rutaFbx);
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
                Transform t = raiz.transform;
                sb.AppendLine("== " + id + " raiz rot " + t.localEulerAngles + " esc " + t.localScale + " hijos: " + Hijos(t));
                Transform viejo = t.Find("Modelo");
                bool yaNuevo = viejo != null && viejo.Find("Armazon") != null;
                if (viejo != null && !yaNuevo)
                {
                    if (Limites(t, out Bounds bv, r => r.transform.IsChildOf(viejo)))
                    {
                        sb.AppendLine("viejo min " + bv.min.ToString("F3") + " max " + bv.max.ToString("F3") + " largoZ " + bv.size.z.ToString("F3"));
                    }
                    Object.DestroyImmediate(viejo.gameObject);
                }
                if (!yaNuevo)
                {
                    GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
                    GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, t);
                    nuevo.name = "Modelo";
                    nuevo.transform.localPosition = Vector3.zero;
                    nuevo.transform.localScale = Vector3.one;
                    nuevo.transform.SetSiblingIndex(0);
                }
                Transform modelo = t.Find("Modelo");
                // El FBX sale con el cañón sobre -Y de Unity y el arriba sobre +Z: este giro (90 sobre X y 180 sobre el eje del cañón) lo deja con el cañón a +Z y arriba +Y.
                modelo.localRotation = GiroModelo;
                sb.AppendLine("nuevo hijos: " + Hijos(modelo));
                Limites(t, out Bounds bn, r => r.transform.IsChildOf(modelo));
                sb.AppendLine("nuevo min " + bn.min.ToString("F3") + " max " + bn.max.ToString("F3") + " largoZ " + bn.size.z.ToString("F3"));
                Transform canon = BuscarHijo(modelo, "Canon");
                if (canon != null && Limites(t, out Bounds bc, r => r.transform.IsChildOf(canon)))
                {
                    Vector3 suma = Vector3.zero;
                    int n = 0;
                    foreach (MeshRenderer r in canon.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Matrix4x4 m = t.worldToLocalMatrix * r.transform.localToWorldMatrix;
                        foreach (Vector3 p in r.GetComponent<MeshFilter>().sharedMesh.vertices)
                        {
                            Vector3 q = m.MultiplyPoint3x4(p);
                            if (q.z >= bc.max.z - MargenBoca)
                            {
                                suma += q;
                                n++;
                            }
                        }
                    }
                    Vector3 punta = suma / Mathf.Max(1, n);
                    punta.z = bc.max.z;
                    Transform boca = BuscarHijo(t, "Boca");
                    if (boca != null)
                    {
                        sb.AppendLine("Boca vieja " + t.InverseTransformPoint(boca.position).ToString("F3") + " -> nueva " + punta.ToString("F3"));
                        boca.position = t.TransformPoint(punta);
                    }
                }
                foreach (Transform h in t.GetComponentsInChildren<Transform>(true))
                {
                    if (h.name.StartsWith("Agarre"))
                    {
                        Vector3 p = t.InverseTransformPoint(h.position);
                        sb.AppendLine(h.name + " en " + p.ToString("F3") + (bn.Contains(p) ? " dentro del arma" : " FUERA del arma nueva"));
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>Pone en ConfigMenuArmas_Base los nombres de las piezas reales (la pistola no cambia) y quita la mira a la escopeta.</summary>
        public static void ConfigurarBanco()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(CreadorMenuArmas.RutaConfig);
            SerializedObject so = new SerializedObject(config);
            SerializedProperty armas = so.FindProperty("armas");
            string[][] piezas =
            {
                new[] { "Bomba", "Canon", "" }, new[] { "Cargador", "Canon", "Mira" }, new[] { "Cargador", "Canon", "Mira" }
            };
            for (int i = 0; i < piezas.Length; i++)
            {
                SerializedProperty e = armas.GetArrayElementAtIndex(i + 1);
                e.FindPropertyRelative("piezaCargador").stringValue = piezas[i][0];
                e.FindPropertyRelative("piezaCanon").stringValue = piezas[i][1];
                e.FindPropertyRelative("piezaMira").stringValue = piezas[i][2];
                e.FindPropertyRelative("tieneMira").boolValue = i != 0;
            }
            for (int i = 0; i < armas.arraySize; i++)
            {
                if (i != 1)
                {
                    armas.GetArrayElementAtIndex(i).FindPropertyRelative("tieneMira").boolValue = true;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Guarda docs/capturas/t970/modelo-w0x.png (de lado, fondo gris) y piso-w0x.png (el prefab de piso sobre un plano).</summary>
        public static void CapturarModelos()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            foreach (Arma a in Armas)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(a.RutaPrefab));
                Foto("modelo-" + a.Id.ToLowerInvariant(), go, Vector3.right, false);
                Object.DestroyImmediate(go);
                GameObject piso = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + a.Carpeta + "/" + a.Carpeta + "_Piso.prefab"));
                piso.transform.position = new Vector3(0f, 0.25f, 0f);
                Foto("piso-" + a.Id.ToLowerInvariant(), piso, new Vector3(0.6f, 0.5f, -1f).normalized, true);
                Object.DestroyImmediate(piso);
            }
        }

        private static void Foto(string nombre, GameObject objeto, Vector3 haciaCamara, bool conPlano)
        {
            List<GameObject> temporales = new List<GameObject>();
            GameObject luz = new GameObject("Luz");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            luz.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            temporales.Add(luz);
            if (conPlano)
            {
                GameObject plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plano.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
                temporales.Add(plano);
            }
            Bounds b = new Bounds(objeto.transform.position, Vector3.zero);
            foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
            GameObject cam = new GameObject("Camara");
            Camera c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.45f, 0.45f, 0.48f);
            c.fieldOfView = 30f;
            cam.transform.position = b.center + haciaCamara * (Mathf.Max(b.size.x, b.size.z) * 2.2f + 0.3f);
            cam.transform.LookAt(b.center);
            temporales.Add(cam);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            RenderTexture.active = null;
            System.IO.Directory.CreateDirectory("docs/capturas/t970");
            System.IO.File.WriteAllBytes("docs/capturas/t970/" + nombre + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            foreach (GameObject g in temporales)
            {
                Object.DestroyImmediate(g);
            }
        }

        private static string Hijos(Transform t)
        {
            string s = "";
            foreach (Transform h in t)
            {
                s += h.name + " ";
            }
            return s;
        }

        private static Transform BuscarHijo(Transform raiz, string nombre)
        {
            foreach (Transform h in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (h.name == nombre)
                {
                    return h;
                }
            }
            return null;
        }
    }
}
