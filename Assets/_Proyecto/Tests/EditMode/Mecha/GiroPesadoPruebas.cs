using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class GiroPesadoPruebas
    {
        private const float VelocidadGiro = 90f;
        private const float Dt = 0.02f;

        [Test]
        public void Calcular_GiroGrande_NoPasaDeVelocidadPorDt()
        {
            Assert.AreEqual(VelocidadGiro * Dt, GiroPesado.Calcular(500f, Dt, VelocidadGiro), 1e-5f);
            Assert.AreEqual(-VelocidadGiro * Dt, GiroPesado.Calcular(-500f, Dt, VelocidadGiro), 1e-5f);
        }

        [Test]
        public void Calcular_GiroPendiente_SeCompletaEnLosSiguientesFrames()
        {
            float pendiente = 100f;
            int frames = 0;
            while (pendiente > 0f && frames < 1000)
            {
                float giro = GiroPesado.Calcular(pendiente, Dt, VelocidadGiro);
                Assert.LessOrEqual(giro, VelocidadGiro * Dt + 1e-5f);
                pendiente -= giro;
                frames++;
            }
            Assert.AreEqual(0f, pendiente, 1e-4f);
            Assert.Greater(frames, 1);
            Assert.AreEqual(100f / (VelocidadGiro * Dt), frames, 1.5f);
        }

        [Test]
        public void Calcular_GiroPequeno_SeCompletaEnUnFrame()
        {
            Assert.AreEqual(0.5f, GiroPesado.Calcular(0.5f, Dt, VelocidadGiro), 1e-6f);
            Assert.AreEqual(-0.5f, GiroPesado.Calcular(-0.5f, Dt, VelocidadGiro), 1e-6f);
        }

        [Test]
        public void Calcular_ConDtCero_NoGira()
        {
            Assert.AreEqual(0f, GiroPesado.Calcular(50f, 0f, VelocidadGiro));
        }
    }
}
