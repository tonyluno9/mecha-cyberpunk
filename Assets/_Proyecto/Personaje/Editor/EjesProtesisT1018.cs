using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>T-1018: renderiza el .fbx crudo de una prótesis desde +Z, +X y +Y (ejes de Unity) para decidir cómo va en el cuerpo. Solo para revisar.</summary>
    public static class EjesProtesisT1018
    {
        public static void Render()
        {
            foreach (string ruta in new[] { "Assets/Arte/Protesis/IMP05_HombreraMisiles/IMP05_HombreraMisiles.fbx", "Assets/Arte/Protesis/IMP04_BrazoLaser/IMP04_BrazoLaser.fbx" })
            {
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (fbx == null) { Debug.Log("[T1018ejes] falta " + ruta); continue; }
                GameObject g = Object.Instantiate(fbx);
                Bounds b = new Bounds(g.transform.position, Vector3.zero);
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                Debug.Log("[T1018ejes] " + fbx.name + " centro " + b.center.ToString("F3") + " tamaño " + b.size.ToString("F3"));
                GameObject luz = new GameObject("Luz"); Light l = luz.AddComponent<Light>(); l.type = LightType.Directional; luz.transform.rotation = Quaternion.Euler(40f, 30f, 0f);
                Camera cam = new GameObject("Cam").AddComponent<Camera>();
                cam.orthographic = true; cam.orthographicSize = Mathf.Max(b.size.x, b.size.y, b.size.z) * 0.65f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.4f, 0.5f, 0.7f);
                foreach ((string nombre, Vector3 dir, Vector3 arriba) v in new[] { ("desdeZmas", Vector3.forward, Vector3.up), ("desdeXmas", Vector3.right, Vector3.up), ("desdeYmas", Vector3.up, Vector3.forward) })
                {
                    cam.transform.position = b.center + v.dir * 5f;
                    cam.transform.rotation = Quaternion.LookRotation(-v.dir, v.arriba);
                    RenderTexture rt = new RenderTexture(512, 512, 24);
                    cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                    Texture2D t = new Texture2D(512, 512, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); t.Apply();
                    Directory.CreateDirectory("Logs/ejes");
                    File.WriteAllBytes("Logs/ejes/" + fbx.name + "-" + v.nombre + ".png", t.EncodeToPNG());
                    RenderTexture.active = null; cam.targetTexture = null;
                }
                Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(luz); Object.DestroyImmediate(g);
            }
        }
    }
}
