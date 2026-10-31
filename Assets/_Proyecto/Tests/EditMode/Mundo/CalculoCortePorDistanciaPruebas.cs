using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-425: cuenta de la altura de corte por distancia del decorado chico.</summary>
    public class CalculoCortePorDistanciaPruebas
    {
        [Test]
        public void ObjetoDeUnMetroACienMetrosConFov60()
        {
            // 1 / (2 * 100 * tan(30°)) = 0.00866
            Assert.AreEqual(0.00866f, CalculoCortePorDistancia.AlturaDeCorte(1f, 100f, 60f), 0.0001f);
        }

        [Test]
        public void MasLejosCortaConAlturaMasPequena()
        {
            Assert.Less(CalculoCortePorDistancia.AlturaDeCorte(2f, 120f, 60f), CalculoCortePorDistancia.AlturaDeCorte(2f, 60f, 60f));
        }

        [Test]
        public void MasGrandeCortaConAlturaMasGrande()
        {
            Assert.Greater(CalculoCortePorDistancia.AlturaDeCorte(2f, 70f, 60f), CalculoCortePorDistancia.AlturaDeCorte(1f, 70f, 60f));
        }

        [Test]
        public void DatosInvalidosNoDanNaN()
        {
            Assert.AreEqual(0f, CalculoCortePorDistancia.AlturaDeCorte(0f, 70f, 60f));
            Assert.AreEqual(0f, CalculoCortePorDistancia.AlturaDeCorte(1f, 0f, 60f));
            Assert.LessOrEqual(CalculoCortePorDistancia.AlturaDeCorte(500f, 1f, 60f), 1f);
        }

        [Test]
        public void SoloLoChicoSeCorta()
        {
            Assert.IsTrue(CalculoCortePorDistancia.EsChico(1.2f, 2.5f));
            Assert.IsTrue(CalculoCortePorDistancia.EsChico(2.5f, 2.5f));
            Assert.IsFalse(CalculoCortePorDistancia.EsChico(8f, 2.5f));
            Assert.IsFalse(CalculoCortePorDistancia.EsChico(0f, 2.5f));
        }
    }
}
