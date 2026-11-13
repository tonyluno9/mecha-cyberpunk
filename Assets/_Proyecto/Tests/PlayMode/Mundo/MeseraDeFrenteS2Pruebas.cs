using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.Casino.Meseras;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// Sesión 2 (14 nov): la R01 de Blender mira hacia -Z y las meseras caminaban de espaldas. Se mide hacia dónde mira el cuerpo animado
    /// (de muslo izquierdo a derecho, girado 90° hacia arriba) contra hacia dónde se movió de verdad, y se toma una foto al final del frame
    /// (después de BrazoCharola) en docs/capturas/s2-14nov/.
    /// </summary>
    public class MeseraDeFrenteS2Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float MinimoDeFrente = 0.7f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        private static Vector3 Frente(Animator a)
        {
            Vector3 f = Vector3.Cross(a.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position, Vector3.up);
            f.y = 0f;
            return f.normalized;
        }

        [UnityTest]
        public IEnumerator Meseras_MiranHaciaDondeCaminan()
        {
            RondaMesera[] meseras = Object.FindObjectsByType<RondaMesera>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(meseras.Length, 3, "Las tres meseras del casino");
            int medidas = 0;
            for (int intento = 0; intento < 400 && medidas < meseras.Length; intento++)
            {
                medidas = 0;
                foreach (RondaMesera r in meseras)
                {
                    if (!r.EstaCaminando) { continue; }
                    Vector3 antes = r.transform.position;
                    yield return null;
                    yield return null;
                    Vector3 paso = r.transform.position - antes;
                    paso.y = 0f;
                    if (paso.sqrMagnitude < 1e-6f || !r.EstaCaminando) { continue; }
                    Vector3 frente = Frente(r.GetComponent<Animator>());
                    Assert.Greater(Vector3.Dot(frente, paso.normalized), MinimoDeFrente, r.name + " camina de espaldas o de lado (frente " + frente.ToString("F2") + ", paso " + paso.normalized.ToString("F2") + ")");
                    medidas++;
                }
                yield return null;
            }
            Assert.Greater(medidas, 0, "Ninguna mesera caminó durante la prueba");

            RondaMesera m = meseras.FirstOrDefault(r => r.EstaCaminando) ?? meseras[0];
            // La cámara renderiza sola al final del frame (después de BrazoCharola); se lee la textura en el frame siguiente.
            // WaitForEndOfFrame no corre en batchmode.
            Vector3 f = Frente(m.GetComponent<Animator>());
            Vector3 p = m.transform.position;
            var go = new GameObject("CamS2");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.depth = 100f;
            go.transform.position = p + f * 2.4f + Vector3.Cross(Vector3.up, f) * 1.2f + Vector3.up * 1.6f;
            go.transform.LookAt(p + Vector3.up);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            cam.enabled = false;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("docs/capturas/s2-14nov");
            File.WriteAllBytes("docs/capturas/s2-14nov/mesera-de-frente.png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
