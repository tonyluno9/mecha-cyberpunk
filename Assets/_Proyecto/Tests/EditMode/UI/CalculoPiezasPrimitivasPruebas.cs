using MechaCyberpunk.UI;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    public class CalculoPiezasPrimitivasPruebas
    {
        [Test]
        public void FactorSobrePedestal_LimitaLaBajadaYNoTocaLoQueSube()
        {
            Assert.AreEqual(1f, CalculoPiezasPrimitivas.FactorSobrePedestal(0.3f, -0.1f, -0.4f, 0.03f));
            Assert.AreEqual(1f, CalculoPiezasPrimitivas.FactorSobrePedestal(-0.1f, -0.1f, -0.4f, 0.03f), "Si cabe, baja completo");
            float factor = CalculoPiezasPrimitivas.FactorSobrePedestal(-0.55f, -0.1f, -0.4f, 0.03f);
            Assert.AreEqual(-0.1f - 0.55f * factor, -0.4f + 0.03f, 0.0001f, "Se queda justo sobre el pedestal más el margen");
            Assert.AreEqual(0f, CalculoPiezasPrimitivas.FactorSobrePedestal(-0.5f, -0.41f, -0.4f, 0.03f), "Si ya está bajo el techo no baja");
        }
    }
}
