using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    public class CalculoSuspensionPruebas
    {
        private const float Rigidez = 18000f;
        private const float Amortiguamiento = 3500f;

        [Test]
        public void Fuerza_SinCompresion_EsCero()
        {
            Assert.AreEqual(0f, CalculoSuspension.Fuerza(0f, 0f, Rigidez, Amortiguamiento));
            Assert.AreEqual(0f, CalculoSuspension.Fuerza(-0.1f, 5f, Rigidez, Amortiguamiento));
        }

        [Test]
        public void Fuerza_CrecePorCompresion()
        {
            float poca = CalculoSuspension.Fuerza(0.05f, 0f, Rigidez, Amortiguamiento);
            float mucha = CalculoSuspension.Fuerza(0.2f, 0f, Rigidez, Amortiguamiento);
            Assert.Greater(poca, 0f);
            Assert.Greater(mucha, poca);
            Assert.AreEqual(Rigidez * 0.2f, mucha, 0.001f);
        }

        [Test]
        public void Fuerza_EstirandoseRapido_NuncaEsNegativa()
        {
            float fuerza = CalculoSuspension.Fuerza(0.01f, -10f, Rigidez, Amortiguamiento);
            Assert.AreEqual(0f, fuerza);
        }

        [Test]
        public void Fuerza_ComprimiendoseRapido_SumaAmortiguamiento()
        {
            float quieta = CalculoSuspension.Fuerza(0.1f, 0f, Rigidez, Amortiguamiento);
            float rapida = CalculoSuspension.Fuerza(0.1f, 1f, Rigidez, Amortiguamiento);
            Assert.AreEqual(Amortiguamiento, rapida - quieta, 0.001f);
        }
    }
}
