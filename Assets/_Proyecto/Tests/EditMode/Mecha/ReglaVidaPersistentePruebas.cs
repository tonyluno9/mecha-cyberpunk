using MechaCyberpunk.Mecha.Estado;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class ReglaVidaPersistentePruebas
    {
        private const float Piso = 0.15f;
        private const float Margen = 1e-3f;

        [Test]
        public void AlPreparar_ConVidaSobreElPiso_ConservaLaVida()
        {
            Assert.AreEqual(70f, ReglaVidaPersistente.AlPreparar(70f, 100f, Piso), Margen);
        }

        [Test]
        public void AlPreparar_ConVidaBajoElPiso_SubeAlPiso()
        {
            Assert.AreEqual(15f, ReglaVidaPersistente.AlPreparar(5f, 100f, Piso), Margen);
        }

        [Test]
        public void AlPreparar_ConVidaCero_SubeAlPiso()
        {
            Assert.AreEqual(15f, ReglaVidaPersistente.AlPreparar(0f, 100f, Piso), Margen);
        }

        [Test]
        public void AlFijarFraccion_ConFraccionNormal_DaEsaFraccionDeLaMaxima()
        {
            Assert.AreEqual(45f, ReglaVidaPersistente.AlFijarFraccion(100f, 0.45f, Piso), Margen);
        }

        [Test]
        public void AlFijarFraccion_BajoElPiso_SubeAlPiso()
        {
            Assert.AreEqual(15f, ReglaVidaPersistente.AlFijarFraccion(100f, 0.05f, Piso), Margen);
            Assert.AreEqual(15f, ReglaVidaPersistente.AlFijarFraccion(100f, 0f, Piso), Margen);
        }

        [Test]
        public void AlFijarFraccion_SobreLaMaxima_QuedaEnLaMaxima()
        {
            Assert.AreEqual(100f, ReglaVidaPersistente.AlFijarFraccion(100f, 1.5f, Piso), Margen);
            Assert.AreEqual(100f, ReglaVidaPersistente.AlFijarFraccion(100f, float.NaN, Piso), Margen);
        }

        [Test]
        public void AlCargar_MismaMaxima_ConservaLaVida()
        {
            Assert.AreEqual(40f, ReglaVidaPersistente.AlCargar(40f, 100f, 100f, Piso), Margen);
        }

        [Test]
        public void AlCargar_MaximaDoble_EscalaPorProporcion()
        {
            Assert.AreEqual(80f, ReglaVidaPersistente.AlCargar(40f, 100f, 200f, Piso), Margen);
        }

        [Test]
        public void AlCargar_ConVidaCero_DevuelveElPiso()
        {
            Assert.AreEqual(15f, ReglaVidaPersistente.AlCargar(0f, 100f, 100f, Piso), Margen);
        }

        [Test]
        public void AlCargar_ConMaximaGuardadaCero_DevuelveLaMaximaActual()
        {
            Assert.AreEqual(100f, ReglaVidaPersistente.AlCargar(40f, 0f, 100f, Piso), Margen);
        }

        [Test]
        public void AlCargar_ConVidaMayorQueLaMaxima_LaLimitaALaMaximaActual()
        {
            Assert.AreEqual(100f, ReglaVidaPersistente.AlCargar(500f, 100f, 100f, Piso), Margen);
        }
    }
}
