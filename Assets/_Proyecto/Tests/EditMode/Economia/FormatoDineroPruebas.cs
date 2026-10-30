using MechaCyberpunk.Economia;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class FormatoDineroPruebas
    {
        [TestCase(0, "Ƀ0")]
        [TestCase(500, "Ƀ500")]
        [TestCase(1250, "Ƀ1,250")]
        [TestCase(1000000, "Ƀ1,000,000")]
        [TestCase(-150, "-Ƀ150")]
        [TestCase(-1250, "-Ƀ1,250")]
        public void Texto_DaElFormatoEsperado(int dinero, string esperado)
        {
            Assert.AreEqual(esperado, FormatoDinero.Texto(dinero));
        }
    }
}
