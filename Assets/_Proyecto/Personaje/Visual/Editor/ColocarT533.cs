using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-533 (plan de la noche 3-nov, paso 1: calor, policía y guardias). Idempotente:
    /// - Persecución: una patrulla desde el nivel 1 de calor (2 y 3 después), más rápida (15 m/s, como un carro del jugador).
    /// - Robar una patrulla (V05, V06, V08) sube el calor al nivel 2 de golpe.
    /// - Guardias de Nova: patrullan su tramo de banqueta, te vigilan y reaccionan (<see cref="GuardiaNova"/>).
    /// - Los SKULLS (pandilla Calaveras) usan el modelo SkullHombre: pandilleros armados y los dos de puños de la calle de tu casa.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ColocarT533.Colocar
    /// </summary>
    public static class ColocarT533
    {
        public const string RutaSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaConfigPersecucion = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const float CalorRoboPatrulla = 55f;
        private const float TramoGuardia = 10f;

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-533/Policía, guardias y SKULLS")]
        public static void Colocar()
        {
            Informe.Clear();
            Persecucion();
            Patrullas();
            CrearPrefabSkulls();
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(ConectarVisualesT520.RutaEscena, OpenSceneMode.Single);
            Guardias();
            Skulls();
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t533-colocar.txt", Informe.ToString());
            Debug.Log("[Personaje] T-533 listo:\n" + Informe);
        }

        private static void Persecucion()
        {
            SerializedObject so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(RutaConfigPersecucion));
            Lista(so.FindProperty("dronesPorNivel"), 1, 1, 2);
            Lista(so.FindProperty("patrullasPorNivel"), 1, 2, 3);
            so.FindProperty("velocidadPatrulla").floatValue = 15f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Informe.AppendLine("Persecución: patrullas 1/2/3 y drones 1/1/2 por nivel; patrulla a 15 m/s");
        }

        private static void Lista(SerializedProperty p, params int[] valores)
        {
            p.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
            {
                p.GetArrayElementAtIndex(i).intValue = valores[i];
            }
        }

        private static void Patrullas()
        {
            foreach (string codigo in new[] { "V05", "V06", "V08" })
            {
                string ruta = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab";
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                Component estacionado = raiz.GetComponent("CarroEstacionado");
                if (estacionado != null)
                {
                    SerializedObject so = new SerializedObject(estacionado);
                    so.FindProperty("calorAlRobar").floatValue = CalorRoboPatrulla;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Informe.AppendLine("Robar una patrulla suma " + CalorRoboPatrulla + " de calor (nivel 2)");
        }

        private static void CrearPrefabSkulls()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ConectarVisualesT520.RutaPandillero);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                ConectarVisualesT520.Reemplazar(contenido, viejo, ConectarVisualesT520.Visual("SkullHombre"), "Modelo", false);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaSkulls);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void Guardias()
        {
            Transform raiz = GameObject.Find("T510_Guardias")?.transform;
            if (raiz == null)
            {
                Informe.AppendLine("FALTA T510_Guardias");
                return;
            }
            Physics.SyncTransforms();
            System.Collections.Generic.List<(Vector3 a, Vector3 b)> usados = new System.Collections.Generic.List<(Vector3, Vector3)>();
            int n = 0;
            foreach (Transform g in raiz)
            {
                GenteDeBanqueta gente = g.GetComponent<GenteDeBanqueta>();
                if (gente == null)
                {
                    continue;
                }
                // El tramo libre de banqueta de Neón Alto más cercano a donde estaba (a lo largo de la calle, lejos del Casino y de los otros guardias).
                Vector3 donde = gente.Modo == ModoGente.Caminar ? gente.PuntoA : g.position;
                (Vector3 a, Vector3 b) mejor = (donde, donde);
                float mejorDistancia = float.MaxValue;
                foreach (float x in new[] { 38f, 38.5f, 39f, 55f, 55.5f, 56f })
                {
                    for (float z = 12f; z <= 84f; z += 1f)
                    {
                        foreach (float sentido in new[] { 1f, -1f })
                        foreach (float tramo in new[] { TramoGuardia, 8f, 6f })
                        {
                            Vector3 a = new Vector3(x, 0f, z);
                            if (!Physics.Raycast(a + Vector3.up * 3f, Vector3.down, out RaycastHit suelo, 4f, ~0, QueryTriggerInteraction.Ignore))
                            {
                                continue;
                            }
                            a.y = suelo.point.y;
                            Vector3 b = a + Vector3.forward * (sentido * tramo);
                            if (b.z < 12f || b.z > 84f || LejosDelCasino(a) < 8f || LejosDelCasino(b) < 8f
                                || usados.Any(u => Mathf.Abs(u.a.x - x) < 2f && Mathf.Min(Mathf.Max(u.a.z, u.b.z), Mathf.Max(a.z, b.z)) > Mathf.Max(Mathf.Min(u.a.z, u.b.z), Mathf.Min(a.z, b.z)) - 3f))
                            {
                                continue;
                            }
                            float d = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(donde.x, 0f, donde.z)) + (TramoGuardia - tramo) * 3f;
                            if (d < mejorDistancia && TramoLibre(a, b))
                            {
                                mejorDistancia = d;
                                mejor = (a, b);
                            }
                        }
                    }
                }
                if (mejorDistancia == float.MaxValue)
                {
                    Informe.AppendLine("  " + g.name + ": sin tramo libre");
                    continue;
                }
                usados.Add(mejor);
                g.position = mejor.a;
                gente.Configurar(ModoGente.Caminar, 0.15f * n, 1f, null, mejor.a, mejor.b, 1.1f, 3f, g);
                if (g.GetComponent<GuardiaNova>() == null)
                {
                    g.gameObject.AddComponent<GuardiaNova>();
                }
                EditorUtility.SetDirty(g.gameObject);
                Informe.AppendLine("  " + g.name + ": de " + mejor.a + " a " + mejor.b);
                n++;
            }
            Informe.AppendLine("Guardias de Nova que patrullan, vigilan y reaccionan: " + n);
        }

        // Distancia a la ruta calle -> puerta del Casino -> EL HACKER (la prueba de T-510 pide más de 7 m).
        private static float LejosDelCasino(Vector3 p)
        {
            Vector3 a = new Vector3(47f, 0f, 47f);
            Vector3 b = new Vector3(74.5f, 0f, 47f);
            p.y = 0f;
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Mathf.Min(Vector3.Distance(p, a + ab * t), Vector3.Distance(p, new Vector3(57.5f, 0f, 47f)));
        }

        private static bool TramoLibre(Vector3 a, Vector3 b)
        {
            Physics.SyncTransforms();
            float largo = Vector3.Distance(a, b);
            for (float d = 0f; d <= largo + 0.01f; d += 0.25f)
            {
                Vector3 p = Vector3.Lerp(a, b, d / largo);
                if (!Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit piso, 4f, ~0, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(piso.point.y - a.y) > 0.15f)
                {
                    return false;
                }
                if (Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.32f, ~0, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }
            }
            return true;
        }

        private static void Skulls()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkulls);
            Material cuerpo = ConectarVisualesT520.Variante("SkullHombre", "PandilleroSkulls", new Color(0.85f, 0.85f, 0.85f), default);
            int armados = 0;
            int punos = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
            {
                if (t == null)
                {
                    continue; // hijo de un pandillero al que ya se le cambió el prefab
                }
                Component p = t.GetComponent("PandilleroAPie");
                if (p == null || t.name == "Jefe" || !(EsSkulls(p) || t.name.StartsWith("CalaveraPunos")))
                {
                    continue;
                }
                Transform modelo = t.Find("Modelo");
                Animator a = modelo != null ? modelo.GetComponentInChildren<Animator>(true) : null;
                bool yaEs = a != null && a.runtimeAnimatorController != null && AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("/SkullHombre/");
                if (yaEs)
                {
                    continue;
                }
                if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                    && AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)) == ConectarVisualesT520.RutaPandillero)
                {
                    string nombre = t.name;
                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(t.gameObject, prefab, InteractionMode.AutomatedAction);
                    t.name = nombre;
                    modelo = t.Find("Modelo");
                    VarianteVisual v = modelo != null ? modelo.GetComponent<VarianteVisual>() ?? modelo.gameObject.AddComponent<VarianteVisual>() : null;
                    v?.Configurar(new[] { cuerpo }, 0, null);
                    armados++;
                }
                else if (t.Find("Visual") == null && t.Find("Cuerpo") != null)
                {
                    // Pandillero de puños (cápsula): las piezas se quedan (las pinta al recibir golpes) pero no se ven; encima va SkullHombre.
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled = false;
                    }
                    ConectarVisualesT520.ColocarSobreElPiso(t, "SkullHombre", cuerpo, new string[0]);
                    punos++;
                }
                EditorUtility.SetDirty(t.gameObject);
            }
            Informe.AppendLine("SKULLS con el modelo SkullHombre: " + armados + " armados y " + punos + " de puños");
        }

        private static bool EsSkulls(Component pandillero)
        {
            Object datos = new SerializedObject(pandillero).FindProperty("datos").objectReferenceValue;
            return datos != null && datos.name.Contains("Calaveras");
        }
    }
}
