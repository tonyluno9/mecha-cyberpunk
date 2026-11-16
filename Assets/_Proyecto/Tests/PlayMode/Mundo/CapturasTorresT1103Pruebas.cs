using System.Collections;
using System.IO;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1103: capturas de las torres de Nova y Neón Alto de noche (hora 23) y de día (hora 13) con la escena real corriendo (CicloDiaNoche vivo).
    /// Solo escriben docs/capturas/t-1103/ con T1103_CAPTURAS=1; T1103_ETAPA=antes escribe solo las tomas del tianguis con el prefijo "antes-".
    /// </summary>
    public class CapturasTorresT1103Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "docs/capturas/t-1103";
        private const float HoraNoche = 23f;
        private const float HoraDia = 13f;
        private const int Ancho = 1280;
        private const int Alto = 720;
        private const float LejaniaMaxima = 1000f;
        private const float CampoDeVision = 70f;
        private static readonly Vector3 HaciaNova = new Vector3(134f, 45f, -43f);

        [UnityTest]
        public IEnumerator Capturas_DeNochePorDia()
        {
            if (System.Environment.GetEnvironmentVariable("T1103_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1103/ con T1103_CAPTURAS=1");
            }
            bool antes = System.Environment.GetEnvironmentVariable("T1103_ETAPA") == "antes";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            foreach (string nombre in new[] { "DirectorPrologo", "Emboscada", "MostradorPeleas" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g != null)
                {
                    g.SetActive(false);
                }
            }
            RelojMundo reloj = Object.FindFirstObjectByType<RelojMundo>();
            Assert.IsNotNull(reloj, "La escena trae el reloj del mundo");
            reloj.Pausar(true);
            Directory.CreateDirectory(Carpeta);
            foreach ((string momento, float hora) in new[] { ("noche", HoraNoche), ("luz", HoraDia) })
            {
                reloj.AvanzarHasta(hora);
                for (int i = 0; i < 5; i++)
                {
                    yield return null;
                }
                string pre = antes ? "antes-" : "";
                yield return Foto(new Vector3(0f, 1.7f, 0f), HaciaNova, pre + "tianguis-" + momento);
                if (antes)
                {
                    continue;
                }
                yield return Foto(new Vector3(-90f, 8f, -50f), HaciaNova, "casa-" + momento);
                yield return Foto(new Vector3(30f, 60f, -43f), new Vector3(104f, 58f, -58f), "calle-nova-" + momento, CampoDeVision);
                yield return Foto(new Vector3(47f, 1.7f, 8f), new Vector3(47f, 60f, 60f), "neon-alto-" + momento);
            }
            Assert.IsTrue(File.Exists(Carpeta + "/" + (antes ? "antes-" : "") + "tianguis-noche.png"));
        }

        private static IEnumerator Foto(Vector3 desde, Vector3 hacia, string nombre, float campo = CampoDeVision)
        {
            GameObject go = new GameObject("CamaraFoto");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = LejaniaMaxima;
            cam.fieldOfView = campo;
            cam.depth = 100f;
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Carpeta + "/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
