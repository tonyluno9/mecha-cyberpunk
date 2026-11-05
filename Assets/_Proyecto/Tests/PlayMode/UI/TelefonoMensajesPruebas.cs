using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Misiones;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-449: el teléfono tiene cinco pestañas con MENSAJES al final (← → las recorren), sin mensajes dice que no hay y con uno lo muestra.
    /// Con CAPTURAR_T449=1 guarda docs/capturas/T-449-1.png (el teléfono en MENSAJES).
    /// </summary>
    public class TelefonoMensajesPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T449";
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Telefono_CincoPestanas_ConMensajesAlFinal()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaTelefono telefono = null;
            yield return EsperarHasta(() =>
            {
                telefono = UnityEngine.Object.FindFirstObjectByType<PantallaTelefono>(FindObjectsInactive.Include);
                return telefono != null && Registro<IJugador>.Actual != null && Registro<ServicioMensajes>.Actual != null;
            }, "el teléfono y los mensajes", 30f);
            yield return new WaitForSecondsRealtime(1f);
            ServicioMensajes mensajes = Registro<ServicioMensajes>.Actual;

            // Sin mensajes: la pestaña lo dice.
            yield return Pulsar(teclado.tKey);
            Assert.IsTrue(telefono.Abierto);
            for (int i = 0; i < 4; i++)
            {
                yield return Pulsar(teclado.rightArrowKey);
            }
            Assert.AreEqual(PestanaTelefono.Mensajes, telefono.Pestana, "Cuatro → llegan a MENSAJES");
            StringAssert.Contains("SIN MENSAJES", telefono.Contenido);
            yield return Pulsar(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTelefono.Mensajes, telefono.Pestana, "No hay una sexta pestaña");
            yield return Pulsar(teclado.leftArrowKey);
            Assert.AreEqual(PestanaTelefono.Favores, telefono.Pestana, "← regresa a FAVORES");
            yield return Pulsar(teclado.tKey);
            Assert.IsFalse(telefono.Abierto);

            // Con un mensaje: se ve, con su remitente.
            Assert.IsTrue(mensajes.Entregar(ReglaMensajes.IdLibre, 1));
            yield return Pulsar(teclado.tKey);
            for (int i = 0; i < 4; i++)
            {
                yield return Pulsar(teclado.rightArrowKey);
            }
            StringAssert.Contains("EL INVENTOR", telefono.Contenido);
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                Camera camara = Registro<IJugador>.Actual.Camara;
                CapturaUI.Capturar("docs/capturas/T-449-1.png", camara);
                Debug.Log("[Prueba] T-449 captura guardada: " + Path.GetFullPath("docs/capturas/T-449-1.png"));
            }
            yield return Pulsar(teclado.tKey);
        }

        private class RelojFalso : IRelojMundo
        {
            public float Hora { get; set; }
            public int Dia => 1;
            public FranjaDia Franja => FranjaDia.Dia;
            public bool EsDeNoche => false;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado => false;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio { add { } remove { } }
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { Hora = hora; }
        }

        // T-633: el teléfono sube desde abajo al sacarlo, baja al guardarlo y muestra la hora del juego.
        [UnityTest]
        public IEnumerator Telefono_SeDeslizaYMuestraLaHora()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaTelefono telefono = null;
            yield return EsperarHasta(() =>
            {
                telefono = UnityEngine.Object.FindFirstObjectByType<PantallaTelefono>(FindObjectsInactive.Include);
                return telefono != null && Registro<IJugador>.Actual != null;
            }, "el teléfono", 30f);
            yield return new WaitForSecondsRealtime(1f);
            RelojFalso reloj = new RelojFalso { Hora = 14.75f };
            Registro<IRelojMundo>.Agregar(reloj);

            Press(teclado.tKey);
            yield return null;
            Release(teclado.tKey);
            Assert.IsTrue(telefono.Abierto);
            Assert.IsTrue(telefono.Deslizando, "Al sacarlo se desliza");
            Assert.Greater(telefono.DesplazamientoY, 100f, "Sale desde abajo de la pantalla");
            float primero = telefono.DesplazamientoY;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.Less(telefono.DesplazamientoY, primero, "Va subiendo");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(telefono.Deslizando);
            Assert.AreEqual(0f, telefono.DesplazamientoY, 0.5f, "Queda en su sitio");
            Assert.AreEqual("14:45", telefono.Hora, "Muestra la hora del juego");
            reloj.Hora = 15.5f;
            yield return null;
            yield return null;
            Assert.AreEqual("15:30", telefono.Hora, "La hora se actualiza con el teléfono abierto");
            if (Environment.GetEnvironmentVariable("CAPTURAR_T633") == "1")
            {
                CapturaUI.Capturar("docs/capturas/T-633-telefono-hora.png", Registro<IJugador>.Actual.Camara);
            }

            if (Environment.GetEnvironmentVariable("CAPTURAR_T638") == "1")
            {
                CapturaUI.Capturar("docs/capturas/T-638-telefono.png", Registro<IJugador>.Actual.Camara);
            }

            Press(teclado.tKey);
            yield return null;
            Release(teclado.tKey);
            Assert.IsFalse(telefono.Abierto);
            Assert.IsTrue(telefono.Deslizando, "Al guardarlo baja");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(telefono.Deslizando);
            Registro<IRelojMundo>.Quitar(reloj);
        }
    }
}
