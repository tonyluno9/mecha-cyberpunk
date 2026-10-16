using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// Integración T-061: el jugador a pie de Tony sube, maneja y baja del carro de Alan en Pruebas_Alan.unity.
    /// La escena se carga en la prueba sin editarla; el carro se encuentra por Registro&lt;IMontable&gt; (sin referenciar Vehiculos/).
    /// </summary>
    public class CarroAlanPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MetrosAlPuntoSubida = 1.5f;
        private const float AlturaPies = 0.05f;
        private const float SegundosManejando = 3f;
        private const float MetrosMinimosManejando = 5f;
        private const float DistanciaAsientoMaxima = 0.3f;
        private const float MaxSegundosFrenar = 8f;
        private const float HoraNoche = 22f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject jugador;
        private JugadorAPie jugadorAPie;
        private IMontable carro;
        private Component carroComponente;
        private HudAPie hud;
        private readonly MedidorFrames medidor = new MedidorFrames();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            medidor.Reiniciar();
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        private IEnumerator Frame()
        {
            yield return null;
            medidor.Registrar(Time.unscaledDeltaTime);
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return Frame();
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return Frame();
            Release(tecla);
            yield return Frame();
        }

        private static int OyentesPrendidos()
        {
            int cuenta = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        private IEnumerator Preparar()
        {
            AsyncOperation carga = EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return carga;

            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }

            foreach (IJugador otro in new List<IJugador>(Registro<IJugador>.Todos))
            {
                if (otro is Component componente)
                {
                    componente.gameObject.SetActive(false);
                }
            }

            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(reloj, "El barrio trae su reloj");
            reloj.Pausar(true);
            reloj.AvanzarHasta(HoraNoche);

            carro = null;
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable.Modo == ModoJugador.Carro)
                {
                    carro = montable;
                }
            }
            Assert.IsNotNull(carro, "Pruebas_Alan trae un IMontable de modo Carro");
            carroComponente = carro as Component;
            Assert.IsNotNull(carroComponente);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            jugador = Object.Instantiate(prefab);
            yield return null;
            yield return null;
            jugadorAPie = jugador.GetComponent<JugadorAPie>();
            hud = jugador.GetComponentInChildren<HudAPie>(true);
            Assert.IsNotNull(hud, "El HUD viene con el jugador");
            Assert.AreSame(jugadorAPie, Registro<IJugador>.Actual, "Un solo IJugador: el de Tony");

            // A 1.5 m del PuntoSubida, mirándolo.
            Vector3 punto = carro.PuntoSubida;
            Vector3 haciaCarro = carroComponente.transform.position - punto;
            haciaCarro.y = 0f;
            Vector3 lado = -haciaCarro.normalized;
            Vector3 posicion = punto + lado * MetrosAlPuntoSubida;
            posicion.y = punto.y + AlturaPies;
            Vector3 mirada = punto - posicion;
            mirada.y = 0f;
            jugadorAPie.Colocar(new Pose(posicion, Quaternion.LookRotation(mirada.normalized, Vector3.up)));
            yield return Esperar(0.5f);
        }

        private bool DentroDeAlgunCollider(Vector3 punto)
        {
            foreach (Collider collider in carroComponente.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger && collider.enabled && (collider.ClosestPoint(punto) - punto).sqrMagnitude < 0.0001f)
                {
                    return true;
                }
            }
            return false;
        }

        [UnityTest]
        public IEnumerator CarroDeAlan_SubeManejaYBajaConE()
        {
            yield return Preparar();
            Assert.IsTrue(carro.PuedeSubir);
            Assert.AreEqual(1, OyentesPrendidos(), "A pie solo oye el jugador");

            // E sube.
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo);
            Assert.IsTrue(carro.Ocupado);
            Assert.AreSame(carro.Camara, jugadorAPie.Camara);
            Assert.AreEqual(1, OyentesPrendidos(), "Un solo AudioListener habilitado dentro");

            // Manejando, la raíz sigue al asiento.
            Vector3 inicio = carroComponente.transform.position;
            Press(teclado.wKey);
            yield return Esperar(SegundosManejando);
            Assert.Greater(Vector3.Distance(inicio, carroComponente.transform.position), MetrosMinimosManejando, "El carro avanza con W");
            Assert.Less(Vector3.Distance(jugador.transform.position, carro.Asiento.position), DistanciaAsientoMaxima, "La raíz sigue al asiento");

            // En marcha, E no baja y el HUD avisa.
            Assert.IsFalse(carro.PuedeBajar, "En marcha no puede bajar");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "En marcha E no baja");
            Assert.IsTrue(carro.Ocupado);
            Assert.AreEqual("Frena para bajar", hud.TextoAvisoInteraccion.text);

            // Se detiene y E baja.
            Release(teclado.wKey);
            float limite = Time.time + MaxSegundosFrenar;
            while (!carro.PuedeBajar && Time.time < limite)
            {
                yield return Frame();
            }
            Assert.IsTrue(carro.PuedeBajar, "El carro llega a poder bajar");
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo);
            Assert.IsFalse(carro.Ocupado);
            Assert.AreEqual(1, OyentesPrendidos(), "Un solo AudioListener habilitado afuera");
            Assert.IsTrue(jugador.GetComponent<CharacterController>().enabled);
            Assert.IsFalse(DentroDeAlgunCollider(jugador.transform.position), "Queda fuera del collider del carro");
            Assert.IsTrue(Physics.Raycast(jugador.transform.position + Vector3.up * 0.5f, Vector3.down, 2f), "Queda sobre el piso");

            // Camina con W.
            yield return Esperar(0.3f);
            Vector3 antes = jugador.transform.position;
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Vector3 recorrido = jugador.transform.position - antes;
            recorrido.y = 0f;
            Assert.Greater(recorrido.magnitude, 0.5f, "Con W camina otra vez");
            Assert.IsTrue(medidor.Cumple, "Frames: " + medidor.Resumen());
        }
    }
}
