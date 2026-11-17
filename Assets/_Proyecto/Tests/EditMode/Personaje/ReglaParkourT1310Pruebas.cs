using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1310: reglas puras del parkour y la caída.</summary>
    public class ReglaParkourT1310Pruebas
    {
        [TestCase(0.4f, AccionParkour.Ninguna)]
        [TestCase(0.9f, AccionParkour.SaltarBarda)]
        [TestCase(1.3f, AccionParkour.SaltarBarda)]
        [TestCase(1.8f, AccionParkour.TreparBorde)]
        [TestCase(2.2f, AccionParkour.TreparBorde)]
        [TestCase(2.5f, AccionParkour.Ninguna)]
        public void Clasificar_SegunAltura_ElijeLaAccion(float altura, AccionParkour esperada)
        {
            Assert.AreEqual(esperada, ReglaParkour.Clasificar(altura, 0.6f, 1.3f, 2.2f));
        }

        [Test]
        public void DebeRodar_SoloDesdeLaAlturaConfigurada()
        {
            Assert.IsFalse(ReglaParkour.DebeRodar(3.9f, 4f));
            Assert.IsTrue(ReglaParkour.DebeRodar(4f, 4f));
        }

        [Test]
        public void DanioCaida_BajoElUmbral_EsCero()
        {
            Assert.AreEqual(0f, ReglaParkour.DanioCaida(3f, 4f, 2f, 0.3f, 100f, 100f));
        }

        [Test]
        public void DanioCaida_DeOchoMetros_TienePocoDanioConTope()
        {
            float danio = ReglaParkour.DanioCaida(8f, 4f, 2f, 0.3f, 100f, 100f);
            Assert.AreEqual(8f, danio, 0.001f);
            Assert.AreEqual(30f, ReglaParkour.DanioCaida(100f, 4f, 2f, 0.3f, 100f, 100f), 0.001f, "Tope: 30 % de la vida máxima");
        }

        [Test]
        public void DanioCaida_NuncaMata()
        {
            Assert.AreEqual(4f, ReglaParkour.DanioCaida(100f, 4f, 2f, 0.9f, 5f, 100f), 0.001f, "Con 5 de vida deja 1");
            Assert.AreEqual(0f, ReglaParkour.DanioCaida(100f, 4f, 2f, 0.9f, 1f, 100f), 0.001f);
        }
    }
}
