using MechaCyberpunk.Economia;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class FormatoDineroPruebas
    {
        [TestCase(0, "$0")]
        [TestCase(500, "$500")]
        [TestCase(1250, "$1,250")]
        [TestCase(1000000, "$1,000,000")]
        [TestCase(-150, "-$150")]
        [TestCase(-1250, "-$1,250")]
        public void Texto_DaElFormatoEsperado(int dinero, string esperado)
        {
            Assert.AreEqual(esperado, FormatoDinero.Texto(dinero));
        }
    }
}
