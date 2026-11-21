using MechaCyberpunk.Casino.Tragamonedas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Casino
{
    /// <summary>T-1535: qué sonido de premio pide la tragamonedas.</summary>
    public class ReglaSonidoTragamonedasPruebas
    {
        [Test]
        public void Premio_SinGanar_NoSuenaNada()
        {
            Assert.IsNull(ReglaSonidoTragamonedas.Premio(0, 50, 15));
        }

        [TestCase(50, "CAS_PREMIO_CHICO")]
        [TestCase(300, "CAS_PREMIO_CHICO")]
        [TestCase(400, "CAS_PREMIO_CHICO")]
        [TestCase(749, "CAS_PREMIO_CHICO")]
        [TestCase(750, "CAS_PREMIO_GRANDE")]
        [TestCase(2000, "CAS_PREMIO_GRANDE")]
        [TestCase(5000, "CAS_PREMIO_GRANDE")]
        public void Premio_ConLosDatosBase_ElChicoOElGrande(int premio, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSonidoTragamonedas.Premio(premio, 50, ReglaSonidoTragamonedas.VecesApuestaPremioGrande));
        }

        [Test]
        public void Premio_ConApuestaCero_EsElChico()
        {
            Assert.AreEqual("CAS_PREMIO_CHICO", ReglaSonidoTragamonedas.Premio(100, 0, 15));
        }
    }
}
