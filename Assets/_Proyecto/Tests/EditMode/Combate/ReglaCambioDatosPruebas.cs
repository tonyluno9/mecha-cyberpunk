using MechaCyberpunk.Combate.Vida;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Combate
{
    public class ReglaCambioDatosPruebas
    {
        [Test]
        public void SinDanio_ConservaCeroDeDanio()
        {
            Assert.AreEqual(130f, ReglaCambioDatos.NuevaVida(100f, 100f, 130f, false), 1e-4f);
        }

        [Test]
        public void ConDanio_SubirMaxima_ConservaElDanio()
        {
            Assert.AreEqual(90f, ReglaCambioDatos.NuevaVida(60f, 100f, 130f, false), 1e-4f);
        }

        [Test]
        public void ConDanio_BajarMaxima_ConservaElDanio()
        {
            Assert.AreEqual(10f, ReglaCambioDatos.NuevaVida(60f, 100f, 50f, false), 1e-4f);
        }

        [Test]
        public void DanioMayorQueLaNuevaMaxima_QuedaEnUnoNuncaCero()
        {
            Assert.AreEqual(1f, ReglaCambioDatos.NuevaVida(20f, 100f, 50f, false), 1e-4f);
        }

        [Test]
        public void EnKO_SigueEnKO()
        {
            Assert.AreEqual(0f, ReglaCambioDatos.NuevaVida(0f, 100f, 130f, true), 1e-4f);
        }
    }
}
