using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-532 (plan de la noche 3-nov, paso 1: tráfico, motos, manejo y patrullas). Idempotente:
    /// - Manejo: freno firme y el carro se detiene solo al soltar el acelerador (más resistencia), menos derrape de lado.
    /// - Cada carro dice su modelo en el HUD ("Sedán viejo", "Patrulla"...), ya no "Carro viejo" para todos.
    /// - Motos: Carro_C19 (la moto del kit de calle) se maneja y se roba; las motos de adorno de la ciudad pasan a ser esa moto.
    /// - Tráfico civil sin patrullas (V05, V06 y V08 son de la policía).
    /// - La patrulla de la persecución (cubos con barra de luces) se ve con el modelo V05.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.ColocarT532.Colocar
    /// </summary>
    public static class ColocarT532
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaConfig = "Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_Viejo.asset";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaModeloPatrulla = "Assets/Arte/Vehiculos/Prefabs/V05.prefab";
        private const float LargoPatrulla = 4.6f;

        public static readonly Dictionary<string, string> Nombres = new Dictionary<string, string>
        {
            { "V01a", "Carro viejito" }, { "V01b", "Carro deportivo" }, { "V02", "Sedán viejo" }, { "V03", "Pickup de trabajo" },
            { "V04", "Combi vieja" }, { "V05", "Patrulla" }, { "V06", "Patrulla camioneta" }, { "V08", "Moto de patrulla" },
            { "C19", "Moto" },
        };

        public static readonly string[] DeLaPolicia = { "V05", "V06", "V08" };

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-532/Tráfico, motos, manejo y patrullas")]
        public static void Colocar()
        {
            Informe.Clear();
            Manejo();
            Informe.AppendLine(CreadorCarrosT511.CrearUno("C19", "C19_Moto", 0f, 2f));
            NombresEnPrefabs();
            Patrulla();
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            MotosDeAdorno();
            TraficoSinPolicia();
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t532-colocar.txt", Informe.ToString());
            Debug.Log("[Vehiculos] T-532 listo:\n" + Informe);
        }

        private static void Manejo()
        {
            SerializedObject so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(RutaConfig));
            // Freno: 9000 N (8 m/s²) a 12500 N (11 m/s²). Rodadura: 500 N (se deslizaba 35 s) a 900 N (se detiene en ~20 s de 16 m/s sin frenar).
            // Con 1700 N el piloto de RecorridoCarroPruebas ya no daba la vuelta en el cruce (probado): 900 es el máximo que se dejó.
            // T-696: el freno sube a 20000 N y el motor a 8800 N (CalibrarCarrosT696): si se vuelve a correr esto, no se deshace.
            so.FindProperty("fuerzaFreno").floatValue = CalibrarCarrosT696.FuerzaFreno;
            so.FindProperty("fuerzaMotor").floatValue = CalibrarCarrosT696.FuerzaMotor;
            so.FindProperty("resistenciaRodadura").floatValue = 900f;
            so.FindProperty("agarreLateral").floatValue = 11f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Informe.AppendLine("Manejo: freno 20000 N, motor 8800 N, rodadura 900 N, agarre lateral 11");
        }

        private static void NombresEnPrefabs()
        {
            foreach (KeyValuePair<string, string> n in Nombres)
            {
                string ruta = CreadorCarrosT511.RutaPrefab(n.Key);
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                if (raiz == null)
                {
                    continue;
                }
                CarroViejo carro = raiz.GetComponent<CarroViejo>();
                if (carro != null)
                {
                    SerializedObject so = new SerializedObject(carro);
                    so.FindProperty("nombreModelo").stringValue = n.Value;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            // El tráfico robable usa sus propios prefabs (CarroTrafico_<modelo>).
            foreach (string ruta in Directory.GetFiles("Assets/_Proyecto/Trafico/Prefabs", "CarroTrafico_*.prefab"))
            {
                string codigo = Path.GetFileNameWithoutExtension(ruta).Substring("CarroTrafico_".Length);
                if (!Nombres.TryGetValue(codigo, out string nombre))
                {
                    continue;
                }
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                CarroViejo carro = raiz.GetComponent<CarroViejo>();
                if (carro != null)
                {
                    SerializedObject so = new SerializedObject(carro);
                    so.FindProperty("nombreModelo").stringValue = nombre;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Informe.AppendLine("Nombres por modelo en los prefabs de carros y del tráfico");
        }

        private static void Patrulla()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPatrulla);
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloPatrulla);
            Transform viejo = raiz.transform.Find("Visual_T532");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            if (modelo == null)
            {
                Informe.AppendLine("FALTA " + RutaModeloPatrulla);
                PrefabUtility.UnloadPrefabContents(raiz);
                return;
            }
            foreach (string cubo in new[] { "Carroceria", "Cabina", "BarraRoja", "BarraAzul" })
            {
                Renderer r = raiz.transform.Find(cubo)?.GetComponent<Renderer>();
                if (r != null)
                {
                    r.enabled = false;
                }
            }
            GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            PrefabUtility.UnpackPrefabInstance(copia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            GameObject envoltorio = new GameObject("Visual_T532");
            envoltorio.transform.SetParent(raiz.transform, false);
            Transform visual = copia.transform.Find("Visual") ?? copia.transform;
            visual.SetParent(envoltorio.transform, false);
            foreach (Collider c in visual.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            if (copia != null && copia != visual.gameObject)
            {
                Object.DestroyImmediate(copia);
            }
            Bounds b = Limites(envoltorio);
            if (b.size.x > b.size.z * 1.3f)
            {
                envoltorio.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                b = Limites(envoltorio);
            }
            envoltorio.transform.localScale = Vector3.one * (LargoPatrulla / Mathf.Max(0.01f, b.size.z));
            b = Limites(envoltorio);
            envoltorio.transform.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = Limites(envoltorio);
            // Las luces de la torreta van sobre el techo del modelo.
            foreach (string luz in new[] { "LuzRoja", "LuzAzul" })
            {
                Transform l = raiz.transform.Find(luz);
                if (l != null)
                {
                    l.localPosition = new Vector3(l.localPosition.x, b.max.y + 0.25f, l.localPosition.z);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPatrulla);
            PrefabUtility.UnloadPrefabContents(raiz);
            Informe.AppendLine("Patrulla de la persecución con el modelo V05 (" + b.size.x.ToString("F1") + " x " + b.size.y.ToString("F1") + " x " + b.size.z.ToString("F1") + " m)");
        }

        private static Bounds Limites(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.zero);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static void MotosDeAdorno()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosT511.RutaPrefab("C19"));
            Transform raiz = GameObject.Find("T532_Motos")?.transform ?? new GameObject("T532_Motos").transform;
            int n = 0;
            foreach (Transform t in EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.StartsWith("C19_Moto") && t.gameObject.activeSelf && t.root.name != "T532_Motos").ToList())
            {
                GameObject moto = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
                moto.name = "MotoRobable_" + (++n);
                // Estacionada junto a la banqueta, a lo largo de la calle y en el sentido de su carril.
                Vector3 p = t.position;
                Vector3 frente = Mathf.Abs(p.z) < 12f ? (p.z > 0f ? Vector3.left : Vector3.right) : (p.x > 47f ? Vector3.forward : Vector3.back);
                moto.transform.SetPositionAndRotation(new Vector3(p.x, 0.5f, p.z), Quaternion.LookRotation(frente));
                t.gameObject.SetActive(false);
            }
            Informe.AppendLine("Motos de adorno cambiadas por motos manejables y robables: " + n + " (total en T532_Motos: " + raiz.childCount + ")");
        }

        private static void TraficoSinPolicia()
        {
            MonoBehaviour trafico = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(m => m != null && m.GetType().Name == "TraficoCalle");
            if (trafico == null)
            {
                Informe.AppendLine("FALTA TraficoCalle");
                return;
            }
            SerializedObject so = new SerializedObject(trafico);
            SerializedProperty lista = so.FindProperty("prefabsCarro");
            int quitados = 0;
            for (int i = lista.arraySize - 1; i >= 0; i--)
            {
                Object p = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                if (p != null && DeLaPolicia.Any(c => p.name.EndsWith("_" + c)))
                {
                    lista.DeleteArrayElementAtIndex(i);
                    if (i < lista.arraySize && lista.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        lista.DeleteArrayElementAtIndex(i);
                    }
                    quitados++;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Informe.AppendLine("Tráfico civil sin patrullas: " + quitados + " modelos de la policía fuera (quedan " + lista.arraySize + ")");
        }
    }
}
