using MechaCyberpunk.Mecha.Taller;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class ReglaBancoTallerPruebas
    {
        [Test]
        public void ConAdeudoYDineroSuficiente_PagaRenta()
        {
            Assert.AreEqual(EstadoBanco.PagarRenta, ReglaBancoTaller.Decidir(500, 500, true, 0f));
            Assert.IsTrue(ReglaBancoTaller.PuedeUsar(EstadoBanco.PagarRenta));
        }

        [Test]
        public void ConAdeudoYSinDinero_EstaBloqueado()
        {
            Assert.AreEqual(EstadoBanco.Bloqueado, ReglaBancoTaller.Decidir(500, 499, true, 60f));
            Assert.IsFalse(ReglaBancoTaller.PuedeUsar(EstadoBanco.Bloqueado));
        }

        [Test]
        public void SinAdeudoYMechaFuera_MechaFuera()
        {
            Assert.AreEqual(EstadoBanco.MechaFuera, ReglaBancoTaller.Decidir(0, 1000, false, 60f));
            Assert.IsFalse(ReglaBancoTaller.PuedeUsar(EstadoBanco.MechaFuera));
        }

        [Test]
        public void SinDanio_SinDanio()
        {
            Assert.AreEqual(EstadoBanco.SinDanio, ReglaBancoTaller.Decidir(0, 1000, true, 0.49f));
            Assert.IsFalse(ReglaBancoTaller.PuedeUsar(EstadoBanco.SinDanio));
        }

        [Test]
        public void ConDanioEnElTaller_RepararAunConDineroCero()
        {
            Assert.AreEqual(EstadoBanco.Reparar, ReglaBancoTaller.Decidir(0, 0, true, 0.5f));
            Assert.AreEqual(EstadoBanco.Reparar, ReglaBancoTaller.Decidir(0, -100, true, 65f));
            Assert.IsTrue(ReglaBancoTaller.PuedeUsar(EstadoBanco.Reparar));
        }

        [Test]
        public void ConAdeudoYMechaFuera_GanaLaRenta()
        {
            Assert.AreEqual(EstadoBanco.PagarRenta, ReglaBancoTaller.Decidir(500, 600, false, 60f));
            Assert.AreEqual(EstadoBanco.Bloqueado, ReglaBancoTaller.Decidir(500, 0, false, 0f));
        }
    }
}
