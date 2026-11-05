using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-593: cada arma pide el ID de sonido que le toca (ARM_PIS / ESC / FUS / SNI), tanto al disparar como al recargar.</summary>
    public class ArmasSonidoT593Pruebas
    {
        [TestCase(ArmaAPie.Pistola, "ARM_PIS_DISPARO", "ARM_PIS_RECARGA")]
        [TestCase(ArmaAPie.Escopeta, "ARM_ESC_DISPARO", "ARM_ESC_RECARGA_CARTUCHO")]
        [TestCase(ArmaAPie.Fusil, "ARM_FUS_DISPARO", "ARM_FUS_RECARGA")]
        [TestCase(ArmaAPie.Sniper, "ARM_SNI_DISPARO", "ARM_SNI_RECARGA")]
        public void CadaArmaPideSuDisparoYSuRecarga(ArmaAPie arma, string disparo, string recarga)
        {
            Assert.AreEqual(disparo, PistolaAPie.IdDisparo(arma));
            Assert.AreEqual(recarga, PistolaAPie.IdRecarga(arma));
        }

        [Test]
        public void ElFusilYaNoPideLosIdsViejosDeSubfusil()
        {
            Assert.IsFalse(PistolaAPie.IdDisparo(ArmaAPie.Fusil).Contains("SUB"));
            Assert.IsFalse(PistolaAPie.IdRecarga(ArmaAPie.Fusil).Contains("SUB"));
        }
    }
}
