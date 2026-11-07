using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-723: la cámara del jugador a pie (Cinemachine) en la ciudad real (Ciudad_Prologo): en la calle como CAM01_APie.png, cerca como
    /// CAM02_APieCerca.png, apuntando, y en el cuarto de la casa (donde antes se veía casi solo el techo). Deja capturas s1d-T-723-*.png con CAPTURAR_S1=1.
    /// </summary>
    public class CamaraEscenaT723Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private static float MetrosALaCalle => float.Parse(System.Environment.GetEnvironmentVariable("H10_METROS") ?? "9", System.Globalization.CultureInfo.InvariantCulture);
        private static float GiroCalle => float.Parse(System.Environment.GetEnvironmentVariable("H10_GIRO") ?? "0", System.Globalization.CultureInfo.InvariantCulture);

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private static IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForSeconds(0.5f);
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (m != null && m.GetType().Name == "DirectorPandillas") m.enabled = false;
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t != null && t.name.StartsWith("CalaveraPunos")) Object.Destroy(t.gameObject);
            }
        }

        private static void IrA(string id, float metrosAdelante, float giro)
        {
            IJugador jugador = Registro<IJugador>.Todos[0];
            foreach (IPuntoAparicion p in Registro<IPuntoAparicion>.Todos)
            {
                if (p.Id == id)
                {
                    Vector3 sitio = p.Pose.position + p.Pose.rotation * Vector3.forward * metrosAdelante + Vector3.up * 0.05f;
                    jugador.Colocar(new Pose(sitio, Quaternion.Euler(0f, giro, 0f)));
                }
            }
        }

        [UnityTest]
        public IEnumerator Calle_CercaYApuntando_ComoCAM01YCAM02()
        {
            yield return Cargar();
            IJugador jugador = Registro<IJugador>.Todos[0];
            IrA("Casa_Puerta", MetrosALaCalle, GiroCalle);
            yield return new WaitForSeconds(1.2f);
            Camera camara = jugador.Camara;
            CamaraCinemachineT723Pruebas.Medir(jugador.Raiz.gameObject, camara, out float fraccion, out float centroX, out float pies, out float cabeza);
            Debug.Log("[T723] calle fraccion " + fraccion.ToString("F3") + " centroX " + centroX.ToString("F3") + " pies " + pies.ToString("F3") + " cabeza " + cabeza.ToString("F3"));
            CapturaS1.FotoCamara("s1d-T-723-calle", camara);
            Assert.That(fraccion, Is.InRange(0.36f, 0.5f), "El personaje ocupa ~40 % del alto");
            Assert.AreEqual(0.5f, centroX, 0.05f);
            Assert.Less(pies, 0.2f);

            // A pie cerca: mirando un poco hacia abajo (CAM02).
            ControladorAPie controlador = jugador.Raiz.GetComponent<ControladorAPie>();
            controlador.Empujar(0f, 20f);
            yield return new WaitForSeconds(0.4f);
            CapturaS1.FotoCamara("s1d-T-723-cerca", camara);
            controlador.Empujar(0f, -20f);

            // Apuntando: mismo encuadre, solo zoom.
            jugador.Raiz.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola);
            yield return new WaitForSeconds(0.3f);
            Press(raton.rightButton);
            yield return new WaitForSeconds(1.2f);
            Assert.Less(camara.fieldOfView, 50f, "Apuntando hace zoom");
            CapturaS1.FotoCamara("s1d-T-723-apuntando", camara);
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Cuarto_DeLaCasa_LaCamaraSeAcercaYSeVeAlPersonaje()
        {
            yield return Cargar();
            IJugador jugador = Registro<IJugador>.Todos[0];
            IrA("Casa_Cama", 0f, 0f);
            yield return new WaitForSeconds(1.5f);
            Camera camara = jugador.Camara;
            Vector3 local = jugador.Raiz.InverseTransformPoint(camara.transform.position);
            Debug.Log("[T723] cuarto jugador " + jugador.Raiz.position + " camara local " + local);
            CapturaS1.FotoCamara("s1d-T-723-cuarto", camara);
            Assert.IsFalse(Physics.CheckSphere(camara.transform.position, 0.1f, ~0, QueryTriggerInteraction.Ignore), "La cámara no queda dentro de una pared");
            Vector3 cabeza = camara.WorldToViewportPoint(jugador.Raiz.position + Vector3.up * 1.7f);
            Assert.That(cabeza.z, Is.GreaterThan(0.3f), "La cámara ve al personaje");
            Assert.That(cabeza.y, Is.InRange(0.1f, 0.9f), "El personaje queda dentro de la pantalla");
        }

        /// <summary>Mide el vehículo en pantalla: centro Y y X (viewport, 0 abajo) y alto, con los renderers de su montable.</summary>
        private static void MedirVehiculo(IMontable montable, Camera camara, out float centroY, out float centroX, out float alto)
        {
            Bounds cajas = default;
            bool primero = true;
            foreach (Renderer r in ((Component)montable).GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (primero) { cajas = r.bounds; primero = false; } else { cajas.Encapsulate(r.bounds); }
            }
            float minY = float.MaxValue, maxY = float.MinValue, minX = float.MaxValue, maxX = float.MinValue;
            foreach (int ix in new[] { -1, 1 }) foreach (int iy in new[] { -1, 1 }) foreach (int iz in new[] { -1, 1 })
            {
                Vector3 v = camara.WorldToViewportPoint(cajas.center + Vector3.Scale(cajas.extents, new Vector3(ix, iy, iz)));
                minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
                minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x);
            }
            centroY = (minY + maxY) * 0.5f;
            centroX = (minX + maxX) * 0.5f;
            alto = maxY - minY;
        }

        private static bool EsMoto(IMontable m)
        {
            string n = ((Component)m).gameObject.name;
            return n.Contains("Moto") || n.Contains("C19") || n.Contains("V07") || n.Contains("V08");
        }

        private static IMontable BuscarMontable(bool moto)
        {
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m.Modo == ModoJugador.Carro && m.PuedeSubir && !m.Ocupado && EsMoto(m) == moto) return m;
            }
            return null;
        }

        private IEnumerator ProbarVehiculo(bool moto, string nombre)
        {
            yield return Cargar();
            IJugador jugador = Registro<IJugador>.Todos[0];
            IMontable montable = BuscarMontable(moto);
            if (montable == null) Assert.Ignore("No hay " + nombre + " libre en la escena");
            Assert.IsTrue(jugador.Raiz.GetComponent<JugadorAPie>().Subir(montable), "Sube al vehiculo");
            yield return new WaitForSeconds(1.5f);
            Camera camara = jugador.Camara;
            MedirVehiculo(montable, camara, out float cy, out float cx, out float alto);
            Debug.Log("[T723] " + nombre + " centroY " + cy.ToString("F3") + " centroX " + cx.ToString("F3") + " alto " + alto.ToString("F3") + " camara " + camara.transform.position + " vehiculo " + ((Component)montable).transform.position);
            CapturaS1.FotoCamara("s1d-T-723-" + nombre, camara);
            Assert.That(cy, Is.InRange(0.1f, 0.38f), "El vehiculo queda centrado abajo (CAM03)");
            Assert.AreEqual(0.5f, cx, 0.08f, "Y centrado en horizontal");
            Assert.That(alto, Is.InRange(0.12f, 0.85f));
        }

        [UnityTest]
        public IEnumerator Carro_CamaraDetrasYArribaConElCarroAbajo() { yield return ProbarVehiculo(false, "carro"); }

        [UnityTest]
        public IEnumerator Moto_CamaraDetrasYArribaConLaMotoAbajo() { yield return ProbarVehiculo(true, "moto"); }
    }
}
