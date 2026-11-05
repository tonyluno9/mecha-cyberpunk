using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    public class CalculoInclinacionMotoPruebas
    {
        private const float Gravedad = 9.81f;

        [Test]
        public void Grados_ConGiroADerecha_InclinaADerecha_YConGiroAIzquierda_AIzquierda()
        {
            Assert.Greater(CalculoInclinacionMoto.Grados(15f, 0.5f, Gravedad, 1f, 40f), 0f);
            Assert.Less(CalculoInclinacionMoto.Grados(15f, -0.5f, Gravedad, 1f, 40f), 0f);
        }

        [Test]
        public void Grados_ParadaORecta_NoInclina()
        {
            Assert.AreEqual(0f, CalculoInclinacionMoto.Grados(0f, 1f, Gravedad, 1f, 40f), 0.0001f);
            Assert.AreEqual(0f, CalculoInclinacionMoto.Grados(20f, 0f, Gravedad, 1f, 40f), 0.0001f);
        }

        [Test]
        public void Grados_SigueLaFormulaDeUnaMotoReal_YRespetaFactorYMaximo()
        {
            // v·w = 9.81 m/s² => atan(1) = 45°.
            Assert.AreEqual(45f, CalculoInclinacionMoto.Grados(9.81f, 1f, Gravedad, 1f, 90f), 0.01f);
            Assert.AreEqual(22.5f, CalculoInclinacionMoto.Grados(9.81f, 1f, Gravedad, 0.5f, 90f), 0.01f);
            Assert.AreEqual(30f, CalculoInclinacionMoto.Grados(50f, 3f, Gravedad, 1f, 30f), 0.0001f);
            Assert.AreEqual(-30f, CalculoInclinacionMoto.Grados(50f, -3f, Gravedad, 1f, 30f), 0.0001f);
        }

        [Test]
        public void Grados_ConMarchaAtras_InclinaAlRevesQueHaciaAdelante()
        {
            Assert.AreEqual(-CalculoInclinacionMoto.Grados(5f, 0.4f, Gravedad, 1f, 40f), CalculoInclinacionMoto.Grados(-5f, 0.4f, Gravedad, 1f, 40f), 0.0001f);
        }

        [Test]
        public void Suavizar_AcercaSinPasarseDelObjetivo_YConDtCeroNoSeMueve()
        {
            float uno = CalculoInclinacionMoto.Suavizar(0f, 10f, 7f, 0.05f);
            Assert.Greater(uno, 0f);
            Assert.Less(uno, 10f);
            Assert.AreEqual(3f, CalculoInclinacionMoto.Suavizar(3f, 10f, 7f, 0f), 0.0001f);
            Assert.AreEqual(10f, CalculoInclinacionMoto.Suavizar(0f, 10f, 100f, 10f), 0.001f);
        }
    }
}
