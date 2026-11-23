using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>Ayudas de verificación de la noche del 23-nov (sesión A): captura con HUD y búsquedas por tipo. Solo editor.</summary>
    public static class Verif23
    {
        public static string Captura(string ruta, bool hud)
        {
            Camera cam = Camera.main;
            if (cam == null) { return "sin camara"; }
            Canvas[] cs = hud ? Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray() : new Canvas[0];
            foreach (Canvas c in cs) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 1f; }
            Canvas.ForceUpdateCanvases();
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            RenderTexture.active = rt;
            Texture2D t = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            t.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.Destroy(t); Object.Destroy(rt);
            foreach (Canvas c in cs) { c.renderMode = RenderMode.ScreenSpaceOverlay; }
            return "ok " + ruta + " canvases " + cs.Length + " cam " + cam.name;
        }

        public static string Tipos(string parte, int max)
        {
            StringBuilder sb = new StringBuilder();
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.GetType().Name.Contains(parte)).Take(max))
            {
                sb.AppendLine(m.GetType().Name + " " + m.name + " " + m.transform.position.ToString("F1") + " act=" + m.gameObject.activeInHierarchy);
            }
            return sb.ToString();
        }

        public static string Comp(string objeto)
        {
            GameObject g = GameObject.Find(objeto);
            if (g == null) { return "no existe " + objeto; }
            StringBuilder sb = new StringBuilder(g.name + " " + g.transform.position.ToString("F2") + " padre=" + (g.transform.parent ? g.transform.parent.name : "-") + "\n");
            foreach (Component c in g.GetComponents<Component>()) { sb.AppendLine(" - " + (c ? c.GetType().Name : "null")); }
            return sb.ToString();
        }
    
        public static string Props(string tipo)
        {
            MonoBehaviour m = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.GetType().Name == tipo);
            if (m == null) { return "sin " + tipo; }
            StringBuilder sb = new StringBuilder(m.name + ": ");
            foreach (var f in m.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            {
                try { sb.Append(f.Name + "=" + f.GetValue(m) + "; "); } catch { }
            }
            foreach (var f in m.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                try { if (f.GetIndexParameters().Length == 0) { sb.Append(f.Name + "=" + f.GetValue(m) + "; "); } } catch { }
            }
            return sb.ToString();
        }
    }
}
