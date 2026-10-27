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

        [TestCase(120, 12)]
        [TestCase(30, 0)]
        [TestCase(49, 0)]
        [TestCase(50, 5)]
        [TestCase(0, 0)]
        [TestCase(-20, 0)]
        public void PuntosPorDinero_SegunElDinero(int dinero, int esperado)
        {
            Assert.AreEqual(esperado, ReglaReparacion.PuntosPorDinero(dinero, PorPunto, Minimo));
        }

        [Test]
        public void PuntosPorDinero_AlcanzaParaElCostoDeEsosPuntos()
        {
            int puntos = ReglaReparacion.PuntosPorDinero(55, 20, 50);
            Assert.LessOrEqual(ReglaReparacion.Costo(puntos, 20, 50), 55);
        }

        [TestCase(60f, 30)]
        [TestCase(30f, 15)]
        [TestCase(11f, 6)]
        [TestCase(10f, 5)]
        [TestCase(9f, 0)]
        [TestCase(0f, 0)]
        public void PuntosDeLaMitad_RedondeaHaciaArribaYExigeDobleDelMinimo(float danio, int esperado)
        {
            Assert.AreEqual(esperado, ReglaReparacion.PuntosDeLaMitad(danio, PorPunto, Minimo));
        }
    }
}
