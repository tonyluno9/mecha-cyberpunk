using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 6-nov tarde (docs/playtest-06nov-b.md): ayudas de fotos para revisar assets antes de meterlos a la ciudad.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2t.FotoCarrosNuevos
    /// </summary>
    public static class ColocarS2t
    {
        public const string CarpetaCapturas = "docs/capturas/";

        /// <summary>V09 y V10 (y los de referencia V02/V03) de frente a 3/4 y de lado, en docs/capturas/s2t-carros-nuevos.png.</summary>
        [MenuItem("Mecha Cyberpunk/S2t/Foto de los carros nuevos")]
        public static void FotoCarrosNuevos()
        {
            FotoPrefabs(new[]
            {
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V02.prefab",
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V09.prefab",
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V03.prefab",
                "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V10.prefab",
            }, CarpetaCapturas + "s2t-carros-nuevos.png");
        }

        /// <summary>
        /// Pone cada prefab en fila mirando a +Z (su frente, si está bien orientado) con una flecha roja hacia +Z en el piso y lo retrata desde
        /// adelante a 3/4. Arriba: vista desde el frente-derecha; abajo: de lado. Abre otra vez Ciudad_Prologo al terminar.
        /// </summary>
        public static void FotoPrefabs(string[] rutas, string salida)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sol = new GameObject("Sol");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.4f;
            sol.transform.rotation = Quaternion.Euler(45f, 200f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = Vector3.one * 10f;
            const float Paso = 7f;
            for (int i = 0; i < rutas.Length; i++)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(rutas[i]);
                if (p == null)
                {
                    Debug.LogWarning("[Arte] Falta " + rutas[i]);
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(p);
                g.transform.position = new Vector3(i * Paso, 0f, 0f);
                GameObject flecha = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flecha.transform.position = new Vector3(i * Paso, 0.02f, 3.6f);
                flecha.transform.localScale = new Vector3(0.3f, 0.04f, 1.2f);
                Material rojo = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.red };
                flecha.GetComponent<Renderer>().sharedMaterial = rojo;
            }
            float centro = (rutas.Length - 1) * Paso * 0.5f;
            Texture2D arriba = Retratar(new Vector3(centro + 6f, 6f, 16f), new Vector3(centro, 0.8f, 0f));
            Texture2D abajo = Retratar(new Vector3(centro, 3f, -22f), new Vector3(centro, 1f, 0f));
            Texture2D hoja = new Texture2D(1280, 1440, TextureFormat.RGB24, false);
            hoja.SetPixels(0, 720, 1280, 720, arriba.GetPixels());
            hoja.SetPixels(0, 0, 1280, 720, abajo.GetPixels());
            hoja.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(salida));
            File.WriteAllBytes(salida, hoja.EncodeToPNG());
            Debug.Log("[Arte] Foto en " + salida);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }


        private static Texture2D Retratar(Vector3 desde, Vector3 hacia)
        {
            GameObject camObj = new GameObject("Cam");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<UniversalAdditionalCameraData>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.22f, 0.26f);
            cam.fieldOfView = 50f;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(camObj);
            return foto;
        }
    }
}
