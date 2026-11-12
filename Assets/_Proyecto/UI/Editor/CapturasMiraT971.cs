using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-971: guarda docs/capturas/t971/pistola-mira-lado.png y pistola-mira-34.png (el prefab de agarre con la mira prendida, fondo gris, 1280x720).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CapturasMiraT971.Capturar
    /// </summary>
    public static class CapturasMiraT971
    {
        public static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            GameObject pistola = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Armas/W01_Pistola/W01_Pistola_agarre.prefab"));
            foreach (Transform t in pistola.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Mira")
                {
                    t.gameObject.SetActive(true);
                }
            }
            GameObject luz = new GameObject("Luz");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.3f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Foto("pistola-mira-lado", pistola, new Vector3(1f, 0.1f, 0f), 0.5f);
            Foto("pistola-mira-34", pistola, new Vector3(0.7f, 0.9f, -0.9f), 0.5f);
            Object.DestroyImmediate(luz);
            Object.DestroyImmediate(pistola);
        }

        private static void Foto(string nombre, GameObject objeto, Vector3 haciaCamara, float distancia)
        {
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
            cam.transform.position = b.center + haciaCamara.normalized * distancia;
            cam.transform.LookAt(b.center);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            RenderTexture.active = null;
            System.IO.Directory.CreateDirectory("docs/capturas/t971");
            System.IO.File.WriteAllBytes("docs/capturas/t971/" + nombre + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam);
        }
    }
}
