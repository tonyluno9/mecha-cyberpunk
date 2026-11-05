using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-639: EL HACKER (SERGIO) sentado en una silla frente a una mesa con laptop, a escala de persona de 1.8 m. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ColocarHackerT639.Colocar
    /// </summary>
    public static class ColocarHackerT639
    {
        public const string NombreSet = "Set_Hacker";
        public const float AlturaPersona = 1.8f;
        private const string CarpetaMateriales = "Assets/_Proyecto/Personaje/Visual/Materiales";

        [MenuItem("Mecha Cyberpunk/T-639/Hacker sentado")]
        public static void Colocar()
        {
            EditorSceneManager.OpenScene(ConectarVisualesT520.RutaEscena, OpenSceneMode.Single);
            Transform raiz = null;
            foreach (MonoBehaviour apoyo in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (apoyo != null && apoyo.GetType().Name == "PersonajeApoyo")
                {
                    Object datos = new SerializedObject(apoyo).FindProperty("datos").objectReferenceValue;
                    if (datos != null && new SerializedObject(datos).FindProperty("id").stringValue == "hacker")
                    {
                        raiz = apoyo.transform;
                    }
                }
            }
            if (raiz == null)
            {
                throw new System.Exception("T-639: no hay PersonajeApoyo con id hacker en la escena");
            }
            VisualPersonaje visual = raiz.GetComponentInChildren<VisualPersonaje>(true);
            Animator animador = visual.GetComponentInChildren<Animator>(true);

            // Escala: la altura de los renderers del modelo en reposo debe ser de 1.8 m.
            Bounds caja = default;
            bool primero = true;
            foreach (Renderer r in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (primero)
                {
                    caja = r.bounds;
                    primero = false;
                }
                else
                {
                    caja.Encapsulate(r.bounds);
                }
            }
            if (!primero && caja.size.y > 0.1f)
            {
                float factor = AlturaPersona / caja.size.y;
                Debug.Log("T-639: altura del modelo " + caja.size.y + " m, factor " + factor);
                if (Mathf.Abs(factor - 1f) > 0.03f)
                {
                    visual.transform.localScale *= factor;
                }
            }

            if (animador.GetComponent<PoseSentado>() == null)
            {
                animador.gameObject.AddComponent<PoseSentado>();
            }

            Transform viejo = raiz.Find(NombreSet);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Transform set = new GameObject(NombreSet).transform;
            set.SetParent(raiz, false);
            set.SetPositionAndRotation(visual.transform.position, visual.transform.rotation);
            Material madera = CrearMaterial("Mat_Hacker_Madera", new Color(0.18f, 0.14f, 0.12f), null);
            Material metal = CrearMaterial("Mat_Hacker_Metal", new Color(0.08f, 0.08f, 0.1f), null);
            Material pantalla = CrearMaterial("Mat_Hacker_Pantalla", new Color(0.02f, 0.1f, 0.12f), new Color(0.1f, 1f, 0.8f) * 2f);
            // Silla: asiento a 0.45 m, respaldo detrás, pie central.
            Cubo("Silla_Asiento", set, new Vector3(0f, 0.43f, -0.05f), new Vector3(0.46f, 0.05f, 0.46f), madera);
            Cubo("Silla_Respaldo", set, new Vector3(0f, 0.72f, -0.3f), new Vector3(0.46f, 0.5f, 0.04f), madera);
            Cubo("Silla_Pie", set, new Vector3(0f, 0.22f, -0.05f), new Vector3(0.06f, 0.4f, 0.06f), metal);
            Cubo("Silla_Base", set, new Vector3(0f, 0.02f, -0.05f), new Vector3(0.5f, 0.04f, 0.5f), metal);
            // Mesa: cubierta a 0.75 m, un poco más lejos que las rodillas.
            Cubo("Mesa_Cubierta", set, new Vector3(0f, 0.74f, 0.8f), new Vector3(1.3f, 0.05f, 0.7f), madera);
            foreach (Vector3 s in new[] { new Vector3(-0.6f, 0.37f, 0.5f), new Vector3(0.6f, 0.37f, 0.5f), new Vector3(-0.6f, 0.37f, 1.1f), new Vector3(0.6f, 0.37f, 1.1f) })
            {
                Cubo("Mesa_Pata", set, s, new Vector3(0.05f, 0.74f, 0.05f), metal);
            }
            // Laptop: base con teclado y pantalla inclinada que ve al hacker.
            Cubo("Laptop_Base", set, new Vector3(0f, 0.785f, 0.7f), new Vector3(0.36f, 0.02f, 0.25f), metal);
            Transform tapa = Cubo("Laptop_Pantalla", set, new Vector3(0f, 0.92f, 0.83f), new Vector3(0.36f, 0.24f, 0.015f), pantalla);
            tapa.localRotation = Quaternion.Euler(-12f, 0f, 0f);

            AssetDatabase.SaveAssets();
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("T-639: hacker sentado en " + raiz.position);
        }

        private static Transform Cubo(string nombre, Transform padre, Vector3 pos, Vector3 tam, Material material)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = tam;
            g.GetComponent<Renderer>().sharedMaterial = material;
            return g.transform;
        }

        private static Material CrearMaterial(string nombre, Color color, Color? emision)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Personaje/Visual", "Materiales");
            }
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            if (emision.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emision.Value);
            }
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
