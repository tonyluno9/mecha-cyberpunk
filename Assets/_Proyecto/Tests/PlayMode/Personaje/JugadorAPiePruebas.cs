using System.Collections;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas del contrato IJugador del jugador a pie.</summary>
    public class JugadorAPiePruebas : BaseJugadorAPie
    {
        /// <summary>Doble de una zona del mundo: guarda el IJugador que encuentra al entrar.</summary>
        public class ZonaDePrueba : MonoBehaviour
        {
            public int Entradas;
            public IJugador Encontrado;

            private void OnTriggerEnter(Collider otro)
            {
                Entradas++;
                Encontrado = otro.GetComponentInParent<IJugador>();
            }
        }

        [UnityTest]
        public IEnumerator Registro_ConElPrefabEnEscena_EsElJugadorActual()
        {
            yield return null;
            Assert.AreSame(jugadorAPie, Registro<IJugador>.Actual);
            Assert.AreEqual(1, Registro<IJugador>.Cantidad);
            Assert.AreSame(jugador.transform, jugadorAPie.Raiz);
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo);
            Assert.AreSame(jugador.GetComponentInChildren<Camera>(), jugadorAPie.Camara);
            Assert.AreNotSame(jugador.transform, jugadorAPie.Camara.transform);
            Assert.AreSame(jugador.transform, jugadorAPie.Camara.transform.root);
        }

        [UnityTest]
        public IEnumerator Registro_AlDesactivarlo_SaleDelRegistro()
        {
            yield return null;
            jugador.SetActive(false);
            Assert.IsNull(Registro<IJugador>.Actual);
            Assert.AreEqual(0, Registro<IJugador>.Cantidad);
            jugador.SetActive(true);
            Assert.AreSame(jugadorAPie, Registro<IJugador>.Actual);
        }

        [UnityTest]
        public IEnumerator Registro_ConOtroJugadorRegistrado_AvisaUnaVez()
        {
            yield return null;
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[Personaje\]"));
            GameObject segundo = Object.Instantiate(jugador, new Vector3(20f, 0.05f, 0f), Quaternion.identity);
            segundo.SetActive(false);
            segundo.SetActive(true);
            Assert.AreEqual(2, Registro<IJugador>.Cantidad);
            yield return null;
            Object.Destroy(segundo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Colocar_DetrasDeUnMuro_LoMueveALaPose()
        {
            Caja(new Vector3(0f, 2f, 3f), new Vector3(20f, 4f, 1f));
            yield return Calentar();
            Pose pose = new Pose(new Vector3(0f, 0.05f, 10f), Quaternion.Euler(0f, 90f, 0f));
            jugadorAPie.Colocar(pose);
            Assert.AreEqual(10f, jugador.transform.position.z, 0.001f);
            Assert.AreEqual(90f, jugador.transform.eulerAngles.y, 0.01f);
            yield return Esperar(0.3f);
            Assert.AreEqual(10f, jugador.transform.position.z, 0.05f);
            Assert.AreEqual(0f, jugador.transform.position.x, 0.05f);
            Assert.AreEqual(0f, controlador.Velocidad.magnitude, 0.001f);
        }

        [UnityTest]
        public IEnumerator Zona_ElJugadorCaminaDentro_RecibeElTriggerYEncuentraElIJugador()
        {
            GameObject zona = Caja(new Vector3(0f, 1f, 5f), new Vector3(2f, 2f, 2f));
            zona.GetComponent<Collider>().isTrigger = true;
            ZonaDePrueba receptor = zona.AddComponent<ZonaDePrueba>();
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Assert.GreaterOrEqual(receptor.Entradas, 1);
            Assert.AreSame(jugadorAPie, receptor.Encontrado);
        }
    }
}
