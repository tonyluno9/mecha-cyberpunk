using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1709 (T74): los tiempos y el tamaño del parpadeo de una zona de glitch.</summary>
    public class ReglaGlitchT74Pruebas
    {
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        [TestCase(7f)]
        [TestCase(-3f)]
        public void Valores_SiempreDentroDeLosRangos(float azar)
        {
            Assert.That(ReglaGlitch.Espera(azar), Is.InRange(1f, 4f));
            Assert.That(ReglaGlitch.Duracion(azar), Is.InRange(0.05f, 0.15f));
            Assert.That(ReglaGlitch.Desplazamiento(azar), Is.InRange(0.01f, 0.03f));
        }

        [Test]
        public void Extremos_SonLosLimites()
        {
            Assert.AreEqual(1f, ReglaGlitch.Espera(0f), 0.0001f);
            Assert.AreEqual(4f, ReglaGlitch.Espera(1f), 0.0001f);
            Assert.AreEqual(0.05f, ReglaGlitch.Duracion(0f), 0.0001f);
            Assert.AreEqual(0.15f, ReglaGlitch.Duracion(1f), 0.0001f);
        }

        [Test]
        public void ElZumbido_EsElDelTransformador()
        {
            Assert.AreEqual("OBJ_TRANSFORMADOR", ReglaGlitch.IdZumbido);
        }
    }
}
