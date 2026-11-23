using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-62: la ventana de aviso pausa el juego mientras está abierta, ENTENDIDO la cierra y todo vuelve; los avisos en cola salen uno tras otro.</summary>
    public class VentanaAvisoT62Pruebas
    {
        [TearDown]
        public void Limpiar()
        {
            while (VentanaAviso.Abierta)
            {
                VentanaAviso.Entender();
            }
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Mostrar_PausaYEntenderLoDevuelve()
        {
            bool entendido = false;
            VentanaAviso.Mostrar("PRUEBA UNO", () => entendido = true);
            yield return null;
            Assert.IsTrue(VentanaAviso.Abierta);
            Assert.AreEqual("PRUEBA UNO", VentanaAviso.TextoActual);
            Assert.AreEqual(0f, Time.timeScale, "el juego se pausa");
            Assert.IsTrue(Pausa.Activa);
            Assert.IsNotNull(VentanaAviso.BotonActual, "tiene su botón ENTENDIDO");
            VentanaAviso.BotonActual.AlPulsar.Invoke();
            yield return null;
            Assert.IsFalse(VentanaAviso.Abierta);
            Assert.IsTrue(entendido);
            Assert.AreEqual(1f, Time.timeScale, "el tiempo vuelve");
            Assert.IsFalse(Pausa.Activa);
        }

        [UnityTest]
        public IEnumerator Mostrar_DosAvisos_SalenUnoTrasOtro()
        {
            VentanaAviso.Mostrar("PRIMERO");
            VentanaAviso.Mostrar("SEGUNDO");
            yield return null;
            Assert.AreEqual("PRIMERO", VentanaAviso.TextoActual);
            VentanaAviso.Entender();
            Assert.IsTrue(VentanaAviso.Abierta);
            Assert.AreEqual("SEGUNDO", VentanaAviso.TextoActual);
            Assert.AreEqual(0f, Time.timeScale);
            VentanaAviso.Entender();
            Assert.IsFalse(VentanaAviso.Abierta);
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
