using System.IO;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-742: capturas de los carros armados por piezas (puertas abiertas, ruedas giradas y dobladas). Las fotos de ajuste salen en Logs/.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CapturasCarrosT742.Capturar
    /// </summary>
    public static class CapturasCarrosT742
    {
        public const string Carpeta = "Logs/t742-";
        private const float RodadoPrueba = 40f;
        private const float DireccionPrueba = 28f;

        [MenuItem("Mecha Cyberpunk/T-742/Capturar carros")]
        public static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArteT250.PonerLuz();
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.localScale = new Vector3(3f, 1f, 3f);
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            foreach (string codigo in new[] { "V01a", "V01b", "V05" })
            {
                GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab"));
                Bounds b = ArteT250.Limites(carro);
                ArteT250.Encuadrar(camara, b, new Vector3(-1f, 0.35f, 1f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-cerrado.png");
                Camera ortogonal = camara;
                ortogonal.orthographic = true;
                ortogonal.orthographicSize = 1.5f;
                ortogonal.nearClipPlane = 0.1f;
                ortogonal.farClipPlane = 100f;
                ortogonal.clearFlags = CameraClearFlags.SolidColor;
                foreach (var vista in new[] { ("lado-izq", new Vector3(-10f, 1.0f, 0f), 90f, 0f), ("lado-der", new Vector3(10f, 1.0f, 0f), -90f, 0f), ("cenital", new Vector3(0f, 10f, 0f), 0f, 90f) })
                {
                    ortogonal.transform.position = vista.Item2;
                    ortogonal.transform.rotation = Quaternion.Euler(vista.Item4, vista.Item3, 0f);
                    ArteT250.Guardar(ortogonal, Carpeta + codigo + "-" + vista.Item1 + ".png");
                }
                ortogonal.orthographic = false;
                Posar(carro, true);
                ArteT250.Encuadrar(camara, b, new Vector3(-1f, 0.35f, 1f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-izq-abierta.png");
                ArteT250.Encuadrar(camara, b, new Vector3(1f, 0.35f, 1f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-der-abierta.png");
                ArteT250.Encuadrar(camara, b, new Vector3(0f, 1.6f, 0.2f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-arriba.png");
                ArteT250.Encuadrar(camara, b, new Vector3(0f, 0.25f, 1f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-frente.png");
                // La llanta delantera izquierda doblada y girada, de cerca, con las puertas cerradas.
                Posar(carro, false);
                Vector3 centro = carro.transform.Find("Rueda_DI").position;
                ArteT250.Encuadrar(camara, new Bounds(centro, new Vector3(1.6f, 1.2f, 1.6f)), new Vector3(-1f, 0.15f, 1f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-rueda-doblada.png");
                Object.DestroyImmediate(carro);
            }
        }

        /// <summary>Deja el carro con las puertas abiertas (o cerradas), las ruedas giradas y las delanteras dobladas, para la foto.</summary>
        public static void Posar(GameObject carro, bool abiertas)
        {
            PuertasCarro puertas = carro.GetComponent<PuertasCarro>();
            SerializedObject sp = new SerializedObject(puertas);
            float angulo = abiertas ? puertas.AnguloApertura : 0f;
            ((Transform)sp.FindProperty("puertaIzquierda").objectReferenceValue).localRotation = Quaternion.Euler(0f, angulo, 0f);
            ((Transform)sp.FindProperty("puertaDerecha").objectReferenceValue).localRotation = Quaternion.Euler(0f, -angulo, 0f);
            SerializedObject sr = new SerializedObject(carro.GetComponent<RuedasVisualesCarro>());
            foreach (string lista in new[] { "direccionDelantera", "giro" })
            {
                SerializedProperty p = sr.FindProperty(lista);
                for (int i = 0; i < p.arraySize; i++)
                {
                    Transform t = (Transform)p.GetArrayElementAtIndex(i).objectReferenceValue;
                    t.localRotation = lista == "giro" ? Quaternion.Euler(RodadoPrueba, 0f, 0f) : Quaternion.Euler(0f, DireccionPrueba, 0f);
                }
            }
        }
    }
}
