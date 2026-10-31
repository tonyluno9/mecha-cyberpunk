using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Misiones;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-449 en Ciudad_Prologo como un jugador: al terminar la historia (F6) llega el mensaje del Inventor con su aviso en el HUD; con dinero llegan
    /// los de la guarida y los implantes; en el teléfono (T, cuatro veces →) la pestaña MENSAJES los lista con el nuevo primero y los marca como
    /// leídos; sin mensajes nuevos no se repiten y todo se guarda y se carga.
    /// </summary>
    public class MensajesPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
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
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
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
        public IEnumerator Mensajes_LlegaronAvisanSeLeenEnElTelefonoYSeGuardan()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null
                    && Registro<ServicioMensajes>.Actual != null && Object.FindFirstObjectByType<AvisoMensaje>() != null
                    && Object.FindFirstObjectByType<PantallaTelefono>(FindObjectsInactive.Include) != null;
            }, "el director arrancó con los mensajes", 30f);
            ServicioMensajes mensajes = Registro<ServicioMensajes>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            AvisoMensaje aviso = Object.FindFirstObjectByType<AvisoMensaje>();
            PantallaTelefono telefono = Object.FindFirstObjectByType<PantallaTelefono>(FindObjectsInactive.Include);
            string ya = string.Empty;
            foreach (ServicioMensajes.MensajeGuardado g in mensajes.Recibidos)
            {
                ya += g.id + " ";
            }
            Assert.AreEqual(0, mensajes.Recibidos.Count, "Al empezar el juego no hay mensajes (ya llegaron: " + ya + "; dinero " + cartera.Dinero + ")");

            // La historia termina y llega el del Inventor, con su aviso.
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            Assert.AreEqual(PasoHistoria.Libre, historia.Paso);
            yield return EsperarHasta(() => mensajes.Recibidos.Count >= 1, "el mensaje del Inventor", 5f);
            Assert.AreEqual(ReglaMensajes.IdLibre, mensajes.Recibidos[0].id);
            Assert.GreaterOrEqual(mensajes.SinLeer, 1);
            yield return EsperarHasta(() => aviso.Visible, "el aviso del mensaje", 3f);
            StringAssert.Contains("EL INVENTOR", aviso.Texto);

            // Con Ƀ2,000 llegan los de la guarida y los implantes.
            cartera.Abonar(3000, MotivoDinero.Premio);
            yield return EsperarHasta(() => mensajes.Recibidos.Count >= 3, "los mensajes por dinero", 5f);
            Assert.AreEqual(ReglaMensajes.IdImplantes, mensajes.Recibidos[mensajes.Recibidos.Count - 1].id, "El de implantes es el más nuevo");
            int cantidad = mensajes.Recibidos.Count;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual(cantidad, mensajes.Recibidos.Count, "Ninguno se repite");

            // El teléfono: T, cuatro veces → hasta MENSAJES, y se leen.
            yield return EsperarHasta(() => !aviso.Visible || aviso.EnCola >= 0, "la cola del aviso", 1f);
            yield return Pulsar(teclado.tKey);
            Assert.IsTrue(telefono.Abierto, "T abre el teléfono");
            for (int i = 0; i < 4; i++)
            {
                yield return Pulsar(teclado.rightArrowKey);
            }
            Assert.AreEqual(PestanaTelefono.Mensajes, telefono.Pestana);
            StringAssert.Contains("EL DOC", telefono.Contenido, "El de implantes sale en la lista");
            StringAssert.Contains("●", telefono.Contenido, "Los nuevos llevan marca de no leído");
            Assert.AreEqual(0, mensajes.SinLeer, "Al verlos quedan leídos");
            Debug.Log("[Prueba] T-449 mensajes en el teléfono: " + telefono.Contenido.Replace("\n", " | "));
            yield return Pulsar(teclado.tKey);
            Assert.IsFalse(telefono.Abierto);

            // Se guarda y se carga.
            string texto = mensajes.Guardar();
            StringAssert.Contains(ReglaMensajes.IdLibre, texto);
            mensajes.Cargar("{}");
            Assert.AreEqual(0, mensajes.Recibidos.Count);
            mensajes.Cargar(texto);
            Assert.AreEqual(cantidad, mensajes.Recibidos.Count);
            Assert.AreEqual(0, mensajes.SinLeer, "Lo leído sigue leído");
            Assert.IsFalse(mensajes.Entregar(ReglaMensajes.IdLibre, 1), "Lo cargado no se vuelve a entregar");
        }
    }
}
