using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Movimiento;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>Subir y bajar del mecha estacionado en Pruebas_Calle con el jugador real y entrada simulada.</summary>
    public class MontajeMechaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaTony = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 8f;
        private const float MaxSegundosFrenar = 6f;
        private const float SegundosManejando = 1.5f;
        private const float DistanciaAsientoMaxima = 0.05f;
        private const float DistanciaMaximaAlBajar = 3f;
        private const float LadoObstaculo = 1.2f;
        private const float AlturaObstaculo = 3f;
        private const float DistanciaObstaculo = 2f;
        private const float MinimoMovido = 0.5f;

        /// <summary>Mide, después de todos los LateUpdate, qué tan lejos está la raíz del jugador del asiento.</summary>
        [DefaultExecutionOrder(10000)]
        public class MedidorAsiento : MonoBehaviour
        {
            public Transform Jugador;
            public Transform Asiento;
            public float Maxima;
            public int Muestras;

            private void LateUpdate()
            {
                Maxima = Mathf.Max(Maxima, Vector3.Distance(Jugador.position, Asiento.position));
                Muestras++;
            }
        }

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private InteraccionAPie interaccion;
        private MontajeMecha montaje;
        private ControladorMecha controlador;
        private int bajarNegado;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            bajarNegado = 0;
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        // La escena se cargó en Single: se destruyen sus raíces para que no contamine otras pruebas.
        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private IEnumerator CargarEscena(string ruta)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private IEnumerator CargarCalle()
        {
            yield return CargarEscena(RutaCalle);
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            interaccion = jugador.GetComponent<InteraccionAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(montaje, "Pruebas_Calle trae el mecha");
            controlador = montaje.GetComponent<ControladorMecha>();
            interaccion.BajarNegado += () => bajarNegado++;
            yield return null;
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // A pie: W hasta quedar a menos de 2 m del punto de subida.
        private IEnumerator CaminarAlMecha()
        {
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida) >= DistanciaParaSubir)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.Less(Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida), DistanciaParaSubir, "Llegó al mecha");
        }

        private IEnumerator Subir()
        {
            yield return CaminarAlMecha();
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "La E frente al mecha lo sube");
        }

        // Espera a que el mecha esté casi detenido.
        private IEnumerator Frenar()
        {
            float fin = Time.time + MaxSegundosFrenar;
            while (Time.time < fin && controlador.VelocidadLocal.magnitude > 0.1f)
            {
                yield return null;
            }
            Assert.Less(controlador.VelocidadLocal.magnitude, 0.5f, "El mecha frenó");
        }

        private static int Contar<T>(System.Func<T, bool> prendido) where T : Object
        {
            int cuenta = 0;
            foreach (T objeto in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (prendido(objeto))
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        private GameObject Obstaculo(Vector3 centro)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.transform.position = centro + Vector3.up * (AlturaObstaculo / 2f);
            caja.transform.localScale = new Vector3(LadoObstaculo, AlturaObstaculo, LadoObstaculo);
            return caja;
        }

        private Vector3 PuntoAlrededor(float grados)
        {
            return montaje.transform.position + Quaternion.Euler(0f, grados, 0f) * montaje.transform.forward * DistanciaObstaculo;
        }

        [UnityTest]
        public IEnumerator Calle_RecienCargada_TraeUnJugadorUnMontableUnaCamaraYUnOido()
        {
            yield return CargarCalle();

            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Un solo jugador");
            Assert.AreEqual(1, Registro<IMontable>.Cantidad, "Un solo montable");
            Assert.AreEqual(ModoJugador.Mecha, Registro<IMontable>.Actual.Modo);
            Assert.AreEqual(1, Contar<Camera>(c => c.enabled), "Una sola cámara prendida");
            Assert.AreEqual(1, Contar<AudioListener>(o => o.enabled), "Un solo oído prendido");
            Assert.IsFalse(montaje.Ocupado);
            Assert.IsTrue(montaje.PuedeSubir);
        }

        [UnityTest]
        public IEnumerator Subir_APieConE_PasaAModoMechaConSuCamaraYUnSoloOido()
        {
            yield return CargarCalle();
            yield return Subir();

            Assert.AreSame(montaje.Camara, jugador.Camara, "La cámara del jugador es la del mecha");
            Assert.IsTrue(montaje.Camara.enabled);
            Assert.AreEqual(1, Contar<Camera>(c => c.enabled), "Una sola cámara prendida");
            Assert.AreEqual(1, Contar<AudioListener>(o => o.enabled), "Un solo oído prendido");
            Assert.IsTrue(montaje.Ocupado);
            Assert.IsTrue(montaje.GetComponent<PeleadorMecha>().ControlActivo, "El mecha tiene control con piloto");
        }

        [UnityTest]
        public IEnumerator Manejar_ConW_MueveAlMechaYElJugadorVaEnElAsiento()
        {
            yield return CargarCalle();
            yield return Subir();

            MedidorAsiento medidor = montaje.gameObject.AddComponent<MedidorAsiento>();
            medidor.Jugador = jugador.Raiz;
            medidor.Asiento = montaje.Asiento;
            Vector3 antes = montaje.transform.position;

            Press(teclado.wKey);
            yield return Esperar(SegundosManejando);
            Release(teclado.wKey);
            yield return null;

            Assert.Greater(Vector3.Distance(antes, montaje.transform.position), MinimoMovido, "W mueve al mecha");
            Assert.Greater(medidor.Muestras, 10, "Se midieron varios frames");
            Assert.Less(medidor.Maxima, DistanciaAsientoMaxima, "El jugador va pegado al asiento cada frame");
        }

        [UnityTest]
        public IEnumerator Bajar_ConE_NiegaMoviendoseYBajaDetenidoFueraDelCuerpo()
        {
            yield return CargarCalle();
            yield return Subir();

            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosFrenar;
            while (Time.time < fin && controlador.VelocidadLocal.magnitude <= 1f)
            {
                yield return null;
            }
            Assert.Greater(controlador.VelocidadLocal.magnitude, 1f, "El mecha va en movimiento");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(1, bajarNegado, "Moviéndose, la E avisa BajarNegado");
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "Moviéndose no baja");

            Release(teclado.wKey);
            yield return Frenar();
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Detenido, la E baja");
            Assert.IsFalse(montaje.Ocupado);
            CharacterController cuerpo = montaje.GetComponent<CharacterController>();
            Assert.IsFalse(cuerpo.bounds.Contains(jugador.Raiz.position), "El jugador quedó fuera del cuerpo del mecha");
            Assert.Less(Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida), DistanciaMaximaAlBajar, "Bajó junto a la escotilla");
            Assert.AreSame(jugador.GetComponentInChildren<Camera>(), jugador.Camara, "Vuelve su cámara");
            Assert.AreEqual(1, Contar<Camera>(c => c.enabled), "Una sola cámara prendida");
            Assert.AreEqual(1, Contar<AudioListener>(o => o.enabled), "Un solo oído prendido");
            Assert.IsFalse(montaje.GetComponent<PeleadorMecha>().ControlActivo, "Vacío, el mecha no tiene control");
        }

        [UnityTest]
        public IEnumerator Bajar_ConElFrenteBloqueado_BajaPorOtroLado()
        {
            yield return CargarCalle();
            yield return Subir();
            yield return Frenar();

            Vector3 frente = PuntoAlrededor(0f);
            Obstaculo(frente);
            yield return null;

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Baja por otro lado");
            Assert.Greater(Vector3.Distance(jugador.Raiz.position, frente), LadoObstaculo, "No quedó en el frente bloqueado");
            Assert.IsFalse(montaje.GetComponent<CharacterController>().bounds.Contains(jugador.Raiz.position));
        }

        [UnityTest]
        public IEnumerator Bajar_ConTodoBloqueado_NoBaja()
        {
            yield return CargarCalle();
            yield return Subir();
            yield return Frenar();

            foreach (float grados in new[] { 0f, -90f, 90f, 180f })
            {
                Obstaculo(PuntoAlrededor(grados));
            }
            yield return null;

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "Con todo bloqueado no baja");
            Assert.IsTrue(montaje.Ocupado);
            Assert.IsTrue(montaje.Camara.enabled, "Sigue dentro, con su cámara");
        }

        [UnityTest]
        public IEnumerator Estacionado_Vacio_NoSeMueveNiGolpea()
        {
            yield return CargarCalle();
            Vector3 posicion = montaje.transform.position;
            Quaternion rotacion = montaje.transform.rotation;

            Press(teclado.wKey);
            Press(teclado.aKey);
            Press(raton.leftButton);
            yield return Esperar(SegundosManejando);
            Release(teclado.wKey);
            Release(teclado.aKey);
            Release(raton.leftButton);
            yield return null;

            Assert.AreEqual(0f, Vector3.Distance(posicion, montaje.transform.position), 0.01f, "WASD no mueve al mecha vacío");
            Assert.AreEqual(0f, Quaternion.Angle(rotacion, montaje.transform.rotation), 0.1f, "No gira");
            Assert.IsFalse(montaje.GetComponent<PeleadorMecha>().ControlActivo);
            SerializedProperty lista = new SerializedObject(montaje.GetComponent<PeleadorMecha>()).FindProperty("controlados");
            Assert.Greater(lista.arraySize, 0, "El mecha tiene componentes controlados");
            for (int i = 0; i < lista.arraySize; i++)
            {
                Behaviour controlado = (Behaviour)lista.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsFalse(controlado.enabled, controlado.GetType().Name + " apagado sin piloto");
            }
        }

        [UnityTest]
        public IEnumerator Directo_EnPruebasTony_NoRegistraMontableYSeMueveConWASD()
        {
            yield return CargarEscena(RutaTony);

            Assert.AreEqual(0, Registro<IMontable>.Cantidad, "Un mecha Directo no es montable");
            ControladorMecha mecha = Object.FindFirstObjectByType<ControladorMecha>();
            Assert.IsNotNull(mecha);
            MontajeMecha montajeDirecto = mecha.GetComponent<MontajeMecha>();
            Assert.IsNotNull(montajeDirecto);
            Assert.IsFalse(montajeDirecto.PuedeSubir);
            Assert.IsTrue(mecha.GetComponent<PeleadorMecha>().ControlActivo);
            Assert.IsTrue(montajeDirecto.Camara.enabled, "Su cámara está prendida desde el inicio");

            Vector3 antes = mecha.transform.position;
            Press(teclado.wKey);
            yield return Esperar(SegundosManejando);
            Release(teclado.wKey);
            yield return null;

            Assert.Greater(Vector3.Distance(antes, mecha.transform.position), MinimoMovido, "W mueve al mecha como siempre");
        }
    }
}
