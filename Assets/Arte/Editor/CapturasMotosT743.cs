using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>T-743: capturas de ajuste de las motos por piezas en Logs/t743-*. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CapturasMotosT743.Capturar</summary>
    public static class CapturasMotosT743
    {
        public const string Carpeta = "Logs/t743-";

        [MenuItem("Mecha Cyberpunk/T-743/Capturar motos")]
        public static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArteT250.PonerLuz();
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.localScale = new Vector3(3f, 1f, 3f);
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            foreach (string codigo in new[] { "V07", "V08", "C19" })
            {
                GameObject moto = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab"));
                Bounds b = ArteT250.Limites(moto);
                camara.orthographic = true;
                camara.orthographicSize = 1.2f;
                camara.nearClipPlane = 0.1f;
                camara.farClipPlane = 100f;
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.transform.position = new Vector3(-10f, 1f, 0f);
                camara.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                ArteT250.Guardar(camara, Carpeta + codigo + "-lado.png");
                camara.transform.position = new Vector3(0f, 10f, 0f);
                camara.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                ArteT250.Guardar(camara, Carpeta + codigo + "-cenital.png");
                camara.orthographic = false;
                ArteT250.Encuadrar(camara, b, new Vector3(-1f, 0.35f, 1f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-3-4.png");
                Posar(moto);
                ArteT250.Encuadrar(camara, b, new Vector3(-1f, 0.3f, 1.4f));
                ArteT250.Guardar(camara, Carpeta + codigo + "-doblada.png");
                Object.DestroyImmediate(moto);
            }
        }

        /// <summary>Delantera doblada a la derecha y llantas giradas.</summary>
        public static void Posar(GameObject moto)
        {
            SerializedObject sr = new SerializedObject(moto.GetComponent<RuedasVisualesCarro>());
            foreach (string lista in new[] { "direccionDelantera", "giro" })
            {
                SerializedProperty p = sr.FindProperty(lista);
                for (int i = 0; i < p.arraySize; i++)
                {
                    Transform t = (Transform)p.GetArrayElementAtIndex(i).objectReferenceValue;
                    t.localRotation = lista == "giro" ? Quaternion.Euler(40f, 0f, 0f) : Quaternion.Euler(0f, 28f, 0f);
                }
            }
        }
    }
}
