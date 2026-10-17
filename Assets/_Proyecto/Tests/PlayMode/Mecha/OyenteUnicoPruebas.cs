using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// Pruebas_Calle con entrada simulada: al subir, encender y bajar del mecha hay siempre exactamente un AudioListener
    /// prendido y Unity no avisa de dos oyentes. También la orientación del mecha estacionado hacia la calle.
    /// </summary>
    public class OyenteUnicoPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MargenEspera = 2f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 10f;
        private const float MaxSegundosEncender = 10f;
        private const float MaxSegundosFrenar = 6f;
        private const float MaxGradosACalle = 15f;
        // La calle corre a lo largo de +Z (ver CreadorEscenaCalle).
        private static readonly Vector3 DireccionCalle = Vector3.forward;

        private Keyboard teclado;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private int oyentesMaximo;
        private int oyentesMinimo;
        private readonly List<string> avisos = new List<string>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            avisos.Clear();
            Application.logMessageReceived += AlRegistrar;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= AlRegistrar;
            Registros.LimpiarTodos();
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
        }

        private void AlRegistrar(string mensaje, string pila, LogType tipo)
        {
            if (mensaje.ToLowerInvariant().Contains("audio listeners"))
            {
                avisos.Add(mensaje);
            }
        }

        private IEnumerator CargarCalle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            yield return null;
        }

        private static int OyentesPrendidos()
        {
            int cuenta = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled && oyente.gameObject.activeInHierarchy)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        // Un frame de juego y se anota cuántos oyentes hay prendidos (el mínimo y el máximo de toda la prueba).
        private IEnumerator Frame()
        {
            yield return null;
            int cuenta = OyentesPrendidos();
            oyentesMaximo = Mathf.Max(oyentesMaximo, cuenta);
            oyentesMinimo = Mathf.Min(oyentesMinimo, cuenta);
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return Frame();
            Release(tecla);
            yield return Frame();
        }

        [UnityTest]
        public IEnumerator SubirEncenderYBajar_EnCadaFrame_HayUnSoloOyente()
        {
            yield return CargarCalle();
            oyentesMaximo = 0;
            oyentesMinimo = int.MaxValue;
            yield return Frame();
            Assert.AreEqual(1, OyentesPrendidos(), "A pie hay un oyente");

            yield return Tecla(teclado.mKey);
            float fin = Time.time + llamado.Config.SegundosLlegada + MargenEspera;
            while (Time.time < fin && llamado.Estado != EstadoLlamado.EnCalle)
            {
                yield return Frame();
            }
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado, "El mecha llegó");

            Press(teclado.wKey);
            fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida) >= DistanciaParaSubir)
            {
                yield return Frame();
            }
            Release(teclado.wKey);
            yield return Frame();

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "La E sube al mecha");
            Assert.AreEqual(1, OyentesPrendidos(), "En el mecha hay un oyente");

            EncendidoMecha encendido = montaje.GetComponent<EncendidoMecha>();
            yield return Tecla(teclado.fKey);
            Press(teclado.spaceKey);
            fin = Time.time + MaxSegundosEncender;
            while (Time.time < fin && encendido.Paso != PasoEncendido.Paso3)
            {
                yield return Frame();
            }
            Release(teclado.spaceKey);
            yield return Tecla(teclado.qKey);
            while (Time.time < fin && !encendido.EstaEncendido)
            {
                yield return Frame();
            }
            Assert.IsTrue(encendido.EstaEncendido, "La cabina quedó encendida");

            fin = Time.time + MaxSegundosFrenar;
            while (Time.time < fin && !montaje.PuedeBajar)
            {
                yield return Frame();
            }
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "La E baja del mecha");
            yield return Frame();
            Assert.AreEqual(1, OyentesPrendidos(), "A pie otra vez hay un oyente");

            Assert.AreEqual(1, oyentesMaximo, "Nunca hubo dos oyentes prendidos");
            Assert.AreEqual(1, oyentesMinimo, "Nunca hubo cero oyentes prendidos");
            Assert.IsEmpty(avisos, "Unity no avisó de dos AudioListener: " + string.Join(" | ", avisos));
        }

        [UnityTest]
        public IEnumerator Arrancar_MechaEstacionado_MiraHaciaLaCalle()
        {
            yield return CargarCalle();
            Vector3 frente = montaje.transform.forward;
            frente.y = 0f;
            float angulo = Vector3.Angle(frente, DireccionCalle);
            Assert.Less(angulo, MaxGradosACalle, "El +Z del mecha estacionado apunta a la calle (grados medidos: " + angulo + ")");
        }
    }
}
