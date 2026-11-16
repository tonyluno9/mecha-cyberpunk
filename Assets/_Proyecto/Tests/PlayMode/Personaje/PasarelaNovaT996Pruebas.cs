using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1104 (antes T-996): las pasarelas PS01 de Neón Alto con sus escaleras y rampas se quitaron. Comprueba que no queda ninguna activa y que
    /// donde estaban se camina con W de punta a punta sin chocar con nada invisible. Con T1104_CAPTURAS=1 escribe docs/capturas/t-1104/.
    /// </summary>
    public class PasarelaNovaT996Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombrePasarela = "PS01_Pasarela";
        private const string NombreRampas = "T996_RampasEscalera";
        private const float XCalle = 47f;
        private const float MitadAncho = 6f;
        private const float ZInicio = 52f;
        private const float ZFin = 80f;
        private const float MaxSegundosCamino = 20f;
        private const float MargenLlegada = 1f;

        private Keyboard teclado;
        private GameObject jugador;
        private ControladorAPie controlador;
        private CharacterController cc;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
        }

        private IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controlador = Object.FindFirstObjectByType<ControladorAPie>();
            Assert.IsNotNull(controlador, "Ciudad_Prologo debe traer al jugador a pie");
            jugador = controlador.gameObject;
            cc = jugador.GetComponent<CharacterController>();
            foreach (string nombre in new[] { "DirectorPrologo", "Emboscada", "MostradorPeleas" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g != null)
                {
                    g.SetActive(false);
                }
            }
            controlador.enabled = false;
            controlador.enabled = true;
            int frames = 0;
            while (!controlador.ControlActivo && frames++ < 200)
            {
                yield return null;
            }
            Assert.IsTrue(controlador.ControlActivo, "El jugador debe poder moverse");
            // Tras empezar, la escena coloca al jugador en su punto de salida: hay que esperarlo antes de teletransportarlo.
            yield return Esperar(1.5f);
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            Scene vacia = SceneManager.CreateScene("PasarelaNovaT996_Vacia_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(vacia);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path.Contains("Ciudad"))
                {
                    yield return SceneManager.UnloadSceneAsync(s);
                }
            }
        }

        public override void TearDown()
        {
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private Vector3 Pos => jugador.transform.position;

        private void Poner(Vector3 posicion, Vector3 mirar)
        {
            cc.enabled = false;
            jugador.transform.position = posicion;
            jugador.transform.rotation = Quaternion.LookRotation(new Vector3(mirar.x, 0f, mirar.z));
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Escena_NoHayPasarelaNiRampasActivas()
        {
            yield return Cargar();
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsEmpty(todos.Where(t => t.name == NombrePasarela && t.gameObject.activeInHierarchy).Select(t => t.position.ToString("F1")).ToArray(),
                "No debe quedar ninguna pasarela PS01 activa");
            Assert.IsEmpty(todos.Where(t => t.name == NombreRampas && t.gameObject.activeInHierarchy).Select(t => t.position.ToString("F1")).ToArray(),
                "No debe quedar ninguna rampa T996 activa");
        }

        [UnityTest]
        public IEnumerator Banqueta_SeCaminaConWDePuntaAPuntaSinChocar()
        {
            yield return Cargar();
            List<string> fallas = new List<string>();
            foreach (float dx in new[] { -MitadAncho, 0f, MitadAncho })
            {
                float x = XCalle + dx;
                float suelo = SueloEn(new Vector3(x, 0f, ZInicio));
                Poner(new Vector3(x, suelo + 0.05f, ZInicio), Vector3.forward);
                yield return Esperar(0.5f);
                Press(teclado.wKey);
                float fin = Time.time + MaxSegundosCamino;
                while (Time.time < fin && Pos.z < ZFin - MargenLlegada)
                {
                    yield return null;
                }
                Release(teclado.wKey);
                yield return null;
                if (Pos.z < ZFin - MargenLlegada)
                {
                    fallas.Add("x " + x.ToString("F1") + ": se atoró en " + Pos.ToString("F2"));
                }
            }
            Assert.IsEmpty(fallas, string.Join(", ", fallas));
        }

        private static float SueloEn(Vector3 xz)
        {
            float mejor = 0.2f;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(xz.x, 3f, xz.z), Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.attachedRigidbody == null && h.point.y < 1f)
                {
                    mejor = Mathf.Max(mejor, h.point.y);
                }
            }
            return mejor;
        }

        [UnityTest]
        public IEnumerator Capturas_DeLaCalle()
        {
            if (System.Environment.GetEnvironmentVariable("T1104_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1104/ con T1104_CAPTURAS=1");
            }
            yield return Cargar();
            System.IO.Directory.CreateDirectory("docs/capturas/t-1104");
            yield return Foto(new Vector3(XCalle, 1.7f, 50f), new Vector3(XCalle, 3f, 70f), "calle-desde-sur");
            yield return Foto(new Vector3(XCalle, 1.7f, 82f), new Vector3(XCalle, 3f, 62f), "calle-desde-norte");
        }

        private static IEnumerator Foto(Vector3 desde, Vector3 hacia, string nombre)
        {
            GameObject go = new GameObject("CamaraFoto");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.fieldOfView = 70f;
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/t-1104/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
