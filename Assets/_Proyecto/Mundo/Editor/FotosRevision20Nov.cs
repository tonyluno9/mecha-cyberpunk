using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Revisión 20-nov: fotos de los lugares que cambiaron las tareas de escena (edificios de las afueras, parque, armería, charcos).
    /// Abre Ciudad_Prologo sin guardar y escribe docs/capturas/_20nov/*.png.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.FotosRevision20Nov.Tomar
    /// </summary>
    public static class FotosRevision20Nov
    {
        private const string Escena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "docs/capturas/_20nov";

        public static void Tomar()
        {
            EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
            Directory.CreateDirectory(Carpeta);
            Foto("afueras-salida2", "T530_Afueras/Salida_2", new Vector3(0f, 18f, -55f), 0);
            Foto("afueras-salida4", "T530_Afueras/Salida_4", new Vector3(0f, 18f, -55f), 0);
            Foto("parque-k09", "Parque_T1499", new Vector3(14f, 9f, -14f), 0);
            Foto("parque-k09-arriba", "Parque_T1499", new Vector3(0.1f, 30f, 0f), 0);
            Foto("charco", "Charco", new Vector3(2.5f, 1.6f, -2.5f), 0);
            Foto("armeria", "Armeria", new Vector3(0f, 2f, -9f), 0);
            Foto("armeria-dentro", "Armeria", new Vector3(0f, 1.7f, 0f), 1);
        }

        // modo 0: mira al objeto desde un desfase; modo 1: desde el centro mirando hacia +Z del objeto.
        private static void Foto(string nombre, string ruta, Vector3 desfase, int modo)
        {
            Transform t = Buscar(ruta);
            if (t == null)
            {
                Debug.LogWarning("[Fotos20nov] no encontré " + ruta);
                return;
            }
            Bounds b = Limites(t);
            var go = new GameObject("CamFotos20nov");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.farClipPlane = 1500f;
            if (modo == 0)
            {
                go.transform.position = b.center + desfase;
                go.transform.LookAt(b.center);
            }
            else
            {
                go.transform.position = b.center + Vector3.up * (desfase.y - b.extents.y);
                go.transform.rotation = Quaternion.LookRotation(t.forward);
            }
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Carpeta, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log("[Fotos20nov] " + nombre + " de " + ruta + " centro " + b.center + " tamaño " + b.size);
        }

        private static Transform Buscar(string ruta)
        {
            GameObject g = GameObject.Find(ruta);
            if (g != null)
            {
                return g.transform;
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name.StartsWith(ruta))
                {
                    return t;
                }
            }
            return null;
        }

        private static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return new Bounds(t.position, Vector3.one);
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
