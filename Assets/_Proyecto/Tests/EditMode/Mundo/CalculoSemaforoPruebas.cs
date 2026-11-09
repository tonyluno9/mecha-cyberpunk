using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-910: el ciclo de los semáforos (lógica pura): verde, ámbar y rojo en orden, y la esquina cruzada en contrafase.</summary>
    public class CalculoSemaforoPruebas
    {
        private const float Verde = 8f;
        private const float Ambar = 2f;
        private const float Rojo = 10f;
        private const float Paso = 0.25f;

        [Test]
        public void Fase_ArrancaEnVerdeYSigueVerdeAmbarRojo()
        {
            CalculoSemaforo c = new CalculoSemaforo(Verde, Ambar, Rojo);
            Assert.AreEqual(FaseSemaforo.Verde, c.Fase);
            c.Avanzar(Verde - 0.1f);
            Assert.AreEqual(FaseSemaforo.Verde, c.Fase);
            c.Avanzar(0.2f);
            Assert.AreEqual(FaseSemaforo.Ambar, c.Fase);
            c.Avanzar(Ambar);
            Assert.AreEqual(FaseSemaforo.Rojo, c.Fase);
            c.Avanzar(Rojo - 0.5f);
            Assert.AreEqual(FaseSemaforo.Rojo, c.Fase);
            c.Avanzar(0.75f);
            Assert.AreEqual(FaseSemaforo.Verde, c.Fase, "vuelve a empezar");
        }

        [Test]
        public void Avanzar_ConUnTiempoMuyLargo_DaLaVuelta()
        {
            CalculoSemaforo c = new CalculoSemaforo(Verde, Ambar, Rojo);
            c.Avanzar(c.Ciclo * 5f + Verde + 0.5f);
            Assert.AreEqual(FaseSemaforo.Ambar, c.Fase);
            Assert.Less(c.Tiempo, c.Ciclo);
        }

        [Test]
        public void Desfase_DeMedioCiclo_EmpiezaEnRojo()
        {
            CalculoSemaforo c = new CalculoSemaforo(Verde, Ambar, Rojo, (Verde + Ambar + Rojo) * 0.5f);
            Assert.AreEqual(FaseSemaforo.Rojo, c.Fase);
        }

        [Test]
        public void Contrafase_NuncaCoincidenEnVerdeNiEnRojo()
        {
            // Rojo = verde + ámbar: así, mientras una esquina está en verde o ámbar, la cruzada está en rojo.
            CalculoSemaforo a = new CalculoSemaforo(Verde, Ambar, Verde + Ambar);
            CalculoSemaforo b = new CalculoSemaforo(Verde, Ambar, Verde + Ambar, (Verde + Ambar + Verde + Ambar) * 0.5f);
            int verdes = 0;
            for (float t = 0f; t < a.Ciclo * 2f; t += Paso)
            {
                bool avanzaA = a.Fase != FaseSemaforo.Rojo;
                Assert.AreNotEqual(a.Fase == FaseSemaforo.Rojo, b.Fase == FaseSemaforo.Rojo, "en t=" + t + ": una en rojo y la otra no");
                Assert.IsFalse(a.Fase == FaseSemaforo.Verde && b.Fase == FaseSemaforo.Verde, "los dos en verde en t=" + t);
                verdes += a.Fase == FaseSemaforo.Verde ? 1 : 0;
                Assert.AreEqual(avanzaA, b.Fase == FaseSemaforo.Rojo);
                a.Avanzar(Paso);
                b.Avanzar(Paso);
            }
            Assert.Greater(verdes, 0, "sí hubo verde");
        }

        [Test]
        public void Constructor_ConDuracionesInvalidas_NoSeCuelga()
        {
            CalculoSemaforo c = new CalculoSemaforo(0f, -1f, 0f);
            c.Avanzar(1f);
            Assert.Greater(c.Ciclo, 0f);
            Assert.DoesNotThrow(() => { FaseSemaforo f = c.Fase; });
        }
    }
}
