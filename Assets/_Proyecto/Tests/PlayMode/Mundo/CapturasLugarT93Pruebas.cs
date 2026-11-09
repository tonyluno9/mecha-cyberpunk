using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Capturas de un lugar (variables T93_LUGAR, T93_PREFIJO y T93_FASE; opcional T93_VISTAS "x,y,z,mx,my,mz;...") y lista de lo que hay en Logs/lugar-dump.txt. Sin T93_LUGAR no hace nada.</summary>
    public class CapturasLugarT93Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        [UnityTest]
        public IEnumerator Lugar()
        {
            string nombre = System.Environment.GetEnvironmentVariable("T93_LUGAR");
            if (string.IsNullOrEmpty(nombre)) { yield break; }
            string prefijo = System.Environment.GetEnvironmentVariable("T93_PREFIJO") ?? "t93";
            string fase = System.Environment.GetEnvironmentVariable("T93_FASE") ?? "antes";
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 90; i++) { yield return null; }
            Transform lugar = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == nombre);
            var dump = new List<string>();
            if (lugar != null)
            {
                dump.Add("lugar " + lugar.position.ToString("F1") + " rot " + lugar.eulerAngles.ToString("F0"));
                foreach (Transform c in lugar) { dump.Add("  hijo " + c.name + " " + c.position.ToString("F1") + (c.gameObject.activeInHierarchy ? "" : " (apagado)")); }
                Bounds caja = new Bounds(lugar.position, Vector3.zero);
                foreach (Renderer r in lugar.GetComponentsInChildren<Renderer>()) { caja.Encapsulate(r.bounds); }
                dump.Add("caja " + caja.center.ToString("F1") + " tam " + caja.size.ToString("F1"));
                var grupos = new SortedDictionary<string, int>();
                foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (!caja.Contains(r.bounds.center)) { continue; }
                    var partes = new List<string>();
                    for (Transform a = r.transform; a != null; a = a.parent) { partes.Insert(0, a.name); }
                    string c = string.Join("/", partes.Take(6)) + " @" + r.bounds.center.ToString("F0") + " tam" + r.bounds.size.ToString("F1");
                    grupos[c] = grupos.TryGetValue(c, out int n) ? n + 1 : 1;
                }
                dump.AddRange(grupos.Select(p => p.Value + " " + p.Key));
                Vector3 cen = caja.center; cen.y = lugar.position.y + 1.7f;
                Vector3 cf = new Vector3(caja.extents.x * 0.9f, 0f, 0f), cz = new Vector3(0f, 0f, caja.extents.z * 0.9f);
                Foto(cen - cf, cen + cf * 0.9f, prefijo + "-" + fase + "-vista-x", 72f);
                Foto(cen + cf, cen - cf * 0.9f, prefijo + "-" + fase + "-vista-menosx", 72f);
                Foto(cen - cz, cen + cz * 0.9f, prefijo + "-" + fase + "-vista-z", 72f);
                Foto(cen + cz, cen - cz * 0.9f, prefijo + "-" + fase + "-vista-menosz", 72f);
            }
            File.WriteAllLines("Logs/lugar-dump.txt", dump);
            string vistas = System.Environment.GetEnvironmentVariable("T93_VISTAS");
            if (!string.IsNullOrEmpty(vistas))
            {
                int k = 0;
                foreach (string v in vistas.Split(';'))
                {
                    float[] f = v.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    Foto(new Vector3(f[0], f[1], f[2]), new Vector3(f[3], f[4], f[5]), prefijo + "-" + fase + "-extra" + (k++), 68f);
                }
            }
        }

        private static void Foto(Vector3 pos, Vector3 mira, string nombre, float fov)
        {
            var go = new GameObject("CamT93");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.05f; cam.farClipPlane = 500f;
            go.transform.position = pos; go.transform.LookAt(mira);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex); Object.Destroy(rt); Object.Destroy(go);
        }
    }
}
