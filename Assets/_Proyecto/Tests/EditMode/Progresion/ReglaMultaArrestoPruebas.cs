using MechaCyberpunk.Progresion.Prologo.Policia;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-690: la multa del arresto es una fracción del dinero, redondeada hacia abajo, sin deuda.</summary>
    public class ReglaMultaArrestoPruebas
    {
        [TestCase(1000, 0.25f, 250)]
        [TestCase(999, 0.25f, 249)]
        [TestCase(3, 0.25f, 0)]
        [TestCase(100, 1f, 100)]
        [TestCase(100, 2f, 100)]
        [TestCase(100, 0f, 0)]
        public void Multa_EsLaFraccionRedondeadaHaciaAbajo(int dinero, float porcentaje, int esperada)
        {
            Assert.AreEqual(esperada, ReglaMultaArresto.Multa(dinero, porcentaje));
        }

        [TestCase(0)]
        [TestCase(-300)]
        public void Multa_SinDineroOConDeuda_NoCobraNada(int dinero)
        {
            Assert.AreEqual(0, ReglaMultaArresto.Multa(dinero, 0.25f));
        }

        [Test]
        public void PorcentajePorDefecto_EsUnCuarto()
        {
            Assert.AreEqual(0.25f, ReglaMultaArresto.PorcentajePorDefecto, 0.0001f);
        }
    }
}
