using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class ModeloCarteraPruebas
    {
        private ModeloCartera cartera;
        private int avisos;
        private int ultimaDiferencia;
        private MotivoDinero ultimoMotivo;

        [SetUp]
        public void SetUp()
        {
            cartera = new ModeloCartera(1000);
            avisos = 0;
            cartera.Cambio += (dif, motivo) => { avisos++; ultimaDiferencia = dif; ultimoMotivo = motivo; };
        }

        [Test]
        public void IntentarPagar_ConDinero_RestaYAvisaUnaVez()
        {
            Assert.IsTrue(cartera.IntentarPagar(300, MotivoDinero.Compra));
            Assert.AreEqual(700, cartera.Dinero);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(-300, ultimaDiferencia);
            Assert.AreEqual(MotivoDinero.Compra, ultimoMotivo);
        }

        [Test]
        public void IntentarPagar_SinDinero_NoCambiaNiAvisa()
        {
            Assert.IsFalse(cartera.IntentarPagar(1001, MotivoDinero.Compra));
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void IntentarPagar_CantidadExacta_Cobra()
        {
            Assert.IsTrue(cartera.IntentarPagar(1000, MotivoDinero.Compra));
            Assert.AreEqual(0, cartera.Dinero);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Operaciones_CantidadNoPositiva_NoHacenNada(int cantidad)
        {
            Assert.IsFalse(cartera.IntentarPagar(cantidad, MotivoDinero.Compra));
            cartera.Abonar(cantidad, MotivoDinero.Premio);
            cartera.CargarDeuda(cantidad, MotivoDinero.Reparacion);
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Abonar_Suma_YAvisa()
        {
            cartera.Abonar(250, MotivoDinero.Premio);
            Assert.AreEqual(1250, cartera.Dinero);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(250, ultimaDiferencia);
            Assert.AreEqual(MotivoDinero.Premio, ultimoMotivo);
        }

        [Test]
        public void CargarDeuda_PuedeDejarDineroNegativo()
        {
            cartera.CargarDeuda(1500, MotivoDinero.Reparacion);
            Assert.AreEqual(-500, cartera.Dinero);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(-1500, ultimaDiferencia);
            Assert.AreEqual(MotivoDinero.Reparacion, ultimoMotivo);
        }

        [TestCase(1, true)]
        [TestCase(1000, true)]
        [TestCase(1001, false)]
        [TestCase(0, false)]
        [TestCase(-1, false)]
        public void PuedePagar_CoincideConIntentarPagar(int cantidad, bool esperado)
        {
            Assert.AreEqual(esperado, cartera.PuedePagar(cantidad));
            Assert.AreEqual(esperado, cartera.IntentarPagar(cantidad, MotivoDinero.Compra));
        }
    }
}
