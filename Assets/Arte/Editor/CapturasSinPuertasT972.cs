using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-972: capturas de lado (por la izquierda, a 5 m, 1280x720) de V01a, V01b, V05 y la patrulla sin puertas, en docs/capturas/t972/.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CapturasSinPuertasT972.Capturar
    /// </summary>
    public static class CapturasSinPuertasT972
    {
        private static readonly (string, string)[] Carros =
        {
            ("v01a-lado", "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01a.prefab"),
            ("v01b-lado", "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01b.prefab"),
            ("v05-lado", "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab"),
            ("patrulla-lado", "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab"),
        };

        [MenuItem("Mecha Cyberpunk/T-972/Capturar carros sin puertas")]
        public static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArteT250.PonerLuz();
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.localScale = new Vector3(3f, 1f, 3f);
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            camara.fieldOfView = 35f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            Directory.CreateDirectory("docs/capturas/t972");
            foreach ((string nombre, string ruta) in Carros)
            {
                GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
                Bounds b = ArteT250.Limites(carro);
                camara.transform.position = new Vector3(b.center.x - 5f, b.center.y + 0.3f, b.center.z);
                camara.transform.LookAt(b.center);
                RenderTexture rt = new RenderTexture(1280, 720, 24);
                camara.targetTexture = rt;
                camara.Render();
                RenderTexture.active = rt;
                Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                foto.Apply();
                File.WriteAllBytes("docs/capturas/t972/" + nombre + ".png", foto.EncodeToPNG());
                camara.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(foto);
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(carro);
            }
        }
    }
}
