using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-995: recorrido visual por Ciudad_Prologo a altura de peatón. Con T995_CAPTURAS=1 escribe docs/capturas/t995/zona-NN.png (1280x720)
    /// en puntos repartidos por todas las zonas, para revisar a ojo piezas flotando, encimadas o con textura rota.
    /// </summary>
    public class RecorridoCiudadT995Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "docs/capturas/t995/";
        private const float AlturaOjos = 1.7f;
        private const int Ancho = 1280;
        private const int Alto = 720;
        private const float CampoVision = 70f;
        private const float Lejos = 300f;
        private const float Cerca = 0.05f;

        /// <summary>(zona, desde XZ, hacia XZ, altura del punto mirado).</summary>
        private static readonly (string zona, Vector2 desde, Vector2 hacia, float alto)[] Puntos =
        {
            ("suburbios-1", new Vector2(-95f, -60f), new Vector2(-112f, -20f), 1.7f),
            ("suburbios-2", new Vector2(-110f, -70f), new Vector2(-110f, -20f), 2.5f),
            ("calle-dorada-1", new Vector2(-70f, -43f), new Vector2(-125f, -43f), 2.5f),
            ("calle-dorada-2", new Vector2(-125f, -43f), new Vector2(-65f, -43f), 2.5f),
            ("mercado-1", new Vector2(-20f, 30f), new Vector2(-20f, 45f), 1.7f),
            ("mercado-2", new Vector2(-35f, -20f), new Vector2(-37f, -8f), 2.0f),
            ("barrio-1", new Vector2(-50f, 20f), new Vector2(-57f, 60f), 2.5f),
            ("neon-alto-1", new Vector2(47f, 50f), new Vector2(47f, 68f), 3.5f),
            ("nova-1", new Vector2(100f, -40f), new Vector2(81f, -45f), 2.0f),
            ("nova-2", new Vector2(80f, -43f), new Vector2(120f, -43f), 3.0f),
            ("casino-fuera", new Vector2(47f, 38f), new Vector2(47f, 48f), 3.0f),
            ("callejon-1", new Vector2(-84f, -59f), new Vector2(-60f, -59f), 1.7f),
            ("callejon-2", new Vector2(-62f, -59f), new Vector2(-86f, -59f), 1.7f),
        };

        [UnityTest]
        public IEnumerator Recorrido_Capturas_ZonasAAlturaDePeaton()
        {
            if (System.Environment.GetEnvironmentVariable("T995_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t995/ con T995_CAPTURAS=1");
            }
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            System.IO.Directory.CreateDirectory(Carpeta);
            int n = 1;
            foreach (var p in Puntos)
            {
                yield return Foto(new Vector3(p.desde.x, AlturaOjos, p.desde.y), new Vector3(p.hacia.x, p.alto, p.hacia.y), "zona-" + n++.ToString("00"));
            }
            Assert.AreEqual(Puntos.Length, System.IO.Directory.GetFiles(Carpeta, "zona-*.png").Length, "Debe haber una foto por punto");
        }

        private static IEnumerator Foto(Vector3 desde, Vector3 hacia, string nombre)
        {
            GameObject go = new GameObject("CamaraFoto");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = Cerca;
            cam.farClipPlane = Lejos;
            cam.fieldOfView = CampoVision;
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
            System.IO.File.WriteAllBytes(Carpeta + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
