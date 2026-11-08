using MechaCyberpunk.UI.Inicio;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    public class CalculoFondoCallesPruebas
    {
        [Test]
        public void Avanzar_AntesDelTiempo_NoCambia()
        {
            CalculoFondoCalles c = new CalculoFondoCalles(4, 8f);
            Assert.IsFalse(c.Avanzar(7.9f));
            Assert.AreEqual(0, c.Actual);
        }

        [Test]
        public void Avanzar_AlCumplirElTiempo_PasaALaSiguiente()
        {
            CalculoFondoCalles c = new CalculoFondoCalles(4, 8f);
            Assert.IsTrue(c.Avanzar(8f));
            Assert.AreEqual(1, c.Actual);
        }

        [Test]
        public void Avanzar_DespuesDeLaUltima_VuelveALaPrimera()
        {
            CalculoFondoCalles c = new CalculoFondoCalles(2, 1f);
            c.Avanzar(1f);
            c.Avanzar(1f);
            Assert.AreEqual(0, c.Actual);
        }

        [Test]
        public void Avanzar_ConUnaSolaCalle_NuncaCambia()
        {
            CalculoFondoCalles c = new CalculoFondoCalles(1, 1f);
            Assert.IsFalse(c.Avanzar(5f));
        }
    }
}
