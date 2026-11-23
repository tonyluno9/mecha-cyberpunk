using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1685 (T36): la noche fija del paso 2 al 11 de la historia y la hora a la que regresa el reloj.</summary>
    public class NocheFijaT1685Pruebas
    {
        [Test]
        public void LaHoraFijaEsLaNocheDeLaPartidaNueva()
        {
            Assert.AreEqual(23f, ReglaNocheFija.HoraNoche);
            Assert.AreEqual(PasoRecorrido.Chica, ReglaNocheFija.UltimoPasoDeNoche);
            Assert.AreEqual(11, (int)ReglaNocheFija.UltimoPasoDeNoche);
        }

        [TestCase(PasoRecorrido.Despierta)]
        [TestCase(PasoRecorrido.Molestan)]
        [TestCase(PasoRecorrido.Decision)]
        [TestCase(PasoRecorrido.Dinero)]
        [TestCase(PasoRecorrido.Suero)]
        [TestCase(PasoRecorrido.Armas)]
        [TestCase(PasoRecorrido.Inventor)]
        [TestCase(PasoRecorrido.Chica)]
        public void DelPaso2Al11EsNocheFija(PasoRecorrido paso)
        {
            Assert.IsTrue(ReglaNocheFija.Aplica(paso, true), paso.ToString());
        }

        [TestCase(PasoRecorrido.Ninguno)]
        [TestCase(PasoRecorrido.Recuerdo)]
        [TestCase(PasoRecorrido.GuerraSkulls)]
        [TestCase(PasoRecorrido.Hacker)]
        [TestCase(PasoRecorrido.Fin)]
        [TestCase(PasoRecorrido.Libre)]
        public void FueraDeEsosPasosElRelojCorre(PasoRecorrido paso)
        {
            Assert.IsFalse(ReglaNocheFija.Aplica(paso, true), paso.ToString());
        }

        [Test]
        public void ConElInterruptorApagadoNuncaSeFija()
        {
            Assert.IsFalse(ReglaNocheFija.Aplica(PasoRecorrido.Dinero, false));
            Assert.IsFalse(ReglaNocheFija.Aplica(PasoRecorrido.Chica, false));
        }

        [Test]
        public void ElInterruptorGeneralEmpiezaPrendido()
        {
            bool antes = ReglaNocheFija.Activa;
            try
            {
                ReglaNocheFija.Activa = true;
                Assert.IsTrue(ReglaNocheFija.Aplica(PasoRecorrido.Suero));
                ReglaNocheFija.Activa = false;
                Assert.IsFalse(ReglaNocheFija.Aplica(PasoRecorrido.Suero));
            }
            finally
            {
                ReglaNocheFija.Activa = antes;
            }
        }

        [TestCase(8f)]     // después de dormir
        [TestCase(12f)]    // mediodía de una partida cargada
        [TestCase(5f)]     // la clínica cruzó la madrugada
        [TestCase(22.5f)]
        [TestCase(0f)]
        public void OtraHoraRegresaALaNoche(float hora)
        {
            Assert.IsTrue(ReglaNocheFija.HayQueVolver(hora), hora.ToString());
        }

        [TestCase(23f)]
        [TestCase(23.5f)]
        [TestCase(23.99f)]
        public void LaMismaNocheNoSeMueve(float hora)
        {
            Assert.IsFalse(ReglaNocheFija.HayQueVolver(hora), hora.ToString());
        }

        [Test]
        public void UnaHoraRaraRegresaALaNoche()
        {
            Assert.IsTrue(ReglaNocheFija.HayQueVolver(float.NaN));
        }
    }
}
