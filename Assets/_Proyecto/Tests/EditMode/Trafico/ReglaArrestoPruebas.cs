using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Trafico
{
    public class ReglaArrestoPruebas
    {
        [Test]
        public void CercaYQuieto_ArrestaTrasLosSegundos()
        {
            ReglaArresto regla = new ReglaArresto(10f, 4f, 2f);
            Assert.IsFalse(regla.Avanzar(true, 5f, 0.5f, 3f));
            Assert.IsTrue(regla.Avanzar(true, 5f, 0.5f, 1f));
        }

        [Test]
        public void SiCorres_OEstaLejos_OsiNoTeVe_NoArresta()
        {
            ReglaArresto regla = new ReglaArresto(10f, 4f, 2f);
            Assert.IsFalse(regla.Avanzar(true, 5f, 8f, 50f), "Corriendo no te arrestan");
            Assert.IsFalse(regla.Avanzar(true, 25f, 0f, 50f), "Lejos no te arrestan");
            Assert.IsFalse(regla.Avanzar(false, 5f, 0f, 50f), "Si no te ve no te arresta");
        }

        [Test]
        public void SiTeMuevesElContadorBajaAlDobleDeRapido()
        {
            ReglaArresto regla = new ReglaArresto(10f, 4f, 2f);
            regla.Avanzar(true, 5f, 0f, 3f);
            regla.Avanzar(true, 5f, 9f, 1f);
            Assert.AreEqual(1f, regla.Acumulado, 0.0001f);
            regla.Avanzar(true, 5f, 9f, 5f);
            Assert.AreEqual(0f, regla.Acumulado, 0.0001f, "No baja de 0");
        }
    }
}
