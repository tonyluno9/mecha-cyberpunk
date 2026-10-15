using MechaCyberpunk.Economia;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class ReglaReparacionPruebas
    {
        private const int PorPunto = 10;
        private const int Minimo = 50;

        [TestCase(0f, 0)]
        [TestCase(0.4f, 0)]
        [TestCase(3f, 50)]
        [TestCase(65f, 650)]
        [TestCase(64.2f, 642)]
        [TestCase(64.25f, 643)]
        public void Costo_SegunElDanio(float danio, int esperado)
        {
            Assert.AreEqual(esperado, ReglaReparacion.Costo(danio, PorPunto, Minimo));
        }
    }
}
