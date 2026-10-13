using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>La cámara libre sube y baja del carro viejo con E usando solo el contrato IMontable.</summary>
    public class CamaraLibreCarroPruebas : InputTestFixture
    {
        private const string RutaCamara = "Assets/_Proyecto/Mundo/Prefabs/CamaraLibre.prefab";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaInicial = 1.5f;
        private const float RapidezParaBajar = 0.5f;

        // Mide la distancia entre dos transforms al final de cada frame (después del LateUpdate de la cámara libre).
        [DefaultExecutionOrder(10000)]
        private sealed class Muestreador : MonoBehaviour
        {
            public Transform A;
            public Transform B;
            public float Maxima;

            private void LateUpdate()
            {
                Maxima = Mathf.Max(Maxima, Vector3.Distance(A.position, B.position));
            }
        }

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject carroObjeto;
        private GameObject camaraObjeto;
        private CarroViejo carro;
        private CamaraLibre camara;
        private int cambiosModo;
        private ModoJugador ultimoModo;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
        }

        public override void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            if (carroObjeto != null)
            {
                Object.DestroyImmediate(carroObjeto);
            }
            Object.DestroyImmediate(piso);
            base.TearDown();
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Carro en el origen mirando a +z; la cámara libre a 1.5 m del PuntoSubida, mirándolo.
        private IEnumerator Preparar()
        {
            yield return Calentar();
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), Vector3.zero, Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            Vector3 puerta = carro.PuntoSubida;
            camaraObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCamara),
                puerta + new Vector3(-DistanciaInicial, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f));
            camara = camaraObjeto.GetComponent<CamaraLibre>();
            cambiosModo = 0;
            ultimoModo = camara.Modo;
            camara.ModoCambio += modo => { cambiosModo++; ultimoModo = modo; };
            yield return Esperar(0.5f);
        }

        private IEnumerator PulsarE()
        {
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
        }

        private static int Prendidas<T>() where T : Behaviour
        {
            int cuenta = 0;
            foreach (T componente in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (componente.enabled)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator Subir_ConEMirandoElPuntoDeSubida_EntraAlCarro()
        {
            yield return Preparar();
            Assert.AreEqual(ModoJugador.APie, camara.Modo);

            yield return PulsarE();
            Assert.AreEqual(ModoJugador.Carro, camara.Modo);
            Assert.AreEqual(1, cambiosModo);
            Assert.AreEqual(ModoJugador.Carro, ultimoModo);
            Assert.IsTrue(carro.Ocupado);
            Assert.AreSame(carro.Camara, camara.Camara, "IJugador.Camara debe ser la del carro");
            Assert.IsTrue(carro.Camara.enabled);
            Assert.IsFalse(camaraObjeto.GetComponentInChildren<Camera>().enabled, "La cámara libre debe apagarse");
            Assert.IsFalse(camaraObjeto.GetComponentInChildren<AudioListener>().enabled);
            Assert.IsFalse(camaraObjeto.GetComponent<CharacterController>().enabled);
            Assert.AreEqual(1, Prendidas<AudioListener>());
            Assert.AreEqual(1, Prendidas<Camera>());
        }

        [UnityTest]
        public IEnumerator Subir_LejosODeEspaldas_NoSube()
        {
            yield return Preparar();
            camara.Colocar(new Pose(camaraObjeto.transform.position, Quaternion.Euler(0f, -90f, 0f)));
            yield return PulsarE();
            Assert.AreEqual(ModoJugador.APie, camara.Modo, "De espaldas al carro no debe subir");
            Assert.AreEqual(0, cambiosModo);

            camara.Colocar(new Pose(carro.PuntoSubida + new Vector3(-4f, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f)));
            yield return PulsarE();
            Assert.AreEqual(ModoJugador.APie, camara.Modo, "A 4 m no debe subir");
            Assert.IsFalse(carro.Ocupado);
        }

        [UnityTest]
        public IEnumerator Manejar_ConLaCamaraLibreDentro_LaRaizSigueAlAsiento()
        {
            yield return Preparar();
            yield return PulsarE();
            Muestreador muestreador = camaraObjeto.AddComponent<Muestreador>();
            muestreador.A = camara.Raiz;
            muestreador.B = carro.Asiento;

            Vector3 inicio = carroObjeto.transform.position;
            Press(teclado.wKey);
            Press(teclado.dKey);
            yield return Esperar(2.5f);
            Release(teclado.wKey);
            Release(teclado.dKey);
            Assert.Greater((carroObjeto.transform.position - inicio).magnitude, 8f, "El carro no se movió con W");
            Assert.Less(muestreador.Maxima, 0.3f, "La raíz se separó " + muestreador.Maxima + " m del asiento");
            Assert.AreEqual(ModoJugador.Carro, camara.Modo);
        }

        [UnityTest]
        public IEnumerator Bajar_ConElCarroEnMarcha_NoBajaYEscribeUnaVezFrenaParaBajar()
        {
            yield return Preparar();
            yield return PulsarE();
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            Release(teclado.wKey);
            Assert.Greater(carro.Rapidez, RapidezParaBajar);

            LogAssert.Expect(LogType.Log, "[Mundo] Frena para bajar");
            yield return PulsarE();
            Assert.AreEqual(ModoJugador.Carro, camara.Modo);
            Assert.IsTrue(carro.Ocupado);
            Assert.AreEqual(1, cambiosModo, "No debe avisar ModoCambio si no bajó");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Bajar_Detenido_VuelveAPieYCaminaConW()
        {
            yield return Preparar();
            yield return PulsarE();
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            yield return null;
            Press(teclado.sKey);
            float limite = Time.time + 6f;
            while (carro.Rapidez > 0.25f && Time.time < limite)
            {
                yield return null;
            }
            Release(teclado.sKey);
            yield return Esperar(0.3f);
            Assert.Less(carro.Rapidez, RapidezParaBajar, "El carro no se detuvo");

            yield return PulsarE();
            Assert.AreEqual(ModoJugador.APie, camara.Modo);
            Assert.AreEqual(2, cambiosModo, "Subir y bajar avisan una vez cada uno");
            Assert.AreEqual(ModoJugador.APie, ultimoModo);
            Assert.IsFalse(carro.Ocupado);
            Assert.AreSame(camaraObjeto.GetComponentInChildren<Camera>(), camara.Camara);
            Assert.IsTrue(camara.Camara.enabled);
            Assert.IsFalse(carro.Camara.enabled);
            Assert.IsTrue(camaraObjeto.GetComponent<CharacterController>().enabled);

            Vector3 inicio = camara.Raiz.position;
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater((camara.Raiz.position - inicio).magnitude, 3f, "Después de bajar no camina con W");
        }

        [UnityTest]
        public IEnumerator SubirYBajarDiezVeces_DejaUnaCamaraYUnListenerPrendidosSinErrores()
        {
            yield return Preparar();
            Assert.AreEqual(1, Prendidas<AudioListener>());
            Assert.AreEqual(1, Prendidas<Camera>());
            for (int i = 0; i < 10; i++)
            {
                yield return PulsarE();
                Assert.AreEqual(ModoJugador.Carro, camara.Modo, "Subida " + i);
                Assert.AreEqual(1, Prendidas<AudioListener>(), "Listeners tras subir " + i);
                Assert.AreEqual(1, Prendidas<Camera>(), "Cámaras tras subir " + i);

                yield return PulsarE();
                Assert.AreEqual(ModoJugador.APie, camara.Modo, "Bajada " + i);
                Assert.AreEqual(1, Prendidas<AudioListener>(), "Listeners tras bajar " + i);
                Assert.AreEqual(1, Prendidas<Camera>(), "Cámaras tras bajar " + i);
            }
            Assert.AreEqual(20, cambiosModo);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
