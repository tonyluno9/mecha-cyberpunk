using MechaCyberpunk.Vehiculos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-595: el motor sube de tono con la rapidez y oscila distinto en cada carro; el claxon queda limitado, variado y con alcance corto.</summary>
    public class ReglaMotorSonidoPruebas
    {
        [Test]
        public void Tono_ConMasRapidez_Sube()
        {
            Assert.Less(ReglaMotorSonido.TonoBase(0f, 0.8f, 2f, 20f), ReglaMotorSonido.TonoBase(10f, 0.8f, 2f, 20f));
            Assert.AreEqual(2f, ReglaMotorSonido.TonoBase(1000f, 0.8f, 2f, 20f), 1e-5f);
        }

        [Test]
        public void Tono_ConOscilacion_VariaConElTiempoYEntreCarros()
        {
            float a0 = ReglaMotorSonido.Tono(5f, 0.37f, 3.3f, 0.8f, 2f, 20f, 0.04f, 0.6f);
            float a1 = ReglaMotorSonido.Tono(5f, 2.9f, 3.3f, 0.8f, 2f, 20f, 0.04f, 0.6f);
            float b0 = ReglaMotorSonido.Tono(5f, 0.37f, 47.7f, 0.8f, 2f, 20f, 0.04f, 0.6f);
            Assert.AreNotEqual(a0, a1, "Cambia con el tiempo.");
            Assert.AreNotEqual(a0, b0, "Cada carro tiene su fase.");
            float @base = ReglaMotorSonido.TonoBase(5f, 0.8f, 2f, 20f);
            Assert.That(a1, Is.InRange(@base * 0.96f, @base * 1.04f));
        }
    }
}
