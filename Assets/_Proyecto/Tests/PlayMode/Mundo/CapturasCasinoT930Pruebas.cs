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
    /// <summary>T-930: capturas del casino VivaCoin (solo con T930_CAPTURAS=1; el prefijo en T930_FASE). Cámara con RenderTexture: en batchmode ScreenCapture no sirve.</summary>
    public class CapturasCasinoT930Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private static bool Activas => System.Environment.GetEnvironmentVariable("T930_CAPTURAS") == "1";
        private static string Fase => System.Environment.GetEnvironmentVariable("T930_FASE") ?? "despues";

        private static string Ruta(Transform t, int niveles)
        {
            var partes = new List<string>();
            for (Transform a = t; a != null; a = a.parent) { partes.Insert(0, a.name); }
            return string.Join("/", partes.Take(niveles));
        }

        [UnityTest]
        public IEnumerator Casino()
        {
            if (!Activas) { yield break; }
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 90; i++) { yield return null; }
            var caja = new Bounds(new Vector3(74f, 3f, 49f), new Vector3(30f, 8f, 26f));
            var grupos = new SortedDictionary<string, int>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || !caja.Contains(r.bounds.center)) { continue; }
                MeshFilter f = r.GetComponent<MeshFilter>();
                string malla = f != null && f.sharedMesh != null ? f.sharedMesh.name : r.GetType().Name;
                string c = Ruta(r.transform, 7) + " tam" + r.bounds.size.ToString("F1") + "  [" + malla + "] @" + r.bounds.center.ToString("F0");
                grupos[c] = grupos.TryGetValue(c, out int n) ? n + 1 : 1;
            }
            File.WriteAllLines("Logs/casino-dump.txt", grupos.Select(p => p.Value + " " + p.Key));
            Foto(new Vector3(62f, 1.7f, 47f), new Vector3(80f, 1.6f, 49f), "entrada-dentro", 72f);
            Foto(new Vector3(82f, 2.2f, 42.5f), new Vector3(64f, 1.4f, 55f), "panoramica", 74f);
            Foto(new Vector3(70f, 1.7f, 54f), new Vector3(64f, 1.5f, 57f), "barra", 70f);
            Foto(new Vector3(75f, 1.7f, 45f), new Vector3(81f, 1.3f, 40f), "fondo-hacker", 70f);
            Foto(new Vector3(47f, 1.7f, 47f), new Vector3(59f, 3f, 47f), "fachada", 60f);
            Foto(new Vector3(72f, 1.7f, 53f), new Vector3(72f, 1.4f, 60f), "cajera", 62f);
            GameObject r01 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Personajes/MeseraRobot/MeseraRobot.fbx");
            if (r01 != null)
            {
                GameObject m = Object.Instantiate(r01, new Vector3(0f, 100f, 0f), Quaternion.identity);
                foreach (Animator a in m.GetComponentsInChildren<Animator>()) { a.enabled = false; }
                yield return null;
                Foto(m.transform.position + new Vector3(0f, 1f, 2.6f), m.transform.position + Vector3.up, "r01-frente", 50f);
                Foto(m.transform.position + new Vector3(2.6f, 1f, 0f), m.transform.position + Vector3.up, "r01-lado", 50f);
                var huesos = new List<string>();
                foreach (Transform t in m.GetComponentsInChildren<Transform>()) { if (t.name.Contains("Hand") || t.name.Contains("Arm") || t.name.Contains("Forearm") || t.name.Contains("Clavicle")) { huesos.Add(t.name + " p" + t.position.ToString("F2") + " r" + t.eulerAngles.ToString("F0")); } }
                File.WriteAllLines("Logs/r01-huesos.txt", huesos);
            }
            foreach (GameObject go in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Select(a => a.gameObject).Where(g => g.name.StartsWith("Mesera_") || g.name.ToLowerInvariant().Contains("croupier")).Take(3))
            {
                Vector3 p = go.transform.position;
                Foto(p + go.transform.forward * 2.2f + Vector3.up * 1.4f, p + Vector3.up * 1f, "personaje-" + go.name, 50f);
            }
        }

        private static void Foto(Vector3 pos, Vector3 mira, string nombre, float fov)
        {
            var go = new GameObject("CamT930");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400f;
            go.transform.position = pos; go.transform.LookAt(mira);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes("docs/capturas/t930-" + Fase + "-" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex); Object.Destroy(rt); Object.Destroy(go);
        }
    }
}
