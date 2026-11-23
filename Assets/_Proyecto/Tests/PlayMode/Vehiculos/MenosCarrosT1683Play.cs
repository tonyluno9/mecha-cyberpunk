using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T45 / T-1683: un carro de Tripo (V20 tuneado, V09 taxi) que revienta por el camino normal de daño (RecibirDanio) queda como armazón quemado en todas sus
    /// mallas. Toma una foto a ~6 m con una cámara propia (RenderTexture) en docs/capturas/_tareas/T45/quemado_&lt;código&gt;.png: con fuego y sin él.
    /// </summary>
    public class MenosCarrosT1683Play
    {
        private const string Carpeta = "docs/capturas/_tareas/T45";
        private const float EsperaMaxima = 8f;

        private GameObject piso;
        private GameObject luz;
        private GameObject carro;
        private GameObject camara;

        [SetUp]
        public void Preparar()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(60f, 1f, 60f);
            luz = new GameObject("Sol");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            luz.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject go in new[] { carro, camara, luz, piso })
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
        }

        [UnityTest]
        public IEnumerator Explotar_CarroTripo_QuedaQuemadoEnTodasSusMallas([Values("V20", "V09")] string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            Assert.IsNotNull(prefab, codigo);
            carro = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
            DanioCarro danio = carro.GetComponentInChildren<DanioCarro>();
            Assert.IsNotNull(danio, codigo + " sin DanioCarro");

            danio.RecibirDanio(new InfoDanio(danio.VidaMaxima * 2f, TipoDanio.Disparo, carro.transform.position, Vector3.forward, null));
            float fin = Time.time + EsperaMaxima;
            while (!danio.Explotado && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(danio.Explotado, codigo + " no explotó");
            yield return new WaitForSeconds(1.5f);

            Renderer[] mallas = carro.GetComponentsInChildren<Renderer>(true).Where(DanioCarro.EsMallaQuemable).ToArray();
            Assert.Greater(mallas.Length, 0);
            foreach (Renderer r in mallas)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.AreSame(DanioCarro.MaterialArmazon, m, codigo + ": " + r.name + " no quedó quemado (" + (m != null ? m.name : "null") + ")");
                }
            }

            camara = new GameObject("CamaraT45");
            Camera cam = camara.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            cam.fieldOfView = 50f;
            Vector3 centro = danio.transform.position + Vector3.up * 0.8f;
            cam.transform.position = centro + new Vector3(-4.2f, 1.2f, 4.2f); // ~6 m
            cam.transform.LookAt(centro);
            Directory.CreateDirectory(Carpeta);
            Foto(cam, Carpeta + "/quemado_" + codigo + "_fuego.png");
            // Sin las partículas (fuego y humo tapan la carrocería) para ver el armazón.
            foreach (ParticleSystemRenderer p in Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
            {
                p.enabled = false;
            }
            Foto(cam, Carpeta + "/quemado_" + codigo + ".png");
        }

        private static void Foto(Camera cam, string ruta)
        {
            const int Ancho = 960;
            const int Alto = 600;
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, Ancho, Alto), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.Destroy(tex);
            Object.Destroy(rt);
        }
    }
}
