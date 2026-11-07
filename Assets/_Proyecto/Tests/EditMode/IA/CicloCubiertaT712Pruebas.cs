using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-712: el ciclo del SKULL con cobertura: se esconde (agachado), asoma, dispara y se vuelve a esconder; sin cobertura no se esconde.</summary>
    public class CicloCubiertaT712Pruebas
    {
        private static ReglaTacticaPandillero Nueva() => new ReglaTacticaPandillero(0.25f, 1.8f, 2f);

        [Test]
        public void ConCobertura_SeEscondeAsomaDisparaYSeVuelveAEsconder()
        {
            ReglaTacticaPandillero r = Nueva();
            r.ForzarCubrirse();
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, r.Actual);
            Assert.IsFalse(r.PuedeDisparar, "Escondido no dispara");
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, r.Avanzar(1.0f, 1f, true, false, false, true));
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Asomado, r.Avanzar(1.0f, 1f, true, false, false, true), "Pasado el tiempo escondido, asoma");
            Assert.IsTrue(r.PuedeDisparar, "Asomado sí dispara");
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, r.Avanzar(0.1f, 1f, true, false, true, true), "Tras el disparo se esconde de nuevo");
        }

        [Test]
        public void SiNoAsomaTiros_VuelveAEsconderseAlTerminarElTiempoAsomado()
        {
            ReglaTacticaPandillero r = Nueva();
            r.ForzarCubrirse();
            r.Avanzar(2f, 1f, true, false, false, true);
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Asomado, r.Actual);
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, r.Avanzar(2.1f, 1f, true, false, false, true));
        }

        [Test]
        public void SinCobertura_NoSeEscondeYRodea()
        {
            ReglaTacticaPandillero r = Nueva();
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Rodear, r.Avanzar(1f, 1f, false, false, true, true));
        }
    }
}
