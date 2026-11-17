using System.Collections;
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
    /// <summary>T-1318: con el jugador real de Ciudad_Prologo, caminar a un pretil de azotea y saltarlo lo deja caer a la calle.</summary>
    public class TechosSaltoT1318Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private Keyboard teclado;
        private GameObject jugador;
        private ControladorAPie controlador;
        private CharacterController cc;

        [UnitySetUp]
        public IEnumerator DescargarCiudadPrevia() { yield return Descargar("T1318_VaciaAntes"); }

        [UnityTearDown]
        public IEnumerator DescargarCiudadAlTerminar() { yield return Descargar("T1318_VaciaDespues"); }

        private static IEnumerator Descargar(string nombre)
        {
            bool hayCiudad = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                hayCiudad |= SceneManager.GetSceneAt(i).path.Contains("Ciudad");
            }
            if (!hayCiudad)
            {
                yield break;
            }
            SceneManager.SetActiveScene(SceneManager.CreateScene(nombre));
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
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado != null ? new InputDevice[] { teclado } : new InputDevice[0]);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Pretil_CaminarYSaltar_CaeDelTecho()
        {
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controlador = Object.FindFirstObjectByType<ControladorAPie>();
            Assert.IsNotNull(controlador);
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
            int frames = 0;
            while (!controlador.ControlActivo && frames++ < 200)
            {
                yield return null;
            }
            Assert.IsTrue(controlador.ControlActivo);
            int estables = 0;
            int intentos = 0;
            while (estables < 10 && intentos++ < 1000)
            {
                yield return null;
                estables = Time.deltaTime < 0.05f ? estables + 1 : 0;
            }

            Transform pretiles = GameObject.Find("S2_Techos").transform.Find("Pretiles");
            Transform pretil = null;
            Vector3 dentro = Vector3.zero;
            foreach (Transform t in pretiles)
            {
                Vector3 punto = t.position - t.forward * 2f;
                bool techo = Physics.Raycast(punto + Vector3.up, Vector3.down, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y - t.position.y) < 0.1f;
                // Del otro lado del pretil debe haber calle libre: nada del techo a menos de 6 m de caída.
                Vector3 fuera = t.position + t.forward * 1.5f;
                bool vacio = !Physics.Raycast(fuera + Vector3.up * 0.5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                bool libre = !Physics.CheckCapsule(punto + Vector3.up * 0.4f, punto + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
                bool paso = !Physics.CheckCapsule(fuera + Vector3.up * 0.6f, fuera + Vector3.up * 1.6f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
                if (techo && vacio && libre && paso)
                {
                    pretil = t;
                    dentro = punto;
                    break;
                }
            }
            Assert.IsNotNull(pretil, "Debe haber un pretil con techo detrás y vacío delante");
            float techoY = pretil.position.y;

            cc.enabled = false;
            jugador.transform.position = dentro + Vector3.up * 0.05f;
            jugador.transform.rotation = Quaternion.LookRotation(new Vector3(pretil.forward.x, 0f, pretil.forward.z));
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
            yield return Esperar(0.5f);

            float fin = Time.time + 6f;
            float minimoY = jugador.transform.position.y;
            float proximoSalto = 0f;
            Press(teclado.wKey);
            while (Time.time < fin)
            {
                if (Time.time >= proximoSalto && Vector3.Dot(jugador.transform.position - pretil.position, pretil.forward) > -0.9f)
                {
                    Press(teclado.spaceKey);
                    yield return null;
                    yield return null;
                    Release(teclado.spaceKey);
                    proximoSalto = Time.time + 0.8f;
                }
                minimoY = Mathf.Min(minimoY, jugador.transform.position.y);
                yield return null;
            }
            Release(teclado.wKey);
            Assert.Greater(Vector3.Dot(jugador.transform.position - pretil.position, pretil.forward), 1f, "Debe haber cruzado el pretil");
            Assert.Less(jugador.transform.position.y, techoY - 2f, "Debe haber saltado el pretil y caído del techo (techo " + techoY + ", pos " + jugador.transform.position + ", pretil " + pretil.position + " fwd " + pretil.forward + ", minY " + minimoY + ", pasos " + Vector3.Dot(jugador.transform.position - pretil.position, pretil.forward) + ")");
        }
    }
}
