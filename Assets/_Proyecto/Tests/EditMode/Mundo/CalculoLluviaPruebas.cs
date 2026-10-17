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
    }
}
