using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-544 (plan de la noche 3-nov, paso 2): pantallas LED con anuncios (imageassets/pantallas, copiados a Assets/Arte/Exteriores/Pantallas). Cada
    /// E03, CY02, N08 y piso T22 de la escena lleva un quad con el anuncio sobre el área de su pantalla (horizontales en las anchas, verticales en las
    /// altas) con PantallaLed (cambia cada 8-12 s y parpadea leve). Las CY02 que estaban paradas en medio de la banqueta se pegan al muro más cercano
    /// a 2.6 m de alto (son de pared). El área de cada pantalla se midió en vistas ortográficas (Logs/t544-vistas.png, ImportarT540.VistasPantallas)
    /// y se ajusta al frente real con un rayo. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT544.Colocar
    /// </summary>
    public static class ColocarT544
    {
        public const string NombreQuad = "PantallaLed_T544";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Exteriores/Pantallas/";
        private const string RutaMaterial = "Assets/Arte/Exteriores/Pantallas/T544_PantallaLed.mat";

        /// <summary>Modelo, centro de la pantalla y tamaño (m, en el prefab sin girar, frente a -Z), si es vertical y si también tiene pantalla atrás.</summary>
        private static readonly (string ruta, Vector2 centro, Vector2 tam, bool vertical, bool atras)[] Modelos =
        {
            ("Assets/Arte/Exteriores/Prefabs/E03_PantallaGigante.prefab", new Vector2(0.2f, 4.69f), new Vector2(10.2f, 4.8f), false, false),
            ("Assets/Arte/Calle/Prefabs/CY02_PantallaVertical.prefab", new Vector2(-0.03f, 1.98f), new Vector2(1.1f, 2.45f), true, false),
            ("Assets/Arte/Exteriores/Prefabs/N08.prefab", new Vector2(0f, 2.83f), new Vector2(1.1f, 2.05f), true, true),
            ("Assets/Arte/Exteriores/Prefabs/T22.prefab", new Vector2(0.35f, 8.51f), new Vector2(13f, 10.8f), false, false),
        };

        private sealed class Medida
        {
            public Mesh Malla;
            public Vector3 Local;
            public Quaternion Giro;
            public Vector2 Tam;
            public bool Vertical;
        }

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-544/Pantallas LED")]
        public static void Colocar()
        {
            Informe.Clear();
            Texture2D[] horizontales = Texturas("pantalla-h-");
            Texture2D[] verticales = Texturas("pantalla-v-");
            Material material = Material();
            List<Medida> medidas = Medir();

            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            foreach (Transform viejo in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == NombreQuad).ToList())
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            PegarAlMuro();
            Type tipo = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Ambiente.PantallaLed")).FirstOrDefault(t => t != null);
            int n = 0;
            System.Random azar = new System.Random(544);
            foreach (MeshFilter f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                foreach (Medida m in medidas.Where(m => m.Malla == f.sharedMesh))
                {
                    GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    q.name = NombreQuad;
                    Object.DestroyImmediate(q.GetComponent<Collider>());
                    q.transform.SetParent(f.transform, false);
                    q.transform.localPosition = m.Local;
                    q.transform.localRotation = m.Giro;
                    q.transform.localScale = new Vector3(m.Tam.x, m.Tam.y, 1f);
                    MeshRenderer r = q.GetComponent<MeshRenderer>();
                    r.sharedMaterial = material;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    if (tipo != null)
                    {
                        Component c = q.AddComponent(tipo);
                        SerializedObject so = new SerializedObject(c);
                        Texture2D[] lista = m.Vertical ? verticales : horizontales;
                        SerializedProperty p = so.FindProperty("anuncios");
                        p.arraySize = lista.Length;
                        for (int i = 0; i < lista.Length; i++)
                        {
                            p.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];
                        }
                        so.FindProperty("fase").floatValue = (float)azar.NextDouble();
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    n++;
                }
            }
            Informe.AppendLine("Pantallas con anuncio: " + n + " (" + horizontales.Length + " horizontales y " + verticales.Length + " verticales)");
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t544-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-544 listo:\n" + Informe);
        }

        private static Texture2D[] Texturas(string prefijo)
        {
            List<Texture2D> lista = new List<Texture2D>();
            foreach (string ruta in Directory.GetFiles(Carpeta, prefijo + "*.png").OrderBy(r => r))
            {
                string r = ruta.Replace('\\', '/');
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(r);
                if (ti != null && (ti.maxTextureSize != 1024 || ti.wrapMode != TextureWrapMode.Clamp))
                {
                    ti.maxTextureSize = 1024;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.mipmapEnabled = true;
                    ti.SaveAndReimport();
                }
                lista.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(r));
            }
            return lista.Where(t => t != null).ToArray();
        }

        private static Material Material()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, RutaMaterial);
            }
            m.SetColor("_BaseColor", Color.white * 1.6f);
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        // En cada modelo: la pantalla en el espacio de su malla (para pegarla igual en cualquier copia, con cualquier escala).
        private static List<Medida> Medir()
        {
            List<Medida> medidas = new List<Medida>();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var mod in Modelos)
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(mod.ruta));
                MeshFilter f = g.GetComponentsInChildren<MeshFilter>().OrderByDescending(x => x.sharedMesh != null ? x.sharedMesh.bounds.size.sqrMagnitude : 0f).First();
                MeshCollider mc = f.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = f.sharedMesh;
                Physics.SyncTransforms();
                foreach (bool atras in mod.atras ? new[] { false, true } : new[] { false })
                {
                    Vector3 hacia = atras ? Vector3.back : Vector3.forward;
                    Vector3 desde = new Vector3(atras ? -mod.centro.x : mod.centro.x, mod.centro.y, atras ? 50f : -50f);
                    Vector3 frente;
                    if (mc.Raycast(new Ray(desde, hacia), out RaycastHit h, 200f))
                    {
                        frente = h.point - hacia * 0.03f;
                    }
                    else
                    {
                        Bounds b = f.GetComponent<Renderer>().bounds;
                        frente = new Vector3(desde.x, desde.y, atras ? b.max.z + 0.03f : b.min.z - 0.03f);
                    }
                    Vector3 escala = f.transform.lossyScale;
                    medidas.Add(new Medida
                    {
                        Malla = f.sharedMesh,
                        Local = f.transform.InverseTransformPoint(frente),
                        // El quad de Unity mira a su -Z: con el giro, mira hacia afuera de la pantalla.
                        Giro = Quaternion.Inverse(f.transform.rotation) * Quaternion.LookRotation(hacia, Vector3.up),
                        Tam = new Vector2(mod.tam.x / Mathf.Abs(escala.x), mod.tam.y / Mathf.Abs(escala.y)),
                        Vertical = mod.vertical,
                    });
                }
                Object.DestroyImmediate(g);
            }
            Informe.AppendLine("Pantallas medidas: " + medidas.Count);
            return medidas;
        }

        // Las CY02 son de pared: si están lejos de un muro, se pegan al más cercano (a 2.6 m de alto, de espaldas al muro).
        private static void PegarAlMuro()
        {
            Physics.SyncTransforms();
            int movidas = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("CY02_PantallaVertical") && t.parent != null && !t.parent.name.StartsWith("CY02")).ToList())
            {
                Vector3 origen = new Vector3(t.position.x, 2.6f, t.position.z);
                RaycastHit mejor = default;
                bool hay = false;
                foreach (Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    foreach (RaycastHit h in Physics.RaycastAll(origen, d, 7f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (h.collider.transform.IsChildOf(t) || h.collider.bounds.size.y < 3f)
                        {
                            continue;
                        }
                        if (!hay || h.distance < mejor.distance)
                        {
                            mejor = h;
                            hay = true;
                        }
                    }
                }
                if (!hay || mejor.distance < 0.8f)
                {
                    continue;
                }
                Vector3 normal = new Vector3(mejor.normal.x, 0f, mejor.normal.z).normalized;
                // El frente del modelo es -Z: mira hacia afuera del muro; la espalda (0.45 m de fondo) toca el muro.
                t.rotation = Quaternion.LookRotation(-normal, Vector3.up);
                t.position = new Vector3(mejor.point.x, 1.2f, mejor.point.z) + normal * 0.5f;
                movidas++;
            }
            Informe.AppendLine("Pantallas CY02 pegadas a su muro: " + movidas);
        }
    }
}
