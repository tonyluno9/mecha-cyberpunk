using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1485: lógica pura de la cámara a pie (FOV al correr, balanceo).</summary>
    public class CamaraPulidaT1485EditPruebas
    {
        [Test]
        public void FactorVelocidad_VaDeCeroAUno()
        {
            Assert.AreEqual(0f, LogicaCamaraAPie.FactorVelocidad(0f, 5f, 7.5f), 1e-5f);
            Assert.AreEqual(0.5f, LogicaCamaraAPie.FactorVelocidad(6.25f, 5f, 7.5f), 1e-5f);
            Assert.AreEqual(1f, LogicaCamaraAPie.FactorVelocidad(20f, 5f, 7.5f), 1e-5f);
        }

        [Test]
        public void FovObjetivo_CorrerAbreYApuntarNo()
        {
            Assert.AreEqual(67f, LogicaCamaraAPie.FovObjetivo(60f, 45f, 7f, 1f, false), 1e-5f);
            Assert.AreEqual(60f, LogicaCamaraAPie.FovObjetivo(60f, 45f, 7f, 0f, false), 1e-5f);
            Assert.AreEqual(45f, LogicaCamaraAPie.FovObjetivo(60f, 45f, 7f, 1f, true), 1e-5f);
        }

        [Test]
        public void Suavizar_AvanzaSinPasarseYNoDependeDelCuadro()
        {
            float a = LogicaCamaraAPie.Suavizar(0f, 10f, 5f, 0.1f);
            Assert.Greater(a, 0f);
            Assert.Less(a, 10f);
            float dos = LogicaCamaraAPie.Suavizar(LogicaCamaraAPie.Suavizar(0f, 10f, 5f, 0.05f), 10f, 5f, 0.05f);
            Assert.AreEqual(a, dos, 1e-4f);
        }

        [Test]
        public void Balanceo_QuietoOApuntandoNoMueve()
        {
            BalanceoCamara b = new BalanceoCamara();
            for (int i = 0; i < 120; i++)
            {
                b.Actualizar(0f, 7.5f, true, 0.25f, 0.035f, 0.02f, 6f, 1f / 60f);
            }
            Assert.AreEqual(0f, b.Vertical, 1e-6f);
            Assert.AreEqual(0f, b.Lateral, 1e-6f);
            for (int i = 0; i < 120; i++)
            {
                b.Actualizar(7.5f, 7.5f, false, 0.25f, 0.035f, 0.02f, 6f, 1f / 60f);
            }
            Assert.AreEqual(0f, b.Intensidad, 1e-3f);
            Assert.AreEqual(0f, b.Vertical, 1e-3f);
        }

        [Test]
        public void Balanceo_ProporcionalALaVelocidadYAcotado()
        {
            float lento = RangoVertical(2f);
            float rapido = RangoVertical(7.5f);
            Assert.Greater(lento, 0f);
            Assert.Greater(rapido, lento * 2f, "Correr balancea bastante más que caminar despacio");
            Assert.LessOrEqual(rapido, 0.035f + 1e-5f, "Nunca pasa de la amplitud configurada");
        }

        private static float RangoVertical(float velocidad)
        {
            BalanceoCamara b = new BalanceoCamara();
            float maximo = 0f;
            for (int i = 0; i < 600; i++)
            {
                b.Actualizar(velocidad, 7.5f, true, 0.25f, 0.035f, 0.02f, 6f, 1f / 60f);
                maximo = System.Math.Max(maximo, System.Math.Abs(b.Vertical));
            }
            return maximo;
        }
    }
}
