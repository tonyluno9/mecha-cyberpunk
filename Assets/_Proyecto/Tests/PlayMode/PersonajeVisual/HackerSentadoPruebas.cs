using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-639: EL HACKER está sentado frente a una mesa con laptop, a escala de persona.</summary>
    public class HackerSentadoPruebas
    {
        private PoseSentado pose;
        private Animator animador;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            pose = Object.FindObjectsByType<PoseSentado>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            animador = pose.GetComponent<Animator>();
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hacker_CaderaALaAlturaDeLaSilla_CabezaSobreLaMesaYMiraAlaMesa()
        {
            yield return null;
            float piso = pose.transform.position.y;
            float cadera = animador.GetBoneTransform(HumanBodyBones.Hips).position.y - piso;
            Assert.That(cadera, Is.InRange(0.35f, 0.75f), "altura de la cadera");
            Transform mesa = GameObject.Find("Mesa_Cubierta").transform;
            float cabeza = animador.GetBoneTransform(HumanBodyBones.Head).position.y;
            Assert.Greater(cabeza, mesa.position.y + 0.1f, "cabeza sobre la mesa");
            Vector3 haciaMesa = Vector3.ProjectOnPlane(mesa.position - animador.GetBoneTransform(HumanBodyBones.Hips).position, Vector3.up).normalized;
            Assert.Greater(Vector3.Dot(pose.transform.forward, haciaMesa), 0.9f, "mira a la mesa");
            Assert.IsNotNull(GameObject.Find("Laptop_Pantalla"));
        }

        [UnityTest]
        public IEnumerator Hacker_EsDeUnosUnoOchentaMetros()
        {
            yield return null;
            Bounds caja = default;
            bool primero = true;
            foreach (SkinnedMeshRenderer r in pose.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (primero) { caja = r.bounds; primero = false; }
                else { caja.Encapsulate(r.bounds); }
            }
            // Sentado mide menos que de pie: entre 1.1 y 1.8 m de la cadera a la cabeza más las piernas.
            Assert.That(caja.size.y, Is.InRange(1.0f, 1.85f));
            Assert.That(animador.GetBoneTransform(HumanBodyBones.Head).position.y - pose.transform.position.y, Is.InRange(0.85f, 1.5f), "cabeza sentado");
        }

        [UnityTest]
        public IEnumerator Capturar_HackerSentado()
        {
            yield return null;
            var go = new GameObject("CamHacker");
            Camera cam = go.AddComponent<Camera>();
            Transform set = GameObject.Find("Set_Hacker").transform;
            cam.transform.position = set.position + set.right * 1.6f + set.forward * 0.5f + Vector3.up * 1.3f;
            cam.transform.LookAt(set.position + set.forward * 0.3f + Vector3.up * 0.9f);
            cam.cullingMask = ~0;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            yield return new WaitForSeconds(0.5f);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/hacker-sentado.png", tex.EncodeToPNG());
            RenderTexture.active = null;
            Assert.IsTrue(File.Exists("Logs/hacker-sentado.png"));
        }
    }
}
