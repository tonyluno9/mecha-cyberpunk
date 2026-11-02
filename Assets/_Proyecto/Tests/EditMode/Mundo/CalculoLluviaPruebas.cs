using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    public class CalculoLluviaPruebas
    {
        [Test]
        public void DebeLlover_DeNocheAfuera_Si()
        {
            Assert.IsTrue(CalculoLluvia.DebeLlover(true, false, true, false));
        }

        [Test]
        public void DebeLlover_EnInterior_No()
        {
            Assert.IsFalse(CalculoLluvia.DebeLlover(true, true, true, true));
        }

        [Test]
        public void DebeLlover_DeDiaSinPermiso_No()
        {
            Assert.IsFalse(CalculoLluvia.DebeLlover(true, false, false, false));
        }

        [Test]
        public void DebeLlover_DeDiaConPermiso_Si()
        {
            Assert.IsTrue(CalculoLluvia.DebeLlover(true, false, false, true));
        }

        [Test]
        public void DebeLlover_ConElInterruptorApagado_No()
        {
            Assert.IsFalse(CalculoLluvia.DebeLlover(false, false, true, true));
        }
    
        [Test]
        public void Intensidad_DeNoche_Toda()
        {
            Assert.AreEqual(1f, CalculoLluvia.Intensidad(true, 0.3f));
        }

        [Test]
        public void Intensidad_DeDia_LaFraccionLigera()
        {
            Assert.AreEqual(0.3f, CalculoLluvia.Intensidad(false, 0.3f), 1e-5f);
            Assert.AreEqual(1f, CalculoLluvia.Intensidad(false, 4f), "Se recorta a 1");
        }
    }
}
