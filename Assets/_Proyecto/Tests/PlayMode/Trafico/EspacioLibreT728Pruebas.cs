using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-728: nunca se hace aparecer un vehículo encima de otro. La caja de espacio libre (EspacioLibre, OverlapBox) ve motos, carros robados o estacionados y al jugador, no cuenta
    /// el piso ni los triggers, y los lugares de salida del tráfico, de los refuerzos y del carro del asalto la respetan (con todos los nodos de la calle tapados no sale ninguno).
    /// </summary>
    public class EspacioLibreT728Pruebas
    {
        private readonly List<GameObject> objetos = new List<GameObject>();

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject o in objetos)
            {
                if (o != null) Object.DestroyImmediate(o);
            }
            objetos.Clear();
        }

        private GameObject Caja(string nombre, Vector3 centro, Vector3 tamano, bool trigger = false)
        {
            GameObject o = new GameObject(nombre);
            o.transform.position = centro;
            BoxCollider c = o.AddComponent<BoxCollider>();
            c.size = tamano;
            c.isTrigger = trigger;
            objetos.Add(o);
            Physics.SyncTransforms();
            return o;
        }

        [UnityTest]
        public IEnumerator Caja_VeUnaMotoYElJugador_NoElPisoNiLosTriggers()
        {
            Vector3 punto = new Vector3(5000f, 0f, 5000f);
            Caja("Piso", punto + Vector3.down * 0.05f, new Vector3(40f, 0.1f, 40f));
            Caja("Banqueta", punto + Vector3.up * 0.1f, new Vector3(40f, 0.2f, 6f));
            yield return null;
            Assert.IsTrue(EspacioLibre.Libre(punto), "El piso y la banqueta no cuentan como estorbo");

            GameObject zona = Caja("Zona", punto + Vector3.up * 1f, new Vector3(3f, 2f, 3f), true);
            Assert.IsTrue(EspacioLibre.Libre(punto), "Un trigger no estorba");
            Object.DestroyImmediate(zona);

            GameObject moto = Caja("Moto", punto + new Vector3(1.5f, 0.6f, 0.5f), new Vector3(0.7f, 1.2f, 2f));
            Physics.SyncTransforms();
            Assert.IsFalse(EspacioLibre.Libre(punto), "Una moto en el lugar estorba");
            Assert.AreSame(moto.GetComponent<Collider>(), EspacioLibre.Estorbo(punto));
            Assert.IsTrue(EspacioLibre.Libre(punto, moto.transform), "El vehículo que se va a poner no se cuenta a sí mismo");
            Object.DestroyImmediate(moto);

            GameObject jugador = new GameObject("Jugador");
            objetos.Add(jugador);
            CharacterController cc = jugador.AddComponent<CharacterController>();
            jugador.transform.position = punto + Vector3.up * 1f;
            Physics.SyncTransforms();
            Assert.IsFalse(EspacioLibre.Libre(punto), "El jugador a pie en el lugar estorba");
            jugador.transform.position = punto + new Vector3(30f, 1f, 0f);
            Physics.SyncTransforms();
            Assert.IsTrue(EspacioLibre.Libre(punto), "Lejos ya no");
        }

        [UnityTest]
        public IEnumerator Ciudad_ConTodosLosNodosTapados_NingunVehiculoSale()
        {
            Registros.LimpiarTodos();
            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            TraficoCalle trafico = null;
            float limite = Time.realtimeSinceStartup + 40f;
            while ((trafico = Object.FindFirstObjectByType<TraficoCalle>()) == null || trafico.Carros.Count == 0 || Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae el tráfico de calle");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
            CarroTrafico cualquiera = trafico.Carros[0];
            int nodos = cualquiera.NodosDeCalle;
            Assert.Greater(nodos, 4, "Hay red de calles");
            Vector3 jugador = Registro<IJugador>.Actual.Raiz.position;

            // Una "moto" (caja chica) estacionada en cada nodo de la calle: ningún refuerzo ni carro del asalto puede salir.
            List<GameObject> motos = new List<GameObject>();
            for (int i = 0; i < nodos; i++)
            {
                Vector3 n = cualquiera.PosicionNodo(i);
                GameObject moto = Caja("MotoEstacionada_" + i, n + Vector3.up * 0.6f, new Vector3(0.7f, 1.2f, 2f));
                motos.Add(moto);
            }
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsNull(trafico.EnviarRefuerzo(jugador, 16f, 8f, 5f, 50f), "Con una moto en cada salida, el refuerzo no sale encima de ninguna");
            for (int i = 0; i < trafico.Carros.Count; i++)
            {
                CarroTrafico c = trafico.Carros[i];
                if (c != null) Assert.IsFalse(c.EnRefuerzo, "Ningún carro quedó marcado de refuerzo sin salir");
            }

            // Al quitar las motos sí sale (el chequeo no bloquea de más).
            foreach (GameObject m in motos) Object.Destroy(m);
            yield return new WaitForFixedUpdate();
            yield return null;
            CarroTrafico saliente = null;
            for (int intento = 0; intento < 20 && saliente == null; intento++)
            {
                saliente = trafico.EnviarRefuerzo(jugador, 16f, 8f, 5f, 50f);
                if (saliente == null) yield return new WaitForSecondsRealtime(0.25f);
            }
            Assert.IsNotNull(saliente, "Sin motos el refuerzo sale");
        }
    }
}
