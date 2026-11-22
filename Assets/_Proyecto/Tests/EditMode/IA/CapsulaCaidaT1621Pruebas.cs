using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1621: la lógica pura de la cápsula de caída: escalas, línea de tiempo, holograma y vuelo de los drones.</summary>
    public class CapsulaCaidaT1621Pruebas
    {
        [TestCase(TipoCapsulaCaida.E16, 1.00f)]
        [TestCase(TipoCapsulaCaida.E18, 0.85f)]
        [TestCase(TipoCapsulaCaida.E15, 0.66f)]
        [TestCase(TipoCapsulaCaida.E17, 0.45f)]
        [TestCase(TipoCapsulaCaida.DR02a, 0.70f)]
        [TestCase(TipoCapsulaCaida.E13, 0.46f)]
        [TestCase(TipoCapsulaCaida.DR01, 0.33f)]
        [TestCase(TipoCapsulaCaida.DR04a, 0.30f)]
        public void EscalaDe_CadaTipo_CoincideConLaTabla(TipoCapsulaCaida tipo, float esperada)
        {
            Assert.AreEqual(esperada, CalculoCapsulaCaida.EscalaDe(tipo), 0.0001f);
        }

        [Test]
        public void TipoDronDeNombre_PrefabsDeDron_ReconoceLosCuatroYNoLasNaves()
        {
            Assert.IsTrue(CalculoCapsulaCaida.TipoDronDeNombre("Dron_DR04_a", out TipoCapsulaCaida a));
            Assert.AreEqual(TipoCapsulaCaida.DR04a, a);
            Assert.IsTrue(CalculoCapsulaCaida.TipoDronDeNombre("Dron_E13(Clone)", out TipoCapsulaCaida b));
            Assert.AreEqual(TipoCapsulaCaida.E13, b);
            Assert.IsFalse(CalculoCapsulaCaida.TipoDronDeNombre("Dron_NV03_a", out _));
        }

        [Test]
        public void Holograma_Curva_SigueLosTiemposDeLaTarea()
        {
            var curva = new CalculoCapsulaCaida.CurvaHolo(() => 0f); // parpadeos al mínimo (0.15)
            Assert.AreEqual(0f, curva.Evaluar(-1f).Holo);
            Assert.AreEqual(0.5f, curva.Evaluar(1.5f).Holo, 0.0001f);
            Assert.AreEqual(1f, curva.Evaluar(3f).Holo, 0.0001f);
            Assert.AreEqual(0f, curva.Evaluar(4f).Fade, 0.0001f);
            Assert.AreEqual(0.15f, curva.Evaluar(6f).Fade, 0.0001f);
            Assert.AreEqual(0.6f, curva.Evaluar(17f).Fade, 0.0001f);
            Assert.AreEqual(1f, curva.Evaluar(23f).Fade, 0.0001f);
            Assert.IsFalse(curva.Evaluar(23f).Desactivar);
            Assert.IsTrue(curva.Evaluar(24f).Desactivar);
        }

        [Test]
        public void Holograma_Parpadeos_QuedanEntre015Y065()
        {
            var curva = new CalculoCapsulaCaida.CurvaHolo(() => 1f);
            Assert.AreEqual(0.65f, curva.Parpadeo(5), 0.0001f);
            Assert.AreEqual(0.65f, curva.Evaluar(15f).Fade, 0.0001f);
        }

        [Test]
        public void AlturaDron_Frames_SigueLaTabla()
        {
            Assert.AreEqual(0.1f, CalculoCapsulaCaida.AlturaDron(39f), 0.0001f);
            Assert.AreEqual(0.1f, CalculoCapsulaCaida.AlturaDron(52f), 0.0001f);
            Assert.AreEqual(1.6f, CalculoCapsulaCaida.AlturaDron(64f), 0.0001f);
            Assert.AreEqual(1.9f, CalculoCapsulaCaida.AlturaDron(76f), 0.0001f);
            Assert.AreEqual(1.7f, CalculoCapsulaCaida.AlturaDron(88f), 0.0001f);
            Assert.AreEqual(1.9f, CalculoCapsulaCaida.AlturaDron(100f), 0.0001f);
            Assert.AreEqual(1.75f, CalculoCapsulaCaida.AlturaDron(112f), 0.0001f);
        }

        [Test]
        public void SegundosDeFrame_ImpactoEnElCuadro12_EsUnSegundoMenosUnCuadroSobre24()
        {
            Assert.AreEqual(11f / 24f, CalculoCapsulaCaida.SegundosDeFrame(CalculoCapsulaCaida.FrameImpacto), 0.0001f);
            Assert.AreEqual(1f, CalculoCapsulaCaida.FrameDeSegundos(0f), 0.0001f);
        }
    }
}
