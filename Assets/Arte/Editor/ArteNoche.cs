using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-419: los 39 modelos de la noche (plan-noche, paso 4) con el flujo de T-250: Assets/Arte/&lt;Zona&gt;/&lt;Código&gt;/, prefab con hijo
    /// Visual y pivote en la base, material URP Lit, frente en -Z. Escena Escenas/Tony/Pruebas_AssetsNoche.unity con una fila por zona.
    /// </summary>
    public static class ArteNoche
    {
        public const string RutaEscenaPruebas = "Assets/_Proyecto/Escenas/Tony/Pruebas_AssetsNoche.unity";

        /// <summary>Medidas creíbles (m): puerta 2.1, persona 1.8.</summary>
        public static readonly ArteT250.Modelo[] Modelos =
        {
            new ArteT250.Modelo("A08_Marcador", ArteT250.Eje.Alto, 3f),
            new ArteT250.Modelo("A11a_PortalSotano", ArteT250.Eje.Alto, 3.6f),
            new ArteT250.Modelo("A11b_EscaleraSotano", ArteT250.Eje.Alto, 2.4f),
            new ArteT250.Modelo("C01_FachadaLocal_cy", ArteT250.Eje.Alto, 4f),
            new ArteT250.Modelo("C02_FachadaVivienda_cy", ArteT250.Eje.Alto, 3.5f),
            new ArteT250.Modelo("C03_FachadaPisoAlto_cy", ArteT250.Eje.Alto, 3.2f),
            new ArteT250.Modelo("C13_PuestoCiber", ArteT250.Eje.Alto, 2.6f),
            new ArteT250.Modelo("C16_Tambo", ArteT250.Eje.Alto, 0.9f),
            new ArteT250.Modelo("C17_Huacales", ArteT250.Eje.Alto, 0.9f),
            new ArteT250.Modelo("C18_BarreraCalle", ArteT250.Eje.Alto, 1f),
            new ArteT250.Modelo("C19_Moto", ArteT250.Eje.Largo, 2f),
            new ArteT250.Modelo("C20a_PisoVentana_cy", ArteT250.Eje.Alto, 3f),
            new ArteT250.Modelo("C20b_PisoDosVentanas_cy", ArteT250.Eje.Alto, 3f),
            new ArteT250.Modelo("C20d_Macetas", ArteT250.Eje.Alto, 3f),
            new ArteT250.Modelo("C21_PisoLiso_cy", ArteT250.Eje.Alto, 3f),
            new ArteT250.Modelo("C22_Cornisa_cy", ArteT250.Eje.Alto, 0.8f),
            new ArteT250.Modelo("C23_Franja", ArteT250.Eje.Alto, 0.6f),
            new ArteT250.Modelo("CY01_ProyectorHolo", ArteT250.Eje.Alto, 2.4f),
            new ArteT250.Modelo("CY02_PantallaVertical", ArteT250.Eje.Alto, 3.5f),
            new ArteT250.Modelo("CY03_MazoCables", ArteT250.Eje.Alto, 2.5f),
            new ArteT250.Modelo("CY04_AntenaAzotea", ArteT250.Eje.Alto, 4f),
            new ArteT250.Modelo("CY05_ServidorCallejero", ArteT250.Eje.Alto, 1.8f),
            new ArteT250.Modelo("CY06_Expendedora", ArteT250.Eje.Alto, 2f),
            new ArteT250.Modelo("CY07a_PlataformaDron", ArteT250.Eje.Alto, 1.2f),
            new ArteT250.Modelo("CY07b_Dron", ArteT250.Eje.Largo, 1.8f),
            new ArteT250.Modelo("CY08_CamaraCorporativa", ArteT250.Eje.Alto, 0.8f),
            new ArteT250.Modelo("CY09_PuestoImplantes", ArteT250.Eje.Alto, 2.6f),
            new ArteT250.Modelo("CY10_CargadorCalle", ArteT250.Eje.Alto, 1.6f),
            new ArteT250.Modelo("E01_EntradaCasino", ArteT250.Eje.Alto, 5f),
            new ArteT250.Modelo("E02_PortonMercadoNegro", ArteT250.Eje.Alto, 4f),
            new ArteT250.Modelo("E03_PantallaGigante", ArteT250.Eje.Alto, 8f),
            new ArteT250.Modelo("H06_TeleVieja", ArteT250.Eje.Alto, 1f),
            new ArteT250.Modelo("H07_Sofa", ArteT250.Eje.Alto, 0.9f),
            new ArteT250.Modelo("H08_EstufaTarja", ArteT250.Eje.Alto, 0.95f),
            new ArteT250.Modelo("H09_RepisaVeladoras", ArteT250.Eje.Alto, 1.2f),
            new ArteT250.Modelo("H11_Buro", ArteT250.Eje.Alto, 0.6f),
            new ArteT250.Modelo("T06_Soldadora", ArteT250.Eje.Alto, 1f),
            new ArteT250.Modelo("T07_GruaMotor", ArteT250.Eje.Alto, 2.1f),
            new ArteT250.Modelo("T08_RepisaTaller", ArteT250.Eje.Alto, 1.9f),
        };

        /// <summary>Busca en los modelos de la noche y, si no, en los de T-250.</summary>
        public static ArteT250.Modelo Buscar(string codigo)
        {
            return Modelos.FirstOrDefault(m => m.Codigo == codigo) ?? ArteT250.Buscar(codigo);
        }

        /// <summary>Batchmode: crea materiales y prefabs de los 39, la escena de pruebas y sus capturas (docs/capturas/noche-fila-*.png).</summary>
        [MenuItem("Mecha Cyberpunk/T-419/Crear prefabs y Pruebas_AssetsNoche")]
        public static void CrearTodo()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (ArteT250.Modelo modelo in Modelos)
            {
                informe.AppendLine(ArteT250.CrearPrefab(modelo));
            }
            File.WriteAllText("Logs/noche-prefabs.txt", informe.ToString());
            AssetDatabase.SaveAssets();
            CrearEscenaPruebas();
            CapturarPruebas();
            Debug.Log("[Arte] T-419 prefabs:\n" + informe);
        }

        private static void CrearEscenaPruebas()
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float z = 0f;
            float anchoTotal = 0f;
            foreach (string zona in ArteT250.Zonas)
            {
                ArteT250.Modelo[] fila = Modelos.Where(m => m.Zona == zona).ToArray();
                for (int parte = 0; parte * 10 < fila.Length; parte++)
                {
                    GameObject grupo = new GameObject("Fila_" + zona + "_" + parte);
                    grupo.transform.position = new Vector3(0f, 0f, z);
                    float x = 0f;
                    foreach (ArteT250.Modelo modelo in fila.Skip(parte * 10).Take(10))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaPrefab);
                        if (prefab == null)
                        {
                            continue;
                        }
                        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                        instancia.transform.SetParent(grupo.transform, false);
                        Bounds b = ArteT250.Limites(instancia);
                        instancia.transform.localPosition = new Vector3(x + b.size.x * 0.5f, 0f, 0f);
                        x += b.size.x + 1.5f;
                    }
                    // Referencia de escala: una persona de 1.8 m al final de la fila.
                    GameObject persona = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    persona.name = "Persona_1.8m";
                    persona.transform.SetParent(grupo.transform, false);
                    persona.transform.localPosition = new Vector3(x + 0.5f, 0.9f, 0f);
                    persona.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                    anchoTotal = Mathf.Max(anchoTotal, x);
                    z -= 20f;
                }
            }
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.position = new Vector3(anchoTotal * 0.5f, 0f, z * 0.5f);
            suelo.transform.localScale = new Vector3(anchoTotal * 0.1f + 4f, 1f, -z * 0.1f + 4f);
            ArteT250.PonerLuz();
            GameObject camaraObjeto = new GameObject("Camara");
            camaraObjeto.tag = "MainCamera";
            camaraObjeto.AddComponent<Camera>();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaEscenaPruebas));
            EditorSceneManager.SaveScene(escena, RutaEscenaPruebas);
        }

        private static void CapturarPruebas()
        {
            EditorSceneManager.OpenScene(RutaEscenaPruebas, OpenSceneMode.Single);
            Camera camara = Camera.main;
            foreach (GameObject fila in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.parent == null && t.name.StartsWith("Fila_")).Select(t => t.gameObject))
            {
                ArteT250.Encuadrar(camara, ArteT250.Limites(fila), new Vector3(0.1f, 0.2f, -1f));
                ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "noche-" + fila.name.ToLowerInvariant().Replace('_', '-') + ".png"));
            }
        }
    }
}
