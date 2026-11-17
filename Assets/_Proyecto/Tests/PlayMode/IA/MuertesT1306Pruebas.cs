using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1306: los que mueren (pandilleros, policías, Nova) quedan TIRADOS en el piso, nunca hincados: a los 4 s la cadera está a menos de
    /// <see cref="AlturaCaderaTirado"/> m del piso, con golpes de frente y por la espalda. Captura en docs/capturas/t-1306/ (T1306_CAPTURAS=1).
    /// </summary>
    public class MuertesT1306Pruebas
    {
        public const float AlturaCaderaTirado = 0.42f;
        private const string Carpeta = "Assets/_Proyecto/IA/Prefabs/";
        private static readonly string[] Prefabs =
        {
            "PandilleroArmado_Skulls", "PandilleroArmado_Snakes", "PoliciaAPie", "GuardiaNova_Armado", "GuardiaPesado_E12", "JefeSnakes_E11",
        };

        private readonly List<GameObject> creados = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            creados.Add(piso);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject g in creados)
            {
                if (g != null) Object.DestroyImmediate(g);
            }
            creados.Clear();
            Registros.LimpiarTodos();
        }

        private static Transform Cadera(GameObject quien)
        {
            Animator a = quien.GetComponentInChildren<Animator>(true);
            if (a != null && a.isHuman)
            {
                Transform h = a.GetBoneTransform(HumanBodyBones.Hips);
                if (h != null) return h;
            }
            foreach (Transform t in quien.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n == "hips" || n.EndsWith("hips") || n == "pelvis") return t;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator AlMorir_TodosQuedanTiradosEnElPiso_NuncaHincados()
        {
            List<(string nombre, PandilleroAPie p)> muertos = new List<(string, PandilleroAPie)>();
            float x = -12f;
            foreach (string nombre in Prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab");
                if (prefab == null)
                {
                    Debug.LogWarning("[T-1306] falta " + nombre);
                    continue;
                }
                for (int lado = 0; lado < 2; lado++)
                {
                    GameObject g = Object.Instantiate(prefab, new Vector3(x, 0.05f, 6f), Quaternion.Euler(0f, 180f, 0f));
                    creados.Add(g);
                    foreach (Animator an in g.GetComponentsInChildren<Animator>(true))
                    {
                        an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    }
                    muertos.Add((nombre + (lado == 0 ? "_frente" : "_espalda"), g.GetComponent<PandilleroAPie>()));
                    x += 2.2f;
                }
            }
            Assert.Greater(muertos.Count, 0);
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < muertos.Count; i++)
            {
                PandilleroAPie p = muertos[i].p;
                Vector3 dir = i % 2 == 0 ? Vector3.forward : Vector3.back; // de frente (lo empuja hacia atrás) o por la espalda
                for (int k = 0; k < 40 && !p.EstaKO; k++)
                {
                    p.RecibirDanio(new InfoDanio(500f, TipoDanio.Disparo, p.transform.position + Vector3.up * 1.2f, dir, null));
                }
                Assert.IsTrue(p.EstaKO, muertos[i].nombre + " muere");
            }
            yield return new WaitForSeconds(4f);

            if (System.Environment.GetEnvironmentVariable("T1306_CAPTURAS") == "1")
            {
                yield return Capturar(new Vector3(0f, 3.2f, -3f), new Vector3(0f, 0f, 6f), "docs/capturas/t-1306/muertos.png");
            }

            List<string> hincados = new List<string>();
            foreach ((string nombre, PandilleroAPie p) in muertos)
            {
                Transform cadera = Cadera(p.gameObject);
                Assert.IsNotNull(cadera, nombre + " tiene cadera");
                float altura = cadera.position.y;
                Debug.Log("[T-1306] " + nombre + ": cadera a " + altura.ToString("F2") + " m, muerte " + (p.Visual != null ? p.Visual.Muerte.ToString() : "-"));
                if (altura > AlturaCaderaTirado)
                {
                    hincados.Add(nombre + " (" + altura.ToString("F2") + " m)");
                }
            }
            Assert.IsEmpty(hincados, "Quedan hincados o sentados: " + string.Join(", ", hincados));
        }

        private static IEnumerator Capturar(Vector3 desde, Vector3 hacia, string ruta)
        {
            GameObject cam = new GameObject("CamaraT1306");
            Camera c = cam.AddComponent<Camera>();
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            c.fieldOfView = 70f;
            GameObject luz = new GameObject("LuzT1306");
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            luz.AddComponent<Light>().type = LightType.Directional;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.4f, 0.55f, 0.75f);
            RenderTexture rt = new RenderTexture(1600, 800, 24);
            c.targetTexture = rt;
            yield return null;
            c.Render();
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            c.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(imagen);
            Object.DestroyImmediate(cam);
            Object.DestroyImmediate(luz);
        }
    }
}
