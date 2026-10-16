using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public sealed class MedidorFramesPruebas
    {
        private static MedidorFrames ConCalentamiento(float lento, int cuantos)
        {
            MedidorFrames m = new MedidorFrames();
            for (int i = 0; i < 400; i++)
            {
                m.Registrar(0.02f); // 2 s de calentamiento y luego unos 300 frames medidos
            }
            for (int i = 0; i < cuantos; i++)
            {
                m.Registrar(lento);
            }
            return m;
        }

        [Test]
        public void IgnoraElCalentamiento()
        {
            MedidorFrames m = new MedidorFrames();
            m.Registrar(0.9f);
            m.Registrar(0.9f);
            for (int i = 0; i < 100; i++)
            {
                m.Registrar(0.02f);
            }
            Assert.IsTrue(m.Cumple, "El frame de 0.9 s ocurrió en el calentamiento");
            Assert.Less(m.Maximo, 0.05f);
            Assert.Less(m.FramesMedidos, 102);
        }

        [Test]
        public void TresFramesDe200ms_Cumplen()
        {
            Assert.IsTrue(ConCalentamiento(0.2f, 3).Cumple);
        }

        [Test]
        public void CuatroFramesDe200ms_NoCumplen_ConPocosFramesMedidos()
        {
            MedidorFrames m = new MedidorFrames(0f);
            for (int i = 0; i < 20; i++)
            {
                m.Registrar(0.02f);
            }
            for (int i = 0; i < 4; i++)
            {
                m.Registrar(0.2f);
            }
            Assert.IsFalse(m.Cumple);
        }

        [Test]
        public void UnFrameDe600ms_NoCumple()
        {
            Assert.IsFalse(ConCalentamiento(0.6f, 1).Cumple);
        }

        [Test]
        public void ElUnoPorCientoDeTolerancia_CreceConMuchosFrames()
        {
            MedidorFrames m = new MedidorFrames(0f);
            for (int i = 0; i < 1000; i++)
            {
                m.Registrar(i % 125 == 0 ? 0.2f : 0.01f); // 8 lentos de 1000
            }
            Assert.IsTrue(m.Cumple, "8 lentos son menos del 1 % de 1000 frames: permitidos 10");
        }

        [Test]
        public void ElResumenTraeElMaximoYElPercentil99()
        {
            MedidorFrames m = ConCalentamiento(0.3f, 1);
            string resumen = m.Resumen();
            StringAssert.Contains("máximo 300 ms", resumen);
            StringAssert.Contains("percentil 99", resumen);
            Assert.AreEqual(0.3f, m.Maximo, 1e-5f);
            Assert.AreEqual(0.02f, m.Percentil99, 1e-5f);
        }
    }
}
